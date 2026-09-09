using RotMG.Common;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class TossObject : Behavior
    {
        public readonly string Child;
        public readonly float Range;
        public readonly float? Angle;
        public readonly int Cooldown;
        public readonly int CooldownVariance;
        public readonly int CooldownOffset;
        public readonly float Probability;
        public readonly string Group;
        public readonly float? MinRange;
        public readonly float? MaxRange;
        public readonly float? MinAngle;
        public readonly float? MaxAngle;
        public readonly float? DensityRange;
        public readonly int? MaxDensity;
        public readonly Region Region;
        public readonly float RegionRange;

        public TossObject(string child, double range = 5, double? angle = null,
            int cooldown = 1000, int cooldownVariance = 0, int cooldownOffset = 0,
            float probability = 1, string group = null,
            double? minAngle = null, double? maxAngle = null,
            double? minRange = null, double? maxRange = null,
            double? densityRange = null, int? maxDensity = null,
            Region region = Region.None, double regionRange = 10)
        {
            Child = child;
            Range = (float)range;
            Angle = angle == null ? null : (float?)(angle.Value * MathUtils.ToRadians);
            Cooldown = MathUtils.NormalizeCooldown(cooldown, 1000);
            CooldownVariance = cooldownVariance;
            CooldownOffset = cooldownOffset;
            Probability = probability;
            Group = group;
            MinAngle = minAngle == null ? null : (float?)(minAngle.Value * MathUtils.ToRadians);
            MaxAngle = maxAngle == null ? null : (float?)(maxAngle.Value * MathUtils.ToRadians);
            MinRange = (float?)minRange;
            MaxRange = (float?)maxRange;
            DensityRange = (float?)densityRange;
            MaxDensity = maxDensity;
            Region = region;
            RegionRange = (float)regionRange;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = CooldownOffset;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            host.StateCooldown[Id] = Cooldown;
            if (CooldownVariance != 0)
                host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);

            if (!MathUtils.Chance(Probability))
                return false;

            ushort type;
            if (Group != null)
            {
                ushort[] types = BehaviorHelpers.GetGroupTypes(Group);
                if (types.Length == 0)
                    return false;
                type = types[MathUtils.Next(types.Length)];
            }
            else
            {
                if (!BehaviorHelpers.TryGetObjType(Child, out type))
                    return false;
            }

            if (DensityRange != null && MaxDensity != null &&
                BehaviorHelpers.CountEntities(host, DensityRange.Value, type) >= MaxDensity.Value)
                return false;

            Position at;
            if (Region != Region.None)
            {
                IntPoint p = host.Position.ToIntPoint();
                for (int i = 0; i < 5; i++)
                {
                    p = host.Parent.GetRegion(Region);
                    if (Math.Abs(p.X - host.Position.X) <= RegionRange &&
                        Math.Abs(p.Y - host.Position.Y) <= RegionRange)
                        break;
                }
                at = new Position(p.X, p.Y);
            }
            else
            {
                float range = MaxRange != null
                    ? MathUtils.NextFloat(MinRange ?? 0, MaxRange.Value)
                    : Range;
                float angle = Angle ??
                    (MinAngle != null || MaxAngle != null
                        ? MathUtils.NextFloat(MinAngle ?? 0, MaxAngle ?? (2 * MathF.PI))
                        : MathUtils.NextAngle());
                at = host.Position + new Position(MathF.Cos(angle) * range, MathF.Sin(angle) * range);
            }

            return BehaviorHelpers.SpawnChild(host, type, at) != null;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
