using RotMG.Common;
using RotMG.Game.Entities;

namespace RotMG.Game.Dungeons.Candyland
{
    //Hunting-ground instance of Candyland. The .jm has decorations and
    //Candyland Spawners only; hunt enemies are placed on open walkable
    //tiles when the first player joins.
    public class World : DungeonWorld
    {
        public Overseer Overseer;

        public World(WorldDesc desc, int seed)
            : base(desc, seed)
        {
            Overseer = new Overseer(this);
        }

        protected override void OnTick()
        {
            Overseer.Tick();
        }

        protected override void OnPlayerEntered(Player player)
        {
            Overseer.OnPlayerEntered(player);
        }
    }
}
