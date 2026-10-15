using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Generic;

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
            //W05: statics come from the spatial index (same distance predicate), not a scan of Statics.
            List<StaticObject> statics = StaticIndex.Scratch;
            host.Parent.StaticGrid.Collect(host.Position, Distance, statics);
            foreach (StaticObject en in statics)
                if (en.Desc.Group == Group)
                    return false;
            return true;
        }
    }
}
