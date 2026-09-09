using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    //Pirate Cave behaviors (original AI for this project).
    public class PirateCave : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Cave Pirate Brawler",
                new Prioritize(
                    new StayCloseToSpawn(1),
                    new Wander(1)
                ),
                new Shoot(10, cooldown: 1200),
                new ItemLoot("Health Potion", 0.05f));
            db.Init("Cave Pirate Sailor",
                new Prioritize(
                    new StayCloseToSpawn(1),
                    new Wander(1)
                ),
                new Shoot(10, cooldown: 1200),
                new ItemLoot("Health Potion", 0.05f));
            db.Init("Cave Pirate Veteran",
                new Prioritize(
                    new Follow(1, 10, 5),
                    new Wander(1)
                ),
                new Shoot(10, cooldown: 1000),
                new ItemLoot("Health Potion", 0.08f));
            db.Init("Pirate Lieutenant",
                new Prioritize(
                    new Follow(1, 10, 5),
                    new Wander(1)
                ),
                new Shoot(12, count: 2, shootAngle: 10, cooldown: 1200),
                new ItemLoot("Health Potion", 0.08f));
            db.Init("Pirate Commander",
                new Prioritize(
                    new Follow(1, 10, 5),
                    new Wander(1)
                ),
                new Shoot(12, count: 3, shootAngle: 12, cooldown: 1400),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Dreadstump the Pirate King",
                new State("Taunt",
                    new Taunt(true, "Arr! Who dares board me ship?"),
                    new TimedTransition("Chase", 1500)
                ),
                new State("Chase",
                    new Prioritize(
                        new Follow(0.8, 12, 4),
                        new Wander(0.8f)
                    ),
                    new Shoot(12, count: 3, shootAngle: 15, cooldown: 1200),
                    new Shoot(8, index: 1, cooldown: 2500),
                    new Spawn("Cave Pirate Brawler", maxChildren: 3, cooldown: 8000),
                    new HpLessTransition(0.4, "Enraged")
                ),
                new State("Enraged",
                    new Taunt(true, "Ye'll feed the fishes!"),
                    new Prioritize(
                        new Follow(1.2, 12, 4),
                        new Wander(1)
                    ),
                    new Shoot(12, count: 5, shootAngle: 12, cooldown: 1000),
                    new Shoot(8, index: 1, cooldown: 1800)
                ),
                new Threshold(0.05f,
                    new ItemLoot("Health Potion", 1),
                    new ItemLoot("Magic Potion", 0.5f),
                    new TierLoot(4, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(4, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.1f)));
        }
    }
}
