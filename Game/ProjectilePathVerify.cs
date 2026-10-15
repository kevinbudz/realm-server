using RotMG.Common;
using RotMG.Game.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RotMG.Game
{
    //--projpath-verify [golden.bin]: checks ProjectilePath against the client's
    //projectile_position (realm-cpp src/objects/ProjectileProperties.cpp).
    //Every projectile desc in the loaded GameData (object and item descs) is
    //sampled at 8 bullet ids (all sign and phase classes) x 3 angles, at
    //t = 0..lifetime in 10 ms steps (lifetime itself is always the last sample).
    //Without an argument the reference is ClientReference below, a literal
    //copy of the C++ function. With a golden file, the reference is the C++
    //output written by tools/projpath (realm-cpp's function compiled verbatim),
    //and every loaded desc must appear in it. Also checks the CoreTests.cpp
    //expectations as anchors. Pass: max error <= MaxErrorTiles everywhere.
    public static class ProjectilePathVerify
    {
        public const double MaxErrorTiles = 0.01;
        private const int StepMs = 10;
        private const int ReplayCasesPerDesc = 12;
        private static readonly int[] BulletIds = { 0, 1, 2, 3, -1, -2, -3, -4 };
        private static readonly double[] Angles = { 0.0, 1.234, -2.7 };
        private const float StartX = 50.5f;
        private const float StartY = 60.25f;

        private sealed class KindStats
        {
            public int Descs;
            public long Samples;
            public double MaxError;
            public double MaxFloatError;
            public string Worst = "";
        }

        public static bool Run(string goldenPath)
        {
            bool ok = CheckAnchors();
            Dictionary<string, KindStats> stats = new Dictionary<string, KindStats>();
            List<KeyValuePair<string, ProjectileDesc>> descs = LoadedDescs();
            int checkedDescs = 0;

            if (goldenPath == null)
            {
                foreach (KeyValuePair<string, ProjectileDesc> d in descs)
                {
                    CheckDesc(d.Key, d.Value, stats);
                    checkedDescs++;
                }
            }
            else
            {
                checkedDescs = CheckGolden(goldenPath, descs, stats, ref ok);
                if (checkedDescs != descs.Count)
                {
                    Program.Print(PrintType.Error, $"projpath verify: golden covers {checkedDescs} of {descs.Count} loaded descs");
                    ok = false;
                }
            }

            long samples = 0;
            foreach (KeyValuePair<string, KindStats> kv in stats.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                samples += kv.Value.Samples;
                Console.WriteLine($"  {kv.Key,-22} descs {kv.Value.Descs,4}  samples {kv.Value.Samples,8}  max err {kv.Value.MaxError:E3} tiles  (float Position {kv.Value.MaxFloatError:E3})  worst <{kv.Value.Worst}>");
                if (kv.Value.MaxError > MaxErrorTiles)
                    ok = false;
            }
            string replay = Player.VerifyEnemyHitReplay(descs.Select(d => d.Value).ToList(), ReplayCasesPerDesc, out bool replayOk);
            Console.WriteLine(replay);
            ok &= replayOk;
            Console.WriteLine($"projpath verify: {descs.Count} loaded descs, {checkedDescs} checked, {samples} samples, reference={(goldenPath == null ? "ClientReference" : "golden " + Path.GetFileName(goldenPath))}");
            return ok;
        }

        //All loaded projectile descs, keyed "<containerType>/<index>" the way
        //tools/projpath writes them: object descs by Projectiles index, item
        //descs by their single Projectile (index 0).
        private static List<KeyValuePair<string, ProjectileDesc>> LoadedDescs()
        {
            List<KeyValuePair<string, ProjectileDesc>> list = new List<KeyValuePair<string, ProjectileDesc>>();
            foreach (ObjectDesc o in Resources.Type2Object.Values.OrderBy(o => o.Type))
                foreach (KeyValuePair<int, ProjectileDesc> kv in o.Projectiles.OrderBy(k => k.Key))
                    list.Add(new KeyValuePair<string, ProjectileDesc>($"{kv.Value.ContainerType}/{kv.Key}", kv.Value));
            foreach (ItemDesc i in Resources.Type2Item.Values.OrderBy(i => i.Type))
                if (i.Projectile != null)
                    list.Add(new KeyValuePair<string, ProjectileDesc>($"{i.Projectile.ContainerType}/0", i.Projectile));
            return list;
        }

        private static string KindOf(ProjectileDesc d)
        {
            string kind = d.Wavy ? "wavy" : d.Parametric ? "parametric" : d.Boomerang ? "boomerang" : "linear";
            if (d.Amplitude != 0) kind += "+amplitude";
            if (d.Accelerate) kind += "+accelerate";
            if (d.Decelerate) kind += "+decelerate";
            return kind;
        }

        private static KindStats StatsFor(Dictionary<string, KindStats> stats, ProjectileDesc d)
        {
            string kind = KindOf(d);
            if (!stats.TryGetValue(kind, out KindStats s))
                stats[kind] = s = new KindStats();
            return s;
        }

        private static void CheckDesc(string key, ProjectileDesc d, Dictionary<string, KindStats> stats)
        {
            KindStats s = StatsFor(stats, d);
            s.Descs++;
            Position start = new Position(StartX, StartY);
            int n = SampleCount(d.LifetimeMS);
            foreach (int id in BulletIds)
                foreach (double angle in Angles)
                    for (int i = 0; i < n; i++)
                    {
                        double t = SampleTime(d.LifetimeMS, i);
                        ProjectilePath.Evaluate(d, start.X, start.Y, angle, id, t, out double x, out double y);
                        ClientReference(d, start.X, start.Y, angle, id, t, out double rx, out double ry);
                        Record(s, key, id, angle, t, x, y, rx, ry, ProjectilePath.PositionAt(d, start, angle, id, t));
                    }
        }

        private static void Record(KindStats s, string key, int id, double angle, double t, double x, double y, double rx, double ry, Position f)
        {
            s.Samples++;
            double err = Math.Sqrt((x - rx) * (x - rx) + (y - ry) * (y - ry));
            double ferr = Math.Sqrt((f.X - rx) * (f.X - rx) + (f.Y - ry) * (f.Y - ry));
            if (ferr > s.MaxFloatError)
                s.MaxFloatError = ferr;
            if (err > s.MaxError || double.IsNaN(err))
            {
                s.MaxError = double.IsNaN(err) ? double.PositiveInfinity : err;
                s.Worst = $"{key} id {id} angle {angle} t {t}";
            }
        }

        private static int SampleCount(int lifetimeMs)
        {
            return (lifetimeMs + StepMs - 1) / StepMs + 1;
        }

        private static double SampleTime(int lifetimeMs, int i)
        {
            return Math.Min(i * StepMs, lifetimeMs);
        }

        //Literal copy of realm-cpp projectile_position on raw doubles. Keep the
        //operand order identical to the C++ (it changes the last bit only, but
        //the verify is about exact parity).
        private static void ClientReference(ProjectileDesc d, double sx, double sy, double angle, int id, double elapsed, out double x, out double y)
        {
            double lifetime = d.LifetimeMS;
            double baseSpeed = (int)d.Speed; // client integer() truncation
            double speed = baseSpeed;
            if (d.Accelerate) speed *= elapsed / lifetime;
            if (d.Decelerate) speed *= 2 - elapsed / lifetime;
            double distance = elapsed * (speed / 10000);
            double phase = id % 2 == 0 ? 0 : Math.PI;
            x = sx;
            y = sy;
            if (d.Wavy)
            {
                double theta = angle + Math.PI / 64 * Math.Sin(phase + 6 * Math.PI * elapsed / 1000);
                x += distance * Math.Cos(theta);
                y += distance * Math.Sin(theta);
            }
            else if (d.Parametric)
            {
                double t = elapsed / lifetime * 2 * Math.PI;
                double vx = Math.Sin(t) * (id % 2 != 0 ? 1 : -1);
                double vy = Math.Sin(2 * t) * (id % 4 < 2 ? 1 : -1);
                double sine = Math.Sin(angle);
                double cosine = Math.Cos(angle);
                x += (vx * cosine - vy * sine) * d.Magnitude;
                y += (vx * sine + vy * cosine) * d.Magnitude;
            }
            else
            {
                if (d.Boomerang)
                {
                    double halfway = lifetime * (baseSpeed / 10000) / 2;
                    if (distance > halfway)
                        distance = halfway - (distance - halfway);
                }
                x += distance * Math.Cos(angle);
                y += distance * Math.Sin(angle);
                if (d.Amplitude != 0)
                {
                    double offset = d.Amplitude * Math.Sin(phase + elapsed / lifetime * d.Frequency * 2 * Math.PI);
                    x += offset * Math.Cos(angle + Math.PI / 2);
                    y += offset * Math.Sin(angle + Math.PI / 2);
                }
            }
        }

        //Golden file (little-endian, written by tools/projpath/golden_harness.cpp):
        //  char[4] "PPG1", int32 recordCount, then per record:
        //  uint16 containerType, uint8 index, int32 bulletId, double angle,
        //  double startX, double startY, int32 lifetimeMs, int32 sampleCount,
        //  sampleCount x (double x, double y), sample i at min(10*i, lifetimeMs).
        private static int CheckGolden(string path, List<KeyValuePair<string, ProjectileDesc>> descs, Dictionary<string, KindStats> stats, ref bool ok)
        {
            Dictionary<string, ProjectileDesc> byKey = descs.ToDictionary(k => k.Key, k => k.Value);
            HashSet<string> seen = new HashSet<string>();
            using (BinaryReader r = new BinaryReader(File.OpenRead(path)))
            {
                string magic = new string(r.ReadChars(4));
                if (magic != "PPG1")
                {
                    Program.Print(PrintType.Error, $"projpath verify: bad golden magic in {path}");
                    ok = false;
                    return 0;
                }
                int records = r.ReadInt32();
                for (int k = 0; k < records; k++)
                {
                    ushort type = r.ReadUInt16();
                    int index = r.ReadByte();
                    int id = r.ReadInt32();
                    double angle = r.ReadDouble();
                    double sx = r.ReadDouble();
                    double sy = r.ReadDouble();
                    int lifetime = r.ReadInt32();
                    int count = r.ReadInt32();
                    string key = $"{type}/{index}";
                    if (!byKey.TryGetValue(key, out ProjectileDesc d))
                    {
                        Program.Print(PrintType.Error, $"projpath verify: golden desc <{key}> is not loaded");
                        ok = false;
                        for (int i = 0; i < count * 2; i++) r.ReadDouble();
                        continue;
                    }
                    if (d.LifetimeMS != lifetime || count != SampleCount(lifetime))
                    {
                        Program.Print(PrintType.Error, $"projpath verify: <{key}> lifetime {d.LifetimeMS} vs golden {lifetime}");
                        ok = false;
                    }
                    if (seen.Add(key))
                        StatsFor(stats, d).Descs++;
                    KindStats s = StatsFor(stats, d);
                    Position start = new Position((float)sx, (float)sy);
                    for (int i = 0; i < count; i++)
                    {
                        double rx = r.ReadDouble();
                        double ry = r.ReadDouble();
                        double t = SampleTime(lifetime, i);
                        ProjectilePath.Evaluate(d, sx, sy, angle, id, t, out double x, out double y);
                        Record(s, key, id, angle, t, x, y, rx, ry, ProjectilePath.PositionAt(d, start, angle, id, t));
                    }
                }
            }
            return seen.Count;
        }

        //CoreTests.cpp expectations (realm-cpp tests/CoreTests.cpp:67-112), in
        //the same units. NaN means "not asserted on this axis".
        private static bool CheckAnchors()
        {
            const string plain = "<Projectile id='0'><ObjectId>a</ObjectId><LifetimeMS>1000</LifetimeMS><Speed>100</Speed>";
            const string amp = "<Amplitude>2</Amplitude><Frequency>1</Frequency>";
            const string par = "<Parametric/><Magnitude>3</Magnitude>";
            const double N = double.NaN;
            Anchor[] anchors =
            {
                new Anchor(plain + "</Projectile>", 0, 0, 5, 7, 250, 7.5, 7, "straight distance"),
                new Anchor(plain + "<Accelerate/></Projectile>", 0, 0, 0, 0, 500, 2.5, N, "Flash acceleration convention"),
                new Anchor(plain + "<Decelerate/></Projectile>", 0, 0, 0, 0, 500, 7.5, N, "deceleration convention"),
                new Anchor(plain + "<Boomerang/></Projectile>", 0, 0, 0, 0, 500, 5, N, "boomerang apex"),
                new Anchor(plain + "<Boomerang/></Projectile>", 0, 0, 0, 0, 750, 2.5, N, "boomerang return"),
                new Anchor(plain + "<Boomerang/></Projectile>", 0, 0, 0, 0, 1000, 0, N, "boomerang origin"),
                new Anchor(plain + "<Boomerang/><Accelerate/></Projectile>", 0, 0, 0, 0, 750, 4.375, N, "acceleration/boomerang uses base-speed halfway"),
                new Anchor(plain + amp + "</Projectile>", 0, 0, 0, 0, 250, 2.5, 2, "sine distance, even phase"),
                new Anchor(plain + amp + "</Projectile>", 1, 0, 0, 0, 250, N, -2, "odd bullet sine phase"),
                new Anchor(plain + par + "</Projectile>", 0, 0, 0, 0, 250, -3, N, "even parametric phase"),
                new Anchor(plain + par + "</Projectile>", 1, 0, 0, 0, 250, 3, N, "odd parametric phase"),
                new Anchor(plain + par + "</Projectile>", 0, 0, 0, 0, 125, N, 3, "parametric second harmonic"),
                new Anchor(plain + par + "</Projectile>", 2, 0, 0, 0, 125, N, -3, "parametric quadrant phase"),
                new Anchor(plain + par + "</Projectile>", -1, 0, 0, 0, 125, N, 3, "negative bullet preserves AS3 signed remainder"),
                new Anchor(plain + par + "</Projectile>", -2, 0, 0, 0, 125, N, 3, "negative even bullet preserves AS3 signed remainder"),
                new Anchor(plain + "<Wavy/></Projectile>", 0, 0, 0, 0, 250, 2.496988640512931, -0.12266918581854504, "wavy numeric reference"),
                new Anchor("<Projectile id='1'><ObjectId>a</ObjectId><LifetimeMS>400</LifetimeMS><Speed>0.02</Speed></Projectile>", 0, 0, 5, 7, 400, 5, 7, "Speed 0.02 is integer() 0 on the client (Oryx Knight id 1)"),
            };

            bool ok = true;
            foreach (Anchor a in anchors)
            {
                ProjectileDesc d = new ProjectileDesc(XElement.Parse(a.Xml), 0);
                ProjectilePath.Evaluate(d, a.Sx, a.Sy, a.Angle, a.Id, a.T, out double x, out double y);
                bool pass = (double.IsNaN(a.Ex) || Math.Abs(x - a.Ex) <= 1e-9) && (double.IsNaN(a.Ey) || Math.Abs(y - a.Ey) <= 1e-9);
                if (!pass)
                {
                    Program.Print(PrintType.Error, $"projpath anchor <{a.Name}> got ({x:R}, {y:R}), want ({a.Ex:R}, {a.Ey:R})");
                    ok = false;
                }
            }
            Console.WriteLine($"  anchors: {anchors.Length} CoreTests expectations {(ok ? "match" : "MISMATCH")}");
            return ok;
        }

        private sealed class Anchor
        {
            public readonly string Xml;
            public readonly int Id;
            public readonly double Angle, Sx, Sy, T, Ex, Ey;
            public readonly string Name;

            public Anchor(string xml, int id, double angle, double sx, double sy, double t, double ex, double ey, string name)
            {
                Xml = xml; Id = id; Angle = angle; Sx = sx; Sy = sy; T = t; Ex = ex; Ey = ey; Name = name;
            }
        }
    }
}
