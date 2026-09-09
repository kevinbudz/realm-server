using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Logic.Behaviors
{
    public class ReproduceChildrenState
    {
        public List<Entity> LivingChildren;
        public int RemainingTime;
    }

    public class ReproduceChildren : Behavior
    {
        public readonly string[] Children;
        public readonly int MaxChildren;
        public readonly int InitialSpawn;
        public readonly int Cooldown;
        public readonly int CooldownVariance;

        public ReproduceChildren(int maxChildren = 5, double initialSpawn = 0.5,
            int cooldown = 0, int cooldownVariance = 0, params string[] children)
        {
            MaxChildren = maxChildren;
            InitialSpawn = (int)(maxChildren * initialSpawn);
            Cooldown = cooldown;
            CooldownVariance = cooldownVariance;
            Children = children;
        }

        private ushort NextType()
        {
            string child = Children[MathUtils.Next(Children.Length)];
            return BehaviorHelpers.GetObjType(child);
        }

        private int NextCooldown()
        {
            int cool = Cooldown;
            if (CooldownVariance != 0)
                cool += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            return cool;
        }

        public override void Enter(Entity host)
        {
            ReproduceChildrenState state = new ReproduceChildrenState
            {
                LivingChildren = new List<Entity>(),
                RemainingTime = NextCooldown()
            };
            host.StateObject[Id] = state;

            if (Children.Length == 0)
                return;
            for (int i = 0; i < InitialSpawn; i++)
            {
                Entity entity = BehaviorHelpers.SpawnChild(host, NextType(), host.Position);
                if (entity != null)
                    state.LivingChildren.Add(entity);
            }
        }

        public override bool Tick(Entity host)
        {
            ReproduceChildrenState state = host.StateObject[Id] as ReproduceChildrenState;
            if (state == null || Children.Length == 0)
                return false;

            state.LivingChildren.RemoveAll(child => child.HP <= 0 || child.Parent == null);

            if (state.RemainingTime <= 0 && state.LivingChildren.Count < MaxChildren)
            {
                Entity entity = BehaviorHelpers.SpawnChild(host, NextType(), host.Position);
                if (entity != null)
                    state.LivingChildren.Add(entity);
                state.RemainingTime = NextCooldown();
                return entity != null;
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
