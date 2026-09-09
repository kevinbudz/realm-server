//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates
{
    public static class Extensions
    {
        public static void Copy<TPixel>(this BitmapRasterizer<TPixel> self, TPixel[,] src, Rect srcRect, Point dst,
            Func<TPixel, bool> transparent = null)
            where TPixel : struct
        {
            int w = srcRect.MaxX - srcRect.X;
            int h = srcRect.MaxY - srcRect.Y;
            TPixel[,] buf = self.Bitmap;

            if (transparent == null)
                transparent = pix => false;

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    TPixel pix = src[x + srcRect.X, y + srcRect.Y];
                    if (transparent(pix))
                        continue;
                    buf[x + dst.X, y + dst.Y] = src[x + srcRect.X, y + srcRect.Y];
                }
        }

        public static Direction Reverse(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Direction.South;
                case Direction.South:
                    return Direction.North;
                case Direction.East:
                    return Direction.West;
                case Direction.West:
                    return Direction.East;
            }
            throw new ArgumentException();
        }
    }
}
