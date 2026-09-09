using RotMG.Game.Logic.Behaviors;

namespace RotMG.Game.Logic.Transitions
{
    public class EntityExistsTransition : Transition
    {
        public readonly float Distance;
        public readonly string Target;

        public EntityExistsTransition(string target, double dist, string targetState) : base(targetState)
        {
            Target = target;
            Distance = (float)dist;
        }

        public override bool Tick(Entity host)
        {
            return BehaviorHelpers.NearestEntityByName(host, Distance, Target) != null;
        }
    }
}
