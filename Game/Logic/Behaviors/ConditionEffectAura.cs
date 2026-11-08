using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference ConditionEffectAuraBehavior: every Cooldown ms, applies the listed effects to players within
    //Range. Duration -1 is permanent (reference AddPermanentCondition).
    //Remove true is the reference ClearConditionEffectAuraBehavior: every Cooldown ms, removes the listed effects from
    //players within Range instead (Duration is unused).
    //IgnoreInvisible (default true) leaves invisible players alone in both modes, as the reference does; false reaches
    //them too, which in remove mode strips invisibility.
    public class ConditionEffectAura : Behavior
    {
        public readonly ConditionEffectIndex[] Effects;
        public readonly float Range;
        public readonly int Cooldown;
        public readonly int Duration;
        public readonly bool Remove;
        public readonly bool IgnoreInvisible;

        public ConditionEffectAura(string effects, double range = 0, int cooldown = 1000, int duration = -1,
            bool remove = false, bool ignoreInvisible = true)
        {
            Effects = BehaviorHelpers.ParseEffects(effects, "ConditionEffectAura");
            Range = (float)range;
            Cooldown = cooldown;
            Duration = duration;
            Remove = remove;
            IgnoreInvisible = ignoreInvisible;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = 0;
        }

        public override bool Tick(Entity host)
        {
            if (Effects.Length == 0 || Range <= 0 || host.Parent == null)
                return false;

            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Range))
            {
                if (!(en is Player player) || (IgnoreInvisible && player.HasConditionEffect(ConditionEffectIndex.Invisible)))
                    continue;
                foreach (ConditionEffectIndex effect in Effects)
                {
                    if (!Remove)
                        player.ApplyConditionEffect(effect, Duration);
                    else if (player.HasConditionEffect(effect))
                        player.RemoveConditionEffect(effect);
                }
            }

            host.StateCooldown[Id] = Cooldown;
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateCooldown.Remove(Id);
        }
    }
}
