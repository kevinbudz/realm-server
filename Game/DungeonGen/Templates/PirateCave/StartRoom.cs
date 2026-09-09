//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.PirateCave
{
    internal class StartRoom : Room
    {
        private readonly int _radius;

        public StartRoom(int radius)
        {
            _radius = radius;
        }

        public override RoomType Type { get { return RoomType.Start; } }

        public override int Width { get { return _radius * 2 + 1; } }

        public override int Height { get { return _radius * 2 + 1; } }

        public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand)
        {
            DungeonTile tile = new DungeonTile
            {
                TileType = PirateCaveTemplate.LightSand
            };

            double cX = Pos.X + _radius + 0.5;
            double cY = Pos.Y + _radius + 0.5;
            Rect bounds = Bounds;
            int r2 = _radius * _radius;
            DungeonTile[,] buf = rasterizer.Bitmap;

            double pR = rand.NextDouble() * (_radius - 2), pA = rand.NextDouble() * 2 * Math.PI;
            int pX = (int)(cX + Math.Cos(pR) * pR);
            int pY = (int)(cY + Math.Sin(pR) * pR);

            for (int x = bounds.X; x < bounds.MaxX; x++)
                for (int y = bounds.Y; y < bounds.MaxY; y++)
                {
                    if ((x - cX) * (x - cX) + (y - cY) * (y - cY) <= r2)
                    {
                        buf[x, y] = tile;
                        if (rand.NextDouble() > 0.95)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = PirateCaveTemplate.PalmTree
                            };
                        }
                    }
                    if (x == pX && y == pY)
                    {
                        buf[x, y].Region = "Spawn";
                        buf[x, y].Object = new DungeonObject
                        {
                            ObjectType = PirateCaveTemplate.CowardicePortal
                        };
                    }
                }
        }
    }
}
