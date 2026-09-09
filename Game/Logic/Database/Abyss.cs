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
    public class Abyss : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Malphas Protector",
                new Shoot(range: 5, count: 3, shootAngle: 5, index: 0, predictive: 0.45f, cooldown: 1200),
                new Orbit(speed: 3.2f, radius: 9, acquireRange: 20, target: "Archdemon Malphas", speedVariance: 0, radiusVariance: 0, orbitClockwise: true),
                new Threshold(0.01f,
                    new ItemLoot(item: "Magic Potion", chance: 0.06f),
                    new ItemLoot(item: "Health Potion", chance: 0.04f)
                )
            );
            db.Init("Malphas Missile",
                new State("Start",
                    new TimedTransition(time: 50, targetState: "Attacking")
                ),
                new State("Attacking",
                    new Follow(speed: 1.1f, acquireRange: 10, range: 0.2f),
                    new PlayerWithinTransition(dist: 1.3f, targetState: "FlashBeforeExplode"),
                    new TimedTransition(time: 5000, targetState: "FlashBeforeExplode")
                ),
                new State("FlashBeforeExplode",
                    new Flash(color: 0xFFFFFF, flashPeriod: 0.1f, flashRepeats: 6),
                    new TimedTransition(time: 600, targetState: "Explode")
                ),
                new State("Explode",
                    new Shoot(range: 0, count: 8, shootAngle: 45, index: 0, fixedAngle: 0),
                    new Suicide()
                )
            );
            db.Init("Archdemon Malphas",
                new State("start_the_fun",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new PlayerWithinTransition(dist: 11, targetState: "he_is_never_alone", seeInvis: true)
                ),
                new State("he_is_never_alone",
                    new Reproduce(children: "Malphas Protector", densityRadius: 24, densityMax: 3, cooldown: 1000),
                    new State("Missile_Fire",
                        new Prioritize(
                            new StayCloseToSpawn(speed: 0.3f, range: 5),
                            new Follow(speed: 0.3f, acquireRange: 8, range: 2)
                        ),
                        new Shoot(range: 8, count: 1, index: 0, angleOffset: 1, predictive: 0.15f, cooldown: 900),
                        new Reproduce(children: "Malphas Missile", densityRadius: 24, densityMax: 4, cooldown: 1800),
                        new State("invulnerable1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(time: 2000, targetState: "vulnerable")
                        ),
                        new State("vulnerable",
                            new TimedTransition(time: 4000, targetState: "invulnerable2")
                        ),
                        new State("invulnerable2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable)
                        ),
                        new TimedTransition(time: 9000, targetState: "Pause1")
                    ),
                    new State("Pause1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Prioritize(
                            new StayCloseToSpawn(speed: 0.4f, range: 5),
                            new Wander(speed: 0.4f)
                        ),
                        new TimedTransition(time: 2500, targetState: "Small_target")
                    ),
                    new State("Small_target",
                        new Prioritize(
                            new StayCloseToSpawn(speed: 0.8f, range: 5),
                            new Wander(speed: 0.8f)
                        ),
                        new ChangeSize(rate: -11, target: 30),
                        new Shoot(range: 0, count: 6, shootAngle: 60, index: 1, fixedAngle: 0, cooldown: 1200),
                        new Shoot(range: 8, count: 1, angleOffset: 0.6f, predictive: 0.15f, cooldown: 900),
                        new TimedTransition(time: 12000, targetState: "Size_matters")
                    ),
                    new State("Size_matters",
                        new Prioritize(
                            new StayCloseToSpawn(speed: 0.2f, range: 5),
                            new Wander(speed: 0.2f)
                        ),
                        new State("Growbig",
                            new ChangeSize(rate: 11, target: 140),
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(time: 1800, targetState: "Shot_rotation1")
                        ),
                        new State("Shot_rotation1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(range: 8, count: 1, index: 2, predictive: 0.2f, cooldown: 900),
                            new Shoot(range: 0, count: 3, shootAngle: 120, index: 3, angleOffset: 0.7f, defaultAngle: 0, cooldown: 700),
                            new TimedTransition(time: 1400, targetState: "Shot_rotation2")
                        ),
                        new State("Shot_rotation2",
                            new Shoot(range: 8, count: 1, index: 2, predictive: 0.2f, cooldown: 900),
                            new Shoot(range: 8, count: 1, index: 2, predictive: 0.25f, cooldown: 2000),
                            new Shoot(range: 0, count: 3, shootAngle: 120, index: 3, angleOffset: 0.7f, defaultAngle: 40, cooldown: 700),
                            new TimedTransition(time: 1400, targetState: "Shot_rotation3")
                        ),
                        new State("Shot_rotation3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(range: 8, count: 1, index: 2, predictive: 0.2f, cooldown: 900),
                            new Shoot(range: 0, count: 3, shootAngle: 120, index: 3, angleOffset: 0.7f, defaultAngle: 80, cooldown: 700),
                            new TimedTransition(time: 1400, targetState: "Shot_rotation1")
                        ),
                        new TimedTransition(time: 13000, targetState: "Pause2")
                    ),
                    new State("Pause2",
                        new ChangeSize(rate: -11, target: 100),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Prioritize(
                            new StayCloseToSpawn(speed: 0.4f, range: 5),
                            new Wander(speed: 0.4f)
                        ),
                        new TimedTransition(time: 2500, targetState: "Bring_on_the_flamers")
                    ),
                    new State("Bring_on_the_flamers",
                        new ChangeSize(rate: 14, target: 100),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Prioritize(
                            new StayCloseToSpawn(speed: 0.4f, range: 5),
                            new Follow(speed: 0.4f, acquireRange: 9, range: 2)
                        ),
                        new Shoot(range: 8, count: 1, predictive: 0.25f, cooldown: 2100),
                        new Reproduce(children: "Malphas Flamer", densityRadius: 24, densityMax: 5, cooldown: 500),
                        new TossObject(child: "Malphas Flamer", range: 6, angle: 0, cooldown: 9000),
                        new TossObject(child: "Malphas Flamer", range: 6, angle: 90, cooldown: 9000),
                        new TossObject(child: "Malphas Flamer", range: 6, angle: 180, cooldown: 9000),
                        new TossObject(child: "Malphas Flamer", range: 6, angle: 270, cooldown: 9000),
                        new TimedTransition(time: 8000, targetState: "Temporary_exhaustion")
                    ),
                    new State("Temporary_exhaustion",
                        new Flash(color: 0x484848, flashPeriod: 0.6f, flashRepeats: 5),
                        new StayBack(speed: 0.4f, distance: 4),
                        new TimedTransition(time: 3200, targetState: "Missile_Fire")
                    )
                ),
                new DropPortalOnDeath(target: "Realm Portal", probability: 1),
                new Threshold(0.01f,
                    new TierLoot(tier: 7, type: TierLoot.LootType.Weapon, chance: 0.2f),
                    new TierLoot(tier: 9, type: TierLoot.LootType.Weapon, chance: 0.11f),
                    new TierLoot(tier: 10, type: TierLoot.LootType.Weapon, chance: 0.05f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Armor, chance: 0.2f),
                    new TierLoot(tier: 9, type: TierLoot.LootType.Armor, chance: 0.11f),
                    new TierLoot(tier: 10, type: TierLoot.LootType.Armor, chance: 0.05f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ability, chance: 0.1f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.06f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ring, chance: 0.1f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ring, chance: 0.06f),
                    new ItemLoot(item: "Wine Cellar Incantation", chance: 0.01f),
                    new ItemLoot(item: "Potion of Vitality", chance: 0.3f, min: 1),
                    new ItemLoot(item: "Potion of Defense", chance: 0.3f),
                    new ItemLoot(item: "Demon Blade", chance: 0.01f)
                )
            );
            db.Init("Malphas Flamer",
                new State("Attacking",
                    new State("Charge",
                        new Prioritize(
                            new Follow(speed: 0.7f, acquireRange: 10, range: 0.1f)
                        ),
                        new PlayerWithinTransition(dist: 2, targetState: "Bullet1", seeInvis: true)
                    ),
                    new State("Bullet1",
                        new ChangeSize(rate: 20, target: 130),
                        new Flash(color: 0xFFAA00, flashPeriod: 0.2f, flashRepeats: 20),
                        new Shoot(range: 8, cooldown: 200),
                        new TimedTransition(time: 4000, targetState: "Wait1")
                    ),
                    new State("Wait1",
                        new ChangeSize(rate: -20, target: 70),
                        new Charge(speed: 3, range: 20, cooldown: 600)
                    ),
                    new HpLessTransition(threshold: 0.2f, targetState: "FlashBeforeExplode")
                ),
                new State("FlashBeforeExplode",
                    new Flash(color: 0xFF0000, flashPeriod: 0.75f, flashRepeats: 1),
                    new TimedTransition(time: 300, targetState: "Explode")
                ),
                new State("Explode",
                    new Shoot(range: 0, count: 8, shootAngle: 45, defaultAngle: 0),
                    new Decay(time: 100)
                ),
                new Threshold(0.01f,
                    new ItemLoot("Health Potion", 0.1f),
                    new ItemLoot("Magic Potion", 0.1f)
                )
            );
        }
    }
}
