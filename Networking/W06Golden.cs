//[W06] Self-checks for the packet layer (--w06-verify): golden bytes for every outbound builder, a span-reader check
//against the stream-backed PacketReader, and writer bounds. The expected hex was captured from the byte[]-returning
//builders before W06, with the inputs below. Update and NewTick are covered only here: in a live world their bytes
//depend on the spawn position and on timing, so the wire captures (tools/hop/pktdiff.py) cannot compare them exactly.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RotMG.Common;
using RotMG.Game;

namespace RotMG.Networking
{
    public static class W06Golden
    {
        //Name and hex of each frame, in the order Builders() returns them.
        private static readonly string[] Expected =
        {
            "MapInfo 14000008000000080000054E6578757300055265616C6DDEADBEEF000000030100",
            "InvResult0 1200000000",
            "InvResult1 1200000001",
            "Failure 00000000020026546F6F206D616E7920636F6E6E656374696F6E732066726F6D20796F75722061646472657373",
            "FailureEmpty 00000000000000",
            "NameResultOk 32010000",
            "NameResultErr 320000104E6F7420656E6F75676820676F6C642E",
            "CreateSuccess 010000002A00000007",
            "Reconnect 130000002A",
            "Ping 3D00000009",
            "Goto 10000000053FC0000040200000",
            "Notification 0A00000005000A7465787420C3A9E4B8ADFFFFFFFF",
            "GlobalNotification 3C000000010002676E",
            "QuestObjId 2300000005",
            "BuyResult 1A0000000000026F6B",
            "GuildResult 2501000167",
            "InvitedToGuild 2B000161000162",
            "TradeRequested 34000178",
            "TradeDone 3A000000010004646F6E65",
            "TradeChanged 3700020100",
            "TradeAccepted 3B000101000100",
            "TradeStart 350001000000010000000201000003796F750001FFFFFFFD000000000001",
            "AccountList 22000000030003000000010000000200000003",
            "AccountListEmpty 22000000000000",
            "PlaySound 2E0005736F756E64",
            "Death 18000000010000000200044F727978",
            "AllyShoot 280000000503003FA00000",
            "Text 0600044E616D6500000005FFFFFFFF050000000568656C6C6F",
            "TextRecipient 0600000000000000000003000003426F620006746F20626F62",
            "Damage 08000000050202010064",
            "DamageNone 0800000005000007",
            "Aoe 1B404000004080000040200000003201FF112233",
            "EnemyShoot 29000004D200000005023F800000400000003F000000003C033DCCCCCD",
            "EnemyShootSingle 29000004D200000005023F800000400000003F000000003C",
            "ShowEffect 0E0100000005FFAABBCC0000000000000000",
            "ShowEffectPos 0E0100000005FFAABBCCBFC00000400000004040000040800000",
            "ThrowEffect 0E0400000005FF00FF003F800000400000004396000000000000",
            "ServerPlayerShoot 07000000010000000203003F8000003F8000003F0000003DCCCCCD00",
            "Update 090002FFFF0005FFFE7FFF80000001000203000000006340600000C01000000300000003021C0005C39C6EC3AF01000000050100FFFFFFF9000000000000000001380000000200000007010000000800",
            "UpdateEmpty 09000000000000",
            "NewTick 0B00010000000B40E000004100000002010000006402000000640301000000641C0001700000000302",
            "NewTickNoStats 0B00010000000B40E00000410000000201000000640200000064",
            "NewTickEmpty 0B0000",
        };

        public static bool Run()
        {
            var failures = new List<string>();
            CheckGolden(failures);
            CheckReader(failures);
            CheckWriter(failures);
            foreach (string f in failures.Count > 20 ? failures.GetRange(0, 20) : failures)
                Console.WriteLine("  W06 verify FAIL: " + f);
            Console.WriteLine($"W06 verify: {Expected.Length} golden frames, reader and writer checks, {failures.Count} mismatches");
            return failures.Count == 0;
        }

        private static void CheckGolden(List<string> failures)
        {
            List<(string Name, Func<string> Frame)> builders = Builders();
            if (builders.Count != Expected.Length)
            {
                failures.Add($"golden: {builders.Count} builders but {Expected.Length} expected frames");
                return;
            }
            for (int i = 0; i < builders.Count; i++)
            {
                string want = Expected[i].Substring(Expected[i].IndexOf(' ') + 1);
                string got = builders[i].Frame();
                if (got != want)
                    failures.Add($"golden {builders[i].Name}: {got} != {want}");
            }
        }

