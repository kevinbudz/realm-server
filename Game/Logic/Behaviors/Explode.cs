using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference ExplodeBehavior: after InitialDelay ms, if a player is within Range (0 = no check), fires a
    //ring of Count projectiles across ArcDegrees and then dies through BehaviorDeath.Die (loot and XP with DropLoot).
    public class Explode : Behavior
    {
        public readonly int ProjectileIndex;
        public readonly int Count;
        public readonly float Range;
        public readonly int InitialDelay;
        public readonly int ArcDegrees;
        public readonly bool DropLoot;
        public readonly bool GiveXP;

        public Explode(int projectileIndex = 0, int count = 12, double range = 5, int initialDelay = 0, int arcDegrees = 360,
            bool dropLoot = false, bool giveXP = false)
        {
            ProjectileIndex = projectileIndex;
            Count = count;
            Range = (float)range;
            InitialDelay = initialDelay;
            ArcDegrees = arcDegrees;
            DropLoot = dropLoot;
            GiveXP = giveXP;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = InitialDelay;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            float arc = ArcDegrees * MathUtils.ToRadians;
            float angle = 0;
            if (Range > 0)
            {
                Entity target = host.GetNearestPlayer(Range);
                if (target == null)
                    return false;
                angle = host.Position.Angle(target.Position) - arc / 2;
            }

            if (Count > 0)
            {
                if (host.Desc.Projectiles.TryGetValue(ProjectileIndex, out ProjectileDesc desc))
                    BehaviorHelpers.SendVolley(host, desc, angle, arc / Count, Count);
                else
                {
#if DEBUG
                    Program.Print(PrintType.Error, $"Explode: missing projectile index <{ProjectileIndex}> for <{host.Desc.DisplayId}>.");
#endif
                }
            }

            BehaviorDeath.Die(host, DropLoot);
            return true;
        }
    }
}
