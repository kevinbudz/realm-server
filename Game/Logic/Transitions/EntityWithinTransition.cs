using RotMG.Game.Logic.Behaviors;

namespace RotMG.Game.Logic.Transitions
{
    public class EntityWithinTransition : Transition
    {
        public readonly float Distance;
        public readonly string Entity;

        public EntityWithinTransition(double dist, string entity, string targetState) : base(targetState)
        {
            Distance = (float)dist;
            Entity = entity;
        }

        public override bool Tick(Entity host)
        {
            return BehaviorHelpers.NearestEntityByName(host, Distance, Entity) != null;
        }
    }
}
