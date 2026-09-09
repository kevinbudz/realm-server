using RotMG.Common;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/Temple.cs.
    //Shared ground/object renderer for TempleA/TempleB.
    public abstract class Temple : ISetPiece
    {
        public abstract int Size { get; }
        public abstract void RenderSetPiece(World world, IntPoint pos);

        protected static readonly string DarkGrass = "Dark Grass";
        protected static readonly string Floor = "Jungle Temple Floor";
        protected static readonly string WallA = "Jungle Temple Bricks";
        protected static readonly string WallB = "Jungle Temple Walls";
        protected static readonly string WallC = "Jungle Temple Column";
        protected static readonly string Flower = "Jungle Ground Flowers";
        protected static readonly string Grass = "Jungle Grass";
        protected static readonly string Tree = "Jungle Tree Big";

        protected static void Render(Temple temple, World world, IntPoint pos, int[,] ground, int[,] objs)
        {
            for (int x = 0; x < temple.Size; x++)                  //Rendering
                for (int y = 0; y < temple.Size; y++)
                {
                    if (ground[x, y] == 1)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, DarkGrass);
                    else if (ground[x, y] == 2)
                        SetPieces.PutGround(world, x + pos.X, y + pos.Y, Floor);

                    if (objs[x, y] == 1)
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, WallA);
                    else if (objs[x, y] == 2)
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, WallB);
                    else if (objs[x, y] == 3)
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, WallC);
                    else if (objs[x, y] == 4)
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, Flower);
                    else if (objs[x, y] == 5)
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, Grass);
                    else if (objs[x, y] == 6)
                        SetPieces.PutStatic(world, x + pos.X, y + pos.Y, Tree);
                }
        }
    }
}
