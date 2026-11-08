namespace RotMG.Game.Logic.Transitions
{
    //Old hitpointsLessThan: holds while the host's HP fraction is strictly below Threshold (reference HPLessThanCondition).
    public class HpLessTransition : Transition
    {
        public readonly float Threshold;

        public HpLessTransition(double threshold, string targetState) : base(targetState)
        {
            Threshold = (float)threshold;
        }

        public override bool Tick(Entity host)
        {
            return host.GetHealthPercentage() < Threshold;
        }
    }
}
