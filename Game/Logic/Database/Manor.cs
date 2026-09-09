using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    //Manor of the Immortals behaviors (original AI for this project).
    public class Manor : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Vampire Bat",
                new Prioritize(
                    new Follow(1.4, 10, 2),
                    new Wander(1.2f)
                ),
                new Shoot(8, cooldown: 1400),
                new ItemLoot("Health Potion", 0.05f));
            db.Init("Coffin Creature",
                new Prioritize(
                    new StayCloseToSpawn(0.6),
                    new Wander(0.6f)
                ),
                new Shoot(12, count: 2, shootAngle: 15, cooldown: 1500),
                new ItemLoot("Health Potion", 0.08f));
            db.Init("Nosferatu",
                new Prioritize(
                    new Follow(0.8, 12, 6),
                    new Wander(0.8f)
                ),
                new Shoot(14, count: 3, shootAngle: 12, cooldown: 1500),
                new Shoot(10, index: 1, cooldown: 2200),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Armor Guard",
                new Prioritize(
                    new Follow(0.7, 10, 5),
                    new Wander(0.7f)
                ),
                new Shoot(14, cooldown: 1300),
                new Shoot(10, index: 1, count: 2, shootAngle: 20, cooldown: 2000),
                new ItemLoot("Health Potion", 0.1f));
            db.Init("Hellhound",
                new Prioritize(
                    new Follow(1.3, 12, 3),
                    new Wander(1)
                ),
                new Shoot(10, cooldown: 1100),
                new ItemLoot("Health Potion", 0.08f));
            db.Init("Lesser Bald Vampire",
                new Prioritize(
                    new Follow(1, 10, 5),
                    new Wander(1)
                ),
                new Shoot(12, count: 2, shootAngle: 15, cooldown: 1400),
                new ItemLoot("Health Potion", 0.08f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Lord Ruthven",
                new State("Taunt",
                    new Taunt(true, "Ah, fresh blood visits my manor."),
                    new TimedTransition("Chase", 1500)
                ),
                new State("Chase",
                    new Prioritize(
                        new Follow(0.9, 14, 6),
                        new Wander(0.8f)
                    ),
                    new Shoot(14, count: 3, shootAngle: 12, cooldown: 1200),
                    new Spawn("Vampire Bat", maxChildren: 4, cooldown: 7000),
                    new HpLessTransition(0.5, "Frenzy")
                ),
                new State("Frenzy",
                    new Taunt(true, "You cannot kill what is already dead!"),
                    new Prioritize(
                        new Follow(1.2, 14, 5),
                        new Wander(1)
                    ),
                    new Shoot(14, count: 3, shootAngle: 12, cooldown: 900),
                    new Shoot(12, index: 1, count: 4, shootAngle: 25, cooldown: 2000),
                    new Spawn("Vampire Bat", maxChildren: 4, cooldown: 7000)
                ),
                new Threshold(0.05f,
                    new ItemLoot("Health Potion", 1),
                    new ItemLoot("Magic Potion", 0.5f),
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(5, TierLoot.LootType.Ability, 0.15f),
                    new TierLoot(5, TierLoot.LootType.Ring, 0.1f)));
        }
    }
}
