using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class MaintainProtectorsState
    {
        public Entity[] Children;
        public Dictionary<int, int>[] Records;
        public ushort[] Types;
        public bool CoolingDown;
        public int CooldownLeft;
    }

    //Port of the reference MaintainProtectorsBehavior. The host keeps NumChildren protectors on a ring of radius
    //Distance around itself. Once any protector dies it waits Cooldown ms, then respawns the missing ones. When every
    //protector is dead the host dies and each protector's damage record is copied onto its corpse (childCorpse).
    //The final death goes through BehaviorDeath.Die with dropLoot and giveXP (reference Kill(NULL, ..., true, true)).
    public class MaintainProtectors : Behavior
    {
        public readonly string Children;
        public readonly string ChildCorpse;
        public readonly int NumChildren;
        public readonly int Cooldown;
        public readonly float Distance;

        private ushort[] _configuredTypes;

        public MaintainProtectors(string children, string childCorpse = null, int numChildren = 2, int cooldown = 30000, double distance = 0)
        {
            Children = children;
            ChildCorpse = childCorpse;
            NumChildren = numChildren;
            Cooldown = cooldown;
            Distance = (float)distance;
        }

        public override void Enter(Entity host)
        {
            //The reference builds its protectors in its constructor, so they are created once per host and survive
            //re-entering the state.
            if (host.StateObject.TryGetValue(Id, out object existing) && existing is MaintainProtectorsState)
                return;

            int count = Math.Max(0, NumChildren);
            ushort[] types = ConfiguredTypes();
            if (types.Length == 0)
                types = new[] { host.Type };
            MaintainProtectorsState state = new MaintainProtectorsState
            {
                Children = new Entity[count],
                Records = new Dictionary<int, int>[count],
                Types = types,
                CoolingDown = false,
                CooldownLeft = Cooldown + JitterHalf(),
            };
            host.StateObject[Id] = state;
            MakeChildren(host, state);
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is MaintainProtectorsState state))
                return false;
            if (host.Parent == null)
                return false;

            int n = state.Children.Length;
            for (int i = 0; i < n; i++)
            {
                Entity child = state.Children[i];
                if (!IsAlive(child))
                    continue;
                if (child is Enemy enemy)
                    SnapshotRecord(state, i, enemy);

                //Stream visual between this protector and a random other one (reference ShowEffectMessage::Stream).
                if (n < 2)
                    continue;
                int j = MathUtils.Next(n - 1);
                if (j >= i)
                    j++;
                BehaviorHelpers.BroadcastNearby(host, GameServer.ShowEffect(ShowEffectIndex.Stream, host.Id,
                    state.CoolingDown ? 0xFFFF00u : 0xFF0000u, ChildPos(host, i, n), ChildPos(host, j, n)));
            }

            if (!state.CoolingDown)
            {
                for (int i = 0; i < n; i++)
                {
                    if (!IsAlive(state.Children[i]))
                    {
                        state.CoolingDown = true;
                        state.CooldownLeft = Cooldown;
                        break;
                    }
                }
                return false;
            }

            bool allDead = true;
            foreach (Entity child in state.Children)
            {
                if (IsAlive(child))
                {
                    allDead = false;
                    break;
                }
            }
            if (allDead)
            {
                SpawnCorpses(host, state);
                BehaviorDeath.Die(host, true);
                return false;
            }

            state.CooldownLeft -= Settings.MillisecondsPerTick;
            if (state.CooldownLeft > 0)
                return false;

            MakeChildren(host, state);
            state.CoolingDown = false;
            return true;
        }

        private void MakeChildren(Entity host, MaintainProtectorsState state)
        {
            int n = state.Children.Length;
            for (int i = 0; i < n; i++)
            {
                if (IsAlive(state.Children[i]))
                    continue;
                ushort type = state.Types[MathUtils.Next(state.Types.Length)];
                state.Children[i] = BehaviorHelpers.SpawnChild(host.Parent, type, ChildPos(host, i, n));
            }
        }

        private void SpawnCorpses(Entity host, MaintainProtectorsState state)
        {
            if (!BehaviorHelpers.TryGetObjType(ChildCorpse ?? string.Empty, out ushort corpseType))
                return;

            int n = state.Children.Length;
            for (int i = 0; i < n; i++)
            {
                Entity corpse = BehaviorHelpers.SpawnChild(host.Parent, corpseType, ChildPos(host, i, n));
                if (corpse is Enemy enemy && state.Records[i] != null)
                    enemy.DamageStorage = new Dictionary<int, int>(state.Records[i]);
            }
        }

        private static void SnapshotRecord(MaintainProtectorsState state, int index, Enemy child)
        {
            Dictionary<int, int> record = state.Records[index];
            if (record == null || Total(record) != Total(child.DamageStorage))
                state.Records[index] = new Dictionary<int, int>(child.DamageStorage);
        }

        private static int Total(Dictionary<int, int> record)
        {
            int total = 0;
            foreach (int damage in record.Values)
                total += damage;
            return total;
        }

        //Protector slots are spaced evenly on the ring of radius Distance around the host (reference ChildPos).
        private Position ChildPos(Entity host, int index, int count)
        {
            if (count <= 0)
                return host.Position;
            float angle = MathF.PI * 2 / count * index;
            return host.Position + new Position(MathF.Cos(angle) * Distance, MathF.Sin(angle) * Distance);
        }

        private int JitterHalf()
        {
            return Cooldown == 0 ? 0 : MathUtils.NextIntSnap(-Cooldown / 2, Cooldown / 2, Settings.MillisecondsPerTick);
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
                    if (name.Length > 0 && BehaviorHelpers.TryGetObjType(name, out ushort type))
                        types.Add(type);
                }
            }
            _configuredTypes = types.ToArray();
            return _configuredTypes;
        }

        private static bool IsAlive(Entity entity)
        {
            return entity != null && entity.Parent != null && !entity.Dead;
        }
    }
}
