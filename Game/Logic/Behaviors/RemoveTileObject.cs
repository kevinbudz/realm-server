using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class RemoveTileObject : Behavior
    {
        public readonly string ObjName;
        public readonly int Range;

        public RemoveTileObject(string objName, int range)
        {
            ObjName = objName;
            Range = range;
        }

        public override void Enter(Entity host)
        {
            if (!BehaviorHelpers.TryGetObjType(ObjName, out ushort objType))
                return;

            int hx = (int)host.Position.X;
            int hy = (int)host.Position.Y;
            for (int y = hy - Range; y <= hy + Range; y++)
                for (int x = hx - Range; x <= hx + Range; x++)
                {
                    Tile? tile = host.Parent.GetTile(x, y);
                    if (tile?.StaticObject == null || tile.Value.StaticObject.Type != objType)
                        continue;
                    if (Math.Abs(x - hx) > Range || Math.Abs(y - hy) > Range)
                        continue;
                    host.Parent.RemoveStatic(x, y);
                }
        }
    }
}
