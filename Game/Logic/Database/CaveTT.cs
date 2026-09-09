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
    public class CaveTT : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Golden Oryx Effigy",
                new DropPortalOnDeath(target: "Realm Portal"),
                new State("Ini",
                        new HpLessTransition(threshold: 0.99, targetState: "Q1 Spawn Minion")
                        ),
                new State("Q1 Spawn Minion",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TossObject(child: "Gold Planet", range: 7, angle: 0, cooldown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 45, cooldown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 90, cooldown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 135, cooldown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 180, cooldown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 225, cooldown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 270, cooldown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 315, cooldown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 0, cooldown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 90, cooldown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 180, cooldown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 270, cooldown: 10000000),
                        new ChangeSize(rate: -1, target: 60),
                        new TimedTransition(time: 4000, targetState: "Q1 Invulnerable")
                        ),
                new State("Q1 Invulnerable",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        //order Expand
                        new EntitiesNotExistsTransition(99, "Q1 Vulnerable Transition", "Treasure Oryx Defender")
                        ),
                new State("Q1 Vulnerable Transition",
                        new State("T1",
                            new SetAltTexture(2),
                            new TimedTransition(time: 50, targetState: "T2")
                            ),
                        new State("T2",
                            new SetAltTexture(minValue: 0, maxValue: 1, cooldown: 100, loop: true)
                            ),
                        new TimedTransition(time: 800, targetState: "Q1 Vulnerable")
                        ),
                new State("Q1 Vulnerable",
                        new SetAltTexture(1),
                        new Taunt(0.75, "My protectors!", "My guardians are gone!", "What have you done?", "You destroy my guardians in my house? Blasphemy!"),
                        //order Shrink
                        new HpLessTransition(threshold: 0.75, targetState: "Q2 Invulnerable Transition")
                        ),
                new State("Q2 Invulnerable Transition",
                        new State("T1_2",
                            new SetAltTexture(2),
                            new TimedTransition(time: 50, targetState: "T2_2")
                            ),
                        new State("T2_2",
                            new SetAltTexture(minValue: 0, maxValue: 1, cooldown: 100, loop: true)
                            ),
                        new TimedTransition(time: 800, targetState: "Q2 Spawn Minion")
                        ),
                new State("Q2 Spawn Minion",
                        new SetAltTexture(0),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 0, cooldown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 90, cooldown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 180, cooldown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 270, cooldown: 10000000),
                        new ChangeSize(rate: -1, target: 60),
                        new TimedTransition(time: 4000, targetState: "Q2 Invulnerable")
                        ),
                new State("Q2 Invulnerable",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        //order expand
                        new EntitiesNotExistsTransition(99, "Q2 Vulnerable Transition", "Treasure Oryx Defender")
                        ),
                new State("Q2 Vulnerable Transition",
                        new State("T1_3",
                            new SetAltTexture(2),
                            new TimedTransition(time: 50, targetState: "T2_3")
                            ),
                        new State("T2_3",
                            new SetAltTexture(minValue: 0, maxValue: 1, cooldown: 100, loop: true)
                            ),
                        new TimedTransition(time: 800, targetState: "Q2 Vulnerable")
                        ),
                new State("Q2 Vulnerable",
                        new SetAltTexture(1),
                        new Taunt(0.75, "My protectors are no more!", "You Mongrels are ruining my beautiful treasure!", "You won't leave with your pilfered loot!", "I'm weakened"),
                        //Shrink
                        new HpLessTransition(threshold: 0.6, targetState: "Q3 Vulnerable Transition")
                        ),
                new State("Q3 Vulnerable Transition",
                        new State("T1_4",
                            new SetAltTexture(2),
                            new TimedTransition(time: 50, targetState: "T2_4")
                            ),
                        new State("T2_4",
                            new SetAltTexture(minValue: 0, maxValue: 1, cooldown: 100, loop: true)
                            ),
                        new TimedTransition(time: 800, targetState: "Q3")
                        ),
                new State("Q3",
                        new SetAltTexture(1),
                        new State("Attack1",
                            new State("CardinalBarrage",
                                new Grenade(radius: 0.5f, damage: 70, range: 0, fixedAngle: 0, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 0, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 90, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 180, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 270, cooldown: 1000),
                                new TimedTransition(time: 1500, targetState: "OrdinalBarrage")
                                ),
                            new State("OrdinalBarrage",
                                new Grenade(radius: 0.5f, damage: 70, range: 0, fixedAngle: 0, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 45, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 135, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 225, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 315, cooldown: 1000),
                                new TimedTransition(time: 1500, targetState: "CardinalBarrage2")
                                ),
                            new State("CardinalBarrage2",
                                new Grenade(radius: 0.5f, damage: 70, range: 0, fixedAngle: 0, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 0, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 90, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 180, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 270, cooldown: 1000),
                                new TimedTransition(time: 1500, targetState: "OrdinalBarrage2")
                                ),
                            new State("OrdinalBarrage2",
                                new Grenade(radius: 0.5f, damage: 70, range: 0, fixedAngle: 0, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 45, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 135, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 225, cooldown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 315, cooldown: 1000),
                                new TimedTransition(time: 1500, targetState: "CardinalBarrage")
                                ),
                            new TimedTransition(time: 8500, targetState: "Attack2")
                            ),
                        new State("Attack2",
                            new Flash(color: 0x0000FF, flashPeriod: 0.1, flashRepeats: 10),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 90, cooldown: 10000000, cooldownOffset: 0),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 90, cooldown: 10000000, cooldownOffset: 200),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 80, cooldown: 10000000, cooldownOffset: 400),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 70, cooldown: 10000000, cooldownOffset: 600),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 60, cooldown: 10000000, cooldownOffset: 800),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 50, cooldown: 10000000, cooldownOffset: 1000),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 40, cooldown: 10000000, cooldownOffset: 1200),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 30, cooldown: 10000000, cooldownOffset: 1400),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 20, cooldown: 10000000, cooldownOffset: 1600),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 10, cooldown: 10000000, cooldownOffset: 1800),
                            new Shoot(range: 0, count: 4, shootAngle: 45, index: 1, defaultAngle: 0, cooldown: 10000000, cooldownOffset: 2200),
                            new Shoot(range: 0, count: 4, shootAngle: 45, index: 1, defaultAngle: 0, cooldown: 10000000, cooldownOffset: 2400),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 0, cooldown: 10000000, cooldownOffset: 2600),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 10, cooldown: 10000000, cooldownOffset: 2800),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 20, cooldown: 10000000, cooldownOffset: 3000),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 30, cooldown: 10000000, cooldownOffset: 3200),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 40, cooldown: 10000000, cooldownOffset: 3400),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 50, cooldown: 10000000, cooldownOffset: 3600),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 60, cooldown: 10000000, cooldownOffset: 3800),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 70, cooldown: 10000000, cooldownOffset: 4000),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 80, cooldown: 10000000, cooldownOffset: 4200),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 90, cooldown: 10000000, cooldownOffset: 4400),
                            new Shoot(range: 0, count: 4, shootAngle: 45, index: 1, defaultAngle: 90, cooldown: 10000000, cooldownOffset: 4600),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 90, cooldown: 10000000, cooldownOffset: 4800),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 90, cooldown: 10000000, cooldownOffset: 5000),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 90, cooldown: 10000000, cooldownOffset: 5200),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 80, cooldown: 10000000, cooldownOffset: 5400),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 70, cooldown: 10000000, cooldownOffset: 5600),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 60, cooldown: 10000000, cooldownOffset: 5800),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 50, cooldown: 10000000, cooldownOffset: 6000),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 40, cooldown: 10000000, cooldownOffset: 6200),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 30, cooldown: 10000000, cooldownOffset: 6400),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 20, cooldown: 10000000, cooldownOffset: 6600),
                            new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 10, cooldown: 10000000, cooldownOffset: 6800),
                            new Shoot(range: 0, count: 4, shootAngle: 45, index: 1, defaultAngle: 0, cooldown: 10000000, cooldownOffset: 7000),
                            new TimedTransition(time: 7000, targetState: "Recuperate")
                            ),
                        new State("Recuperate",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new HealSelf(cooldown: 1000, amount: 200),
                            new TimedTransition(time: 3000, targetState: "Attack1")
                            )
                        ),
                new Threshold(0.01f,
                    new ItemLoot(item: "Potion of Defense", chance: 0.3f),
                    new ItemLoot(item: "Potion of Attack", chance: 0.3f),
                    new ItemLoot(item: "Potion of Speed", chance: 0.3f),
                    new ItemLoot(item: "Potion of Dexterity", chance: 0.3f),
                    new ItemLoot(item: "Potion of Vitality", chance: 0.3f),
                    new ItemLoot(item: "Potion of Wisdom", chance: 0.3f),
                    new ItemLoot(item: "Wine Cellar Incantation", chance: 0.01f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Weapon, chance: 0.5f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Armor, chance: 0.5f),
                    new TierLoot(tier: 9, type: TierLoot.LootType.Weapon, chance: 0.1f),
                    new TierLoot(tier: 9, type: TierLoot.LootType.Armor, chance: 0.1f),
                    new TierLoot(tier: 10, type: TierLoot.LootType.Weapon, chance: 0.05f),
                    new TierLoot(tier: 10, type: TierLoot.LootType.Armor, chance: 0.05f),
                    new TierLoot(tier: 5, type: TierLoot.LootType.Ability, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.5f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ring, chance: 0.5f),
                    new TierLoot(tier: 5, type: TierLoot.LootType.Ring, chance: 0.05f)
                ));
            db.Init("Treasure Oryx Defender",
                new Prioritize(
                        new Orbit(speed: 0.5, radius: 3, acquireRange: 6, target: "Golden Oryx Effigy", speedVariance: 0, radiusVariance: 0)
                        ),
                new Shoot(range: 0, count: 8, shootAngle: 45, defaultAngle: 0, cooldown: 3000));
            db.Init("Gold Planet",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new Prioritize(
                        new Orbit(speed: 0.5, radius: 7, acquireRange: 20, target: "Golden Oryx Effigy", speedVariance: 0, radiusVariance: 0)
                        ),
                new State("GreySpiral",
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 90, cooldown: 10000, cooldownOffset: 0),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 90, cooldown: 10000, cooldownOffset: 400),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 80, cooldown: 10000, cooldownOffset: 800),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 70, cooldown: 10000, cooldownOffset: 1200),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 0, defaultAngle: 60, cooldown: 10000, cooldownOffset: 1600),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 50, cooldown: 10000, cooldownOffset: 2000),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 40, cooldown: 10000, cooldownOffset: 2400),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 30, cooldown: 10000, cooldownOffset: 2800),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 20, cooldown: 10000, cooldownOffset: 3200),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 0, defaultAngle: 10, cooldown: 10000, cooldownOffset: 3600),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: 0, cooldown: 10000, cooldownOffset: 4000),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: -10, cooldown: 10000, cooldownOffset: 4400),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: -20, cooldown: 10000, cooldownOffset: 4800),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 1, defaultAngle: -30, cooldown: 10000, cooldownOffset: 5200),
                        new Shoot(range: 0, count: 2, shootAngle: 180, index: 0, defaultAngle: -40, cooldown: 10000, cooldownOffset: 5600),
                        new TimedTransition(time: 5600, targetState: "Reset"), new EntityNotExistsTransition(target: "Golden Oryx Effigy", dist: 999, targetState: "Die")),
                new State("Reset",
                        new TimedTransition(time: 0, targetState: "GreySpiral"), new EntityNotExistsTransition(target: "Golden Oryx Effigy", dist: 999, targetState: "Die")),
                new State("Die",
                        new Suicide(), new EntityNotExistsTransition(target: "Golden Oryx Effigy", dist: 999, targetState: "Die")));
        }
    }
}
