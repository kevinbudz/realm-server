namespace RotMG.Game.Logic.Behaviors
{
    public class Order : Behavior
    {
        public readonly float Range;
        public readonly string Children;
        public readonly string TargetState;

        public Order(double range, string children, string targetState)
        {
            Range = (float)range;
            Children = children;
            TargetState = targetState;
        }

        public override bool Tick(Entity host)
        {
            foreach (Entity entity in BehaviorHelpers.EntitiesByName(host, Range, Children))
                BehaviorHelpers.SwitchToState(entity, TargetState);
            return true;
        }
    }
}
