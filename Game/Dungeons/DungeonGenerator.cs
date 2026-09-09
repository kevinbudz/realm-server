using RotMG.Common;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Dungeons
{
    //Room-and-corridor dungeon generator written for this project. It backs
    //the dungeons without a reference template; Abyss of Demons, Mad Lab
    //and Pirate Cave generate through the realm-src-master DungeonGen shell
    //in Game/DungeonGen (see DungeonWorld).
    public class DungeonGenerator
    {
        public struct Rect
        {
            public int X, Y, W, H;
            public int CX { get { return X + W / 2; } }
            public int CY { get { return Y + H / 2; } }
        }

        private readonly Random _rand;

        public bool[,] Floor;
        public List<Rect> Rooms = new List<Rect>();
        public int Width;
        public int Height;
        public IntPoint Entrance;
        public IntPoint BossRoom;

        public DungeonGenerator(int seed)
        {
            _rand = new Random(seed);
        }

        public void Generate(int width, int height, int roomCount)
        {
            Width = width;
            Height = height;
            Floor = new bool[width, height];
            Rooms.Clear();

            for (int attempt = 0; attempt < roomCount * 40 && Rooms.Count < roomCount; attempt++)
            {
                int w = _rand.Next(5, 12);
                int h = _rand.Next(5, 12);
                int x = _rand.Next(2, Math.Max(3, width - w - 2));
                int y = _rand.Next(2, Math.Max(3, height - h - 2));
                Rect room = new Rect { X = x, Y = y, W = w, H = h };

                bool overlaps = false;
                foreach (Rect other in Rooms)
                {
                    if (room.X < other.X + other.W + 1 && room.X + room.W + 1 > other.X &&
                        room.Y < other.Y + other.H + 1 && room.Y + room.H + 1 > other.Y)
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (!overlaps)
                    Rooms.Add(room);
            }

            //Guarantee at least entrance + boss room.
            if (Rooms.Count == 0)
                Rooms.Add(new Rect { X = width / 2 - 4, Y = height / 2 - 4, W = 8, H = 8 });
            if (Rooms.Count == 1)
            {
                Rect first = Rooms[0];
                Rect second = new Rect
                {
                    X = Math.Min(width - 9, Math.Max(2, first.X + 14)),
                    Y = Math.Min(height - 9, Math.Max(2, first.Y)),
                    W = 7,
                    H = 7
                };
                Rooms.Add(second);
            }

            foreach (Rect room in Rooms)
                CarveRoom(room);
            for (int i = 1; i < Rooms.Count; i++)
                CarveCorridor(Rooms[i - 1].CX, Rooms[i - 1].CY, Rooms[i].CX, Rooms[i].CY);

            Entrance = new IntPoint(Rooms[0].CX, Rooms[0].CY);
            BossRoom = new IntPoint(Rooms[Rooms.Count - 1].CX, Rooms[Rooms.Count - 1].CY);
        }

        private void CarveRoom(Rect room)
        {
            for (int x = room.X; x < room.X + room.W; x++)
                for (int y = room.Y; y < room.Y + room.H; y++)
                    Floor[x, y] = true;
        }

        private void CarveCorridor(int x0, int y0, int x1, int y1)
        {
            int x = x0;
            int y = y0;
            while (x != x1)
            {
                CarveDisc(x, y);
                x += Math.Sign(x1 - x);
            }
            while (y != y1)
            {
                CarveDisc(x, y);
                y += Math.Sign(y1 - y);
            }
            CarveDisc(x, y);
        }

        private void CarveDisc(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px > 0 && py > 0 && px < Width - 1 && py < Height - 1)
                        Floor[px, py] = true;
                }
        }

        public bool IsFloor(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height && Floor[x, y];
        }
    }
}
