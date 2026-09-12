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

        public static void Main(string[] args)
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
            PendingWork = new ConcurrentQueue<Work>();
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            if (args != null && args.Length > 0 && args[0] == "--p4-verify")
            {
                bool ok = Player.VerifyGotoAckClock();
                DrainWork();
                Console.WriteLine(ok ? "P4 verify: PASS" : "P4 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            Settings.Init();
            Resources.Init();
            Database.Init();
            AppServer.Init();
            GameServer.Init();
            Manager.Init();

            if (args != null && args.Length > 0 && args[0] == "--p2-verify")
            {
                bool ok = Manager.VerifyDungeonLifecycle();
                DrainWork();
                Console.WriteLine(ok ? "P2 verify: PASS" : "P2 verify: FAIL");
                Environment.Exit(ok ? 0 : 1);
            }

            ThreadUtils.StartNewThread(ThreadPriority.Lowest, AppServer.Start);
            ThreadUtils.StartNewThread(ThreadPriority.Lowest, GameServer.Start);
            GameServer.StartIo();

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
