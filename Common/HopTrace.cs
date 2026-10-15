//[hop] Hop-latency trace for Escape / world-change timing (Escape -> Reconnect -> nexus first Update).
//Permanent but opt-in: start the server with REALM_HOPTRACE=1 to enable it. When the variable is
//unset, Enabled is false, every public entry point returns at once, and call sites check
//HopTrace.Enabled before they take timestamps or build strings.
//Every line it prints starts with "[hop]". Call sites are tagged "//[hop]" (grep -rn "\[hop\]").
//Timestamps are taken at the event (Stopwatch ticks); Program.Print only
//defers the write to the main loop, so the printed offsets stay accurate.
//Harness: tools/hop/run.sh (analysis: tools/hop/analyze.py).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using RotMG.Networking;

namespace RotMG.Common
{
    public static class HopTrace
    {
        //Read once at startup. This static readonly bool is the only cost when tracing is off.
        public static readonly bool Enabled = Environment.GetEnvironmentVariable("REALM_HOPTRACE") == "1";

        //Client.HopMask bits: one per outbound packet whose socket write is timestamped.
        public const int WatchReconnect = 1;
        public const int WatchMapInfo = 2;
        public const int WatchCreateSuccess = 4;
        public const int WatchUpdate = 8;
        public const int WatchNewTick = 16;

        private const double ExpireSeconds = 30;

        private sealed class Epoch
        {
            public long T0;
            public int Done;
            public long PassBytes; //allocation gate: _passBytes at the Escape (see PassEnd)
            public int Ticks; //allocation gate: _worldTicks at the Escape
        }

        private static readonly object _gate = new object();
        //accountId -> the Escape that opened the hop
        private static readonly Dictionary<int, Epoch> _epochs = new Dictionary<int, Epoch>();
        //accounts whose Reconnect was written; the next accepted socket claims the oldest
        private static readonly Queue<int> _awaitingSocket = new Queue<int>();
        private static int _latestAccount;
        private static long _latestT0;
        private static volatile int _active;

        //Allocation gate (REALM_HOPTRACE only). The main thread runs every world tick and every client Read, so its
        //main-loop passes are measured with GC.GetAllocatedBytesForCurrentThread(). Main thread only, except that the
        //IO thread reads _passBytes and _worldTicks in MarkDone (hence the Volatile reads there).
        private static long _passStart; //allocated bytes on the main thread when the current pass began
        private static long _passBytes; //total bytes of completed passes
        private static long _tickPassBytes; //total bytes of completed passes that ran a world tick
        private static int _worldTicks; //world ticks run since start

        public static bool Active => Enabled && _active > 0;

        public static long Now() => Enabled ? Stopwatch.GetTimestamp() : 0;

        private static double Ms(long from, long to) => (to - from) * 1000.0 / Stopwatch.Frequency;

        //Main thread: Escape handler accepted the request.
        public static void EscapeRx(Client client, long at)
        {
            if (!Enabled || client.Account == null)
                return;
            int acc = client.Account.Id;
            //Bytes so far: completed passes plus the part of this pass already allocated (the Escape runs inside one).
            long passBytes = Volatile.Read(ref _passBytes) + (GC.GetAllocatedBytesForCurrentThread() - _passStart);
            lock (_gate)
            {
                ExpireLocked(at);
                _epochs[acc] = new Epoch { T0 = at, PassBytes = passBytes, Ticks = _worldTicks };
                _latestAccount = acc;
                _latestT0 = at;
                _active = _epochs.Count;
            }
            client.HopAccount = acc;
            Interlocked.Or(ref client.HopMask, WatchReconnect);
            Mark(acc, "escape rx", at);
        }

        //Any thread. Prints only while a hop is open for this account.
        public static void Mark(int acc, string label, long at = 0)
        {
            if (!Enabled || acc < 0 || !Active)
                return;
            long ts = at == 0 ? Now() : at;
            long t0;
            lock (_gate)
            {
                if (!_epochs.TryGetValue(acc, out Epoch epoch))
                    return;
                t0 = epoch.T0;
            }
            Program.Print(PrintType.Info, $"[hop] acc={acc} t=+{Ms(t0, ts):F3}ms {label}");
        }

        //Main thread, before a packet is queued: the IO thread timestamps its socket write.
        public static void Watch(Client client, int bit)
        {
            if (!Enabled)
                return;
            if (client.HopAccount >= 0 && Active)
                Interlocked.Or(ref client.HopMask, bit);
        }

        //Accept thread: a new socket appeared. Claims the oldest account whose Reconnect was written.
        public static void Accepted(Client client, long at)
        {
            if (!Enabled)
                return;
            int acc = -1; //-1 = not a traced hop socket (account ids start at 0)
            lock (_gate)
            {
                if (_awaitingSocket.Count > 0)
                    acc = _awaitingSocket.Dequeue();
            }
            Volatile.Write(ref client.HopMask, 0);
            client.HopAccount = acc;
            if (acc >= 0)
                Mark(acc, $"new connection accepted (client {client.Id})", at);
        }

        //Main thread: a Load/Create queued CreateSuccess. Arms the first Update and NewTick watches.
        public static void Joined(Client client)
        {
            if (!Enabled)
                return;
            int acc = client.HopAccount;
            if (acc < 0)
                return;
            Mark(acc, "joined world (AddEntity + CreateSuccess queued)");
            Interlocked.Or(ref client.HopMask, WatchUpdate | WatchNewTick);
        }

