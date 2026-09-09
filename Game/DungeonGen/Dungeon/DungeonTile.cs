//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;

namespace RotMG.Game.DungeonGen.Dungeon
{
    public struct TileType
    {
        public readonly uint Id;
        public readonly string Name;

        public TileType(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public static bool operator ==(TileType a, TileType b)
        {
            return a.Id == b.Id || a.Name == b.Name;
        }

        public static bool operator !=(TileType a, TileType b)
        {
            return a.Id != b.Id && a.Name != b.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is TileType && (TileType)obj == this;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public struct DungeonTile
    {
        public TileType TileType;
        public string Region;
        public DungeonObject Object;
    }
}
