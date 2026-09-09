using RotMG.Common;
using System;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/Pyre.cs.
    //Chest spawns empty (no static-chest loot roller here; see
    //SetPieces.PutEntity).
    public class Pyre : ISetPiece
    {
        public int Size { get { return 30; } }

        private static readonly string Floor = "Scorch Blend";
        private static readonly string Chest = "Treasure Chest";

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    double dx = x - (Size / 2.0);
                    double dy = y - (Size / 2.0);
                    double r = Math.Sqrt(dx * dx + dy * dy) + _rand.NextDouble() * 4 - 2;
                    if (r <= 10)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                }

            SetPieces.SpawnEnemy(world, "Phoenix Lord", pos.X + 15.5f, pos.Y + 15.5f);
            SetPieces.PutEntity(world, Chest, pos.X + 15.5f, pos.Y + 15.5f);
        }
    }
}
