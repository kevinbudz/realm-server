using RotMG.Game.Entities;
using RotMG.Networking;

namespace RotMG.Game.Logic.Behaviors
{
    //Broadcasts a GlobalNotification packet to every player in the host's
    //world on state entry (e.g. Davy Jones key HUD updates, see DavyJones.cs).
    public class GlobalNotification : Behavior
    {
        public readonly string Message;

        public GlobalNotification(string message)
        {
            Message = message;
        }

        public override void Enter(Entity host)
        {
            if (host.Parent == null)
                return;
            byte[] packet = GameServer.GlobalNotification(0, Message);
            foreach (Player player in host.Parent.Players.Values)
                player.Client.Send(packet);
        }
    }
}
