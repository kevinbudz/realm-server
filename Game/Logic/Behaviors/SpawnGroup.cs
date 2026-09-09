using RotMG.Common;
using RotMG.Utils;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class SpawnGroup : Behavior
    {
        public readonly string Group;
        public readonly int MaxChildren;
        public readonly int InitialSpawn;
        public readonly int Cooldown;
        public readonly int CooldownVariance;
        public readonly float Radius;

        public SpawnGroup(string group, int maxChildren = 5, double initialSpawn = 0.5,
            int cooldown = 0, int cooldownVariance = 0, double radius = 0)
        {
            Group = group;
            MaxChildren = maxChildren;
            InitialSpawn = (int)(maxChildren * initialSpawn);
            Cooldown = cooldown;
            CooldownVariance = cooldownVariance;
            Radius = (float)radius;
        }

        public override void Enter(Entity host)
        {
            SpawnState state = new SpawnState
            {
                CurrentNumber = InitialSpawn,
                RemainingTime = NextCooldown()
            };
            host.StateObject[Id] = state;

            for (int i = 0; i < InitialSpawn; i++)
            {
                if (SpawnOne(host) != null)
                    state.CurrentNumber++;
            }
        }

        private int NextCooldown()
        {
            int cool = Cooldown;
            if (CooldownVariance != 0)
                cool += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            return cool;
        }

        private Entity SpawnOne(Entity host)
        {
            ushort[] types = BehaviorHelpers.GetGroupTypes(Group);
            if (types.Length == 0)
                return null;
            Position at = host.Position;
            if (Radius > 0)
                at = host.Position + MathUtils.Position(Radius, Radius);
            if (host.Parent.GetTileF(at.X, at.Y) == null)
                return null;
            return BehaviorHelpers.SpawnChild(host, types[MathUtils.Next(types.Length)], at);
        }

        public override bool Tick(Entity host)
        {
            SpawnState state = host.StateObject[Id] as SpawnState;
            if (state == null)
                return false;

            if (state.RemainingTime <= 0 && state.CurrentNumber < MaxChildren)
            {
                if (SpawnOne(host) != null)
                    state.CurrentNumber++;
                state.RemainingTime = NextCooldown();
                return true;
            }

            state.RemainingTime -= Settings.MillisecondsPerTick;
            return false;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
