//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System.Collections.Generic;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen
{
    public class RoomCollision
    {
        private const int GridScale = 3;
        private const int GridSize = 1 << GridScale;

        private struct RoomKey
        {
            public readonly int XKey;
            public readonly int YKey;

            public RoomKey(int x, int y)
            {
                XKey = x >> GridScale;
                YKey = y >> GridScale;
            }

            public override int GetHashCode()
            {
                return XKey * 7 + YKey;
            }
        }

        private readonly Dictionary<RoomKey, HashSet<Room>> _rooms = new Dictionary<RoomKey, HashSet<Room>>();

        private void Add(int x, int y, Room rm)
        {
            RoomKey key = new RoomKey(x, y);
            HashSet<Room> roomList = _rooms.GetValueOrCreate(key, k => new HashSet<Room>());
            roomList.Add(rm);
        }

        public void Add(Room rm)
        {
            Rect bounds = rm.Bounds;
            int x = bounds.X, y = bounds.Y;
            for (; y <= bounds.MaxY + GridSize; y += GridSize)
            {
                for (x = bounds.X; x <= bounds.MaxX + 20; x += GridSize)
                    Add(x, y, rm);
            }
        }

        private void Remove(int x, int y, Room rm)
        {
            RoomKey key = new RoomKey(x, y);
            HashSet<Room> roomList;
            if (_rooms.TryGetValue(key, out roomList))
                roomList.Remove(rm);
        }

        public void Remove(Room rm)
        {
            Rect bounds = rm.Bounds;
            int x = bounds.X, y = bounds.Y;
            for (; y <= bounds.MaxY + GridSize; y += GridSize)
            {
                for (x = bounds.X; x <= bounds.MaxX + 20; x += GridSize)
                    Remove(x, y, rm);
            }
        }

        private bool HitTest(int x, int y, Rect bounds)
        {
            RoomKey key = new RoomKey(x, y);
            HashSet<Room> roomList = _rooms.GetValueOrDefault(key, (HashSet<Room>)null);
            if (roomList != null)
            {
                foreach (Room room in roomList)
                    if (!room.Bounds.Intersection(bounds).IsEmpty)
                        return true;
            }
            return false;
        }

        public bool HitTest(Room rm)
        {
            Rect bounds = new Rect(rm.Bounds.X - 1, rm.Bounds.Y - 1, rm.Bounds.MaxX + 1, rm.Bounds.MaxY + 1);

            int x = bounds.X, y = bounds.Y;
            for (; y <= bounds.MaxY + GridSize; y += GridSize)
            {
                for (x = bounds.X; x <= bounds.MaxX + GridSize; x += GridSize)
                {
                    if (HitTest(x, y, bounds))
                        return true;
                }
            }
            return false;
        }
    }
}
