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
    public class LotLL : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Lord of the Lost Lands",
                new State("Waiting",
                        new HpLessTransition(0.99, "Start1.0")
                        ),
                new State("Start1.0",
                        new HpLessTransition(0.1, "Dead"),
                        new State("Start",
                            new SetAltTexture(0),
                            new Prioritize(
                                new Wander(0.8f)
                                ),
                            new Shoot(0, count: 7, shootAngle: 10, fixedAngle: 180, cooldown: 2000),
                            new Shoot(0, count: 7, shootAngle: 10, fixedAngle: 0, cooldown: 2000),
                            new TossObject("Guardian of the Lost Lands", 5, cooldown: 1000),
                            new TimedTransition("Spawning Guardian", 100)
                            ),
                        new State("Spawning Guardian",
                            new TossObject("Guardian of the Lost Lands", 5, cooldown: 1000),
                            new TimedTransition("Attack", 3100)
                            ),
                        new State("Attack",
                            new SetAltTexture(0),
                            new Wander(0.8f),
                            new PlayerWithinTransition(1, "Follow"),
                            new TimedTransition("Gathering", 10000),
                            new State("Attack1.0",
                                new TimedRandomTransition(3000, false,
                                    "Attack1.1",
                                    "Attack1.2"),
                                new State("Attack1.1",
                                    new Shoot(12, count: 7, shootAngle: 10, cooldown: 2000),
                                    new Shoot(12, count: 7, shootAngle: 190, cooldown: 2000),
                                    new TimedTransition("Attack1.0", 2000)
                                    ),
                                new State("Attack1.2",
                                    new Shoot(0, count: 7, shootAngle: 10, fixedAngle: 180, cooldown: 3000),
                                    new Shoot(0, count: 7, shootAngle: 10, fixedAngle: 0, cooldown: 3000),
                                    new TimedTransition("Attack1.0", 2000)
                                    )
                                )
                            ),
                        new State("Follow",
                            new Prioritize(
                                new Follow(1, 20, 3),
                                new Wander(0.4f)
                                ),
                            new Shoot(20, count: 7, shootAngle: 10, cooldown: 1300),
                            new TimedTransition("Gathering", 5000)
                            ),
                        new State("Gathering",
                            new Taunt(0.99, "Gathering power!"),
                            new SetAltTexture(3),
                            new TimedTransition("Gathering1.0", 2000)
                            ),
                        new State("Gathering1.0",
                            new TimedTransition("Protection", 5000),
                            new State("Gathering1.1",
                                new Shoot(30, 4, fixedAngle: 90, index: 1, cooldown: 2000),
                                new TimedTransition("Gathering1.2", 1500)
                                ),
                            new State("Gathering1.2",
                                new Shoot(30, 4, fixedAngle: 45, index: 1, cooldown: 2000),
                                new TimedTransition("Gathering1.1", 1500)
                                )
                            ),
                        new State("Protection",
                            new SetAltTexture(0),
                            new TossObject("Protection Crystal", 4, angle: 0, cooldown: 5000),
                            new TossObject("Protection Crystal", 4, angle: 45, cooldown: 5000),
                            new TossObject("Protection Crystal", 4, angle: 90, cooldown: 5000),
                            new TossObject("Protection Crystal", 4, angle: 135, cooldown: 5000),
                            new TossObject("Protection Crystal", 4, angle: 180, cooldown: 5000),
                            new TossObject("Protection Crystal", 4, angle: 225, cooldown: 5000),
                            new TossObject("Protection Crystal", 4, angle: 270, cooldown: 5000),
                            new TossObject("Protection Crystal", 4, angle: 315, cooldown: 5000),
                            new EntityExistsTransition("Protection Crystal", 10, "Waiting")
                            )
                        ),
                new State("Waiting",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new SetAltTexture(1),
                        new EntityNotExistsTransition("Protection Crystal", 10, "Start1.0")
                        ),
                new State("Dead",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new SetAltTexture(3),
                        new Taunt(0.99, "NOOOO!!!!!!"),
                        new Flash(0xFF0000, .1, 1000),
                        new TimedTransition("Suicide", 2000)
                        ),
                new State("Suicide",
                        new ConditionalEffect(ConditionEffectIndex.StunImmune, true),
                        new Shoot(0, 8, fixedAngle: 360 / 8, index: 1),
                        new Suicide()
                        ),
                new Threshold(0.01f,
                    new TierLoot(8, TierLoot.LootType.Weapon, .15f),
                    new TierLoot(9, TierLoot.LootType.Weapon, .1f),
                    new TierLoot(10, TierLoot.LootType.Weapon, .07f),
                    new TierLoot(11, TierLoot.LootType.Weapon, .05f),
                    new TierLoot(4, TierLoot.LootType.Ability, .15f),
                    new TierLoot(5, TierLoot.LootType.Ability, .07f),
                    new TierLoot(8, TierLoot.LootType.Armor, .2f),
                    new TierLoot(9, TierLoot.LootType.Armor, .15f),
                    new TierLoot(10, TierLoot.LootType.Armor, .10f),
                    new TierLoot(11, TierLoot.LootType.Armor, .07f),
                    new TierLoot(12, TierLoot.LootType.Armor, .04f),
                    new TierLoot(3, TierLoot.LootType.Ring, .15f),
                    new TierLoot(4, TierLoot.LootType.Ring, .07f),
                    new TierLoot(5, TierLoot.LootType.Ring, .03f),
                    new ItemLoot("Potion of Defense", .1f),
                    new ItemLoot("Potion of Attack", .1f),
                    new ItemLoot("Potion of Vitality", .1f),
                    new ItemLoot("Potion of Wisdom", .1f),
                    new ItemLoot("Potion of Speed", .1f),
                    new ItemLoot("Potion of Dexterity", .1f),
                    new ItemLoot("Shield of Ogmur", .004f)
                    ));
            db.Init("Protection Crystal",
                new Prioritize(
                        new Orbit(0.3, 4, 10, "Lord of the Lost Lands")
                        ),
                new Shoot(8, count: 4, shootAngle: 7, cooldown: 500));
            db.Init("Guardian of the Lost Lands",
                new State("Full",
                        new Spawn("Knight of the Lost Lands", 2, 1, cooldown: 4000),
                        new Prioritize(
                            new Follow(0.6, 20, 6),
                            new Wander(0.2f)
                            ),
                        new Shoot(10, count: 8, fixedAngle: 360 / 8, cooldown: 3000, index: 1),
                        new Shoot(10, count: 5, shootAngle: 10, cooldown: 1500),
                        new HpLessTransition(0.25, "Low")
                        ),
                new State("Low",
                        new Prioritize(
                            new StayBack(0.6, 5),
                            new Wander(0.2f)
                            ),
                        new Shoot(10, count: 8, fixedAngle: 360 / 8, cooldown: 3000, index: 1),
                        new Shoot(10, count: 5, shootAngle: 10, cooldown: 1500)
                        ),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.1f));
            db.Init("Knight of the Lost Lands",
                new Prioritize(
                        new Follow(1, 20, 4),
                        new StayBack(0.5, 2),
                        new Wander(0.3f)
                        ),
                new Shoot(13, 1, cooldown: 700),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.1f));
        }
    }
}