        //Fixed inputs for every outbound builder. Convert.ToHexString takes the span the builder returns.
        private static List<(string Name, Func<string> Frame)> Builders()
        {
            return new List<(string Name, Func<string> Frame)>
            {
                ("MapInfo", () => Convert.ToHexString(GameServer.MapInfo(2048, 2048, "Nexus", "Realm", 0xDEADBEEFu, 3, true, false))),
                ("InvResult0", () => Convert.ToHexString(GameServer.InvResult(0))),
                ("InvResult1", () => Convert.ToHexString(GameServer.InvResult(1))),
                ("Failure", () => Convert.ToHexString(GameServer.Failure(2, "Too many connections from your address"))),
                ("FailureEmpty", () => Convert.ToHexString(GameServer.Failure(0, ""))),
                ("NameResultOk", () => Convert.ToHexString(GameServer.NameResult(true, ""))),
                ("NameResultErr", () => Convert.ToHexString(GameServer.NameResult(false, "Not enough gold."))),
                ("CreateSuccess", () => Convert.ToHexString(GameServer.CreateSuccess(42, 7))),
                ("Reconnect", () => Convert.ToHexString(GameServer.Reconnect(42))),
                ("Ping", () => Convert.ToHexString(GameServer.Ping(9))),
                ("Goto", () => Convert.ToHexString(GameServer.Goto(5, new Position(1.5f, 2.5f)))),
                ("Notification", () => Convert.ToHexString(GameServer.Notification(5, "text é中", 0xFFFFFFFFu))),
                ("GlobalNotification", () => Convert.ToHexString(GameServer.GlobalNotification(1, "gn"))),
                ("QuestObjId", () => Convert.ToHexString(GameServer.QuestObjId(5))),
                ("BuyResult", () => Convert.ToHexString(GameServer.BuyResult(0, "ok"))),
                ("GuildResult", () => Convert.ToHexString(GameServer.GuildResult(true, "g"))),
                ("InvitedToGuild", () => Convert.ToHexString(GameServer.InvitedToGuild("a", "b"))),
                ("TradeRequested", () => Convert.ToHexString(GameServer.TradeRequested("x"))),
                ("TradeDone", () => Convert.ToHexString(GameServer.TradeDone(1, "done"))),
                ("TradeChanged", () => Convert.ToHexString(GameServer.TradeChanged(new[] { true, false }))),
                ("TradeAccepted", () => Convert.ToHexString(GameServer.TradeAccepted(new[] { true }, new[] { false }))),
                ("TradeStart", () => Convert.ToHexString(GameServer.TradeStart(
                    new[] { new TradeItem { Item = 1, SlotType = 2, Tradeable = true, Included = false } },
                    "you",
                    new[] { new TradeItem { Item = -3, SlotType = 0, Tradeable = false, Included = true } }))),
                ("AccountList", () => Convert.ToHexString(GameServer.AccountList(3, new List<int> { 1, 2, 3 }))),
                ("AccountListEmpty", () => Convert.ToHexString(GameServer.AccountList(0, new List<int>()))),
                ("PlaySound", () => Convert.ToHexString(GameServer.PlaySound("sound"))),
                ("Death", () => Convert.ToHexString(GameServer.Death(1, 2, "Oryx"))),
                ("AllyShoot", () => Convert.ToHexString(GameServer.AllyShoot(5, 0x0300, 1.25f))),
                ("Text", () => Convert.ToHexString(GameServer.Text("Name", 5, -1, 5, "", "hello"))),
                ("TextRecipient", () => Convert.ToHexString(GameServer.Text("", 0, 3, 0, "Bob", "to bob"))),
                ("Damage", () => Convert.ToHexString(GameServer.Damage(5, new[] { ConditionEffectIndex.Weak, ConditionEffectIndex.Quiet }, 100))),
                ("DamageNone", () => Convert.ToHexString(GameServer.Damage(5, new ConditionEffectIndex[0], 7))),
                ("Aoe", () => Convert.ToHexString(GameServer.Aoe(new Position(3f, 4f), 2.5f, 50, ConditionEffectIndex.Quiet, 0xFF112233u))),
                ("EnemyShoot", () => Convert.ToHexString(GameServer.EnemyShoot(1234, 5, 2, new Position(1f, 2f), 0.5f, 60, 3, 0.1f))),
                ("EnemyShootSingle", () => Convert.ToHexString(GameServer.EnemyShoot(1234, 5, 2, new Position(1f, 2f), 0.5f, 60, 1, 0.1f))),
                ("ShowEffect", () => Convert.ToHexString(GameServer.ShowEffect(ShowEffectIndex.Heal, 5, 0xFFAABBCCu))),
                ("ShowEffectPos", () => Convert.ToHexString(GameServer.ShowEffect(ShowEffectIndex.Heal, 5, 0xFFAABBCCu,
                    new Position(-1.5f, 2f), new Position(3f, 4f)))),
                ("ThrowEffect", () => Convert.ToHexString(GameServer.ThrowEffect(5, 0xFF00FF00u, new Position(1f, 2f), 300))),
                ("ServerPlayerShoot", () => Convert.ToHexString(GameServer.ServerPlayerShoot(1, 2, 0x0300, new Position(1f, 1f), 0.5f, 0.1f, new List<Projectile>()))),
                ("Update", () => Convert.ToHexString(GameServer.Update(GoldenTiles(), GoldenDefinitions(), GoldenDrops()))),
                ("UpdateEmpty", () => Convert.ToHexString(GameServer.Update(new List<TileData>(), new List<ObjectDefinition>(), new List<ObjectDrop>()))),
                ("NewTick", () => Convert.ToHexString(GameServer.NewTick(GoldenStatuses(), GoldenPlayerStats()))),
                ("NewTickNoStats", () => Convert.ToHexString(GameServer.NewTick(GoldenStatuses(), new Dictionary<StatType, object>()))),
                ("NewTickEmpty", () => Convert.ToHexString(GameServer.NewTick(new List<ObjectStatus>(), new Dictionary<StatType, object>()))),
            };
        }

