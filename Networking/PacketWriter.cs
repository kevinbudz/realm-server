using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RotMG.Networking
{
    public class PacketWriter : BinaryWriter
    {
        public PacketWriter(Stream input) : base(input, Encoding.UTF8) { }

        public override void Write(short value)
        {
            base.Write(IPAddress.NetworkToHostOrder(value));
        }

        public override void Write(ushort value)
        {
            base.Write((ushort)IPAddress.HostToNetworkOrder((short)value));
        }

        public override void Write(int value)
        {
            base.Write(IPAddress.NetworkToHostOrder(value));
        }

        public override void Write(uint value)
        {
            base.Write((uint)IPAddress.NetworkToHostOrder((int)value));
        }

        public override void Write(float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            base.Write((byte)(bits >> 24));
            base.Write((byte)(bits >> 16));
            base.Write((byte)(bits >> 8));
            base.Write((byte)bits);
        }

        [ThreadStatic]
        private static MemoryStream _scratchStream;
        [ThreadStatic]
        private static PacketWriter _scratchWriter;

        //Rents a PacketWriter over a per-thread reusable MemoryStream (no per-packet
        //stream allocation). Not re-entrant: copy RentedBytes() before Rent() again.
        public static PacketWriter Rent()
        {
            if (_scratchStream == null)
            {
                _scratchStream = new MemoryStream(512);
                _scratchWriter = new PacketWriter(_scratchStream);
            }
            else
            {
                _scratchStream.SetLength(0);
                _scratchStream.Position = 0;
            }
            return _scratchWriter;
        }

        public static byte[] RentedBytes()
        {
            return _scratchStream.ToArray();
        }

        public override void Write(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            Write((short)data.Length);
            base.Write(data);
        }

        public void WriteUTF32(string value)
        {
            Write(value.Length);
            Write(Encoding.UTF8.GetBytes(value));
        }

        public void WriteNullTerminatedString(string str)
        {
            Write(Encoding.UTF8.GetBytes(str));
            Write((byte)0);
        }

        public static void BlockCopyInt32(byte[] data, int int32)
        {
            data[0] = (byte)(int32 >> 24);
            data[1] = (byte)(int32 >> 16);
            data[2] = (byte)(int32 >> 8);
            data[3] = (byte)int32;
        }
    }
}
