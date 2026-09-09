using RotMG.Common;
using System.Collections.Generic;

namespace RotMG.Game.Logic.Behaviors
{
    public class GroundTileState
    {
        public ushort OriginalType;
        public int X;
        public int Y;
    }

    public class GroundTransform : Behavior
    {
        public readonly string TileId;
        public readonly int Radius;
        public readonly bool Persist;
        public readonly int? RelativeX;
        public readonly int? RelativeY;

        public GroundTransform(string tileId, int radius = 0, int? relativeX = null, int? relativeY = null, bool persist = false)
        {
            TileId = tileId;
            Radius = radius;
            RelativeX = relativeX;
            RelativeY = relativeY;
            Persist = persist;
        }

        public override void Enter(Entity host)
        {
            List<GroundTileState> changed = new List<GroundTileState>();
            host.StateObject[Id] = changed;

            if (!Resources.Id2Tile.TryGetValue(TileId, out TileDesc desc))
                return;

            if (RelativeX != null && RelativeY != null)
            {
                int x = (int)host.Position.X + RelativeX.Value;
                int y = (int)host.Position.Y + RelativeY.Value;
                Tile tile = host.Parent.GetTile(x, y);
                if (tile == null || tile.Type == desc.Type)
                    return;
                changed.Add(new GroundTileState { OriginalType = tile.Type, X = x, Y = y });
                host.Parent.UpdateTile(x, y, desc.Type);
                return;
            }

            int hx = (int)host.Position.X;
            int hy = (int)host.Position.Y;
            for (int y = hy - Radius; y <= hy + Radius; y++)
                for (int x = hx - Radius; x <= hx + Radius; x++)
                {
                    Tile tile = host.Parent.GetTile(x, y);
                    if (tile == null || tile.Type == desc.Type)
                        continue;
                    changed.Add(new GroundTileState { OriginalType = tile.Type, X = x, Y = y });
                    host.Parent.UpdateTile(x, y, desc.Type);
                }
        }

        public override void Exit(Entity host)
        {
            if (!Persist && host.StateObject[Id] is List<GroundTileState> changed && host.Parent != null)
                foreach (GroundTileState tile in changed)
                    host.Parent.UpdateTile(tile.X, tile.Y, tile.OriginalType);
            host.StateObject.Remove(Id);
        }
    }
}
