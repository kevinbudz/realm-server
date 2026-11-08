using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Transitions
{
    //Old compound condition: fires when every child condition holds. Children are ticked every tick
    //(no short-circuit) so their timers keep running. Zero children fire immediately, as a condition-less
    //reference transition does. Targets are comma-separated; with several, one is picked uniformly at random
    //when this fires. Children are never resolved against states, so their own target is unused.
    public class AllTransition : Transition
    {
        private readonly Transition[] _conditions;

        public AllTransition(string targetStates, params Transition[] conditions) : base(ParseTargets(targetStates))
        {
            _conditions = conditions ?? Array.Empty<Transition>();
        }

        public override void Enter(Entity host)
        {
            foreach (Transition condition in _conditions)
                condition.Enter(host);
        }

        public override void Exit(Entity host)
        {
            foreach (Transition condition in _conditions)
                condition.Exit(host);
        }

        public override bool Tick(Entity host)
        {
            bool all = true;
            foreach (Transition condition in _conditions)
                if (!condition.Tick(host))
                    all = false;

            if (!all || TargetStates == null || TargetStates.Length == 0)
                return false;

            SelectedState = MathUtils.Next(TargetStates.Length);
            TargetState = TargetStates[SelectedState];
            return true;
        }

        private static string[] ParseTargets(string targetStates)
        {
            string[] targets = (targetStates ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return targets.Length > 0 ? targets : new[] { "" };
        }
    }
}
