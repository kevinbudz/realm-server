using RotMG;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Behaviors
{
    public class Shoot : Behavior
    {
        public readonly float Range;
        public readonly byte Count;
        public readonly float ShootAngle;
        public readonly float? FixedAngle;
        public readonly float? RotateAngle;
        public readonly float AngleOffset;
        public readonly float? DefaultAngle;
        public readonly float Predictive;
        public readonly int Index;
        public readonly int CooldownOffset;
        public readonly int CooldownVariance;
        public readonly int Cooldown;

        public Shoot(
            float range = 5, 
            byte count = 1, 
            float? shootAngle = null, 
            int index = 0, 
            float? fixedAngle = null, 
            float? rotateAngle = null, 
            float angleOffset = 0, 
            float? defaultAngle = null,
            float predictive = 0,
            int cooldownOffset = 0,
            int cooldownVariance = 0,
            int cooldown = 0)
        {
            Range = range;
            Count = count;
            ShootAngle = count == 1 ? 0 : (shootAngle ?? 360f / count) * MathUtils.ToRadians;
            Index = index;
            FixedAngle = fixedAngle * MathUtils.ToRadians;
            RotateAngle = rotateAngle * MathUtils.ToRadians;
            AngleOffset = angleOffset * MathUtils.ToRadians;
            DefaultAngle = defaultAngle * MathUtils.ToRadians;
            Predictive = predictive;
            CooldownOffset = cooldownOffset;
            CooldownVariance = cooldownVariance;
            Cooldown = MathUtils.NormalizeCooldown(cooldown, 1000);
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown.Add(Id, CooldownOffset);
            if (RotateAngle != null) 
                host.StateObject.Add(Id, (int)0);
        }

        public override bool Tick(Entity host)
        {
            if (host.StateCooldown[Id] <= 0)
            {
                if (host.HasConditionEffect(ConditionEffectIndex.Stunned))
                    return false;

                byte count = Count;
                if (host.HasConditionEffect(ConditionEffectIndex.Dazed))
                    count = (byte)Math.Ceiling(count / 2f);

                Entity target = Range <= 0 ? null : host.GetNearestPlayer(Range);
                float? fixedAngle = GetFixedAngle(host);
                if (target != null || DefaultAngle != null || fixedAngle != null)
                {
                    if (!host.Desc.Projectiles.TryGetValue(Index, out ProjectileDesc desc))
                    {
#if DEBUG
                        Program.Print(PrintType.Error, $"Missing projectile index <{Index}> for <{host.Desc.DisplayId}>, skipping shot.");
#endif
                        host.StateCooldown[Id] = Cooldown;
                        return true;
                    }
                    float angle = 0;

                    if (fixedAngle != null)
                    {
                        angle = fixedAngle.Value;
                    }
                    else if (target != null)
                    {
                        angle = (float)Math.Atan2(target.Position.Y - host.Position.Y, target.Position.X - host.Position.X);
                        if (Predictive != 0)
                        {
                            int historyTicks = GetPredictiveHistoryTicks();
                            angle += (float)Predict(
                                host.Position,
                                target.TryGetHistory(historyTicks),
                                target.Position,
                                desc.Speed,
                                Predictive,
                                historyTicks * Settings.MillisecondsPerTick);
                        }
                    }
                    else if (DefaultAngle != null)
                        angle = (float)DefaultAngle;

                    angle += AngleOffset;

                    if (RotateAngle != null)
                    {
                        int rotateCount = (int)host.StateObject[Id];
                        angle += (float)RotateAngle * rotateCount;
                        rotateCount++;
                        host.StateObject[Id] = rotateCount;
                    }

                    int damage = desc.Damage;
                    float startAngle = angle - ShootAngle * (count - 1) / 2;
                    int startId = host.Parent.NextProjectileId;
                    host.Parent.NextProjectileId += count;

                    List<Projectile> projectiles = new List<Projectile>();
                    for (byte k = 0; k < count; k++)
                        projectiles.Add(new Projectile(host, desc, startId + k, Manager.TotalTime, startAngle + (ShootAngle * k), host.Position, damage));

                    byte[] packet = GameServer.EnemyShoot(startId, host.Id, desc.BulletType, host.Position, startAngle, (short)damage, count, ShootAngle);
                    
                    foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Player.SightRadius))
                    {
                        if (en is Player player)
                        {
                            if (player.Entities.Contains(host))
                            {
                                player.AwaitProjectiles(projectiles);
                                player.Client.Send(packet);
                            }
                        }
                    }
                }

                host.StateCooldown[Id] = Cooldown;
                if (CooldownVariance != 0)
                    host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
                return true;
            }

            // Check the timer before decrementing it. This preserves the historical
            // cycle semantics: a timer that reaches zero this tick completes on
            // the next behavior tick.
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            return false;
        }

        protected virtual float? GetFixedAngle(Entity host) => FixedAngle;

        private static int GetPredictiveHistoryTicks()
        {
            // Target history is indexed in ticks, while the reference accepts a
            // time window. Sample the closest available point to 100 ms.
            return Math.Max(1, (int)Math.Round(100.0 / Settings.MillisecondsPerTick));
        }

        protected static double Predict(
            Position origin,
            Position historicalTarget,
            Position currentTarget,
            float projectileSpeed,
            double strength,
            int sampleMilliseconds)
        {
            if (sampleMilliseconds <= 0 || double.IsNaN(strength) || double.IsInfinity(strength))
                return 0;

            double oldAngle = Math.Atan2(historicalTarget.Y - origin.Y, historicalTarget.X - origin.X);
            double currentAngle = Math.Atan2(currentTarget.Y - origin.Y, currentTarget.X - origin.X);
            double angularDelta = currentAngle - oldAngle;

            // Use the shortest signed angular change so crossing the +/-pi seam
            // does not turn a small movement into an almost-full-circle lead.
            angularDelta = Math.Atan2(Math.Sin(angularDelta), Math.Cos(angularDelta));
            double angularVelocity = angularDelta / (sampleMilliseconds / 1000.0);
            return angularVelocity * (projectileSpeed / 100.0) * strength;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
            if (RotateAngle != null)
                host.StateObject.Remove(Id);
        }
    }
}
