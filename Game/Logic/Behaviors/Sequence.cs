namespace RotMG.Game.Logic.Behaviors
{
    //Destination has no cycle-status system: a child reporting false
    //(idle or finished) advances the sequence, mirroring Completed/NotStarted.
    public class Sequence : Behavior
    {
        public readonly Behavior[] Children;

        public Sequence(params Behavior[] children)
        {
            Children = children;
        }

        public override void Enter(Entity host)
        {
            foreach (Behavior child in Children)
                child.Enter(host);
            host.StateObject[Id] = 0;
        }

        public override bool Tick(Entity host)
        {
            if (Children.Length == 0)
                return false;

            int index = (int)host.StateObject[Id];
            if (!Children[index].Tick(host))
            {
                index++;
                if (index == Children.Length)
                    index = 0;
                host.StateObject[Id] = index;
            }
            return true;
        }

        public override void Exit(Entity host)
        {
            foreach (Behavior child in Children)
                child.Exit(host);
            host.StateObject.Remove(Id);
        }

        public override void Death(Entity host)
        {
            foreach (Behavior child in Children)
                child.Death(host);
        }
    }
}
