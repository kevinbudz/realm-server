using RotMG.Common;
using RotMG.Utils;
using System;
using System.Linq;

namespace RotMG.Game.Logic.Behaviors
{
    public class ReproduceGroup : Behavior
    {
        public readonly float DensityRadius;
        public readonly int DensityMax;
        public readonly string Group;
        public readonly int Cooldown;
        public readonly int CooldownVariance;
        public readonly Region Region;
        public readonly float RegionRange;

        public ReproduceGroup(double densityRadius, int densityMax, string group,
            int cooldown = 60000, int cooldownVariance = 0,
            Region region = Region.None, double regionRange = 10)
        {
            DensityRadius = (float)densityRadius;
            DensityMax = densityMax;
            Group = group;
            Cooldown = cooldown;
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

            ushort[] types = BehaviorHelpers.GetGroupTypes(Group);
            if (types.Length == 0)
                return false;

            int count = 0;
            foreach (Entity en in host.Parent.EntityChunks.HitTest(host.Position, DensityRadius))
                if (types.Contains(en.Type))
                    count++;
            if (count >= DensityMax)
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

            bool spawned = BehaviorHelpers.SpawnChild(host, types[MathUtils.Next(types.Length)], at) != null;
            host.StateCooldown[Id] = NextCooldown();
            return spawned;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
