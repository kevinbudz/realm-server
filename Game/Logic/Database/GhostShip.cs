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
    public class GhostShip : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Ghost Ship",
                new State("Waiting Player",
                        new SetAltTexture(1),
                        new Prioritize(
                            new Orbit(0.2, 2, 10, "Ghost Ship Anchor")
                            ),
                        new HpLessTransition(0.98, "Start")
                        ),
                new State("Start",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new ReturnToSpawn(0.5, 0),
                        new SetAltTexture(0),
                        new TimedTransition("PreAttack", 3000)
                        ),
                new State("PreAttack",
                        new ReturnToSpawn(0.5, 0),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new SetAltTexture(2),
                        new OrderOnce(100, "Beach Spectre Spawner", "Active"),
                        new TimedTransition("Phase1+2", 300)
                        ),
                new State("Phase1+2",
                        new SetAltTexture(0),
                        new Reproduce("Vengeful Spirit", 20, 4, cooldown: 4000),
                        new TossObject("Water Mine", angle: 1, cooldown: 5000, minRange: 3, maxRange: 7,
                            minAngle: 1, maxAngle: 359),
                        new State("Attack",
                            new Prioritize(
                                new Follow(0.2, 10, 3),
                                new Wander(0.3f),
                                new StayCloseToSpawn(0.4, 7)
                                ),
                            new Taunt(0.30, "Fire at will!"),
                            new Shoot(10, count: 3, shootAngle: 6, cooldown: 2000),
                            new Shoot(10, count: 1, cooldown: 800),
                            new HpLessTransition(0.90, "TransAttack")
                            ),
                        new State("TransAttack",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new State("TransAttack1",
                                new Taunt(0.99, "Ready..."),
                                new ReturnToSpawn(0.3),
                                new TimedTransition("TransAttack1.1", 1000)
                                ),
                            new State("TransAttack1.1",
                                new Wander(0.3f),
                                new Taunt(0.99, "Aim..."),
                                new TimedTransition("Phase2", 1000)
                                ),
                            new State("Phase2",
                                new HpLessTransition(0.7, "Phase3"),
                                new Prioritize(
                                    new Follow(0.2, 10, 3),
                                    new Wander(0.3f),
                                    new StayCloseToSpawn(0.4, 7)
                                    ),
                                new State("Attack1.1",
                                    new Taunt(0.99, "FIRE!"),
                                    new Shoot(0, count: 3, shootAngle: 20, fixedAngle: 270, cooldown: 3000),
                                    new Shoot(0, count: 3, shootAngle: 20, fixedAngle: 90, cooldown: 3000),
                                    new TimedTransition("Attack1.2", 1000)
                                    ),
                                new State("Attack1.2",
                                    new Shoot(0, count: 3, shootAngle: 20, fixedAngle: 0, cooldown: 3000),
                                    new Shoot(0, count: 3, shootAngle: 20, fixedAngle: 180, cooldown: 3000),
                                    new TimedTransition("Attack1.3", 1000)
                                    ),
                                new State("Attack1.3",
                                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                    new Shoot(0, count: 3, shootAngle: 20, fixedAngle: 270, cooldown: 3000),
                                    new Shoot(0, count: 3, shootAngle: 20, fixedAngle: 90, cooldown: 3000),
                                    new TimedTransition("Attack1.4", 1000)
                                    ),
                                new State("Attack1.4",
                                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                    new Shoot(0, count: 12, fixedAngle: 360 / 12, cooldown: 3000),
                                    new TimedTransition("Attack1.5", 1500)
                                    ),
                                new State("Attack1.5",
                                    new Shoot(0, count: 1, fixedAngle: 0, index: 1, cooldown: 3000),
                                    new Shoot(0, count: 1, fixedAngle: 90, index: 1, cooldown: 3000),
                                    new Shoot(0, count: 1, fixedAngle: 180, index: 1, cooldown: 3000),
                                    new Shoot(0, count: 1, fixedAngle: 270, index: 1, cooldown: 3000),
                                    new TimedTransition("Attack1.6", 1000)
                                    ),
                                new State("Attack1.6",
                                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                    new Shoot(0, count: 1, fixedAngle: 45, index: 1, cooldown: 3000),
                                    new Shoot(0, count: 1, fixedAngle: 135, index: 1, cooldown: 3000),
                                    new Shoot(0, count: 1, fixedAngle: 225, index: 1, cooldown: 3000),
                                    new Shoot(0, count: 1, fixedAngle: 315, index: 1, cooldown: 3000),
                                    new TimedTransition("Attack1.7", 1000)
                                    ),
                                new State("Attack1.7",
                                    new Shoot(0, count: 1, fixedAngle: 0, index: 1, cooldown: 5000),
                                    new Shoot(0, count: 1, fixedAngle: 90, index: 1, cooldown: 5000),
                                    new Shoot(0, count: 1, fixedAngle: 180, index: 1, cooldown: 5000),
                                    new Shoot(0, count: 1, fixedAngle: 270, index: 1, cooldown: 5000),
                                    new TimedTransition("TransAttack", 3000)
                                    )))
                        ),
                new State("Phase3",
                        new Shoot(0, 4, fixedAngle: 360 / 4, rotateAngke: 35, cooldown: 1000),
                        new Shoot(10, count: 1, index: 1, cooldown: 1400, predictive: 1),
                        new Reproduce("Vengeful Spirit", 20, 4, cooldown: 4000),
                        new Reproduce("Water Mine", 20, 5, cooldown: 3000),
                        new HpLessTransition(0.5, "Phase4"),
                        new ReturnToSpawn(0.4),
                        new State("Invul",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("Vul", 2000)
                            ),
                        new State("Vul",
                            new TimedTransition("Invul", 3000)
                            )
                        ),
                new State("Phase4",
                        new Prioritize(
                            new Follow(0.2, 10, 3),
                            new Wander(0.4f),
                            new StayCloseToSpawn(0.4, 7)
                            ),
                        new TossObject("Water Mine", angle: 1, cooldown: 5000, minRange: 3, maxRange: 7,
                            minAngle: 1, maxAngle: 359),
                        new Shoot(20, count: 2, shootAngle: 8, cooldown: 800),
                        new Shoot(20, count: 3, shootAngle: 8, cooldown: 1300),
                        new Shoot(20, count: 2, shootAngle: 8, cooldown: 2000, index: 1),
                        new HpLessTransition(0.4, "PrePhase5"),
                        new State("Invul1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("Vul1", 2000)
                            ),
                        new State("Vul1",
                            new TimedTransition("Invul1", 3000)
                            )
                        ),
                new State("PrePhase5",
                        new TossObject("Tempest Cloud", 8, 0, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 36, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 72, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 108, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 144, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 180, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 216, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 252, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 288, cooldown: 10000),
                        new TossObject("Tempest Cloud", 8, 324, cooldown: 10000),
                        new EntityExistsTransition("Tempest Cloud", 10, "Phase5")
                        ),
                new State("Phase5",
                        new OrderOnce(100, "Beach Spectre Spawner", "Active"),
                        new Shoot(0, 4, fixedAngle: 360 / 4, rotateAngke: 35, cooldown: 1000),
                        new Shoot(10, count: 1, index: 1, cooldown: 1400, predictive: 1),
                        new Reproduce("Vengeful Spirit", 20, 4, cooldown: 4000),
                        new Reproduce("Water Mine", 20, 5, cooldown: 3000),
                        new HpLessTransition(0.2, "PrePhase6"),
                        new ReturnToSpawn(0.4),
                        new State("Invul2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("Vul2", 2000)
                            ),
                        new State("Vul2",
                            new TimedTransition("Invul2", 3000)
                            )
                        ),
                new State("PrePhase6",
                        new Taunt(0.99, "Fire at will!!!"),
                        new TimedTransition("Phase6", 100)
                        ),
                new State("Phase6",
                        new Prioritize(
                            new Follow(0.2, 10, 3),
                            new Wander(0.4f),
                            new StayCloseToSpawn(0.4, 7)
                            ),
                        new TossObject("Water Mine", angle: 1, cooldown: 5000, minRange: 3, maxRange: 7,
                            minAngle: 1, maxAngle: 359),
                        new Shoot(20, count: 2, shootAngle: 8, cooldown: 800),
                        new Shoot(20, count: 3, shootAngle: 8, cooldown: 1300),
                        new Shoot(20, count: 2, shootAngle: 8, cooldown: 2000, index: 1),
                        new State("Invul3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("Vul3", 2000)
                            ),
                        new State("Vul3",
                            new TimedTransition("Invul3", 3000)
                            )
                        ),
                new ItemLoot("Ghost Pirate Rum", .2f),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Wisdom", .1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.12f),
                    new TierLoot(9, TierLoot.LootType.Weapon, 0.10f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.12f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.10f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.12f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.10f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.07f),
                    new TierLoot(10, TierLoot.LootType.Weapon, 0.06f),
                    new TierLoot(11, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(10, TierLoot.LootType.Armor, 0.06f),
                    new TierLoot(11, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(5, TierLoot.LootType.Ability, 0.04f),
                    new TierLoot(5, TierLoot.LootType.Ring, 0.04f),
                    new ItemLoot("Wine Cellar Incantation", .001f)
                    ));
            db.Init("Ghost Ship Anchor",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Waiting",
                        new EntityNotExistsTransition("Ghost Ship", 20, "Davy Or No ?")
                        ),
                new State("Davy Or No ?",
                        new TimedRandomTransition(10, false, // 1/2 to get davy
                            "Davy",
                            "nah")
                        ),
                new State("nah",
                        new Suicide()
                        ),
                new State("nah2",
                        new DropPortalOnDeath("Davy Jones's Locker Portal", 1),
                        new Suicide()
                        ),
                new State("Davy",
                        new GroundTransform("Ghost Water Beach", relativeX: 1, relativeY: 0, persist: true),
                        new GroundTransform("Ghost Water Beach", relativeX: 0, relativeY: 0, persist: true),
                        new GroundTransform("Ghost Water Beach", relativeX: -1, relativeY: 0, persist: true),
                        new GroundTransform("Ghost Water Beach", relativeX: 0, relativeY: 1, persist: true),
                        new GroundTransform("Ghost Water Beach", relativeX: 0, relativeY: -1, persist: true),
                        new TossObject("Palm Tree", 1, angle: 0, cooldown: 10000),
                        new TossObject("Palm Tree", 1, angle: 180, cooldown: 10000),
                        new TimedTransition("nah2", 1000)
                        ));
            db.Init("Vengeful Spirit",
                new ChangeSize(20, 100),
                new State("Charge",
                        new Prioritize(
                            new Charge(3),
                            new Wander(0.1f)
                            ),
                        new Shoot(5, count: 3, shootAngle: 8, cooldown: 1000),
                        new EntityNotExistsTransition("Ghost Ship", 20, "Die")
                        ),
                new State("Die",
                        new Suicide()
                        ));
            db.Init("Tempest Cloud",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Texture",
                        new ChangeSize(10, 140),
                        new State("Texture1",
                            new SetAltTexture(1, 9, cooldown: 200),
                            new TimedTransition("Attack", 2000)
                            )
                        ),
                new State("Attack",
                        new Prioritize(
                            new Orbit(0.4, 8, 20, "Ghost Ship")
                            ),
                        new Shoot(0, count: 10, fixedAngle: 360 / 10, cooldown: 700),
                        new EntityNotExistsTransition("Ghost Ship", 30, "Die")
                        ),
                new State("Die",
                        new State("Tex",
                            new SetAltTexture(9),
                            new TimedTransition("Tex1", 200)
                            ),
                        new State("Tex1",
                            new SetAltTexture(8),
                            new TimedTransition("Tex2", 200)
                            ),
                        new State("Tex2",
                            new SetAltTexture(7),
                            new TimedTransition("Tex3", 200)
                            ),
                        new State("Tex3",
                            new SetAltTexture(6),
                            new TimedTransition("Tex4", 200)
                            ),
                        new State("Tex4",
                            new SetAltTexture(5),
                            new TimedTransition("Tex5", 200)
                            ),
                        new State("Tex5",
                            new SetAltTexture(4),
                            new TimedTransition("Tex6", 200)
                            ),
                        new State("Tex6",
                            new SetAltTexture(3),
                            new TimedTransition("Tex7", 200)
                            ),
                        new State("Tex7",
                            new SetAltTexture(2),
                            new TimedTransition("Tex8", 200)
                            ),
                        new State("Tex8",
                            new SetAltTexture(1),
                            new TimedTransition("Tex9", 200)
                            ),
                        new State("Tex9",
                            new SetAltTexture(0),
                            new TimedTransition("Tex10", 200)
                            ),
                        new State("Tex10",
                            new Suicide()
                            )
                        ));
            db.Init("Water Mine",
                new Decay(10000),
                new State("GRAB IT",
                        new EntityNotExistsTransition("Ghost Ship", 20, "BOOOM"),
                        new Prioritize(
                            new Follow(0.2, 10, 0)
                            ),
                        new PlayerWithinTransition(3, "BOOOM")
                        ),
                new State("BOOOM",
                        new Shoot(0, count: 10, fixedAngle: 360 / 10),
                        new Suicide()
                        ));
            db.Init("Beach Spectre Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Waiting Order", new EntityNotExistsTransition("Ghost Ship", 60, "Die")),
                new State("Active",
                        new PlayerWithinTransition(7, "Spawn"), new EntityNotExistsTransition("Ghost Ship", 60, "Die")),
                new State("Spawn",
                        new Reproduce("Beach Spectre", 3, 1, cooldown: 1000),
                        new NoPlayerWithinTransition(8, "Active"), new EntityNotExistsTransition("Ghost Ship", 60, "Die")),
                new State("Die",
                        new Suicide(), new EntityNotExistsTransition("Ghost Ship", 60, "Die")));
            db.Init("Beach Spectre",
                new ChangeSize(20, 100),
                new State("Attack",
                        new Prioritize(
                            new Wander(0.1f)
                            ),
                        new Shoot(5, count: 3, shootAngle: 12, cooldown: 1300),
                        new NoPlayerWithinTransition(7, "Die")
                        ),
                new State("Die",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new ChangeSize(20, 0),
                        new Decay(3000)
                        ));
        }
    }
}
