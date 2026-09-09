using RotMG.Common;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/KageKami.cs: the
    //master renders the KageKami proto sub-map here, now shipped under
    //Resources/Worlds/Setpieces.
    public class KageKami : ISetPiece
    {
        public int Size { get { return 65; } }

        public void RenderSetPiece(World world, IntPoint pos)
        {
            SetPieces.RenderSubMap(world, pos, "SP_KageKami.jm");
        }
    }
}
