using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Spawns a random 1..MaxChildren of Children once, when the host
    //enters the world, and never again. Candyland hunt enemies use
    //this so the Overseer only places hunt enemies while each one
    //brings its own minions. Minions land scattered around the host
    //instead of stacked on its tile.
    public class SpawnOnce : Behavior
    {
        private const float ScatterRadius = 2;

        public readonly string Children;
        public readonly int MaxChildren;

        public SpawnOnce(string children, int maxChildren = 3)
        {
            Children = children;
            MaxChildren = maxChildren;
        }

        public override void Enter(Entity host)
        {
            if (!BehaviorHelpers.TryGetObjType(Children, out ushort type))
                return;
            int count = MathUtils.Next(MaxChildren) + 1;
            for (int i = 0; i < count; i++)
                BehaviorHelpers.SpawnChild(host, type, PickScatterPos(host));
        }

        private static Position PickScatterPos(Entity host)
        {
            World world = host.Parent;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                float dx = MathUtils.NextFloat(-ScatterRadius, ScatterRadius);
                float dy = MathUtils.NextFloat(-ScatterRadius, ScatterRadius);
                int tx = (int)(host.Position.X + dx);
                int ty = (int)(host.Position.Y + dy);
                if (tx < 0 || ty < 0 || tx >= world.Width || ty >= world.Height)
                    continue;
                if (!world.IsPassable(tx, ty, true))
                    continue;
                return new Position(host.Position.X + dx, host.Position.Y + dy);
            }
            return host.Position;
        }
    }
}
