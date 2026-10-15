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
    public class ForbiddenJungle : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Great Coil Snake",
                            
                                new DropPortalOnDeath("Forbidden Jungle Portal", 0.2f, timeout: 100),
                                new Prioritize(
                                    new StayCloseToSpawn(0.8f, 5),
                                    new Wander(0.4f)
                                    ),
                                new State("Waiting",
                                    new PlayerWithinTransition(15, "Attacking")
                                    ),
                                new State("Attacking",
                                    new Shoot(10, index: 0, cooldown: 700, cooldownOffset: 600),
                                    new Shoot(10, 10, 36, 1, cooldown: 2000),
                                    new TossObject("Great Snake Egg", 4, 0, 5000, cooldownOffset: 0),
                                    new TossObject("Great Snake Egg", 4, 90, 5000, cooldownOffset: 600),
                                    new TossObject("Great Snake Egg", 4, 180, 5000, cooldownOffset: 1200),
                                    new TossObject("Great Snake Egg", 4, 270, 5000, cooldownOffset: 1800),
                                    new NoPlayerWithinTransition(30, "Waiting")
                                    )
                                
                        );

            db.Init("Great Snake Egg",
                            
                                new TransformOnDeath("Great Temple Snake", 1, 2),
                                new State("Wait",
                                    new TimedTransition(2500, "Explode"),
                                    new PlayerWithinTransition(2, "Explode")
                                    ),
                                new State("Explode",
                                    new Suicide()
                                    )
                                
                        );

            db.Init("Great Temple Snake",
                            
                                new Prioritize(
                                    new Follow(0.6f),
                                    new Wander(0.4f)
                                    ),
                                new Shoot(10, 2, 7, 0, cooldown: 1000, cooldownOffset: 0),
                                new Shoot(10, 6, 60, 1, cooldown: 2000, cooldownOffset: 600)
                                
                        );
        }
    }
}
