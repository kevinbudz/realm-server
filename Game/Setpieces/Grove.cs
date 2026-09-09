using RotMG.Common;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/Grove.cs.
    public class Grove : ISetPiece
    {
        public int Size { get { return 25; } }

        private static readonly string Floor = "Light Grass";
        private static readonly string Tree = "Cherry Tree";

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            int radius = _rand.Next(Size - 5, Size + 1) / 2;
            List<IntPoint> border = new List<IntPoint>();

            int[,] t = new int[Size, Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    double dx = x - (Size / 2.0);
                    double dy = y - (Size / 2.0);
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    if (r <= radius)
                    {
                        t[x, y] = 1;
                        if (radius - r < 1.5)
                            border.Add(new IntPoint(x, y));
                    }
                }

            HashSet<IntPoint> trees = new HashSet<IntPoint>();
            while (trees.Count < border.Count * 0.5)
                trees.Add(border[_rand.Next(0, border.Count)]);

            foreach (IntPoint i in trees)
                t[i.X, i.Y] = 2;

            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    if (t[x, y] == 1)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                    else if (t[x, y] == 2)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        //Reference scales the tree via tile ObjCfg
                        //("size:120/140"); the local equivalent is Size.
                        Entity tree = SetPieces.PutStatic(world, x + pos.X, y + pos.Y, Tree);
                        if (tree != null)
                            tree.Size = _rand.Next() % 2 == 0 ? 120 : 140;
                    }
                }

            Entity ent = SetPieces.SpawnEnemy(world, "Ent Ancient", pos.X + Size / 2 + 1, pos.Y + Size / 2 + 1);
            if (ent != null)
                ent.Size = 140;
        }
    }
}
