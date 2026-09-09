using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Ticks children for Period ms, then reports false (finished) and restarts,
    //mirroring the source cycle Completed status for use inside Sequence.
    public class Timed : Behavior
    {
        public readonly int Period;
        public readonly Behavior[] Children;

        public Timed(int period, params Behavior[] behaviors)
        {
            Period = period;
            Children = behaviors;
        }

        public override void Enter(Entity host)
        {
            foreach (Behavior child in Children)
                child.Enter(host);
            host.StateObject[Id] = Period;
        }

        public override bool Tick(Entity host)
        {
            int remaining = (int)host.StateObject[Id];
            foreach (Behavior child in Children)
                child.Tick(host);

            remaining -= Settings.MillisecondsPerTick;
            if (remaining <= 0)
            {
                host.StateObject[Id] = Period;
                return false;
            }

            host.StateObject[Id] = remaining;
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
