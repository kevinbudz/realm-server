using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace RotMG.Networking
{
    //[W06] A length prefix is negative or runs past the end of the frame. Read disconnects the client for it (the frame
    //is malformed). A short fixed-size field throws EndOfStreamException instead, and Read only drops that packet.
    public sealed class MalformedPacketException : Exception
    {
        public MalformedPacketException(string message) : base(message) { }
    }

    //[W06] Big-endian reader over a span (the frame body in the receive path). A ref struct, so it never allocates and
    //never copies the body. Handlers take it as `ref SpanReader rdr`, so the cursor advances for the caller too.
    //
    //  - A fixed-size read past the end throws EndOfStreamException (Read drops the packet, as before).
    //  - A length-prefixed read (ReadString, ReadUTF32, ReadBytes) with a negative length, or a length past the end of
    //    the frame, throws MalformedPacketException (Read disconnects that client). The old PacketReader cut such a
    //    string short and went on; the W06 ledger row asks for this stricter rule.
    //  - ReadString/ReadUTF32 decode UTF-8; ReadSingle is a big-endian float.
    //ReadBytes allocates only the bytes it returns.
    public ref struct SpanReader
    {
        private readonly ReadOnlySpan<byte> _buf;
        private int _pos;

        public SpanReader(ReadOnlySpan<byte> body)
        {
            _buf = body;
            _pos = 0;
        }

        public int Position => _pos;
        public int Remaining => _buf.Length - _pos;

        //True while bytes remain (PacketReader's PeekChar() != -1 check).
        public bool HasMore => _pos < _buf.Length;

        private ReadOnlySpan<byte> Take(int n)
        {
            if (n > _buf.Length - _pos)
                throw new EndOfStreamException();
            ReadOnlySpan<byte> s = _buf.Slice(_pos, n);
            _pos += n;
            return s;
        }

        //Length-prefixed bytes: the count must be non-negative and fit in what is left of the frame.
        private ReadOnlySpan<byte> TakeCounted(int count)
        {
            if (count < 0)
                throw new MalformedPacketException($"negative length {count}");
            if (count > _buf.Length - _pos)
                throw new MalformedPacketException($"length {count} runs past the {_buf.Length - _pos} bytes left");
            ReadOnlySpan<byte> s = _buf.Slice(_pos, count);
            _pos += count;
            return s;
        }

        public byte ReadByte()
        {
            if (_pos >= _buf.Length)
                throw new EndOfStreamException();
            return _buf[_pos++];
        }

        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public short ReadInt16()
        {
            return BinaryPrimitives.ReadInt16BigEndian(Take(2));
        }

        public ushort ReadUInt16()
        {
            return BinaryPrimitives.ReadUInt16BigEndian(Take(2));
        }

        public int ReadInt32()
        {
            return BinaryPrimitives.ReadInt32BigEndian(Take(4));
        }

        public float ReadSingle()
        {
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(Take(4)));
        }

        //i16 BE length (signed, as PacketReader.ReadBytes saw it), then the bytes.
        public byte[] ReadBytes(int count)
        {
            return TakeCounted(count).ToArray();
        }

        //u16 BE length, then UTF-8 (PacketReader.ReadString). The length is read signed, so 0x8000 and up are negative.
        public string ReadString()
        {
            return Encoding.UTF8.GetString(TakeCounted(ReadInt16()));
        }

        //i32 BE length, then UTF-8 (PacketReader.ReadUTF32).
        public string ReadUTF32()
        {
            return Encoding.UTF8.GetString(TakeCounted(ReadInt32()));
        }
    }
}
