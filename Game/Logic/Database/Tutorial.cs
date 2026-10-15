// Behavior registrations adapted from ossimc82/fabiano-swagger-of-doom
// (commit 6fd20aad4a7905b13f25389c68368a942a2b68cb, 2015-11-09),
// licensed under AGPL-3.0; see the upstream repository LICENSE.
// Target XML IDs and target primitive APIs were verified before registration.
using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Conditionals;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    public class Tutorial : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("West Tutorial Gun",
                            
                                new Shoot(32, fixedAngle: 180, cooldown: 3000, cooldownVariance: 1000)
                                
                        );

            db.Init("North Tutorial Gun",
                            
                                new Shoot(32, fixedAngle: 270, cooldown: 3000, cooldownVariance: 1000)
                                
                        );

            db.Init("East Tutorial Gun",
                            
                                new Shoot(32, fixedAngle: 0, cooldown: 3000, cooldownVariance: 1000)
                                
                        );

            db.Init("South Tutorial Gun",
                            
                                new Shoot(32, fixedAngle: 90, cooldown: 3000, cooldownVariance: 1000)
                                
                        );

            db.Init("Evil Chicken",
                            
                                new Wander(0.3f)
                                
                        );

            db.Init("Evil Chicken Minion",
                            
                                new Wander(0.3f),
                                new Protect(0.3f, "Evil Chicken God")
                                
                        );

            db.Init("Evil Chicken God",
                            
                                new Prioritize(
                                    new Follow(0.4f, range: 5),
                                    new Wander(0.3f)
                                    ),
                                new Reproduce("Evil Chicken Minion", densityMax: 12)
                                
                        );

            db.Init("Evil Hen",
                            
                                new Wander(0.3f)
                                ,
                            new ItemLoot("Minor Health Potion", 1)
                        );

            db.Init("Kitchen Guard",
                            
                                new Prioritize(
                                    new Follow(0.6f, range: 6),
                                    new Wander(0.4f)
                                    ),
                                new Shoot(7)
                                
                        );

            db.Init("Butcher",
                            
                                new Prioritize(
                                    new Follow(0.8f, range: 1),
                                    new Wander(0.4f)
                                    ),
                                new Shoot(3)
                                ,
                            new ItemLoot("Minor Health Potion", 0.1f),
                            new ItemLoot("Minor Magic Potion", 0.1f)
                        );

            db.Init("Bonegrind the Butcher",
                            
                                new State("Begin",
                                    new Wander(0.6f),
                                    new PlayerWithinTransition(10, "AttackX")
                                    ),
                                new State("AttackX",
                                    new Taunt("Ah, fresh meat for the minions!"),
                                    new Shoot(6, cooldown: 1400),
                                    new Prioritize(
                                        new Follow(0.6f, 9, 3),
                                        new Wander(0.6f)
                                        ),
                                    new TimedTransition(4500, "AttackY"),
                                    new HealthTransition(0.3f, "Flee")
                                    ),
                                new State("AttackY",
                                    new Prioritize(
                                        new Follow(0.6f, 9, 3),
                                        new Wander(0.6f)
                                        ),
                                    new Sequence(
                                        new Shoot(7, 4, fixedAngle: 25),
                                        new Shoot(7, 4, fixedAngle: 50),
                                        new Shoot(7, 4, fixedAngle: 75),
                                        new Shoot(7, 4, fixedAngle: 100),
                                        new Shoot(7, 4, fixedAngle: 125)
                                        ),
                                    new TimedTransition(5200, "AttackX"),
                                    new HealthTransition(0.3f, "Flee")
                                    ),
                                new State("Flee",
                                    new Taunt("The meat ain't supposed to bite back! Waaaaa!!"),
                                    new Flash(0xff000000, 10, 100),
                                    new Prioritize(
                                        new StayBack(0.5f, 6),
                                        new Wander(0.5f)
                                        )
                                    )
                                ,
                            new ItemLoot("Minor Health Potion", 1),
                            new ItemLoot("Minor Magic Potion", 1)
                        );
        }
    }
}
