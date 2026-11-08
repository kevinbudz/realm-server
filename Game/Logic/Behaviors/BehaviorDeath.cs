using System.Collections.Generic;
using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Behaviors
{
    //Shared death for behaviors that kill their own host without a damage event (reference Kill(NULL, ..., dropLoot, giveXP)).
    //With dropLoot, the host dies through Enemy.Death for its top damage dealer. That path awards loot, XP, quest and
    //dungeon completion, kill stats, the death hooks, and the removal, so this helper must not remove the host again.
    //Enemy.Death always awards XP; giveXP is therefore implied whenever the loot path runs, and no generated call site
    //sets dropLoot without giveXP. Without dropLoot, or when no live non-bot player dealt damage, the death hooks run and
    //the host is removed directly (no loot, no XP, no kill stats).
    internal static class BehaviorDeath
    {
        public static void Die(Entity host, bool dropLoot)
        {
            if (host.Parent == null)
                return;

            if (dropLoot && host is Enemy enemy)
            {
                Player killer = TopDamager(enemy);
                if (killer != null)
                {
                    enemy.Death(killer);
                    return;
                }
            }

            if (host.Behavior != null)
            {
                foreach (Behavior behavior in host.Behavior.Behaviors)
                    behavior.Death(host);
                foreach (State state in host.CurrentStates)
                    foreach (Behavior behavior in state.Behaviors)
                        behavior.Death(host);
            }
            host.Dead = true;
            host.Parent.RemoveEntity(host);
        }

        //Highest damage dealer still in the host's world. Zero-damage records (hits on an invulnerable host) do not count,
        //and bots never receive credit, as in Enemy.Death.
        private static Player TopDamager(Enemy enemy)
        {
            Player best = null;
            int bestDamage = 0;
            foreach (KeyValuePair<int, int> record in enemy.DamageStorage)
            {
                if (record.Value <= bestDamage)
                    continue;
                if (enemy.Parent.GetEntity(record.Key) is Player player && !(player is Bot))
                {
                    best = player;
                    bestDamage = record.Value;
                }
            }
            return best;
        }
    }
}
