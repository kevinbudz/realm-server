using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class Taunt : Behavior
    {
        public readonly float Probability;
        public readonly bool Broadcast;
        public readonly int Cooldown;
        public readonly int CooldownVariance;
        public readonly string[] Text;

        public Taunt(params string[] text)
            : this(1, false, 0, 0, text) { }

        public Taunt(double probability, params string[] text)
            : this(probability, false, 0, 0, text) { }

        public Taunt(bool broadcast, params string[] text)
            : this(1, broadcast, 0, 0, text) { }

        public Taunt(double probability, bool broadcast, params string[] text)
            : this(probability, broadcast, 0, 0, text) { }

        public Taunt(int cooldown, params string[] text)
            : this(1, false, cooldown, 0, text) { }

        public Taunt(double probability, int cooldown, params string[] text)
            : this(probability, false, cooldown, 0, text) { }

        public Taunt(bool broadcast, int cooldown, params string[] text)
            : this(1, broadcast, cooldown, 0, text) { }

        public Taunt(double probability, bool broadcast, int cooldown, params string[] text)
            : this(probability, broadcast, cooldown, 0, text) { }

        public Taunt(double probability, bool broadcast, int cooldown, int cooldownVariance, params string[] text)
        {
            Probability = (float)probability;
            Broadcast = broadcast;
            Cooldown = cooldown;
            CooldownVariance = cooldownVariance;
            Text = text;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = 0;
        }

        public override bool Tick(Entity host)
        {
            if (Cooldown == 0 && host.StateCooldown.TryGetValue(Id, out int fired) && fired == -1)
                return false;

            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            host.StateCooldown[Id] = Cooldown;
            if (CooldownVariance != 0)
                host.StateCooldown[Id] += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            if (Cooldown == 0)
                host.StateCooldown[Id] = -1;

            if (Text.Length == 0 || !MathUtils.Chance(Probability))
                return false;

            string taunt = Text[MathUtils.Next(Text.Length)];
            if (taunt.Contains("{PLAYER}"))
            {
                Entity player = host.GetNearestPlayer(10);
                if (player == null || !(player is Player p))
                    return false;
                taunt = taunt.Replace("{PLAYER}", p.Name);
            }
            taunt = taunt.Replace("{HP}", host.HP.ToString());

            byte[] packet = GameServer.Text("#" + (host.Desc.DisplayId ?? host.Desc.Id), host.Id, -1, 3, "", taunt);
            if (Broadcast && host.Parent != null)
            {
                foreach (Player player in host.Parent.Players.Values)
                    player.Client.Send(packet);
            }
            else
                BehaviorHelpers.BroadcastNearby(host, packet, 15);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
