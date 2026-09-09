//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.Abyss
{
    internal class StartRoom : Room
    {
        private readonly int _len;
        internal Point portalPos;

        public StartRoom(int len)
        {
            _len = len;
        }

        public override RoomType Type { get { return RoomType.Start; } }

        public override int Width { get { return _len; } }

        public override int Height { get { return _len; } }

        public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand)
        {
            rasterizer.FillRect(Bounds, new DungeonTile
            {
                TileType = AbyssTemplate.RedSmallChecks
            });

            DungeonTile[,] buf = rasterizer.Bitmap;
            Rect bounds = Bounds;

            bool portalPlaced = false;
            while (!portalPlaced)
            {
                int x = rand.Next(bounds.X + 2, bounds.MaxX - 4);
                int y = rand.Next(bounds.Y + 2, bounds.MaxY - 4);
                if (buf[x, y].Object != null)
                    continue;

                buf[x, y].Region = "Spawn";
                buf[x, y].Object = new DungeonObject
                {
                    ObjectType = AbyssTemplate.CowardicePortal
                };
                portalPos = new Point(x, y);
                portalPlaced = true;
            }
        }
    }
}
