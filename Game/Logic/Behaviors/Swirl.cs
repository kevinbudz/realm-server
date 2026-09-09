using RotMG.Common;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class SwirlState
    {
        public Position Center;
        public bool Acquired;
        public int RemainingTime;
    }

    public class Swirl : Behavior
    {
        public readonly float Speed;
        public readonly float Radius;
        public readonly float AcquireRange;
        public readonly bool Targeted;

        public Swirl(double speed = 1, double radius = 8, double acquireRange = 10, bool targeted = true)
        {
            Speed = (float)speed;
            Radius = (float)radius;
            AcquireRange = (float)acquireRange;
            Targeted = targeted;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new SwirlState
            {
                Center = Targeted ? new Position() : host.Position,
                Acquired = !Targeted
            };
        }

        public override bool Tick(Entity host)
        {
            SwirlState state = host.StateObject[Id] as SwirlState;

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            int period = (int)(1000 * Radius / host.GetSpeed(Speed) * (2 * Math.PI));
            if (!state.Acquired && state.RemainingTime <= 0 && Targeted)
            {
                Entity entity = host.GetNearestPlayer(AcquireRange) ?? host.GetNearestEntity(AcquireRange);
                if (entity != null && (entity.Position.X != host.Position.X || entity.Position.Y != host.Position.Y))
                {
                    float l = host.Position.Distance(entity.Position);
                    float hx = (host.Position.X + entity.Position.X) / 2;
                    float hy = (host.Position.Y + entity.Position.Y) / 2;
                    float c = MathF.Sqrt(Math.Abs(Radius * Radius - l * l) / 4);
                    state.Center = new Position(
                        hx + c * (host.Position.Y - entity.Position.Y) / l,
                        hy + c * (entity.Position.X - host.Position.X) / l);
                    state.RemainingTime = period;
                    state.Acquired = true;
                }
                else
                    state.Acquired = false;
            }
            else if (state.RemainingTime <= 0 || (state.RemainingTime - period > 200 && host.GetNearestEntity(2) != null))
            {
                if (Targeted)
                {
                    state.Acquired = false;
                    state.RemainingTime = (host.GetNearestPlayer(AcquireRange) ?? host.GetNearestEntity(AcquireRange)) != null ? 0 : 5000;
                }
                else
                    state.RemainingTime = 5000;
            }
            else
                state.RemainingTime -= Settings.MillisecondsPerTick;

            double angle;
            if (host.Position.Y == state.Center.Y && host.Position.X == state.Center.X)
                angle = Math.Atan2(host.Position.Y - state.Center.Y + MathUtils.NextFloat(-1, 1),
                    host.Position.X - state.Center.X + MathUtils.NextFloat(-1, 1));
            else
                angle = Math.Atan2(host.Position.Y - state.Center.Y, host.Position.X - state.Center.X);

            float spd = host.GetSpeed(Speed) * (state.Acquired ? 1 : 0.2f);
            float angularSpd = spd / Radius;
            angle += angularSpd * Settings.SecondsPerTick;

            Position goal = new Position(
                state.Center.X + MathF.Cos((float)angle) * Radius,
                state.Center.Y + MathF.Sin((float)angle) * Radius);
            Position vect = goal - host.Position;
            vect.Normalize();
            vect *= spd * Settings.SecondsPerTick;
            host.ValidateAndMove(host.Position + vect);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
