//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;

namespace RotMG.Game.DungeonGen.Dungeon
{
    public abstract class FixedRoom : Room
    {
        public abstract Tuple<Direction, int>[] ConnectionPoints { get; }

        public override Range NumBranches { get { return new Range(1, ConnectionPoints.Length); } }
    }
}
