using System;

namespace RotMG.Game
{
    //[W08] Serialized ObjectStatus bytes of one entity for one broadcast phase (Entity.SharedStatus). An entity holds one
    //blob per form only while the phase that filled it runs: World.ReleaseWireBlobs returns them to a per-world pool at the
    //end of the phase. So the memory is bounded by one tick's working set, not by every entity ever sent.
    internal sealed class WireBlob
    {
        public long Gen;
        public byte[] Buf = Array.Empty<byte>();
        public int Len;
    }
}
