using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Transitions
{
    public class NoEntityWithinTransition : Transition
    {
        public readonly float Distance;

        public NoEntityWithinTransition(int dist, string targetState) : base(targetState)
        {
            Distance = dist;
        }

        public override bool Tick(Entity host)
        {
            if (host.Parent.EntityChunks.AnyInRadius(host.Position, Distance, host))
                return false;
            return !host.Parent.PlayerChunks.AnyInRadius(host.Position, Distance);
        }
    }
}
