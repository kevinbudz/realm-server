using RotMG.Game.Entities;
using System.Linq;

namespace RotMG.Game.Logic.Transitions
{
    public class NoEntityWithinTransition : Transition
    {
        public readonly float Distance;

        public NoEntityWithinTransition(int dist, string targetState) : base(targetState)
        {
            Distance = dist;
        }

        public override bool Tick(Entity host)
        {
            if (host.Parent.EntityChunks.HitTest(host.Position, Distance).Any(e => !e.Equals(host)))
                return false;
            return !host.Parent.PlayerChunks.HitTest(host.Position, Distance).Any();
        }
    }
}
