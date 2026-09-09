using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Logic.Behaviors
{
    public class RingAttack : Behavior
    {
        public readonly float Radius;
        public readonly int Count;
        public readonly float Offset;
        public readonly int ProjectileIndex;
        public readonly float AngleToIncrement;
        public readonly float? FixedAngle;
        public readonly int Cooldown;

        public RingAttack(double radius, int count, double offset, int projectileIndex, double angleToIncrement, double? fixedAngle = null, int cooldown = 1000)
        {
            Radius = (float)radius;
            Count = count;
            Offset = (float)((double)offset * Math.PI / 180);
            ProjectileIndex = projectileIndex;
            AngleToIncrement = (float)((double)angleToIncrement * Math.PI / 180);
            FixedAngle = fixedAngle == null ? null : (float?)((double)fixedAngle * Math.PI / 180);
            Cooldown = MathUtils.NormalizeCooldown(cooldown, 1000);
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = 0;
            host.StateObject[Id] = 0;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;
            host.StateCooldown[Id] = Cooldown;

            if (host.HasConditionEffect(ConditionEffectIndex.Stunned))
                return false;

            Entity target = Radius <= 0 ? null : host.GetNearestPlayer(Radius);
            float? baseAngle = FixedAngle;
            if (baseAngle == null)
            {
                if (target == null)
                    return true;
                baseAngle = (float)Math.Atan2(target.Position.Y - host.Position.Y, target.Position.X - host.Position.X);
            }

            if (!host.Desc.Projectiles.TryGetValue(ProjectileIndex, out ProjectileDesc desc))
            {
#if DEBUG
                Program.Print(PrintType.Error, $"Missing projectile index <{ProjectileIndex}> for <{host.Desc.DisplayId}>, skipping ring attack.");
#endif
                return true;
            }

            int volleys = (int)host.StateObject[Id];
            host.StateObject[Id] = volleys + 1;
            float angle = (float)baseAngle + Offset + AngleToIncrement * volleys;

            int count = Count;
            if (host.HasConditionEffect(ConditionEffectIndex.Dazed))
                count = Math.Max(1, count / 2);

            float angleInc = (float)(2 * Math.PI / count);
            int damage = desc.Damage;
            int startId = host.Parent.NextProjectileId;
            host.Parent.NextProjectileId += count;

            List<Projectile> projectiles = new List<Projectile>();
            for (int k = 0; k < count; k++)
                projectiles.Add(new Projectile(host, desc, startId + k, Manager.TotalTime, angle + angleInc * k, host.Position, damage));

            byte[] packet = GameServer.EnemyShoot(startId, host.Id, desc.BulletType, host.Position, angle, (short)damage, (byte)count, angleInc);

            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Player.SightRadius))
            {
                if (en is Player player && player.Entities.Contains(host))
                {
                    player.AwaitProjectiles(projectiles);
                    player.Client.Send(packet);
                }
            }
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
            host.StateObject.Remove(Id);
        }
    }
}
