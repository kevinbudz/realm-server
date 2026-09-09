using RotMG.Game.Logic.Behaviors;

namespace RotMG.Game.Logic.Transitions
{
    public class EntityHpLessTransition : Transition
    {
        public readonly float Distance;
        public readonly string Entity;
        public readonly float Threshold;

        public EntityHpLessTransition(double dist, string entity, double threshold, string targetState)
            : base(targetState)
        {
            Distance = (float)dist;
            Entity = entity;
            Threshold = (float)threshold;
        }

        public override bool Tick(Entity host)
        {
            Entity entity = BehaviorHelpers.NearestEntityByName(host, Distance, Entity);
            if (entity == null || entity.MaxHP <= 0)
                return false;
            return (float)entity.HP / entity.MaxHP < Threshold;
        }
    }
}
