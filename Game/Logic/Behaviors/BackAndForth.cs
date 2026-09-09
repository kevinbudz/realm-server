using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class BackAndForth : Behavior
    {
        public readonly float Speed;
        public readonly int Distance;

        public BackAndForth(double speed, int distance = 5)
        {
            Speed = (float)speed;
            Distance = distance;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = (float)Distance;
        }

        public override bool Tick(Entity host)
        {
            float dist = (float)host.StateObject[Id];

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            float moveDist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            if (dist > 0)
            {
                host.ValidateAndMove(host.Position + new Position(moveDist, 0));
                dist -= moveDist;
                if (dist <= 0)
                    dist = -Distance;
            }
            else
            {
                host.ValidateAndMove(host.Position + new Position(-moveDist, 0));
                dist += moveDist;
                if (dist >= 0)
                    dist = Distance;
            }

            host.StateObject[Id] = dist;
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
