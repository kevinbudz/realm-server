namespace RotMG.Game.Logic.Behaviors
{
    public class DestroyOnDeath : Behavior
    {
        public readonly string Target;

        public DestroyOnDeath(string target)
        {
            Target = target;
        }

        public override void Death(Entity host)
        {
            if (host.Parent == null)
                return;
            foreach (Entity entity in BehaviorHelpers.EntitiesByName(host, 250, Target))
                host.Parent.RemoveEntity(entity);
        }
    }
}
