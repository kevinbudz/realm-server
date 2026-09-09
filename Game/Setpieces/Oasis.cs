using RotMG.Common;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/Oasis.cs.
    //Chest spawns empty (no static-chest loot roller here; see
    //SetPieces.PutEntity).
    public class Oasis : ISetPiece
    {
        public int Size { get { return 30; } }

        private static readonly string Floor = "Light Grass";
        private static readonly string Water = "Shallow Water";
        private static readonly string Tree = "Palm Tree";
        private static readonly string Chest = "Treasure Chest";

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            const int outerRadius = 13;
            const int waterRadius = 10;
            const int islandRadius = 3;
            List<IntPoint> border = new List<IntPoint>();

            int[,] t = new int[Size, Size];
            for (int y = 0; y < Size; y++)      //Outer
                for (int x = 0; x < Size; x++)
                {
                    double dx = x - (Size / 2.0);
                    double dy = y - (Size / 2.0);
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    if (r <= outerRadius)
                        t[x, y] = 1;
                }

            for (int y = 0; y < Size; y++)      //Water
                for (int x = 0; x < Size; x++)
                {
                    double dx = x - (Size / 2.0);
                    double dy = y - (Size / 2.0);
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    if (r <= waterRadius)
                    {
                        t[x, y] = 2;
                        if (waterRadius - r < 1)
                            border.Add(new IntPoint(x, y));
                    }
                }

            for (int y = 0; y < Size; y++)      //Island
                for (int x = 0; x < Size; x++)
                {
                    double dx = x - (Size / 2.0);
                    double dy = y - (Size / 2.0);
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    if (r <= islandRadius)
                    {
                        t[x, y] = 1;
                        if (islandRadius - r < 1)
                            border.Add(new IntPoint(x, y));
                    }
                }

            HashSet<IntPoint> trees = new HashSet<IntPoint>();
            while (trees.Count < border.Count * 0.5)
                trees.Add(border[_rand.Next(0, border.Count)]);

            foreach (IntPoint i in trees)
                t[i.X, i.Y] = 3;

            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    if (t[x, y] == 1)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                    else if (t[x, y] == 2)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Water);
                    else if (t[x, y] == 3)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        Entity tree = SetPieces.PutStatic(world, x + pos.X, y + pos.Y, Tree);
                        if (tree != null)
                            tree.Size = _rand.Next() % 2 == 0 ? 120 : 140;
                    }
                }

            SetPieces.SpawnEnemy(world, "Oasis Giant", pos.X + 15.5f, pos.Y + 15.5f);
            SetPieces.PutEntity(world, Chest, pos.X + 15.5f, pos.Y + 15.5f);
        }
    }
}
