using RotMG.Common;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/Graveyard.cs.
    //The reference fills the chest with rolled tier loot; this codebase has
    //no static-chest loot roller, so the Treasure Chest spawns empty (see
    //SetPieces.PutEntity).
    public class Graveyard : ISetPiece
    {
        public int Size { get { return 34; } }

        private static readonly string Floor = "Grass";
        private static readonly string WallA = "Grey Wall";
        private static readonly string WallB = "Destructible Grey Wall";
        private static readonly string Cross = "Cross";
        private static readonly string Chest = "Treasure Chest";

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            int[,] t = new int[23, 35];

            for (int x = 0; x < 23; x++)    //Floor
                for (int y = 0; y < 35; y++)
                    t[x, y] = _rand.Next() % 3 == 0 ? 0 : 1;

            for (int y = 0; y < 35; y++)    //Perimeters
                t[0, y] = t[22, y] = 2;
            for (int x = 0; x < 23; x++)
                t[x, 0] = t[x, 34] = 2;

            List<IntPoint> pts = new List<IntPoint>();
            for (int y = 0; y < 11; y++)    //Crosses
                for (int x = 0; x < 7; x++)
                {
                    if (_rand.Next() % 3 > 0)
                        t[2 + 3 * x, 2 + 3 * y] = 4;
                    else
                        pts.Add(new IntPoint(2 + 3 * x, 2 + 3 * y));
                }

            for (int x = 0; x < 23; x++)    //Corruption
                for (int y = 0; y < 35; y++)
                {
                    if (t[x, y] == 1 || t[x, y] == 0 || t[x, y] == 4) continue;
                    double p = _rand.NextDouble();
                    if (p < 0.1)
                        t[x, y] = 1;
                    else if (p < 0.4)
                        t[x, y]++;
                }

            //Boss & Chest
            IntPoint pt = pts[_rand.Next(0, pts.Count)];
            t[pt.X, pt.Y] = 5;
            t[pt.X + 1, pt.Y] = 6;

            int r = _rand.Next(0, 4);
            for (int i = 0; i < r; i++)     //Rotation
                t = SetPieces.RotateCW(t);
            int w = t.GetLength(0), h = t.GetLength(1);

            for (int x = 0; x < w; x++)     //Rendering
                for (int y = 0; y < h; y++)
                {
                    if (t[x, y] == 1)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                    else if (t[x, y] == 2)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, WallA);
                    }
                    else if (t[x, y] == 3)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        SetPieces.PutEntity(world, WallB, x + pos.X + 0.5f, y + pos.Y + 0.5f);
                    }
                    else if (t[x, y] == 4)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, Cross);
                    }
                    else if (t[x, y] == 5)
                        SetPieces.PutEntity(world, Chest, pos.X + x + 0.5f, pos.Y + y + 0.5f);
                    else if (t[x, y] == 6)
                        SetPieces.SpawnEnemy(world, "Deathmage", pos.X + x, pos.Y + y);
                }
        }
    }
}
