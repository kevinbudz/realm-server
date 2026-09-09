using RotMG.Common;
using System;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/Tower.cs.
    //Reference spawns 0x0928, which is "Ghost King" (used by name here).
    public class Tower : ISetPiece
    {
        private static readonly int[,] Quarter;

        static Tower()
        {
            string s =
"............XX\n" +
"........XXXXXX\n" +
"......XXXXXXXX\n" +
".....XXXX=====\n" +
"....XXX=======\n" +
"...XXX========\n" +
"..XXX=========\n" +
"..XX==========\n" +
".XXX==========\n" +
".XX===========\n" +
".XX===========\n" +
".XX===========\n" +
"XXX===========\n" +
"XXX===========";
            string[] a = s.Split('\n');
            Quarter = new int[14, 14];
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 14; x++)
                    Quarter[x, y] =
                        a[y][x] == 'X' ? 1 :
                            (a[y][x] == '=' ? 2 : 0);
        }

        public int Size { get { return 27; } }

        private static readonly string Floor = "Rock";
        private static readonly string Wall = "Grey Wall";

        private readonly Random _rand = new Random();

        public void RenderSetPiece(World world, IntPoint pos)
        {
            int[,] t = new int[27, 27];

            int[,] q = (int[,])Quarter.Clone();

            for (int y = 0; y < 14; y++)        //Top left
                for (int x = 0; x < 14; x++)
                    t[x, y] = q[x, y];

            q = SetPieces.ReflectHori(q);           //Top right
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 14; x++)
                    t[13 + x, y] = q[x, y];

            q = SetPieces.ReflectVert(q);           //Bottom right
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 14; x++)
                    t[13 + x, 13 + y] = q[x, y];

            q = SetPieces.ReflectHori(q);           //Bottom left
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 14; x++)
                    t[x, 13 + y] = q[x, y];

            for (int y = 1; y < 4; y++)             //Opening
                for (int x = 8; x < 19; x++)
                    t[x, y] = 2;
            t[12, 0] = t[13, 0] = t[14, 0] = 2;

            int r = _rand.Next(0, 4);                //Rotation
            for (int i = 0; i < r; i++)
                t = SetPieces.RotateCW(t);

            t[13, 13] = 3;

            for (int x = 0; x < 27; x++)            //Rendering
                for (int y = 0; y < 27; y++)
                {
                    if (t[x, y] == 1)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, Wall);
                    }
                    else if (t[x, y] == 2)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                    else if (t[x, y] == 3)
                    {
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);
                        SetPieces.SpawnEnemy(world, "Ghost King", pos.X + x, pos.Y + y);
                    }
                }
        }
    }
}
