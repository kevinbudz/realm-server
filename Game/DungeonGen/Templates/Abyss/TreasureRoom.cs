//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.Abyss
{
    internal class TreasureRoom : FixedRoom
    {
        public override RoomType Type { get { return RoomType.Special; } }

        public override int Width { get { return 15; } }

        public override int Height { get { return 21; } }

        private static readonly Tuple<Direction, int>[] _connections = {
            Tuple.Create(Direction.South, 6)
        };

        public override Tuple<Direction, int>[] ConnectionPoints { get { return _connections; } }

        public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand)
        {
            rasterizer.Copy(AbyssTemplate.MapTemplate, new Rect(70, 10, 85, 31), Pos, tile => tile.TileType.Name == "Space");

            Rect bounds = Bounds;
            DungeonTile[,] buf = rasterizer.Bitmap;
            for (int x = bounds.X; x < bounds.MaxX; x++)
                for (int y = bounds.Y; y < bounds.MaxY; y++)
                {
                    if (buf[x, y].TileType != AbyssTemplate.Space)
                        buf[x, y].Region = "Treasure";
                }
        }
    }
}
