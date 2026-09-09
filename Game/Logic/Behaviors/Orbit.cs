using RotMG.Common;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class OrbitState
    {
        public float Speed;
        public float Radius;
        public int Direction;
    }

    public class Orbit : Behavior
    {
        public readonly float Speed;
        public readonly float Radius;
        public readonly float AcquireRange;
        public readonly string Target;
        public readonly float SpeedVariance;
        public readonly float RadiusVariance;
        public readonly bool? OrbitClockwise;

        public Orbit(double speed, double radius, double acquireRange = 10, string target = null,
            double? speedVariance = null, double? radiusVariance = null, bool? orbitClockwise = false)
        {
            Speed = (float)speed;
            Radius = (float)radius;
            AcquireRange = (float)acquireRange;
            Target = target;
            SpeedVariance = (float)(speedVariance ?? speed * 0.1);
            RadiusVariance = (float)(radiusVariance ?? speed * 0.1);
            OrbitClockwise = orbitClockwise;
        }

        public override void Enter(Entity host)
        {
            int dir;
            if (OrbitClockwise == null)
                dir = MathUtils.NextBool() ? 1 : -1;
            else
                dir = OrbitClockwise.Value ? 1 : -1;

            host.StateObject[Id] = new OrbitState
            {
                Speed = Speed + SpeedVariance * (MathUtils.NextFloat(-1, 1)),
                Radius = Radius + RadiusVariance * (MathUtils.NextFloat(-1, 1)),
                Direction = dir
            };
        }

        public override bool Tick(Entity host)
        {
            OrbitState state = host.StateObject[Id] as OrbitState;

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            Entity entity = Target == null
                ? host.GetNearestPlayer(AcquireRange) ?? host.GetNearestEntity(AcquireRange)
                : BehaviorHelpers.NearestEntityByName(host, AcquireRange, Target);

            if (entity == null)
                return false;

            double angle;
            if (host.Position.Y == entity.Position.Y && host.Position.X == entity.Position.X)
                angle = Math.Atan2(host.Position.Y - entity.Position.Y + MathUtils.NextFloat(-1, 1),
                    host.Position.X - entity.Position.X + MathUtils.NextFloat(-1, 1));
            else
                angle = Math.Atan2(host.Position.Y - entity.Position.Y, host.Position.X - entity.Position.X);

            float angularSpd = state.Direction * host.GetSpeed(state.Speed) / state.Radius;
            angle += angularSpd * Settings.SecondsPerTick;

            Position goal = new Position(
                entity.Position.X + MathF.Cos((float)angle) * state.Radius,
                entity.Position.Y + MathF.Sin((float)angle) * state.Radius);
            Position vect = goal - host.Position;
            vect.Normalize();
            vect *= host.GetSpeed(state.Speed) * Settings.SecondsPerTick;
            host.ValidateAndMove(host.Position + vect);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
