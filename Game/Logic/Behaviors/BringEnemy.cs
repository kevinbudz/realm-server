using RotMG.Game.Entities;
using System.Linq;

namespace RotMG.Game.Logic.Behaviors
{
    //Hand-written toolkit: not emitted by Tools/XmlBehaviorConverter (files/xml has no equivalent).
    public class BringEnemy : Behavior
    {
        public readonly string Name;
        public readonly double Range;

        public BringEnemy(string name, double range)
        {
            Name = name;
            Range = range;
        }

        public override void Enter(Entity host)
        {
            foreach (Entity entity in BehaviorHelpers.EntitiesByName(host, (float)Range, Name).OfType<Enemy>())
                host.Parent.MoveEntity(entity, host.Position);
        }
    }
}
