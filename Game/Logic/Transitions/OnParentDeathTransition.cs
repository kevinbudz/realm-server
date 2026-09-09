using RotMG.Utils;

namespace RotMG.Game.Logic.Transitions
{
    //Destination enemies track no parent link, so the nearest entity seen on
    //entry is treated as the parent: the transition fires once it is gone.
    public class WatchedParentState
    {
        public int ParentId = -1;
    }

    public class OnParentDeathTransition : Transition
    {
        public OnParentDeathTransition(string targetState) : base(targetState) { }

        public override void Enter(Entity host)
        {
            WatchedParentState state = new WatchedParentState();
            Entity nearest = host.GetNearestEntity(50);
            if (nearest != null)
                state.ParentId = nearest.Id;
            host.StateObject[Id] = state;
        }

        public override bool Tick(Entity host)
        {
            WatchedParentState state = host.StateObject[Id] as WatchedParentState;
            if (state == null || state.ParentId == -1)
                return false;

            Entity parent = host.Parent.GetEntity(state.ParentId);
            return parent == null || parent.Dead;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
