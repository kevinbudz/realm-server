using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class MutePlayer : Behavior
    {
        public readonly int DurationMin;

        public MutePlayer(int durationMin = 0)
        {
            DurationMin = durationMin;
        }

        public override void Enter(Entity host)
        {
            Entity target = host.GetNearestPlayer(Player.SightRadius);
            if (target == null || !(target is Player player))
                return;
            if (player.Parent == null || player.Client.Account.Muted)
                return;

            player.Client.Account.Muted = true;
            if (DurationMin > 0)
                Manager.AddTimedAction(DurationMin * 60000, () => player.Client.Account.Muted = false);
        }
    }
}
