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

        public override bool Tick(Entity host)
        {
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            //Walk back to where the entity was placed, not where it was
            //when this state was entered (which made this a no-op).
            //Mirrors realm-src-master ReturnToSpawn -> Enemy.SpawnPoint.
            Position spawn = host.SpawnPoint;
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
    }
}
