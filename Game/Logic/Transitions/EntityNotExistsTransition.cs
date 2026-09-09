using RotMG.Game.Logic.Behaviors;
using RotMG.Utils;

namespace RotMG.Game.Logic.Transitions
{
    public class EntityNotExistsTransition : Transition
    {
        public readonly float Distance;
        public readonly string Target;
        public readonly bool CheckAttackTarget;

        public EntityNotExistsTransition(string target, double dist, string targetState, bool checkAttackTarget = false)
            : base(targetState)
        {
            Target = target;
            Distance = (float)dist;
            CheckAttackTarget = checkAttackTarget;
        }

        public override bool Tick(Entity host)
        {
            if (CheckAttackTarget)
                return host.GetNearestPlayer(Distance) == null;
            if (Target == null)
                return host.GetNearestEntity(Distance) == null;
            return BehaviorHelpers.NearestEntityByName(host, Distance, Target) == null;
        }
    }
}
