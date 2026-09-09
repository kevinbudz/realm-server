//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates
{
    public class MapRender
    {
        protected BitmapRasterizer<DungeonTile> Rasterizer { get; private set; }
        protected DungeonGraph Graph { get; private set; }
        protected Random Rand { get; private set; }

        internal void Init(BitmapRasterizer<DungeonTile> rasterizer, DungeonGraph graph, Random rand)
        {
            Rasterizer = rasterizer;
            Graph = graph;
            Rand = rand;
        }

        public virtual void Rasterize()
        {
        }
    }
}
