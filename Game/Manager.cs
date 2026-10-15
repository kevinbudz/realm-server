using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Game.Logic;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RotMG.Game
{
    public static class Manager
    {
        public const int NexusId = -1;
        public const int RealmId = -2;
        public const int GuildId = -3;
        public const int EditorId = -4;
        public const int VaultId = -5;
        public const int TutorialId = -6;

        public static int NextWorldId;
        public static int NextClientId;
        //All realm instance ids: RealmId (primary) first, then generated
        //ones. Portals, overseers, and reset/quake lifecycles are per id.
        public static readonly List<int> RealmIds = new List<int>();
        public static Dictionary<int, int> AccountIdToClientId;
        public static Dictionary<int, Client> Clients;
        //Live Account/Character kept across Reconnect so Hello/Load do not
        //round-trip SQLite (the async writer can still be committing).
        public const int SessionHandoffTtlMs = 15000;
        public const int DungeonEmptyGraceMs = 60000;
        private static Dictionary<int, SessionHandoff> SessionHandoffs;
        private static Dictionary<int, PendingTransfer> PendingTransfers;
        public static Dictionary<int, World> Worlds;
        public static Dictionary<int, World> VaultWorlds;
        public static Dictionary<string, World> GuildHallWorlds;
        public static Dictionary<int, string> PortalDungeons = new Dictionary<int, string>();
        public static SortedDictionary<int, Queue<Action>> Timers;
        private static readonly List<Client> ClientSnapshot = new List<Client>();
        private static readonly List<World> WorldSnapshot = new List<World>();
        //Main-thread work queue: worker-built worlds are published through
        //here so Worlds dict mutation stays single-threaded. Drained in Tick.
        private static readonly ConcurrentQueue<Action> MainThreadQueue = new ConcurrentQueue<Action>();
        //World-tick fan-out: one worker per world, leaving a core for the
        //tick, IO and writer threads. Worlds are independent by construction
        //(see the Tick comment); shared registries go through SyncRoot.
        private static readonly ParallelOptions WorldParallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
        };
        private static readonly HashSet<int> _realmResetsInFlight = new HashSet<int>();
        private static readonly HashSet<int> _castleBuildsInFlight = new HashSet<int>();

        public static void RunOnMainThread(Action action)
        {
            if (action != null)
                MainThreadQueue.Enqueue(action);
        }
        public static BehaviorDb Behaviors;
        public static Stopwatch TickWatch;
        public static int TotalTicks;
        public static int TotalTime;
        public static int TotalTimeUnsynced;
        public static int TickDelta; //work duration of the last tick (diagnostic; the gate reads TickDueAt)
        public static int LastTickTime; //start of the last tick on TickWatch
        //W04 test hooks, both off unless the env var is set (read once). REALM_TICK_LOAD_MS=N sleeps N ms
        //at the end of each tick's work (synthetic load). REALM_TICKTRACE=1 prints one "[tick]" line per tick
        //(start, start-to-start period, work).
        private static readonly int SyntheticTickLoadMs = ReadTickEnvInt("REALM_TICK_LOAD_MS");
        internal static readonly bool TickTraceEnabled = Environment.GetEnvironmentVariable("REALM_TICKTRACE") == "1";
        private static double _tracePrevStartMs = -1;
        private static int _traceGen2;
        //Tick schedule (W04): the next tick deadline on TickWatch, -1 until the first tick. A tick that
        //starts more than MaxTickLagTicks periods late resyncs instead of replaying the backlog.
        public const int MaxTickLagTicks = 5;
        private const int TickWarnIntervalMs = 1000;
        private static long TickDueAt = -1;
        private static long _lastTickWarnMs = -TickWarnIntervalMs;

        private static int ReadTickEnvInt(string name)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(name), out int value) && value > 0 && value <= 1000 ? value : 0;
        }

        public static void Init()
        {
            Player.InitSightCircle();
            Player.InitSightRays();

            TickWatch = Stopwatch.StartNew();
            AccountIdToClientId = new Dictionary<int, int>();
            Clients = new Dictionary<int, Client>();
            SessionHandoffs = new Dictionary<int, SessionHandoff>();
            PendingTransfers = new Dictionary<int, PendingTransfer>();
            Worlds = new Dictionary<int, World>();
            VaultWorlds = new Dictionary<int, World>();
            GuildHallWorlds = new Dictionary<string, World>(StringComparer.OrdinalIgnoreCase);
            Entities.Vendors.MerchantLists.ResolveItems();
            Timers = new SortedDictionary<int, Queue<Action>>();

            Behaviors = new BehaviorDb();

            AddWorld(Resources.Worlds["Nexus"], NexusId);
            RealmIds.Add(RealmId);
            AddWorld(Resources.Worlds["Realm"], RealmId);
            for (int i = 1; i < Settings.RealmInstances; i++)
                RealmIds.Add(AddWorld(CreateWorld(Resources.Worlds["Realm"])));
            AddWorld(Resources.Worlds["Vault"], VaultId);
            //Safe solo instance for /tutorial: Nexus tiles served under the
            //"Tutorial" name so the client starts its scripted overlay.
            //(Id -6 is the first free negative id; -1..-5 are taken.)
            AddWorld(Resources.Worlds["Tutorial"], TutorialId);

            //Realm portals live on the map's Realm_Portals region (one per
            //tracked realm, so extra realms each get their own portal). The
            //map-baked Vault and Guild Hall portals resolve dynamically, so
            //no spawn-adjacent placeholder portals are placed here.
            NexusWorld nexus = (NexusWorld)Worlds[NexusId];
            foreach (int realmId in RealmIds)
                nexus.Monitor.AddPortal(realmId);
        }

        public static World CreateWorld(WorldDesc desc, int mapIndex = -1)
        {
            IGameMap map = mapIndex < 0
                ? desc.Maps[MathUtils.Next(desc.Maps.Length)]
                : desc.Maps[mapIndex % desc.Maps.Length];
            switch (desc.Id)
            {
                case "Nexus": return new NexusWorld(map, desc);
                case "Realm": return new RealmWorld(map, desc);
                case "Vault": return new VaultWorld(map, desc);
                case "GuildHall": return new GuildHallWorld(map, desc);
                case "Castle": return new CastleWorld(map, desc);
                default: return new World(map, desc);
            }
        }

        public static void AddWorld(WorldDesc desc)
        {
            AddWorld(desc, Interlocked.Increment(ref NextWorldId));
        }

        public static World AddWorld(WorldDesc desc, int id)
        {
            World world = CreateWorld(desc);
            world.Id = id;
            lock (SyncRoot)
            {
                Worlds[world.Id] = world;
            }
#if DEBUG
            Program.Print(PrintType.Debug, $"Added World ID <{world.Id}> <{desc.Id}:{desc.DisplayName}>");
#endif
            return world;
        }

        public static int AddWorld(World world)
        {
            world.Id = Interlocked.Increment(ref NextWorldId);
            lock (SyncRoot)
            {
                Worlds[world.Id] = world;
            }
            return world.Id;
        }

        public static Portal PlacePortal(World world, string portalId, World target)
        {
            ushort type = Resources.Id2Object[portalId].Type;
            IntPoint spawn = world.GetRegion(Region.Spawn);
            for (int r = 1; r < 20; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        //Indexed directly for the link below: Tile is a
                        //struct, so a GetTile local would be a copy.
                        int tx = spawn.X + dx;
                        int ty = spawn.Y + dy;
                        Tile? tile = world.GetTile(tx, ty);
                        if (tile == null || tile.Value.StaticObject != null)
                            continue;
                        Portal portal = new Portal(type) { WorldInstance = target };
                        if (world.AddEntity(portal, new Position(spawn.X + dx + 0.5f, spawn.Y + dy + 0.5f)) != -1)
                        {
                            world.Tiles[tx, ty].StaticObject = portal;
                            world.BumpTile(tx, ty);
#if DEBUG
                            Program.Print(PrintType.Debug, $"Placed portal <{portalId}> to <{target.Name}> at <{spawn.X + dx},{spawn.Y + dy}>");
#endif
                            return portal;
                        }
                    }
#if DEBUG
            Program.Print(PrintType.Error, $"Failed to place portal <{portalId}>.");
#endif
            return null;
        }

        public static World GetWorld(int id)
        {
            lock (SyncRoot)
            {
                if (Worlds.TryGetValue(id, out World world))
                    return world;
                return null;
            }
        }

        //Personal vault instance per account, mirroring realm-src-master
        //wServer/realm/worlds/logic/Vault.cs.
        public static World GetVaultWorld(Client client)
        {
            lock (SyncRoot)
            {
                if (VaultWorlds.TryGetValue(client.Account.Id, out World world))
                    return world;
                WorldDesc desc = Resources.Worlds["Vault"];
                world = CreateWorld(desc, 0);
                world.Id = Interlocked.Increment(ref NextWorldId);
                Worlds[world.Id] = world;
                if (world is VaultWorld vault)
                    vault.PopulateVault(client);
                VaultWorlds[client.Account.Id] = world;
                return world;
            }
        }

        public static bool TryGetPortalDungeon(int portalId, out string dungeon)
        {
            lock (SyncRoot)
            {
                return PortalDungeons.TryGetValue(portalId, out dungeon);
            }
        }

        public static void RemovePortalDungeon(int portalId)
        {
            lock (SyncRoot)
            {
                PortalDungeons.Remove(portalId);
            }
        }

        //Fresh dungeon instance per portal, mirroring the dynamic case of
        //realm-src-master Portal.CreateWorld. The dungeon is a Worlds.xml
        //entry; callers check Dungeons.DungeonWorld.IsSupported first.
        public static World GetDungeonWorld(Portal portal, WorldDesc desc)
        {
            World world = CreateDungeonWorld(desc);
            portal.WorldInstance = world;
            lock (SyncRoot)
            {
                PortalDungeons[portal.Id] = desc.Id;
            }
            return world;
        }

        //Portal-less dungeon instance for callers that move players into a
        //dungeon without a portal (see /quake). Swept like any other dungeon
        //once empty.
        public static World CreateDungeonWorld(WorldDesc desc)
        {
            int seed = Guid.NewGuid().GetHashCode();
            World world = desc.Id == "Candyland Hunting Grounds"
                ? new Dungeons.Candyland.World(desc, seed)
                : new Dungeons.DungeonWorld(desc, seed);
            AddWorld(world);
            return world;
        }

        //Shared hall instance per guild, mirroring GuildHall.cs. The map tier
        //follows the guild level; an occupied hall keeps its tier until empty.
        public static World GetGuildHallWorld(string guildName)
        {
            int level = Database.GetGuildLevel(guildName);
            lock (SyncRoot)
            {
                if (GuildHallWorlds.TryGetValue(guildName, out World existing))
                {
                    if (existing is GuildHallWorld hall && hall.Level == level)
                        return existing;
                    if (existing.Players.Count > 0)
                        return existing;
                    Worlds.Remove(existing.Id);
                    GuildHallWorlds.Remove(guildName);
                }
                WorldDesc desc = Resources.Worlds["GuildHall"];
                World world = CreateWorld(desc, Math.Min(level, desc.Maps.Length - 1));
                world.Id = Interlocked.Increment(ref NextWorldId);
                Worlds[world.Id] = world;
                if (world is GuildHallWorld hallWorld)
                {
                    hallWorld.GuildName = guildName;
                    hallWorld.Level = level;
                }
                GuildHallWorlds[guildName] = world;
                return world;
            }
        }

        //Castle siege world for a quaking realm, mirroring the castle
        //spawn in realm-src-master wServer/realm/Oryx.cs SendToCastle.
        //CastleWorld.PlayersEntering fans out spawn points there. The
        //"Castle" map file and Worlds.xml entry are added by hand.
        public static World CreateCastleWorld(int playersEntering)
        {
            World world = CreateWorld(Resources.Worlds["Castle"]);
            if (world is CastleWorld castle)
                castle.PlayersEntering = playersEntering;
            AddWorld(world);
            return world;
        }

        public static void QuakeRealmToCastle(RealmWorld realm)
        {
            if (realm.Players.Count == 0)
                return;
            if (!Settings.AsyncWorldCreation)
            {
                World syncCastle = CreateCastleWorld(realm.Players.Count);
                realm.QuakeToWorld(syncCastle);
                return;
            }
            //Build the castle off the tick thread: construction is a full
            //tile loop plus population, and the sync version freezes every
            //world for its duration at the climax of the event. The realm
            //keeps ticking meanwhile; reconnect timers start at publish
            //(inside QuakeToWorld), so clients can only ever be pointed at
            //a world that already exists. Closed is set now so nobody
            //re-enters during the build (QuakeToWorld re-asserts it).
            if (!_castleBuildsInFlight.Add(realm.Id))
                return;
            realm.Closed = true;
            int entering = realm.Players.Count;
            Task.Run(() =>
            {
                try
                {
                    World castle = CreateWorld(Resources.Worlds["Castle"]);
                    if (castle is CastleWorld asyncCastle)
                        asyncCastle.PlayersEntering = entering;
                    RunOnMainThread(() =>
                    {
                        lock (SyncRoot)
                        {
                            _castleBuildsInFlight.Remove(realm.Id);
                        }
                        if (realm.Players.Count == 0)
                            return; //Everyone left mid-build: drop the castle rather than leak an empty world.
                        castle.Id = Interlocked.Increment(ref NextWorldId);
                        lock (SyncRoot)
                        {
                            Worlds[castle.Id] = castle;
                        }
                        realm.QuakeToWorld(castle);
                    });
                }
                catch (Exception e)
                {
                    RunOnMainThread(() =>
                    {
                        _castleBuildsInFlight.Remove(realm.Id);
                        Program.Print(PrintType.Error, "Async castle build failed: " + e.Message);
                    });
                }
            });
        }

        //Drops an empty closed realm instance and builds a fresh one under
        //the same id, re-pointing that instance's Nexus portal at it.
        //Adapted from reference Realm.Tick, which re-Inits the world in
        //place; static maps here cannot reset in place, so the world is
        //recreated (the constructor re-rolls SBName, setpieces and
        //the overseer). Instance-keyed: every realm has its own cycle.
        public static void ResetRealm()
        {
            if (GetWorld(RealmId) is RealmWorld realm)
                ResetRealmInstance(realm);
        }

        public static void ResetRealmInstance(RealmWorld realm)
        {
            if (realm.Players.Count > 0)
                return;
            if (!Settings.AsyncWorldCreation)
            {
                //Runs on the realm's own worker thread during parallel ticks:
                //construction touches only the fresh world, and the publish
                //below takes SyncRoot (see PublishResetRealm).
                PublishResetRealm(realm, CreateWorld(Resources.Worlds["Realm"]));
                return;
            }
            //Same off-thread treatment as the castle path above. The old
            //closed realm stays published during the build (it is empty
            //and closed, so nothing can enter); a failed build just clears
            //the flag and the next Overseer tick retries. The set is shared
            //across realm worker threads, so guard it.
            lock (SyncRoot)
            {
                if (!_realmResetsInFlight.Add(realm.Id))
                    return;
            }
            Task.Run(() =>
            {
                try
                {
                    World fresh = CreateWorld(Resources.Worlds["Realm"]);
                    fresh.Id = realm.Id;
                    RunOnMainThread(() =>
                    {
                        lock (SyncRoot)
                        {
                            _realmResetsInFlight.Remove(realm.Id);
                        }
                        PublishResetRealm(realm, fresh);
                    });
                }
                catch (Exception e)
                {
                    RunOnMainThread(() =>
                    {
                        lock (SyncRoot)
                        {
                            _realmResetsInFlight.Remove(realm.Id);
                        }
                        Program.Print(PrintType.Error, "Async realm reset failed: " + e.Message);
                    });
                }
            });
        }

        //Swaps the fresh realm in under its stable id. Re-checks the
        //preconditions: the world may have been replaced while a worker
        //build was in flight. The Nexus statics scan is safe here: Nexus
        //statics only change on single-threaded phases (portal place/remove
        //from handlers and timer actions), never on a world tick.
        private static void PublishResetRealm(RealmWorld old, World fresh)
        {
            lock (SyncRoot)
            {
                if (!(Worlds.TryGetValue(old.Id, out World current) && current == old))
                    return;
                if (old.Players.Count > 0)
                    return;
                Worlds.Remove(old.Id);
                fresh.Id = old.Id;
                Worlds[old.Id] = fresh;
                if (Worlds.TryGetValue(NexusId, out World nexus))
                {
                    foreach (StaticObject stat in nexus.Statics.Values.ToArray())
                        if (stat is Portal portal && portal.WorldInstance == old)
                            portal.WorldInstance = fresh;
                    if (nexus is NexusWorld nexusWorld)
                        nexusWorld.Monitor.UpdateWorldInstance(old.Id, fresh);
                }
            }
        }

        public static Player GetPlayer(string name)
        {
            lock (SyncRoot)
            {
                foreach (Client client in Clients.Values)
                    if (client.Player != null)
                        if (client.Player.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                            return client.Player;
                return null;
            }
        }

        //Guards all cross-world registries (Clients/AccountIdToClientId,
        //Worlds/VaultWorlds/GuildHallWorlds/PortalDungeons/Timers, id
        //counters): world worker threads, the socket IO thread and the tick
        //thread share them. Single lock, leaf-only: holders never block on
        //the DB writer or on sockets while holding it.
        public static readonly object SyncRoot = new object();

        public static void AddClient(Client client)
        {
#if DEBUG
            if (client == null)
                throw new Exception("Client is null.");
            Program.AssertMainThread("Manager.AddClient");
#endif
            lock (SyncRoot)
            {
                client.Id = ++NextClientId;
                Clients[client.Id] = client;
            }
        }

        public static void RemoveClient(Client client)
        {
#if DEBUG
            if (client == null)
                throw new Exception("Client is null.");
            Program.AssertMainThread("Manager.RemoveClient");
#endif
            lock (SyncRoot)
            {
                Clients.Remove(client.Id);
            }
        }

        public static Client GetClient(int accountId)
        {
            lock (SyncRoot)
            {
                if (AccountIdToClientId.TryGetValue(accountId, out int clientId) &&
                    Clients.TryGetValue(clientId, out Client client))
                    return client;
                return null;
            }
        }

        public static void LinkClient(int accountId, int clientId)
        {
            lock (SyncRoot)
            {
                AccountIdToClientId[accountId] = clientId;
            }
        }

        public static void UnlinkClient(int accountId)
        {
            lock (SyncRoot)
            {
                AccountIdToClientId.Remove(accountId);
            }
        }

        //Authoritative in-memory models for one account during a world
        //transfer. Hello/Load consume this instead of re-reading SQLite.
        public class SessionHandoff
        {
            public AccountModel Account;
            public CharacterModel Character;
            public int ExpiresAt;
        }

        public static void StoreHandoff(AccountModel account, CharacterModel character)
        {
            if (account == null)
                return;
            lock (SyncRoot)
            {
                SessionHandoffs[account.Id] = new SessionHandoff
                {
                    Account = account,
                    Character = character,
                    ExpiresAt = TotalTimeUnsynced + SessionHandoffTtlMs
                };
            }
#if DEBUG
            Program.Print(PrintType.Debug, $"Handoff stored for account {account.Id}");
#endif
        }

        public static bool TryTakeHandoff(int accountId, out SessionHandoff handoff)
        {
            lock (SyncRoot)
            {
                if (SessionHandoffs.TryGetValue(accountId, out handoff))
                {
                    SessionHandoffs.Remove(accountId);
#if DEBUG
                    Program.Print(PrintType.Debug, $"Handoff consumed for account {accountId}");
#endif
                    return true;
                }
            }
            handoff = null;
            return false;
        }

        public static bool HasHandoff(int accountId)
        {
            lock (SyncRoot)
            {
                return SessionHandoffs.ContainsKey(accountId);
            }
        }

        //Live session, in-flight reconnect handoff, or a fresh DB load. Admin
        //commands must mutate this instance so a second AccountModel cannot
        //overwrite gold/fame/locks with a stale SQLite snapshot.
        public static AccountModel GetAuthoritativeAccount(int accountId)
        {
            Client live = GetClient(accountId);
            if (live?.Account != null)
                return live.Account;
            lock (SyncRoot)
            {
                if (SessionHandoffs.TryGetValue(accountId, out SessionHandoff handoff))
                    return handoff.Account;
            }
            AccountModel acc = new AccountModel(accountId);
            if (acc.IsNull)
                return null;
            acc.Load();
            return acc;
        }

        //In-flight reconnect authorization. Recorded for every Reconnect
        //(including static negative ids) so a stale dungeon grant cannot
        //outlive an Escape to Nexus. PendingEntrants is only bumped for
        //dynamic worlds (Id > 0) that the empty-dungeon sweep can reclaim.
        //Expires after the same 15s window as SessionHandoff.
        public class PendingTransfer
        {
            public int AccountId;
            public int WorldId;
            public int ExpiresAt;
        }

        public static void RegisterPendingTransfer(int accountId, World world)
        {
            if (world == null)
                return;
            lock (SyncRoot)
            {
                if (PendingTransfers.TryGetValue(accountId, out PendingTransfer existing))
                    ReleasePendingEntrant(existing.WorldId);
                PendingTransfers[accountId] = new PendingTransfer
                {
                    AccountId = accountId,
                    WorldId = world.Id,
                    ExpiresAt = TotalTimeUnsynced + SessionHandoffTtlMs
                };
                if (world.Id > 0)
                    world.PendingEntrants++;
            }
        }

        public static bool TryMatchPendingTransfer(int accountId, int worldId)
        {
            lock (SyncRoot)
            {
                if (!PendingTransfers.TryGetValue(accountId, out PendingTransfer existing))
                    return false;
                if (existing.ExpiresAt < TotalTimeUnsynced)
                    return false;
                return existing.WorldId == worldId;
            }
        }

        //Hello world lookup. Static negative ids stay open (Vault/Guild
        //remap to this account's instance). Positive ids require a live
        //pending transfer for this account matching the requested id.
        public static World ResolveHelloWorld(Client client, int gameId)
        {
            if (client?.Account == null)
                return null;

            switch (gameId)
            {
                case NexusId:
                case RealmId:
                case TutorialId:
                case EditorId:
                    return GetWorld(gameId);
                case VaultId:
                    return GetVaultWorld(client);
                case GuildId:
                    if (string.IsNullOrWhiteSpace(client.Account.GuildName))
                        return null;
                    return GetGuildHallWorld(client.Account.GuildName);
            }

            if (gameId <= 0)
                return null;
            if (!TryMatchPendingTransfer(client.Account.Id, gameId))
                return null;
            return GetWorld(gameId);
        }

        public static void CompletePendingTransfer(int accountId)
        {
            lock (SyncRoot)
            {
                if (!PendingTransfers.TryGetValue(accountId, out PendingTransfer existing))
                    return;
                PendingTransfers.Remove(accountId);
                ReleasePendingEntrant(existing.WorldId);
            }
        }

        private static void ReleasePendingEntrant(int worldId)
        {
            if (Worlds.TryGetValue(worldId, out World world) && world.PendingEntrants > 0)
                world.PendingEntrants--;
        }

        private static void SweepExpiredPendingTransfers()
        {
            List<PendingTransfer> expired = null;
            lock (SyncRoot)
            {
                foreach (KeyValuePair<int, PendingTransfer> kv in PendingTransfers)
                {
                    if (kv.Value.ExpiresAt >= TotalTimeUnsynced)
                        continue;
                    if (expired == null)
                        expired = new List<PendingTransfer>();
                    expired.Add(kv.Value);
                }
                if (expired == null)
                    return;
                for (int i = 0; i < expired.Count; i++)
                {
                    PendingTransfers.Remove(expired[i].AccountId);
                    ReleasePendingEntrant(expired[i].WorldId);
                }
            }
        }

        public static bool ShouldReclaimEmptyDungeon(World world, int now)
        {
            return world is Dungeons.DungeonWorld
                && world.Players.Count == 0
                && world.PendingEntrants == 0
                && now - Math.Max(world.CreatedAt, world.LastEmptyAt) > DungeonEmptyGraceMs;
        }

        public static void SweepEmptyDungeons(IList<World> snapshot)
        {
            int removed = 0;
            int remaining = 0;
            lock (SyncRoot)
            {
                int now = TotalTimeUnsynced;
                for (int i = 0; i < snapshot.Count; i++)
                {
                    World world = snapshot[i];
                    if (!ShouldReclaimEmptyDungeon(world, now))
                        continue;
                    Worlds.Remove(world.Id);
                    removed++;
                }
                remaining = Worlds.Count;
            }
            if (removed > 0)
                Program.Print(PrintType.Info, $"Reclaimed {removed} empty dungeon(s); {remaining} worlds remain");
        }

        private static void SweepExpiredHandoffs()
        {
            List<SessionHandoff> expired = null;
            lock (SyncRoot)
            {
                foreach (KeyValuePair<int, SessionHandoff> kv in SessionHandoffs)
                {
                    if (kv.Value.ExpiresAt >= TotalTimeUnsynced)
                        continue;
                    if (expired == null)
                        expired = new List<SessionHandoff>();
                    expired.Add(kv.Value);
                }
                if (expired == null)
                    return;
                for (int i = 0; i < expired.Count; i++)
                    SessionHandoffs.Remove(expired[i].Account.Id);
            }
            for (int i = 0; i < expired.Count; i++)
            {
                SessionHandoff handoff = expired[i];
                try
                {
                    if (handoff.Character != null && !handoff.Character.Dead)
                        Database.SaveAccountAndCharacter(handoff.Account, handoff.Character);
                    else
                        handoff.Account.Save();
                }
                catch { }
            }
        }

        //Point-in-time copy for the socket IO thread: enumerating the live
        //dict concurrently with Add/Remove would throw.
        public static Client[] SnapshotClients()
        {
            lock (SyncRoot)
            {
                Client[] snapshot = new Client[Clients.Count];
                Clients.Values.CopyTo(snapshot, 0);
                return snapshot;
            }
        }

        //World worker threads schedule timers mid-tick (enemy deaths,
        //grenades, poison); the drain below runs single-threaded pre-phase.
        public static void AddTimedAction(int time, Action action)
        {
            int due = TotalTicks + TicksFromTime(time);
            lock (SyncRoot)
            {
                if (!Timers.TryGetValue(due, out Queue<Action> queue))
                {
                    queue = new Queue<Action>();
                    Timers[due] = queue;
                }
                queue.Enqueue(action);
            }
        }

        public static int TicksFromTime(int time)
        {
#if DEBUG
            if (((float)time / (float)Settings.MillisecondsPerTick) != time / Settings.MillisecondsPerTick)
                throw new Exception("Time out of sync with tick rate.");
#endif
            return time / Settings.MillisecondsPerTick;
        }

        //Sequential branch of Tick (one world or none). Same catch as the parallel branch, applied per world: an
        //exception must not escape Tick into Program's main loop, which has no handler (process exit). Per world
        //also keeps one faulting world from skipping the other worlds' ticks this pass.
        internal static void TickWorldsSequential(List<World> worlds)
        {
            foreach (World world in worlds)
            {
                try
                {
                    world.Tick();
                }
#if DEBUG
                catch (Exception e)
                {
                    Program.Print(PrintType.Error, e.ToString());
                }
#else
                catch { }
#endif
            }
        }

        public static void Tick()
        {
            TotalTimeUnsynced = (int)TickWatch.ElapsedMilliseconds;

            lock (SyncRoot)
            {
                ClientSnapshot.Clear();
                ClientSnapshot.AddRange(Clients.Values);
            }
            foreach (Client client in ClientSnapshot)
                client.Tick();

            //Fixed-cadence gate (W04). Tick k is due at TickDueAt, and each tick moves the deadline forward by
            //exactly one period, never by the tick's start or duration. The old gate fired at
            //LastTickTime + (period - TickDelta), so each tick's own work shortened the next period and ticks
            //ran faster than 10 TPS under load, while TotalTime still advanced 100 ms per tick.
            //At most one tick runs per main-loop pass, so a backlog drains one tick per pass with client
            //packets handled in between. A tick more than MaxTickLagTicks periods late drops the backlog:
            //the schedule restarts from now (with a rate-limited warning), so a long stall does not turn
            //into a burst of world ticks.
            long nowMs = TickWatch.ElapsedMilliseconds;
            int period = Settings.MillisecondsPerTick;
            if (TickDueAt < 0)
                TickDueAt = nowMs + period;
            if (nowMs >= TickDueAt)
            {
                long lateMs = nowMs - TickDueAt;
                if (lateMs > (long)MaxTickLagTicks * period)
                {
                    if (nowMs - _lastTickWarnMs >= TickWarnIntervalMs)
                    {
                        _lastTickWarnMs = nowMs;
                        Program.Print(PrintType.Warn, $"Tick schedule was {lateMs} ms behind (limit {MaxTickLagTicks} ticks); resynced to now");
                    }
                    TickDueAt = nowMs;
                }
                TickDueAt += period;

                LastTickTime = (int)nowMs;
                double traceStartMs = TickTraceEnabled ? TickWatch.Elapsed.TotalMilliseconds : 0;
                double traceMark = traceStartMs;
                double lapTimers = 0, lapMain = 0, lapWorlds = 0, lapSweeps = 0;

                //Only run timers due at tick entry: an action that reschedules
                //itself for the current tick must wait for the next tick,
                //otherwise one misbehaving behavior hangs the main thread here.
                //The action cap is a backstop so a timer flood can only stall
                //one tick, never the server. Drained under SyncRoot: worker
                //threads only ever Add (briefly blocked here), and actions
                //re-entering AddTimedAction take the same thread's lock.
                int timerTick = TotalTicks;
                int timerActions = 0;
                bool timerBudgetExceeded = false;
                const int MaxTimerActionsPerTick = 10000;
                lock (SyncRoot)
                {
                    while (Timers.Count > 0)
                    {
                        //First entry without LINQ: foreach over the concrete
                        //SortedDictionary type uses its struct enumerator, so
                        //this allocates nothing per tick.
                        KeyValuePair<int, Queue<Action>> next = default;
                        foreach (KeyValuePair<int, Queue<Action>> first in Timers)
                        {
                            next = first;
                            break;
                        }
                        if (next.Key > timerTick)
                            break;
                        Timers.Remove(next.Key);
                        while (next.Value.Count > 0)
                        {
                            if (++timerActions > MaxTimerActionsPerTick)
                            {
                                Program.Print(PrintType.Error, "Timer action budget exceeded, deferring remainder");
                                Timers[next.Key] = next.Value;
                                timerBudgetExceeded = true;
                                break;
                            }
                            //One bad timer must not kill the server: isolate it
                            //like MainThreadQueue work below (log in DEBUG,
                            //swallow in release) and continue the tick.
                            try
                            {
                                next.Value.Dequeue()();
                            }
#if DEBUG
                            catch (Exception e)
                            {
                                Program.Print(PrintType.Error, e.ToString());
                            }
#else
                            catch { }
#endif
                        }
                        if (timerBudgetExceeded)
                            break;
                    }
                }

                //Publish worker-finished work (async world builds) before
                //the worlds tick so fresh worlds participate immediately.
                lapTimers = Lap(ref traceMark);
                while (MainThreadQueue.TryDequeue(out Action mainAction))
                {
                    try
                    {
                        mainAction();
                    }
#if DEBUG
                    catch (Exception e)
                    {
                        Program.Print(PrintType.Error, e.ToString());
                    }
#else
                    catch { }
#endif
                }

                //Worlds tick in parallel: each world touches only its own
                //entities/chunks/tiles on its worker thread, while cross-world
                //state (Worlds/Timers/Clients dicts, ids) goes through
                //SyncRoot and persistence through the DB writer. Broadcasts
                //stay parallel inside each world as before. Single-world
                //servers skip the pool entirely.
                lapMain = Lap(ref traceMark);
                lock (SyncRoot)
                {
                    WorldSnapshot.Clear();
                    WorldSnapshot.AddRange(Worlds.Values);
                }
                if (WorldSnapshot.Count > 1)
                {
                    try
                    {
                        Parallel.ForEach(WorldSnapshot, WorldParallelOptions, world => world.Tick());
                    }
#if DEBUG
                    catch (Exception e)
                    {
                        Program.Print(PrintType.Error, e.ToString());
                    }
#else
                    catch { }
#endif
                }
                else
                {
                    TickWorldsSequential(WorldSnapshot);
                }

                lapWorlds = Lap(ref traceMark);
                if (TotalTicks % Settings.TicksPerSecond == 0)
                {
                    SweepExpiredHandoffs();
                    SweepExpiredPendingTransfers();
                }

                //Reclaim empty generated dungeons (personal vaults, guild
                //halls and static worlds are cached separately and kept).
                //Grace + PendingEntrants cover the Reconnect/Hello gap:
                //the instance is created with 0 players, then the old
                //socket dies, then the new Hello looks up the world.
                if (TotalTicks % (Settings.TicksPerSecond * 30) == 0)
                    SweepEmptyDungeons(WorldSnapshot);

                lapSweeps = Lap(ref traceMark);
                if (SyntheticTickLoadMs > 0)
                    Thread.Sleep(SyntheticTickLoadMs);

                TickDelta = (int)(TickWatch.ElapsedMilliseconds - LastTickTime);
                TotalTime += Settings.MillisecondsPerTick;
                TotalTicks++;
                if (TickTraceEnabled)
                    TraceTick(traceStartMs, TickDelta, lapTimers, lapMain, lapWorlds, lapSweeps);
            }
        }

        //One line per tick: start-to-start period and work duration (REALM_TICKTRACE=1 only).
        private static void TraceTick(double startMs, int workMs, double timers, double mainQueue, double worlds, double sweeps)
        {
            int gen2 = GC.CollectionCount(2);
            if (_tracePrevStartMs >= 0)
                Program.Print(PrintType.Info, $"[tick] start={startMs:F2} period={startMs - _tracePrevStartMs:F2} work={workMs} " +
                    $"timers={timers:F1} mainq={mainQueue:F1} worlds={worlds:F1} sweeps={sweeps:F1} gen2={gen2 - _traceGen2} " +
                    $"pending={Database.PendingWriteCount()}");
            _tracePrevStartMs = startMs;
            _traceGen2 = gen2;
        }

        //Trace-only phase timer: returns the ms since mark and moves mark forward. Zero cost when tracing is off.
        private static double Lap(ref double mark)
        {
            if (!TickTraceEnabled)
                return 0;
            double now = TickWatch.Elapsed.TotalMilliseconds;
            double delta = now - mark;
            mark = now;
            return delta;
        }
    }
}
