using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class MakeMinionsState
    {
        public ushort[] Types;
        public List<Entity> Children;
        public int CooldownLeft;
    }

    //Port of the reference MakeMinionsBehavior: spawns minions on a cooldown. MaxChildren caps the live children this
    //host made (0 = unlimited). The configured types are global data; the host-type fallback is per entity.
    public class MakeMinions : Behavior
    {
        public readonly string Children;
        public readonly int MaxChildren;
        public readonly int Cooldown;
        public readonly bool StartWithMinions;
        public readonly string Group;
        public readonly float MinOffset;
        public readonly float MaxOffset;
        public readonly bool CooldownJitter;

        private ushort[] _configuredTypes;

        public MakeMinions(string children = null, int maxChildren = 0, int cooldown = 30000, bool startWithMinions = true,
            string group = null, double minOffset = 0, double maxOffset = 0, bool cooldownJitter = false)
        {
            Children = children;
            MaxChildren = maxChildren;
            Cooldown = cooldown;
            StartWithMinions = startWithMinions;
            Group = group;
            MinOffset = (float)minOffset;
            MaxOffset = (float)maxOffset;
            CooldownJitter = cooldownJitter;
        }

        public override void Enter(Entity host)
        {
            if (host.StateObject.TryGetValue(Id, out object existing) && existing is MakeMinionsState living)
            {
                //Re-entering the state keeps the living minions and restarts the cooldown (reference Clear()).
                living.CooldownLeft = 0;
                return;
            }

            ushort[] types = ConfiguredTypes();
            if (types.Length == 0)
                types = new[] { host.Type };
            MakeMinionsState state = new MakeMinionsState { Types = types, Children = new List<Entity>() };
            host.StateObject[Id] = state;

            //The reference spawns start minions in its constructor, so they are created once per host, not per entry.
            if (StartWithMinions)
                for (int i = 0; i < MaxChildren; i++)
                    if (!MakeChild(host, state))
                        return;
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is MakeMinionsState state))
                return false;

            state.CooldownLeft -= Settings.MillisecondsPerTick;
            if (state.CooldownLeft > 0)
                return false;

            state.CooldownLeft = Cooldown;
            if (CooldownJitter)
                state.CooldownLeft += MathUtils.NextIntSnap(-Cooldown / 2, Cooldown / 2, Settings.MillisecondsPerTick);

            if (MaxChildren > 0)
            {
                state.Children.RemoveAll(child => !IsAlive(child));
                if (state.Children.Count >= MaxChildren)
                    return false;
            }
            return MakeChild(host, state);
        }

        private bool MakeChild(Entity host, MakeMinionsState state)
        {
            ushort type = state.Types[MathUtils.Next(state.Types.Length)];
            Position pos = host.Position;
            if (MaxOffset > 0)
            {
                float d = MathUtils.NextFloat(MinOffset, MaxOffset);
                float a = MathUtils.NextAngle();
                pos = pos + new Position(MathF.Cos(a) * d, MathF.Sin(a) * d);
            }

            Entity child = BehaviorHelpers.SpawnChild(host, type, pos);
            if (child == null)
                return false;
            if (MaxChildren > 0)
                state.Children.Add(child);
            return true;
        }

        private ushort[] ConfiguredTypes()
        {
            if (_configuredTypes != null)
                return _configuredTypes;

            List<ushort> types = new List<ushort>();
            if (!string.IsNullOrWhiteSpace(Children))
            {
                foreach (string raw in Children.Split(','))
                {
                    string name = raw.Trim();
                    if (name.Length == 0)
                        continue;
                    if (BehaviorHelpers.TryGetObjType(name, out ushort type))
                        types.Add(type);
                    else
                    {
#if DEBUG
                        Program.Print(PrintType.Error, $"MakeMinions: unknown child <{name}>.");
#endif
                    }
                }
            }
            if (!string.IsNullOrEmpty(Group))
                types.AddRange(BehaviorHelpers.GetGroupTypes(Group));

            _configuredTypes = types.ToArray();
            return _configuredTypes;
        }

        private static bool IsAlive(Entity entity)
        {
            return entity != null && entity.Parent != null && !entity.Dead;
        }
    }
}
