using RotMG.Utils;

namespace RotMG.Game.Logic.Transitions
{
    public class NoPlayerWithinTransition : Transition
    {
        public readonly float Distance;

        public NoPlayerWithinTransition(double dist, string targetState) : base(targetState)
        {
            Distance = (float)dist;
        }

        public override bool Tick(Entity host)
        {
            return host.GetNearestPlayer(Distance) == null && host.GetNearestEntity(Distance) == null;
        }
    }
}
