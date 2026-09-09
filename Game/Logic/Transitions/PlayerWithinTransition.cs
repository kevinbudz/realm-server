using RotMG.Utils;
using System.Linq;

namespace RotMG.Game.Logic.Transitions
{
    public class PlayerWithinTransition : Transition
    {
        public readonly float Distance;
        public readonly bool SeeInvis;

        public PlayerWithinTransition(double dist, string targetState, bool seeInvis = false) : base(targetState)
        {
            Distance = (float)dist;
            SeeInvis = seeInvis;
        }

        public override bool Tick(Entity host)
        {
            if (SeeInvis)
                return host.Parent.PlayerChunks.HitTest(host.Position, Distance).Any();
            return host.GetNearestPlayer(Distance) != null;
        }
    }
}
