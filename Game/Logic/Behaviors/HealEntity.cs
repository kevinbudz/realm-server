using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class HealEntity : Behavior
    {
        public readonly float Range;
        public readonly string Name;
        public readonly int? Amount;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public HealEntity(double range, string name = null, int? healAmount = null, int cooldown = 1000, int cooldownVariance = 0)
        {
            Range = (float)range;
            Name = name;
            Amount = healAmount;
            Cooldown = cooldown;
            CooldownVariance = cooldownVariance;
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

            foreach (Entity entity in BehaviorHelpers.EntitiesByName(host, Range, Name))
            {
                if (!(entity is Enemy))
                    continue;
                Heal(host, entity);
            }

            host.StateCooldown[Id] = Cooldown;
            if (CooldownVariance != 0)
                host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            return true;
        }

        private void Heal(Entity host, Entity entity)
        {
            int maxHp = entity.Desc.MaxHP;
            int newHp = maxHp;
            if (Amount != null)
                newHp = Math.Min(maxHp, entity.HP + Amount.Value);
            if (newHp == entity.HP)
                return;

            int n = newHp - entity.HP;
            entity.HP = newHp;
            BehaviorHelpers.BroadcastNearby(entity, GameServer.ShowEffect(ShowEffectIndex.Heal, entity.Id, 0xffffffff));
            BehaviorHelpers.BroadcastNearby(host, GameServer.ShowEffect(ShowEffectIndex.Flow, host.Id, 0xffffffff, entity.Position));
            BehaviorHelpers.BroadcastNearby(entity, GameServer.Notification(entity.Id, "+" + n, 0xff00ff00));
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
