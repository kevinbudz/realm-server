using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
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

        public static bool IsMainThread =>
            Thread.CurrentThread.ManagedThreadId == MainThread;

        //DEBUG-only: Disconnect, Manager.Add/RemoveClient and Database sync
        //writes must stay on this thread. Worker threads use RequestDisconnect.
        public static void AssertMainThread(string where)
        {
#if DEBUG
            if (Thread.CurrentThread.ManagedThreadId != MainThread)
                throw new Exception($"{where} must run on the main thread (tid {Thread.CurrentThread.ManagedThreadId}, main {MainThread})");
#endif
        }

        public static void Main(string[] args)
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
            PendingWork = new ConcurrentQueue<Work>();
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            if (args != null && args.Length > 0 && args[0] == "--p21-verify")
            {
                bool ok = ConditionEffectsLayout.Verify();
                DrainWork();
                Console.WriteLine(ok ? "P21 verify: PASS" : "P21 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p4-verify")
            {
                bool ok = Player.VerifyGotoAckClock();
                DrainWork();
                Console.WriteLine(ok ? "P4 verify: PASS" : "P4 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p6-verify")
            {
                bool ok = Player.VerifyTimingPolicy();
                DrainWork();
                Console.WriteLine(ok ? "P6 verify: PASS" : "P6 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            //Pre-Init like the other early verifies: truncated bodies throw
            //during parse before any handler touches worlds or the database.
            if (args != null && args.Length > 0 && args[0] == "--p23-verify")
            {
                bool ok = GameServer.VerifyTruncatedInbound();
                DrainWork();
                Console.WriteLine(ok ? "P23 verify: PASS" : "P23 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p25-verify")
            {
                bool ok = GameServer.VerifyPrePlayerDrop();
                DrainWork();
                Console.WriteLine(ok ? "P25 verify: PASS" : "P25 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && (args[0] == "--p14-verify" || args[0] == "--p16-verify"))
            {
                bool ok = Player.VerifyMoveCorrection();
                DrainWork();
                string tag = args[0] == "--p16-verify" ? "P16" : "P14";
                Console.WriteLine(ok ? $"{tag} verify: PASS" : $"{tag} verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && (args[0] == "--p14-verify" || args[0] == "--p16-verify"))
            {
                bool ok = Player.VerifyMoveCorrection();
                DrainWork();
                string tag = args[0] == "--p16-verify" ? "P16" : "P14";
                Console.WriteLine(ok ? $"{tag} verify: PASS" : $"{tag} verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            //Pre-Init like the other early verifies (no sockets, no
            //worlds): only the game data and database the check reads.
            if (args != null && args.Length > 0 && args[0] == "--sizequake-verify")
            {
                Settings.Init();
                Resources.Init();
                Database.Init();
                bool ok = Player.VerifySizeQuake();
                DrainWork();
                Console.WriteLine(ok ? "SizeQuake verify: PASS" : "SizeQuake verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            //[W13] File sinks for the server only. The --*-verify flags (before and after this point) stay console-only,
            //as before W13. Start runs before Settings.Init so the startup lines are kept.
            if (args == null || args.Length == 0 || !args[0].EndsWith("-verify", StringComparison.Ordinal))
                LogFiles.Start();
            Settings.Init();
            Resources.Init();
            Database.Init();
            AppServer.Init();
            GameServer.Init();
            Manager.Init();
            //[W05] Discover chat commands at startup (main thread), not on the first command. Verify runs skip it.
            if (args == null || args.Length == 0 || !args[0].EndsWith("-verify", StringComparison.Ordinal))
                Program.Print(PrintType.Info, $"Chat commands registered: {RotMG.Game.Commands.CommandTable.Warm()}");

            if (args != null && args.Length > 0 && args[0] == "--p2-verify")
            {
                bool ok = Manager.VerifyDungeonLifecycle();
                DrainWork();
                Console.WriteLine(ok ? "P2 verify: PASS" : "P2 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p10-verify")
            {
                bool ok = Player.VerifyInventoryResync();
                DrainWork();
                Console.WriteLine(ok ? "P10 verify: PASS" : "P10 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p9-verify")
            {
                bool ok = Player.VerifyWorldTransfer();
                DrainWork();
                Console.WriteLine(ok ? "P9 verify: PASS" : "P9 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p15-verify")
            {
                bool ok = Client.VerifyDisconnectThreading();
                DrainWork();
                Console.WriteLine(ok ? "P15 verify: PASS" : "P15 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p3-verify")
            {
                bool ok = Client.VerifySendBackpressure();
                DrainWork();
                Console.WriteLine(ok ? "P3 verify: PASS" : "P3 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p18-verify")
            {
                bool ok = GameServer.VerifyIpAccounting();
                DrainWork();
                Console.WriteLine(ok ? "P18 verify: PASS" : "P18 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && (args[0] == "--p7-verify" || args[0] == "--p8-verify"))
            {
                bool ok = Client.VerifyHandshakeFailure();
                DrainWork();
                string tag = args[0] == "--p8-verify" ? "P8" : "P7";
                Console.WriteLine(ok ? $"{tag} verify: PASS" : $"{tag} verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p13-verify")
            {
                bool ok = GameServer.VerifyClientRobustnessProbes();
                DrainWork();
                Console.WriteLine(ok ? "P13 verify: PASS" : "P13 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p19-verify")
            {
                bool ok = Manager.VerifyWorldAuthorization();
                DrainWork();
                Console.WriteLine(ok ? "P19 verify: PASS" : "P19 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p22-verify")
            {
                bool ok = AppServer.VerifyCharListCredentials();
                DrainWork();
                Console.WriteLine(ok ? "P22 verify: PASS" : "P22 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--p24-verify")
            {
                bool ok = Player.VerifyProjectileIdLockstep();
                DrainWork();
                Console.WriteLine(ok ? "P24 verify: PASS" : "P24 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            if (args != null && args.Length > 0 && args[0] == "--projpath-verify")
            {
                bool ok = ProjectilePathVerify.Run(args.Length > 1 ? args[1] : null);
                DrainWork();
                Console.WriteLine(ok ? "ProjPath verify: PASS" : "ProjPath verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            //[W05] Correctness sweep and static index (Game/W05Verify.cs). Runs after Manager.Init, like the P2/P19 checks.
            if (args != null && args.Length > 0 && args[0] == "--w05-verify")
            {
                bool ok = W05Verify.Run();
                DrainWork();
                Console.WriteLine(ok ? "W05 verify: PASS" : "W05 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            //[W06] Golden bytes for the packet builders, span reader and writer bounds (Networking/W06Golden.cs).
            if (args != null && args.Length == 1 && args[0] == "--w06-verify")
            {
                bool ok = W06Golden.Run();
                Console.WriteLine(ok ? "W06 verify: PASS" : "W06 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            ThreadUtils.StartNewThread(ThreadPriority.Lowest, AppServer.Start);
            ThreadUtils.StartNewThread(ThreadPriority.Lowest, GameServer.Start);
            GameServer.StartIo();

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(Terminate);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Terminate);

            while (!Terminating)
            {
                HopTrace.PassStart(); //[hop] allocation gate: one pass = queued work, Database.Tick, Manager.Tick
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
                int worldTicksBefore = Manager.TotalTicks; //[hop]
                Manager.Tick();
                HopTrace.PassEnd(Manager.TotalTicks != worldTicksBefore); //[hop]

#if DEBUG
                Thread.Sleep(2);
#endif
#if RELEASE
                Thread.Sleep(1);
#endif
            }

            Terminate(null, null);
        }

        private static int _terminateRan;

        public static void Terminate(object sender, EventArgs e)
        {
            //Runs once: ProcessExit fires this on one thread while Main calls
            //it again after the loop exits.
            if (Interlocked.Exchange(ref _terminateRan, 1) == 1)
                return;
            StartTerminating();
            Thread.Sleep(200);
            foreach (Client c in Manager.Clients.Values.ToArray())
            {
                try { c.Disconnect(); }
                catch { }
            }
            //Disconnects used to queue their saves onto PendingWork, which
            //the main loop above no longer drains once Terminating is set, so
            //every graceful shutdown silently lost all session progress.
            //Disconnect now saves synchronously, but AppServer callbacks and
            //any other queued work still need a drain before the checkpoint.
            DrainWork();
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
            //[W13] Last: drains queued log lines to the files and closes them.
            try
            {
                LogFiles.Shutdown();
            }
            catch { }
        }

        //Executes everything still queued, inline, with the same exception
        //semantics as the main loop.
        public static void DrainWork()
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
                    try { Console.WriteLine($"<Work drain error> {e1}"); } catch { }
#endif
                    try
                    {
                        work.Callback?.Invoke();
                    }
                    catch { }
                }
            }
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
            DateTime now = DateTime.Now;
            string text = $"{data}";
            string message = $"<{now.ToShortTimeString()}> {text}";
            //[W13] File sinks: queued for the LogWriter thread. No I/O here.
            LogFiles.Enqueue(type, now, text);
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
