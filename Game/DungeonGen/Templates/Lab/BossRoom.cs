//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.Lab
{
    internal class BossRoom : FixedRoom
    {
        private static readonly Rect _template = new Rect(0, 0, 24, 50);

        public override RoomType Type { get { return RoomType.Target; } }

        public override int Width { get { return _template.MaxX - _template.X; } }

        public override int Height { get { return _template.MaxY - _template.Y; } }

        private static readonly Tuple<Direction, int>[] _connections = {
            Tuple.Create(Direction.South, 10)
        };

        public override Tuple<Direction, int>[] ConnectionPoints { get { return _connections; } }

        public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand)
        {
            rasterizer.Copy(LabTemplate.MapTemplate, _template, Pos);
            LabTemplate.DrawSpiderWeb(rasterizer, Bounds, rand);
        }
    }
}