        private static List<TileData> GoldenTiles()
        {
            return new List<TileData>
            {
                new TileData { X = -1, Y = 5, TileType = 0xFFFE },
                new TileData { X = 32767, Y = -32768, TileType = 1 },
            };
        }

        private static List<ObjectDefinition> GoldenDefinitions()
        {
            return new List<ObjectDefinition>
            {
                new ObjectDefinition
                {
                    ObjectType = 0x0300,
                    ObjectStatus = new ObjectStatus
                    {
                        Id = 99,
                        Position = new Position(3.5f, -2.25f),
                        Stats = new Dictionary<StatType, object> { { StatType.MaxHP, 770 }, { StatType.Name, "Ünï" }, { StatType.HP, 5 } },
                    },
                },
                new ObjectDefinition
                {
                    ObjectType = 0x0100,
                    ObjectStatus = new ObjectStatus
                    {
                        Id = -7,
                        Position = new Position(0f, 0f),
                        Stats = new Dictionary<StatType, object> { { StatType.GuildName, "" } },
                    },
                },
            };
        }

        private static List<ObjectDrop> GoldenDrops()
        {
            return new List<ObjectDrop> { new ObjectDrop { Id = 7, Explode = true }, new ObjectDrop { Id = 8, Explode = false } };
        }

        private static List<ObjectStatus> GoldenStatuses()
        {
            return new List<ObjectStatus>
            {
                new ObjectStatus
                {
                    Id = 11,
                    Position = new Position(7f, 8f),
                    Stats = new Dictionary<StatType, object> { { StatType.HP, 100 }, { StatType.Size, 100 } },
                },
            };
        }

        private static Dictionary<StatType, object> GoldenPlayerStats()
        {
            return new Dictionary<StatType, object> { { StatType.HP, 100 }, { StatType.Name, "p" }, { StatType.MaxHP, 770 } };
        }

        //A span over a frame's bytes, read the way Read does: the outcome is the value, or the exception kind.
        private static string Outcome(byte[] frame, Func<byte[], string> read)
        {
            try
            {
                return "ok:" + read(frame);
            }
            catch (MalformedPacketException)
            {
                return "malformed";
            }
            catch (EndOfStreamException)
            {
                return "eos";
            }
        }

