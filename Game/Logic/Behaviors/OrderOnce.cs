using System.Collections.Generic;

namespace RotMG.Game.Logic.Behaviors
{
    public class OrderOnce : Behavior
    {
        public readonly float Range;
        public readonly string Children;
        public readonly string TargetState;

        public OrderOnce(double range, string children, string targetState)
        {
            Range = (float)range;
            Children = children;
            TargetState = targetState;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new HashSet<int>();
        }

        public override bool Tick(Entity host)
        {
            HashSet<int> ordered = host.StateObject[Id] as HashSet<int>;
            foreach (Entity entity in BehaviorHelpers.EntitiesByName(host, Range, Children))
            {
                if (ordered.Contains(entity.Id))
                    continue;
                if (BehaviorHelpers.SwitchToState(entity, TargetState))
                    ordered.Add(entity.Id);
            }
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
