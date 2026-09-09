using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class Duration : Behavior
    {
        public readonly Behavior Child;
        public readonly int Time;

        public Duration(Behavior child, int duration)
        {
            Child = child;
            Time = duration;
        }

        public override void Enter(Entity host)
        {
            Child.Enter(host);
            host.StateObject[Id] = 0;
        }

        public override bool Tick(Entity host)
        {
            int elapsed = (int)host.StateObject[Id];
            if (elapsed > Time)
                return false;

            Child.Tick(host);
            host.StateObject[Id] = elapsed + Settings.MillisecondsPerTick;
            return true;
        }

        public override void Exit(Entity host)
        {
            Child.Exit(host);
            host.StateObject.Remove(Id);
        }

        public override void Death(Entity host)
        {
            Child.Death(host);
        }
    }
}
