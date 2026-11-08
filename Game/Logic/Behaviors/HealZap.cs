using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference HealZapBehavior: every Cooldown ms, heals the nearest wounded living player within Radius by
    //Healing, with a line effect from the healer to the target.
    public class HealZap : Behavior
    {
        public readonly float Radius;
        public readonly int Healing;
        public readonly int Cooldown;
        public readonly uint Color;

        public HealZap(double radius = 5, int healing = 25, int cooldown = 1000, uint color = 0xFFFFFF)
        {
            Radius = (float)radius;
            Healing = healing;
            Cooldown = cooldown;
            Color = color;
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

            Player closest = null;
            float radiusSq = Radius * Radius;
            float best = float.MaxValue;
            foreach (Entity en in BehaviorHelpers.NearbyEntities(host, Radius))
            {
                if (!(en is Player player) || player.HP <= 0 || player.HP >= player.MaxHP)
                    continue;
                float d = host.Position.DistanceSquared(player.Position);
                if (d <= radiusSq && d < best)
                {
                    best = d;
                    closest = player;
                }
            }
            if (closest == null)
                return false;

            BehaviorHelpers.BroadcastNearby(host, GameServer.ShowEffect(ShowEffectIndex.Line, closest.Id, Color, host.Position));
            closest.Heal(Healing, false);
            return true;
        }
    }
}
