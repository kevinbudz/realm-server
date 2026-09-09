using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class OrderOnDeath : Behavior
    {
        public readonly float Range;
        public readonly string Target;
        public readonly string StateName;
        public readonly float Probability;

        public OrderOnDeath(double range, string target, string state, double probability = 1)
        {
            Range = (float)range;
            Target = target;
            StateName = state;
            Probability = (float)probability;
        }

        public override void Death(Entity host)
        {
            if (!MathUtils.Chance(Probability))
                return;
            foreach (Entity entity in BehaviorHelpers.EntitiesByName(host, Range, Target))
                BehaviorHelpers.SwitchToState(entity, StateName);
        }
    }
}
