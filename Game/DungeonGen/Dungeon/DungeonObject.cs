//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.DungeonGen.Dungeon
{
    public struct ObjectType
    {
        public readonly uint Id;
        public readonly string Name;

        public ObjectType(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public static bool operator ==(ObjectType a, ObjectType b)
        {
            return a.Id == b.Id || a.Name == b.Name;
        }

        public static bool operator !=(ObjectType a, ObjectType b)
        {
            return a.Id != b.Id && a.Name != b.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectType && (ObjectType)obj == this;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class DungeonObject
    {
        public ObjectType ObjectType;
        public KeyValuePair<string, string>[] Attributes = Empty<KeyValuePair<string, string>>.Array;

        //Object spawn config in the map "key:val;..." form the world loader
        //understands. (The reference has no usable serializer here — its
        //Wmap bridge calls the default Object.ToString — so attributes from
        //template maps would be lost; emitting the config keeps e.g. the
        //"size:85" markers working.)
        public string ToCfgString()
        {
            if (Attributes == null || Attributes.Length == 0)
                return null;
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> attr in Attributes)
            {
                if (string.IsNullOrEmpty(attr.Key))
                    continue;
                sb.Append(attr.Key);
                sb.Append(':');
                sb.Append(attr.Value);
                sb.Append(';');
            }
            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}
