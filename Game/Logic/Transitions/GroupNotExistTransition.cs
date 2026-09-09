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
            return true;
        }
    }
}
