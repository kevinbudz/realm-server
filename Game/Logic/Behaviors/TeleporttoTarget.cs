using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Hand-written toolkit: not emitted by Tools/XmlBehaviorConverter (files/xml has no equivalent).
    public class TeleporttoTarget : Behavior
    {
        public readonly float Range;

        public TeleporttoTarget(double range)
        {
            Range = (float)range;
        }

        public override bool Tick(Entity host)
        {
            Entity player = host.GetNearestPlayer(Player.SightRadius);
            if (player == null)
                return false;

            if (host.Position.Distance(player.Position) > Range)
            {
                host.Parent.MoveEntity(host, player.Position);
                return true;
            }

            return false;
        }
    }
}
