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

        public static int NextWorldId;
        public static int NextClientId;
        public static Dictionary<int, int> AccountIdToClientId;
        public static Dictionary<int, Client> Clients;
        public static Dictionary<int, World> Worlds;
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
            Timers = new SortedDictionary<int, Queue<Action>>();

            Behaviors = new BehaviorDb();

            AddWorld(Resources.Worlds["Nexus"], NexusId);
        }

        public static void AddWorld(WorldDesc desc)
        {
            AddWorld(desc, ++NextWorldId);
        }

        public static World AddWorld(WorldDesc desc, int id)
        {
            World world = new World(desc.Maps[MathUtils.Next(desc.Maps.Length)], desc);
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

        public static World GetWorld(int id)
        {
            if (Worlds.TryGetValue(id, out World world))
                return world;
            return null;
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

                while (Timers.Count > 0)
                {
                    KeyValuePair<int, Queue<Action>> next = Timers.First();
                    if (next.Key > TotalTicks)
                        break;
                    Timers.Remove(next.Key);
                    while (next.Value.Count > 0)
                        next.Value.Dequeue()();
                }

                WorldSnapshot.Clear();
                WorldSnapshot.AddRange(Worlds.Values);
                foreach (World world in WorldSnapshot)
                    world.Tick();

                TickDelta = (int)(TickWatch.ElapsedMilliseconds - LastTickTime);
                TotalTime += Settings.MillisecondsPerTick;
                TotalTicks++;
            }
        }
    }
}
