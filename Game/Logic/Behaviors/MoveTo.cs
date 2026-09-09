using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class MoveTo : Behavior
    {
        public readonly float Speed;
        public readonly float X;
        public readonly float Y;

        public MoveTo(float speed, float x, float y)
        {
            Speed = speed;
            X = x;
            Y = y;
        }

        public override bool Tick(Entity host)
        {
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            Position path = new Position(X - host.Position.X, Y - host.Position.Y);
            float dist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            if (host.Position.Distance(new Position(X, Y)) <= dist)
            {
                host.ValidateAndMove(new Position(X, Y));
                return false;
            }

            path.Normalize();
            host.ValidateAndMove(host.Position + path * dist);
            return true;
        }
    }
}
