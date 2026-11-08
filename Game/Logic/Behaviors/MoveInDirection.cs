using System;
using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class MoveInDirectionState
    {
        public Position Target;
        public int CooldownLeft;
    }

    //Port of the reference MoveBehavior ("Move"): every Cooldown ms picks a point Distance tiles away at Angle degrees
    //from the host's current position, then walks there. Stops early if it cannot make progress.
    public class MoveInDirection : Behavior
    {
        public readonly float Speed;
        public readonly float Distance;
        public readonly float AngleRadians;
        public readonly int Cooldown;

        public MoveInDirection(double speed = 1, double distance = 3, double angle = 0, int cooldown = 10000)
        {
            Speed = (float)speed;
            Distance = (float)distance;
            AngleRadians = (float)(angle * MathUtils.ToRadians);
            Cooldown = cooldown;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new MoveInDirectionState();
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is MoveInDirectionState state))
                return false;
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            if (state.CooldownLeft <= 0)
            {
                state.Target = host.Position + new Position(MathF.Cos(AngleRadians) * Distance, MathF.Sin(AngleRadians) * Distance);
                state.CooldownLeft = Cooldown;
            }
            state.CooldownLeft -= Settings.MillisecondsPerTick;

            float step = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            if (BehaviorHelpers.MoveToward(host, state.Target, step))
                return true;

            state.Target = host.Position;
            return false;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
