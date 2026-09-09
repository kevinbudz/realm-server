using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    //Davy Jones's Locker behaviors (original AI for this project).
    public class DavyJones : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Davy Jones",
                new State("Taunt",
                    new Taunt(true, "Your soul belongs to the Locker now."),
                    new TimedTransition("Broadside", 1500)
                ),
                new State("Broadside",
                    new Prioritize(
                        new Follow(0.8, 14, 7),
                        new Wander(0.8f)
                    ),
                    new Shoot(14, count: 5, shootAngle: 15, cooldown: 1400),
                    new Spawn("Fishman Warrior", maxChildren: 3, cooldown: 8000),
                    new HpLessTransition(0.5, "Frenzy")
                ),
                new State("Frenzy",
                    new Taunt(true, "The sea claims its due!"),
                    new Prioritize(
                        new Follow(1.1, 14, 6),
                        new Wander(1)
                    ),
                    new Shoot(14, count: 5, shootAngle: 15, cooldown: 1000),
                    new Shoot(12, index: 1, count: 3, shootAngle: 30, cooldown: 2200),
                    new Spawn("Fishman Warrior", maxChildren: 3, cooldown: 8000)
                ),
                new Threshold(0.05f,
                    new ItemLoot("Health Potion", 1),
                    new ItemLoot("Magic Potion", 0.5f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(6, TierLoot.LootType.Ability, 0.15f),
                    new TierLoot(6, TierLoot.LootType.Ring, 0.1f)));
        }
    }
}
