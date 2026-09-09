using RotMG.Common;

namespace RotMG.Game.Logic.Behaviors
{
    public class Transform : Behavior
    {
        public readonly string Target;

        public Transform(string target)
        {
            Target = target;
        }

        public override bool Tick(Entity host)
        {
            if (host.Parent == null || !BehaviorHelpers.TryGetObjType(Target, out ushort type))
                return false;

            Position at = host.Position;
            World world = host.Parent;
            world.RemoveEntity(host);
            Entity entity = Entity.Resolve(type);
            world.AddEntity(entity, at);
            return true;
        }
    }
}
