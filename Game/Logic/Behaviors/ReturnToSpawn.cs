using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class ReturnToSpawn : Behavior
    {
        public readonly float Speed;
        public readonly float ReturnWithinRadius;

        public ReturnToSpawn(double speed, double returnWithinRadius = 1)
        {
            Speed = (float)speed;
            ReturnWithinRadius = (float)returnWithinRadius;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = host.Position;
        }

        public override bool Tick(Entity host)
        {
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            Position spawn = (Position)host.StateObject[Id];
            Position vect = spawn - host.Position;
            if (host.Position.Distance(spawn) > ReturnWithinRadius)
            {
                vect.Normalize();
                vect *= host.GetSpeed(Speed) * Settings.SecondsPerTick;
                host.ValidateAndMove(host.Position + vect);
                return true;
            }

            return false;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
