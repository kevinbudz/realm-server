using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class DropPortalOnDeath : Behavior
    {
        public readonly string Target;
        public readonly float Probability;
        public readonly int? Timeout;

        public DropPortalOnDeath(string target, double probability = 1, int? timeout = null)
        {
            Target = target;
            Probability = (float)probability;
            Timeout = timeout;
        }

        public override void Death(Entity host)
        {
            if (host.Parent == null || !MathUtils.Chance(Probability))
                return;
            if (!BehaviorHelpers.TryGetObjType(Target, out ushort type))
                return;

            Entity portal = BehaviorHelpers.SpawnChild(host, type, host.Position);
            if (portal == null)
                return;

            int timeout = Timeout ?? 30;
            if (timeout != 0)
            {
                World world = host.Parent;
                int portalId = portal.Id;
                Manager.AddTimedAction(timeout * 1000, () =>
                {
                    Entity entity = world.GetEntity(portalId);
                    if (entity != null && entity.Parent != null)
                        world.RemoveEntity(entity);
                });
            }
        }
    }
}
