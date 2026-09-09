using RotMG.Game.Entities;
using System.Collections.Generic;

namespace RotMG.Game.Logic.Behaviors
{
    public class RemoveEntity : Behavior
    {
        public readonly float Dist;
        public readonly string Children;

        public RemoveEntity(double dist, string children)
        {
            Dist = (float)dist;
            Children = children;
        }

        public override void Enter(Entity host)
        {
            if (host.Parent == null)
                return;
            List<Entity> victims = new List<Entity>();
            foreach (Entity en in BehaviorHelpers.EntitiesByName(host, Dist, Children))
            {
                if (en.Equals(host) || en is Player)
                    continue;
                victims.Add(en);
            }
            foreach (Entity en in victims)
                host.Parent.RemoveEntity(en);
        }
    }
}
