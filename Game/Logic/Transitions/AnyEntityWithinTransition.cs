using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Transitions
{
    public class AnyEntityWithinTransition : Transition
    {
        public readonly float Distance;

        public AnyEntityWithinTransition(int dist, string targetState) : base(targetState)
        {
            Distance = dist;
        }

        public override bool Tick(Entity host)
        {
            if (host.Parent.EntityChunks.AnyInRadius(host.Position, Distance, host))
                return true;
            return host.Parent.PlayerChunks.AnyInRadius(host.Position, Distance);
        }
    }
}
