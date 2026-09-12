using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RotMG.Networking
{
    public enum ProtocolState
    {
        Handshaked, //Indicates that the client has initialized a connection and is now waiting for Hello packet.
        Awaiting, //Received Hello and is now waiting for a Load/Create packet to put the player in game.
        Connected, //Indicates that the client is now fully initialized and is in game.
        Disconnected //Packets received will no longer be processed and the server will disconnect the client.
    }

    //One framed inbound packet waiting for handler dispatch on the tick
    //thread. The IO thread only frames; GameServer.Read stays tick-threaded
    //so game logic never runs concurrently.
    public struct InboundPacket
    {
        public int Id;
        public byte[] Body;
    }

    public class Client
    {
        public volatile ProtocolState State;
        public int Id;
        public int TargetWorldId;
        public string IP;

        public AccountModel Account;
        public CharacterModel Character;
        public CharacterModel HandoffCharacter;
        public Player Player;
        public wRandom Random;
        public bool Active; //Used in escape to stop incoming packets (so you don't die)
        public bool Reconnecting; //Set by Escape/UsePortal/Quake/chat before Reconnect
        public int DCTime;

        private const int MaxPendingPackets = 256;
        private const int MaxPendingDisconnect = 1024;
        private Socket _socket;
        //Concurrent: parallel world broadcast enqueues from worker threads
        //while FlushSend dequeues on the IO thread.
        private ConcurrentQueue<byte[]> _pending;
        //Framed inbound packets (IO thread produces, tick thread consumes)
        //plus a death flag set by the IO thread when the socket dies.
        private readonly ConcurrentQueue<InboundPacket> _inbound = new ConcurrentQueue<InboundPacket>();
        private volatile bool _socketDead;
        //Packet that fit-checked but couldn't flush; only the IO thread
        //touches this (concurrent queues have no push-front).
        private byte[] _held;
        //Set by Send from any thread when the client stops draining; acted
        //on in Tick. Worker threads must not call Disconnect: they set
        //_disconnectRequested and Tick performs teardown on the main thread.
        private volatile bool _sendOverflow;
        private volatile bool _disconnectRequested;
        private volatile string _disconnectReason;
        //Shared by FlushSend/PollReceive (IO thread) and FinishDisconnect
        //(main thread): socket close and pooled-buffer return never race
        //a mid-copy flush.
        private readonly object _ioLock = new object();
        private SendState _send;
        private ReceiveState _receive;

        public Client(SendState send, ReceiveState receive)
        {
            _pending = new ConcurrentQueue<byte[]>();
            _send = send;
            _receive = receive;
        }

        public void BeginReconnect()
        {
            Reconnecting = true;
            Active = false;
        }

        public void ScheduleReconnectDisconnect()
        {
            int id = Id;
            Manager.AddTimedAction(2000, () =>
            {
                if (Id == id && State != ProtocolState.Disconnected)
                    Disconnect();
            });
        }

        //Any thread: flag the client so Tick tears it down on the main
        //thread. Active is cleared immediately so DrainInbound skips work.
        public void RequestDisconnect(string reason = null)
        {
            if (State == ProtocolState.Disconnected)
                return;
            if (reason != null)
                _disconnectReason = reason;
            Active = false;
            _disconnectRequested = true;
        }

        //Main-thread teardown. Worker threads must use RequestDisconnect;
        //DEBUG asserts, RELEASE defers so a missed site cannot recycle a
        //buffer the IO thread is still writing.
        public void Disconnect()
        {
            Program.AssertMainThread("Client.Disconnect");
            if (!Program.IsMainThread)
            {
                RequestDisconnect();
                return;
            }
            FinishDisconnect(false);
        }

        public void DisconnectWaitingForSave()
        {
            Program.AssertMainThread("Client.Disconnect");
            if (!Program.IsMainThread)
            {
                RequestDisconnect();
                return;
            }
            FinishDisconnect(true);
        }

        private bool ConsumeDisconnectRequest()
        {
            if (!_sendOverflow && !_disconnectRequested)
                return false;
            _sendOverflow = false;
            _disconnectRequested = false;
            return true;
        }

        private void FinishDisconnect(bool waitForSave) //Disconnects, clears all individual client data and pushes the instance back to the server queue.
        {
            Program.AssertMainThread("Client.Disconnect");
            if (State == ProtocolState.Disconnected)
                return;
#if DEBUG
            try
            {
                string reason = _disconnectReason;
                string extra = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
                Program.Print(PrintType.Debug, $"Disconnecting client from <{_socket?.RemoteEndPoint}>{extra}");
            }
            catch (Exception ex) 
            {
                Program.Print(PrintType.Error, ex.ToString());
            }
#endif
            //Queue the save before the socket closes. Bundles commit FIFO on
            //the writer thread, so a disconnect storm no longer pays one
            //fsync per player on the tick thread; Shutdown still drains the
            //queue before the final checkpoint, so graceful stops lose
            //nothing. (The old main-loop work queue was dropped for the
            //opposite reason: it was abandoned on shutdown, silently rolling
            //characters back while counterparties kept their copies.)
            if (Account != null)
            {
                Account.Connected = false;
                Manager.UnlinkClient(Account.Id);

                AccountModel acc = Account;
                CharacterModel ch = Character ?? HandoffCharacter;
                bool reconnecting = Reconnecting;
                if (Player != null && Character != null)
                {
                    Player.SaveToCharacter();
                    if (Player.Parent != null)
                        Player.Parent.RemoveEntity(Player);
                    ch = Character;
                }

                bool dead = ch == null || ch.Dead; //Already saved during death.
                try
                {
                    if (reconnecting && !dead)
                    {
                        Manager.StoreHandoff(acc, ch);
                        if (ch != null)
                            Database.SaveAccountAndCharacter(acc, ch);
                        else
                            acc.Save();
                    }
                    else if (dead)
                        acc.Save();
                    else if (ch != null)
                        Database.SaveAccountAndCharacter(acc, ch, waitForSave);
                    else
                        acc.Save();
                }
                catch { }
            }

            //Socket close + send-buffer return under _ioLock so the IO
            //thread cannot be mid-Send/Receive or mid-copy of _send.Data.
            //Do not take SyncRoot here (lock order: SyncRoot then never
            //_ioLock from the IO snapshot path; _ioLock then never SyncRoot).
            lock (_ioLock)
            {
                State = ProtocolState.Disconnected;
                if (_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                    }
#if DEBUG
                    catch (Exception ex)
                    {
                        Program.Print(PrintType.Error, ex);
                    }
#endif
#if RELEASE
                    catch 
                    {

                    }
#endif
                }
                _held = null;
                _send.Reset();
                _receive.Reset();
                _pending.Clear();
                while (_inbound.TryDequeue(out _)) { }
                _socketDead = false;
                _sendOverflow = false;
                _disconnectRequested = false;
                _disconnectReason = null;
            }

            //Clear data 
            Active = false;
            Reconnecting = false;
            Account = null;
            Player = null;
            Character = null;
            HandoffCharacter = null;
            Random = null;
            TargetWorldId = -1;

            //Push back client to queue
            Manager.RemoveClient(this);
            GameServer.AddBack(this);
        }

        public void BeginHandling(Socket socket, string ip)
        {
            _socket = socket;
            _socket.Blocking = false;

            State = ProtocolState.Handshaked;
            IP = ip;
            Active = true;
            Reconnecting = false;
            HandoffCharacter = null;
            DCTime = -1;
            _socketDead = false;
            _sendOverflow = false;
            _disconnectRequested = false;
            _disconnectReason = null;
            while (_inbound.TryDequeue(out _)) { }

            Manager.AddClient(this);
        }

        //Tick-thread half: no socket calls here. The IO thread frames
        //inbound packets and flushes outbound ones; this only dispatches
        //framed packets to handlers and acts on IO-thread death flags.
        public void Tick()
        {
            try
            {
                if (ConsumeDisconnectRequest())
                {
                    Disconnect();
                    return;
                }

                if (_socket == null || _socketDead || !_socket.Connected)
                {
                    Disconnect();
                    return;
                }

                DrainInbound();

                if (ConsumeDisconnectRequest())
                    Disconnect();
            }
#if DEBUG
            catch (Exception ex)
            {
                Program.Print(PrintType.Error, ex.ToString());
                Disconnect();
            }
#endif
#if RELEASE
            catch 
            { 
                Disconnect(); 
            }
#endif
        }

        private void DrainInbound()
        {
            //Bounded per tick so one flooding client cannot starve the rest.
            int budget = 32;
            while (budget-- > 0 && _inbound.TryDequeue(out InboundPacket packet))
                GameServer.Read(this, packet.Id, packet.Body);
        }

        public void Send(byte[] packet)
        {
            if (_pending.Count >= MaxPendingPackets)
            {
                if (_pending.Count >= MaxPendingDisconnect)
                {
                    _sendOverflow = true; //Client is not draining; Tick drops it instead of growing without bound.
                    return;
                }
                _pending.TryDequeue(out _); //Drop the stalest packet to make room for fresh state.
            }
            _pending.Enqueue(packet);
        }

        //IO-thread half: frame available bytes into inbound packets. Never
        //touches game state and never dispatches handlers; fatal framing
        //only raises _socketDead for the tick thread to act on.
        public void PollReceive()
        {
            lock (_ioLock)
            {
                if (State == ProtocolState.Disconnected || _socketDead)
                    return;
                try
                {
                    PollReceiveInner();
                }
                catch (ObjectDisposedException)
                {
                    _socketDead = true;
                }
                catch (SocketException)
                {
                    _socketDead = true;
                }
            }
        }

        private void PollReceiveInner()
        {
            switch (_receive.State)
            {
                case SocketEventState.Awaiting:
                    if (_socket.Available >= GameServer.PrefixLength)
                    {
                        _socket.Receive(_receive.PacketBytes, GameServer.PrefixLength, SocketFlags.None);
                        _receive.PacketLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_receive.PacketBytes, 0));
                        _receive.State = SocketEventState.InProgress;
                        PollReceiveInner();
                    }
                    break;
                case SocketEventState.InProgress:
                    if (_receive.PacketLength == 1014001516) //Hacky policy file..
                    {
                        _socket.Send(GameServer.PolicyFile, 0, GameServer.PolicyFile.Length, SocketFlags.None);
                        return;
                    }

                    if (_receive.PacketLength < GameServer.PrefixLength ||
                        _receive.PacketLength > GameServer.BufferSize)
                    {
                        _socketDead = true;
                        return;
                    }

                    //Only loop when a full packet was actually consumed: a
                    //fragmented packet must wait for the next poll, otherwise
                    //this recurses with no progress until the stack overflows.
                    if ((_socket.Available + GameServer.PrefixLength) < _receive.PacketLength)
                        break;

                    if (_socket.Available != 0)
                        _socket.Receive(_receive.PacketBytes, GameServer.PrefixLength, _receive.PacketLength - GameServer.PrefixLength, SocketFlags.None);
                    _inbound.Enqueue(new InboundPacket { Id = _receive.GetPacketId(), Body = _receive.GetPacketBody() });
                    _receive.Reset();

                    PollReceiveInner();
                    break;
            }
        }

        //IO-thread half of the old StartSend: coalesce everything queued
        //into the pooled buffer and push it with socket sends. The socket is
        //non-blocking (see BeginHandling), so a full kernel buffer just
        //defers the remainder to a later poll instead of stalling anyone.
        //Only the IO thread touches _send/_held.
        public void FlushSend()
        {
            lock (_ioLock)
            {
                if (State == ProtocolState.Disconnected)
                {
                    _send.ReturnPooledBuffer();
                    return;
                }
                try
                {
                    _send.MarkInUse();
                    try
                    {
                        FlushSendInner();
                    }
                    finally
                    {
                        _send.MarkIdle();
                    }
                }
                catch (ObjectDisposedException)
                {
                    _socketDead = true;
                }
                catch (SocketException ex)
                {
                    //WouldBlock just means the kernel buffer is full; the
                    //remainder goes out on a later poll. Anything else kills it.
                    if (ex.SocketErrorCode != SocketError.WouldBlock)
                        _socketDead = true;
                }
            }
        }

        private void FlushSendInner()
        {
            if (_send.State == SocketEventState.Awaiting)
            {
                if (_held == null && _pending.IsEmpty)
                    return;

                _send.EnsureBuffer();
                _send.MarkInUse();
                byte[] buf = _send.Data;
                int total = 0;
                int queued = _pending.Count + (_held == null ? 0 : 1);
                while (queued-- > 0)
                {
                    byte[] packet;
                    if (_held != null)
                    {
                        packet = _held;
                        _held = null;
                    }
                    else if (!_pending.TryDequeue(out packet))
                        break;
                    //Measured after the dequeue, so a concurrent overflow-drop
                    //by a worker thread can never desync the framing.
                    int length = packet.Length + GameServer.PrefixLengthWithId;
                    if (total + length > buf.Length)
                    {
                        if (total == 0)
                        {
                            //Single packet larger than the pooled buffer: rent an
                            //exact-size buffer for this flush (returned on Reset).
                            _send.Grow(length);
                            buf = _send.Data;
                        }
                        else
                        {
                            _held = packet;
                            break;
                        }
                    }
                    buf[total] = (byte)(length >> 24);
                    buf[total + 1] = (byte)(length >> 16);
                    buf[total + 2] = (byte)(length >> 8);
                    buf[total + 3] = (byte)length;
                    Buffer.BlockCopy(packet, 0, buf, total + GameServer.PrefixLengthWithId, packet.Length);
                    total += length;
                }

                if (total == 0)
                    return;

                _send.PacketLength = total;
                _send.BytesWritten = 0;
                _send.State = SocketEventState.InProgress;
            }

            try
            {
                int written = _socket.Send(_send.Data, _send.BytesWritten, _send.PacketLength - _send.BytesWritten, SocketFlags.None);
                if (written < _send.PacketLength - _send.BytesWritten)
                    _send.BytesWritten += written;
                else
                    _send.Reset();
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
            {
                //Kernel buffer full; the remainder goes out on a later tick.
            }
        }

        //Holds _ioLock and marks the send buffer in-use, mimicking FlushSend
        //mid-copy so Disconnect must wait instead of returning the rent.
        public void DebugSimulateFlushHold(int holdMs)
        {
            lock (_ioLock)
            {
                _send.EnsureBuffer();
                _send.MarkInUse();
                try
                {
                    int n = Math.Min(16, _send.Data.Length);
                    Buffer.BlockCopy(_send.Data, 0, _send.Data, 0, n);
                    if (holdMs > 0)
                        Thread.Sleep(holdMs);
                }
                finally
                {
                    _send.MarkIdle();
                }
            }
        }

        //Headless stand-in for "DEBUG server, 3+ worlds, frequent kicks":
        //RequestDisconnect from Parallel.ForEach over live worlds, Tick
        //teardown on the main thread, ClientSnapshot under SyncRoot while a
        //worker mutates Clients, and the ArrayPool in-use check.
        public static bool VerifyDisconnectThreading()
        {
            int worldCount;
            lock (Manager.SyncRoot)
                worldCount = Manager.Worlds.Count;
            if (worldCount < 3)
            {
                Program.Print(PrintType.Error, $"P15 verify: expected 3+ worlds, have {worldCount}");
                return false;
            }
            Program.Print(PrintType.Info, $"P15 verify: {worldCount} worlds");

            List<World> worlds;
            lock (Manager.SyncRoot)
                worlds = new List<World>(Manager.Worlds.Values);

            const int clientCount = 48;
            List<Client> clients = new List<Client>(clientCount);
            for (int i = 0; i < clientCount; i++)
            {
                Client c = new Client(new SendState(), new ReceiveState());
                c.State = ProtocolState.Connected;
                c.Active = true;
                Manager.AddClient(c);
                clients.Add(c);
            }

            Parallel.ForEach(worlds, world =>
            {
                foreach (Client c in clients)
                    c.RequestDisconnect("p15-verify world " + world.Id);
            });

            foreach (Client c in clients)
            {
                c.Tick();
                if (c.State != ProtocolState.Disconnected)
                {
                    Program.Print(PrintType.Error, "P15 verify: Tick did not tear down a requested disconnect");
                    return false;
                }
            }
            Program.Print(PrintType.Info, $"P15 verify: {clientCount} RequestDisconnect teardowns from {worldCount} worker worlds");

#if DEBUG
            Client workerKick = new Client(new SendState(), new ReceiveState());
            workerKick.State = ProtocolState.Connected;
            workerKick.Active = true;
            Manager.AddClient(workerKick);
            Exception workerEx = null;
            Thread worker = new Thread(() =>
            {
                try { workerKick.Disconnect(); }
                catch (Exception e) { workerEx = e; }
            });
            worker.IsBackground = true;
            worker.Start();
            worker.Join();
            if (workerEx == null)
            {
                Program.Print(PrintType.Error, "P15 verify: worker Disconnect() did not assert");
                workerKick.RequestDisconnect();
                workerKick.Tick();
                return false;
            }
            workerKick.RequestDisconnect();
            workerKick.Tick();
            Program.Print(PrintType.Info, "P15 verify: worker Disconnect() asserted");
#endif

            Client hold = new Client(new SendState(), new ReceiveState());
            hold.State = ProtocolState.Connected;
            hold.Active = true;
            Manager.AddClient(hold);
            Exception holdEx = null;
            Thread io = new Thread(() =>
            {
                try { hold.DebugSimulateFlushHold(80); }
                catch (Exception e) { holdEx = e; }
            });
            io.IsBackground = true;
            io.Start();
            Thread.Sleep(20);
            try { hold.Disconnect(); }
            catch (Exception e) { holdEx = holdEx ?? e; }
            io.Join();
            if (holdEx != null)
            {
                Program.Print(PrintType.Error, $"P15 verify: flush-hold Disconnect threw {holdEx}");
                return false;
            }
            if (hold.State != ProtocolState.Disconnected)
            {
                Program.Print(PrintType.Error, "P15 verify: flush-hold Disconnect did not complete");
                return false;
            }
            Program.Print(PrintType.Info, "P15 verify: Disconnect waited on IO flush without ArrayPool return");

#if DEBUG
            SendState pooled = new SendState();
            pooled.EnsureBuffer();
            pooled.MarkInUse();
            bool threw = false;
            try { pooled.ReturnPooledBuffer(); }
            catch (Exception) { threw = true; }
            if (!threw)
            {
                Program.Print(PrintType.Error, "P15 verify: in-use buffer return did not throw");
                return false;
            }
            pooled.MarkIdle();
            try { pooled.ReturnPooledBuffer(); }
            catch (Exception e)
            {
                Program.Print(PrintType.Error, $"P15 verify: idle buffer return threw {e}");
                return false;
            }
            Program.Print(PrintType.Info, "P15 verify: ArrayPool in-use check fires, idle return is clean");
#endif

            Client dummy = new Client(new SendState(), new ReceiveState());
            dummy.State = ProtocolState.Connected;
            Exception snapshotEx = null;
            Thread mutator = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < 20000; i++)
                    {
                        lock (Manager.SyncRoot)
                        {
                            Manager.Clients[int.MaxValue] = dummy;
                            Manager.Clients.Remove(int.MaxValue);
                        }
                    }
                }
                catch (Exception e) { snapshotEx = e; }
            });
            mutator.IsBackground = true;
            mutator.Start();
            try
            {
                for (int i = 0; i < 20000; i++)
                {
                    lock (Manager.SyncRoot)
                    {
                        List<Client> snap = new List<Client>();
                        snap.AddRange(Manager.Clients.Values);
                    }
                }
            }
            catch (Exception e) { snapshotEx = snapshotEx ?? e; }
            mutator.Join();
            if (snapshotEx != null)
            {
                Program.Print(PrintType.Error, $"P15 verify: ClientSnapshot raced ({snapshotEx.Message})");
                return false;
            }
            Program.Print(PrintType.Info, "P15 verify: ClientSnapshot.AddRange under SyncRoot never threw");

            Exception tickEx = null;
            for (int i = 0; i < 80; i++)
            {
                Client c = new Client(new SendState(), new ReceiveState());
                c.State = ProtocolState.Connected;
                c.Active = true;
                Manager.AddClient(c);
                ThreadPool.QueueUserWorkItem(_ => c.RequestDisconnect("p15-soak"));
                c.RequestDisconnect("p15-soak");
                try { Manager.Tick(); }
                catch (Exception e)
                {
                    tickEx = e;
                    break;
                }
            }
            if (tickEx != null)
            {
                Program.Print(PrintType.Error, $"P15 verify: Manager.Tick threw {tickEx}");
                return false;
            }
            Program.Print(PrintType.Info, "P15 verify: Manager.Tick soak with worker RequestDisconnect did not throw");
            return true;
        }
    }

    public class wRandom
    {
        private uint _seed;

        public wRandom(uint seed)
        {
            _seed = seed;
        }

        public void Drop(int count)
        {
            for (int i = 0; i < count; i++)
                Gen();
        }

        public uint NextIntRange(uint min, uint max)
        {
            return min == max ? min : min + Gen() % (max - min);
        }

        private uint Gen()
        {
            uint lb = 16807 * (_seed & 0xFFFF);
            uint hb = 16807 * (_seed >> 16);
            lb = lb + ((hb & 32767) << 16);
            lb = lb + (hb >> 15);
            if (lb > 2147483647)
            {
                lb = lb - 2147483647;
            }
            return _seed = lb;
        }
    }
}
