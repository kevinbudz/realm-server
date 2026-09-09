using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class StayBack : Behavior
    {
        public readonly float Speed;
        public readonly float Distance;
        public readonly string Entity;

        public StayBack(double speed, double distance = 8, string entity = null)
        {
            Speed = (float)speed;
            Distance = (float)distance;
            Entity = entity;
        }

        public override bool Tick(Entity host)
        {
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            Entity target = Entity != null
                ? BehaviorHelpers.NearestEntityByName(host, Distance, Entity)
                : host.GetNearestPlayer(Distance) ?? host.GetNearestEntity(Distance);

            if (target == null)
                return false;

            Position vect = target.Position - host.Position;
            vect.Normalize();
            float dist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            host.ValidateAndMove(host.Position + vect * -dist);
            return true;
        }
    }
}
