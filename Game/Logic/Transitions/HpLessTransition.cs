namespace RotMG.Game.Logic.Transitions
{
    public class HpLessTransition : HealthTransition
    {
        public HpLessTransition(double threshold, string targetState)
            : base((float)threshold, targetState) { }
    }
}
