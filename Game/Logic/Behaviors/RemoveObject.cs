using RotMG.Common;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class RemoveObject : Behavior
    {
        public readonly string ObjName;
        public readonly int Range;

        public RemoveObject(string objName, int range)
        {
            ObjName = objName;
            Range = range;
        }

        public override void Enter(Entity host)
        {
            Sweep(host);
            host.StateCooldown[Id] = 1000;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;
            Sweep(host);
            host.StateCooldown[Id] = 1000;
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }

        private void Sweep(Entity host)
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
                    Tile? tile = host.Parent.GetTile(x, y);
                    if (tile?.StaticObject == null || tile.Value.StaticObject.Type != objType)
                        continue;
                    host.Parent.RemoveStatic(x, y);
                }
        }
    }
}
