using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    //Dreamscape Labyrinth behaviors (original AI for this project).
    public class Dreamscape : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Assassin of Oryx",
                new Prioritize(
                    new Follow(1.4, 12, 4),
                    new Wander(1.2f)
                ),
                new Shoot(12, count: 2, shootAngle: 12, cooldown: 1100),
                new Shoot(10, index: 1, cooldown: 1900),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Minion of Oryx",
                new Prioritize(
                    new Follow(1, 12, 5),
                    new Wander(1)
                ),
                new Shoot(12, count: 3, shootAngle: 12, cooldown: 1300),
                new Shoot(11, index: 1, count: 2, shootAngle: 20, cooldown: 2000),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Oryx the Mad God 1",
                new State("Taunt",
                    new Taunt(true, "You dare enter MY dreamscape?"),
                    new TimedTransition("Wake", 1500)
                ),
                new State("Wake",
                    new Prioritize(
                        new Follow(0.7, 16, 8),
                        new Wander(0.7f)
                    ),
                    new Shoot(15, count: 4, shootAngle: 12, cooldown: 1100),
                    new Shoot(13, index: 1, count: 3, shootAngle: 30, cooldown: 1900),
                    new Spawn("Minion of Oryx", maxChildren: 3, cooldown: 9000),
                    new HpLessTransition(0.6, "Nightmare")
                ),
                new State("Nightmare",
                    new Taunt(true, "SLEEP NOW. FOREVER."),
                    new Prioritize(
                        new Follow(0.9, 16, 7),
                        new Wander(0.9f)
                    ),
                    new Shoot(15, count: 6, shootAngle: 20, index: 4, cooldown: 1600),
                    new Shoot(13, count: 3, shootAngle: 12, index: 5, cooldown: 1300),
                    new Spawn("Assassin of Oryx", maxChildren: 2, cooldown: 10000),
                    new HpLessTransition(0.3, "Wrath")
                ),
                new State("Wrath",
                    new Taunt(true, "I AM ORYX. I AM ETERNAL."),
                    new Prioritize(
                        new Follow(1.1, 16, 6),
                        new Wander(1)
                    ),
                    new Shoot(15, count: 4, shootAngle: 12, cooldown: 800),
                    new Shoot(13, count: 8, shootAngle: 45, index: 0, cooldown: 2200)
                ),
                new Threshold(0.05f,
                    new ItemLoot("Health Potion", 1),
                    new ItemLoot("Magic Potion", 0.5f),
                    new TierLoot(9, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(8, TierLoot.LootType.Ability, 0.15f),
                    new TierLoot(8, TierLoot.LootType.Ring, 0.1f)));
        }
    }
}
