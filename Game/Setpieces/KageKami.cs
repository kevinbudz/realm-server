using RotMG.Common;

namespace RotMG.Game.Setpieces
{
    //Ported from realm-src-master wServer/realm/setpieces/KageKami.cs.
    //Master renders the KageKami proto sub-map here; that resource does not
    //exist in this project (see Resources/Worlds/Worlds.xml), so — following
    //the local Hermit/GhostShip precedent — the Kage Kami is summoned
    //directly onto cleared ground. Revisit if the proto map is ever added.
    public class KageKami : ISetPiece
    {
        public int Size { get { return 65; } }

        public void RenderSetPiece(World world, IntPoint pos)
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    SetPieces.ClearTile(world, x + pos.X, y + pos.Y);
            SetPieces.SpawnEnemy(world, "Kage Kami", pos.X + Size / 2f, pos.Y + Size / 2f);
        }
    }
}
