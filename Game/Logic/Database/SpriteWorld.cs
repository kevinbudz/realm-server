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
    public class SpriteWorld : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Native Fire Sprite",
                new StayAbove(speed: 0.4, altitude: 95),
                new Shoot(range: 10, count: 2, shootAngle: 7, index: 0, cooldown: 300),
                new Wander(speed: 1.4f),
                new Threshold(0.01f,
                    new TierLoot(tier: 5, type: TierLoot.LootType.Weapon, chance: 0.02f),
                    new ItemLoot(item: "Magic Potion", chance: 0.05f)
                    ));
            db.Init("Native Ice Sprite",
                new StayAbove(speed: 0.4, altitude: 105),
                new Shoot(range: 10, count: 3, shootAngle: 7, index: 0, cooldown: 1000),
                new Wander(speed: 1.4f),
                new Threshold(0.01f,
                    new TierLoot(tier: 2, type: TierLoot.LootType.Ability, chance: 0.04f),
                    new ItemLoot(item: "Magic Potion", chance: 0.05f)
                    ));
            db.Init("Native Magic Sprite",
                new StayAbove(speed: 0.4, altitude: 115),
                new Shoot(range: 10, count: 4, shootAngle: 7, index: 0, cooldown: 1000),
                new Wander(speed: 1.4f),
                new Threshold(0.01f,
                    new TierLoot(tier: 6, type: TierLoot.LootType.Armor, chance: 0.01f),
                    new ItemLoot(item: "Magic Potion", chance: 0.05f)
                    ));
            db.Init("Native Nature Sprite",
                new Shoot(range: 10, count: 5, shootAngle: 7, index: 0, cooldown: 1000),
                new Wander(speed: 1.6f),
                new Threshold(0.01f,
                    new ItemLoot(item: "Magic Potion", chance: 0.015f),
                    new ItemLoot(item: "Sprite Wand", chance: 0.015f),
                    new ItemLoot(item: "Ring of Greater Magic", chance: 0.01f)
                    ));
            db.Init("Native Darkness Sprite",
                new Shoot(range: 10, count: 5, shootAngle: 7, index: 0, cooldown: 1000),
                new Wander(speed: 1.6f),
                new Threshold(0.01f,
                    new ItemLoot(item: "Health Potion", chance: 0.015f),
                    new ItemLoot(item: "Ring of Dexterity", chance: 0.01f)
                    ));
            db.Init("Native Sprite God",
                new StayAbove(speed: 0.4, altitude: 200),
                new Shoot(range: 12, count: 4, shootAngle: 10, index: 0, cooldown: 1000),
                new Shoot(range: 12, index: 1, predictive: 1, cooldown: 1000),
                new Wander(speed: 0.4f),
                new Threshold(0.01f,
                    new TierLoot(tier: 6, type: TierLoot.LootType.Weapon, chance: 0.02f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Weapon, chance: 0.01f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Weapon, chance: 0.005f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Armor, chance: 0.02f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Armor, chance: 0.01f),
                    new TierLoot(tier: 9, type: TierLoot.LootType.Armor, chance: 0.005f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ring, chance: 0.01f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.01f),
                    new ItemLoot(item: "Potion of Attack", chance: 0.02f)
                    ));
            db.Init("Limon the Sprite God",
                new DropPortalOnDeath(target: "Glowing Realm Portal", probability: 1),
                new State("start_the_fun",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new PlayerWithinTransition(dist: 11, targetState: "begin_teleport1", seeInvis: true)
                        ),
                new State("begin_teleport1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Prioritize(
                            new StayCloseToSpawn(speed: 0.5, range: 7),
                            new Wander(speed: 0.5f)
                            ),
                        new Flash(color: 0x00FF00, flashPeriod: 0.25, flashRepeats: 8),
                        new TimedTransition(time: 2000, targetState: "teleport1")
                        ),
                new State("teleport1",
                        new Prioritize(
                            new StayCloseToSpawn(speed: 1.6, range: 7),
                            new Follow(speed: 6, acquireRange: 10, range: 2),
                            new Follow(speed: 0.3, acquireRange: 10, range: 0.2)
                        ),
                        new TimedTransition(time: 300, targetState: "circle_player")
                        ),
                new State("circle_player",
                        new Shoot(range: 8, count: 2, shootAngle: 10, index: 0, angleOffset: 0.7f, predictive: 0.4f, cooldown: 400),
                        new Shoot(range: 8, count: 2, shootAngle: 180, index: 0, angleOffset: 0.7f, predictive: 0.4f, cooldown: 400),
                        new Prioritize(
                            new StayCloseToSpawn(speed: 1.3, range: 7),
                            new Orbit(speed: 1.8, radius: 4, acquireRange: 5, target: null),
                            new Follow(speed: 6, acquireRange: 10, range: 2),
                            new Follow(speed: 0.3, acquireRange: 10, range: 0.2)
                        ),
                        new State("check_if_not_moving",
                            new NotMovingTransition(targetState: "boom", delay: 500)
                            ),
                        new State("boom",
                            new Shoot(range: 8, count: 18, shootAngle: 20, index: 0, angleOffset: 0.4f, predictive: 0.4f, cooldown: 1500),
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(time: 1000, targetState: "check_if_not_moving")
                            ),
                        new TimedTransition(time: 10000, targetState: "set_up_the_box")
                        ),
                new State("set_up_the_box",
                        new TossObject(child: "Limon Element 1", range: 9.5, angle: 315, cooldown: 1000000),
                        new TossObject(child: "Limon Element 2", range: 9.5, angle: 225, cooldown: 1000000),
                        new TossObject(child: "Limon Element 3", range: 9.5, angle: 135, cooldown: 1000000),
                        new TossObject(child: "Limon Element 4", range: 9.5, angle: 45, cooldown: 1000000),
                        new TossObject(child: "Limon Element 1", range: 14, angle: 315, cooldown: 1000000),
                        new TossObject(child: "Limon Element 2", range: 14, angle: 225, cooldown: 1000000),
                        new TossObject(child: "Limon Element 3", range: 14, angle: 135, cooldown: 1000000),
                        new TossObject(child: "Limon Element 4", range: 14, angle: 45, cooldown: 1000000),
                        new State("shielded1",
                            new Shoot(range: 8, count: 1, predictive: 0.1f, cooldown: 1000),
                            new Shoot(range: 8, count: 3, shootAngle: 120, angleOffset: 0.3f, predictive: 0.1f, cooldown: 500),
                            new TimedTransition(targetState: "shielded2", 1500)
                            ),
                        new State("shielded2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(range: 8, count: 3, shootAngle: 120, angleOffset: 0.3f, predictive: 0.2f, cooldown: 800),
                            new TimedTransition(targetState: "shielded2", 3500)
                            ),
                        new TimedTransition(time: 20000, targetState: "Summon_the_sprites")
                        ),
                new State("Summon_the_sprites",
                        new StayCloseToSpawn(speed: 0.5, range: 8),
                        new Wander(speed: 0.5f),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Shoot(range: 8, count: 3, shootAngle: 15, cooldown: 1300),
                        new Spawn(children: "Magic Sprite", maxChildren: 2, initialSpawn: 0, cooldown: 500),
                        new Spawn(children: "Ice Sprite", maxChildren: 1, initialSpawn: 0, cooldown: 500),
                        new TimedTransition(time: 11000, targetState: "begin_teleport1"),
                        new HpLessTransition(threshold: 0.2, targetState: "begin_teleport1")
                        ),
                new Threshold(0.01f,
                    new TierLoot(tier: 7, type: TierLoot.LootType.Armor, chance: 0.15f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ability, chance: 0.11f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.124f),
                    new TierLoot(tier: 5, type: TierLoot.LootType.Ability, chance: 0.11f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ring, chance: 0.11f),
                    new ItemLoot(item: "Potion of Dexterity", chance: 0.3f, min: 1),
                    new ItemLoot(item: "Potion of Defense", chance: 0.1f),
                    new ItemLoot(item: "Sprite Wand", chance: 0.01f),
                    new ItemLoot(item: "Wine Cellar Incantation", chance: 0.02f),
                    new ItemLoot(item: "Staff of Extreme Prejudice", chance: 0.01f),
                    new ItemLoot(item: "Cloak of the Planewalker", chance: 0.01f)
                    ));
            db.Init("Limon Element 1",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new Decay(time: 20000),
                new State("Setup",
                        new TimedTransition(time: 2000, targetState: "Attacking1"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking1",
                        new Shoot(range: 999, fixedAngle: 180, defaultAngle: 180, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 90, defaultAngle: 90, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Attacking2"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking2",
                        new Shoot(range: 999, fixedAngle: 180, defaultAngle: 180, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 90, defaultAngle: 90, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 135, defaultAngle: 135, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Attacking3"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking3",
                        new Shoot(range: 999, fixedAngle: 180, defaultAngle: 180, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 90, defaultAngle: 90, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Setup"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Suicide",
                        new Suicide(), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")));
            db.Init("Limon Element 2",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new Decay(time: 20000),
                new State("Setup",
                        new TimedTransition(time: 2000, targetState: "Attacking1"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking1",
                        new Shoot(range: 999, fixedAngle: 90, defaultAngle: 90, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 0, defaultAngle: 0, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Attacking2"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking2",
                        new Shoot(range: 999, fixedAngle: 90, defaultAngle: 90, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 0, defaultAngle: 0, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 45, defaultAngle: 45, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Attacking3"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking3",
                        new Shoot(range: 999, fixedAngle: 90, defaultAngle: 90, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 0, defaultAngle: 0, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Setup"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Suicide",
                        new Suicide(), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")));
            db.Init("Limon Element 3",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new Decay(time: 20000),
                new State("Setup",
                        new TimedTransition(time: 2000, targetState: "Attacking1"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking1",
                        new Shoot(range: 999, fixedAngle: 0, defaultAngle: 0, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 270, defaultAngle: 270, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Attacking2"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking2",
                        new Shoot(range: 999, fixedAngle: 0, defaultAngle: 0, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 270, defaultAngle: 270, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 315, defaultAngle: 315, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Attacking3"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking3",
                        new Shoot(range: 999, fixedAngle: 0, defaultAngle: 0, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 270, defaultAngle: 270, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Setup"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Suicide",
                        new Suicide(), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")));
            db.Init("Limon Element 4",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new Decay(time: 20000),
                new State("Setup",
                        new TimedTransition(time: 2000, targetState: "Attacking1"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking1",
                        new Shoot(range: 999, fixedAngle: 270, defaultAngle: 270, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 180, defaultAngle: 180, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Attacking2"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking2",
                        new Shoot(range: 999, fixedAngle: 270, defaultAngle: 270, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 180, defaultAngle: 180, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 225, defaultAngle: 225, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Attacking3"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Attacking3",
                        new Shoot(range: 999, fixedAngle: 270, defaultAngle: 270, cooldown: 300),
                        new Shoot(range: 999, fixedAngle: 180, defaultAngle: 180, cooldown: 300),
                        new TimedTransition(time: 6000, targetState: "Setup"), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")),
                new State("Suicide",
                        new Suicide(), new EntityNotExistsTransition(target: "Limon the Sprite God", dist: 999, targetState: "Suicide")));
        }
    }
}
