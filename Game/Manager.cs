using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Game.Logic;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace RotMG.Game
{
    public static class Manager
    {
        public const int NexusId = -1;
        public const int RealmId = -2;
        public const int GuildId = -3;
        public const int EditorId = -4;
        public const int VaultId = -5;

        public static int NextWorldId;
        public static int NextClientId;
        public static Dictionary<int, int> AccountIdToClientId;
        public static Dictionary<int, Client> Clients;
        public static Dictionary<int, World> Worlds;
        public static Dictionary<int, World> VaultWorlds;
        public static Dictionary<string, World> GuildHallWorlds;
        public static Dictionary<int, string> PortalDungeons = new Dictionary<int, string>();
        public static SortedDictionary<int, Queue<Action>> Timers;
        private static readonly List<Client> ClientSnapshot = new List<Client>();
        private static readonly List<World> WorldSnapshot = new List<World>();
        public static BehaviorDb Behaviors;
        public static Stopwatch TickWatch;
        public static int TotalTicks;
        public static int TotalTime;
        public static int TotalTimeUnsynced;
        public static int TickDelta;
        public static int LastTickTime;

        public static void Init()
        {
            Player.InitSightCircle();
            Player.InitSightRays();

            TickWatch = Stopwatch.StartNew();
            AccountIdToClientId = new Dictionary<int, int>();
            Clients = new Dictionary<int, Client>();
            Worlds = new Dictionary<int, World>();
            VaultWorlds = new Dictionary<int, World>();
            GuildHallWorlds = new Dictionary<string, World>(StringComparer.OrdinalIgnoreCase);
            Entities.Vendors.MerchantLists.ResolveItems();
            Timers = new SortedDictionary<int, Queue<Action>>();

            Behaviors = new BehaviorDb();

            AddWorld(Resources.Worlds["Nexus"], NexusId);
            AddWorld(Resources.Worlds["Realm"], RealmId);
            AddWorld(Resources.Worlds["Vault"], VaultId);

            //Realm portals live on the map's Realm_Portals region (one per
            //tracked realm, so extra realms each get their own portal). The
            //map-baked Vault and Guild Hall portals resolve dynamically, so
            //no spawn-adjacent placeholder portals are placed here.
            ((NexusWorld)Worlds[NexusId]).Monitor.AddPortal(RealmId);
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
                default: return new World(map, desc);
            }
        }

        public static void AddWorld(WorldDesc desc)
        {
            AddWorld(desc, ++NextWorldId);
        }

        public static World AddWorld(WorldDesc desc, int id)
        {
            World world = CreateWorld(desc);
            world.Id = id;
            Worlds[world.Id] = world;
#if DEBUG
            Program.Print(PrintType.Debug, $"Added World ID <{world.Id}> <{desc.Id}:{desc.DisplayName}>");
#endif
            return world;
        }

        public static int AddWorld(World world)
        {
            world.Id = ++NextWorldId;
            Worlds[world.Id] = world;
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
                        Tile tile = world.GetTile(spawn.X + dx, spawn.Y + dy);
                        if (tile == null || tile.StaticObject != null)
                            continue;
                        Portal portal = new Portal(type) { WorldInstance = target };
                        if (world.AddEntity(portal, new Position(spawn.X + dx + 0.5f, spawn.Y + dy + 0.5f)) != -1)
                        {
                            tile.StaticObject = portal;
                            tile.UpdateCount++;
                            world.UpdateCount++;
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
            if (Worlds.TryGetValue(id, out World world))
                return world;
            return null;
        }

        //Personal vault instance per account, mirroring realm-src-master
        //wServer/realm/worlds/logic/Vault.cs.
        public static World GetVaultWorld(Client client)
        {
            if (VaultWorlds.TryGetValue(client.Account.Id, out World world))
                return world;
            WorldDesc desc = Resources.Worlds["Vault"];
            world = CreateWorld(desc, 0);
            AddWorld(world);
            if (world is VaultWorld vault)
                vault.PopulateVault(client);
            VaultWorlds[client.Account.Id] = world;
            return world;
        }

        //Fresh generated dungeon per portal, mirroring the dynamic case of
        //realm-src-master Portal.CreateWorld.
        public static World GetDungeonWorld(Portal portal, Dungeons.DungeonDef def)
        {
            World world = new Dungeons.DungeonWorld(def, Guid.NewGuid().GetHashCode());
            AddWorld(world);
            portal.WorldInstance = world;
            PortalDungeons[portal.Id] = def.Name;
            return world;
        }

        //Shared hall instance per guild, mirroring GuildHall.cs. The map tier
        //follows the guild level; an occupied hall keeps its tier until empty.
        public static World GetGuildHallWorld(string guildName)
        {
            int level = Database.GetGuildLevel(guildName);
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
            AddWorld(world);
            if (world is GuildHallWorld hallWorld)
            {
                hallWorld.GuildName = guildName;
                hallWorld.Level = level;
            }
            GuildHallWorlds[guildName] = world;
            return world;
        }

        //Castle siege world for a quaking realm, mirroring the castle
        //spawn in realm-src-master wServer/realm/Oryx.cs SendToCastle.
        //Castle.PlayersEntering fans out spawn points there; this
        //codebase has no spawn fan-out, so playersEntering is kept for
        //caller compatibility and unused. The "Castle" map file and
        //Worlds.xml entry are added by hand.
        public static World CreateCastleWorld(int playersEntering)
        {
            World world = CreateWorld(Resources.Worlds["Castle"]);
            AddWorld(world);
            return world;
        }

        public static void QuakeRealmToCastle(RealmWorld realm)
        {
            if (realm.Players.Count == 0)
                return;
            World castle = CreateCastleWorld(realm.Players.Count);
            realm.QuakeToWorld(castle);
        }

        //Drops an empty closed realm and builds a fresh one under the
        //same id, re-pointing the Nexus realm portal at it. Adapted
        //from reference Realm.Tick, which re-Inits the world in place;
        //static maps here cannot reset in place, so the world is
        //recreated (the constructor re-rolls SBName, setpieces and
        //the overseer).
        public static void ResetRealm()
        {
            if (!(Worlds.TryGetValue(RealmId, out World world) && world is RealmWorld))
                return;
            if (world.Players.Count > 0)
                return;
            Worlds.Remove(RealmId);
            World fresh = CreateWorld(Resources.Worlds["Realm"]);
            fresh.Id = RealmId;
            Worlds[RealmId] = fresh;
            if (Worlds.TryGetValue(NexusId, out World nexus))
            {
                foreach (StaticObject stat in nexus.Statics.Values.ToArray())
                    if (stat is Portal portal && portal.WorldInstance == world)
                        portal.WorldInstance = fresh;
                if (nexus is NexusWorld nexusWorld)
                    nexusWorld.Monitor.UpdateWorldInstance(RealmId, fresh);
            }
        }

        public static Player GetPlayer(string name)
        {
            foreach (Client client in Clients.Values)
                if (client.Player != null)
                    if (client.Player.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        return client.Player;
            return null;
        }

        public static void AddClient(Client client)
        {
#if DEBUG
            if (client == null)
                throw new Exception("Client is null.");
#endif
            client.Id = ++NextClientId;
            Clients[client.Id] = client;
        }

        public static void RemoveClient(Client client)
        {
#if DEBUG
            if (client == null)
                throw new Exception("Client is null.");
#endif
            Clients.Remove(client.Id);
        }

        public static Client GetClient(int accountId)
        {
            if (AccountIdToClientId.TryGetValue(accountId, out int clientId))
                return Clients[clientId];
            return null;
        }

        public static void AddTimedAction(int time, Action action)
        {
            int due = TotalTicks + TicksFromTime(time);
            if (!Timers.TryGetValue(due, out Queue<Action> queue))
            {
                queue = new Queue<Action>();
                Timers[due] = queue;
            }
            queue.Enqueue(action);
        }

        public static int TicksFromTime(int time)
        {
#if DEBUG
            if (((float)time / (float)Settings.MillisecondsPerTick) != time / Settings.MillisecondsPerTick)
                throw new Exception("Time out of sync with tick rate.");
#endif
            return time / Settings.MillisecondsPerTick;
        }

        public static void Tick()
        {
            TotalTimeUnsynced = (int)TickWatch.ElapsedMilliseconds;

            ClientSnapshot.Clear();
            ClientSnapshot.AddRange(Clients.Values);
            foreach (Client client in ClientSnapshot)
                client.Tick();

            if ((int)TickWatch.ElapsedMilliseconds - LastTickTime >= (Settings.MillisecondsPerTick - TickDelta))
            {
                LastTickTime = (int)TickWatch.ElapsedMilliseconds;

                //Only run timers due at tick entry: an action that reschedules
                //itself for the current tick must wait for the next tick,
                //otherwise one misbehaving behavior hangs the main thread here.
                //The action cap is a backstop so a timer flood can only stall
                //one tick, never the server.
                int timerTick = TotalTicks;
                int timerActions = 0;
                bool timerBudgetExceeded = false;
                const int MaxTimerActionsPerTick = 10000;
                while (Timers.Count > 0)
                {
                    KeyValuePair<int, Queue<Action>> next = Timers.First();
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
                        next.Value.Dequeue()();
                    }
                    if (timerBudgetExceeded)
                        break;
                }

                WorldSnapshot.Clear();
                WorldSnapshot.AddRange(Worlds.Values);
                foreach (World world in WorldSnapshot)
                    world.Tick();

                //Reclaim empty generated dungeons (personal vaults, guild
                //halls and static worlds are cached separately and kept).
                if (TotalTicks % (Settings.TicksPerSecond * 30) == 0)
                {
                    foreach (World world in WorldSnapshot)
                        if (world is Dungeons.DungeonWorld && world.Players.Count == 0)
                            Worlds.Remove(world.Id);
                }

                TickDelta = (int)(TickWatch.ElapsedMilliseconds - LastTickTime);
                TotalTime += Settings.MillisecondsPerTick;
                TotalTicks++;
            }
        }
    }
}
