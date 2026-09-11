using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Transitions
{
    public class GroupNotExistTransition : Transition
    {
        public readonly float Distance;
        public readonly string Group;

        public GroupNotExistTransition(double dist, string targetState, string group) : base(targetState)
        {
            Distance = (float)dist;
            Group = group;
        }

        public override bool Tick(Entity host)
        {
            if (string.IsNullOrWhiteSpace(Group))
                return false;
            foreach (Entity en in host.Parent.EntityChunks.HitTest(host.Position, Distance))
                if (en.Desc.Group == Group)
                    return false;
            //Statics are not in EntityChunks (see
            //BehaviorHelpers.NearbyEntities), but realm-src-master sees
            //them via EnemiesCollision.
            float r2 = Distance * Distance;
            foreach (Entity en in host.Parent.Statics.Values)
                if (en.Desc.Group == Group && host.Position.DistanceSquared(en.Position) < r2)
                    return false;
            return true;
        }
    }
}