        private static void CheckReader(List<string> failures)
        {
            //Targeted frames with known answers.
            Expect(failures, "ReadString", new byte[] { 0x00, 0x03, (byte)'a', (byte)'b', (byte)'c' }, f => new SpanReader(f).ReadString(), "ok:abc");
            Expect(failures, "ReadString empty", new byte[] { 0x00, 0x00 }, f => new SpanReader(f).ReadString(), "ok:");
            Expect(failures, "ReadString past end", new byte[] { 0x00, 0x05, (byte)'a', (byte)'b' }, f => new SpanReader(f).ReadString(), "malformed");
            Expect(failures, "ReadString negative", new byte[] { 0xFF, 0xFF }, f => new SpanReader(f).ReadString(), "malformed");
            Expect(failures, "ReadString prefix short", new byte[] { 0x00 }, f => new SpanReader(f).ReadString(), "eos");
            Expect(failures, "ReadUTF32", new byte[] { 0, 0, 0, 2, (byte)'h', (byte)'i' }, f => new SpanReader(f).ReadUTF32(), "ok:hi");
            Expect(failures, "ReadUTF32 negative", new byte[] { 0x80, 0, 0, 0 }, f => new SpanReader(f).ReadUTF32(), "malformed");
            Expect(failures, "ReadUTF32 past end", new byte[] { 0, 0, 0, 9, (byte)'h' }, f => new SpanReader(f).ReadUTF32(), "malformed");
            Expect(failures, "ReadBytes negative", new byte[] { 1, 2, 3 }, f => string.Join(",", new SpanReader(f).ReadBytes(-1)), "malformed");
            Expect(failures, "ReadBytes past end", new byte[] { 1, 2, 3 }, f => string.Join(",", new SpanReader(f).ReadBytes(4)), "malformed");
            Expect(failures, "ReadBytes exact", new byte[] { 1, 2, 3 }, f => string.Join(",", new SpanReader(f).ReadBytes(3)), "ok:1,2,3");
            Expect(failures, "ReadInt32 short", new byte[] { 1, 2, 3 }, f => new SpanReader(f).ReadInt32().ToString(), "eos");
            Expect(failures, "ReadSingle", new byte[] { 0x3F, 0xC0, 0, 0 }, f => new SpanReader(f).ReadSingle().ToString(), "ok:1.5");
            Expect(failures, "ReadInt16 BE", new byte[] { 0x01, 0x02 }, f => new SpanReader(f).ReadInt16().ToString(), "ok:258");

            //Random frames: every read is checked against the frame bytes (the length rules) and, when it succeeds, against
            //the stream-backed PacketReader the span reader replaces.
            var rnd = new Random(20261015);
            int reads = 0, malformed = 0, mismatches = 0;
            for (int t = 0; t < 20000; t++)
            {
                byte[] frame = new byte[rnd.Next(0, 24)];
                rnd.NextBytes(frame);
                if (frame.Length >= 2 && rnd.Next(2) == 0)
                {
                    short len = (short)rnd.Next(-3, 30);
                    frame[0] = (byte)(len >> 8);
                    frame[1] = (byte)len;
                }
                var legacy = new PacketReader(new MemoryStream(frame));
                SpanReader span = new SpanReader(frame);
                for (int k = 0; k < 8; k++)
                {
                    int op = rnd.Next(10);
                    int n = rnd.Next(-1, 6);
                    int pos = span.Position;
                    string want = ModelOutcome(frame, pos, op, n);
                    string got;
                    string value = null;
                    try
                    {
                        value = StepSpan(ref span, op, n);
                        got = "ok:";
                    }
                    catch (MalformedPacketException)
                    {
                        got = "malformed";
                    }
                    catch (EndOfStreamException)
                    {
                        got = "eos";
                    }
                    reads++;
                    if (got != want)
                    {
                        mismatches++;
                        failures.Add($"reader op {op} n {n} at {pos} on {Convert.ToHexString(frame)}: got {got}, model {want}");
                        break;
                    }
                    if (value == null)
                    {
                        malformed++;
                        break;
                    }
                    string legacyValue = StepLegacy(legacy, op, n);
                    if (legacyValue != value || legacy.BaseStream.Position != span.Position)
                    {
                        mismatches++;
                        failures.Add($"reader op {op} n {n} at {pos} on {Convert.ToHexString(frame)}: span {value} at {span.Position}, legacy {legacyValue} at {legacy.BaseStream.Position}");
                        break;
                    }
                }
            }
            Console.WriteLine($"W06 verify: reader {reads} reads, {malformed} malformed stops, {mismatches} mismatches");
        }

