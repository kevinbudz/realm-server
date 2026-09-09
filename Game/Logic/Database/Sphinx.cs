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
    public class Sphinx : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Grand Sphinx",
                new DropPortalOnDeath("Tomb of the Ancients Portal", 0.33),
                new State("Spawned",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Reproduce("Horrid Reaper", 30, 4, cooldown: 100),
                        new TimedTransition("Attack1", 500)
                        ),
                new State("Attack1",
                        new Prioritize(
                            new Wander(0.5f)
                            ),
                        new Shoot(12, count: 1, cooldown: 800),
                        new Shoot(12, count: 3, shootAngle: 10, cooldown: 1000),
                        new Shoot(12, count: 1, shootAngle: 130, cooldown: 1000),
                        new Shoot(12, count: 1, shootAngle: 230, cooldown: 1000),
                        new TimedTransition("TransAttack2", 6000)
                        ),
                new State("TransAttack2",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Wander(0.5f),
                        new Flash(0x00FF0C, .25, 8),
                        new Taunt(0.99, "You hide behind rocks like cowards but you cannot hide from this!"),
                        new TimedTransition("Attack2", 2000)
                        ),
                new State("Attack2",
                        new Prioritize(
                            new Wander(0.5f)
                            ),
                        new Shoot(0, count: 8, shootAngle: 10, fixedAngle: 0, rotateAngke: 70, cooldown: 2000,
                            index: 1),
                        new Shoot(0, count: 8, shootAngle: 10, fixedAngle: 180, rotateAngke: 70, cooldown: 2000,
                            index: 1),
                        new TimedTransition("TransAttack3", 6200)
                        ),
                new State("TransAttack3",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Wander(0.5f),
                        new Flash(0x00FF0C, .25, 8),
                        new TimedTransition("Attack3", 2000)
                        ),
                new State("Attack3",
                        new Prioritize(
                            new Wander(0.5f)
                            ),
                        new Shoot(20, count: 9, fixedAngle: 360 / 9, index: 2, cooldown: 2300),
                        new TimedTransition("TransAttack1", 6000),
                        new State("Shoot1",
                            new Shoot(20, count: 2, shootAngle: 4, index: 2, cooldown: 700),
                            new TimedRandomTransition(1000, false,
                                "Shoot1",
                                "Shoot2"
                                )
                            ),
                        new State("Shoot2",
                            new Shoot(20, count: 8, shootAngle: 5, index: 2, cooldown: 1100),
                            new TimedRandomTransition(1000, false,
                                "Shoot1",
                                "Shoot2"
                                )
                            )
                        ),
                new State("TransAttack1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Wander(0.5f),
                        new Flash(0x00FF0C, .25, 8),
                        new TimedTransition("Attack1", 2000),
                        new HpLessTransition(0.15, "Order")
                        ),
                new State("Order",
                        new Wander(0.5f),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Order(30, "Horrid Reaper", "Die"),
                        new TimedTransition("Attack1", 1900)
                        ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Vitality", 0.1f, 1),
                    new ItemLoot("Potion of Wisdom", 0.1f, 1),
                    new ItemLoot("Helm of the Juggernaut", 0.004f)
                    ));
            db.Init("Horrid Reaper",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("Move",
                        new Prioritize(
                            new StayCloseToSpawn(3, 10),
                            new Wander(3)
                            ),
                        new EntityNotExistsTransition("Grand Sphinx", 50, "Die"), //Just to be sure
                        new TimedRandomTransition(2000, true, "Attack")
                        ),
                new State("Attack",
                        new Shoot(0, count: 6, fixedAngle: 360 / 6, cooldown: 700),
                        new PlayerWithinTransition(2, "Follow"),
                        new TimedRandomTransition(5000, true, "Move")
                        ),
                new State("Follow",
                        new Prioritize(
                            new Follow(0.7, 10, 3)
                            ),
                        new Shoot(7, count: 1, cooldown: 700),
                        new TimedRandomTransition(5000, true, "Move")
                        ),
                new State("Die",
                        new Taunt(0.99, "OOaoaoAaAoaAAOOAoaaoooaa!!!"),
                        new Decay(1000)
                        ));
        }
    }
}
