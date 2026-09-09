using RotMG.Common;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class Reproduce : Behavior
    {
        public readonly string Children;
        public readonly float DensityRadius;
        public readonly int DensityMax;
        public readonly int Cooldown;
        public readonly int CooldownVariance;
        public readonly Region Region;
        public readonly float RegionRange;

        public Reproduce(string children = null, double densityRadius = 10, int densityMax = 5,
            int cooldown = 60000, int cooldownVariance = 0,
            Region region = Region.None, double regionRange = 10)
        {
            Children = children;
            DensityRadius = (float)densityRadius;
            DensityMax = densityMax;
            Cooldown = MathUtils.NormalizeCooldown(cooldown, 60000);
            CooldownVariance = cooldownVariance;
            Region = region;
            RegionRange = (float)regionRange;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = NextCooldown();
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
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            ushort type;
            if (Children == null)
                type = host.Type;
            else if (!BehaviorHelpers.TryGetObjType(Children, out type))
                return false;

            if (BehaviorHelpers.CountEntities(host, DensityRadius, type) >= DensityMax)
            {
                host.StateCooldown[Id] = NextCooldown();
                return false;
            }

            Position at = host.Position;
            if (Region != Region.None)
            {
                IntPoint p = host.Parent.GetRegion(Region);
                if (Math.Abs(p.X - host.Position.X) > RegionRange ||
                    Math.Abs(p.Y - host.Position.Y) > RegionRange)
                {
                    host.StateCooldown[Id] = NextCooldown();
                    return false;
                }
                at = new Position(p.X, p.Y);
            }

            if (host.Parent.GetTileF(at.X, at.Y) == null)
            {
                host.StateCooldown[Id] = NextCooldown();
                return false;
            }

            bool spawned = BehaviorHelpers.SpawnChild(host, type, at) != null;
            host.StateCooldown[Id] = NextCooldown();
            return spawned;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
