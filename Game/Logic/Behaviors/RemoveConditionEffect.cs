using RotMG.Common;

namespace RotMG.Game.Logic.Behaviors
{
    public class RemoveConditionEffect : Behavior
    {
        public readonly ConditionEffectIndex Effect;

        public RemoveConditionEffect(ConditionEffectIndex effect)
        {
            Effect = effect;
        }

        public override void Enter(Entity host)
        {
            host.RemoveConditionEffect(Effect);
        }
    }
}
