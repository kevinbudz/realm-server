using System;
using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference KeepDistanceBehavior: with a target inside AcquireRange (the nearest player, or the nearest
    //entity named Entity), walks toward it when farther than Distance and away when closer, stopping within Deadband of
    //Distance. Movement step follows the existing movement ports: GetSpeed(Speed) * SecondsPerTick tiles per tick.
    //Approach false is the reference StayBackBehavior (converter: approach false, deadband 0, acquireRange = distance):
    //every tick it steps straight away from the target by the full step, without the clamp at the target distance, and
    //reports true whenever it had a target, moved or not.
    public class KeepDistance : Behavior
    {
        public readonly float Speed;
        public readonly float Distance;
        public readonly float AcquireRange;
        public readonly bool Approach;
        public readonly float Deadband;
        public readonly string EntityName;

        public KeepDistance(double speed = 1, double distance = 10, double acquireRange = 15, bool approach = true,
            double deadband = 1, string entity = null)
        {
            Speed = (float)speed;
            Distance = (float)distance;
            AcquireRange = (float)acquireRange;
            Approach = approach;
            Deadband = (float)deadband;
            EntityName = entity;
        }

        public override bool Tick(Entity host)
        {
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed) || AcquireRange <= 0)
                return false;

            Entity target = EntityName != null
                ? BehaviorHelpers.NearestEntityByName(host, AcquireRange, EntityName)
                : host.GetNearestPlayer(AcquireRange);
            if (target == null)
                return false;

            float dist = host.Position.Distance(target.Position);
            if (Math.Abs(dist - Distance) < Deadband)
                return false;

            if (!Approach)
            {
                Position vect = target.Position - host.Position;
                vect.Normalize();
                float away = host.GetSpeed(Speed) * Settings.SecondsPerTick;
                host.ValidateAndMove(host.Position + vect * -away);
                return true;
            }

            Position goal = dist > Distance
                ? target.Position
                : new Position(2 * host.Position.X - target.Position.X, 2 * host.Position.Y - target.Position.Y);
            float step = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            return BehaviorHelpers.MoveToward(host, goal, step);
        }
    }
}
