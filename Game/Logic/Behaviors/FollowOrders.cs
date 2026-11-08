using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;
using System.Collections.Generic;

namespace RotMG.Game.Logic.Behaviors
{
    //Old FollowOrdersBehavior: the host adopts the orders its nearest listened-to leader is currently giving
    //(see OrderDb). While no leader is held, a search runs whenever the cooldown timer reaches zero, and the
    //timer restarts on each search. Orders change means Exit the old block's behaviors and Enter the new block's.
    //Blocks are shared across followers; per-entity state lives in StateObject[Id] and StateCooldown[Id].
    public class FollowOrders : Behavior
    {
        public readonly string ListenIds;
        public readonly float AcquireRange;
        public readonly int Cooldown;

        //Resolved once on the first tick from ListenIds.
        private HashSet<ushort> _listenTypes;

        private sealed class FollowState
        {
            public Entity Leader;
            public OrderDb.OrderBlock Block;
        }

        public FollowOrders(string listenIds, double acquireRange = 15, int cooldown = 3000)
        {
            ListenIds = listenIds;
            AcquireRange = (float)acquireRange;
            Cooldown = cooldown;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = 0;
            host.StateObject[Id] = new FollowState();
        }

        public override bool Tick(Entity host)
        {
            FollowState state = GetState(host);

            int left = host.StateCooldown[Id] - Settings.MillisecondsPerTick;
            host.StateCooldown[Id] = left > 0 ? left : 0;

            if (state.Leader == null)
            {
                if (host.StateCooldown[Id] > 0)
                {
                    SetBlock(host, state, null);
                    return false;
                }

                state.Leader = FindClosestListen(host);
                host.StateCooldown[Id] = Cooldown;
                if (state.Leader == null)
                {
                    SetBlock(host, state, null);
                    return false;
                }
            }
            else if (!IsLive(state.Leader, host))
            {
                //Dead or gone from this world: drop the leader. The search timer keeps running, as in the reference.
                state.Leader = null;
                SetBlock(host, state, null);
                return false;
            }

            SetBlock(host, state, OrderDb.Resolve(state.Leader, host.Type));
            if (state.Block == null)
                return false;

            foreach (Behavior behavior in state.Block.Behaviors)
                behavior.Tick(host);
            return true;
        }

        public override void Exit(Entity host)
        {
            if (host.StateObject.TryGetValue(Id, out object value) && value is FollowState state)
                SetBlock(host, state, null);
            host.StateCooldown.Remove(Id);
            host.StateObject.Remove(Id);
        }

        private FollowState GetState(Entity host)
        {
            if (!host.StateCooldown.ContainsKey(Id))
                host.StateCooldown[Id] = 0;
            if (host.StateObject.TryGetValue(Id, out object value) && value is FollowState state)
                return state;

            state = new FollowState();
            host.StateObject[Id] = state;
            return state;
        }

        private static bool IsLive(Entity leader, Entity host)
        {
            return !leader.Dead && leader.Parent != null && leader.Parent == host.Parent;
        }

        //Nearest live non-player entity within AcquireRange whose type is listed. Strict distance, as in the reference.
        private Entity FindClosestListen(Entity host)
        {
            HashSet<ushort> types = ListenTypes();
            if (types.Count == 0)
                return null;

            Entity closest = null;
            float closestSq = AcquireRange * AcquireRange;
            foreach (Entity en in BehaviorHelpers.NearbyEntities(host, AcquireRange))
            {
                if (en is Player || en.Dead || !types.Contains(en.Type))
                    continue;
                float d = host.Position.DistanceSquared(en.Position);
                if (d < closestSq)
                {
                    closest = en;
                    closestSq = d;
                }
            }
            return closest;
        }

        private HashSet<ushort> ListenTypes()
        {
            if (_listenTypes != null)
                return _listenTypes;

            HashSet<ushort> types = new HashSet<ushort>();
            if (!string.IsNullOrWhiteSpace(ListenIds))
            {
                foreach (string raw in ListenIds.Split(','))
                {
                    string id = raw.Trim();
                    if (id.Length == 0)
                        continue;
                    if (BehaviorHelpers.TryGetObjType(id, out ushort type))
                    {
                        types.Add(type);
                        continue;
                    }
#if DEBUG
                    Program.Print(PrintType.Debug, $"FollowOrders: unknown listen id <{id}>, ignored.");
#endif
                }
            }
            _listenTypes = types;
            return types;
        }

        //Exits the current block's behaviors and enters the next block's, if they differ.
        private static void SetBlock(Entity host, FollowState state, OrderDb.OrderBlock next)
        {
            if (ReferenceEquals(state.Block, next))
                return;

            if (state.Block != null)
                foreach (Behavior behavior in state.Block.Behaviors)
                    behavior.Exit(host);
            state.Block = next;
            if (next != null)
                foreach (Behavior behavior in next.Behaviors)
                    behavior.Enter(host);
        }
    }
}
