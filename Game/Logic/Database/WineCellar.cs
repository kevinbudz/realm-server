using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    //Oryx's Wine Cellar behaviors (original AI for this project).
    public class WineCellar : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Aberrant Blaster",
                new Prioritize(
                    new StayBack(0.9, 7),
                    new Wander(0.9f)
                ),
                new Shoot(13, cooldown: 1100),
                new ItemLoot("Health Potion", 0.08f));
            db.Init("Monstrosity Scarab",
                new Prioritize(
                    new Follow(1.1, 10, 3),
                    new Wander(1)
                ),
                new Shoot(10, cooldown: 1200),
                new ItemLoot("Health Potion", 0.08f));
            db.Init("Purple Goo",
                new Prioritize(
                    new Follow(0.6, 10, 4),
                    new Wander(0.6f)
                ),
                new Shoot(10, count: 4, shootAngle: 30, cooldown: 1800),
                new ItemLoot("Health Potion", 0.1f));
            db.Init("Vintner of Oryx",
                new Prioritize(
                    new Follow(0.9, 12, 6),
                    new Wander(0.9f)
                ),
                new Shoot(12, cooldown: 1300),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Abomination of Oryx",
                new State("Taunt",
                    new Taunt(true, "Oryx provides. DRINK!"),
                    new TimedTransition("Pour", 1500)
                ),
                new State("Pour",
                    new Prioritize(
                        new Follow(0.9, 14, 6),
                        new Wander(0.9f)
                    ),
                    new Shoot(13, count: 3, shootAngle: 15, index: 2, cooldown: 1100),
                    new Shoot(11, count: 5, shootAngle: 20, index: 0, cooldown: 1900),
                    new Spawn("Aberrant Blaster", maxChildren: 3, cooldown: 8000),
                    new HpLessTransition(0.45, "Overwhelming")
                ),
                new State("Overwhelming",
                    new Taunt(true, "DROWN IN HIS BOUNTY!"),
                    new Prioritize(
                        new Follow(1.1, 14, 5),
                        new Wander(1)
                    ),
                    new Shoot(13, count: 8, shootAngle: 45, index: 4, cooldown: 2000),
                    new Shoot(11, count: 3, shootAngle: 15, index: 3, cooldown: 1200)
                ),
                new Threshold(0.05f,
                    new ItemLoot("Health Potion", 1),
                    new ItemLoot("Magic Potion", 0.5f),
                    new ItemLoot("Wine Cellar Incantation", 0.1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(7, TierLoot.LootType.Ability, 0.15f),
                    new TierLoot(7, TierLoot.LootType.Ring, 0.1f)));
        }
    }
}
