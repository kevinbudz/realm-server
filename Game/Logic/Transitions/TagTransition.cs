namespace RotMG.Game.Logic.Transitions
{
    //Old hasTag / noTag: holds while the host's tag presence equals hasTag.
    public class TagTransition : Transition
    {
        public readonly string Tag;
        public readonly bool Present;

        public TagTransition(string tag, bool hasTag, string targetState) : base(targetState)
        {
            Tag = tag;
            Present = hasTag;
        }

        public override bool Tick(Entity host)
        {
            return host.HasTag(Tag) == Present;
        }
    }
}
