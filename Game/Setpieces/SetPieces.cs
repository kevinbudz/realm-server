using RotMG.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RotMG.Game.Setpieces
{
    //Live realm event pieces, ported from realm-src-master
    //wServer/realm/setpieces/ISetPiece.cs.
    public interface ISetPiece
    {
        int Size { get; }
        void RenderSetPiece(World world, IntPoint pos);
    }

    //Shared render helpers adapting the master setpieces (written against
    //Wmap/TileRegion) to this codebase's Tile/World model, plus the Oryx
    //event registry from wServer/realm/Oryx.cs.
    public static class SetPieces
    {
        public static readonly List<Tuple<string, ISetPiece>> Events =
            new List<Tuple<string, ISetPiece>>()
            {
                Tuple.Create("Skull Shrine", (ISetPiece)new SkullShrine()),
                Tuple.Create("Cube God", (ISetPiece)new CubeGod()),
                Tuple.Create("Pentaract", (ISetPiece)new Pentaract()),
                Tuple.Create("Grand Sphinx", (ISetPiece)new Sphinx()),
                Tuple.Create("Lord of the Lost Lands", (ISetPiece)new LordoftheLostLands()),
                Tuple.Create("Hermit God", (ISetPiece)new Hermit()),
                Tuple.Create("Ghost Ship", (ISetPiece)new GhostShip())
            };

        // Static setpiece registry with reference placement counts, VERBATIM
        // from realm-src-master wServer/realm/setpieces/SetPieces.cs
        // (min inclusive, max exclusive via Random.Next).
        private static Tuple<ISetPiece, int, int, TerrainType[]> SetPiece(ISetPiece piece, int min, int max, params TerrainType[] terrains)
        {
            return Tuple.Create(piece, min, max, terrains);
        }

        private static readonly List<Tuple<ISetPiece, int, int, TerrainType[]>> StaticPieces =
            new List<Tuple<ISetPiece, int, int, TerrainType[]>>()
            {
                SetPiece(new Building(), 80, 100, TerrainType.LowForest, TerrainType.LowPlains, TerrainType.MidForest),
                SetPiece(new Graveyard(), 5, 10, TerrainType.LowSand, TerrainType.LowPlains),
                SetPiece(new Grove(), 17, 25, TerrainType.MidForest, TerrainType.MidPlains),
                SetPiece(new LichyTemple(), 4, 7, TerrainType.MidForest, TerrainType.MidPlains),
                SetPiece(new Castle(), 4, 7, TerrainType.HighForest, TerrainType.HighPlains),
                SetPiece(new Tower(), 8, 15, TerrainType.HighForest, TerrainType.HighPlains),
                SetPiece(new TempleA(), 10, 20, TerrainType.MidForest, TerrainType.MidPlains),
                SetPiece(new TempleB(), 10, 20, TerrainType.MidForest, TerrainType.MidPlains),
                SetPiece(new Oasis(), 0, 5, TerrainType.LowSand, TerrainType.MidSand),
                SetPiece(new Pyre(), 0, 5, TerrainType.MidSand, TerrainType.HighSand),
                SetPiece(new LavaFissure(), 3, 5, TerrainType.Mountains),
                SetPiece(new Crystal(), 1, 1, TerrainType.Mountains),
                SetPiece(new KageKami(), 2, 3, TerrainType.HighForest, TerrainType.HighPlains)
            };

        private struct Rect
        {
            public int X;
            public int Y;
            public int W;
            public int H;

            public static bool Intersects(Rect r1, Rect r2)
            {
                return !(r2.X > r1.X + r1.W ||
                         r2.X + r2.W < r1.X ||
                         r2.Y > r1.Y + r1.H ||
                         r2.Y + r2.H < r1.Y);
            }
        }

        // Terrain anchor for placement, going through the same
        // painted-first classifier as Oryx (see TerrainClassifier).
        private static TerrainType GetTerrainAt(World world, List<IntPoint> spawns, int x, int y)
        {
            Tile tile = world.GetTile(x, y);
            if (tile == null)
                return TerrainType.None;
            TileDesc ground;
            if (!Resources.Type2Tile.TryGetValue(tile.Type, out ground))
                return TerrainType.None;
            return TerrainClassifier.GetTileTerrain(world.Map, ground.Id, x, y, spawns);
        }

        // Reference placement algorithm from realm-src-master
        // SetPieces.ApplySetPieces, adapted to the local World/Tile model
        // (World.Width/Height, classified terrain, local helpers).
        public static void ApplySetPieces(World world)
        {
            List<IntPoint> spawnList;
            List<IntPoint> spawns;
            if (world.Map.Regions.TryGetValue(Region.Spawn, out spawns) && spawns.Count > 0)
                spawnList = spawns;
            else
                spawnList = new List<IntPoint>() { new IntPoint(world.Width / 2, world.Height / 2) };

            Random rand = new Random();
            HashSet<Rect> rects = new HashSet<Rect>();
            foreach (var dat in StaticPieces)
            {
                int size = dat.Item1.Size;
                int count = rand.Next(dat.Item2, dat.Item3);
                for (int i = 0; i < count; i++)
                {
                    IntPoint pt = new IntPoint();
                    Rect rect;

                    int max = 50;
                    do
                    {
                        pt.X = rand.Next(0, world.Width);
                        pt.Y = rand.Next(0, world.Height);
                        rect = new Rect() { X = pt.X, Y = pt.Y, W = size, H = size };
                        max--;
                    } while ((Array.IndexOf(dat.Item4, GetTerrainAt(world, spawnList, pt.X, pt.Y)) == -1 ||
                             rects.Any(_ => Rect.Intersects(rect, _))) &&
                             max > 0);
                    if (max <= 0) continue;
                    dat.Item1.RenderSetPiece(world, pt);
                    rects.Add(rect);
                }
            }
        }

        // Live-entity placement for destructible walls and treasure chests,
        // which the reference EnterWorlds as entities instead of painting as
        // static tile objects. (Chests spawn empty: no static-chest loot
        // roller exists here; loot flows from enemy behavior tables.)
        public static Entity PutEntity(World world, string objName, float x, float y)
        {
            return SpawnEnemy(world, objName, x, y);
        }

        private static readonly Dictionary<string, JSMap> SubMaps = new Dictionary<string, JSMap>();

        //Setpiece sub-maps from realm-src-master common/resources/worlds
        //(SP_Hermit/SP_GhostShip/SP_KageKami.jm), loaded once and cached.
        public static JSMap GetSubMap(string file)
        {
            lock (SubMaps)
            {
                JSMap map;
                if (!SubMaps.TryGetValue(file, out map))
                {
                    string path = Resources.CombineResourcePath($"Worlds/Setpieces/{file}");
                    map = new JSMap(File.ReadAllText(path));
                    SubMaps[file] = map;
                }
                return map;
            }
        }

        //Projects a .jm sub-map onto the live world at pos, mirroring
        //realm-src-master Wmap.ProjectOntoWorld (as called by
        //SetPieces.RenderFromProto): empty (ground-none) tiles are skipped,
        //everything else paints ground, regions and objects, with objects
        //entering the world exactly like map objects at world load.
        public static void RenderSubMap(World world, IntPoint pos, string file)
        {
            JSMap map = GetSubMap(file);
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    int wx = pos.X + x;
                    int wy = pos.Y + y;
                    Tile tile = world.GetTile(wx, wy);
                    if (tile == null)
                        continue;
                    JSTile src = map.Tiles[x, y];
                    if (src.GroundType == 255)
                        continue;
                    world.RemoveStatic(wx, wy);
                    tile.Type = src.GroundType;
                    tile.UpdateCount++;
                    world.UpdateCount++;
                    if (src.Region != Region.None)
                    {
                        tile.Region = src.Region;
                        List<IntPoint> list;
                        if (!world.Map.Regions.TryGetValue(src.Region, out list))
                            world.Map.Regions[src.Region] = list = new List<IntPoint>();
                        list.Add(new IntPoint(wx, wy));
                        tile.UpdateCount++;
                        world.UpdateCount++;
                    }
                    if (src.ObjectType == 0xff)
                        continue;
                    ObjectDesc desc;
                    if (!Resources.Type2Object.TryGetValue(src.ObjectType, out desc))
                        continue;
                    ParseOffsets(src.Key, out float ox, out float oy);
                    Entity entity = Entity.Resolve(src.ObjectType);
                    Wmap.ApplyObjCfg(entity, src.Key);
                    if (world.AddEntity(entity, new Position(wx + 0.5f + ox, wy + 0.5f + oy)) == -1)
                        continue;
                    if (entity is Entities.StaticObject staticObject)
                    {
                        tile.StaticObject = staticObject;
                        if (entity.Desc.BlocksSight)
                            tile.BlocksSight = true;
                        tile.UpdateCount++;
                        world.UpdateCount++;
                    }
                }
        }

        //Spawn-position tweaks from the map object config (see the Hermit
        //God's xOffset). Size/hp/eff come from Wmap.ApplyObjCfg; display
        //names have no local member and merchant/conn fields do not occur
        //in the shipped sub-maps, so both are ignored.
        private static void ParseOffsets(string cfg, out float ox, out float oy)
        {
            ox = 0;
            oy = 0;
            if (string.IsNullOrEmpty(cfg))
                return;
            foreach (string part in cfg.Split(';'))
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                string[] kv = part.Split(':');
                if (kv.Length < 2)
                    continue;
                if (kv[0] == "xOffset")
                    float.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out ox);
                else if (kv[0] == "yOffset")
                    float.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out oy);
            }
        }

        public static int[,] ReflectVert(int[,] mat)
        {
            int m = mat.GetLength(0);
            int n = mat.GetLength(1);
            int[,] ret = new int[m, n];
            for (int x = 0; x < m; x++)
                for (int y = 0; y < n; y++)
                    ret[x, n - y - 1] = mat[x, y];
            return ret;
        }

        public static int[,] ReflectHori(int[,] mat)
        {
            int m = mat.GetLength(0);
            int n = mat.GetLength(1);
            int[,] ret = new int[m, n];
            for (int x = 0; x < m; x++)
                for (int y = 0; y < n; y++)
                    ret[m - x - 1, y] = mat[x, y];
            return ret;
        }

        public static int[,] RotateCW(int[,] mat)
        {
            int m = mat.GetLength(0);
            int n = mat.GetLength(1);
            int[,] ret = new int[n, m];
            for (int r = 0; r < m; r++)
                for (int c = 0; c < n; c++)
                    ret[c, m - 1 - r] = mat[r, c];
            return ret;
        }

        public static bool InBounds(World world, int x, int y)
        {
            return x >= 0 && y >= 0 && x < world.Width && y < world.Height;
        }

        public static bool PutGround(World world, int x, int y, string groundName)
        {
            Tile tile = world.GetTile(x, y);
            TileDesc desc;
            if (tile == null || !Resources.Id2Tile.TryGetValue(groundName, out desc))
                return false;
            world.RemoveStatic(x, y);
            tile.Type = desc.Type;
            tile.UpdateCount++;
            world.UpdateCount++;
            return true;
        }

        public static bool ClearTile(World world, int x, int y)
        {
            if (world.GetTile(x, y) == null)
                return false;
            world.RemoveStatic(x, y);
            return true;
        }

        public static Entity PutStatic(World world, int x, int y, string objName)
        {
            Tile tile = world.GetTile(x, y);
            ObjectDesc desc;
            if (tile == null || !Resources.Id2Object.TryGetValue(objName, out desc))
                return null;
            world.RemoveStatic(x, y);
            Entity entity = Entity.Resolve(desc.Type);
            if (world.AddEntity(entity, new Position(x + 0.5f, y + 0.5f)) == -1)
                return null;
            if (entity is Entities.StaticObject staticObject)
            {
                tile.StaticObject = staticObject;
                if (entity.Desc.BlocksSight)
                    tile.BlocksSight = true;
                tile.UpdateCount++;
                world.UpdateCount++;
            }
            return entity;
        }

        public static Entity SpawnEnemy(World world, string name, float x, float y)
        {
            ObjectDesc desc;
            if (!Resources.Id2Object.TryGetValue(name, out desc))
                return null;
            Entity entity = Entity.Resolve(desc.Type);
            if (world.AddEntity(entity, new Position(x, y)) == -1)
                return null;
            return entity;
        }

        public static Entity SpawnEnemy(World world, ushort type, float x, float y)
        {
            Entity entity = Entity.Resolve(type);
            if (world.AddEntity(entity, new Position(x, y)) == -1)
                return null;
            return entity;
        }
    }
}
