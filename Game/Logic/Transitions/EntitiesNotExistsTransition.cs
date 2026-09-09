using RotMG.Game.Logic.Behaviors;
using System.Linq;

namespace RotMG.Game.Logic.Transitions
{
    public class EntitiesNotExistsTransition : Transition
    {
        public readonly float Distance;
        public readonly string[] Targets;

        public EntitiesNotExistsTransition(double dist, string targetState, params string[] targets) : base(targetState)
        {
            Distance = (float)dist;
            Targets = targets;
        }

        public override bool Tick(Entity host)
        {
            if (Targets.Length == 0)
                return false;
            return Targets.All(target => BehaviorHelpers.NearestEntityByName(host, Distance, target) == null);
        }
    }
}
