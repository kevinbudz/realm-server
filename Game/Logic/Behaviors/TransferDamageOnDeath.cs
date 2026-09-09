using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Behaviors
{
    public class TransferDamageOnDeath : Behavior
    {
        public readonly string Target;
        public readonly float Radius;

        public TransferDamageOnDeath(string target, float radius = 50)
        {
            Target = target;
            Radius = radius;
        }

        public override void Death(Entity host)
        {
            if (!(host is Enemy enemy) || enemy.DamageStorage.Count == 0)
                return;

            Entity target = BehaviorHelpers.NearestEntityByName(host, Radius, Target);
            if (target == null || !(target is Enemy targetEnemy) || target.Parent == null)
                return;

            foreach (var kv in enemy.DamageStorage)
            {
                if (targetEnemy.DamageStorage.ContainsKey(kv.Key))
                    targetEnemy.DamageStorage[kv.Key] += kv.Value;
                else
                    targetEnemy.DamageStorage.Add(kv.Key, kv.Value);
            }
        }
    }
}
