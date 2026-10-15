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
    public class OryxChicken : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Test Egg",
                            
                                new State("Idle",
                                    new HealthTransition(630000, "1")
                                ),
                                new State("1",
                                    new HealthTransition(610000, "2"),
                                    new SetAltTexture(1)
                                ),
                                new State("2",
                                    new HealthTransition(590000, "3"),
                                    new SetAltTexture(2)
                                ),
                                new State("3",
                                    new HealthTransition(570000, "4"),
                                    new SetAltTexture(3)
                                ),
                                new State("4",
                                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                                    new SetAltTexture(4),
                                    new TimedTransition(250, "Break")
                                ),
                                new State("Break",
                                    new Transform("TestChicken 2")
                                )
                            
                        );

            db.Init("TestChicken 2",
                            
                                new HealthTransition(1000, "Death"),
                                new Prioritize(
                                    new StayCloseToSpawn(0.6f, 5),
                                    new Wander(0.6f)
                                ),
                                new State("Idle",
                                    new ChangeSize(20, 100),
                                    new TimedTransition(600, "Start")
                                ),
                                new State("Start",
                                    new Taunt("CLUCK!"),
                                    new State("Shoot",
                                        new EntityNotExistsTransition("TestChicken Small", 10, "Spawn Minions"),
                                        new Shoot(10, 2)
                                    ),
                                    new State("Spawn Minions",
                                        new Spawn("TestChicken Small", 5, 1),
                                        new TimedTransition(0, "Shoot")
                                    )
                                ),
                                new State("Death",
                                    new Shoot(100, 10, index: 6),
                                    new Suicide()
                                )
                            
                        );

            db.Init("TestChicken Small",
                            
                                new Prioritize(
                                    new Protect(0.6f, "TestChicken 2"),
                                    new Wander(0.6f)
                                ),
                                new State("Default",
                                    new Shoot(10, 8, index: 2, cooldown: 1000)
                                ),
                                new State("Despawn",
                                    new Suicide()
                                )
                            
                        );
        }
    }
}
