using RotMG.Networking;

namespace RotMG.Game.Logic.Behaviors
{
    public class AnnounceOnDeath : Behavior
    {
        public readonly string Message;

        public AnnounceOnDeath(string message)
        {
            Message = message;
        }

        public override void Death(Entity host)
        {
            if (host.Parent == null)
                return;
            byte[] packet = GameServer.Text("#" + (host.Desc.DisplayId ?? host.Desc.Id), host.Id, -1, 0, "", Message);
            BehaviorHelpers.BroadcastNearby(host, packet);
        }
    }
}
