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
    public class CandylandMobs : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Wishing Troll",
                new State("BaseAttack",
                    new Shoot(10, 3, 15, 0, predictive: 1, cooldown: 1400),
                    new Grenade(radius: 5, damage: 200, range: 8, cooldown: 3000),
                    new Shoot(10, 1, index: 1, predictive: 1, cooldown: 2000),
                    new State("Choose",
                        new TimedRandomTransition(3800, false, "Run", "Attack")
                    ),
                    new State("Run",
                        new StayBack(1.1f, 10),
                        new TimedTransition(1200, "Choose")
                    ),
                    new State("Attack",
                        new Charge(1.2f, 11, 1000),
                        new TimedTransition(1000, "Choose")
                    ),
                    new HpLessTransition(0.6f, "NextAttack")
                ),
                new State("NextAttack",
                    new Shoot(10, 5, 10, 1, predictive: 0.5f, angleOffset: 0.4f, cooldown: 2000),
                    new Shoot(10, 1, index: 1, predictive: 1, cooldown: 2000),
                    new Shoot(10, 3, 15, 0, predictive: 1, angleOffset: 1, cooldown: 4000),
                    new Grenade(radius: 5, damage: 200, range: 8, cooldown: 3000),
                    new State("Choose2",
                        new TimedRandomTransition(3800, false, "Run2", "Attack2")
                    ),
                    new State("Run2",
                        new StayBack(1.5f, 10),
                        new TimedTransition(1500, "Choose2"),
                        new PlayerWithinTransition(3.5, "Boom")
                    ),
                    new State("Attack2",
                        new Charge(1.2f, 11, 1000),
                        new TimedTransition(1000, "Choose2"),
                        new PlayerWithinTransition(3.5, "Boom")
                    ),
                    new State("Boom",
                        new Shoot(0, 20, 18, 1, cooldown: 2000),
                        new TimedTransition(200, "Choose2")
                    )
                ),
                new StayCloseToSpawn(1.5f, 15),
                new Prioritize(
                    new Follow(1, 11, 5)
                ),
                new Wander(0.4f)
            );
            db.Init("Unicorn",
                new Prioritize(
                    new Charge(1.4f, 11, 3800),
                    new StayBack(0.8f, 6)
                ),
                new State("Start",
                    new State("Shoot",
                        new Shoot(10, 1, index: 0, predictive: 1, cooldown: 200),
                        new TimedTransition(850, "ShootPause")
                    ),
                    new State("ShootPause",
                        new Shoot(4.5f, 3, 10, 1, predictive: 0.4f, cooldownOffset: 500, cooldown: 3000),
                        new Shoot(4.5f, 3, 10, 1, predictive: 0.4f, cooldownOffset: 1000, cooldown: 3000),
                        new Shoot(4.5f, 3, 10, 1, predictive: 0.4f, cooldownOffset: 1500, cooldown: 3000),
                        new TimedTransition(1200, "Shoot")
                    )
                )
            );
            db.Init("Spilled IceCream",
                new Prioritize(
                    new Charge(1.4f, 11, 3800),
                    new StayBack(0.8f, 6)
                ),
                new State("Start",
                    new State("Shoot",
                        new Shoot(10, 1, index: 0, predictive: 1, cooldown: 200),
                        new TimedTransition(850, "ShootPause")
                    ),
                    new State("ShootPause",
                        new Shoot(4.5f, 3, 10, 0, predictive: 0.4f, cooldownOffset: 500, cooldown: 3000),
                        new Shoot(4.5f, 3, 10, 0, predictive: 0.4f, cooldownOffset: 1000, cooldown: 3000),
                        new Shoot(4.5f, 3, 10, 0, predictive: 0.4f, cooldownOffset: 1500, cooldown: 3000),
                        new TimedTransition(1200, "Shoot")
                    )
                )
            );
            db.Init("Beefy Fairy",
                new StayCloseToSpawn(1, 13),
                new Prioritize(
                    new Protect(1.2f, "Beefy Fairy", 15, 8, 6),
                    new Orbit(1.2f, 4, 7)
                ),
                new Wander(0.6f),
                new Shoot(10, 2, 30, 0, predictive: 1, cooldown: 2000),
                new Shoot(10, 1, index: 0, predictive: 1, cooldownOffset: 1000, cooldown: 2000)
            );
            db.Init("Hard Candy",
                new Shoot(5, 3, 12, 0, predictive: 0.6f, cooldown: 1000),
                new StayCloseToSpawn(1.3f, 13),
                new Prioritize(
                    new Charge(1.3f, 13, 2500),
                    new Protect(0.8f, "Big Creampuff", 15, 7, 6)
                ),
                new Wander(0.6f)
            );
        }
    }
}
