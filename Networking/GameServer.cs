using RotMG.Common;
using RotMG.Game;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public const byte MaxClientsPerIp = 4;

        private static bool _terminating;
        private static Socket _listener;
        private static ConcurrentQueue<Client> _clients;
        private static ConcurrentQueue<Client> _addBack;
        private static Dictionary<string, int> _connected;

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
                        if (add.IP != null)
                        {
                            _connected[add.IP]--;
                            if (_connected[add.IP] == 0)
                                _connected.Remove(add.IP);
                            add.IP = null;
                        }

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
                        skt.Disconnect(false);
                        continue;
                    }

                    string ip = skt.RemoteEndPoint.ToString().Split(':')[0];
                    if (!_connected.ContainsKey(ip))
                        _connected[ip] = 1;
                    else
                    {
                        if (_connected[ip] == MaxClientsPerIp)
                        {
#if DEBUG
                            Program.Print(PrintType.Warn, $"Too many clients connected, disconnecting <{skt.RemoteEndPoint}>");
#endif
                            skt.Disconnect(false);
                            continue;
                        }
                        _connected[ip]++;
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
    }
}
