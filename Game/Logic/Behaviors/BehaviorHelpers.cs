using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
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
            //W05: answered by World.StaticGrid (same predicate, cells only), not a scan of Statics. Collected
            //before yielding, in the old Statics dictionary order, so a caller that mutates the world while
            //iterating does not hit a dictionary-modified error.
            List<StaticObject> statics = new List<StaticObject>();
            host.Parent.StaticGrid.Collect(host.Position, range, statics, inDictionaryOrder: true);
            foreach (StaticObject en in statics)
                if (!en.Equals(host))
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
            List<StaticObject> statics = StaticIndex.Scratch;
            host.Parent.StaticGrid.Collect(host.Position, radius, statics);
            foreach (StaticObject en in statics)
                if (en.Type == type)
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

        public static void BroadcastNearby(Entity host, ReadOnlySpan<byte> packet, float? radius = null)
        {
            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, radius ?? Player.SightRadius))
                if (en is Player player)
                    player.Client.Send(packet);
        }

        //Players who can see host: within sight radius and tracking it in Entities. Collected eagerly, in hit-test order.
        public static Player[] GetViewers(Entity host)
        {
            List<Player> viewers = new List<Player>();
            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Player.SightRadius))
                if (en is Player player && player.Entities.Contains(host))
                    viewers.Add(player);
            return viewers.ToArray();
        }

        public static void BroadcastToViewers(Entity host, ReadOnlySpan<byte> packet)
        {
            foreach (Player player in GetViewers(host))
                player.Client.Send(packet);
        }

        //Sends one volley of count projectiles starting at startAngle, stepping angleStep radians, to the viewers of host.
        //Each viewer is told about the projectiles first so their hits resolve.
        public static void SendVolley(Entity host, ProjectileDesc desc, float startAngle, float angleStep, int count)
        {
            int damage = desc.Damage;
            int startId = host.Parent.NextProjectileId;
            host.Parent.NextProjectileId += count;

            List<Projectile> projectiles = new List<Projectile>();
            for (int k = 0; k < count; k++)
                projectiles.Add(new Projectile(host, desc, startId + k, Manager.TotalTime, startAngle + angleStep * k, host.Position, damage));

            ReadOnlySpan<byte> packet = GameServer.EnemyShoot(startId, host.Id, desc.BulletType, host.Position, startAngle, (short)damage, (byte)count, angleStep);
            foreach (Player player in GetViewers(host))
            {
                player.AwaitProjectiles(projectiles);
                player.Client.Send(packet);
            }
        }

        //Steps host up to step tiles toward dest without passing it. False when already there (within 0.0001) or when
        //the move did not change the position (blocked).
        public static bool MoveToward(Entity host, Position dest, float step)
        {
            Position from = host.Position;
            float dist = from.Distance(dest);
            if (dist <= 0.0001f)
                return false;
            host.ValidateAndMove(from + (dest - from) * (MathF.Min(step, dist) / dist));
            return host.Position != from;
        }

        //Condition effect by name (case-insensitive, trimmed). Unknown names give Nothing; owner names the caller in the
        //DEBUG diagnostic.
        public static ConditionEffectIndex ParseEffect(string name, string owner)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ConditionEffectIndex.Nothing;
            foreach (ConditionEffectIndex index in Enum.GetValues(typeof(ConditionEffectIndex)))
                if (index != ConditionEffectIndex.Nothing && string.Equals(index.ToString(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return index;
#if DEBUG
            Program.Print(PrintType.Error, $"{owner}: unknown condition effect <{name}>.");
#endif
            return ConditionEffectIndex.Nothing;
        }

        //Comma-separated condition effect names. Unknown names are dropped.
        public static ConditionEffectIndex[] ParseEffects(string names, string owner)
        {
            List<ConditionEffectIndex> list = new List<ConditionEffectIndex>();
            if (string.IsNullOrWhiteSpace(names))
                return list.ToArray();

            foreach (string raw in names.Split(','))
            {
                string name = raw.Trim();
                if (name.Length == 0)
                    continue;
                ConditionEffectIndex? match = null;
                foreach (ConditionEffectIndex index in Enum.GetValues(typeof(ConditionEffectIndex)))
                {
                    if (index != ConditionEffectIndex.Nothing && string.Equals(index.ToString(), name, StringComparison.OrdinalIgnoreCase))
                    {
                        match = index;
                        break;
                    }
                }
                if (match != null)
                    list.Add(match.Value);
#if DEBUG
                else
                    Program.Print(PrintType.Error, $"{owner}: unknown condition effect <{name}>.");
#endif
            }
            return list.ToArray();
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
