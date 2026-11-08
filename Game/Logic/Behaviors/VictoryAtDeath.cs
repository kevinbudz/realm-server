using RotMG.Common;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference VictoryAtDeathBehavior: on death, drops the exit portal 0.2 tiles off the corpse (Oryx 1 drops
    //the wine cellar portal). The reference also increments an "oryx_kills" stat; this server has no such counter, so
    //that part is not ported.
    public class VictoryAtDeath : Behavior
    {
        public override void Death(Entity host)
        {
            if (host.Parent == null)
                return;

            string portalId = "Portal to Nexus";
            if (BehaviorHelpers.TryGetObjType("Oryx the Mad God 1", out ushort oryxType) && host.Type == oryxType)
                portalId = "Locked Wine Cellar Portal";

            if (BehaviorHelpers.TryGetObjType(portalId, out ushort portalType))
                BehaviorHelpers.SpawnChild(host.Parent, portalType, host.Position + new Position(0.2f, 0.2f));
        }
    }
}
