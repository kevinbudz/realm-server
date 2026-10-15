using RotMG.Common;
using RotMG.Game.Commands;
using RotMG.Game.DungeonGen;
using RotMG.Game.DungeonGen.Templates;
using RotMG.Game.Entities;
using RotMG.Game.Entities.Vendors;
using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Transitions;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RotMG.Game
{
    //--w05-verify (Program.cs): regression checks for the W05 correctness sweep and the static index.
    //Each check prints "W05 verify: ..." and the flag exits 0 only if every check passes. Run it from a throwaway
    //cwd with a generated Settings.xml (see tools/hop/server_ctl.sh); the Realm world and Database come from Manager.Init.
    //BehaviorHelpers.NearbyEntities and the static scans are compared against copies of the pre-W05 code kept here.
    internal static class W05Verify
    {
        private static bool _failed;
        private static int _nextAccountId = 920000;

        public static bool Run()
        {
            _failed = false;
            CheckDecoyLevelUpAssist();
            CheckTeleportGate();
            CheckVendorReach();
            CheckEditAccountList();
            CheckHelloLengthBounds();
            CheckReadBytesAllocation();
            CheckStaticIndex();
            CheckDungeonGenBudget();
            CheckLegendaryBound();
            CheckGuildCommitGate();
            CheckSequentialTickIsolation();
            return !_failed;
        }

        private static void Check(bool ok, string what)
        {
            if (ok)
                Program.Print(PrintType.Info, "W05 verify: " + what);
            else
            {
                _failed = true;
                Program.Print(PrintType.Error, "W05 verify FAIL: " + what);
            }
        }

        private static void Info(string what)
        {
            Program.Print(PrintType.Info, "W05 verify: " + what);
        }

        //Same construction as Manager's P19 verify: a Handshaked stub client with an account that skips the DB reload.
        private static Client MakeClient(string guildName = null)
        {
            int accountId = _nextAccountId++;
            Client client = new Client(new SendState(), new ReceiveState());
            client.State = ProtocolState.Handshaked;
            client.Active = true;
            client.Account = new AccountModel(accountId, skipReload: true)
            {
                Name = "w05-" + accountId,
                GuildName = guildName,
                LockedIds = new List<int>(),
                IgnoredIds = new List<int>(),
                AliveChars = new List<int>(),
                DeadChars = new List<int>(),
                OwnedSkins = new List<int>(),
                Stats = new StatsInfo { ClassStats = Database.CreateClassStats() }
            };
            PlayerDesc pdesc = Resources.Type2Player.Values.First();
            CharacterModel ch = new CharacterModel(0, 0)
            {
                ClassType = pdesc.Type,
                Level = 1,
                HP = 100,
                MP = 100,
                Stats = new int[8],
                Inventory = (int[])pdesc.Equipment.Clone(),
                ItemDatas = (int[])pdesc.ItemDatas.Clone(),
                FameStats = new FameStatsInfo()
            };
            for (int i = 0; i < 8; i++)
                ch.Stats[i] = pdesc.Stats[i].StartingValue;
            client.Character = ch;
            client.State = ProtocolState.Connected;
            return client;
        }

        //Tries the offsets around `center` in turn, so a test does not depend on which tiles are open.
        private static bool TryAdd(World world, Entity en, Position center, out Position placed)
        {
            float[] offsets = { 0f, 1f, -1f, 2f, -2f, 3f, -3f };
            foreach (float dy in offsets)
                foreach (float dx in offsets)
                {
                    placed = new Position(center.X + dx, center.Y + dy);
                    if (world.AddEntity(en, placed) != -1)
                        return true;
                }
            placed = center;
            return false;
        }

        private static Player JoinNear(World world, Client client, Position center, out Position placed)
        {
            Player player = new Player(client);
            client.Player = player;
            if (!TryAdd(world, player, center, out placed))
                return null;
            return player;
        }

        private static Position SpawnOf(World world)
        {
            return world.GetRegion(Region.Spawn).ToPosition();
        }

        private static byte[] Be16(ushort v)
        {
            byte[] b = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(b, v);
            return b;
        }

        private static byte[] Be32(int v)
        {
            byte[] b = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(b, v);
            return b;
        }

        private static byte[] Str(string s)
        {
            byte[] u = Encoding.UTF8.GetBytes(s);
            return Be16((ushort)u.Length).Concat(u).ToArray();
        }

        private static PacketReader ReaderOf(params byte[][] parts)
        {
            return new PacketReader(new MemoryStream(parts.SelectMany(p => p).ToArray()));
        }

        //1. Enemy.Death, level-up assist: PlayerChunks holds Decoys, and the unchecked (p as Player) cast threw.
        private static void CheckDecoyLevelUpAssist()
        {
            World realm = Manager.GetWorld(Manager.RealmId);
            Position spawn = SpawnOf(realm);

            Client killerClient = MakeClient();
            Client otherClient = MakeClient();
            Player killer = JoinNear(realm, killerClient, spawn, out Position killerAt);
            Player other = killer == null ? null : JoinNear(realm, otherClient, killerAt, out _);
            if (killer == null || other == null)
            {
                Check(false, "decoy assist: could not place two players");
                return;
            }

            Enemy enemy = Entity.Resolve(0x0603) as Enemy;
            bool enemyPlaced = enemy != null && TryAdd(realm, enemy, killerAt, out _);
            Decoy decoy = new Decoy(killer, 0f, 10000);
            bool decoyPlaced = TryAdd(realm, decoy, killerAt, out Position decoyAt);
            Check(enemyPlaced && decoyPlaced, "decoy assist: enemy and decoy placed near the killer");
            if (!enemyPlaced || !decoyPlaced)
                return;

            //Level 1 with 49 EXP: the first GainEXP of any kill levels the killer up (threshold 50).
            killer.EXP = 49;
            killer.NextLevelEXP = Player.GetNextLevelEXP(1);
            int before = other.FameStats.LevelUpAssists;
            bool threw = false;
            string error = null;
            try
            {
                enemy.Death(killer);
            }
            catch (Exception e)
            {
                threw = true;
                error = e.GetType().Name + ": " + e.Message;
            }
            Check(!threw, "decoy assist: Enemy.Death with a decoy in PlayerChunks does not throw" + (threw ? " (" + error + ")" : ""));
            Check(other.FameStats.LevelUpAssists == before + 1, $"decoy assist: other player +1 level-up assist (now {other.FameStats.LevelUpAssists}, was {before})");
            Check(killer.FameStats.LevelUpAssists == 0, "decoy assist: the leveling player gets no assist for itself");
            Info($"decoy at {decoyAt} is a Decoy: {decoy.GetType().Name}, in PlayerChunks: {realm.PlayerChunks.HitTest(decoyAt, 1f).Contains(decoy)}");
        }

        //2. Teleport packet ignored World.AllowTeleport. Realm allows teleport, so flip the flag on the live instance and restore it.
        private static void CheckTeleportGate()
        {
            World realm = Manager.GetWorld(Manager.RealmId);
            Position spawn = SpawnOf(realm);
            Client aClient = MakeClient();
            Client bClient = MakeClient();
            Player a = JoinNear(realm, aClient, spawn, out Position aAt);
            Player b = a == null ? null : JoinNear(realm, bClient, new Position(aAt.X + 6, aAt.Y), out _);
            if (a == null || b == null)
            {
                Check(false, "teleport gate: could not place two players");
                return;
            }

            SeeTile(a, b.Position, realm);
            bool saved = realm.AllowTeleport;
            try
            {
                realm.AllowTeleport = false;
                Position beforePos = a.Position;
                int teleports = a.FameStats.Teleports;
                GameServer.Teleport(aClient, ReaderOf(Be32(b.Id)));
                Check(a.Position.Distance(beforePos) == 0f && a.FameStats.Teleports == teleports,
                    "teleport gate: AllowTeleport=false refuses the Teleport packet (no move, no count)");

                //The /tp command (Rank.Player) reaches the same gate.
                if (CommandTable.TryFind("/tp", out CommandInfo tp))
                {
                    Position beforeTp = a.Position;
                    tp.Handler(a, new CommandCall("/tp", new[] { b.Name }, b.Name));
                    Check(a.Position.Distance(beforeTp) == 0f, "teleport gate: /tp refuses when AllowTeleport=false");
                }
                else
                    Check(false, "teleport gate: /tp is registered");

                realm.AllowTeleport = true;
                GameServer.Teleport(aClient, ReaderOf(Be32(b.Id)));
                Check(a.Position.Distance(b.Position) < 0.01f && a.FameStats.Teleports == teleports + 1,
                    "teleport gate: AllowTeleport=true still teleports to the target");
            }
            finally
            {
                realm.AllowTeleport = saved;
            }
        }

        //Player.Teleport refuses a tile the player has not been sent; mark it seen the way a sight update does.
        private static void SeeTile(Player player, Position at, World world)
        {
            int x = (int)at.X, y = (int)at.Y;
            Tile? tile = world.GetTileF(x, y);
            System.Reflection.MethodInfo set = typeof(Player).GetMethod("SetSeenTileUpdate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            set.Invoke(player, new object[] { x, y, tile.Value.UpdateCount });
        }

        //3. Vendor Buy had no reach check. IsInBuyRange uses the portal reach (1.5 tiles); GameServer.Buy refuses beyond it.
        private static void CheckVendorReach()
        {
            ushort chestType = Resources.Id2Object["Vault Chest"].Type;
            ClosedVaultChest vendor = new ClosedVaultChest(chestType);
            vendor.Position = new Position(10f, 10f);
            Player stub = new Player(MakeClient());
            stub.Position = new Position(11f, 10f);
            bool near = vendor.IsInBuyRange(stub);
            stub.Position = new Position(11.5f, 10f);
            bool edge = vendor.IsInBuyRange(stub);
            stub.Position = new Position(11.6f, 10f);
            bool far = vendor.IsInBuyRange(stub);
            Check(near && edge && !far, $"vendor reach: 1.0 and 1.5 tiles in range, 1.6 out (got {near}/{edge}/{far})");

            //End to end through the handler: a far Buy must not spend fame or add a chest.
            World realm = Manager.GetWorld(Manager.RealmId);
            Position spawn = SpawnOf(realm);
            Client client = MakeClient();
            Player player = JoinNear(realm, client, spawn, out Position at);
            if (player == null || !TryAdd(realm, vendor, new Position(at.X + 8, at.Y), out Position vendorAt))
            {
                Check(false, "vendor reach: could not place a player and a vendor");
                return;
            }
            client.Account.Stats.Fame = 100000;
            int fameBefore = client.Account.Stats.Fame;
            int chestsBefore = realm.Statics.Values.Count(s => s is ClosedVaultChest);
            GameServer.Buy(client, ReaderOf(Be32(vendor.Id)));
            Check(client.Account.Stats.Fame == fameBefore && realm.Statics.Values.Count(s => s is ClosedVaultChest) == chestsBefore,
                $"vendor reach: Buy from {vendorAt.Distance(at):F1} tiles away spends nothing (fame {fameBefore} -> {client.Account.Stats.Fame})");
            realm.RemoveEntity(vendor);
        }

        //4. EditAccountList: no dedupe, so a double add survived a single remove; no cap.
        private static void CheckEditAccountList()
        {
            World realm = Manager.GetWorld(Manager.RealmId);
            Position spawn = SpawnOf(realm);
            Client selfClient = MakeClient();
            Client targetClient = MakeClient();
            Player self = JoinNear(realm, selfClient, spawn, out Position at);
            Player target = self == null ? null : JoinNear(realm, targetClient, at, out _);
            if (self == null || target == null)
            {
                Check(false, "account list: could not place two players");
                return;
            }
            List<int> ignored = selfClient.Account.IgnoredIds;

            void Send(int listId, bool add, int objectId)
            {
                byte[] body = Be32(listId).Concat(new byte[] { (byte)(add ? 1 : 0) }).Concat(Be32(objectId)).ToArray();
                GameServer.EditAccountList(selfClient, new PacketReader(new MemoryStream(body)));
            }

            Send(1, true, target.Id);
            Send(1, true, target.Id);
            Check(ignored.Count == 1, $"account list: adding the same account twice keeps one entry (count {ignored.Count})");
            Send(1, false, target.Id);
            Check(ignored.Count == 0, $"account list: one remove clears a double add (count {ignored.Count})");

            for (int i = 0; i < GameServer.MaxAccountListEntries; i++)
                ignored.Add(-(i + 2));
            Send(1, true, target.Id);
            Check(ignored.Count == GameServer.MaxAccountListEntries && !ignored.Contains(target.AccountId),
                $"account list: full list (cap {GameServer.MaxAccountListEntries}) refuses a new id (count {ignored.Count})");

            ignored.Clear();
            ignored.AddRange(new[] { target.AccountId, target.AccountId, 77 });
            Send(1, false, target.Id);
            Check(ignored.Count == 1 && ignored[0] == 77,
                "account list: remove drops every copy (legacy duplicates cleaned)");
            ignored.Clear();
        }

        //5. Hello reads an Int32 length and ReadBytes it before auth. Negative, over the frame cap, and past the packet
        //end must return without throwing or allocating.
        private static void CheckHelloLengthBounds()
        {
            Client client = MakeClient();
            ProtocolState state = client.State;
            int[] lengths = { -1, int.MinValue, GameServer.BufferSize + 1, 1000000000, 5 };
            foreach (int len in lengths)
            {
                PacketReader rdr = ReaderOf(Str("1.0.0"), Be32(0), Str("user"), Str("pass"), Be32(len));
                long before = GC.GetAllocatedBytesForCurrentThread();
                bool threw = false;
                string error = null;
                try
                {
                    GameServer.Hello(client, rdr);
                }
                catch (Exception e)
                {
                    threw = true;
                    error = e.GetType().Name;
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(!threw && client.State == state && allocated < 1024 * 1024,
                    $"hello bounds: mapJson length {len} rejected (threw={threw}{(error != null ? " " + error : "")}, allocated {allocated} bytes)");
            }
        }

        //Evidence for the Hello bound: BinaryReader.ReadBytes allocates the requested count before it reads.
        private static void CheckReadBytesAllocation()
        {
            const int Request = 16 * 1024 * 1024;
            PacketReader rdr = new PacketReader(new MemoryStream(new byte[4]));
            long before = GC.GetAllocatedBytesForCurrentThread();
            rdr.ReadBytes(Request);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Info($"ReadBytes({Request}) on a 4-byte stream allocated {allocated} bytes (the unchecked Hello length did this before the bound)");
        }

        //6. Static index vs the pre-W05 scans. Compares every query against copies of the old code.
        private static void CheckStaticIndex()
        {
            World realm = Manager.GetWorld(Manager.RealmId);
            List<StaticObject> statics = realm.Statics.Values.ToList();
            Check(statics.Count > 1000, $"static index: realm has {statics.Count} statics");
            Entity dummy = Entity.Resolve(0x0603);
            if (dummy == null || statics.Count == 0)
            {
                Check(false, "static index: no host entity or no statics");
                return;
            }
            dummy.Parent = realm;
            Random rnd = new Random(1234);

            CheckOrderKeys(realm, "as loaded");
            Compare(realm, statics, dummy, rnd, 3000, true, "phase 1 (as loaded)");

            //Timing: the old full scan vs the index for range 15 at random hosts, statics only.
            Position[] samplePoints = Enumerable.Range(0, 300)
                .Select(_ => new Position(rnd.Next(realm.Width), rnd.Next(realm.Height))).ToArray();
            Stopwatch sw = Stopwatch.StartNew();
            long oldHits = 0;
            foreach (Position at in samplePoints)
            {
                dummy.Position = at;
                oldHits += OldStaticsNear(realm, dummy, 15f).Count;
            }
            double oldMs = sw.Elapsed.TotalMilliseconds;
            sw.Restart();
            long newHits = 0;
            List<StaticObject> buf = new List<StaticObject>();
            foreach (Position at in samplePoints)
            {
                buf.Clear();
                realm.StaticGrid.Collect(at, 15f, buf, inDictionaryOrder: true);
                newHits += buf.Count;
            }
            double newMs = sw.Elapsed.TotalMilliseconds;
            Check(oldHits == newHits, $"static index: timing sample hit counts agree ({oldHits} vs {newHits})");
            Info($"static index timing, 300 range-15 queries over {statics.Count} statics: old scan {oldMs:F1} ms, index {newMs:F2} ms");

            //Mutations: removals (slot reuse breaks Dictionary order), moves inside the map, and moves off the grid
            //(loose list). Off-grid moves are legal for MoveEntity; NaN is refused by it.
            int removed = 0;
            foreach (StaticObject st in statics.OrderBy(_ => rnd.Next()).Take(400).ToList())
                if (realm.Statics.ContainsKey(st.Id))
                {
                    realm.RemoveEntity(st);
                    removed++;
                }
            int moved = 0;
            foreach (StaticObject st in realm.Statics.Values.OrderBy(_ => rnd.Next()).Take(300).ToList())
            {
                realm.MoveEntity(st, new Position(rnd.Next(realm.Width), rnd.Next(realm.Height)));
                moved++;
            }
            int offGrid = 0;
            foreach (StaticObject st in realm.Statics.Values.OrderBy(_ => rnd.Next()).Take(30).ToList())
            {
                Position target = offGrid % 3 == 0 ? new Position(-7.5f, -3f)
                    : offGrid % 3 == 1 ? new Position(realm.Width + 5f, 4f)
                    : new Position(-0.5f, realm.Height + 9.25f);
                realm.MoveEntity(st, target);
                offGrid++;
            }
            Info($"static index: mutated {removed} removed, {moved} moved, {offGrid} moved off-grid; index holds {realm.StaticGrid.Count}, Statics holds {realm.Statics.Count}");
            Check(realm.StaticGrid.Count == realm.Statics.Count, "static index: index membership equals Statics after add/remove/move");
            List<StaticObject> current = realm.Statics.Values.ToList();
            CheckOrderKeys(realm, "after mutations");
            Compare(realm, current, dummy, rnd, 3000, true, "phase 2 (after removals, moves, off-grid moves)");

            //Behavior statics (World.Tick used to scan all of Statics for them), in the same order as that scan.
            List<int> wantBehavior = realm.Statics.Values.Where(s => s.Behavior != null).Select(s => s.Id).ToList();
            List<int> gotBehavior = realm.BehaviorStatics.Select(s => s.Id).ToList();
            Check(wantBehavior.SequenceEqual(gotBehavior), $"static index: behavior statics match in order ({gotBehavior.Count} tracked)");
        }

        //The order keys must reproduce Statics enumeration order exactly (this is what keeps results order-identical).
        private static void CheckOrderKeys(World realm, string phase)
        {
            List<StaticObject> enumerated = realm.Statics.Values.ToList();
            bool sameOrder = true;
            for (int i = 1; i < enumerated.Count; i++)
                if (enumerated[i - 1].StaticOrder >= enumerated[i].StaticOrder)
                {
                    sameOrder = false;
                    break;
                }
            Check(sameOrder, $"static index order keys {phase}: strictly increasing in Statics enumeration order ({enumerated.Count} statics)");
            Info($"static index order keys {phase}: Id order matches enumeration: {enumerated.Select(e => e.Id).SequenceEqual(enumerated.OrderBy(e => e.Id).Select(e => e.Id))}");
        }

        private static List<Entity> OldStaticsNear(World world, Entity host, float range)
        {
            //Copy of the pre-W05 BehaviorHelpers.NearbyEntities static loop, in Statics dictionary order.
            List<Entity> result = new List<Entity>();
            float r2 = range * range;
            foreach (Entity en in world.Statics.Values)
                if (!en.Equals(host) && host.Position.DistanceSquared(en.Position) < r2)
                    result.Add(en);
            return result;
        }

        private static List<Entity> OldNearby(Entity host, float range)
        {
            //Copy of the pre-W05 BehaviorHelpers.NearbyEntities, in its original order.
            List<Entity> result = new List<Entity>();
            foreach (Entity en in host.Parent.EntityChunks.HitTest(host.Position, range))
                if (!en.Equals(host))
                    result.Add(en);
            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, range))
                if (!en.Equals(host))
                    result.Add(en);
            result.AddRange(OldStaticsNear(host.Parent, host, range));
            return result;
        }

        private static int OldCount(Entity host, float radius, ushort type)
        {
            int count = 0;
            foreach (Entity en in host.Parent.EntityChunks.HitTest(host.Position, radius))
                if (en.Type == type)
                    count++;
            float r2 = radius * radius;
            foreach (Entity en in host.Parent.Statics.Values)
                if (en.Type == type && host.Position.DistanceSquared(en.Position) < r2)
                    count++;
            return count;
        }

        private static bool OldGroupNotExist(Entity host, float distance, string group)
        {
            foreach (Entity en in host.Parent.EntityChunks.HitTest(host.Position, distance))
                if (en.Desc.Group == group)
                    return false;
            float r2 = distance * distance;
            foreach (Entity en in host.Parent.Statics.Values)
                if (en.Desc.Group == group && host.Position.DistanceSquared(en.Position) < r2)
                    return false;
            return true;
        }

        private static void Compare(World realm, List<StaticObject> pool, Entity dummy, Random rnd, int queries, bool orderMustMatch, string phase)
        {
            float[] fixedRanges = { 0f, 0.5f, 1f, 3f, 8f, 15f, 16f, 16.5f, 25f, 40f, -5f, float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            int setMismatch = 0, orderMismatch = 0, countMismatch = 0, transitionMismatch = 0, nonEmpty = 0;
            int hitsTotal = 0;
            string firstBad = null;
            for (int q = 0; q < queries; q++)
            {
                Entity host;
                if (rnd.Next(2) == 0 && pool.Count > 0)
                    host = pool[rnd.Next(pool.Count)];
                else
                {
                    host = dummy;
                    //Mostly around the map, sometimes well outside it.
                    float x = (float)(rnd.NextDouble() * (realm.Width + 60) - 30);
                    float y = (float)(rnd.NextDouble() * (realm.Height + 60) - 30);
                    dummy.Position = new Position(x, y);
                }
                float range = rnd.Next(4) == 0
                    ? fixedRanges[rnd.Next(fixedRanges.Length)]
                    : (float)(rnd.NextDouble() * 40);

                List<Entity> oldAll = OldNearby(host, range);
                List<Entity> newAll = BehaviorHelpers.NearbyEntities(host, range).ToList();
                hitsTotal += newAll.Count;
                if (newAll.Count > 0)
                    nonEmpty++;
                bool sameSet = oldAll.Select(e => e.Id).OrderBy(id => id).SequenceEqual(newAll.Select(e => e.Id).OrderBy(id => id));
                if (!sameSet)
                {
                    setMismatch++;
                    firstBad ??= $"range {range} host {host.Id} at {host.Position}";
                }
                if (orderMustMatch && !oldAll.Select(e => e.Id).SequenceEqual(newAll.Select(e => e.Id)))
                    orderMismatch++;

                if (!float.IsNaN(range) && range >= 0 && !float.IsInfinity(range))
                {
                    ushort type = pool.Count > 0 ? pool[rnd.Next(pool.Count)].Type : dummy.Type;
                    if (OldCount(host, range, type) != BehaviorHelpers.CountEntities(host, range, type))
                        countMismatch++;
                }
                string group = pool.Count > 0 ? pool[rnd.Next(pool.Count)].Desc.Group : null;
                if (!string.IsNullOrWhiteSpace(group) && !float.IsNaN(range) && !float.IsInfinity(range) && range >= 0)
                {
                    bool expected = OldGroupNotExist(host, range, group);
                    bool actual = new GroupNotExistTransition(range, "x", group).Tick(host);
                    if (expected != actual)
                        transitionMismatch++;
                }
            }
            Check(setMismatch == 0, $"static index {phase}: NearbyEntities result sets equal over {queries} queries ({setMismatch} differ{(firstBad != null ? ", first " + firstBad : "")}; {nonEmpty} non-empty, {hitsTotal} hits)");
            if (orderMustMatch)
                Check(orderMismatch == 0, $"static index {phase}: NearbyEntities sequence (chunks, then statics) equals the old order ({orderMismatch} differ)");
            Check(countMismatch == 0, $"static index {phase}: CountEntities equal ({countMismatch} differ)");
            Check(transitionMismatch == 0, $"static index {phase}: GroupNotExistTransition equal ({transitionMismatch} differ)");
            dummy.Position = new Position(0, 0);
        }

        //7. DungeonGen retry caps. Normal generations stay far under the budget, a lowered budget throws, and a failed
        //build retries MaxBuildAttempts seeds, then fails without adding a world.
        private static void CheckDungeonGenBudget()
        {
            string[] templates = { "Abyss of Demons", "Mad Lab", "Pirate Cave" };
            int maxWork = 0, failures = 0, seeds = 0;
            long totalMs = 0;
            foreach (string name in templates)
            {
                DungeonTemplate template = DungeonTemplates.GetTemplate(name);
                if (template == null)
                {
                    Check(false, $"dungeon budget: template {name} exists");
                    return;
                }
                for (int seed = 1; seed <= 1000; seed++)
                {
                    seeds++;
                    Generator gen = new Generator(seed, DungeonTemplates.GetTemplate(name));
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        gen.Generate();
                        gen.ExportGraph();
                    }
                    catch (DungeonGenerationException)
                    {
                        failures++;
                    }
                    totalMs += sw.ElapsedMilliseconds;
                    maxWork = Math.Max(maxWork, gen.WorkUsed);
                }
            }
            Info($"dungeon budget: {seeds} generations, max work {maxWork} of {Generator.MaxWork}, {totalMs} ms total, {failures} over budget");
            Check(failures == 0 && maxWork * 20 < Generator.MaxWork, "dungeon budget: normal seeds finish with headroom (max work under a twentieth of the cap)");

            int savedMax = Generator.MaxWork;
            try
            {
                Generator.MaxWork = 50;
                bool threw = false;
                try
                {
                    new Generator(1, DungeonTemplates.GetTemplate("Mad Lab")).Generate();
                }
                catch (DungeonGenerationException)
                {
                    threw = true;
                }
                Check(threw, "dungeon budget: exhausted budget throws DungeonGenerationException");

                if (!Resources.Worlds.TryGetValue("Mad Lab", out WorldDesc desc))
                {
                    Check(false, "dungeon budget: Mad Lab world desc exists");
                    return;
                }
                int worldsBefore;
                lock (Manager.SyncRoot)
                    worldsBefore = Manager.Worlds.Count;
                bool buildThrew = false;
                try
                {
                    Manager.CreateDungeonWorld(desc);
                }
                catch (DungeonGenerationException)
                {
                    buildThrew = true;
                }
                int worldsAfter;
                lock (Manager.SyncRoot)
                    worldsAfter = Manager.Worlds.Count;
                Check(buildThrew && worldsAfter == worldsBefore,
                    $"dungeon budget: CreateDungeonWorld fails after {Dungeons.DungeonWorld.MaxBuildAttempts} seeds and adds no world ({worldsBefore} -> {worldsAfter})");
            }
            finally
            {
                Generator.MaxWork = savedMax;
            }
        }

        //9. /legendary rolled until T7 with no bound: a slot type that cannot roll one spun forever.
        private static void CheckLegendaryBound()
        {
            if (!CommandTable.TryFind("/legendary", out CommandInfo legendary))
            {
                Check(false, "legendary: /legendary is registered");
                return;
            }
            World realm = Manager.GetWorld(Manager.RealmId);
            Client client = MakeClient();
            Player p = JoinNear(realm, client, SpawnOf(realm), out _);
            if (p == null)
            {
                Check(false, "legendary: could not place a player");
                return;
            }

            //Non-modifiable slot types always roll "no modifiers" (Roll returns before the rank loop), so T7 never comes.
            ushort never = 0;
            bool foundNever = false;
            foreach (KeyValuePair<ushort, ItemDesc> kv in Resources.Type2Item)
            {
                bool anyT7 = false;
                for (int i = 0; i < 40 && !anyT7; i++)
                    anyT7 = kv.Value.Roll().Item1;
                if (!anyT7)
                {
                    never = kv.Key;
                    foundNever = true;
                    break;
                }
            }
            if (!foundNever)
            {
                Check(false, "legendary: found an item type with no modifiers");
                return;
            }
            p.Inventory[0] = never;
            p.ItemDatas[0] = -1;
            Stopwatch sw = Stopwatch.StartNew();
            bool threw = false;
            try
            {
                legendary.Handler(p, new CommandCall("/legendary", new[] { "0" }, "0"));
            }
            catch (Exception)
            {
                threw = true;
            }
            Check(!threw && sw.ElapsedMilliseconds < 5000 && p.ItemDatas[0] == -1,
                $"legendary: an item that cannot roll T7 returns and stays unchanged ({sw.ElapsedMilliseconds} ms, threw={threw})");

            //Positive case: an item that does roll T7 gets the bit.
            ushort can = 0;
            bool foundCan = false;
            foreach (KeyValuePair<ushort, ItemDesc> kv in Resources.Type2Item)
            {
                bool anyT7 = false;
                for (int i = 0; i < 10000 && !anyT7; i++)
                    anyT7 = (kv.Value.Roll().Item2 & ItemData.T7) != 0;
                if (anyT7)
                {
                    can = kv.Key;
                    foundCan = true;
                    break;
                }
            }
            if (!foundCan)
            {
                Check(false, "legendary: found an item type that rolls T7");
                return;
            }
            p.Inventory[0] = can;
            p.ItemDatas[0] = -1;
            legendary.Handler(p, new CommandCall("/legendary", new[] { "0" }, "0"));
            Check((p.ItemDatas[0] & (int)ItemData.T7) != 0, "legendary: a rollable item gets a T7 roll");
        }

        //10. /gkick on yourself is /leave. The guild stays until the commit (W03), then clears.
        private static void CheckGuildCommitGate()
        {
            if (!CommandTable.TryFind("/gkick", out CommandInfo gkick))
            {
                Check(false, "guild commit: /gkick is registered");
                return;
            }
            World realm = Manager.GetWorld(Manager.RealmId);
            Client client = MakeClient();
            Player p = JoinNear(realm, client, SpawnOf(realm), out _);
            if (p == null)
            {
                Check(false, "guild commit: could not place a player");
                return;
            }
            Random rnd = new Random(77);
            string guild = "W05 " + new string(Enumerable.Range(0, 6).Select(_ => (char)('a' + rnd.Next(26))).ToArray());
            bool created = false;
            Database.GuildResult made = Database.CreateGuild(guild, client.Account, committed => created = committed);
            Check(made == Database.GuildResult.OK, $"guild commit: CreateGuild accepted ({made})");
            if (made != Database.GuildResult.OK)
                return;
            PumpUntil(() => created, 10000);
            Check(created && client.Account.GuildName == guild, "guild commit: guild created and founder in it");
            p.GuildName = guild;

            gkick.Handler(p, new CommandCall("/gkick", new[] { p.Name }, p.Name));
            Check(p.GuildName == guild, "guild commit: /gkick on yourself keeps the guild name until the commit");
            PumpUntil(() => p.GuildName == null, 10000);
            Check(p.GuildName == null && client.Account.GuildName == null,
                "guild commit: the guild is cleared after the commit (player and account)");
            bool statusThrew = false;
            try
            {
                p.GetObjectStatus(false).Write(PacketWriter.Rent());
            }
            catch (Exception)
            {
                statusThrew = true;
            }
            Check(!statusThrew, "guild commit: a player with a cleared GuildName still writes its status (no NRE in the tick)");
        }

        //Runs the main-thread queue (commit outcomes arrive through Manager.RunOnMainThread) until the condition holds.
        private static void PumpUntil(Func<bool> done, int timeoutMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (!done() && sw.ElapsedMilliseconds < timeoutMs)
            {
                Manager.Tick();
                System.Threading.Thread.Sleep(5);
            }
        }

        //8. Tick isolation: a world whose Tick throws must not stop the other worlds' ticks (sequential branch).
        private static void CheckSequentialTickIsolation()
        {
            if (!Resources.Worlds.TryGetValue("Spider Den", out WorldDesc denDesc) || !Dungeons.DungeonWorld.IsSupported(denDesc))
            {
                Check(false, "tick isolation: Spider Den desc exists");
                return;
            }
            World faulty = Manager.CreateDungeonWorld(denDesc);
            World healthy = Manager.CreateDungeonWorld(denDesc);
            Client client = MakeClient();
            Player victim = JoinNear(faulty, client, SpawnOf(faulty), out _);
            if (victim == null)
            {
                Check(false, "tick isolation: could not place a player in the faulty world");
                return;
            }
            //A Behavior enemy next to a player whose CurrentStates is null: TickStates dereferences it.
            Enemy broken = Entity.Resolve(0x0603) as Enemy;
            if (broken == null || !TryAdd(faulty, broken, victim.Position, out _) || broken.Behavior == null)
            {
                Check(false, "tick isolation: placed a behavior enemy in the faulty world");
                return;
            }
            broken.CurrentStates = null;
            bool throwsDirectly = false;
            try
            {
                faulty.Tick();
            }
            catch (Exception)
            {
                throwsDirectly = true;
            }
            Check(throwsDirectly, "tick isolation: fault injection makes World.Tick throw");
            if (!throwsDirectly)
                return;

            bool escaped = false;
            try
            {
                Manager.TickWorldsSequential(new List<World> { faulty, healthy });
            }
            catch (Exception)
            {
                escaped = true;
            }
            Check(!escaped, "tick isolation: a throwing world does not escape TickWorldsSequential");
        }
    }
}
