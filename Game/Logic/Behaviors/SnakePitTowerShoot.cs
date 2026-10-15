// Attack behavior adapted from ossimc82/fabiano-swagger-of-doom
// (commit 6fd20aad4a7905b13f25389c68368a942a2b68cb, 2015-11-09),
// licensed under AGPL-3.0; see THIRD_PARTY_NOTICES.md.
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    /// <summary>
    /// Fires along the Snake Pit wall opening next to the tower.
    /// </summary>
    public sealed class SnakePitTowerShoot : Shoot
    {
        public SnakePitTowerShoot()
            : base(range: 25, count: 1, index: 0, cooldown: 2000)
        {
        }

        protected override float? GetFixedAngle(Entity host)
        {
            if (host.Parent?.Map == null)
                return 0;

            int x = (int)host.Position.X + 1;
            int y = (int)host.Position.Y;
            IGameMap map = host.Parent.Map;
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                return 0;

            return map.GetObjectType(x, y) != 0 ? 180 * MathUtils.ToRadians : 0;
        }
    }
}
