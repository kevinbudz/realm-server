using RotMG.Common;
using System;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/SkullShrine.cs.
    public class SkullShrine : ISetPiece
    {
        public int Size { get { return 33; } }

        private static readonly string Grass = "Blue Grass";
        private static readonly string Tile = "Castle Stone Floor Tile";
        private static readonly string TileDark = "Castle Stone Floor Tile Dark";
        private static readonly string Stone = "Cracked Purple Stone";
        private static readonly string PillarA = "Blue Pillar";
        private static readonly string PillarB = "Broken Blue Pillar";

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            int[,] t = new int[33, 33];

            for (int x = 0; x < 33; x++)
                for (int y = 0; y < 33; y++)
                {
                    if (Math.Abs(x - Size / 2) / (Size / 2.0) + _rand.NextDouble() * 0.3 < 0.95 &&
                        Math.Abs(y - Size / 2) / (Size / 2.0) + _rand.NextDouble() * 0.3 < 0.95)
                        t[x, y] = 1;
                }

            for (int x = 12; x < 21; x++)
                for (int y = 4; y < 29; y++)
                    t[x, y] = 2;
            t = SetPieces.RotateCW(t);
            for (int x = 12; x < 21; x++)
                for (int y = 4; y < 29; y++)
                    t[x, y] = 2;

            for (int x = 13; x < 20; x++)
                for (int y = 5; y < 28; y++)
                    t[x, y] = 4;
            t = SetPieces.RotateCW(t);
            for (int x = 13; x < 20; x++)
                for (int y = 5; y < 28; y++)
                    t[x, y] = 4;

            for (int i = 0; i < 4; i++)
            {
                for (int x = 13; x < 20; x++)
                    for (int y = 5; y < 7; y++)
                        t[x, y] = 3;
                t = SetPieces.RotateCW(t);
            }

            for (int i = 0; i < 4; i++)
            {
                t[13, 7] = _rand.Next() % 3 == 0 ? 6 : 5;
                t[19, 7] = _rand.Next() % 3 == 0 ? 6 : 5;
                t[13, 10] = _rand.Next() % 3 == 0 ? 6 : 5;
                t[19, 10] = _rand.Next() % 3 == 0 ? 6 : 5;
                t = SetPieces.RotateCW(t);
            }

            Noise noise = new Noise(Environment.TickCount);
            for (int x = 0; x < 33; x++)
                for (int y = 0; y < 33; y++)
                    if (noise.GetNoise(x / 33f * 8, y / 33f * 8, 0.5f) < 0.2)
                        t[x, y] = 0;

            for (int x = 0; x < 33; x++)
                for (int y = 0; y < 33; y++)
                {
                    if (t[x, y] == 1)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Grass);
                    else if (t[x, y] == 2)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, TileDark);
                    else if (t[x, y] == 3)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Tile);
                    else if (t[x, y] == 4 || t[x, y] == 0)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Stone);
                    else if (t[x, y] == 5)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Stone);
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, PillarA);
                    }
                    else if (t[x, y] == 6)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Stone);
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, PillarB);
                    }
                }

            SetPieces.SpawnEnemy(world, "Skull Shrine", pos.X + Size / 2f, pos.Y + Size / 2f);
        }
    }
}
