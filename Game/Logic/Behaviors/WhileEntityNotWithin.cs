using RotMG.Game.Logic.Conditionals;

namespace RotMG.Game.Logic.Behaviors
{
    public class WhileEntityNotWithin : Conditional
    {
        public readonly string EntityName;
        public readonly float Range;

        public WhileEntityNotWithin(Behavior child, string entityName, double range)
            : base(child)
        {
            EntityName = entityName;
            Range = (float)range;
        }

        public override bool ConditionMet(Entity host)
        {
            return BehaviorHelpers.NearestEntityByName(host, Range, EntityName) == null;
        }
    }
}
