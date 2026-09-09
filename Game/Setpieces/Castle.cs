using RotMG.Common;
using System;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/Castle.cs.
    //Chest spawns empty (no static-chest loot roller here; see
    //SetPieces.PutEntity).
    public class Castle : ISetPiece
    {
        public int Size { get { return 40; } }

        private static readonly string Floor = "Rock";
        private static readonly string Bridge = "Bridge";
        private static readonly string WaterA = "Shallow Water";
        private static readonly string WaterB = "Dark Water";
        private static readonly string WallA = "Grey Wall";
        private static readonly string WallB = "Destructible Grey Wall";
        private static readonly string Chest = "Treasure Chest";

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            int[,] t = new int[31, 40];

            for (int x = 0; x < 13; x++)    //Moats
                for (int y = 0; y < 13; y++)
                {
                    if ((x == 0 && (y < 3 || y > 9)) ||
                        (y == 0 && (x < 3 || x > 9)) ||
                        (x == 12 && (y < 3 || y > 9)) ||
                        (y == 12 && (x < 3 || x > 9)))
                        continue;
                    t[x + 0, y + 0] = t[x + 18, y + 0] = 2;
                    t[x + 0, y + 27] = t[x + 18, y + 27] = 2;
                }
            for (int x = 3; x < 28; x++)
                for (int y = 3; y < 37; y++)
                {
                    if (x < 6 || x > 24 || y < 6 || y > 33)
                        t[x, y] = 2;
                }

            for (int x = 7; x < 24; x++)    //Floor
                for (int y = 7; y < 33; y++)
                    t[x, y] = _rand.Next() % 3 == 0 ? 0 : 1;

            for (int x = 0; x < 7; x++)    //Perimeter
                for (int y = 0; y < 7; y++)
                {
                    if ((x == 0 && y != 3) ||
                        (y == 0 && x != 3) ||
                        (x == 6 && y != 3) ||
                        (y == 6 && x != 3))
                        continue;
                    t[x + 3, y + 3] = t[x + 21, y + 3] = 4;
                    t[x + 3, y + 30] = t[x + 21, y + 30] = 4;
                }
            for (int x = 6; x < 25; x++)
                t[x, 6] = t[x, 33] = 4;
            for (int y = 6; y < 34; y++)
                t[6, y] = t[24, y] = 4;

            for (int x = 13; x < 18; x++)    //Bridge
                for (int y = 3; y < 7; y++)
                    t[x, y] = 6;

            for (int x = 0; x < 31; x++)    //Corruption
                for (int y = 0; y < 40; y++)
                {
                    if (t[x, y] == 1 || t[x, y] == 0) continue;
                    double p = _rand.NextDouble();
                    if (t[x, y] == 6)
                    {
                        if (p < 0.4)
                            t[x, y] = 0;
                        continue;
                    }

                    if (p < 0.1)
                        t[x, y] = 1;
                    else if (p < 0.4)
                        t[x, y]++;
                }

            //Boss & Chest
            t[15, 27] = 7;
            t[15, 20] = 8;

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
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, WaterA);
                    else if (t[x, y] == 3)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, WaterB);
                    else if (t[x, y] == 4)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, WallA);
                    }
                    else if (t[x, y] == 5)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        SetPieces.PutEntity(world, WallB, x + pos.X + 0.5f, y + pos.Y + 0.5f);
                    }
                    else if (t[x, y] == 6)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Bridge);
                    else if (t[x, y] == 7)
                        SetPieces.PutEntity(world, Chest, pos.X + x + 0.5f, pos.Y + y + 0.5f);
                    else if (t[x, y] == 8)
                        SetPieces.SpawnEnemy(world, "Cyclops God", pos.X + x, pos.Y + y);
                }
        }
    }
}
