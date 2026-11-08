using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic
{
    //Leader orders for FollowOrders (old GameObject::GetOrders and the XML Orders blocks). Keyed by leader object
    //type, then by state path: the case-insensitive state names from the root down, joined with '/' (for example
    //"rage/animate2"). The path "" holds leader-wide orders that apply in any state. Registration runs during
    //BehaviorDb init and lookups are read-only afterwards, so no locking is needed.
    public static class OrderDb
    {
        public sealed class OrderBlock
        {
            //null when the block applies to every follower.
            public readonly HashSet<ushort> Targets;
            public readonly Behavior[] Behaviors;

            public OrderBlock(HashSet<ushort> targets, Behavior[] behaviors)
            {
                Targets = targets;
                Behaviors = behaviors;
            }

            public bool Applies(ushort followerType)
            {
                return Targets == null || Targets.Contains(followerType);
            }
        }

        private static readonly Dictionary<ushort, Dictionary<string, List<OrderBlock>>> Registry =
            new Dictionary<ushort, Dictionary<string, List<OrderBlock>>>();

        //Null or blank targetIds applies the block to every follower. Registering the same leader, state path and
        //target set again replaces the earlier block in place, so re-running BehaviorDb init is idempotent.
        public static void Register(string leaderId, string leaderStateName, string targetIds, params Behavior[] behaviors)
        {
            if (string.IsNullOrWhiteSpace(leaderId) || !BehaviorHelpers.TryGetObjType(leaderId, out ushort leaderType))
            {
#if DEBUG
                Program.Print(PrintType.Debug, $"OrderDb: unknown leader <{leaderId}>, orders ignored.");
#endif
                return;
            }

            HashSet<ushort> targets = ParseTargets(targetIds, out bool valid);
            if (!valid)
                return;

            string path = NormalizePath(leaderStateName);
            OrderBlock block = new OrderBlock(targets, behaviors ?? Array.Empty<Behavior>());

            if (!Registry.TryGetValue(leaderType, out Dictionary<string, List<OrderBlock>> byPath))
            {
                byPath = new Dictionary<string, List<OrderBlock>>();
                Registry[leaderType] = byPath;
            }
            if (!byPath.TryGetValue(path, out List<OrderBlock> blocks))
            {
                blocks = new List<OrderBlock>();
                byPath[path] = blocks;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                if (SameTargets(blocks[i].Targets, targets))
                {
                    blocks[i] = block;
                    return;
                }
            }
            blocks.Add(block);
        }

        //Mirrors GameObject::GetOrders: the leader's current states are tried deepest first, each as its state path,
        //then the leader-wide "" blocks. The first block that applies to the follower wins. Null if none applies.
        public static OrderBlock Resolve(Entity leader, ushort followerType)
        {
            if (leader == null || !Registry.TryGetValue(leader.Type, out Dictionary<string, List<OrderBlock>> byPath))
                return null;

            List<State> states = leader.CurrentStates;
            if (states != null)
            {
                for (int i = states.Count - 1; i >= 0; i--)
                {
                    if (byPath.TryGetValue(PathTo(states, i), out List<OrderBlock> blocks))
                    {
                        OrderBlock found = FirstApplying(blocks, followerType);
                        if (found != null)
                            return found;
                    }
                }
            }

            if (byPath.TryGetValue("", out List<OrderBlock> wide))
                return FirstApplying(wide, followerType);
            return null;
        }

        //"Rage/Animate2" becomes "rage/animate2". Null or blank means leader-wide.
        public static string NormalizePath(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
                return "";

            StringBuilder path = new StringBuilder();
            foreach (string raw in stateName.Split('/'))
            {
                string segment = raw.Trim().ToLower();
                if (segment.Length == 0)
                    continue;
                if (path.Length > 0)
                    path.Append('/');
                path.Append(segment);
            }
            return path.ToString();
        }

        //null or blank means every follower. Ids that do not resolve are dropped; valid is false when ids were
        //given but none resolved, in which case the registration is skipped.
        private static HashSet<ushort> ParseTargets(string targetIds, out bool valid)
        {
            valid = true;
            if (string.IsNullOrWhiteSpace(targetIds))
                return null;

            HashSet<ushort> types = new HashSet<ushort>();
            foreach (string raw in targetIds.Split(','))
            {
                string id = raw.Trim();
                if (id.Length == 0)
                    continue;
                if (!BehaviorHelpers.TryGetObjType(id, out ushort type))
                {
#if DEBUG
                    Program.Print(PrintType.Debug, $"OrderDb: unknown target <{id}>, ignored.");
#endif
                    continue;
                }
                types.Add(type);
            }
            valid = types.Count > 0;
            return types;
        }

        private static string PathTo(List<State> states, int last)
        {
            StringBuilder path = new StringBuilder();
            for (int i = 0; i <= last; i++)
            {
                if (i > 0)
                    path.Append('/');
                path.Append(states[i].StringId);
            }
            return path.ToString();
        }

        private static OrderBlock FirstApplying(List<OrderBlock> blocks, ushort followerType)
        {
            foreach (OrderBlock block in blocks)
                if (block.Applies(followerType))
                    return block;
            return null;
        }

        private static bool SameTargets(HashSet<ushort> a, HashSet<ushort> b)
        {
            if (a == null || b == null)
                return a == b;
            return a.SetEquals(b);
        }
    }
}
