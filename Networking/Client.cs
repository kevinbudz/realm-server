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
        public ProtocolState State;
        public int Id;
        public int TargetWorldId;
        public string IP;

        public AccountModel Account;
        public CharacterModel Character;
        public Player Player;
        public wRandom Random;
        public bool Active; //Used in escape to stop incoming packets (so you don't die)
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
        //on in Tick. Disconnect itself stays main-thread-only: it touches
        //Manager dicts, the DB and the socket.
        private volatile bool _sendOverflow;
        private SendState _send;
        private ReceiveState _receive;

        public Client(SendState send, ReceiveState receive)
        {
            _pending = new ConcurrentQueue<byte[]>();
            _send = send;
            _receive = receive;
        }

        public void Disconnect() //Disconnects, clears all individual client data and pushes the instance back to the server queue.
        {
            if (State == ProtocolState.Disconnected)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Already dcd");
#endif
                return;
            }
#if DEBUG
            try
            {
                Program.Print(PrintType.Debug, $"Disconnecting client from <{_socket.RemoteEndPoint}>");
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
                CharacterModel ch = Character;
                if (Player != null && Player.Parent != null)
                {
                    Player.SaveToCharacter();
                    Player.Parent.RemoveEntity(Player);
                    bool dead = ch == null || ch.Dead; //Already saved during death.
                    try
                    {
                        if (dead)
                            acc.Save();
                        else if (ch != null)
                            Database.SaveAccountAndCharacter(acc, ch);
                        else
                            acc.Save();
                    }
                    catch { }
                }
                else
                {
                    try { acc.Save(); }
                    catch { }
                }
            }

            //Shutdown socket
            State = ProtocolState.Disconnected;

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

            //Clear data 
            Active = false;
            _send.Reset();
            _receive.Reset();
            _pending.Clear();
            while (_inbound.TryDequeue(out _)) { }
            _socketDead = false;
            Account = null;
            Player = null;
            Character = null;
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
            DCTime = -1;
            _socketDead = false;
            _sendOverflow = false;
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
                if (_sendOverflow)
                {
                    _sendOverflow = false;
                    Disconnect();
                    return;
                }

                if (_socketDead || !_socket.Connected)
                {
                    Disconnect();
                    return;
                }

                DrainInbound();
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
            if (State == ProtocolState.Disconnected)
                return;
            try
            {
                FlushSendInner();
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

        private void FlushSendInner()
        {
            if (_send.State == SocketEventState.Awaiting)
            {
                if (_held == null && _pending.IsEmpty)
                    return;

                _send.EnsureBuffer();
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
