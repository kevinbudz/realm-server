using System;
using System.IO;

namespace RotMG.Networking
{
    //[W06] Adapters for callers that still hold a stream-backed PacketReader: the verify flags and W05Verify build one
    //over a MemoryStream. Each copies the unread bytes and runs the span handler, so the handler body exists once.
    //Packet dispatch (GameServer.Read) never goes through these; it passes a SpanReader directly.
    public static partial class GameServer
    {
        private static SpanReader SpanOf(PacketReader rdr, out byte[] body)
        {
            MemoryStream ms = (MemoryStream)rdr.BaseStream;
            byte[] all = ms.ToArray();
            int pos = (int)ms.Position;
            body = new byte[all.Length - pos];
            Buffer.BlockCopy(all, pos, body, 0, body.Length);
            return new SpanReader(body);
        }

        public static void Teleport(Client client, PacketReader rdr)
        {
            SpanReader span = SpanOf(rdr, out _);
            Teleport(client, ref span);
        }

        public static void Buy(Client client, PacketReader rdr)
        {
            SpanReader span = SpanOf(rdr, out _);
            Buy(client, ref span);
        }

        public static void Hello(Client client, PacketReader rdr)
        {
            SpanReader span = SpanOf(rdr, out _);
            Hello(client, ref span);
        }

        public static void EditAccountList(Client client, PacketReader rdr)
        {
            SpanReader span = SpanOf(rdr, out _);
            EditAccountList(client, ref span);
        }

        public static void UsePortal(Client client, PacketReader rdr)
        {
            SpanReader span = SpanOf(rdr, out _);
            UsePortal(client, ref span);
        }
    }
}
