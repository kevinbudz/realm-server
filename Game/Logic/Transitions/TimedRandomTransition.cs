using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Transitions
{
    public class TimedRandomTransition : Transition
    {
        public readonly int Time;
        public readonly bool Randomized;

        public TimedRandomTransition(int time, bool randomizedTime = false, params string[] states)
            : base(states)
        {
            Time = time;
            Randomized = randomizedTime;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = Randomized && Time > 0 ? MathUtils.Next(Time) : Time;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            host.StateCooldown[Id] = Time;
            if (TargetStates.Length == 0)
                return false;
            SelectedState = MathUtils.Next(TargetStates.Length);
            TargetState = TargetStates[SelectedState];
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
