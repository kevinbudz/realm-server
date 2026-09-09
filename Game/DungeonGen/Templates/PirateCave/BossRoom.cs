//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.PirateCave
{
    internal class BossRoom : Room
    {
        private readonly int _radius;

        public BossRoom(int radius)
        {
            _radius = radius;
        }

        public override RoomType Type { get { return RoomType.Target; } }

        public override int Width { get { return _radius * 2 + 1; } }

        public override int Height { get { return _radius * 2 + 1; } }

        public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand)
        {
            DungeonTile tile = new DungeonTile
            {
                TileType = PirateCaveTemplate.BrownLines
            };

            double cX = Pos.X + _radius + 0.5;
            double cY = Pos.Y + _radius + 0.5;
            Rect bounds = Bounds;
            int r2 = _radius * _radius;
            DungeonTile[,] buf = rasterizer.Bitmap;

            for (int x = bounds.X; x < bounds.MaxX; x++)
                for (int y = bounds.Y; y < bounds.MaxY; y++)
                {
                    if ((x - cX) * (x - cX) + (y - cY) * (y - cY) <= r2)
                        buf[x, y] = tile;
                }

            int numKing = 1;
            int numBoss = new Range(4, 7).Random(rand);
            int numMinion = new Range(4, 7).Random(rand);

            r2 = (_radius - 2) * (_radius - 2);
            while (numKing > 0 || numBoss > 0 || numMinion > 0)
            {
                int x = rand.Next(bounds.X, bounds.MaxX);
                int y = rand.Next(bounds.Y, bounds.MaxY);

                if ((x - cX) * (x - cX) + (y - cY) * (y - cY) > r2)
                    continue;

                if (buf[x, y].Object != null || buf[x, y].TileType != PirateCaveTemplate.BrownLines)
                    continue;

                switch (rand.Next(3))
                {
                    case 0:
                        if (numKing > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = PirateCaveTemplate.PirateKing
                            };
                            numKing--;
                        }
                        break;
                    case 1:
                        if (numBoss > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = PirateCaveTemplate.Boss[rand.Next(PirateCaveTemplate.Boss.Length)]
                            };
                            numBoss--;
                        }
                        break;
                    case 2:
                        if (numMinion > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = PirateCaveTemplate.Minion[rand.Next(PirateCaveTemplate.Minion.Length)]
                            };
                            numMinion--;
                        }
                        break;
                }
            }
        }
    }
}
