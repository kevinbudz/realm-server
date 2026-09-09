using RotMG.Game.Entities;
using System.Linq;

namespace RotMG.Game.Logic.Transitions
{
    public class AnyEntityWithinTransition : Transition
    {
        public readonly float Distance;

        public AnyEntityWithinTransition(int dist, string targetState) : base(targetState)
        {
            Distance = dist;
        }

        public override bool Tick(Entity host)
        {
            if (host.Parent.EntityChunks.HitTest(host.Position, Distance).Any(e => !e.Equals(host)))
                return true;
            return host.Parent.PlayerChunks.HitTest(host.Position, Distance).Any();
        }
    }
}
