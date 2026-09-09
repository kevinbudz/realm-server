using RotMG.Common;
using RotMG.Game.Entities;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    public class ConditionEffectRegion : Behavior
    {
        public readonly ConditionEffectIndex[] Effects;
        public readonly int Range;
        public readonly int Duration;

        public ConditionEffectRegion(ConditionEffectIndex effect, int range = 2, int duration = -1)
            : this(new ConditionEffectIndex[] { effect }, range, duration) { }

        public ConditionEffectRegion(ConditionEffectIndex[] effects, int range = 2, int duration = -1)
        {
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Range = range;
            Duration = duration;
        }

        public override bool Tick(Entity host)
        {
            if (host.Parent == null)
                return false;

            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Range))
                if (en is Player player)
                    foreach (ConditionEffectIndex effect in Effects)
                        player.ApplyConditionEffect(effect, Duration);
            return true;
        }
    }
}
