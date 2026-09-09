using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class TransformOnDeath : Behavior
    {
        public readonly string Target;
        public readonly int Min;
        public readonly int Max;
        public readonly float Probability;

        public TransformOnDeath(string target, int min = 1, int max = 1, double probability = 1)
        {
            Target = target;
            Min = min;
            Max = max;
            Probability = (float)probability;
        }

        public override void Death(Entity host)
        {
            if (host.Parent == null || !MathUtils.Chance(Probability))
                return;
            if (!BehaviorHelpers.TryGetObjType(Target, out ushort type))
                return;

            int count = MathUtils.Next(Max - Min + 1) + Min;
            for (int i = 0; i < count; i++)
                BehaviorHelpers.SpawnChild(host, type, host.Position);
        }
    }
}
