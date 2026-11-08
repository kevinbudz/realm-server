using System;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference ZapBehavior: every Cooldown ms, zaps the nearest enemy within Radius for Damage, applying the
    //condition effect and optional poison. Enemy damage needs a player as the hitter, so this acts only for a Pet, using
    //its owner as the hitter; any other host does nothing.
    public class Zap : Behavior
    {
        public readonly float Radius;
        public readonly int Damage;
        public readonly int Cooldown;
        public readonly uint Color;
        public readonly ConditionEffectIndex Effect;
        public readonly int ConditionDuration;
        public readonly int PoisonTotalDamage;
        public readonly int PoisonDuration;

        public Zap(double radius = 5, int damage = 10, int cooldown = 1000, uint color = 0xFFFFFF, string conditionEffect = null,
            int conditionDuration = 0, int poisonTotalDamage = 0, int poisonDuration = 0)
        {
            Radius = (float)radius;
            Damage = damage;
            Cooldown = cooldown;
            Color = color;
            Effect = BehaviorHelpers.ParseEffect(conditionEffect, "Zap");
            ConditionDuration = conditionDuration;
            PoisonTotalDamage = poisonTotalDamage;
            PoisonDuration = poisonDuration;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = Cooldown;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;
            host.StateCooldown[Id] = Cooldown;

            Player owner = (host as Pet)?.PlayerOwner;
            if (owner == null || owner.Parent == null)
                return false;

            Enemy target = FindTarget(host);
            if (target == null)
                return false;

            //Each packet is broadcast before the next one is built: a PacketWriter span is only valid until the next Rent.
            BehaviorHelpers.BroadcastNearby(host, GameServer.ShowEffect(ShowEffectIndex.Line, target.Id, Color, host.Position));
            BehaviorHelpers.BroadcastNearby(target, GameServer.ShowEffect(ShowEffectIndex.Nova, target.Id, Color, new Position(1, 0)));

            ConditionEffectDesc[] effects = Effect == ConditionEffectIndex.Nothing
                ? new ConditionEffectDesc[0]
                : new[] { new ConditionEffectDesc(Effect, ConditionDuration) };
            target.Damage(owner, Damage, effects, false, true);

            //Poison ticks once per second, as in Player.UseItem's poison grenade: total damage spread over the duration.
            if (PoisonTotalDamage > 0 && PoisonDuration > 0 && target.Parent != null && !target.Dead)
                target.ApplyPoison(owner, new ConditionEffectDesc[0],
                    (int)(PoisonTotalDamage / (PoisonDuration / 1000f)), PoisonTotalDamage);
            return true;
        }

        //Closest enemy within Radius that is not immune to the damage; one already under the zap's effect is skipped.
        private Enemy FindTarget(Entity host)
        {
            Enemy closest = null;
            float radiusSq = Radius * Radius;
            float best = float.MaxValue;
            foreach (Entity en in BehaviorHelpers.NearbyEntities(host, Radius))
            {
                if (!(en is Enemy enemy))
                    continue;
                if (enemy.HasConditionEffect(ConditionEffectIndex.Invincible) ||
                    enemy.HasConditionEffect(ConditionEffectIndex.Invulnerable) ||
                    enemy.HasConditionEffect(ConditionEffectIndex.Stasis))
                    continue;
                if (Effect != ConditionEffectIndex.Nothing && enemy.HasConditionEffect(Effect))
                    continue;

                float d = host.Position.DistanceSquared(enemy.Position);
                if (d <= radiusSq && d < best)
                {
                    best = d;
                    closest = enemy;
                }
            }
            return closest;
        }
    }
}
