namespace RotMG.Game.Logic.Behaviors
{
    //Old setTag: gives the host a tag on Enter and re-applies it every tick. Tags outlive the state.
    //Set false is the old clearTag: the tag is removed on Enter and on every tick instead.
    public class SetTag : Behavior
    {
        public readonly string Tag;
        public readonly bool Set;

        public SetTag(string tag, bool set = true)
        {
            Tag = tag;
            Set = set;
        }

        public override void Enter(Entity host) => Apply(host);

        public override bool Tick(Entity host)
        {
            Apply(host);
            return true;
        }

        private void Apply(Entity host)
        {
            if (Set)
                host.AddTag(Tag);
            else
                host.RemoveTag(Tag);
        }
    }
}
