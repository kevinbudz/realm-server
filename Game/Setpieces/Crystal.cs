using RotMG.Common;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/Crystal.cs.
    public class Crystal : ISetPiece
    {
        public int Size { get { return 5; } }

        public void RenderSetPiece(World world, IntPoint pos)
        {
            SetPieces.SpawnEnemy(world, "Mysterious Crystal", pos.X + 2.5f, pos.Y + 2.5f);
        }
    }
}
