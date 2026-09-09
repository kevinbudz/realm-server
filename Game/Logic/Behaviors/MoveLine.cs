using RotMG.Common;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class MoveLine : Behavior
    {
        public readonly float Speed;
        public readonly float Direction;

        public MoveLine(double speed, double direction = 0)
        {
            Speed = (float)speed;
            Direction = (float)direction * MathUtils.ToRadians;
        }

        public override bool Tick(Entity host)
        {
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            Position vect = new Position(MathF.Cos(Direction), MathF.Sin(Direction));
            float dist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            host.ValidateAndMove(host.Position + vect * dist);
            return true;
        }
    }
}
