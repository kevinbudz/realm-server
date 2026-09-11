using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Logic.Behaviors
{
    internal static class BehaviorHelpers
    {
        public static ushort GetObjType(string id)
        {
            if (Resources.Id2Object.TryGetValue(id, out ObjectDesc desc))
                return desc.Type;
            if (Resources.IdLower2Object.TryGetValue(id.ToLower(), out desc))
                return desc.Type;
            throw new Exception($"Unknown entity '{id}'");
        }

        public static bool TryGetObjType(string id, out ushort type)
        {
            if (Resources.Id2Object.TryGetValue(id, out ObjectDesc desc) ||
                Resources.IdLower2Object.TryGetValue(id.ToLower(), out desc))
            {
                type = desc.Type;
                return true;
            }
            type = 0;
            return false;
        }

        public static ushort[] GetGroupTypes(string group)
        {
            return Resources.Type2Object.Values
                .Where(d => d.Group == group)
                .Select(d => d.Type)
                .ToArray();
        }

        public static bool MatchesName(Entity entity, string name)
        {
            return entity.Desc.DisplayId == name || entity.Desc.Id == name;
        }

        public static IEnumerable<Entity> NearbyEntities(Entity host, float range)
        {
            foreach (Entity en in host.Parent.EntityChunks.HitTest(host.Position, range))
                if (!en.Equals(host))
                    yield return en;
            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, range))
                if (!en.Equals(host))
                    yield return en;
            //GameObject entities (e.g. Dr Terrible Bubble, Monster Cage)
            //live in World.Statics, not EntityChunks (see
            //World.MoveEntity), so chunk-only queries never see them.
            //realm-src-master keeps statics in EnemiesCollision, meaning
            //Orders and name transitions reach them there. Yield in-range
            //statics here for parity; name/group filters downstream keep
            //this narrow to queries that actually name a static.
            float r2 = range * range;
            foreach (Entity en in host.Parent.Statics.Values)
                if (!en.Equals(host) && host.Position.DistanceSquared(en.Position) < r2)
                    yield return en;
        }

        public static Entity NearestEntity(Entity host, float range, ushort? type = null)
        {
            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in NearbyEntities(host, range))
            {
                if (type != null && en.Type != type.Value)
                    continue;
                float d = host.Position.DistanceSquared(en.Position);
                if (d < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }

        public static Entity NearestEntityByName(Entity host, float range, string name)
        {
            if (name == null)
                return NearestEntity(host, range);
            if (TryGetObjType(name, out ushort type))
                return NearestEntity(host, range, type);
            Entity nearest = null;
            float dist = float.MaxValue;
            foreach (Entity en in NearbyEntities(host, range))
            {
                if (!MatchesName(en, name))
                    continue;
                float d = host.Position.DistanceSquared(en.Position);
                if (d < dist)
                {
                    nearest = en;
                    dist = d;
                }
            }
            return nearest;
        }

        public static IEnumerable<Entity> EntitiesByName(Entity host, float range, string name)
        {
            ushort type = 0;
            bool hasType = name != null && TryGetObjType(name, out type);
            foreach (Entity en in NearbyEntities(host, range))
                if (name == null || (hasType && en.Type == type) || MatchesName(en, name))
                    yield return en;
        }

        public static IEnumerable<Entity> EntitiesByGroup(Entity host, float range, string group)
        {
            foreach (Entity en in NearbyEntities(host, range))
                if (en.Desc.Group == group)
                    yield return en;
        }

        public static int CountEntities(Entity host, float radius, ushort type)
        {
            int count = 0;
            foreach (Entity en in host.Parent.EntityChunks.HitTest(host.Position, radius))
                if (en.Type == type)
                    count++;
            //See NearbyEntities: statics are not in EntityChunks, but
            //realm-src-master counts them via EnemiesCollision.
            float r2 = radius * radius;
            foreach (Entity en in host.Parent.Statics.Values)
                if (en.Type == type && host.Position.DistanceSquared(en.Position) < r2)
                    count++;
            return count;
        }

        public static Entity SpawnChild(Entity host, ushort type, Position at)
        {
            return SpawnChild(host.Parent, type, at);
        }

        public static Entity SpawnChild(World world, ushort type, Position at)
        {
            if (world == null)
                return null;
            Entity entity = Entity.Resolve(type);
            entity.Spawned = true;
            if (world.AddEntity(entity, at) == -1)
                return null;
            return entity;
        }

        public static void BroadcastNearby(Entity host, byte[] packet, float? radius = null)
        {
            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, radius ?? Player.SightRadius))
                if (en is Player player)
                    player.Client.Send(packet);
        }

        public static void BroadcastToViewers(Entity host, byte[] packet)
        {
            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Player.SightRadius))
                if (en is Player player && player.Entities.Contains(host))
                    player.Client.Send(packet);
        }

        public static State FindState(IEnumerable<State> roots, string name)
        {
            string id = name.ToLower();
            foreach (State state in roots)
            {
                if (state.StringId == id)
                    return state;
                State found = FindState(state.States.Values, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        public static bool SwitchToState(Entity target, string name)
        {
            if (target.Behavior == null || target.CurrentStates == null)
                return false;

            State state = FindState(target.Behavior.States.Values, name);
            if (state == null || target.CurrentStates.Contains(state))
                return false;

            for (int k = target.CurrentStates.Count - 1; k >= 0; k--)
            {
                foreach (Behavior behavior in target.CurrentStates[k].Behaviors)
                    behavior.Exit(target);
                foreach (Transition transition in target.CurrentStates[k].Transitions)
                    transition.Exit(target);
            }
            target.CurrentStates.Clear();

            Dictionary<int, State> roots = target.Behavior.States;
            List<State> path = new List<State>();
            if (!FindPath(roots.Values, state, path))
                return false;

            State leaf = path.Last();
            while (leaf.States.Count > 0)
            {
                leaf = leaf.States.Values.First();
                path.Add(leaf);
            }

            foreach (State s in path)
            {
                target.CurrentStates.Add(s);
                foreach (Behavior behavior in s.Behaviors)
                    behavior.Enter(target);
                foreach (Transition transition in s.Transitions)
                    transition.Enter(target);
            }
            return true;
        }

        private static bool FindPath(IEnumerable<State> roots, State target, List<State> path)
        {
            foreach (State state in roots)
            {
                path.Add(state);
                if (state == target)
                    return true;
                if (FindPath(state.States.Values, target, path))
                    return true;
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }
    }
}
