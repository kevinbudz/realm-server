using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class HealSelf : Behavior
    {
        public readonly int? Amount;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public HealSelf(int cooldown = 1000, int cooldownVariance = 0, int? amount = null)
        {
            Cooldown = MathUtils.NormalizeCooldown(cooldown, 1000);
            CooldownVariance = cooldownVariance;
            Amount = amount;
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

            int maxHp = host.Desc.MaxHP;
            int newHp = Amount != null ? Math.Min(maxHp, host.HP + Amount.Value) : maxHp;
            if (newHp != host.HP)
            {
                int n = newHp - host.HP;
                host.HP = newHp;
                BehaviorHelpers.BroadcastNearby(host, GameServer.ShowEffect(ShowEffectIndex.Heal, host.Id, 0xffffffff));
                BehaviorHelpers.BroadcastNearby(host, GameServer.Notification(host.Id, "+" + n, 0xff00ff00));
            }

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
