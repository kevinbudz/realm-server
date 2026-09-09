//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.Lab
{
    internal class Corridor : MapCorridor
    {
        public override void Rasterize(Room src, Room dst, Point srcPos, Point dstPos)
        {
            Default(srcPos, dstPos, new DungeonTile
            {
                TileType = LabTemplate.LabFloor
            });
        }
    }
}
