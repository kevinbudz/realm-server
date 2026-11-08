namespace RotMG.Game.Logic.Transitions
{
    //Old hasGlobalTag / noGlobalTag: holds while the host's world has (or lacks) the tag, per hasTag.
    public class GlobalTagTransition : Transition
    {
        public readonly string Tag;
        public readonly bool Present;

        public GlobalTagTransition(string tag, bool hasTag, string targetState) : base(targetState)
        {
            Tag = tag;
            Present = hasTag;
        }

        public override bool Tick(Entity host)
        {
            World world = host.Parent;
            return world != null && world.HasGlobalTag(Tag) == Present;
        }
    }
}
