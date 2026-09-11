using RotMG.Common;
using System.Collections.Generic;

namespace RotMG.Game.Dungeons.Candyland
{
    //Hunt tiles are scanned from the loaded map instead of the old .jm
    //enemy paint. A tile is valid when the ground is walkable (not
    //Empty/Void/NoWalk), it is outside the spawn and boss rooms, and it
    //is at least StaticClearance tiles from every static (colliding walls
    //and non-blocking decorations alike).
    public partial class Overseer
    {
        private const int StaticClearance = 2;
        private const int SpawnRoomX = 132;
        private const int SpawnRoomY = 125;
        private const int SpawnRoomSize = 19;
        private const int BossRoomX = 114;
        private const int BossRoomY = 183;
        private const int BossRoomSize = 32;

        //Hunt slots hold the 5 hunt enemies plus Spilled IceCream and
        //Hard Candy, which spawn independently of any mob and never roll
        //a boss. The remaining minions are never placed by the Overseer;
        //each hunt enemy brings 1-3 of its own once via SpawnOnce.
        private static readonly (string Name, int Count)[] HuntMix =
        {
            ("Big Creampuff", 4),
            ("Rototo", 4),
            ("Wishing Troll", 3),
            ("Unicorn", 3),
            ("Beefy Fairy", 2),
            ("Spilled IceCream", 3),
            ("Hard Candy", 3)
        };

        private static List<IntPoint> CollectHuntTiles(World world)
        {
            int w = world.Width;
            int h = world.Height;
            bool[,] nearStatic = new bool[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (world.Tiles[x, y].StaticObject == null)
                        continue;
                    MarkStaticNeighborhood(nearStatic, w, h, x, y);
                }
            }

            bool[,] hasEntity = new bool[w, h];
            foreach (RotMG.Game.Entity en in world.Entities.Values)
            {
                int x = (int)en.Position.X;
                int y = (int)en.Position.Y;
                if (x >= 0 && y >= 0 && x < w && y < h)
                    hasEntity[x, y] = true;
            }

            List<IntPoint> tiles = new List<IntPoint>();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (nearStatic[x, y] || hasEntity[x, y])
                        continue;
                    if (InBox(x, y, SpawnRoomX, SpawnRoomY, SpawnRoomSize))
                        continue;
                    if (InBox(x, y, BossRoomX, BossRoomY, BossRoomSize))
                        continue;
                    if (IsUnwalkable(world, x, y))
                        continue;
                    tiles.Add(new IntPoint(x, y));
                }
            }
            return tiles;
        }

        //Chebyshev distance to the static must be >= StaticClearance, so a
        //value of 2 keeps the object's tile and its 8 neighbors clear.
        private static void MarkStaticNeighborhood(bool[,] nearStatic, int w, int h, int sx, int sy)
        {
            int pad = StaticClearance - 1;
            for (int dx = -pad; dx <= pad; dx++)
            {
                for (int dy = -pad; dy <= pad; dy++)
                {
                    int x = sx + dx;
                    int y = sy + dy;
                    if (x >= 0 && y >= 0 && x < w && y < h)
                        nearStatic[x, y] = true;
                }
            }
        }

        private static bool InBox(int x, int y, int x0, int y0, int size)
        {
            return x >= x0 && x < x0 + size && y >= y0 && y < y0 + size;
        }

        private static bool IsUnwalkable(World world, int x, int y)
        {
            Tile? tile = world.GetTile(x, y);
            if (tile == null)
                return true;
            if (!Resources.Type2Tile.TryGetValue(tile.Value.Type, out TileDesc ground))
                return true;
            return ground.NoWalk;
        }
    }
}
