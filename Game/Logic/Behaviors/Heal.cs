using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class HealState
    {
        public HashSet<ushort> Types;
        public int CooldownLeft;
    }

    //Port of the reference HealBehavior: every Cooldown ms, heals up to MaxTargets damaged entities of the target types
    //within Range by MaxHeal (-1 = to full). Target types come from TargetIds and TargetGroups; with neither, the
    //host's own group is used. Without targets the behavior retries after 1 second.
    public class Heal : Behavior
    {
        public readonly float Range;
        public readonly int Cooldown;
        public readonly string TargetIds;
        public readonly string TargetGroups;
        public readonly int MaxTargets;
        public readonly int MaxHeal;
        public readonly bool HideVisual;

        public Heal(double range = 10, int cooldown = 10000, string targetIds = null, string targetGroups = null,
            int maxTargets = -1, int maxHeal = -1, bool hideVisual = false)
        {
            Range = (float)range;
            Cooldown = cooldown;
            TargetIds = targetIds;
            TargetGroups = targetGroups;
            MaxTargets = maxTargets;
            MaxHeal = maxHeal;
            HideVisual = hideVisual;
        }

        public override void Enter(Entity host)
        {
            //The host's group depends on the entity, and the instance may be shared by several types, so targets are
            //resolved per entity.
            HashSet<ushort> types = new HashSet<ushort>();
            if (!string.IsNullOrWhiteSpace(TargetIds))
            {
                foreach (string raw in TargetIds.Split(','))
                {
                    string name = raw.Trim();
                    if (name.Length == 0)
                        continue;
                    if (BehaviorHelpers.TryGetObjType(name, out ushort type))
                        types.Add(type);
                    else
                    {
#if DEBUG
                        Program.Print(PrintType.Error, $"Heal: unknown target <{name}>.");
#endif
                    }
                }
            }

            bool hasIds = types.Count > 0;
            if (TargetGroups != null)
            {
                foreach (string raw in TargetGroups.Split(','))
                {
                    string group = raw.Trim();
                    if (group.Length > 0)
                        foreach (ushort type in BehaviorHelpers.GetGroupTypes(group))
                            types.Add(type);
                }
            }
            else if (!hasIds && host.Desc.Group != null)
            {
                foreach (ushort type in BehaviorHelpers.GetGroupTypes(host.Desc.Group))
                    types.Add(type);
            }

            host.StateObject[Id] = new HealState { Types = types, CooldownLeft = Cooldown };
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is HealState state))
                return false;

            state.CooldownLeft -= Settings.MillisecondsPerTick;
            if (state.CooldownLeft > 0)
                return false;

            List<Entity> needing = new List<Entity>();
            float rangeSq = Range * Range;
            foreach (Entity en in BehaviorHelpers.NearbyEntities(host, Range))
            {
                if (!state.Types.Contains(en.Type) || en.HP >= en.MaxHP)
                    continue;
                if (host.Position.DistanceSquared(en.Position) >= rangeSq)
                    continue;
                needing.Add(en);
            }
            if (MaxTargets >= 0 && needing.Count > MaxTargets)
                needing.RemoveRange(MaxTargets, needing.Count - MaxTargets);

            if (needing.Count == 0)
            {
                state.CooldownLeft = 1000;
                return false;
            }

            foreach (Entity target in needing)
            {
                int amount = MaxHeal < 0 ? target.MaxHP : MaxHeal;
                if (!HideVisual)
                    BehaviorHelpers.BroadcastNearby(target, GameServer.ShowEffect(ShowEffectIndex.Line, target.Id, 0xFFFFFF, host.Position));
                HealTarget(target, amount);
            }

            state.CooldownLeft = Cooldown;
            return true;
        }

        private void HealTarget(Entity target, int amount)
        {
            int before = target.HP;
            target.HP = Math.Min(target.MaxHP, target.HP + amount);
            int healed = target.HP - before;
            if (HideVisual || healed <= 0)
                return;

            BehaviorHelpers.BroadcastNearby(target, GameServer.ShowEffect(ShowEffectIndex.Heal, target.Id, 0xffffffff));
            BehaviorHelpers.BroadcastNearby(target, GameServer.Notification(target.Id, "+" + healed, 0xff00ff00));
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
