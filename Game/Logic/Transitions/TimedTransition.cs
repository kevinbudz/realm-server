using RotMG.Common;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Transitions
{
    public class TimedTransition : Transition
    {
        public readonly int Time;
        public readonly bool Randomized;

        public TimedTransition(int time, string targetState, bool randomized = false) : base(targetState)
        {
            Time = time;
            Randomized = randomized;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown.Add(Id, Randomized && Time > 0 ? MathUtils.Next(Time) : Time);
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] <= 0)
            {
                host.StateCooldown[Id] = Time;
                return true;
            }
            return false;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
