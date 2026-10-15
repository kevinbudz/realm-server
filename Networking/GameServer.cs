using RotMG.Common;
using RotMG.Game;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RotMG.Networking
{
    public enum SocketEventState
    {
        Awaiting, //Can start sending/receiving
        InProgress, //Currently sending/receiving
    }

    public class ReceiveState
    {
        public int Filled; //[W10] bytes in PacketBytes not yet framed: a partial packet at the tail, kept for the next read
        public readonly byte[] PacketBytes;

        public ReceiveState()
        {
            PacketBytes = new byte[GameServer.BufferSize];
        }

        public void Reset()
        {
            Filled = 0;
        }
    }

    public class SendState
    {
        public int BytesWritten;
        public int PacketLength; //Total coalesced bytes staged in Data.
        public SocketEventState State;

        public byte[] Data; //Pooled single-flush buffer (BufferSize), rented lazily.
#if DEBUG
        //Set while the send chain stages or SendAsync is reading this buffer.
        //ReturnPooledBuffer throws if it would recycle a still-referenced rent.
        internal volatile byte[] InUseBuffer;
#endif

        public void EnsureBuffer()
        {
            if (Data == null)
                Data = ArrayPool<byte>.Shared.Rent(GameServer.BufferSize);
        }

        public void Grow(int minimumLength)
        {
            byte[] old = Data;
#if DEBUG
            bool transferring = InUseBuffer != null && object.ReferenceEquals(InUseBuffer, old);
            if (transferring)
                InUseBuffer = null;
#endif
            if (old != null)
                ArrayPool<byte>.Shared.Return(old);
            Data = ArrayPool<byte>.Shared.Rent(minimumLength);
#if DEBUG
            if (transferring)
                InUseBuffer = Data;
#endif
        }

        public void Reset()
        {
            State = SocketEventState.Awaiting;
            PacketLength = 0;
            BytesWritten = 0;
            if (Data != null && Data.Length > GameServer.BufferSize)
                ReturnPooledBuffer();
        }

        //The send chain owns pooled-buffer lifetime: call under Client._ioLock
        //when State is Disconnected, or from Reset for an oversized flush.
        public void ReturnPooledBuffer()
        {
            if (Data == null)
                return;
#if DEBUG
            if (InUseBuffer != null && object.ReferenceEquals(InUseBuffer, Data))
                throw new Exception("ArrayPool: returning send buffer still referenced by a SendAsync");
#endif
            ArrayPool<byte>.Shared.Return(Data);
            Data = null;
        }

        public void MarkInUse()
        {
#if DEBUG
            InUseBuffer = Data;
#endif
        }

        public void MarkIdle()
        {
#if DEBUG
            InUseBuffer = null;
#endif
        }
    }

    public static partial class GameServer
    {
        public const int BufferSize = 0x10000;
        public const int PrefixLength = 5;
        public const int PrefixLengthWithId = PrefixLength - 1;
        public const int AddBackMinDelay = 10000;
        public const string TooManyConnectionsMessage = "Too many connections from your address";
        public const string ServerFullMessage = "Server is full";

        private static bool _terminating;
        private static Socket _listener;
        private static ConcurrentQueue<Client> _clients;
        private static ConcurrentQueue<Client> _addBack;
        private static Dictionary<string, int> _connected;
        private static readonly object _connectedLock = new object();

        public static void Init()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Settings.Ports[1]);
            _listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(endpoint);

            _connected = new Dictionary<string, int>();
            _addBack = new ConcurrentQueue<Client>();
            _clients = new ConcurrentQueue<Client>();
            for (int i = 0; i < Settings.MaxClients; i++)
                _clients.Enqueue(new Client(new SendState(), new ReceiveState()));
        }

        public static void Stop()
        {
            _terminating = true;
            StopIo();
            Thread.Sleep(200);
        }

        //[W10] No IO thread. Accepts, receives and sends are completion-driven (SocketAsyncEventArgs). Start() listens and
        //arms the accept loop, and Program also calls StartIo(). Whichever runs first does the work; the other returns.
        private static int _accepting; //Interlocked: 1 once the listener is listening

        public static void StartIo()
        {
            if (Interlocked.Exchange(ref _accepting, 1) != 0)
                return;
            _listener.Listen((int)(Settings.MaxClients * 1.2f));
            Program.Print(PrintType.Info, $"Started GameServer listening at <{_listener.LocalEndPoint}>");

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnAcceptCompleted;
            ArmAccept(args);
        }

        public static void StopIo()
        {
            //Closing the listener aborts the pending accept. Its completion sees _terminating and stops the loop.
            try { _listener?.Close(); } catch { }
        }

        public static void Start()
        {
            StartIo();
        }

        //Accept loop. AcceptAsync completes on a threadpool thread. A completion that happens synchronously is handled
        //inline, not by recursion. There is no per-accept sleep any more (the old Thread.Sleep(10) capped accepts at 100/s).
        private static void ArmAccept(SocketAsyncEventArgs args)
        {
            while (!_terminating)
            {
                args.AcceptSocket = null;
                bool pending;
                try
                {
                    pending = _listener.AcceptAsync(args);
                }
                catch (Exception ex)
                {
                    if (_terminating || ex is ObjectDisposedException)
                        return;
#if DEBUG
                    Program.Print(PrintType.Error, ex.ToString());
#endif
                    Thread.Sleep(10); //transient (for example descriptor exhaustion): back off instead of spinning
                    continue;
                }
                if (pending)
                    return;
                HandleAccept(args);
            }
        }

        private static void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            HandleAccept(e);
            ArmAccept(e);
        }

        private static void HandleAccept(SocketAsyncEventArgs e)
        {
            Socket skt = e.AcceptSocket;
            if (skt == null)
            {
                if (e.SocketError != SocketError.Success && !_terminating)
                    Thread.Sleep(10);
                return;
            }
            if (e.SocketError != SocketError.Success || _terminating)
            {
                CloseSocket(skt);
                return;
            }
            try
            {
                Admit(skt);
            }
            catch (Exception ex)
            {
                //Admit hands the socket off only as its last step, so on an exception the socket is still ours.
#if DEBUG
                Program.Print(PrintType.Error, ex.ToString());
#else
                _ = ex;
#endif
                CloseSocket(skt);
            }
        }

        //Admission for one accepted socket. A rejected socket is closed here. An admitted one belongs to the main thread from
        //the PushWork hand-off on. Nothing is registered (pool slot, IP count) until the peer address is known.
        private static void Admit(Socket skt)
        {
            long hopAccept = HopTrace.Enabled ? HopTrace.Now() : 0; //[hop]
            IPEndPoint remote = (IPEndPoint)skt.RemoteEndPoint;
            string ip = remote.Address.ToString(); //[W10] was Split(':')[0]: every IPv6 peer shared the key "["
            DrainAddBack();

            if (!_clients.TryDequeue(out Client client))
            {
#if DEBUG
                Program.Print(PrintType.Warn, $"No pooled client available, aborted connection from <{remote}>");
#endif
                RejectSocket(skt, ServerFullMessage);
                return;
            }
            if (!TryAcquireIp(ip))
            {
#if DEBUG
                Program.Print(PrintType.Warn, $"Too many clients connected, disconnecting <{remote}>");
#endif
                _clients.Enqueue(client);
                RejectSocket(skt, TooManyConnectionsMessage);
                return;
            }
#if DEBUG
            Program.Print(PrintType.Debug, $"Client connected from <{remote}>");
#endif
            if (HopTrace.Enabled) HopTrace.Accepted(client, hopAccept); //[hop]
            Program.PushWork(() =>
            {
                client.BeginHandling(skt, ip);
            });
        }

        //Returns disconnected clients to the pool once their wait is over AND their last receive and send have completed.
        //A client whose socket operations are still in flight stays queued: its SAEAs and buffers are still in use.
        private static void DrainAddBack()
        {
            List<Client> queueBack = new List<Client>();
            while (_addBack.TryDequeue(out Client add))
            {
                if (Manager.TotalTimeUnsynced - add.DCTime > AddBackMinDelay && add.IoQuiescent)
                    _clients.Enqueue(add);
                else
                    queueBack.Add(add);
            }

            foreach (Client q in queueBack)
                _addBack.Enqueue(q);
        }

        internal static void CloseSocket(Socket skt)
        {
            try { skt.Close(); } catch { }
        }

        public static void AddBack(Client client)
        {
            client.DCTime = Manager.TotalTimeUnsynced;
            _addBack.Enqueue(client);
        }

        //Framed Failure(2, ...) on a raw socket that never entered Client.
        //Used when the pool is empty or the per-IP cap is hit, so the
        //Flash client can show a reason instead of a bare TCP drop.
        public static byte[] FramePacket(ReadOnlySpan<byte> packet) //[W06] span input: builders return spans now
        {
            int length = packet.Length + PrefixLengthWithId;
            byte[] buf = new byte[length];
            buf[0] = (byte)(length >> 24);
            buf[1] = (byte)(length >> 16);
            buf[2] = (byte)(length >> 8);
            buf[3] = (byte)length;
            packet.CopyTo(buf.AsSpan(PrefixLengthWithId));
            return buf;
        }

        public static void RejectSocket(Socket skt, string description)
        {
            try
            {
                byte[] framed = FramePacket(Failure(FailureForceCloseGame, description));
                skt.NoDelay = true;
                skt.Send(framed);
                try { skt.Shutdown(SocketShutdown.Both); } catch { }
            }
            catch { }
            try { skt.Close(); } catch { }
        }

        public static bool TryAcquireIp(string ip)
        {
            int cap = Math.Max(1, Settings.MaxClientsPerIp);
            lock (_connectedLock)
            {
                if (!_connected.TryGetValue(ip, out int n))
                    n = 0;
                if (n >= cap)
                    return false;
                _connected[ip] = n + 1;
                return true;
            }
        }

        public static void ReleaseIp(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return;
            lock (_connectedLock)
            {
                if (!_connected.TryGetValue(ip, out int n))
                    return;
                if (n <= 1)
                    _connected.Remove(ip);
                else
                    _connected[ip] = n - 1;
            }
        }

        public static int CountConnected(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return 0;
            lock (_connectedLock)
            {
                return _connected.TryGetValue(ip, out int n) ? n : 0;
            }
        }
    }
}
