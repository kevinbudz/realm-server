using System;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class ShadowState
    {
        public bool Reshadowing;
    }

    //Port of the reference ShadowBehavior: a Pet follows its owner. It idles inside Range, starts moving when it falls
    //behind and stops once back within ReshadowRange, and teleports when more than 30 tiles away. The reference's
    //master id is the Pet's PlayerOwner here. The Pet is removed when the owner has left the world.
    public class Shadow : Behavior
    {
        private const float TeleportDistance = 30;

        public readonly float Range;
        public readonly float Speed;
        public readonly float ReshadowRange;

        public Shadow(double range = 3, double speed = 3, double reshadowRange = 0.5)
        {
            Range = (float)range;
            Speed = (float)speed;
            ReshadowRange = (float)reshadowRange;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new ShadowState();
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is ShadowState state))
                return false;
            Player owner = (host as Pet)?.PlayerOwner;
            if (owner == null || host.Parent == null)
                return false;
            if (owner.Parent == null)
            {
                host.Parent.RemoveEntity(host);
                return false;
            }
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed) || Range <= 0)
                return false;

            float dist = host.Position.Distance(owner.Position);
            if (dist <= ReshadowRange && state.Reshadowing)
            {
                state.Reshadowing = false;
                return false;
            }
            if (dist > TeleportDistance)
            {
                host.Parent.MoveEntity(host, owner.Position);
                return true;
            }
            if (dist < Range && !state.Reshadowing)
                return false;

            state.Reshadowing = true;
            float step = host.GetSpeed(Speed) * Settings.SecondsPerTick * (dist / Range);
            return BehaviorHelpers.MoveToward(host, owner.Position, step);
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
