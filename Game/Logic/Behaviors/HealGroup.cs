using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class HealGroup : Behavior
    {
        public readonly float Range;
        public readonly string Group;
        public readonly int? Amount;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public HealGroup(double range, string group, int cooldown = 1000, int cooldownVariance = 0, int? healAmount = null)
        {
            Range = (float)range;
            Group = group;
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

            foreach (Entity entity in BehaviorHelpers.EntitiesByGroup(host, Range, Group))
            {
                if (!(entity is Enemy))
                    continue;
                int maxHp = entity.Desc.MaxHP;
                int newHp = Amount != null ? Math.Min(maxHp, entity.HP + Amount.Value) : maxHp;
                if (newHp == entity.HP)
                    continue;

                int n = newHp - entity.HP;
                entity.HP = newHp;
                BehaviorHelpers.BroadcastNearby(entity, GameServer.ShowEffect(ShowEffectIndex.Heal, entity.Id, 0xffffffff));
                BehaviorHelpers.BroadcastNearby(host, GameServer.ShowEffect(ShowEffectIndex.Flow, host.Id, 0xffffffff, entity.Position));
                BehaviorHelpers.BroadcastNearby(entity, GameServer.Notification(entity.Id, "+" + n, 0xff00ff00));
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