        //IO thread: a watched packet is now in the send buffer.
        public static int WatchBitFor(ReadOnlySpan<byte> packet) //[W06] span overload: the send path passes frame bytes, not arrays
        {
            if (!Enabled || packet.Length == 0)
                return 0;
            switch (packet[0])
            {
                case (byte)GameServer.PacketId.Reconnect: return WatchReconnect;
                case (byte)GameServer.PacketId.MapInfo: return WatchMapInfo;
                case (byte)GameServer.PacketId.CreateSuccess: return WatchCreateSuccess;
                case (byte)GameServer.PacketId.Update: return WatchUpdate;
                case (byte)GameServer.PacketId.NewTick: return WatchNewTick;
            }
            return 0;
        }

        //IO thread, under Client._ioLock: the kernel accepted bytes for a watched packet.
        public static void Written(Client client, int bit, long at)
        {
            if (!Enabled)
                return;
            int acc = client.HopAccount;
            if (acc < 0)
                return;
            switch (bit)
            {
                case WatchReconnect:
                    lock (_gate)
                        _awaitingSocket.Enqueue(acc);
                    Mark(acc, "Reconnect written to socket", at);
                    break;
                case WatchMapInfo:
                    Mark(acc, "MapInfo written to socket", at);
                    break;
                case WatchCreateSuccess:
                    Mark(acc, "CreateSuccess written to socket", at);
                    break;
                case WatchUpdate:
                    Mark(acc, "first Update written to socket", at);
                    break;
                case WatchNewTick:
                    Mark(acc, "first NewTick written to socket", at);
                    break;
                default:
                    return;
            }
            if (bit == WatchUpdate || bit == WatchNewTick)
                MarkDone(acc, bit, at);
        }

        //Closes the hop once both the first Update and first NewTick reached the socket.
        private static void MarkDone(int acc, int bit, long at)
        {
            lock (_gate)
            {
                if (!_epochs.TryGetValue(acc, out Epoch epoch))
                    return;
                epoch.Done |= bit;
                if (epoch.Done != (WatchUpdate | WatchNewTick))
                    return;
                _epochs.Remove(acc);
                _active = _epochs.Count;
                Program.Print(PrintType.Info, $"[hop] acc={acc} t=+{Ms(epoch.T0, at):F3}ms HOP COMPLETE (first Update and NewTick written)");
                //Allocation gate: main-thread bytes from the Escape pass to the last completed pass, and the world ticks in between.
                Program.Print(PrintType.Info, $"[hop] acc={acc} alloc main-thread bytes={Volatile.Read(ref _passBytes) - epoch.PassBytes} world ticks={Volatile.Read(ref _worldTicks) - epoch.Ticks}");
            }
        }

        //Main thread: start of one main-loop pass (queued work, Database.Tick, Manager.Tick). No-op when tracing is off.
        public static void PassStart()
        {
            if (Enabled)
                _passStart = GC.GetAllocatedBytesForCurrentThread();
        }

        //Main thread: end of the pass that PassStart opened. ranTick: a world tick ran during it.
        public static void PassEnd(bool ranTick)
        {
            if (!Enabled)
                return;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - _passStart;
            Interlocked.Add(ref _passBytes, bytes);
            if (!ranTick)
                return;
            Interlocked.Add(ref _tickPassBytes, bytes);
            int ticks = Interlocked.Increment(ref _worldTicks);
            if (ticks % 100 == 0)
                Program.Print(PrintType.Info, $"[hop] world ticks={ticks} avg main-thread bytes per tick={Volatile.Read(ref _tickPassBytes) / ticks}");
        }

        //Database.SetKey / GetKey: open = lock wait + connection open, exec = Execute* (includes the WAL commit + fsync for writes).
        public static void Db(string op, string path, long t0, long x0, long x1)
        {
            if (!Enabled || !Active)
                return;
            long end = Now();
            int acc;
            long latest;
            lock (_gate)
            {
                acc = _latestAccount;
                latest = _latestT0;
            }
            Program.Print(PrintType.Info, $"[hop] acc={acc} t=+{Ms(latest, t0):F3}ms db {op} {path} open={Ms(t0, x0):F3}ms exec={Ms(x0, x1):F3}ms total={Ms(t0, end):F3}ms");
        }

        //Database writer thread: one RunBatch (group of queued jobs).
        public static void Batch(int jobs, int plain, long t0)
        {
            if (!Enabled || !Active)
                return;
            long end = Now();
            int acc;
            long latest;
            lock (_gate)
            {
                acc = _latestAccount;
                latest = _latestT0;
            }
            Program.Print(PrintType.Info, $"[hop] acc={acc} t=+{Ms(latest, t0):F3}ms writer batch jobs={jobs} plain={plain} func={jobs - plain} ms={Ms(t0, end):F3}");
        }

        //Caller holds _gate.
        private static void ExpireLocked(long at)
        {
            List<int> stale = null;
            foreach (KeyValuePair<int, Epoch> pair in _epochs)
            {
                if (Ms(pair.Value.T0, at) > ExpireSeconds * 1000)
                    (stale ??= new List<int>()).Add(pair.Key);
            }
            if (stale != null)
                foreach (int acc in stale)
                    _epochs.Remove(acc);
            _active = _epochs.Count;
        }
    }
}
