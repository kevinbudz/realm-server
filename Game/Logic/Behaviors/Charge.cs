using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class ChargeState
    {
        public Position Direction;
        public int RemainingTime;
    }

    public class Charge : Behavior
    {
        public readonly float Speed;
        public readonly float Range;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public Charge(double speed = 4, float range = 10, int cooldown = 2000, int cooldownVariance = 0)
        {
            Speed = (float)speed;
            Range = range;
            Cooldown = MathUtils.NormalizeCooldown(cooldown, 2000);
            CooldownVariance = cooldownVariance;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new ChargeState();
        }

        private int NextCooldown()
        {
            int cool = Cooldown;
            if (CooldownVariance != 0)
                cool += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            return cool;
        }

        public override bool Tick(Entity host)
        {
            ChargeState state = host.StateObject[Id] as ChargeState;

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            if (state.RemainingTime <= 0)
            {
                if (state.Direction.X == 0 && state.Direction.Y == 0)
                {
                    Entity player = host.GetNearestPlayer(Range) ?? host.GetNearestEntity(Range);
                    if (player != null && (player.Position.X != host.Position.X || player.Position.Y != host.Position.Y))
                    {
                        state.Direction = player.Position - host.Position;
                        float d = host.Position.Distance(player.Position);
                        state.Direction.Normalize();
                        state.RemainingTime = (int)(d / host.GetSpeed(Speed) * 1000);
                    }
                }
                else
                {
                    state.Direction = new Position();
                    state.RemainingTime = NextCooldown();
                    return false;
                }
            }

            if (state.Direction.X != 0 || state.Direction.Y != 0)
            {
                float dist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
                host.ValidateAndMove(host.Position + state.Direction * dist);
                state.RemainingTime -= Settings.MillisecondsPerTick;
                return true;
            }

            state.RemainingTime -= Settings.MillisecondsPerTick;
            return false;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
