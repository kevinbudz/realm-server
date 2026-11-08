using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference TrapTriggerBehavior. Each shot drops a trap at the host's position (plus jitter) that
    //detonates at once: enemies within the (varied) radius take the damage and condition effect, and a nova is shown.
    //The detonation reuses the Trap entity's world placement, but the nova uses this behavior's Color (the Trap entity
    //has a fixed color). Detonation damage needs a player hitter, so this acts only for a Pet (its owner is the hitter).
    //SoundId is not ported: the PlaySound packet here takes a sound name, not a numeric id.
    public class TrapTrigger : Behavior
    {
        public readonly float Radius;
        public readonly float RadiusVariability;
        public readonly int MinDamage;
        public readonly int MaxDamage;
        public readonly float PosJitter;
        public readonly ConditionEffectIndex Effect;
        public readonly int CondDuration;
        public readonly int Cooldown;
        public readonly int CooldownJitter;
        public readonly int SoundId;
        public readonly uint Color;

        public TrapTrigger(double radius = 1, double radiusVariability = 0, int minDamage = 10, int maxDamage = 10,
            double posJitter = 0, string condEffect = null, int condDuration = 5000, int cooldown = 3000,
            int cooldownJitter = 0, int soundId = -1, uint color = 0xaa00ff)
        {
            Radius = (float)radius;
            RadiusVariability = (float)radiusVariability;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            PosJitter = (float)posJitter;
            Effect = BehaviorHelpers.ParseEffect(condEffect, "TrapTrigger");
            CondDuration = condDuration;
            Cooldown = cooldown;
            CooldownJitter = cooldownJitter;
            SoundId = soundId;
            Color = color;
        }

        public override void Enter(Entity host)
        {
            //The reference starts with tilFire_ = 0, so the first trap fires on the first tick.
            host.StateCooldown[Id] = 0;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            int next = Cooldown;
            if (CooldownJitter > 0)
                next += (int)MathUtils.NextFloat(-CooldownJitter, CooldownJitter);
            host.StateCooldown[Id] = next;

            Player owner = (host as Pet)?.PlayerOwner;
            if (owner == null || owner.Parent == null || host.Parent == null)
                return false;

            Position at = host.Position;
            if (PosJitter > 0)
                at = at + MathUtils.Position(PosJitter, PosJitter);
            float radius = Radius + MathUtils.NextFloat(-Radius * RadiusVariability, Radius * RadiusVariability);
            int damage = MaxDamage > MinDamage ? MathUtils.NextInt(MinDamage, MaxDamage + 1) : MinDamage;
            ConditionEffectDesc[] effects = Effect == ConditionEffectIndex.Nothing
                ? new ConditionEffectDesc[0]
                : new[] { new ConditionEffectDesc(Effect, CondDuration) };

            Detonate(host.Parent, owner, at, radius, damage, effects);
            return true;
        }

        private void Detonate(World world, Player owner, Position at, float radius, int damage, ConditionEffectDesc[] effects)
        {
            Trap trap = new Trap(owner, radius, damage, effects);
            if (world.AddEntity(trap, at) == -1)
                return;

            List<Enemy> victims = new List<Enemy>();
            foreach (Entity en in world.EntityChunks.HitTest(at, radius))
            {
                if (en is Enemy enemy &&
                    !enemy.HasConditionEffect(ConditionEffectIndex.Invincible) &&
                    !enemy.HasConditionEffect(ConditionEffectIndex.Stasis))
                    victims.Add(enemy);
            }
            foreach (Enemy enemy in victims)
            {
                if (enemy.Parent != null && !enemy.Dead)
                    enemy.Damage(owner, damage, effects, false, true);
            }

            byte[] nova = GameServer.ShowEffect(ShowEffectIndex.Nova, trap.Id, Color, new Position(radius, 0)).ToArray();
            foreach (Entity en in world.PlayerChunks.HitTest(at, Player.SightRadius))
            {
                if (en is Player player && (player.Client.Account.Effects || player.Equals(owner)))
                    player.Client.Send(nova);
            }

            world.RemoveEntity(trap);
        }
    }
}
