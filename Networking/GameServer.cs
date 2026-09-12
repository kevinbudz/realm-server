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
        public int PacketLength;
        public readonly byte[] PacketBytes;
        public SocketEventState State;

        public ReceiveState()
        {
            PacketBytes = new byte[GameServer.BufferSize];
            PacketLength = GameServer.PrefixLength;
        }

        public byte[] GetPacketBody()
        {
            byte[] packetBody = new byte[PacketLength - GameServer.PrefixLength];
            Array.Copy(PacketBytes, GameServer.PrefixLength, packetBody, 0, packetBody.Length);
            return packetBody;
        }

        public int GetPacketId()
        {
            return PacketBytes[4];
        }

        public void Reset()
        {
            State = SocketEventState.Awaiting;
            PacketLength = 0;
        }
    }

    public class SendState
    {
        public int BytesWritten;
        public int PacketLength; //Total coalesced bytes staged in Data.
        public SocketEventState State;

        public byte[] Data; //Pooled single-flush buffer (BufferSize), rented lazily.
#if DEBUG
        //Set while FlushSendInner is copying or socket.Send'ing this buffer.
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

        //IO thread owns pooled-buffer lifetime: call under Client._ioLock
        //when State is Disconnected, or from Reset for an oversized flush.
        public void ReturnPooledBuffer()
        {
            if (Data == null)
                return;
#if DEBUG
            if (InUseBuffer != null && object.ReferenceEquals(InUseBuffer, Data))
                throw new Exception("ArrayPool: returning send buffer still referenced by FlushSend");
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

        private static bool _ioRunning;
        private static Thread _ioThread;

        //Dedicated socket thread: frames inbound bytes and flushes outbound
        //queues at ~1kHz so syscalls and memcpy never run on the tick
        //thread. Handler dispatch stays in Client.Tick (tick thread), so no
        //game logic runs here; per-client errors only flag the client.
        public static void StartIo()
        {
            if (_ioThread != null)
                return;
            _ioRunning = true;
            _ioThread = new Thread(IoLoop)
            {
                IsBackground = true,
                Name = "GameServerIo",
                Priority = ThreadPriority.AboveNormal
            };
            _ioThread.Start();
        }

        public static void StopIo()
        {
            _ioRunning = false;
            try { _ioThread?.Join(2000); } catch { }
            _ioThread = null;
        }

        private static void IoLoop()
        {
            while (_ioRunning)
            {
                try
                {
                    Client[] snapshot = Manager.SnapshotClients();
                    foreach (Client client in snapshot)
                    {
                        try
                        {
                            client.PollReceive();
                            client.FlushSend();
                        }
                        catch { }
                    }
                }
                catch { }
                Thread.Sleep(1);
            }
        }

        public static void Start()
        {
            _listener.Listen((int)(Settings.MaxClients * 1.2f));
            Program.Print(PrintType.Info, $"Started GameServer listening at <{_listener.LocalEndPoint}>");

            while (!_terminating)
            {
                try
                {
                    //Wait for a client to connect and validate the connection.
                    Socket skt = _listener.Accept();

                    List<Client> queueBack = new List<Client>();
                    while (_addBack.TryDequeue(out Client add))
                    {
                        if (!(Manager.TotalTimeUnsynced - add.DCTime > AddBackMinDelay))
                            queueBack.Add(add);
                        else
                            _clients.Enqueue(add);
                    }

                    foreach (Client q in queueBack)
                        _addBack.Enqueue(q);

#if DEBUG
                    if (skt == null || !skt.Connected)
                    {
                        Program.Print(PrintType.Warn, "<Socket connection aborted>");
                        continue;
                    }
#endif

#if DEBUG
                    Program.Print(PrintType.Debug, $"Client connected from <{skt.RemoteEndPoint}>");
#endif

                    Client client;
                    if (!_clients.TryDequeue(out client))
                    {
#if DEBUG
                        Program.Print(PrintType.Warn, $"No pooled client available, aborted connection from <{skt.RemoteEndPoint}>");
#endif
                        RejectSocket(skt, ServerFullMessage);
                        continue;
                    }

                    string ip = skt.RemoteEndPoint.ToString().Split(':')[0];
                    if (!TryAcquireIp(ip))
                    {
#if DEBUG
                        Program.Print(PrintType.Warn, $"Too many clients connected, disconnecting <{skt.RemoteEndPoint}>");
#endif
                        _clients.Enqueue(client);
                        RejectSocket(skt, TooManyConnectionsMessage);
                        continue;
                    }

                    Program.PushWork(() =>
                    {
                        client.BeginHandling(skt, ip);
                    });

                    Thread.Sleep(10);
                }
#if DEBUG
                catch (Exception ex)
                {
                    Program.Print(PrintType.Error, ex.ToString());
                }
#endif
#if RELEASE
                catch
                {

                }
#endif
            }
        }

        public static void AddBack(Client client)
        {
            client.DCTime = Manager.TotalTimeUnsynced;
            _addBack.Enqueue(client);
        }

        //Framed Failure(2, ...) on a raw socket that never entered Client.
        //Used when the pool is empty or the per-IP cap is hit, so the
        //Flash client can show a reason instead of a bare TCP drop.
        public static byte[] FramePacket(byte[] packet)
        {
            int length = packet.Length + PrefixLengthWithId;
            byte[] buf = new byte[length];
            buf[0] = (byte)(length >> 24);
            buf[1] = (byte)(length >> 16);
            buf[2] = (byte)(length >> 8);
            buf[3] = (byte)length;
            Buffer.BlockCopy(packet, 0, buf, PrefixLengthWithId, packet.Length);
            return buf;
        }

        public static void RejectSocket(Socket skt, string description)
        {
            try
            {
                byte[] framed = FramePacket(Failure(2, description));
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

        public static bool VerifyIpAccounting()
        {
            static bool Fail(string msg)
            {
                Program.Print(PrintType.Error, "P18 verify: " + msg);
                return false;
            }

            if (_connected == null)
                _connected = new Dictionary<string, int>();

            byte[] packet = Failure(2, TooManyConnectionsMessage);
            byte[] framed = FramePacket(packet);
            int length = (framed[0] << 24) | (framed[1] << 16) | (framed[2] << 8) | framed[3];
            if (length != framed.Length)
                return Fail($"framed length prefix {length} != {framed.Length}");
            if (framed[4] != (byte)PacketId.Failure)
                return Fail("framed packet is not Failure");
            using (PacketReader rdr = new PacketReader(new MemoryStream(framed, 5, framed.Length - 5)))
            {
                int errorId = rdr.ReadInt32();
                string text = rdr.ReadString();
                if (errorId != 2 || text != TooManyConnectionsMessage)
                    return Fail($"Failure payload {errorId}/{text}");
            }
            Program.Print(PrintType.Info, "P18 verify: framed Failure(2, Too many connections from your address)");

            int oldCap = Settings.MaxClientsPerIp;
            Settings.MaxClientsPerIp = 4;
            string ip = "p18-verify";
            try
            {
                while (CountConnected(ip) > 0)
                    ReleaseIp(ip);

                for (int i = 0; i < 4; i++)
                {
                    if (!TryAcquireIp(ip))
                        return Fail($"acquire {i} of 4 failed");
                }
                if (TryAcquireIp(ip))
                    return Fail("5th acquire succeeded at cap 4");
                if (CountConnected(ip) != 4)
                    return Fail($"count {CountConnected(ip)} after cap, expected 4");

                ReleaseIp(ip);
                if (CountConnected(ip) != 3)
                    return Fail("ReleaseIp did not decrement immediately");
                if (!TryAcquireIp(ip))
                    return Fail("acquire after immediate release failed (old bug: held until pool recycle)");
                if (CountConnected(ip) != 4)
                    return Fail("count after re-acquire");

                while (CountConnected(ip) > 0)
                    ReleaseIp(ip);
                Program.Print(PrintType.Info, "P18 verify: cap 4 rejects the 5th, ReleaseIp frees a slot immediately");
                return true;
            }
            finally
            {
                while (CountConnected(ip) > 0)
                    ReleaseIp(ip);
                Settings.MaxClientsPerIp = oldCap;
            }
        }
    }
}
