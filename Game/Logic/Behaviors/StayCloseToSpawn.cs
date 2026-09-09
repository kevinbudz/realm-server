using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class StayCloseToSpawn : Behavior
    {
        public readonly float Speed;
        public readonly int Range;

        public StayCloseToSpawn(double speed, int range = 5)
        {
            Speed = (float)speed;
            Range = range;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = host.Position;
        }

        public override bool Tick(Entity host)
        {
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            if (!(host.StateObject[Id] is Position spawn))
            {
                host.StateObject[Id] = host.Position;
                return false;
            }

            Position vect = spawn - host.Position;
            if (host.Position.Distance(spawn) > Range)
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
