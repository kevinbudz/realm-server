using RotMG.Common;
using RotMG.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Dungeons
{
    //Instanced dungeon built from a DungeonDef by DungeonGenerator, then
    //populated with that dungeon's behavior roster. Entry points: dungeon
    //portals in the realm (see Oryx portal upkeep) and locked-portal keys
    //(see Player AEUnlockPortal).
    public class DungeonWorld : World
    {
        public DungeonDef Def;
        public Entity Boss;

        public DungeonWorld(DungeonDef def, int seed)
            : base(BuildMap(def, seed, out List<DungeonGenerator.Rect> rooms, out IntPoint entrance), def.Name, def.Name, 0)
        {
            Def = def;
            Populate(rooms, entrance, seed);
        }

        private static JSMap BuildMap(DungeonDef def, int seed, out List<DungeonGenerator.Rect> rooms, out IntPoint entrance)
        {
            DungeonGenerator gen = new DungeonGenerator(seed);
            gen.Generate(def.Width, def.Height, def.Rooms);
            rooms = gen.Rooms;
            entrance = gen.Entrance;

            TileDesc rockDesc = ResolveGround(def.RockGround, "Dirt");
            TileDesc[] floorDescs = new TileDesc[def.Grounds.Length];
            for (int i = 0; i < def.Grounds.Length; i++)
                floorDescs[i] = ResolveGround(def.Grounds[i], "Dirt");

            ushort wallType = ResolveWall(def.Walls);
            Random rand = new Random(seed + 1);

            JSMap map = new JSMap(def.Width, def.Height);
            for (int x = 0; x < def.Width; x++)
                for (int y = 0; y < def.Height; y++)
                {
                    JSTile tile = map.Tiles[x, y];
                    if (gen.IsFloor(x, y))
                    {
                        tile.GroundType = floorDescs[rand.Next(floorDescs.Length)].Type;
                        tile.ObjectType = 0xff;
                    }
                    else
                    {
                        tile.GroundType = rockDesc.Type;
                        tile.ObjectType = 0xff;
                    }
                    tile.Region = Region.None;
                    map.Tiles[x, y] = tile;
                }

            //Outline rock-adjacent-to-floor tiles with wall objects.
            if (wallType != 0xffff)
            {
                for (int x = 0; x < def.Width; x++)
                    for (int y = 0; y < def.Height; y++)
                    {
                        if (gen.IsFloor(x, y))
                            continue;
                        bool adjacent = false;
                        for (int dx = -1; dx <= 1 && !adjacent; dx++)
                            for (int dy = -1; dy <= 1 && !adjacent; dy++)
                                if (gen.IsFloor(x + dx, y + dy))
                                    adjacent = true;
                        if (!adjacent)
                            continue;
                        JSTile tile = map.Tiles[x, y];
                        tile.ObjectType = wallType;
                        map.Tiles[x, y] = tile;
                    }
            }

            //Stamp the spawn region on the entrance room.
            foreach (DungeonGenerator.Rect room in rooms)
            {
                Region region = room.CX == entrance.X && room.CY == entrance.Y ? Region.Spawn : Region.None;
                if (region == Region.None)
                    continue;
                for (int x = room.X; x < room.X + room.W; x++)
                    for (int y = room.Y; y < room.Y + room.H; y++)
                    {
                        JSTile tile = map.Tiles[x, y];
                        tile.Region = region;
                        map.Tiles[x, y] = tile;
                    }
            }

            map.InitRegions();
            return map;
        }

        private static TileDesc ResolveGround(string name, string fallback)
        {
            TileDesc desc;
            if (!string.IsNullOrWhiteSpace(name) && Resources.Id2Tile.TryGetValue(name, out desc))
                return desc;
            if (Resources.Id2Tile.TryGetValue(fallback, out desc))
                return desc;
            return Resources.Type2Tile.Values.First();
        }

        private static ushort ResolveWall(string[] candidates)
        {
            if (candidates != null)
                foreach (string name in candidates)
                {
                    ObjectDesc desc;
                    if (Resources.Id2Object.TryGetValue(name, out desc))
                        return desc.Type;
                }
            return 0xffff;
        }

        private void Populate(List<DungeonGenerator.Rect> rooms, IntPoint entrance, int seed)
        {
            Random rand = new Random(seed + 2);

            List<ushort> mobTypes = new List<ushort>();
            foreach (string name in Def.Mobs)
            {
                ObjectDesc desc;
                if (!Resources.Id2Object.TryGetValue(name, out desc))
                    continue;
                if (Manager.Behaviors.Resolve(desc.Type) == null)
                    continue;
                mobTypes.Add(desc.Type);
            }
            ushort bossType = ResolveBoss();
            if (mobTypes.Count == 0 && bossType == 0xffff)
                return;

            for (int i = 0; i < rooms.Count; i++)
            {
                DungeonGenerator.Rect room = rooms[i];
                bool isEntrance = i == 0;
                bool isBossRoom = i == rooms.Count - 1;

                if (isBossRoom)
                {
                    if (bossType != 0xffff)
                        SpawnEnemy(bossType, room.CX + 0.5f, room.CY + 0.5f, true);
                    if (mobTypes.Count > 0)
                    {
                        SpawnEnemy(mobTypes[rand.Next(mobTypes.Count)], room.CX - 1.5f, room.CY + 0.5f, false);
                        SpawnEnemy(mobTypes[rand.Next(mobTypes.Count)], room.CX + 1.5f, room.CY + 0.5f, false);
                    }
                    continue;
                }

                if (isEntrance || mobTypes.Count == 0)
                    continue;

                int pack = rand.Next(3, 7);
                for (int k = 0; k < pack; k++)
                {
                    int x = rand.Next(room.X, room.X + room.W);
                    int y = rand.Next(room.Y, room.Y + room.H);
                    Tile tile = GetTile(x, y);
                    if (tile == null || tile.StaticObject != null)
                        continue;
                    SpawnEnemy(mobTypes[rand.Next(mobTypes.Count)], x + 0.5f, y + 0.5f, false);
                }
            }
        }

        private ushort ResolveBoss()
        {
            ObjectDesc desc;
            if (!string.IsNullOrWhiteSpace(Def.Boss) &&
                Resources.Id2Object.TryGetValue(Def.Boss, out desc) &&
                Manager.Behaviors.Resolve(desc.Type) != null)
                return desc.Type;

            //Fall back to the first quest-flagged roster entry.
            foreach (string name in Def.Mobs)
            {
                if (!Resources.Id2Object.TryGetValue(name, out desc))
                    continue;
                if (desc.Quest && Manager.Behaviors.Resolve(desc.Type) != null)
                    return desc.Type;
            }
            return 0xffff;
        }

        private void SpawnEnemy(ushort type, float x, float y, bool boss)
        {
            Entity entity = Entity.Resolve(type);
            if (AddEntity(entity, new Position(x, y)) == -1)
                return;
            if (boss)
                Boss = entity;
        }

        public void OnBossKilled(Enemy boss, Player killer)
        {
            if (Boss == null || !boss.Equals(Boss))
                return;
            Boss = null;
            byte[] packet = Networking.GameServer.Text("", 0, -1, 0, "", "Dungeon cleared!");
            foreach (Player player in Players.Values)
                player.Client.Send(packet);
        }
    }
}
