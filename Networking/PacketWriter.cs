using System;
using System.Buffers.Binary;
using System.Text;

namespace RotMG.Networking
{
    //[W06] Big-endian packet writer over a per-thread scratch buffer (replaces the BinaryWriter over MemoryStream).
    //
    //Rent() hands out a ref struct handle to the thread's scratch, so passing it around costs nothing. Builders return
    //RentedBytes(): a span over the scratch that is valid until the next Rent() on the same thread. Client.Send copies
    //it into a pooled frame at once, so the usual "build, then send" needs no array. Anything kept, or used after another
    //builder has run on this thread, must be copied with ToArray(), because that builder rewinds the same scratch.
    //Builders that can run while another span is still being sent (Client.BeginSlowDisconnect) use RentDetached().
    //
    //Wire rules are the old writer's, byte for byte: ints/shorts/uints/ushorts/floats big-endian; bool one byte;
    //string = u16 BE UTF-8 byte count + bytes; WriteUTF32 = i32 BE of the char count + raw UTF-8 (kept as is).
    //Long, ulong and double keep BinaryWriter's little-endian order, because the old class did not override them.
    //The string length is bounds-checked: over 65535 UTF-8 bytes throws instead of silently wrapping the prefix.
    public ref struct PacketWriter
    {
        private const int MaxStringBytes = ushort.MaxValue;

        private sealed class Scratch
        {
            public byte[] Buf = new byte[1024];
            public int Length;
        }

        [ThreadStatic]
        private static Scratch _scratch;

        private readonly Scratch _s;

        private PacketWriter(Scratch s)
        {
            _s = s;
        }

        public static PacketWriter Rent()
        {
            Scratch s = _scratch ??= new Scratch();
            s.Length = 0;
            return new PacketWriter(s);
        }

        //A writer with its own buffer, for builders that must not touch the thread's scratch. Read it with ToArray().
        public static PacketWriter RentDetached()
        {
            return new PacketWriter(new Scratch());
        }

        //Span over the bytes written since Rent(). Valid until the next Rent() on this thread.
        public static ReadOnlySpan<byte> RentedBytes()
        {
            Scratch s = _scratch;
            return s == null ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>(s.Buf, 0, s.Length);
        }

        public byte[] ToArray()
        {
            return _s.Buf.AsSpan(0, _s.Length).ToArray();
        }

        public int Length => _s.Length;

        //Reserves n bytes at the end and returns them. Grows to the required size, or double the old size if that is larger.
        private Span<byte> Reserve(int n)
        {
            Scratch s = _s;
            int required = s.Length + n;
            if (required > s.Buf.Length)
            {
                byte[] grown = new byte[Math.Max(required, s.Buf.Length * 2)];
                Buffer.BlockCopy(s.Buf, 0, grown, 0, s.Length);
                s.Buf = grown;
            }
            Span<byte> span = s.Buf.AsSpan(s.Length, n);
            s.Length = required;
            return span;
        }

        public void Write(byte value)
        {
            Reserve(1)[0] = value;
        }

        public void Write(sbyte value)
        {
            Reserve(1)[0] = (byte)value;
        }

        public void Write(bool value)
        {
            Reserve(1)[0] = value ? (byte)1 : (byte)0;
        }

        public void Write(short value)
        {
            BinaryPrimitives.WriteInt16BigEndian(Reserve(2), value);
        }

        public void Write(ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(Reserve(2), value);
        }

        public void Write(int value)
        {
            BinaryPrimitives.WriteInt32BigEndian(Reserve(4), value);
        }

        public void Write(uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(Reserve(4), value);
        }

        public void Write(float value)
        {
            BinaryPrimitives.WriteInt32BigEndian(Reserve(4), BitConverter.SingleToInt32Bits(value));
        }

        //Same as BinaryWriter (little-endian): the old class never overrode these.
        public void Write(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(Reserve(8), value);
        }

        public void Write(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(Reserve(8), value);
        }

        public void Write(double value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(Reserve(8), BitConverter.DoubleToInt64Bits(value));
        }

        public void Write(byte[] value)
        {
            Write((ReadOnlySpan<byte>)value);
        }

        public void Write(ReadOnlySpan<byte> value)
        {
            value.CopyTo(Reserve(value.Length));
        }

        //u16 BE byte count + UTF-8, the same as the old Write(string). A null string throws ArgumentNullException,
        //as Encoding.UTF8.GetBytes(null) did before.
        public void Write(string value)
        {
            int count = Encoding.UTF8.GetByteCount(value);
            if (count > MaxStringBytes)
                throw new ArgumentOutOfRangeException(nameof(value), $"string is {count} UTF-8 bytes; the packet limit is {MaxStringBytes}");
            Span<byte> dst = Reserve(2 + count);
            BinaryPrimitives.WriteUInt16BigEndian(dst, (ushort)count);
            Encoding.UTF8.GetBytes(value.AsSpan(), dst.Slice(2));
        }

        //Kept as the old writer had it: i32 BE of the char count, then the raw UTF-8 bytes (no length prefix on the bytes).
        public void WriteUTF32(string value)
        {
            Write(value.Length);
            Encoding.UTF8.GetBytes(value.AsSpan(), Reserve(Encoding.UTF8.GetByteCount(value)));
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
