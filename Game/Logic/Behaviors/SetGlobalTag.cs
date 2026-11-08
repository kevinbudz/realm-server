using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    //Old setGlobalTag: sets a world tag on Enter and on every tick. With sendNotification, the tag name is
    //broadcast as a global notification (type 0) to every player in the world, first on the first apply and then
    //at most once per NotificationCooldownMs for this entity, as the reference does. The reference's
    //AddInitialNotification list for late joiners is not ported.
    //Set false is the old clearGlobalTag: the world tag is removed on Enter and on every tick (no notification).
    public class SetGlobalTag : Behavior
    {
        public const int NotificationCooldownMs = 3000;

        public readonly string Tag;
        public readonly bool Set;
        public readonly bool SendNotification;

        public SetGlobalTag(string tag, bool set = true, bool sendNotification = false)
        {
            Tag = tag;
            Set = set;
            SendNotification = sendNotification;
        }

        public override void Enter(Entity host) => Apply(host);

        public override bool Tick(Entity host)
        {
            Apply(host);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }

        private void Apply(Entity host)
        {
            World world = host.Parent;
            if (world == null)
                return;

            if (!Set)
            {
                world.RemoveGlobalTag(Tag);
                return;
            }

            world.AddGlobalTag(Tag);
            if (!SendNotification)
                return;

            host.StateCooldown.TryGetValue(Id, out int left);
            left -= Settings.MillisecondsPerTick;
            if (left > 0)
            {
                host.StateCooldown[Id] = left;
                return;
            }

            ReadOnlySpan<byte> packet = GameServer.GlobalNotification(0, Tag);
            foreach (Player player in world.Players.Values)
                player.Client.Send(packet);
            host.StateCooldown[Id] = NotificationCooldownMs;
        }
    }
}
