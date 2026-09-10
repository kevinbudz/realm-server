using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
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
        private Queue<byte[]> _pending;
        private SendState _send;
        private ReceiveState _receive;

        public Client(SendState send, ReceiveState receive)
        {
            _pending = new Queue<byte[]>();
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
            //Save synchronously before the socket closes. These used to be
            //queued onto the main loop's work queue, so a crash (or the
            //shutdown path, which stops that loop first) silently dropped
            //them and rolled the character back to its previous save while
            //trade/vault counterparties kept their copies: a duplication
            //machine. A disconnect-time write is two small rows; the tick it
            //costs is worth the durability.
            if (Account != null)
            {
                Account.Connected = false;
                Manager.AccountIdToClientId.Remove(Account.Id);

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

            Manager.AddClient(this);
        }

        public void Tick()
        {
            try
            {
                if (!_socket.Connected)
                {
                    Disconnect();
                    return;
                }

                StartReceive();
                StartSend();
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

        public void Send(byte[] packet)
        {
            if (_pending.Count >= MaxPendingPackets)
            {
                if (_pending.Count >= MaxPendingDisconnect)
                {
                    Disconnect(); //Client is not draining; drop it instead of growing without bound.
                    return;
                }
                _pending.TryDequeue(out _); //Drop the stalest packet to make room for fresh state.
            }
            _pending.Enqueue(packet);
        }

        private void StartReceive()
        {
            switch (_receive.State)
            {
                case SocketEventState.Awaiting:
                    if (_socket.Available >= GameServer.PrefixLength)
                    {
                        _socket.Receive(_receive.PacketBytes, GameServer.PrefixLength, SocketFlags.None);
                        _receive.PacketLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_receive.PacketBytes, 0));
                        _receive.State = SocketEventState.InProgress;
                        StartReceive();
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
                        Disconnect();
                        return;
                    }

                    //Only loop when a full packet was actually consumed: a
                    //fragmented packet must wait for the next tick, otherwise
                    //this recurses with no progress until the stack overflows.
                    if ((_socket.Available + GameServer.PrefixLength) < _receive.PacketLength)
                        break;

                    if (_socket.Available != 0)
                        _socket.Receive(_receive.PacketBytes, GameServer.PrefixLength, _receive.PacketLength - GameServer.PrefixLength, SocketFlags.None);
                    GameServer.Read(this, _receive.GetPacketId(), _receive.GetPacketBody());
                    _receive.Reset();

                    StartReceive();
                    break;
            }
        }

        private void StartSend()
        {
            //One coalesced flush per tick: frame everything queued into the pooled
            //buffer and push it with a single socket send. The socket is non-blocking
            //(see BeginHandling), so a full kernel buffer just defers the remainder
            //to a later tick instead of stalling the tick thread.
            if (_send.State == SocketEventState.Awaiting)
            {
                if (_pending.Count == 0)
                    return;

                _send.EnsureBuffer();
                byte[] buf = _send.Data;
                int total = 0;
                int queued = _pending.Count;
                while (queued-- > 0)
                {
                    byte[] packet = _pending.Peek();
                    int framed = packet.Length + GameServer.PrefixLengthWithId;
                    if (total + framed > buf.Length)
                    {
                        if (total == 0)
                        {
                            //Single packet larger than the pooled buffer: rent an
                            //exact-size buffer for this flush (returned on Reset).
                            _send.Grow(framed);
                            buf = _send.Data;
                        }
                        else break;
                    }
                    _pending.Dequeue();
                    int length = packet.Length + GameServer.PrefixLengthWithId;
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
