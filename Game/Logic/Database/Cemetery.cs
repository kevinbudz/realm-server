using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    //Haunted Cemetery behaviors (original AI for this project).
    public class Cemetery : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Blue Zombie",
                new Prioritize(
                    new Follow(0.9, 10, 2),
                    new Wander(0.9f)
                ),
                new Shoot(9, cooldown: 1400),
                new ItemLoot("Health Potion", 0.06f));
            db.Init("Zombie Hulk",
                new Prioritize(
                    new Follow(0.8, 12, 3),
                    new Wander(0.8f)
                ),
                new Shoot(9, count: 2, shootAngle: 20, cooldown: 1500),
                new Shoot(11, index: 1, cooldown: 2200),
                new ItemLoot("Health Potion", 0.1f));
            db.Init("Classic Ghost",
                new Prioritize(
                    new Follow(1, 10, 5),
                    new Wander(1)
                ),
                new Shoot(11, cooldown: 1300),
                new ItemLoot("Health Potion", 0.08f),
                new ItemLoot("Magic Potion", 0.04f));
            db.Init("Werewolf",
                new Prioritize(
                    new Follow(1.3, 12, 3),
                    new Wander(1)
                ),
                new Shoot(10, count: 2, shootAngle: 15, cooldown: 1200),
                new ItemLoot("Health Potion", 0.1f));
            db.Init("Ghost of Skuld",
                new Prioritize(
                    new Follow(0.8, 14, 7),
                    new Wander(0.8f)
                ),
                new Shoot(13, count: 3, shootAngle: 12, cooldown: 1400),
                new Shoot(15, index: 1, cooldown: 2500),
                new ItemLoot("Health Potion", 0.15f),
                new ItemLoot("Magic Potion", 0.1f));
            db.Init("Flying Flame Skull",
                new Prioritize(
                    new Follow(1.4, 12, 4),
                    new Wander(1.2f)
                ),
                new Shoot(9, cooldown: 1000),
                new ItemLoot("Health Potion", 0.06f));
        }
    }
}
