using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Logic.Transitions
{
    public class ThresholdState
    {
        public double CurrentThreshold;
        public List<double> Thresholds;
        public int SelectedState;
    }

    public class HpBoundaryTransition : Transition
    {
        public readonly List<double> Thresholds;

        public HpBoundaryTransition(double[] thresholds, string[] targetStates)
            : base(targetStates)
        {
            Thresholds = thresholds.ToList();
        }

        public HpBoundaryTransition(double threshold, string targetState)
            : base(targetState)
        {
            Thresholds = new List<double> { threshold };
        }

        public override void Enter(Entity host)
        {
            if (Thresholds.Count == 0)
                return;
            host.StateObject[Id] = new ThresholdState
            {
                CurrentThreshold = Thresholds[0],
                Thresholds = Thresholds.ToList(),
                SelectedState = 0
            };
        }

        public override bool Tick(Entity host)
        {
            ThresholdState state = host.StateObject[Id] as ThresholdState;
            if (state == null || state.Thresholds == null || host.MaxHP <= 0)
                return false;

            float hpp = (float)host.HP / host.MaxHP;
            if (hpp > state.CurrentThreshold)
                return false;

            if (TargetStates.Length == 0)
                return false;
            SelectedState = state.SelectedState;
            TargetState = TargetStates[SelectedState];
            if (state.Thresholds.Count <= 1)
                state.Thresholds = null;
            else
            {
                state.Thresholds.RemoveAt(0);
                state.CurrentThreshold = state.Thresholds[0];
                state.SelectedState++;
            }
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
