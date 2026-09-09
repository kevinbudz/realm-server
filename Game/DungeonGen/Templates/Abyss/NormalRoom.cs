//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.Abyss
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
                TileType = AbyssTemplate.RedSmallChecks
            });

            int numImp = new Range(0, 2).Random(rand);
            int numDemon = new Range(2, 4).Random(rand);
            int numBrute = new Range(1, 4).Random(rand);
            int numSkull = new Range(1, 3).Random(rand);

            DungeonTile[,] buf = rasterizer.Bitmap;
            Rect bounds = Bounds;
            while (numImp > 0 || numDemon > 0 || numBrute > 0 || numSkull > 0)
            {
                int x = rand.Next(bounds.X, bounds.MaxX);
                int y = rand.Next(bounds.Y, bounds.MaxY);
                if (buf[x, y].Object != null)
                    continue;

                switch (rand.Next(4))
                {
                    case 0:
                        if (numImp > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = AbyssTemplate.AbyssImp
                            };
                            numImp--;
                        }
                        break;
                    case 1:
                        if (numDemon > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = AbyssTemplate.AbyssDemon[rand.Next(AbyssTemplate.AbyssDemon.Length)]
                            };
                            numDemon--;
                        }
                        break;
                    case 2:
                        if (numBrute > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = AbyssTemplate.AbyssBrute[rand.Next(AbyssTemplate.AbyssBrute.Length)]
                            };
                            numBrute--;
                        }
                        break;
                    case 3:
                        if (numSkull > 0)
                        {
                            buf[x, y].Object = new DungeonObject
                            {
                                ObjectType = AbyssTemplate.AbyssBones
                            };
                            numSkull--;
                        }
                        break;
                }
            }
        }
    }
}
