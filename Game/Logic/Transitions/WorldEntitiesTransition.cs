using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Logic.Transitions
{
    //Old exist / noneExist census conditions. exist=true holds when every listed type has a live entity in the
    //world; exist=false holds when none of them does. World-wide, not distance-limited. Counts come from
    //World.CountLiveOfType, so a tick costs one lookup per listed type.
    public class WorldEntitiesTransition : Transition
    {
        public readonly string Names;
        public readonly bool Exist;

        //Resolved once on the first tick. -1 marks an unknown name, which counts as absent.
        private int[] _types;

        public WorldEntitiesTransition(string names, bool exist, string targetState) : base(targetState)
        {
            Names = names;
            Exist = exist;
        }

        public override bool Tick(Entity host)
        {
            World world = host.Parent;
            if (world == null)
                return false;
            if (_types == null)
                _types = ResolveTypes(Names);

            foreach (int type in _types)
            {
                int count = type < 0 ? 0 : world.CountLiveOfType((ushort)type);
                if (Exist && count == 0)
                    return false;
                if (!Exist && count > 0)
                    return false;
            }
            return true;
        }

        private static int[] ResolveTypes(string names)
        {
            if (string.IsNullOrWhiteSpace(names))
                return Array.Empty<int>();

            List<int> types = new List<int>();
            foreach (string raw in names.Split(','))
            {
                string name = raw.Trim();
                if (name.Length == 0)
                    continue;
                if (BehaviorHelpers.TryGetObjType(name, out ushort type))
                {
                    types.Add(type);
                    continue;
                }
#if DEBUG
                Program.Print(PrintType.Debug, $"WorldEntitiesTransition: unknown object <{name}>, counted as absent.");
#endif
                types.Add(-1);
            }
            return types.ToArray();
        }
    }
}
