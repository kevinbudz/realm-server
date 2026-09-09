using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class Decay : Behavior
    {
        public readonly int Time;

        public Decay(int time = 10000)
        {
            Time = time;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = Time;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] <= 0)
                host.Parent.RemoveEntity(host);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
