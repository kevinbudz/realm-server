using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    //Forbidden Jungle behaviors (original AI for this project).
    public class Jungle : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Great Temple Snake",
                new Prioritize(
                    new Follow(1, 10, 4),
                    new Wander(1)
                ),
                new Shoot(10, count: 2, shootAngle: 12, cooldown: 1200),
                new ItemLoot("Health Potion", 0.06f));
            db.Init("Great Coil Snake",
                new Prioritize(
                    new Follow(0.9, 12, 5),
                    new Wander(0.9f)
                ),
                new Shoot(14, cooldown: 1300),
                new Shoot(9, index: 1, count: 3, shootAngle: 20, cooldown: 2000),
                new ItemLoot("Health Potion", 0.1f));
            db.Init("Basilisk Baby",
                new Prioritize(
                    new Follow(1.1, 10, 3),
                    new Wander(1)
                ),
                new Shoot(9, cooldown: 1300),
                new ItemLoot("Health Potion", 0.05f));
            db.Init("Basilisk",
                new Prioritize(
                    new Follow(1, 12, 5),
                    new Wander(1)
                ),
                new Shoot(11, count: 2, shootAngle: 15, cooldown: 1400),
                new Shoot(9, index: 2, count: 2, shootAngle: 25, cooldown: 2200),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Mask Shaman",
                new Prioritize(
                    new StayCloseToSpawn(0.8),
                    new Wander(0.8f)
                ),
                new Shoot(12, cooldown: 1500),
                new ItemLoot("Health Potion", 0.06f));
            db.Init("Mask Warrior",
                new Prioritize(
                    new Follow(1, 10, 3),
                    new Wander(1)
                ),
                new Shoot(9, cooldown: 1300),
                new ItemLoot("Health Potion", 0.08f));
            db.Init("Mask Hunter",
                new Prioritize(
                    new StayBack(0.9, 6),
                    new Wander(0.9f)
                ),
                new Shoot(13, cooldown: 1100),
                new ItemLoot("Health Potion", 0.08f));
            db.Init("Mixcoatl the Masked God",
                new State("Taunt",
                    new Taunt(true, "The jungle provides... your corpse will feed it."),
                    new TimedTransition("Rings", 1500)
                ),
                new State("Rings",
                    new Prioritize(
                        new Follow(0.7, 14, 7),
                        new Wander(0.7f)
                    ),
                    new Shoot(12, count: 8, shootAngle: 45, index: 1, cooldown: 1800),
                    new Shoot(10, index: 0, cooldown: 800),
                    new TimedTransition("Bolts", 6000)
                ),
                new State("Bolts",
                    new Prioritize(
                        new Follow(1, 14, 6),
                        new Wander(1)
                    ),
                    new Shoot(13, count: 3, shootAngle: 10, index: 2, cooldown: 1000),
                    new Spawn("Mask Warrior", maxChildren: 3, cooldown: 9000),
                    new TimedTransition("Rings", 6000)
                ),
                new Threshold(0.05f,
                    new ItemLoot("Health Potion", 1),
                    new ItemLoot("Magic Potion", 0.5f),
                    new TierLoot(5, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(5, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.15f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.1f)));
        }
    }
}
