using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Buffers;
using System.Buffers.Binary;
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
        public byte[] Body; //pooled; null for an empty body. The tick thread returns it after dispatch.
        public int Length;

        public ReadOnlySpan<byte> Span => Body == null ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>(Body, 0, Length);
    }

    //One outbound frame waiting for FlushSendInner. Data is a pooled array holding the whole frame: the 4-byte BE
    //length prefix, then the packet id and body. Length is the frame size. The IO thread returns Data to ArrayPool
    //once the frame is staged. Droppable packets are cosmetics the client can lose without desync (effects,
    //other-player damage numbers, ally shots). Everything else is protocol state and must never be silently discarded.
    public struct OutgoingPacket
    {
        public byte[] Data;
        public int Length;
        public bool Droppable;

        public static bool IsDroppable(ReadOnlySpan<byte> packet)
        {
            if (packet.Length < 1)
                return false;
            switch ((GameServer.PacketId)packet[0])
            {
                case GameServer.PacketId.ShowEffect:
                case GameServer.PacketId.Notification:
                case GameServer.PacketId.PlaySound:
                case GameServer.PacketId.Damage:
                case GameServer.PacketId.AllyShoot:
                case GameServer.PacketId.GlobalNotification:
                    return true;
                case GameServer.PacketId.Text:
                    return IsTextBubble(packet);
                default:
                    return false;
            }
        }

        //Speech bubbles (bubbleTime > 0). Chat/info/error lines keep
        //bubbleTime 0 and stay reliable until the byte cap.
        private static bool IsTextBubble(ReadOnlySpan<byte> packet)
        {
            int i = 1;
            if (!SkipUtf(packet, ref i))
                return false;
            i += 8; //objectId + numStars
            return i < packet.Length && packet[i] != 0;
        }

        private static bool SkipUtf(ReadOnlySpan<byte> packet, ref int i)
        {
            if (i + 2 > packet.Length)
                return false;
            int len = (packet[i] << 8) | packet[i + 1];
            i += 2;
            if (len < 0 || i + len > packet.Length)
                return false;
            i += len;
            return true;
        }
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
        //Set by BeginTransfer before Reconnect. Also the transferring mark:
        //Player.Tick skips validation/ack sweeps, Death/Damage no-op, and
        //Disconnect persists the already-captured Character when Parent is null.
        public bool Reconnecting;
        //Create whose character row is still committing (W03). A fresh token per Create; the completion joins the
        //world only if the token is still the current one. Cleared on every connection reset, so a late completion
        //cannot join a later connection that reuses this object.
        public object PendingJoin;
        //[hop] trace state, see Common/HopTrace.cs. Stays 0 unless a traced hop owns this socket.
        public int HopAccount = -1; //-1 = not a traced hop socket (account ids start at 0)
        public int HopMask;
        public int CaptureId; //[W06] PacketCapture connection number (0 = not captured), set in BeginHandling.
        private List<(int End, int Bit)> _hopMarks; //[hop] IO thread only (under _ioLock)
        //Set by SendFailureAndClose: Read ignores further packets and Tick
        //skips the handshake timeout so Disconnect cannot wipe the Failure
        //before the 1 s flush window.
        public bool Closing => _closing;
        public int DCTime;
        public int PendingCount => _pending.Count;
        public int PendingBytes => Volatile.Read(ref _pendingBytes);
        public int DroppedCosmeticCount => Volatile.Read(ref _droppedCosmetic);

        //Byte budget, not packet count: 256 frames was ~0.5 s of a busy
        //realm tick stream, so WiFi jitter dropped one-shot Update/Goto/
        //EnemyShoot and permanently desynced the client. Cosmetics are
        //refused past the soft cap; any reliable packet that would pass
        //the hard cap disconnects with Failure instead of dropping state.
        internal const int DroppablePendingBytes = 2 * 1024 * 1024;
        internal const int MaxPendingBytes = 4 * 1024 * 1024;
        //Hello -> Load/Create must finish within this window or Tick
        //returns the pooled slot. Also covers idle TCP and stuck policy
        //sockets if the IO thread never saw "<pol".
        public const int HandshakeTimeoutMs = 5000;
        //First 4 bytes of "<policy-file-request/>" as a big-endian int.
        private const int PolicyFileMagic = 1014001516;
        private Socket _socket;
        //Concurrent: parallel world broadcast enqueues from worker threads
        //while FlushSend dequeues on the IO thread.
        private ConcurrentQueue<OutgoingPacket> _pending;
        private int _pendingBytes;
        private int _droppedCosmetic;
        private int _lastQueueDebugMs;
        //Set when the send budget is exhausted: further Send() no-ops,
        //Failure is staged in _disconnectFlush so it leaves before close.
        private volatile int _slowDisconnect;
        private byte[] _disconnectFlush;
        //Framed inbound packets (IO thread produces, tick thread consumes)
        //plus a death flag set by the IO thread when the socket dies.
        private readonly ConcurrentQueue<InboundPacket> _inbound = new ConcurrentQueue<InboundPacket>();
        private volatile bool _socketDead;
        //Packet that fit-checked but couldn't flush; only the IO thread
        //touches this (concurrent queues have no push-front).
        private OutgoingPacket _held;
        private bool _hasHeld;
        //Legacy overflow flag still consumed by Tick. Slow-link disconnects
        //now go through _slowDisconnect so Failure can flush before close.
        private volatile bool _sendOverflow;
        private volatile bool _disconnectRequested;
        private volatile string _disconnectReason;
        private volatile bool _closing;
        //Shared by FlushSend/PollReceive (IO thread) and FinishDisconnect
        //(main thread): socket close and pooled-buffer return never race
        //a mid-copy flush.
        private readonly object _ioLock = new object();
        private SendState _send;
        private ReceiveState _receive;
        private int _handshakeStartedAt;
        private int _lastInboundAt;

        public Client(SendState send, ReceiveState receive)
        {
            _pending = new ConcurrentQueue<OutgoingPacket>();
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
            FinishDisconnect();
        }

        //Kept for the Hello kick path. The disconnect save is queued like every other save: the next main-thread
        //read of the account (Hello's load included) sees it through the writer's read-your-writes overlay, so the
        //tick thread no longer waits for the commit here.
        public void DisconnectWaitingForSave()
        {
            Program.AssertMainThread("Client.Disconnect");
            if (!Program.IsMainThread)
            {
                RequestDisconnect();
                return;
            }
            FinishDisconnect();
        }

        private bool ConsumeDisconnectRequest()
        {
            if (!_sendOverflow && !_disconnectRequested)
                return false;
            _sendOverflow = false;
            _disconnectRequested = false;
            return true;
        }

        private void FinishDisconnect() //Disconnects, clears all individual client data and pushes the instance back to the server queue.
        {
            Program.AssertMainThread("Client.Disconnect");
            if (State == ProtocolState.Disconnected)
                return;
            bool hopOldSocket = HopTrace.Enabled && Reconnecting && HopAccount >= 0; //[hop]
#if DEBUG
            try
            {
                string reason = _disconnectReason;
                string extra = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
                Program.Print(PrintType.Debug, $"Disconnecting client from <{_socket?.RemoteEndPoint}>{extra} sendQueue={_pending.Count} packets/{Volatile.Read(ref _pendingBytes)} bytes droppedCosmetic={Volatile.Read(ref _droppedCosmetic)}");
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
                    if (Player.Parent != null)
                    {
                        Player.SaveToCharacter();
                        Player.Parent.RemoveEntity(Player);
                    }
                    else if (!reconnecting)
                    {
                        //Dropped without BeginTransfer: still flush live stats
                        //off the entity. A reconnecting player was already
                        //saved and pulled from the world; do not rewrite
                        //Character from the disposed body.
                        Player.SaveToCharacter();
                    }
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
                        Database.SaveAccountAndCharacter(acc, ch);
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
                ReleaseHeld(); //[W06] pooled frames go back to ArrayPool
                _send.Reset();
                _receive.Reset();
                DrainPending();
                DrainInboundQueue();
                _socketDead = false;
                _sendOverflow = false;
                _disconnectRequested = false;
                _disconnectReason = null;
                _slowDisconnect = 0;
                _disconnectFlush = null;
                _closing = false;
                _droppedCosmetic = 0;
                Volatile.Write(ref _pendingBytes, 0);
                _hopMarks = null; //[hop]
            }
            if (hopOldSocket) //[hop]
                HopTrace.Mark(HopAccount, "old socket closed (server side)"); //[hop]

            //Clear data 
            Active = false;
            Reconnecting = false;
            PendingJoin = null;
            string ip = IP;
            IP = null;
            GameServer.ReleaseIp(ip);
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
            CaptureId = PacketCapture.NextConnection(); //[W06]
            //Nagle + Linux delayed ACK added ~40 ms between back-to-back small writes (e.g. CreateSuccess then the first Update).
            //The IO thread already coalesces each poll's packets into one buffer, so NoDelay only affects the rest.
            _socket.NoDelay = true;

            State = ProtocolState.Handshaked;
            IP = ip;
            Active = true;
            Reconnecting = false;
            PendingJoin = null;
            HandoffCharacter = null;
            DCTime = -1;
            //TickWatch, not TotalTimeUnsynced: the latter is 0 until the
            //first Manager.Tick, and Init can already be >5 s in.
            _handshakeStartedAt = (int)Manager.TickWatch.ElapsedMilliseconds;
            _lastInboundAt = _handshakeStartedAt;
            _socketDead = false;
            _sendOverflow = false;
            _disconnectRequested = false;
            _disconnectReason = null;
            _slowDisconnect = 0;
            _disconnectFlush = null;
            _closing = false;
            _droppedCosmetic = 0;
            Volatile.Write(ref _pendingBytes, 0);
            DrainPending(); //[W06]
            DrainInboundQueue(); //[W06]

            Manager.AddClient(this);
            if (HopTrace.Enabled) HopTrace.Mark(HopAccount, "BeginHandling on main thread"); //[hop]
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

                if (!_closing && (State == ProtocolState.Handshaked || State == ProtocolState.Awaiting))
                {
                    int now = Manager.TotalTimeUnsynced;
                    if (now == 0 && Manager.TickWatch != null)
                        now = (int)Manager.TickWatch.ElapsedMilliseconds;
                    int elapsed = unchecked(now - _handshakeStartedAt);
                    if (elapsed >= HandshakeTimeoutMs)
                    {
#if DEBUG
                        Program.Print(PrintType.Debug, $"Handshake timeout client {Id} from <{IP}> after {elapsed} ms");
#endif
                        Disconnect();
                        return;
                    }
                }

#if DEBUG
                LogSendQueueIfStalled();
#endif

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
            {
                NoteInbound();
                try
                {
                    GameServer.Read(this, packet.Id, packet.Span);
                }
                finally
                {
                    ReturnInboundBody(packet); //[W06]
                }
            }
        }

        public void NoteInbound()
        {
            int now = Manager.TotalTimeUnsynced;
            if (now == 0 && Manager.TickWatch != null)
                now = (int)Manager.TickWatch.ElapsedMilliseconds;
            _lastInboundAt = now;
        }

        public bool IdleTimedOut(int timeoutMs)
        {
            if (_lastInboundAt <= 0)
                return false;
            int now = Manager.TotalTimeUnsynced;
            if (now == 0 && Manager.TickWatch != null)
                now = (int)Manager.TickWatch.ElapsedMilliseconds;
            return now - _lastInboundAt > timeoutMs;
        }

        public void Send(byte[] packet)
        {
            if (packet == null)
                return;
            Send(new ReadOnlySpan<byte>(packet), OutgoingPacket.IsDroppable(packet));
        }

        public void Send(byte[] packet, bool droppable)
        {
            if (packet == null)
                return;
            Send(new ReadOnlySpan<byte>(packet), droppable);
        }

        //[W06] Builders return a span over their scratch buffer. The frame is serialized here into a pooled array
        //(length prefix + packet), so the span can be reused as soon as this returns and no byte[] is allocated per send.
        public void Send(ReadOnlySpan<byte> packet)
        {
            Send(packet, OutgoingPacket.IsDroppable(packet));
        }

        public void Send(ReadOnlySpan<byte> packet, bool droppable)
        {
            if (packet.Length == 0)
                return;
            if (State == ProtocolState.Disconnected || _slowDisconnect != 0 || _closing)
                return;

            int framed = packet.Length + GameServer.PrefixLengthWithId;
            int pending = Volatile.Read(ref _pendingBytes);

            if (droppable && pending + framed > DroppablePendingBytes)
            {
                Interlocked.Increment(ref _droppedCosmetic);
#if DEBUG
                LogSendQueue("drop cosmetic");
#endif
                return;
            }

            if (!droppable && pending + framed > MaxPendingBytes)
            {
                BeginSlowDisconnect();
                return;
            }

            byte[] frame = ArrayPool<byte>.Shared.Rent(framed);
            BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, GameServer.PrefixLengthWithId), framed);
            packet.CopyTo(frame.AsSpan(GameServer.PrefixLengthWithId));
            Interlocked.Add(ref _pendingBytes, framed);
            _pending.Enqueue(new OutgoingPacket { Data = frame, Length = framed, Droppable = droppable });
