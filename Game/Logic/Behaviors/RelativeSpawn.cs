using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class SpawnState
    {
        public int CurrentNumber;
        public int RemainingTime;
    }

    public class RelativeSpawn : Behavior
    {
        public readonly string Children;
        public readonly int X;
        public readonly int Y;
        public readonly int MaxChildren;
        public readonly int InitialSpawn;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public RelativeSpawn(string children, int x = 0, int y = 0, int maxChildren = 5, double initialSpawn = 0.5,
            int cooldown = 0, int cooldownVariance = 0)
        {
            Children = children;
            X = x;
            Y = y;
            MaxChildren = maxChildren;
            InitialSpawn = (int)(maxChildren * initialSpawn);
            Cooldown = cooldown;
            CooldownVariance = cooldownVariance;
        }

        public override void Enter(Entity host)
        {
            SpawnState state = new SpawnState
            {
                CurrentNumber = InitialSpawn,
                RemainingTime = NextCooldown()
            };
            host.StateObject[Id] = state;

            if (!BehaviorHelpers.TryGetObjType(Children, out ushort type))
                return;
            for (int i = 0; i < InitialSpawn; i++)
            {
                if (BehaviorHelpers.SpawnChild(host,
                    type, new Position(host.Position.X + X + 0.5f, host.Position.Y + Y + 0.5f)) != null)
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

        public override bool Tick(Entity host)
        {
            SpawnState state = host.StateObject[Id] as SpawnState;
            if (state == null)
                return false;

            if (state.RemainingTime <= 0 && state.CurrentNumber < MaxChildren)
            {
                float x = host.Position.X + X + 0.5f;
                float y = host.Position.Y + Y + 0.5f;
                if (host.Parent.GetTileF(x, y) == null)
                    return false;
                if (!BehaviorHelpers.TryGetObjType(Children, out ushort type))
                    return false;

                if (BehaviorHelpers.SpawnChild(host, type, new Position(x, y)) != null)
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
