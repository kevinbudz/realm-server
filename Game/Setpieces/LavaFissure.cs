using RotMG.Common;
using System;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/LavaFissure.cs.
    //Chest spawns empty (no static-chest loot roller here; see
    //SetPieces.PutEntity).
    public class LavaFissure : ISetPiece
    {
        public int Size { get { return 40; } }

        private static readonly string Lava = "Lava Blend";
        private static readonly string Floor = "Partial Red Floor";
        private static readonly string Chest = "Treasure Chest";

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            int[,] p = new int[Size, Size];
            const double SCALE = 5.5;
            for (int x = 0; x < Size; x++)      //Lava
            {
                double t = (double)x / Size * Math.PI;
                double x_ = t / Math.Sqrt(2) - Math.Sin(t) / (SCALE * Math.Sqrt(2));
                double y1 = t / Math.Sqrt(2) - 2 * Math.Sin(t) / (SCALE * Math.Sqrt(2));
                double y2 = t / Math.Sqrt(2) + Math.Sin(t) / (SCALE * Math.Sqrt(2));
                y1 /= Math.PI / Math.Sqrt(2);
                y2 /= Math.PI / Math.Sqrt(2);

                int y1_ = (int)Math.Ceiling(y1 * Size);
                int y2_ = (int)Math.Floor(y2 * Size);
                for (int i = y1_; i < y2_; i++)
                    p[x, i] = 1;
            }

            for (int x = 0; x < Size; x++)      //Floor
                for (int y = 0; y < Size; y++)
                {
                    if (p[x, y] == 1 && _rand.Next() % 5 == 0)
                        p[x, y] = 2;
                }

            int r = _rand.Next(0, 4);            //Rotation
            for (int i = 0; i < r; i++)
                p = SetPieces.RotateCW(p);
            p[20, 20] = 2;

            for (int x = 0; x < Size; x++)      //Rendering
                for (int y = 0; y < Size; y++)
                {
                    if (p[x, y] == 1)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Lava);
                    else if (p[x, y] == 2)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Lava);
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, Floor);
                    }
                }

            SetPieces.SpawnEnemy(world, "Red Demon", pos.X + 20.5f, pos.Y + 20.5f);
            SetPieces.PutEntity(world, Chest, pos.X + 20.5f, pos.Y + 20.5f);
        }
    }
}
