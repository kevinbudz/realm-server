using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        //--projpath-verify replay: drives the real ResolveEnemyHit (closed form)
        //and a copy of the pre-W09 resim (LegacyEnemyHit) over the same shots in
        //a dungeon world. Shots are generated per desc with a random bullet id
        //(positive and negative), angle and hit time, and the target stands on
        //the client's position at that time. A third of the shots get a void
        //tile (a wall) on the client path. "Legit" means the client path is
        //clear at 1 ms steps, so a refused legit shot is a false rejection.
        //Returns the summary text; ok is false when the new path refuses a legit shot.
        public static string VerifyEnemyHitReplay(List<ProjectileDesc> descs, int casesPerDesc, out bool ok)
        {
            ok = false;
            if (!Resources.Worlds.TryGetValue("Spider Den", out WorldDesc wdesc)
                || !Dungeons.DungeonWorld.IsSupported(wdesc))
                return "replay: Spider Den world desc missing";
            ObjectDesc enemyDesc = Resources.Type2Object.Values
                .Where(o => o.Enemy && !o.Static).OrderBy(o => o.Type).FirstOrDefault();
            if (enemyDesc == null)
                return "replay: no enemy desc";

            World world = Manager.CreateDungeonWorld(wdesc);
            List<IntPoint> spawns = world.GetSpawnPoints();
            if (spawns.Count == 0)
                return "replay: dungeon has no spawn points";
            Position spawn = spawns[0].ToPosition();

            Player player = CreateVerifyPlayer("ProjPathReplay");
            Random rng = new Random(12345);
            ReplayTally tally = new ReplayTally();
            try
            {
                if (world.AddEntity(player, spawn) == -1)
                    return "replay: failed to add player";

                foreach (ProjectileDesc d in descs)
                {
                    for (int c = 0; c < casesPerDesc; c++)
                        RunReplayCase(world, player, enemyDesc, d, c, rng, tally);
                }
            }
            finally
            {
                player.Client.State = ProtocolState.Disconnected;
                if (player.Parent == world)
                    world.RemoveEntity(player);
            }

            ok = tally.NewFalseRejects == 0;
            return tally.Summary();
        }

        private sealed class ReplayTally
        {
            public int Cases, Skipped, Legit, NewFalseRejects, OldFalseRejects, OldFalseRejectsParity;
            public int ZeroElapsed, OldZeroElapsedRejects, OldZeroStepRejects, OldOtherRejects, NewMiss;
            public int WallCases, NewWallRejects, OldThroughWall, NewThroughWall;
            public readonly SortedDictionary<string, int[]> ByKind = new SortedDictionary<string, int[]>(StringComparer.Ordinal);

            public int[] Kind(ProjectileDesc d)
            {
                string kind = d.Wavy ? "wavy" : d.Parametric ? "parametric" : d.Boomerang ? "boomerang" : "linear";
                if (d.Amplitude != 0) kind += "+amplitude";
                if (!ByKind.TryGetValue(kind, out int[] counts))
                    ByKind[kind] = counts = new int[4];
                return counts; // [0] legit shots, [1] new false rejects, [2] old false rejects, [3] cases
            }

            public string Summary()
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"  replay: {Cases} shots ({Skipped} skipped, unplaceable target), legit {Legit}, wall-on-path {WallCases}, elapsed 0 {ZeroElapsed}");
                sb.AppendLine($"  replay: new false rejects {NewFalseRejects} (must be 0); old false rejects {OldFalseRejects} (parity {OldFalseRejectsParity}, elapsed 0 {OldZeroElapsedRejects})");
                sb.AppendLine($"  replay: old false rejects by cause: zero-speed resim (0 steps) {OldZeroStepRejects}, other {OldOtherRejects}");
                sb.AppendLine($"  replay: wall-on-path: new rejects {NewWallRejects}, old accepted through wall {OldThroughWall}, new accepted through wall (sweep miss) {NewThroughWall}, new neither hit nor removed {NewMiss}");
                foreach (KeyValuePair<string, int[]> kv in ByKind)
                    sb.AppendLine($"  replay {kv.Key,-22} shots {kv.Value[3],6} legit {kv.Value[0],6} new false rejects {kv.Value[1],3} old false rejects {kv.Value[2],5}");
                return sb.ToString().TrimEnd();
            }

        }

        private static void RunReplayCase(World world, Player player, ObjectDesc enemyDesc, ProjectileDesc d, int c, Random rng, ReplayTally tally)
        {
            int[] kindCounts = tally.Kind(d);
            tally.Cases++;
            kindCounts[3]++;
            int bulletId = rng.Next(4) == 0 ? rng.Next(0, 4) : -rng.Next(1, 9);
            float angle = (float)(rng.NextDouble() * 2 * Math.PI - Math.PI);
            int elapsed = c == 0 ? 0 : rng.Next(1, d.LifetimeMS + 1);
            if (c == 0)
                tally.ZeroElapsed++;
            Position start = player.Position;

            Projectile probe = new Projectile(player, d, bulletId, Manager.TotalTime, angle, start, 1);
            bool legit = true;
            for (int t = 0; t <= elapsed && legit; t++)
                if (player.ProjectileBlockedAt(probe, ProjectilePath.PositionAt(d, start, probe.Angle, bulletId, t)))
                    legit = false;

            //Wall on the client path (about a third of the shots, not at t = 0 or the hit).
            bool wallSet = false;
            ushort wallPrev = 0;
            int wx = 0, wy = 0;
            bool wallCase = c % 3 == 2 && elapsed > 1;
            if (wallCase)
            {
                int tw = rng.Next(1, elapsed);
                Position wp = ProjectilePath.PositionAt(d, start, probe.Angle, bulletId, tw);
                wx = (int)wp.X;
                wy = (int)wp.Y;
                if (wp.X >= 0 && wp.Y >= 0 && wx < world.Width && wy < world.Height)
                {
                    wallPrev = world.Tiles[wx, wy].Type;
                    world.Tiles[wx, wy].Type = 255;
                    wallSet = true;
                }
                legit = false;
                tally.WallCases++;
            }

            Position target = ProjectilePath.PositionAt(d, start, probe.Angle, bulletId, elapsed);
            Enemy enemy = new Enemy(enemyDesc.Type);
            try
            {
                if (world.AddEntity(enemy, target) == -1)
                {
                    tally.Skipped++;
                    return;
                }
                enemy.HP = 1000000;

                //New: the real closed-form decision.
                Projectile pNew = new Projectile(player, d, bulletId, Manager.TotalTime, angle, start, 1);
                player.ShotProjectiles[bulletId] = pNew;
                player.ResolveEnemyHit(bulletId, pNew, enemy, elapsed);
                bool newAccept = pNew.Hit.Contains(enemy.Id);
                bool newWall = !newAccept && !player.ShotProjectiles.ContainsKey(bulletId);
                bool newMiss = !newAccept && !newWall;
                if (newMiss)
                    tally.NewMiss++;
                player.ShotProjectiles.Remove(bulletId);

                //Old: the pre-W09 resim on a fresh bullet (no CanHit side effects).
                Projectile pOld = new Projectile(player, d, bulletId, Manager.TotalTime, angle, start, 1);
                bool oldAccept = LegacyEnemyHitAccepts(player, pOld, enemy, elapsed, out bool oldWall);

                if (legit)
                {
                    tally.Legit++;
                    kindCounts[0]++;
                    if (!newAccept)
                    {
                        tally.NewFalseRejects++;
                        kindCounts[1]++;
                    }
                    if (!oldAccept)
                    {
                        tally.OldFalseRejects++;
                        kindCounts[2]++;
                        if (elapsed == 0)
                            tally.OldZeroElapsedRejects++;
                        else if (LegacySteps(d, elapsed) == 0)
                            tally.OldZeroStepRejects++;
                        else if (d.Parametric && bulletId % 2 != 0 && bulletId < 0)
                            tally.OldFalseRejectsParity++;
                        else
                            tally.OldOtherRejects++;
                    }
                }
                else if (wallCase)
                {
                    if (newWall) tally.NewWallRejects++;
                    if (oldAccept) tally.OldThroughWall++;
                    if (newAccept) tally.NewThroughWall++;
                }
            }
            finally
            {
                if (wallSet)
                    world.Tiles[wx, wy].Type = wallPrev;
                if (enemy.Parent == world)
                    world.RemoveEntity(enemy);
            }
        }

        //Pre-W09 TryHitEnemy decision, kept only for the replay: the float
        //re-simulation (steps = ceil(Speed/100 * elapsed * 8 / 1000), wall check
        //on every earlier sample, hit test at the last) and the old PositionAt,
        //including its `Id % 2 == 1` parity and float precision.
        private static int LegacySteps(ProjectileDesc d, int elapsed)
        {
            return (int)Math.Ceiling((d.Speed / 100f) * ((elapsed * 8) / 1000f));
        }

        private static bool LegacyEnemyHitAccepts(Player player, Projectile p, Entity target, int elapsed, out bool wallRemoved)
        {
            wallRemoved = false;
            int steps = LegacySteps(p.Desc, elapsed);
            float timeStep = (float)elapsed / steps;
            for (int k = 0; k <= steps; k++)
            {
                Position pos = LegacyPositionAt(p, k * timeStep);
                if (k == steps)
                {
                    if (target.Desc.Static)
                    {
                        if (pos.Distance(target.Position) <= EnemyHitRangeAllowance)
                            return true;
                    }
                    else
                    {
                        for (int j = 0; j <= EnemyHitHistoryBacktrack; j++)
                            if (pos.Distance(target.TryGetHistory(j)) <= EnemyHitRangeAllowance)
                                return true;
                    }
                    return false;
                }
                if (player.ProjectileBlockedAt(p, pos))
                {
                    wallRemoved = true;
                    return false;
                }
            }
            return false;
        }

        private static Position LegacyPositionAt(Projectile p, float elapsed)
        {
            Position pos = new Position(p.StartPosition.X, p.StartPosition.Y);
            float speed = p.Desc.Speed;
            if (p.Desc.Accelerate) speed *= elapsed / p.Desc.LifetimeMS;
            if (p.Desc.Decelerate) speed *= 2 - elapsed / p.Desc.LifetimeMS;
            float dist = elapsed * (speed / 10000f);
            float phase = p.Id % 2 == 0 ? 0 : MathF.PI;
            if (p.Desc.Wavy)
            {
                float theta = p.Angle + (MathF.PI / 64.0f) * MathF.Sin(phase + 6 * MathF.PI * elapsed / 1000.0f);
                pos.X = pos.X + dist * MathF.Cos(theta);
                pos.Y = pos.Y + dist * MathF.Sin(theta);
            }
            else if (p.Desc.Parametric)
            {
                float t = elapsed / p.Desc.LifetimeMS * 2 * MathF.PI;
                float x = MathF.Sin(t) * (p.Id % 2 == 1 ? 1 : -1);
                float y = MathF.Sin(2 * t) * (p.Id % 4 < 2 ? 1 : -1);
                float sin = MathF.Sin(p.Angle);
                float cos = MathF.Cos(p.Angle);
                pos.X = pos.X + (((x * cos) - (y * sin)) * p.Desc.Magnitude);
                pos.Y = pos.Y + (((x * sin) + (y * cos)) * p.Desc.Magnitude);
            }
            else
            {
                if (p.Desc.Boomerang)
                {
                    float halfway = p.Desc.LifetimeMS * (p.Desc.Speed / 10000) / 2;
                    if (dist > halfway) dist = halfway - (dist - halfway);
                }
                pos.X = pos.X + dist * MathF.Cos(p.Angle);
                pos.Y = pos.Y + dist * MathF.Sin(p.Angle);
                if (p.Desc.Amplitude != 0)
                {
                    float deflection = p.Desc.Amplitude * MathF.Sin(phase + elapsed / p.Desc.LifetimeMS * p.Desc.Frequency * 2 * MathF.PI);
                    pos.X = pos.X + deflection * MathF.Cos(p.Angle + MathF.PI / 2);
                    pos.Y = pos.Y + deflection * MathF.Sin(p.Angle + MathF.PI / 2);
                }
            }
            return pos;
        }
    }
}
