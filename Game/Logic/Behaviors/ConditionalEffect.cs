using RotMG.Common;

namespace RotMG.Game.Logic.Behaviors
{
    public class ConditionalEffect : Behavior
    {
        public readonly ConditionEffectIndex Effect;
        public readonly bool Permanent;
        public readonly int Duration;

        public ConditionalEffect(ConditionEffectIndex effect, bool perm = false, int duration = -1)
        {
            Effect = effect;
            Permanent = perm;
            Duration = duration;
        }

        public override void Enter(Entity host)
        {
            host.ApplyConditionEffect(Effect, Duration);
        }

        public override void Exit(Entity host)
        {
            if (!Permanent)
                host.RemoveConditionEffect(Effect);
        }
    }
}
