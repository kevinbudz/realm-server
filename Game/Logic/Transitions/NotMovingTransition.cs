using RotMG.Common;

namespace RotMG.Game.Logic.Transitions
{
    public class NotMovingState
    {
        public Position Position;
        public int Delay;
    }

    public class NotMovingTransition : Transition
    {
        public readonly int Delay;

        public NotMovingTransition(string targetState, int delay = 250) : base(targetState)
        {
            Delay = delay;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new NotMovingState
            {
                Position = host.Position,
                Delay = Delay
            };
        }

        public override bool Tick(Entity host)
        {
            NotMovingState state = host.StateObject[Id] as NotMovingState;
            if (state == null)
                return false;

            if (state.Delay <= 0)
            {
                if (state.Position == host.Position)
                {
                    host.StateObject[Id] = new NotMovingState
                    {
                        Position = host.Position,
                        Delay = Delay
                    };
                    return true;
                }
                state.Position = host.Position;
                state.Delay = Delay;
                return false;
            }

            state.Delay -= Settings.MillisecondsPerTick;
            return false;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
