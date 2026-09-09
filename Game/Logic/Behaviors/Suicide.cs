using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Behaviors
{
    public class Suicide : Behavior
    {
        public override bool Tick(Entity host)
        {
            if (!(host is Enemy) || host.Parent == null)
                return false;

            if (host.Behavior != null)
            {
                foreach (Behavior behavior in host.Behavior.Behaviors)
                    behavior.Death(host);
                foreach (State state in host.CurrentStates)
                    foreach (Behavior behavior in state.Behaviors)
                        behavior.Death(host);
            }

            host.Dead = true;
            host.Parent.RemoveEntity(host);
            return true;
        }
    }
}