#if DEBUG
            LogSendQueueIfStalled();
#endif
        }

        //Queue Failure, ignore further inbound, close after the IO thread
        //has a second to flush. Immediate Disconnect() clears _pending and
        //the client never sees the reason (P7).
        public const int FailureCloseDelayMs = 1000;

        public void SendFailureAndClose(int id, string text)
        {
            if (State == ProtocolState.Disconnected || _closing)
                return;

            Send(GameServer.Failure(id, text));
            _closing = true;
            Active = false;

            int clientId = Id;
            Manager.AddTimedAction(FailureCloseDelayMs, () =>
            {
                if (Id == clientId && State != ProtocolState.Disconnected)
                    Disconnect();
            });
        }

        //Discard the backlog so Failure can leave the NIC before the
        //socket closes. Staged in _disconnectFlush (not the FIFO) so
        //FlushSendInner writes it first instead of behind up to 4 MiB.
        private void BeginSlowDisconnect()
        {
            if (State == ProtocolState.Disconnected)
                return;
            if (Interlocked.Exchange(ref _slowDisconnect, 1) != 0)
                return;

            Active = false;
            _disconnectReason = "Connection too slow";

            DrainPending(); //[W06]
            Interlocked.Exchange(ref _pendingBytes, 0);

            //[W06] Owned copy: this runs inside Send, possibly in a caller's send loop, so it must not use the shared scratch.
            byte[] failure = GameServer.OwnedFailure(GameServer.FailureForceCloseGame, "Connection too slow");
            Interlocked.Exchange(ref _disconnectFlush, failure);

#if DEBUG
            Program.Print(PrintType.Debug, $"Send queue overflow client {Id}: disconnecting, Failure staged first");
#endif

            Manager.AddTimedAction(1000, () =>
            {
                if (State != ProtocolState.Disconnected)
                    RequestDisconnect("Connection too slow");
            });
        }

        private void AccountDequeued(int framed)
        {
            int cur, next;
            do
            {
                cur = Volatile.Read(ref _pendingBytes);
                next = cur - framed;
                if (next < 0)
                    next = 0;
            } while (Interlocked.CompareExchange(ref _pendingBytes, next, cur) != cur);
        }

