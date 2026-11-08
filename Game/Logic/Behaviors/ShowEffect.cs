using System;
using RotMG.Common;
using RotMG.Networking;

namespace RotMG.Game.Logic.Behaviors
{
    public class ShowEffectState
    {
        public bool Sent;
    }

    //Port of the reference ShowEffectBehavior: sends one visual effect on the host per state entry. Flash also carries
    //FlashPeriod and FlashRepeats. The type name is matched case-insensitively against ShowEffectIndex; an unknown name
    //sends nothing.
    public class ShowEffect : Behavior
    {
        public readonly ShowEffectIndex Effect;
        public readonly uint Color;
        public readonly float FlashPeriod;
        public readonly int FlashRepeats;

        public ShowEffect(string type = "Flash", uint color = 0xFFFFFF, double flashPeriod = 0.75, int flashRepeats = 1)
        {
            Effect = ParseEffect(type);
            Color = color;
            FlashPeriod = (float)flashPeriod;
            FlashRepeats = flashRepeats;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new ShowEffectState();
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is ShowEffectState state))
                return false;
            if (state.Sent)
                return true;
            state.Sent = true;

            if (Effect == ShowEffectIndex.Unknown || host.Parent == null)
                return true;

            ReadOnlySpan<byte> packet = Effect == ShowEffectIndex.Flash
                ? GameServer.ShowEffect(Effect, host.Id, Color, new Position(FlashPeriod, FlashRepeats))
                : GameServer.ShowEffect(Effect, host.Id, Color);
            BehaviorHelpers.BroadcastNearby(host, packet);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }

        private static ShowEffectIndex ParseEffect(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return ShowEffectIndex.Unknown;
            foreach (ShowEffectIndex index in Enum.GetValues(typeof(ShowEffectIndex)))
                if (index != ShowEffectIndex.Unknown && string.Equals(index.ToString(), type.Trim(), StringComparison.OrdinalIgnoreCase))
                    return index;
#if DEBUG
            Program.Print(PrintType.Error, $"ShowEffect: unknown effect type <{type}>.");
#endif
            return ShowEffectIndex.Unknown;
        }
    }
}
