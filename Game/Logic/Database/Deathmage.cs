using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Conditionals;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Database
{
    public class Deathmage : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Skeleton",
                new Shoot(3),
                new State("Default",
                    new Prioritize(
                        new Follow(1, range: 1),
                        new Wander(0.4f)
                    )
                ),
                new State("Protect",
                    new Prioritize(
                        new Protect(1, "Deathmage"),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new State("Circling",
                    new Prioritize(
                        new Orbit(1, 10),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new State("Engaging",
                    new Prioritize(
                        new Follow(1, acquireRange: 15, range: 1),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new ItemLoot("Long Sword", 0.02f),
                new ItemLoot("Dirk", 0.02f)
            );
            db.Init("Skeleton Swordsman",
                new Shoot(3),
                new State("Default",
                    new Prioritize(
                        new Follow(1, range: 1),
                        new Wander(0.4f)
                    )
                ),
                new State("Protect",
                    new Prioritize(
                        new Protect(1, "Deathmage"),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new State("Circling",
                    new Prioritize(
                        new Orbit(1, 10),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new State("Engaging",
                    new Prioritize(
                        new Follow(1, acquireRange: 15, range: 1),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new ItemLoot("Long Sword", 0.03f),
                new ItemLoot("Steel Shield", 0.02f),
                new ItemLoot("Bronze Helm", 0.02f)
            );
            db.Init("Skeleton Veteran",
                new Shoot(3),
                new State("Default",
                    new Prioritize(
                        new Follow(1, range: 1),
                        new Wander(0.4f)
                    )
                ),
                new State("Protect",
                    new Prioritize(
                        new Protect(1, "Deathmage"),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new State("Circling",
                    new Prioritize(
                        new Orbit(1, 10),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new State("Engaging",
                    new Prioritize(
                        new Follow(1, acquireRange: 15, range: 1),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new ItemLoot("Long Sword", 0.03f),
                new ItemLoot("Golden Shield", 0.02f),
                new ItemLoot("Cloak of Darkness", 0.01f),
                new ItemLoot("Spider Venom", 0.01f)
            );
            db.Init("Skeleton Mage",
                new Shoot(10),
                new State("Default",
                    new Prioritize(
                        new Follow(1, range: 7),
                        new Wander(0.4f)
                    )
                ),
                new State("Protect",
                    new Prioritize(
                        new Protect(1, "Deathmage"),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new State("Circling",
                    new Prioritize(
                        new Orbit(1, 10),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new State("Engaging",
                    new Prioritize(
                        new Follow(1, acquireRange: 15, range: 1),
                        new Wander(0.4f)
                    ),
                    new EntityNotExistsTransition("Deathmage", 10, "Default")
                ),
                new ItemLoot("Missile Wand", 0.02f),
                new ItemLoot("Comet Staff", 0.02f),
                new ItemLoot("Comet Staff", 0.02f),
                new ItemLoot("Fire Nova Spell", 0.02f)
            );
            db.Init("Deathmage",
                new State("Waiting",
                    new Prioritize(
                        new StayCloseToSpawn(0.8f, 5),
                        new Wander(0.4f)
                    ),
                    new Order(10, "Skeleton", "Protect"),
                    new Order(10, "Skeleton Swordsman", "Protect"),
                    new Order(10, "Skeleton Veteran", "Protect"),
                    new Order(10, "Skeleton Mage", "Protect"),
                    new PlayerWithinTransition(15, "Attacking")
                ),
                new State("Attacking",
                    new Taunt(0.2f, 2000, "{PLAYER}, you will soon be my undead slave!",
                        "My skeletons will make short work of you.",
                        "You will never leave this graveyard alive!"
                    ),
                    new Prioritize(
                        new StayCloseToSpawn(0.8f, 5),
                        new Follow(0.8f, range: 8),
                        new Wander(0.4f)
                    ),
                    new Shoot(10, count: 3, shootAngle: 15, predictive: 1),
                    new State("Circling",
                        new Orbit(0.8f, 5),
                        new Order(10, "Skeleton", "Circling"),
                        new Order(10, "Skeleton Swordsman", "Circling"),
                        new Order(10, "Skeleton Veteran", "Circling"),
                        new Order(10, "Skeleton Mage", "Circling"),
                        new TimedTransition(2000, "Engaging")
                    ),
                    new State("Engaging",
                        new Order(10, "Skeleton", "Engaging"),
                        new Order(10, "Skeleton Swordsman", "Engaging"),
                        new Order(10, "Skeleton Veteran", "Engaging"),
                        new Order(10, "Skeleton Mage", "Engaging"),
                        new TimedTransition(2000, "Circling")
                    ),
                    new NoPlayerWithinTransition(30, "Waiting")
                ),
                new Spawn("Skeleton", maxChildren: 4, cooldown: 8000),
                new Spawn("Skeleton Swordsman", maxChildren: 2, cooldown: 8000),
                new Spawn("Skeleton Veteran", maxChildren: 1, cooldown: 8000),
                new Spawn("Skeleton Mage", maxChildren: 1, cooldown: 8000)
            );
        }
    }
}
