using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class HealPlayer : Behavior
    {
        public readonly float Range;
        public readonly int Amount;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public HealPlayer(double range, int cooldown = 1000, int cooldownVariance = 0, int healAmount = 100)
        {
            Range = (float)range;
            Cooldown = cooldown;
            CooldownVariance = cooldownVariance;
            Amount = healAmount;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = 0;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            if (host.HasConditionEffect(ConditionEffectIndex.Stunned))
                return false;

            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Range))
                if (en is Player player)
                    player.Heal(Amount, false);

            host.StateCooldown[Id] = Cooldown;
            if (CooldownVariance != 0)
                host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
