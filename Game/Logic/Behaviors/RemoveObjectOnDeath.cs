using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class RemoveObjectOnDeath : Behavior
    {
        public readonly string ObjName;
        public readonly int Range;

        public RemoveObjectOnDeath(string objName, int range)
        {
            ObjName = objName;
            Range = range;
        }

        public override void Death(Entity host)
        {
            if (host.Parent == null)
                return;
            if (!BehaviorHelpers.TryGetObjType(ObjName, out ushort objType))
                return;
            int hx = (int)host.Position.X;
            int hy = (int)host.Position.Y;
            for (int y = hy - Range; y <= hy + Range; y++)
                for (int x = hx - Range; x <= hx + Range; x++)
                {
                    if (Math.Abs(x - hx) > Range || Math.Abs(y - hy) > Range)
                        continue;
                    Tile tile = host.Parent.GetTile(x, y);
                    if (tile?.StaticObject == null || tile.StaticObject.Type != objType)
                        continue;
                    host.Parent.RemoveStatic(x, y);
                }
        }
    }
}
