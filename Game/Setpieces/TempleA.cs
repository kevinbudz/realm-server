using RotMG.Common;
using System;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/TempleA.cs.
    //Reference spawns 0x0dc2 ("Great Coil Snake", used by name here) and a
    //loot-filled chest; the chest spawns empty (see SetPieces.PutEntity).
    public class TempleA : Temple
    {
        public override int Size { get { return 60; } }

        private static readonly string Chest = "Treasure Chest";
        private static readonly string Boss = "Great Coil Snake";

        private readonly Random _rand = new Random();

        public override void RenderSetPiece(World world, IntPoint pos)
        {
            int[,] t = new int[Size, Size];
            int[,] o = new int[Size, Size];

            for (int x = 0; x < 60; x++)                    //Flooring
                for (int y = 0; y < 60; y++)
                {
                    if (Math.Abs(x - Size / 2) / (Size / 2.0) + _rand.NextDouble() * 0.3 < 0.9 &&
                        Math.Abs(y - Size / 2) / (Size / 2.0) + _rand.NextDouble() * 0.3 < 0.9)
                    {
                        double dist = Math.Sqrt(((x - Size / 2) * (x - Size / 2) + (y - Size / 2) * (y - Size / 2)) / ((Size / 2.0) * (Size / 2.0)));
                        t[x, y] = _rand.NextDouble() < (1 - dist) * (1 - dist) ? 2 : 1;
                    }
                }

            for (int x = 0; x < Size; x++)                  //Corruption
                for (int y = 0; y < Size; y++)
                    if (_rand.Next() % 50 == 0)
                        t[x, y] = 0;

            const int bas = 17;                             //Walls
            for (int x = 0; x < 20; x++)
            {
                o[bas + x, bas] = x == 19 ? 2 : 1;
                o[bas + x, bas + 1] = 2;
            }
            for (int y = 0; y < 20; y++)
            {
                o[bas, bas + y] = y == 19 ? 2 : 1;
                if (y != 0)
                    o[bas + 1, bas + y] = 2;
            }
            for (int x = 0; x < 19; x++)
            {
                o[bas + 8 + x, bas + 25] = 2;
                o[bas + 8 + x, bas + 26] = x == 0 ? 2 : 1;
            }
            for (int y = 0; y < 19; y++)
            {
                if (y != 18)
                    o[bas + 25, bas + 8 + y] = 2;
                o[bas + 26, bas + 8 + y] = y == 0 ? 2 : 1;
            }
            o[bas + 5, bas + 5] = 3;                        //Columns
            o[bas + 21, bas + 5] = 3;
            o[bas + 5, bas + 21] = 3;
            o[bas + 21, bas + 21] = 3;
            o[bas + 9, bas + 9] = 3;
            o[bas + 17, bas + 9] = 3;
            o[bas + 9, bas + 17] = 3;
            o[bas + 17, bas + 17] = 3;

            for (int x = 0; x < Size; x++)                  //Plants
                for (int y = 0; y < Size; y++)
                {
                    if (((x > 5 && x < bas) || (x < Size - 5 && x > Size - bas) ||
                         (y > 5 && y < bas) || (y < Size - 5 && y > Size - bas)) &&
                        o[x, y] == 0 && t[x, y] == 1)
                    {
                        double r = _rand.NextDouble();
                        if (r > 0.6)        //0.4
                            o[x, y] = 4;
                        else if (r > 0.35)  //0.25
                            o[x, y] = 5;
                        else if (r > 0.33)  //0.02
                            o[x, y] = 6;
                    }
                }

            int rotation = _rand.Next(0, 4);               //Rotation
            for (int i = 0; i < rotation; i++)
            {
                t = SetPieces.RotateCW(t);
                o = SetPieces.RotateCW(o);
            }

            Render(this, world, pos, t, o);

            //Boss & Chest
            SetPieces.PutEntity(world, Chest, pos.X + Size / 2, pos.Y + Size / 2);
            SetPieces.SpawnEnemy(world, Boss, pos.X + Size / 2, pos.Y + Size / 2);
        }
    }
}
