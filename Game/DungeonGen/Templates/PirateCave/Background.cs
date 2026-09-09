//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.PirateCave
{
    internal class Background : MapRender
    {
        public override void Rasterize()
        {
            DungeonTile tile = new DungeonTile
            {
                TileType = PirateCaveTemplate.ShallowWater
            };

            Rasterizer.Clear(tile);
        }
    }
}
