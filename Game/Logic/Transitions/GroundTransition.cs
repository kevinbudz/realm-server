using RotMG.Common;

namespace RotMG.Game.Logic.Transitions
{
    public class GroundTransition : Transition
    {
        public readonly string Ground;

        public GroundTransition(string ground, string targetState) : base(targetState)
        {
            Ground = ground;
        }

        public override bool Tick(Entity host)
        {
            if (!Resources.Id2Tile.TryGetValue(Ground, out TileDesc desc))
                return false;
            Tile? tile = host.Parent.GetTile((int)host.Position.X, (int)host.Position.Y);
            return tile != null && tile.Value.Type == desc.Type;
        }
    }
}
