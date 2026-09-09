using RotMG.Common;
using System;

namespace RotMG.Game.Setpieces
{
    //Oryx event bosses, ported from realm-src-master
    //wServer/realm/setpieces/{CubeGod,Pentaract,LordoftheLostLands,Hermit,GhostShip}.cs.
    public class CubeGod : ISetPiece
    {
        public int Size { get { return 5; } }

        public void RenderSetPiece(World world, IntPoint pos)
        {
            SetPieces.SpawnEnemy(world, "Cube God", pos.X + 2.5f, pos.Y + 2.5f);
        }
    }

    public class LordoftheLostLands : ISetPiece
    {
        public int Size { get { return 5; } }

        public void RenderSetPiece(World world, IntPoint pos)
        {
            SetPieces.SpawnEnemy(world, "Lord of the Lost Lands", pos.X + 2.5f, pos.Y + 2.5f);
        }
    }

    public class Hermit : ISetPiece
    {
        public int Size { get { return 32; } }

        public void RenderSetPiece(World world, IntPoint pos)
        {
            //Master renders the SP_Hermit sub-map here; without that resource
            //the Hermit God is summoned directly onto cleared ground.
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    SetPieces.ClearTile(world, x + pos.X, y + pos.Y);
            SetPieces.SpawnEnemy(world, "Hermit God", pos.X + Size / 2f, pos.Y + Size / 2f);
        }
    }

    public class GhostShip : ISetPiece
    {
        public int Size { get { return 40; } }

        public void RenderSetPiece(World world, IntPoint pos)
        {
            //Master renders the SP_GhostShip sub-map here; without that
            //resource the Ghost Ship is summoned directly.
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    SetPieces.ClearTile(world, x + pos.X, y + pos.Y);
            SetPieces.SpawnEnemy(world, "Ghost Ship", pos.X + Size / 2f, pos.Y + Size / 2f);
        }
    }

    public class Pentaract : ISetPiece
    {
        public int Size { get { return 41; } }

        private static readonly string Floor = "Scorch Blend";
        private static readonly byte[,] Circle = new byte[,]
        {
            { 0, 0, 1, 1, 1, 0, 0 },
            { 0, 1, 1, 1, 1, 1, 0 },
            { 1, 1, 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1, 1, 1 },
            { 0, 1, 1, 1, 1, 1, 0 },
            { 0, 0, 1, 1, 1, 0, 0 }
        };

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            int[,] t = new int[41, 41];

            for (int i = 0; i < 5; i++)
            {
                double angle = (360 / 5 * i) * Math.PI / 180;
                int xs = (int)(Math.Cos(angle) * 15 + 20 - 3);
                int ys = (int)(Math.Sin(angle) * 15 + 20 - 3);

                for (int x = 0; x < 7; x++)
                    for (int y = 0; y < 7; y++)
                        t[xs + x, ys + y] = Circle[x, y];
                t[xs + 3, ys + 3] = 2;
            }
            t[20, 20] = 3;

            for (int x = 0; x < 40; x++)
                for (int y = 0; y < 40; y++)
                {
                    if (t[x, y] == 1)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                    else if (t[x, y] == 2)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        SetPieces.SpawnEnemy(world, 0x0d5e, pos.X + x + 0.5f, pos.Y + y + 0.5f);
                    }
                    else if (t[x, y] == 3)
                        SetPieces.SpawnEnemy(world, "Pentaract", pos.X + x + 0.5f, pos.Y + y + 0.5f);
                }
        }
    }
}