#if DEBUG
        private void LogSendQueueIfStalled()
        {
            int bytes = Volatile.Read(ref _pendingBytes);
            int count = _pending.Count;
            if (bytes < 262144 && count < 256)
                return;
            LogSendQueue("stall");
        }

        private void LogSendQueue(string why)
        {
            int now = Environment.TickCount;
            int last = _lastQueueDebugMs;
            if (why != "overflow" && unchecked(now - last) < 1000)
                return;
            if (Interlocked.Exchange(ref _lastQueueDebugMs, now) != last && why != "overflow")
                return;
            Program.Print(PrintType.Debug, $"Send queue {why} client {Id}: {_pending.Count} packets / {Volatile.Read(ref _pendingBytes)} bytes droppedCosmetic={Volatile.Read(ref _droppedCosmetic)}");
        }
#endif

        //IO-thread half: frame available bytes into inbound packets. Never
        //touches game state and never dispatches handlers; fatal framing
        //only raises _socketDead for the tick thread to act on.
        public void PollReceive()
        {
            lock (_ioLock)
            {
                if (State == ProtocolState.Disconnected)
                    return;
                try
                {
                    //Keep draining after the policy reply so a late
                    //"<policy-file-request/>\0" tail is not sitting in the
                    //buffer when Tick closes (that would RST).
                    if (_socketDead)
                    {
                        DrainReceiveBuffer();
                        return;
                    }
                    //Peer FIN: SelectRead && Available==0. Connected stays
                    //true until an I/O, so without this a client that closes
                    //holds its IP slot until the handshake timeout.
                    if (_socket.Poll(0, SelectMode.SelectRead) && _socket.Available == 0)
                    {
                        _socketDead = true;
                        return;
                    }
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

        private void DrainReceiveBuffer()
        {
            if (_socket == null)
                return;
            int available = _socket.Available;
            if (available <= 0)
                return;
            byte[] dump = new byte[Math.Min(available, 256)];
            while (available > 0)
            {
                int n = _socket.Receive(dump, 0, Math.Min(dump.Length, available), SocketFlags.None);
                if (n <= 0)
                    break;
                available = _socket.Available;
            }
        }

        //Frames are read a piece at a time into _receive.PacketBytes: the 5-byte header (4-byte length + id) first, then the
        //rest of the frame. A frame that arrives in pieces waits for the next poll, and the bytes already read are kept.
        private void PollReceiveInner()
        {
            while (true)
            {
                if (_receive.State == SocketEventState.Awaiting)
                {
                    if (!FillReceive(GameServer.PrefixLength))
                        return;
                    _receive.PacketLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_receive.PacketBytes, 0));
                    _receive.State = SocketEventState.InProgress;
                }

                if (_receive.PacketLength == PolicyFileMagic)
                {
                    _socket.Send(GameServer.PolicyFile, 0, GameServer.PolicyFile.Length, SocketFlags.None);
                    DrainReceiveBuffer();
                    _receive.Reset();
                    _socketDead = true;
                    return;
                }

                if (_receive.PacketLength < GameServer.PrefixLength ||
                    _receive.PacketLength > GameServer.BufferSize)
                {
                    _socketDead = true;
                    return;
                }

                if (!FillReceive(_receive.PacketLength))
                    return;

                //[W06] The body is copied once into a pooled array: the tick thread dispatches it later, after this
                //thread has moved on to the next frame in PacketBytes. The tick thread returns the array after dispatch.
                int id = _receive.GetPacketId();
                int bodyLength = _receive.PacketLength - GameServer.PrefixLength;
                byte[] body = null;
                if (bodyLength > 0)
                {
                    body = ArrayPool<byte>.Shared.Rent(bodyLength);
                    Buffer.BlockCopy(_receive.PacketBytes, GameServer.PrefixLength, body, 0, bodyLength);
                }
                if (PacketCapture.Enabled) PacketCapture.Record(CaptureId, PacketCapture.DirInbound, (byte)id, new ReadOnlySpan<byte>(_receive.PacketBytes, GameServer.PrefixLength, bodyLength)); //[W06]
                _inbound.Enqueue(new InboundPacket { Id = id, Body = body, Length = bodyLength });
                _receive.Reset();
            }
        }

        //[W06] Reads from the socket into _receive.PacketBytes until it holds `target` bytes of the current frame (the
        //header included). Returns false when the frame is still incomplete or the socket is dead. A frame the peer stops
        //sending in the middle is never completed: when the peer closes (FIN), the socket is readable with nothing to read,
        //and that marks the client dead. Before this, a truncated frame waited for bytes that would never come.
        private bool FillReceive(int target)
        {
            while (_receive.Filled < target)
            {
                int available = _socket.Available;
                if (available == 0)
                {
                    if (_socket.Poll(0, SelectMode.SelectRead))
                        _socketDead = true;
                    return false;
                }
                int n = _socket.Receive(_receive.PacketBytes, _receive.Filled, Math.Min(available, target - _receive.Filled), SocketFlags.None);
                if (n <= 0)
                {
                    _socketDead = true;
                    return false;
                }
                _receive.Filled += n;
            }
            return true;
        }

        //[W06] Pool hygiene: whichever thread dequeues an entry is the only one that returns its array.
        private void DrainPending()
        {
            while (_pending.TryDequeue(out OutgoingPacket outgoing))
                ArrayPool<byte>.Shared.Return(outgoing.Data);
        }

        private void DrainInboundQueue()
        {
            while (_inbound.TryDequeue(out InboundPacket packet))
                ReturnInboundBody(packet);
        }

        //IO thread only (under _ioLock).
        private void ReleaseHeld()
        {
            if (_hasHeld)
                ArrayPool<byte>.Shared.Return(_held.Data);
            _held = default;
            _hasHeld = false;
        }

        private static void ReturnInboundBody(InboundPacket packet)
        {
            if (packet.Body != null)
                ArrayPool<byte>.Shared.Return(packet.Body);
        }

        //[hop] IO thread (under _ioLock): a watched packet was copied into the send buffer.
        //Its end offset is timestamped once the kernel accepts that many bytes.
        private void HopNoteBuffered(ReadOnlySpan<byte> packet, int end)
        {
            int bit = HopTrace.WatchBitFor(packet);
            if (bit == 0 || (Volatile.Read(ref HopMask) & bit) == 0)
                return;
            Interlocked.And(ref HopMask, ~bit);
            (_hopMarks ??= new List<(int End, int Bit)>()).Add((end, bit));
        }

        //[hop] IO thread: sentUpTo buffer bytes reached the kernel.
        private void HopWritten(int sentUpTo)
        {
            if (_hopMarks == null || _hopMarks.Count == 0)
                return;
            long at = HopTrace.Now();
            int n = 0;
            while (n < _hopMarks.Count && _hopMarks[n].End <= sentUpTo)
                n++;
            for (int i = 0; i < n; i++)
                HopTrace.Written(this, _hopMarks[i].Bit, at);
            _hopMarks.RemoveRange(0, n);
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
                byte[] disconnect = Interlocked.Exchange(ref _disconnectFlush, null);
                if (disconnect == null && !_hasHeld && _pending.IsEmpty)
                    return;

                _send.EnsureBuffer();
                _send.MarkInUse();
                byte[] buf = _send.Data;
                int total = 0;

                if (disconnect != null)
                {
                    //Failure-first: do not coalesce the abandoned backlog.
                    ReleaseHeld();
                    int length = disconnect.Length + GameServer.PrefixLengthWithId;
                    if (length > buf.Length)
                    {
                        _send.Grow(length);
                        buf = _send.Data;
                    }
                    buf[0] = (byte)(length >> 24);
                    buf[1] = (byte)(length >> 16);
                    buf[2] = (byte)(length >> 8);
                    buf[3] = (byte)length;
                    Buffer.BlockCopy(disconnect, 0, buf, GameServer.PrefixLengthWithId, disconnect.Length);
                    if (PacketCapture.Enabled) PacketCapture.Record(CaptureId, PacketCapture.DirOutbound, disconnect[0], new ReadOnlySpan<byte>(disconnect, 1, disconnect.Length - 1)); //[W06]
                    total = length;
                }
                else
                {
                    int queued = _pending.Count + (_hasHeld ? 1 : 0);
                    while (queued-- > 0)
                    {
                        OutgoingPacket outgoing;
                        if (_hasHeld)
                        {
                            outgoing = _held;
                            _held = default;
                            _hasHeld = false;
                        }
                        else if (!_pending.TryDequeue(out outgoing))
                            break;
                        else
                            AccountDequeued(outgoing.Length);

                        int length = outgoing.Length;
                        if (total + length > buf.Length)
                        {
                            if (total == 0)
                            {
                                //Single frame larger than the pooled buffer: rent an
                                //exact-size buffer for this flush (returned on Reset).
                                _send.Grow(length);
                                buf = _send.Data;
                            }
                            else
                            {
                                _held = outgoing;
                                _hasHeld = true;
                                break;
                            }
                        }
                        Buffer.BlockCopy(outgoing.Data, 0, buf, total, length);
                        if (PacketCapture.Enabled) PacketCapture.Record(CaptureId, PacketCapture.DirOutbound, outgoing.Data[GameServer.PrefixLength - 1], new ReadOnlySpan<byte>(outgoing.Data, GameServer.PrefixLength, length - GameServer.PrefixLength)); //[W06]
                        if (HopTrace.Enabled && Volatile.Read(ref HopMask) != 0) //[hop]
                            HopNoteBuffered(new ReadOnlySpan<byte>(outgoing.Data, GameServer.PrefixLengthWithId, length - GameServer.PrefixLengthWithId), total + length); //[hop]
                        total += length;
                        ArrayPool<byte>.Shared.Return(outgoing.Data);
                    }
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
                {
                    _send.BytesWritten += written;
                    if (HopTrace.Enabled) HopWritten(_send.BytesWritten); //[hop]
                }
                else
                {
                    if (HopTrace.Enabled) HopWritten(_send.PacketLength); //[hop]
                    _send.Reset();
                }
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

        //Headless: never drop protocol-state packets, drop cosmetics at
        //2 MiB, disconnect with Failure staged first at 4 MiB.
        public static bool VerifySendBackpressure()
        {
            static byte[] Dummy(GameServer.PacketId id, int extra)
            {
                byte[] packet = new byte[1 + extra];
                packet[0] = (byte)id;
                return packet;
            }

            static bool Fail(string msg)
            {
                Program.Print(PrintType.Error, "P3 verify: " + msg);
                return false;
            }

            byte[] bubble = GameServer.Text("n", 1, 0, 5, "", "hi").ToArray();
            byte[] info = GameServer.Text("", 0, -1, 0, "", "hello").ToArray();
            if (!OutgoingPacket.IsDroppable(bubble))
                return Fail("Text bubble should be droppable");
            if (OutgoingPacket.IsDroppable(info))
                return Fail("info Text should not be droppable");
            if (!OutgoingPacket.IsDroppable(Dummy(GameServer.PacketId.ShowEffect, 8)))
                return Fail("ShowEffect should be droppable");
            if (!OutgoingPacket.IsDroppable(Dummy(GameServer.PacketId.Damage, 8)))
                return Fail("Damage should be droppable");
            if (!OutgoingPacket.IsDroppable(Dummy(GameServer.PacketId.AllyShoot, 8)))
                return Fail("AllyShoot should be droppable");
            if (OutgoingPacket.IsDroppable(Dummy(GameServer.PacketId.Update, 8)))
                return Fail("Update must not be droppable");
            if (OutgoingPacket.IsDroppable(Dummy(GameServer.PacketId.Goto, 8)))
                return Fail("Goto must not be droppable");
            if (OutgoingPacket.IsDroppable(Dummy(GameServer.PacketId.EnemyShoot, 8)))
                return Fail("EnemyShoot must not be droppable");
            if (OutgoingPacket.IsDroppable(Dummy(GameServer.PacketId.MapInfo, 8)))
                return Fail("MapInfo must not be droppable");
            if (OutgoingPacket.IsDroppable(Dummy(GameServer.PacketId.CreateSuccess, 8)))
                return Fail("CreateSuccess must not be droppable");
            Program.Print(PrintType.Info, "P3 verify: droppable tagging");

            Client keep = new Client(new SendState(), new ReceiveState());
            keep.State = ProtocolState.Connected;
            keep.Active = true;
            keep.Id = 1;

            byte[] createSuccess = GameServer.CreateSuccess(42, 7).ToArray();
            keep.Send(createSuccess);

            byte[] tick = Dummy(GameServer.PacketId.NewTick, 8);
            for (int i = 0; i < 300; i++)
                keep.Send(tick);
            if (keep.PendingCount != 301)
                return Fail($"drop-oldest still active: queued {keep.PendingCount} after 301 reliable packets");
            //[W06] Data is the whole frame (length prefix first), so the packet id is at PrefixLength - 1.
            if (!keep._pending.TryPeek(out OutgoingPacket head) || head.Data[GameServer.PrefixLength - 1] != (byte)GameServer.PacketId.CreateSuccess)
                return Fail("CreateSuccess was dropped from the head of the queue");
            Program.Print(PrintType.Info, "P3 verify: 301 reliable packets kept (old cap was 256), head is CreateSuccess");

            Client cosmetics = new Client(new SendState(), new ReceiveState());
            cosmetics.State = ProtocolState.Connected;
            cosmetics.Active = true;
            cosmetics.Id = 2;
            byte[] effect = Dummy(GameServer.PacketId.ShowEffect, 1024);
            int effectFramed = effect.Length + GameServer.PrefixLengthWithId;
            while (cosmetics.PendingBytes + effectFramed <= DroppablePendingBytes)
                cosmetics.Send(effect);
            int cappedCount = cosmetics.PendingCount;
            int cappedBytes = cosmetics.PendingBytes;
            for (int i = 0; i < 1000; i++)
                cosmetics.Send(effect);
            if (cosmetics.PendingCount != cappedCount || cosmetics.PendingBytes != cappedBytes)
                return Fail($"cosmetics queued past soft cap: {cosmetics.PendingCount} packets / {cosmetics.PendingBytes} bytes");
            if (cosmetics.DroppedCosmeticCount < 1000)
                return Fail($"expected 1000 dropped cosmetics, got {cosmetics.DroppedCosmeticCount}");

            byte[] update = Dummy(GameServer.PacketId.Update, 1024);
            cosmetics.Send(update);
            if (cosmetics.PendingCount != cappedCount + 1)
                return Fail("reliable Update was refused at the cosmetic watermark");
            Program.Print(PrintType.Info, $"P3 verify: cosmetics capped at {cappedBytes} bytes, Update still queued");

            Client overflow = new Client(new SendState(), new ReceiveState());
            overflow.State = ProtocolState.Connected;
            overflow.Active = true;
            overflow.Id = 3;
            overflow.Send(createSuccess);
            byte[] filler = Dummy(GameServer.PacketId.NewTick, 1024);
            int fillerFramed = filler.Length + GameServer.PrefixLengthWithId;
            while (overflow.PendingBytes + fillerFramed <= MaxPendingBytes)
                overflow.Send(filler);
            overflow.Send(filler);
            if (overflow._slowDisconnect == 0)
                return Fail("reliable overflow did not start slow-disconnect");
            if (overflow._disconnectFlush == null || overflow._disconnectFlush[0] != (byte)GameServer.PacketId.Failure)
                return Fail("Failure was not staged first on overflow");
            if (overflow.PendingCount != 0)
                return Fail($"backlog was not discarded for Failure-first flush ({overflow.PendingCount} left)");
            overflow.Send(filler);
            if (overflow.PendingCount != 0)
                return Fail("Send after overflow still queued");

            overflow._send.EnsureBuffer();
            byte[] disconnect = Interlocked.Exchange(ref overflow._disconnectFlush, null);
            if (disconnect == null)
                return Fail("disconnect flush was consumed unexpectedly");
            int length = disconnect.Length + GameServer.PrefixLengthWithId;
            byte[] buf = overflow._send.Data;
            buf[0] = (byte)(length >> 24);
            buf[1] = (byte)(length >> 16);
            buf[2] = (byte)(length >> 8);
            buf[3] = (byte)length;
            Buffer.BlockCopy(disconnect, 0, buf, GameServer.PrefixLengthWithId, disconnect.Length);
            if (buf[4] != (byte)GameServer.PacketId.Failure)
                return Fail("framed disconnect packet is not Failure");
            using (PacketReader rdr = new PacketReader(new MemoryStream(disconnect, 1, disconnect.Length - 1)))
            {
                int errorId = rdr.ReadInt32();
                string text = rdr.ReadString();
                if (errorId != 2 || text != "Connection too slow")
                    return Fail($"Failure payload {errorId}/{text}");
            }
            Program.Print(PrintType.Info, "P3 verify: overflow discards backlog, stages Failure(2, Connection too slow) first");
            return true;
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

        public static bool VerifyHandshakeFailure()
        {
            static bool Fail(string msg)
            {
                Program.Print(PrintType.Error, "P7/P8 verify: " + msg);
                return false;
            }

            static byte[] HelloBody(string version, int gameId, string user, string pass)
            {
                PacketWriter wtr = PacketWriter.Rent();
                wtr.Write(version);
                wtr.Write(gameId);
                wtr.Write(user);
                wtr.Write(pass);
                wtr.Write(0);
                return PacketWriter.RentedBytes().ToArray();
            }

            static bool PeekFailure(Client client, int expectedId, string expectedText, string label)
            {
                //[W06] Data is the whole frame: length prefix, then the id, then the body.
                if (!client._pending.TryPeek(out OutgoingPacket head) || head.Data == null || head.Length < GameServer.PrefixLength)
                {
                    Program.Print(PrintType.Error, $"P7/P8 verify: {label}: queue empty");
                    return false;
                }
                if (head.Data[GameServer.PrefixLength - 1] != (byte)GameServer.PacketId.Failure)
                {
                    Program.Print(PrintType.Error, $"P7/P8 verify: {label}: packet id {head.Data[GameServer.PrefixLength - 1]}");
                    return false;
                }
                using (PacketReader rdr = new PacketReader(new MemoryStream(head.Data, GameServer.PrefixLength, head.Length - GameServer.PrefixLength)))
                {
                    int errorId = rdr.ReadInt32();
                    string text = rdr.ReadString();
                    if (errorId != expectedId || text != expectedText)
                    {
                        Program.Print(PrintType.Error, $"P7/P8 verify: {label}: Failure({errorId}, {text})");
                        return false;
                    }
                }
                return true;
            }

            if (Settings.MillisecondsPerTick <= 0 || string.IsNullOrEmpty(Settings.BuildVersion))
                return Fail("Settings not loaded");
            if (Settings.BuildVersion != "1.0.0")
                return Fail($"BuildVersion {Settings.BuildVersion}, expected 1.0.0");
            Program.Print(PrintType.Info, $"P8 verify: Settings.BuildVersion={Settings.BuildVersion}");

            Client close = new Client(new SendState(), new ReceiveState());
            close.State = ProtocolState.Handshaked;
            close.Active = true;
            close.Id = 7001;
            close.SendFailureAndClose(GameServer.FailureForceCloseGame, "Failed to load character.");
            if (close.State == ProtocolState.Disconnected)
                return Fail("SendFailureAndClose disconnected immediately (Failure would not flush)");
            if (!close.Closing || close.Active)
                return Fail("Closing flag/Active not set");
            if (close.PendingCount != 1)
                return Fail($"expected 1 queued Failure, have {close.PendingCount}");
            if (!PeekFailure(close, GameServer.FailureForceCloseGame, "Failed to load character.", "SendFailureAndClose"))
                return false;

            close.SendFailureAndClose(GameServer.FailureForceCloseGame, "again");
            if (close.PendingCount != 1)
                return Fail("second SendFailureAndClose queued another packet");
            close.Send(GameServer.Text("", 0, -1, 0, "", "nope"));
            if (close.PendingCount != 1)
                return Fail("Send after Closing still queued");

            byte[] extraHello = HelloBody(Settings.BuildVersion, Manager.NexusId, "nouser", "nopass");
            GameServer.Read(close, (int)GameServer.PacketId.Hello, extraHello);
            if (close.PendingCount != 1 || close.State != ProtocolState.Handshaked)
                return Fail("Read processed a packet after Closing");
            Program.Print(PrintType.Info, "P7 verify: SendFailureAndClose queues Failure(2), stays connected, Read/Send ignored");

            Client mismatch = new Client(new SendState(), new ReceiveState());
            mismatch.State = ProtocolState.Handshaked;
            mismatch.Active = true;
            mismatch.Id = 7002;
            mismatch.IP = "p8-verify";
            byte[] badVersion = HelloBody("0.0.1", Manager.NexusId, "nouser", "nopass");
            using (PacketReader rdr = new PacketReader(new MemoryStream(badVersion)))
                GameServer.Hello(mismatch, rdr);
            if (mismatch.State == ProtocolState.Disconnected)
                return Fail("version mismatch disconnected immediately");
            if (!mismatch.Closing)
                return Fail("version mismatch did not set Closing");
            if (!PeekFailure(mismatch, GameServer.FailureIncorrectVersion, Settings.BuildVersion, "version mismatch"))
                return false;
            Program.Print(PrintType.Info, "P8 verify: Hello with 0.0.1 sends Failure(1, BuildVersion) and waits to flush");

            Client badLogin = new Client(new SendState(), new ReceiveState());
            badLogin.State = ProtocolState.Handshaked;
            badLogin.Active = true;
            badLogin.Id = 7003;
            badLogin.IP = "p7-verify";
            byte[] badCreds = HelloBody(Settings.BuildVersion, Manager.NexusId, "nouser", "nopass");
            using (PacketReader rdr = new PacketReader(new MemoryStream(badCreds)))
                GameServer.Hello(badLogin, rdr);
            if (badLogin.State == ProtocolState.Disconnected)
                return Fail("invalid account disconnected immediately");
            if (!PeekFailure(badLogin, GameServer.FailureForceCloseGame, "Invalid account.", "invalid account"))
                return false;
            Program.Print(PrintType.Info, "P7 verify: Hello with matching version + bad password sends Failure(2, Invalid account.)");
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
