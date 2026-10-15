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
    //thread. Socket completions only frame; GameServer.Read stays tick-threaded
    //so game logic never runs concurrently.
    public struct InboundPacket
    {
        public int Id;
        public byte[] Body; //pooled; null for an empty body. The tick thread returns it after dispatch.
        public int Length;

        public ReadOnlySpan<byte> Span => Body == null ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>(Body, 0, Length);
    }

    //One outbound frame waiting for the send chain (StageLocked). Data is a pooled array holding the whole frame: the 4-byte BE
    //length prefix, then the packet id and body. Length is the frame size. The send chain returns Data to ArrayPool
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
        private List<(int End, int Bit)> _hopMarks; //[hop] send chain only (under _ioLock)
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
        //sockets whose first bytes never arrive.
        public const int HandshakeTimeoutMs = 5000;
        //First 4 bytes of "<policy-file-request/>" as a big-endian int.
        private const int PolicyFileMagic = 1014001516;
        private Socket _socket;
        //Concurrent: parallel world broadcast enqueues from worker threads
        //while the send chain dequeues under _ioLock.
        private ConcurrentQueue<OutgoingPacket> _pending;
        private int _pendingBytes;
        private int _droppedCosmetic;
        private int _lastQueueDebugMs;
        //Set when the send budget is exhausted: further Send() no-ops,
        //Failure is staged in _disconnectFlush so it leaves before close.
        private volatile int _slowDisconnect;
        private byte[] _disconnectFlush;
        //Framed inbound packets (receive completions produce, tick thread consumes)
        //plus a death flag set by a completion when the socket dies.
        private readonly ConcurrentQueue<InboundPacket> _inbound = new ConcurrentQueue<InboundPacket>();
        private volatile bool _socketDead;
        //Packet that fit-checked but couldn't flush; only the send chain
        //touches this (concurrent queues have no push-front).
        private OutgoingPacket _held;
        private bool _hasHeld;
        //Legacy overflow flag still consumed by Tick. Slow-link disconnects
        //now go through _slowDisconnect so Failure can flush before close.
        private volatile bool _sendOverflow;
        private volatile bool _disconnectRequested;
        private volatile string _disconnectReason;
        private volatile bool _closing;
        //Shared by the send chain and receive completions (threadpool) and FinishDisconnect
        //(main thread): socket close and pooled-buffer return never race
        //a mid-copy flush.
        private readonly object _ioLock = new object();
        private SendState _send;
        private ReceiveState _receive;
        //[W10] Socket-event state. Fresh SAEAs per connection (BeginHandling). _recvInFlight/_sendInFlight are guarded
        //by _ioLock. _sendKick is the send-chain token (KickSend/ReleaseSendChain). _teardownClaim makes FinishDisconnect
        //run once per connection.
        private SocketAsyncEventArgs _recvArgs;
        private SocketAsyncEventArgs _sendArgs;
        private bool _recvInFlight;
        private bool _sendInFlight;
        private int _sendKick;
        private int _teardownClaim;
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
        //buffer a send is still reading.
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
            if (Interlocked.Exchange(ref _teardownClaim, 1) != 0) //[W10] idempotent: one teardown per connection
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
                TeardownSendLocked(); //[W10] returns the send buffer unless a SendAsync is still reading it
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
            try
            {
                socket.Blocking = false;
                //Nagle + Linux delayed ACK added ~40 ms between back-to-back small writes (e.g. CreateSuccess then the first Update).
                //Each send flush is coalesced into one write, so NoDelay only affects the rest.
                socket.NoDelay = true;
            }
            catch (Exception)
            {
                //[W10] The peer is already gone. Nothing is registered yet, so release the IP and the pooled slot here.
                GameServer.CloseSocket(socket);
                GameServer.ReleaseIp(ip);
                GameServer.AddBack(this);
                return;
            }

            lock (_ioLock)
            {
                _socket = socket;
                CaptureId = PacketCapture.NextConnection(); //[W06]
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
                _receive.Reset();
                _send.Reset();
                //[W10] New socket-event objects per connection, so a completion from an earlier connection cannot match them.
                _recvArgs = new SocketAsyncEventArgs();
                _recvArgs.Completed += OnReceiveCompleted;
                _sendArgs = new SocketAsyncEventArgs();
                _sendArgs.Completed += OnSendCompleted;
                Volatile.Write(ref _teardownClaim, 0);
            }

            Manager.AddClient(this);
            if (HopTrace.Enabled) HopTrace.Mark(HopAccount, "BeginHandling on main thread"); //[hop]

            lock (_ioLock)
                ReceiveLoopLocked(); //[W10] the first read is armed once the client is registered
        }

        //Tick-thread half: no socket calls here. Receive completions frame
        //inbound packets and flushes outbound ones; this only dispatches
        //framed packets to handlers and acts on death flags.
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
            KickSend(); //[W10] the send chain writes it on the threadpool, never the caller
