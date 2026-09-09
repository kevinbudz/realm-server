//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using System.Linq;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.Lab
{
    internal class NormalRoom : FixedRoom
    {
        [Flags]
        internal enum RoomFlags
        {
            Evil = 1,

            ConnectionMask = 6,
            Conn_Floor = 0,
            Conn_None = 2,
            Conn_Destructible = 4
        }

        private struct RoomTemplate
        {
            public readonly Rect Bounds;
            public readonly Range NumBranches;
            public readonly RoomFlags Flags;
            public readonly Tuple<Direction, int>[] Connections;

            public RoomTemplate(Rect bounds, Range numBranches, RoomFlags flags, params Tuple<Direction, int>[] connections)
            {
                Bounds = bounds;
                Flags = flags;
                NumBranches = numBranches;
                Connections = connections;
            }
        }

        private static Rect Rect(int x, int y, int w, int h)
        {
            return new Rect(x, y, x + w, y + h);
        }

        private static readonly RoomTemplate[] _roomTemplates = {
            new RoomTemplate(Rect(24, 0, 26, 24),
                new Range(1, 4), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 11),
                Tuple.Create(Direction.South, 11),
                Tuple.Create(Direction.East, 10),
                Tuple.Create(Direction.West, 10)
                ),
            new RoomTemplate(Rect(50, 0, 16, 12),
                new Range(2, 2), RoomFlags.Conn_Destructible,
                Tuple.Create(Direction.East, 1),
                Tuple.Create(Direction.West, 7)
                ),
            new RoomTemplate(Rect(66, 0, 25, 12),
                new Range(1, 2), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 4),
                Tuple.Create(Direction.North, 17),
                Tuple.Create(Direction.South, 4),
                Tuple.Create(Direction.South, 17),
                Tuple.Create(Direction.East, 4),
                Tuple.Create(Direction.West, 4)
                ),
            new RoomTemplate(Rect(24, 24, 21, 20),
                new Range(1, 4), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 8),
                Tuple.Create(Direction.South, 9),
                Tuple.Create(Direction.East, 8),
                Tuple.Create(Direction.West, 9)
                ),
            new RoomTemplate(Rect(50, 12, 18, 27),
                new Range(1, 2), RoomFlags.Conn_Destructible,
                Tuple.Create(Direction.North, 7),
                Tuple.Create(Direction.South, 7)
                ),
            new RoomTemplate(Rect(68, 12, 22, 31),
                new Range(2, 3), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 4),
                Tuple.Create(Direction.South, 4),
                Tuple.Create(Direction.East, 13)
                ),
            new RoomTemplate(Rect(0, 50, 40, 22),
                new Range(1, 2), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.East, 9),
                Tuple.Create(Direction.West, 9)
                ),
            new RoomTemplate(Rect(40, 44, 25, 25),
                new Range(2, 4), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 4),
                Tuple.Create(Direction.North, 17),
                Tuple.Create(Direction.South, 4),
                Tuple.Create(Direction.South, 17),
                Tuple.Create(Direction.East, 4),
                Tuple.Create(Direction.East, 17),
                Tuple.Create(Direction.West, 4),
                Tuple.Create(Direction.West, 17)
                ),
            new RoomTemplate(Rect(65, 43, 32, 23),
                new Range(1, 3), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.South, 14),
                Tuple.Create(Direction.East, 6),
                Tuple.Create(Direction.West, 6)
                ),
            new RoomTemplate(Rect(0, 72, 24, 24),
                new Range(1, 2), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.South, 13),
                Tuple.Create(Direction.West, 6)
                ),
            new RoomTemplate(Rect(24, 72, 22, 19),
                new Range(1, 3), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 2),
                Tuple.Create(Direction.South, 5),
                Tuple.Create(Direction.East, 14)
                ),
            new RoomTemplate(Rect(46, 69, 42, 50),
                new Range(2, 2), RoomFlags.Evil | RoomFlags.Conn_Destructible,
                Tuple.Create(Direction.North, 19),
                Tuple.Create(Direction.South, 19)
                ),
            new RoomTemplate(Rect(0, 128, 31, 31),
                new Range(2, 4), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 13),
                Tuple.Create(Direction.South, 13),
                Tuple.Create(Direction.East, 13),
                Tuple.Create(Direction.West, 13)
                ),
            new RoomTemplate(Rect(31, 119, 21, 32),
                new Range(1, 2), RoomFlags.Conn_Destructible,
                Tuple.Create(Direction.North, 15),
                Tuple.Create(Direction.East, 26)
                ),
            new RoomTemplate(Rect(52, 119, 25, 12),
                new Range(1, 2), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 4),
                Tuple.Create(Direction.North, 17),
                Tuple.Create(Direction.South, 4),
                Tuple.Create(Direction.South, 17),
                Tuple.Create(Direction.East, 4),
                Tuple.Create(Direction.West, 4)
                ),
            new RoomTemplate(Rect(77, 119, 20, 13),
                new Range(1, 2), RoomFlags.Conn_Destructible,
                Tuple.Create(Direction.East, 5),
                Tuple.Create(Direction.West, 5)
                ),
            new RoomTemplate(Rect(52, 132, 28, 20),
                new Range(1, 3), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 3),
                Tuple.Create(Direction.South, 3),
                Tuple.Create(Direction.East, 8)
                ),
            new RoomTemplate(Rect(0, 159, 28, 32),
                new Range(2, 2), RoomFlags.Evil | RoomFlags.Conn_Destructible,
                Tuple.Create(Direction.South, 4),
                Tuple.Create(Direction.West, 3)
                ),
            new RoomTemplate(Rect(32, 152, 32, 21),
                new Range(1, 2), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.North, 14),
                Tuple.Create(Direction.South, 14)
                ),
            new RoomTemplate(Rect(30, 173, 25, 24),
                new Range(1, 2), RoomFlags.Conn_Floor,
                Tuple.Create(Direction.East, 10),
                Tuple.Create(Direction.West, 10)
                ),
            new RoomTemplate(Rect(65, 152, 21, 29),
                new Range(2, 2), RoomFlags.Evil | RoomFlags.Conn_Destructible,
                Tuple.Create(Direction.North, 8),
                Tuple.Create(Direction.South, 8)
                )
        };

        private readonly int _currentId;
        private RoomTemplate _current;

        private static readonly DungeonObject _destWall = new DungeonObject
        {
            ObjectType = LabTemplate.DestructibleWall
        };

        public NormalRoom(NormalRoom prev, Random rand, bool noEvil)
        {
            System.Collections.Generic.List<int> indexes = Enumerable.Range(0, _roomTemplates.Length).ToList();
            rand.Shuffle(indexes);
            foreach (int index in indexes)
            {
                if (prev != null && index == prev._currentId)
                    continue;

                if ((_roomTemplates[index].Flags & RoomFlags.Evil) != 0 && noEvil)
                    continue;

                if (prev != null)
                {
                    bool ok = false;
                    foreach (Tuple<Direction, int> conn in prev.ConnectionPoints)
                    {
                        Direction d = conn.Item1.Reverse();
                        if (_roomTemplates[index].Connections.Any(targetConn => targetConn.Item1 == d))
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok)
                        continue;
                }

                _currentId = index;
            }
            _current = _roomTemplates[_currentId];
        }

        public override RoomType Type { get { return RoomType.Normal; } }

        public override int Width { get { return _current.Bounds.MaxX - _current.Bounds.X; } }

        public override int Height { get { return _current.Bounds.MaxY - _current.Bounds.Y; } }

        public override Tuple<Direction, int>[] ConnectionPoints { get { return _current.Connections; } }

        public override Range NumBranches { get { return _current.NumBranches; } }

        public RoomFlags Flags { get { return _current.Flags; } }

        public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand)
        {
            rasterizer.Copy(LabTemplate.MapTemplate, _current.Bounds, Pos);

            if ((_current.Flags & RoomFlags.Evil) == 0)
                LabTemplate.CreateEnemies(rasterizer, Bounds, rand);

            RoomFlags flags = _current.Flags & RoomFlags.ConnectionMask;
            DungeonTile? tile = null;
            switch (flags)
            {
                case RoomFlags.Conn_Floor:
                    tile = new DungeonTile
                    {
                        TileType = LabTemplate.LabFloor
                    };
                    break;

                case RoomFlags.Conn_Destructible:
                    tile = new DungeonTile
                    {
                        TileType = LabTemplate.LabFloor,
                        Object = _destWall
                    };
                    break;
                default:
                    return;
            }

            foreach (Edge edge in Edges)
            {
                Direction direction = edge.Linkage.Direction;
                if (edge.RoomA != this)
                    direction = direction.Reverse();

                Point a, b;
                switch (direction)
                {
                    case Direction.South:
                        a = new Point(edge.Linkage.Offset, Pos.Y + Height - 1);
                        b = new Point(a.X + 3, a.Y);
                        break;

                    case Direction.North:
                        a = new Point(edge.Linkage.Offset, Pos.Y);
                        b = new Point(a.X + 3, a.Y);
                        break;

                    case Direction.East:
                        a = new Point(Pos.X + Width - 1, edge.Linkage.Offset);
                        b = new Point(a.X, a.Y + 3);
                        break;

                    case Direction.West:
                        a = new Point(Pos.X, edge.Linkage.Offset);
                        b = new Point(a.X, a.Y + 3);
                        break;

                    default:
                        throw new ArgumentException();
                }
                rasterizer.DrawLine(a, b, tile.Value);
            }

            LabTemplate.DrawSpiderWeb(rasterizer, Bounds, rand);
        }
    }
}
