//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Templates;

namespace RotMG.Game.DungeonGen.Dungeon
{
    public class DungeonGraph
    {
        public DungeonTemplate Template { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Room[] Rooms { get; private set; }

        internal DungeonGraph(DungeonTemplate template, Room[] rooms)
        {
            Template = template;

            int dx = int.MaxValue, dy = int.MaxValue;
            int mx = int.MinValue, my = int.MinValue;

            for (int i = 0; i < rooms.Length; i++)
            {
                Rect bounds = rooms[i].Bounds;

                if (bounds.X < dx)
                    dx = bounds.X;
                if (bounds.Y < dy)
                    dy = bounds.Y;

                if (bounds.MaxX > mx)
                    mx = bounds.MaxX;
                if (bounds.MaxY > my)
                    my = bounds.MaxY;
            }

            const int Pad = 4;

            Width = mx - dx + Pad * 2;
            Height = my - dy + Pad * 2;

            for (int i = 0; i < rooms.Length; i++)
            {
                Room room = rooms[i];
                Point pos = room.Pos;
                room.Pos = new Point(pos.X - dx + Pad, pos.Y - dy + Pad);

                foreach (Edge edge in room.Edges)
                {
                    if (edge.RoomA != room)
                        continue;
                    if (edge.Linkage.Direction == Direction.South || edge.Linkage.Direction == Direction.North)
                        edge.Linkage = new Link(edge.Linkage.Direction, edge.Linkage.Offset - dx + Pad);
                    else if (edge.Linkage.Direction == Direction.East || edge.Linkage.Direction == Direction.West)
                        edge.Linkage = new Link(edge.Linkage.Direction, edge.Linkage.Offset - dy + Pad);
                }
            }
            Rooms = rooms;
        }
    }
}
