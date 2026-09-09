using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class KillPlayer : Behavior
    {
        public readonly string KillMessage;
        public readonly bool KillAll;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public KillPlayer(string killMessage, int cooldown = 1000, int cooldownVariance = 0, bool killAll = false)
        {
            KillMessage = killMessage;
            Cooldown = MathUtils.NormalizeCooldown(cooldown, 1000);
            CooldownVariance = cooldownVariance;
            KillAll = killAll;
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

            Entity target = host.GetNearestPlayer(Player.SightRadius);
            if (target == null)
                return false;

            if (KillAll)
            {
                foreach (Entity en in host.Parent.Players.Values)
                    Kill(host, en as Player);
            }
            else
                Kill(host, target as Player);

            if (KillMessage != null)
            {
                byte[] packet = GameServer.Text("#" + (host.Desc.DisplayId ?? host.Desc.Id), host.Id, -1, 3, "", KillMessage);
                BehaviorHelpers.BroadcastNearby(host, packet, 15);
            }

            host.StateCooldown[Id] = Cooldown;
            if (CooldownVariance != 0)
                host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            return true;
        }

        private void Kill(Entity host, Player player)
        {
            if (player == null || player.Parent == null)
                return;
            BehaviorHelpers.BroadcastNearby(host,
                GameServer.ShowEffect(ShowEffectIndex.Flow, host.Id, 0xffffffff, player.Position));
            player.Death(host.Desc.DisplayId);
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
