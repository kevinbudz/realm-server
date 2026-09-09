using RotMG.Common;
using System;
using System.Collections.Generic;

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
