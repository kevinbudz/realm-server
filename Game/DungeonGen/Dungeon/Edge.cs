//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using System.Diagnostics;

namespace RotMG.Game.DungeonGen.Dungeon
{
    public enum Direction
    {
        South = 0,
        East = 1,
        North = 2,
        West = 3
    }

    public struct Link
    {
        public readonly Direction Direction;
        public readonly int Offset;

        public Link(Direction direction, int offset)
        {
            Direction = direction;
            Offset = offset;
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", Direction, Offset);
        }
    }

    public class Edge
    {
        private Edge()
        {
        }

        public Room RoomA { get; private set; }
        public Room RoomB { get; private set; }
        public Link Linkage { get; set; }

        public static void Link(Room a, Room b, Link link)
        {
            Debug.Assert(a != b);
            Edge edge = new Edge
            {
                RoomA = a,
                RoomB = b,
                Linkage = link
            };
            a.Edges.Add(edge);
            b.Edges.Add(edge);
        }

        public static void UnLink(Room a, Room b)
        {
            Edge edge = null;
            foreach (Edge ed in a.Edges)
                if (ed.RoomA == b || ed.RoomB == b)
                {
                    edge = ed;
                    break;
                }
            if (edge == null)
                throw new ArgumentException();
            a.Edges.Remove(edge);
            b.Edges.Remove(edge);
        }
    }
}
