using System;

namespace RotMG.Game
{
    //[W08] Per-player "sent at the tile's current version" flags, one bit per world tile (index y * Width + x). Replaces
    //the per-player Dictionary<long,int> of last-sent UpdateCount values, which was never pruned. The bit array is sized
    //to the world once, so the state does not grow as the player explores.
    //Invariant (kept by World.BumpTile, which runs on every tile version change): bit set <=> the player was sent the
    //tile at its current UpdateCount. A version change clears the bit for every player in the world.
    public sealed class TileSeenBits
    {
        private readonly ulong[] _bits;
        private readonly int _width;
        private readonly int _height;

        public TileSeenBits(int width, int height)
        {
            _width = width;
            _height = height;
            _bits = new ulong[((long)width * height + 63) / 64];
        }

        public long ByteSize => _bits.Length * 8L;

        public bool Get(int x, int y)
        {
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                return false;
            int i = y * _width + x;
            return (_bits[i >> 6] & (1UL << (i & 63))) != 0;
        }

        public void Set(int x, int y)
        {
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                return;
            int i = y * _width + x;
            _bits[i >> 6] |= 1UL << (i & 63);
        }

        public void Clear(int x, int y)
        {
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                return;
            int i = y * _width + x;
            _bits[i >> 6] &= ~(1UL << (i & 63));
        }
    }
}
