//[W06] Env-gated wire tap for the byte-identical capture gate (tools/hop/pktdiff.py decodes it).
//REALM_PKTCAP=<file> records every framed game packet the server reads or writes, per connection and in order.
//File: magic "PKTCAP1\n", then one record per frame: u32 LE conn, u8 dir ('I' inbound, 'O' outbound),
//u32 LE length, then the frame bytes (packet id + body, without the 4-byte length prefix).
//Records are written from the IO thread only (frame read / frame staged for the socket). Unset, Enabled is
//false, NextConnection returns 0 and Record returns at once.
using System;
using System.IO;
using System.Threading;

namespace RotMG.Common
{
    public static class PacketCapture
    {
        public const byte DirInbound = (byte)'I';
        public const byte DirOutbound = (byte)'O';

        public static readonly bool Enabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REALM_PKTCAP"));

        private static readonly object _gate = new object();
        private static FileStream _file; //unbuffered: every record reaches the file before a SIGTERM can end the process
        private static int _lastConn;

        //Connection sequence number, assigned in BeginHandling (main thread, accept order). 0 = not captured.
        public static int NextConnection()
        {
            if (!Enabled)
                return 0;
            return Interlocked.Increment(ref _lastConn);
        }

        //id is the packet id byte, body the rest of the frame (the frame is id + body, without the length prefix).
        public static void Record(int conn, byte dir, byte id, ReadOnlySpan<byte> body)
        {
            if (!Enabled || conn == 0)
                return;
            lock (_gate)
            {
                if (_file == null)
                    Open();
                int length = body.Length + 1;
                byte[] head = new byte[9];
                head[0] = (byte)conn;
                head[1] = (byte)(conn >> 8);
                head[2] = (byte)(conn >> 16);
                head[3] = (byte)(conn >> 24);
                head[4] = dir;
                head[5] = (byte)length;
                head[6] = (byte)(length >> 8);
                head[7] = (byte)(length >> 16);
                head[8] = (byte)(length >> 24);
                _file.Write(head, 0, head.Length);
                _file.WriteByte(id);
                _file.Write(body);
            }
        }

        private static void Open()
        {
            string path = Environment.GetEnvironmentVariable("REALM_PKTCAP");
            _file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 1);
            byte[] magic = System.Text.Encoding.ASCII.GetBytes("PKTCAP1\n");
            _file.Write(magic, 0, magic.Length);
        }
    }
}
