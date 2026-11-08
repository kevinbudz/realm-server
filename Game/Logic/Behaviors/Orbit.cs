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

    //Port of the reference OrbitBehavior. Also covers the reference CircleBehavior: Circle is Orbit with no Target,
    //zero variances, a random direction (orbitClockwise: null) and reportMoved. Speed and radius variance are absolute
    //amounts here; the converter converts the reference's fractions.
    //Each tick the host steps along the orbit by at most one movement step, never past the orbit point (clamped like
    //MoveToward). Without reportMoved the behavior reports true whenever a target is found; with it, the tick reports
    //whether the host actually moved, which is what a Circle inside Prioritize relied on.
    public class Orbit : Behavior
    {
        public readonly float Speed;
        public readonly float Radius;
        public readonly float AcquireRange;
        public readonly string Target;
        public readonly float SpeedVariance;
        public readonly float RadiusVariance;
        public readonly bool? OrbitClockwise;
        public readonly bool ReportMoved;

        public Orbit(double speed, double radius, double acquireRange = 10, string target = null,
            double? speedVariance = null, double? radiusVariance = null, bool? orbitClockwise = false, bool reportMoved = false)
        {
            Speed = (float)speed;
            Radius = (float)radius;
            AcquireRange = (float)acquireRange;
            Target = target;
            SpeedVariance = (float)(speedVariance ?? speed * 0.1);
            RadiusVariance = (float)(radiusVariance ?? speed * 0.1);
            OrbitClockwise = orbitClockwise;
            ReportMoved = reportMoved;
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

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed) || Radius <= 0 || AcquireRange <= 0)
                return false;

            Entity entity = Target == null
                ? host.GetNearestPlayer(AcquireRange)
                : BehaviorHelpers.NearestEntityByName(host, AcquireRange, Target);

            if (entity == null)
                return false;

            double angle;
            if (host.Position.Y == entity.Position.Y && host.Position.X == entity.Position.X)
                angle = Math.Atan2(host.Position.Y - entity.Position.Y + MathUtils.NextFloat(-1, 1),
                    host.Position.X - entity.Position.X + MathUtils.NextFloat(-1, 1));
            else
                angle = Math.Atan2(host.Position.Y - entity.Position.Y, host.Position.X - entity.Position.X);

            float speed = host.GetSpeed(state.Speed);
            float angularSpd = state.Direction * speed / state.Radius;
            angle += angularSpd * Settings.SecondsPerTick;

            Position goal = new Position(
                entity.Position.X + MathF.Cos((float)angle) * state.Radius,
                entity.Position.Y + MathF.Sin((float)angle) * state.Radius);
            bool moved = BehaviorHelpers.MoveToward(host, goal, speed * Settings.SecondsPerTick);
            return ReportMoved ? moved : true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