#if DEBUG
            LogSendQueueIfStalled();
#endif
        }

        //Queue Failure, ignore further inbound, close after the send chain
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
        //StageLocked writes it first instead of behind up to 4 MiB.
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
            KickSend(); //[W10] Failure goes out now, not on the next flush

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

        //[W10] Socket-event state, all guarded by _ioLock. A connection has at most one read and one send in flight.
        //IoQuiescent tells the accept side when a pooled client's buffers are free to reuse.
        public bool IoQuiescent
        {
            get
            {
                lock (_ioLock)
                    return !_recvInFlight && !_sendInFlight;
            }
        }

        //[W10] Receive chain. One ReceiveAsync is in flight per connection, into _receive.PacketBytes from Filled to the end
        //of the buffer. A completion appends the bytes, frames every complete packet into the inbound queue (the tick thread
        //dispatches them, as before), keeps the partial tail at the front, and arms the next read. Everything runs under
        //_ioLock, so Disconnect and a completion never touch the buffer at the same time.
        //Framing is validated inside the try in ConsumeReceiveLocked: a bad length marks this client dead, and no other.
        private bool CanReceiveLocked()
        {
            return !_socketDead && _socket != null && State != ProtocolState.Disconnected;
        }

        //Lock held. Arms reads until one is pending. A read that completes synchronously is consumed here.
        private void ReceiveLoopLocked()
        {
            SocketAsyncEventArgs args = _recvArgs;
            while (CanReceiveLocked())
            {
                int room = GameServer.BufferSize - _receive.Filled;
                if (room <= 0)
                {
                    //Cannot happen: a full buffer always holds a complete packet, and FrameReceivedLocked consumes it.
                    _socketDead = true;
                    return;
                }
                args.SetBuffer(_receive.PacketBytes, _receive.Filled, room);
                _recvInFlight = true;
                bool pending;
                try
                {
                    pending = _socket.ReceiveAsync(args);
                }
                catch (Exception)
                {
                    //ObjectDisposed or SocketException: the connection is gone.
                    _recvInFlight = false;
                    _socketDead = true;
                    return;
                }
                if (pending)
                    return;
                _recvInFlight = false;
                if (!ConsumeReceiveLocked(args))
                    return;
            }
        }

        //Receive completion (threadpool). A completion from an earlier connection is ignored.
        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            //SocketAsyncEventArgs completions pass the Socket as sender, so the SAEA is matched by the argument (e).
            lock (_ioLock)
            {
                if (!ReferenceEquals(e, _recvArgs))
                    return;
                _recvInFlight = false;
                if (!CanReceiveLocked())
                    return; //closed by Disconnect (the read was aborted) or already dead: drop the bytes
                try
                {
                    if (ConsumeReceiveLocked(e))
                        ReceiveLoopLocked();
                }
                catch (Exception)
                {
                    _socketDead = true;
                }
            }
        }

        //Lock held. True when reading should continue. A FIN (0 bytes) or a socket error ends the connection.
        private bool ConsumeReceiveLocked(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred <= 0)
            {
                _socketDead = true;
                return false;
            }
            _receive.Filled += e.BytesTransferred;
            try
            {
                FrameReceivedLocked();
            }
            catch (Exception)
            {
                _socketDead = true; //malformed framing, or a failed policy reply
            }
            return !_socketDead;
        }

        //Lock held. Frames every complete packet in the buffer, then keeps the partial tail at the front. The layout is the
        //same as before: 4-byte big-endian length (counting itself), packet id, body. A header equal to the policy-request
        //magic is answered, as before.
        private void FrameReceivedLocked()
        {
            byte[] buf = _receive.PacketBytes;
            int end = _receive.Filled;
            int pos = 0;
            while (end - pos >= GameServer.PrefixLength)
            {
                int length = BinaryPrimitives.ReadInt32BigEndian(new ReadOnlySpan<byte>(buf, pos, 4));
                if (length == PolicyFileMagic)
                {
                    ServePolicyLocked();
                    return;
                }
                if (length < GameServer.PrefixLength || length > GameServer.BufferSize)
                {
                    _socketDead = true;
                    return;
                }
                if (end - pos < length)
                    break;

                //[W06] The body is copied once into a pooled array: the tick thread dispatches it later, after this
                //thread has moved on to the next frame in PacketBytes. The tick thread returns the array after dispatch.
                int id = buf[pos + GameServer.PrefixLengthWithId];
                int bodyLength = length - GameServer.PrefixLength;
                byte[] body = null;
                if (bodyLength > 0)
                {
                    body = ArrayPool<byte>.Shared.Rent(bodyLength);
                    Buffer.BlockCopy(buf, pos + GameServer.PrefixLength, body, 0, bodyLength);
                }
                if (PacketCapture.Enabled) PacketCapture.Record(CaptureId, PacketCapture.DirInbound, (byte)id, new ReadOnlySpan<byte>(buf, pos + GameServer.PrefixLength, bodyLength)); //[W06]
                _inbound.Enqueue(new InboundPacket { Id = id, Body = body, Length = bodyLength });
                pos += length;
            }

            int rest = end - pos;
            if (pos > 0 && rest > 0)
                Buffer.BlockCopy(buf, pos, buf, 0, rest);
            _receive.Filled = rest;
        }

        //Lock held. Sends the policy file, drains what the peer sent so the close does not reset the connection, and marks
        //the client dead. Same sequence as the pre-W10 IO thread.
        private void ServePolicyLocked()
        {
            _socket.Send(GameServer.PolicyFile, 0, GameServer.PolicyFile.Length, SocketFlags.None);
            DrainReceiveBuffer();
            _receive.Reset();
            _socketDead = true;
        }

        //Lock held. Reads and discards whatever is already in the kernel buffer.
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

        //Send chain only (under _ioLock).
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

        //[hop] Send chain (under _ioLock): a watched packet was copied into the send buffer.
        //Its end offset is timestamped once the kernel accepts that many bytes.
        private void HopNoteBuffered(ReadOnlySpan<byte> packet, int end)
        {
            int bit = HopTrace.WatchBitFor(packet);
            if (bit == 0 || (Volatile.Read(ref HopMask) & bit) == 0)
                return;
            Interlocked.And(ref HopMask, ~bit);
            (_hopMarks ??= new List<(int End, int Bit)>()).Add((end, bit));
        }

        //[hop] Send chain (under _ioLock): sentUpTo buffer bytes reached the kernel.
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

        //[W10] Send chain. Send() queues the frame and calls KickSend, which runs the chain on the threadpool. The caller never
        //makes the syscall, so the tick thread stays off the socket, as it was when the IO thread owned sends. At most one
        //SendAsync is in flight per connection. _sendKick is the chain token: a kick takes it, and it goes back only when the
        //chain has nothing left to send. A completion keeps the token while it continues the chain, so one party at a time
        //stages or sends. Staging (coalescing) is the old FlushSendInner, so the bytes on the wire are unchanged.
        private static readonly WaitCallback RunSendChainCallback = state => ((Client)state).RunSendChain();

        private void KickSend()
        {
            if (_socket == null || State == ProtocolState.Disconnected)
                return;
            if (Interlocked.CompareExchange(ref _sendKick, 1, 0) != 0)
                return;
            ThreadPool.UnsafeQueueUserWorkItem(RunSendChainCallback, this);
        }

        //Threadpool entry. The kick has taken the token.
        private void RunSendChain()
        {
            bool pending = false;
            try
            {
                lock (_ioLock)
                    pending = DriveSendLocked();
            }
            catch (Exception)
            {
                lock (_ioLock)
                    _socketDead = true;
            }
            if (!pending)
                ReleaseSendChain();
        }

        //Token holder, nothing in flight. Gives the token back, or keeps it while work remains.
        private void ReleaseSendChain()
        {
            while (true)
            {
                Interlocked.Exchange(ref _sendKick, 0);
                if (!HasSendWork())
                    return;
                if (Interlocked.CompareExchange(ref _sendKick, 1, 0) != 0)
                    return; //a kick took the token between the release and this retake
                bool pending;
                try
                {
                    lock (_ioLock)
                        pending = DriveSendLocked();
                }
                catch (Exception)
                {
                    lock (_ioLock)
                        _socketDead = true;
                    Interlocked.Exchange(ref _sendKick, 0);
                    return;
                }
                if (pending)
                    return; //the in-flight send's completion now owns the token
            }
        }

        //Work exists only on a connected socket.
        private bool HasSendWork()
        {
            return _socket != null && (!_pending.IsEmpty || Volatile.Read(ref _disconnectFlush) != null);
        }

        //SendAsync completion (threadpool). A completion from an earlier connection is ignored.
        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            bool release = false;
            //SocketAsyncEventArgs completions pass the Socket as sender, so the SAEA is matched by the argument (e).
            lock (_ioLock)
            {
                if (!ReferenceEquals(e, _sendArgs))
                    return;
                try
                {
                    _sendInFlight = false;
                    _send.MarkIdle();
                    bool pending = ConsumeSendLocked(e) && DriveSendLocked();
                    if (State == ProtocolState.Disconnected)
                        TeardownSendLocked(); //[W10] the abort completed: nothing is in flight now, return the buffer
                    release = !pending;
                }
                catch (Exception)
                {
                    _socketDead = true;
                    release = true;
                }
            }
            if (release)
                ReleaseSendChain();
        }

        //Lock held, token held. Returns true while a SendAsync is in flight (its completion continues the chain). Otherwise the
        //caller gives the token back.
        private bool DriveSendLocked()
        {
            while (true)
            {
                if (_sendInFlight)
                    return true;
                if (_socket == null || _sendArgs == null)
                    return false;
                //Only a disconnect stops the chain here. A peer FIN (_socketDead) still flushes what is queued, as the IO thread
                //did before; a real send error ends the chain through the SendAsync catch or ConsumeSendLocked.
                if (State == ProtocolState.Disconnected)
                {
                    DrainSendLocked();
                    return false;
                }
                if (_send.BytesWritten >= _send.PacketLength)
                {
                    _send.Reset();
                    if (!StageLocked())
                        return false;
                }

                _sendArgs.SetBuffer(_send.Data, _send.BytesWritten, _send.PacketLength - _send.BytesWritten);
                _sendInFlight = true;
                _send.MarkInUse();
                bool pending;
                try
                {
                    pending = _socket.SendAsync(_sendArgs);
                }
                catch (Exception)
                {
                    _sendInFlight = false;
                    _send.MarkIdle();
                    _socketDead = true;
                    DrainSendLocked();
                    return false;
                }
                if (pending)
                    return true;

                _sendInFlight = false;
                _send.MarkIdle();
                if (!ConsumeSendLocked(_sendArgs))
                {
                    DrainSendLocked();
                    return false;
                }
            }
        }

        //Lock held, nothing in flight. Coalesces queued frames (and a pending Failure-first flush) into the pooled send buffer.
        //Returns false when there is nothing to send. Frames are coalesced in queue order and capped at the buffer size, as before.
        private bool StageLocked()
        {
            byte[] disconnect = Interlocked.Exchange(ref _disconnectFlush, null);
            if (disconnect == null && !_hasHeld && _pending.IsEmpty)
                return false;

            _send.EnsureBuffer();
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
                return false;

            _send.PacketLength = total;
            _send.BytesWritten = 0;
            _send.State = SocketEventState.InProgress;
            return true;
        }

        //Lock held, a send just completed or failed. False when the connection failed (the caller marks it dead).
        private bool ConsumeSendLocked(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred <= 0)
            {
                _socketDead = true;
                return false;
            }
            _send.BytesWritten += e.BytesTransferred;
            if (HopTrace.Enabled) HopWritten(_send.BytesWritten); //[hop]
            return true;
        }

        //Lock held, nothing in flight: the connection is dead or disconnected, so drop what was queued.
        private void DrainSendLocked()
        {
            ReleaseHeld();
            DrainPending();
            Interlocked.Exchange(ref _disconnectFlush, null);
            Interlocked.Exchange(ref _pendingBytes, 0);
            TeardownSendLocked();
        }

        //Lock held. Returns the pooled send buffer. While a SendAsync is in flight the kernel may still be reading it, so this
        //does nothing: the completion calls it again once nothing is in flight.
        private void TeardownSendLocked()
        {
            if (_sendInFlight)
                return;
            _send.Reset();
            _send.ReturnPooledBuffer();
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
