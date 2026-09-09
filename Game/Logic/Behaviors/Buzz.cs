using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class BuzzState
    {
        public Position Direction;
        public float RemainingDistance;
    }

    public class Buzz : Behavior
    {
        public readonly float Speed;
        public readonly float Distance;

        public Buzz(double speed = 2, double dist = 0.5)
        {
            Speed = (float)speed;
            Distance = (float)dist;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new BuzzState();
        }

        public override bool Tick(Entity host)
        {
            BuzzState state = host.StateObject[Id] as BuzzState;

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            if (state.RemainingDistance <= 0)
            {
                Position dir;
                do
                {
                    dir = new Position(MathUtils.Next(3) - 1, MathUtils.Next(3) - 1);
                } while (dir.X == 0 && dir.Y == 0);
                dir.Normalize();
                state.Direction = dir;
                state.RemainingDistance = Distance;
            }

            float dist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            host.ValidateAndMove(host.Position + state.Direction * dist);
            state.RemainingDistance -= dist;
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
