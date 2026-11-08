using RotMG.Utils;

namespace RotMG.Game.Logic.Transitions
{
    //Old "random" condition: holds on a tick with the given chance. Evaluated every tick, like the reference.
    public class RandomTransition : Transition
    {
        public readonly float Probability;

        public RandomTransition(double probability, string targetState) : base(targetState)
        {
            Probability = (float)probability;
        }

        public override bool Tick(Entity host)
        {
            return MathUtils.NextFloat() < Probability;
        }
    }
}
