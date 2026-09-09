using RotMG.Common;
using RotMG.Networking;

namespace RotMG.Game.Logic.Behaviors
{
    public class Flash : Behavior
    {
        public readonly uint Color;
        public readonly float FlashPeriod;
        public readonly int FlashRepeats;

        public Flash(uint color, double flashPeriod, int flashRepeats)
        {
            Color = color;
            FlashPeriod = (float)flashPeriod;
            FlashRepeats = flashRepeats;
        }

        public override void Enter(Entity host)
        {
            BehaviorHelpers.BroadcastNearby(host, GameServer.ShowEffect(
                ShowEffectIndex.Flash, host.Id, Color, new Position(FlashPeriod, FlashRepeats)));
        }
    }
}
