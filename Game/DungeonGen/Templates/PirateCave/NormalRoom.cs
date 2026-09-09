//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.PirateCave
{
    internal class NormalRoom : Room
    {
        private readonly int _w;
        private readonly int _h;

        public NormalRoom(int w, int h)
        {
            _w = w;
            _h = h;
        }

        public override RoomType Type { get { return RoomType.Normal; } }

        public override int Width { get { return _w; } }

        public override int Height { get { return _h; } }

        public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand)
        {
            rasterizer.FillRect(Bounds, new DungeonTile
            {
                TileType = PirateCaveTemplate.BrownLines
            });

            int numBoss = new Range(0, 1).Random(rand);
            int numMinion = new Range(3, 5).Random(rand);
            int numPet = new Range(0, 2).Random(rand);

            DungeonTile[,] buf = rasterizer.Bitmap;
            Rect bounds = Bounds;
            while (numBoss > 0 || numMinion > 0 || numPet > 0)
            {
                int x = rand.Next(bounds.X, bounds.MaxX);
                int y = rand.Next(bounds.Y, bounds.MaxY);
                if (buf[x, y].Object != null)
                    continue;

                switch (rand.Next(3))
                {
                    case 0:
                        if (numBoss > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = PirateCaveTemplate.Boss[rand.Next(PirateCaveTemplate.Boss.Length)]
                            };
                            numBoss--;
                        }
                        break;
                    case 1:
                        if (numMinion > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = PirateCaveTemplate.Minion[rand.Next(PirateCaveTemplate.Minion.Length)]
                            };
                            numMinion--;
                        }
                        break;
                    case 2:
                        if (numPet > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = PirateCaveTemplate.Pet[rand.Next(PirateCaveTemplate.Pet.Length)]
                            };
                            numPet--;
                        }
                        break;
                }
            }
        }
    }
}
