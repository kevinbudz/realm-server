using RotMG.Game.Entities;
using RotMG.Game.Logic.Conditionals;
using System.Linq;

namespace RotMG.Game.Logic.Behaviors
{
    public class WhileWatched : Conditional
    {
        public WhileWatched(Behavior child)
            : base(child) { }

        public override bool ConditionMet(Entity host)
        {
            if (host.Parent == null)
                return false;
            return host.Parent.PlayerChunks.HitTest(host.Position, Player.SightRadius)
                .OfType<Player>()
                .Any(player => player.Entities.Contains(host));
        }
    }
}
