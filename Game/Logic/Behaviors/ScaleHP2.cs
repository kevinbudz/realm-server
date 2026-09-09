using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;
using System.Collections.Generic;

namespace RotMG.Game.Logic.Behaviors
{
    public class ScaleHP2 : Behavior
    {
        private class ScaleState
        {
            public HashSet<string> Counted = new HashSet<string>();
            public int ScaledTo;
        }

        public readonly int Percentage;
        public readonly int ScaleAfter;
        public readonly float Range;

        public ScaleHP2(int amount, int scaleStart = 0, double range = 25.0)
        {
            Percentage = amount;
            ScaleAfter = scaleStart;
            Range = (float)range;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new ScaleState { ScaledTo = ScaleAfter };
            host.StateCooldown[Id] = 0;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;
            host.StateCooldown[Id] = 1000;

            if (!(host.StateObject.TryGetValue(Id, out object obj) && obj is ScaleState state))
                return false;

            foreach (Player player in host.Parent.Players.Values)
            {
                if (player.Position.Distance(host.Position) > Range)
                    continue;
                state.Counted.Add(player.Name);
            }

            int counted = state.Counted.Count;
            if (counted > state.ScaledTo)
            {
                int boost = (counted - state.ScaledTo) * Percentage * host.Desc.MaxHP / 100;
                state.ScaledTo = counted;
                if (boost > 0)
                {
                    host.MaxHP += boost;
                    host.HP += boost;
                }
            }
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
            host.StateObject.Remove(Id);
        }
    }
}
