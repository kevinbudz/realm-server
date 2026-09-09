using RotMG.Common;
using RotMG.Game;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;

namespace RotMG
{
    public class Program
    {
        private static bool Terminating;
        private static int MainThread;
        private static ConcurrentQueue<Work> PendingWork;

        public static void Main(string[] args)
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
            PendingWork = new ConcurrentQueue<Work>();
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            Settings.Init();
            Resources.Init();
            if (args.Contains("--seed-test")) //TEMPORARY startup verification hook, reverted after use
            {
                Manager.Init();
                RealmWorld realm = (RealmWorld)Manager.Worlds[Manager.RealmId];
                int foes = 0;
                System.Collections.Generic.Dictionary<TerrainType, int> byTerrain =
                    new System.Collections.Generic.Dictionary<TerrainType, int>();
                foreach (Entity en in realm.Entities.Values)
                    if (en is RotMG.Game.Entities.Enemy e)
                    {
                        foes++;
                        byTerrain.TryGetValue(e.Terrain, out int n);
                        byTerrain[e.Terrain] = n + 1;
                    }
                Console.WriteLine($"Seed test: {foes} realm enemies.");
                foreach (System.Collections.Generic.KeyValuePair<TerrainType, int> kv in byTerrain.OrderBy(k => k.Key.ToString()))
                    Console.WriteLine($"  {kv.Key}: {kv.Value}");

                System.Collections.Generic.List<IntPoint> spawnTiles =
                    realm.Map.Regions.TryGetValue(Region.Spawn, out System.Collections.Generic.List<IntPoint> sp) ? sp
                    : new System.Collections.Generic.List<IntPoint>();
                Console.WriteLine($"Spawn tiles: {spawnTiles.Count}, first=({(spawnTiles.Count > 0 ? spawnTiles[0].X : -1)},{(spawnTiles.Count > 0 ? spawnTiles[0].Y : -1)})");

                System.Collections.Generic.Dictionary<string, int> noneTypes =
                    new System.Collections.Generic.Dictionary<string, int>();
                foreach (Entity en in realm.Entities.Values)
                    if (en is RotMG.Game.Entities.Enemy e && e.Terrain == TerrainType.None)
                    {
                        string name = en.Desc == null ? "<nodesc>" : en.Desc.Id;
                        noneTypes.TryGetValue(name, out int n);
                        noneTypes[name] = n + 1;
                    }
                Console.WriteLine("Top Terrain=None enemy types:");
                foreach (System.Collections.Generic.KeyValuePair<string, int> kv in noneTypes.OrderByDescending(k => k.Value).Take(10))
                    Console.WriteLine($"  {kv.Key}: {kv.Value}");
                Console.WriteLine("Seed test complete, exiting.");
                return;
            }
            Database.Init();
            AppServer.Init();
            GameServer.Init();
            Manager.Init();

            ThreadUtils.StartNewThread(ThreadPriority.Lowest, AppServer.Start);
            ThreadUtils.StartNewThread(ThreadPriority.Lowest, GameServer.Start);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(Terminate);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Terminate);

            while (!Terminating)
            {
                while (PendingWork.TryDequeue(out Work work))
                {
                    try
                    {
                        work.Request();
                        work.Callback?.Invoke();
                    }
#if DEBUG
                    catch (Exception e1)
#endif
#if RELEASE
                    catch
#endif
                    {
#if DEBUG
                        Print(PrintType.Error, e1.ToString());
#endif
                        try
                        {
                            work.Callback?.Invoke();
                        }
#if DEBUG
                        catch (Exception e2)
                        {
                            Print(PrintType.Error, e2.ToString());
                        }
#endif
#if RELEASE
                        catch { }
#endif
                    }
                }

                Database.Tick();
                Manager.Tick();

#if DEBUG
                Thread.Sleep(2);
#endif
#if RELEASE
                Thread.Sleep(1);
#endif
            }

            Terminate(null, null);
        }

        public static void Terminate(object sender, EventArgs e)
        {
            StartTerminating();
            Thread.Sleep(200);
            foreach (Client c in Manager.Clients.Values.ToArray())
            {
                try { c.Disconnect(); }
                catch { }
            }
            Thread.Sleep(200);
            try
            {
                AppServer.Stop();
                GameServer.Stop();
            }
            catch { }
            try
            {
                Database.Shutdown();
            }
            catch { }
        }

        public static void StartTerminating()
        {
            Terminating = true;
        }

        public static void PushWork(Action request, Action callback = null)
        {
            PendingWork.Enqueue(new Work
            {
                Request = request,
                Callback = callback
            });
        }

        public static void Print(PrintType type, object data)
        {
#if RELEASE
            if (type == PrintType.Debug)
                return;
#endif
            string message = $"<{DateTime.Now.ToShortTimeString()}> {data}";
            PushWork(() => 
            {
                switch (type)
                {
                    case PrintType.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                    case PrintType.Info:
                        break;
                    case PrintType.Warn:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case PrintType.Error:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.BackgroundColor = ConsoleColor.Red;
                        break;
                }
                Console.Write(message);
                Console.ResetColor();
                Console.WriteLine();
            });
        }
    }

    public enum PrintType
    {
        Debug,
        Info,
        Warn,
        Error
    }

    public struct Work
    {
        public Action Request;
        public Action Callback;
    }
}