        //What the frame bytes say a read at pos must do: "ok:*", "eos" (ran off the end) or "malformed" (bad length).
        private static string ModelOutcome(byte[] frame, int pos, int op, int n)
        {
            int left = frame.Length - pos;
            switch (op)
            {
                case 0: return left >= 1 ? "ok:" : "eos";
                case 1: return left >= 1 ? "ok:" : "eos";
                case 2: return left >= 2 ? "ok:" : "eos";
                case 3: return left >= 2 ? "ok:" : "eos";
                case 4: return left >= 4 ? "ok:" : "eos";
                case 5: return left >= 4 ? "ok:" : "eos";
                case 6:
                    if (left < 2) return "eos";
                    int sl = (short)((frame[pos] << 8) | frame[pos + 1]);
                    return sl < 0 || sl > left - 2 ? "malformed" : "ok:";
                case 7:
                    if (left < 4) return "eos";
                    int ul = (frame[pos] << 24) | (frame[pos + 1] << 16) | (frame[pos + 2] << 8) | frame[pos + 3];
                    return ul < 0 || ul > left - 4 ? "malformed" : "ok:";
                case 8: return n < 0 || n > left ? "malformed" : "ok:";
                default: return "ok:";
            }
        }

        //Span-side step. Values are rendered the same way StepLegacy renders them.
        private static string StepSpan(ref SpanReader r, int op, int n)
        {
            switch (op)
            {
                case 0: return r.ReadByte().ToString();
                case 1: return r.ReadBoolean().ToString();
                case 2: return r.ReadInt16().ToString();
                case 3: return r.ReadUInt16().ToString();
                case 4: return r.ReadInt32().ToString();
                case 5: return BitConverter.SingleToInt32Bits(r.ReadSingle()).ToString();
                case 6: return "s:" + r.ReadString();
                case 7: return "u:" + r.ReadUTF32();
                case 8: return "b:" + string.Join(",", r.ReadBytes(n));
                default: return r.HasMore.ToString();
            }
        }

        private static string StepLegacy(PacketReader r, int op, int n)
        {
            switch (op)
            {
                case 0: return r.ReadByte().ToString();
                case 1: return r.ReadBoolean().ToString();
                case 2: return r.ReadInt16().ToString();
                case 3: return r.ReadUInt16().ToString();
                case 4: return r.ReadInt32().ToString();
                case 5: return BitConverter.SingleToInt32Bits(r.ReadSingle()).ToString();
                case 6: return "s:" + r.ReadString();
                case 7: return "u:" + r.ReadUTF32();
                case 8: return "b:" + string.Join(",", r.ReadBytes(n));
                default: return (r.BaseStream.Position < r.BaseStream.Length).ToString();
            }
        }

        private static void Expect(List<string> failures, string label, byte[] frame, Func<byte[], string> read, string want)
        {
            string got = Outcome(frame, read);
            if (got != want)
                failures.Add($"reader {label}: got {got}, want {want}");
        }

        private static void CheckWriter(List<string> failures)
        {
            //The largest string the u16 prefix allows is 65535 bytes; one byte more must throw, not wrap the prefix.
            string max = new string('a', 65535);
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write(max);
            ReadOnlySpan<byte> frame = PacketWriter.RentedBytes();
            if (frame.Length != 65537 || frame[0] != 0xFF || frame[1] != 0xFF)
                failures.Add("writer: 65535-byte string frame wrong");
            if (!ThrowsOutOfRange(() => PacketWriter.Rent().Write(max + "b")))
                failures.Add("writer: 65536-byte string did not throw");
            if (!ThrowsArgumentNull(() => PacketWriter.Rent().Write((string)null)))
                failures.Add("writer: null string did not throw ArgumentNullException");

            //A detached writer must not move the shared scratch: a span taken before it stays intact.
            PacketWriter shared = PacketWriter.Rent();
            shared.Write((byte)7);
            ReadOnlySpan<byte> kept = PacketWriter.RentedBytes();
            byte[] owned = GameServer.OwnedFailure(2, "x");
            if (kept.Length != 1 || kept[0] != 7)
                failures.Add("writer: detached build rewound the shared scratch");
            if (owned.Length != 1 + 4 + 2 + 1 || owned[0] != (byte)GameServer.PacketId.Failure)
                failures.Add("writer: OwnedFailure layout");
        }

        private static bool ThrowsOutOfRange(Action a)
        {
            try { a(); return false; }
            catch (ArgumentOutOfRangeException) { return true; }
        }

        private static bool ThrowsArgumentNull(Action a)
        {
            try { a(); return false; }
            catch (ArgumentNullException) { return true; }
        }
    }
}
