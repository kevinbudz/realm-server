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
    public class Mountain : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Arena Horseman Anchor",
                new ConditionalEffect(ConditionEffectIndex.Invincible));
            db.Init("Arena Headless Horseman",
                new Spawn("Arena Horseman Anchor", 1, 1),
                new DropPortalOnDeath("Haunted Cemetery Portal", .4),
                new State("EverythingIsCool",
                        new HpLessTransition(0.1, "End"),
                        new State("Circle",
                            new Shoot(15, 3, shootAngle: 25, index: 0, cooldown: 1000),
                            new Shoot(15, index: 1, cooldown: 1000),
                            new Orbit(1, 5, 10, "Arena Horseman Anchor"),
                            new TimedTransition("Shoot", 8000)
                            ),
                        new State("Shoot",
                            new ReturnToSpawn(1.5),
                            new ConditionalEffect(ConditionEffectIndex.Invincible),
                            new Flash(0xF0E68C, 1, 6),
                            new Shoot(15, 8, index: 2, cooldown: 1500),
                            new Shoot(15, index: 1, cooldown: 2500),
                            new TimedTransition("Circle", 6000)
                            )
                        ),
                new State("End",
                        new Prioritize(
                            new Follow(1.5, 20, 1),
                            new Wander(1.5f)
                            ),
                        new Flash(0xF0E68C, 1, 1000),
                        new Shoot(15, 3, shootAngle: 25, index: 0, cooldown: 1000),
                        new Shoot(15, index: 1, cooldown: 1000)
                        ));
            db.Init("Sprite God",
                new Prioritize(
                        new StayAbove(1, 200),
                        new Wander(0.4f)
                        ),
                new Shoot(12, index: 0, count: 4, shootAngle: 10),
                new Shoot(10, index: 1, predictive: 1),
                new Reproduce(densityMax: 3),
                new ReproduceChildren(5, .5, 5000, 0, "Sprite Child"),
                new Threshold(.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.01f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.01f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.02f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.02f)
                    ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Attack", 0.07f)
                    ));
            db.Init("Sprite Child",
                new Prioritize(
                        new StayAbove(1, 200),
                        new Protect(0.4, "Sprite God", protectionRange: 1),
                        new Wander(0.4f)
                        ),
                new DropPortalOnDeath("Glowing Portal", .11));
            db.Init("Ent God",
                new Prioritize(
                        new StayAbove(1, 200),
                        new Follow(1, range: 7),
                        new Wander(0.4f)
                        ),
                new Shoot(12, count: 5, shootAngle: 10, predictive: 1, cooldown: 1250),
                new Reproduce(densityMax: 3),
                new Threshold(.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.01f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.01f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.02f)
                    ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Defense", 0.07f)
                    ));
            db.Init("Slime God",
                new Prioritize(
                        new StayAbove(1, 200),
                        new Follow(1, range: 7),
                        new Wander(0.4f)
                        ),
                new Shoot(12, index: 0, count: 5, shootAngle: 10, predictive: 1, cooldown: 1000),
                new Shoot(10, index: 1, predictive: 1, cooldown: 650),
                new Reproduce(densityMax: 2),
                new Threshold(.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.01f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.01f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.02f)
                    ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Defense", 0.07f)
                    ));
            db.Init("Ghost God",
                new Prioritize(
                        new StayAbove(1, 200),
                        new Follow(1, range: 7),
                        new Wander(0.4f)
                        ),
                new Shoot(12, count: 7, shootAngle: 25, predictive: 0.5f, cooldown: 900),
                new Reproduce(densityMax: 3),
                new DropPortalOnDeath("Undead Lair Portal", 0.17),
                new Threshold(.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.015f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.005f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.02f)
                    ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Speed", 0.07f)
                    ));
            db.Init("Rock Bot",
                new Spawn("Paper Bot", maxChildren: 1, initialSpawn: 1, cooldown: 10000),
                new Spawn("Steel Bot", maxChildren: 1, initialSpawn: 1, cooldown: 10000),
                new Swirl(speed: 0.6, radius: 3, targeted: false),
                new State("Waiting",
                        new PlayerWithinTransition(15, "Attacking")
                        ),
                new State("Attacking",
                        new Shoot(8, cooldown: 2000),
                        new HealGroup(8, "Papers", cooldown: 1000),
                        new Taunt(0.5, "We are impervious to non-mystic attacks!"),
                        new TimedTransition("Waiting", 10000)
                        ),
                new Threshold(.01f,
                    new TierLoot(5, TierLoot.LootType.Weapon, 0.16f),
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.08f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(5, TierLoot.LootType.Armor, 0.16f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.08f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.05f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.1f),
                    new ItemLoot("Purple Drake Egg", 0.01f)
                    ),
                new Threshold(0.04f,
                    new ItemLoot("Potion of Attack", 0.03f)
                    ));
            db.Init("Paper Bot",
                new DropPortalOnDeath("Puppet Theatre Portal", 0.15),
                new Prioritize(
                        new Orbit(0.4, 3, target: "Rock Bot"),
                        new Wander(0.8f)
                        ),
                new State("Idle",
                        new PlayerWithinTransition(15, "Attack")
                        ),
                new State("Attack",
                        new Shoot(8, count: 3, shootAngle: 20, cooldown: 800),
                        new HealGroup(8, "Steels", cooldown: 1000),
                        new NoPlayerWithinTransition(30, "Idle"),
                        new HpLessTransition(0.2, "Explode")
                        ),
                new State("Explode",
                        new Shoot(0, count: 10, shootAngle: 36, fixedAngle: 0),
                        new Decay(0)
                        ),
                new Threshold(.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.01f),
                    new ItemLoot("Health Potion", 0.04f),
                    new ItemLoot("Magic Potion", 0.01f),
                    new ItemLoot("Tincture of Life", 0.01f)
                    ),
                new Threshold(0.04f,
                    new ItemLoot("Potion of Attack", 0.03f)
                    ));
            db.Init("Steel Bot",
                new Prioritize(
                        new Orbit(0.4, 3, target: "Rock Bot"),
                        new Wander(0.8f)
                        ),
                new State("Idle",
                        new PlayerWithinTransition(15, "Attack")
                        ),
                new State("Attack",
                        new Shoot(8, count: 3, shootAngle: 20, cooldown: 800),
                        new HealGroup(8, "Rocks", cooldown: 1000),
                        new Taunt(0.5, "Silly squishy. We heal our brothers in a circle."),
                        new NoPlayerWithinTransition(30, "Idle"),
                        new HpLessTransition(0.2, "Explode")
                        ),
                new State("Explode",
                        new Shoot(0, count: 10, shootAngle: 36, fixedAngle: 0),
                        new Decay(0)
                        ),
                new Threshold(.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.01f),
                    new ItemLoot("Health Potion", 0.04f),
                    new ItemLoot("Magic Potion", 0.01f)
                    ),
                new Threshold(0.04f,
                    new ItemLoot("Potion of Attack", 0.03f)
                    ));
            db.Init("Djinn",
                new State("Idle",
                        new Prioritize(
                            new StayAbove(1, 200),
                            new Wander(0.8f)
                            ),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Reproduce(densityMax: 3, densityRadius: 20),
                        new PlayerWithinTransition(8, "Attacking")
                        ),
                new State("Attacking",
                        new State("Bullet",
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 90, cooldownOffset: 0, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 100, cooldownOffset: 200, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 110, cooldownOffset: 400, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 120, cooldownOffset: 600, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 130, cooldownOffset: 800, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 140, cooldownOffset: 1000,
                                shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 150, cooldownOffset: 1200,
                                shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 160, cooldownOffset: 1400,
                                shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 170, cooldownOffset: 1600,
                                shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 180, cooldownOffset: 1800,
                                shootAngle: 90),
                            new Shoot(1, count: 8, cooldown: 10000, fixedAngle: 180, cooldownOffset: 2000,
                                shootAngle: 45),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 180, cooldownOffset: 0, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 170, cooldownOffset: 200, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 160, cooldownOffset: 400, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 150, cooldownOffset: 600, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 140, cooldownOffset: 800, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 130, cooldownOffset: 1000,
                                shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 120, cooldownOffset: 1200,
                                shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 110, cooldownOffset: 1400,
                                shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 100, cooldownOffset: 1600,
                                shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 90, cooldownOffset: 1800, shootAngle: 90),
                            new Shoot(1, count: 4, cooldown: 10000, fixedAngle: 90, cooldownOffset: 2000,
                                shootAngle: 22.5f),
                            new TimedTransition("Wait", 2000)
                            ),
                        new State("Wait",
                            new Follow(0.7, range: 0.5),
                            new Flash(0xff00ff00, 0.1, 20),
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("Bullet", 2000)
                            ),
                        new NoPlayerWithinTransition(13, "Idle"),
                        new HpLessTransition(0.5, "FlashBeforeExplode")
                        ),
                new State("FlashBeforeExplode",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Flash(0xff0000, 0.3, 3),
                        new TimedTransition("Explode", 1000)
                        ),
                new State("Explode",
                        new Shoot(0, count: 10, shootAngle: 36, fixedAngle: 0),
                        new Suicide()
                        ),
                new Threshold(.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.015f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.005f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.02f)
                    ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Speed", 0.07f)
                    ));
            db.Init("Leviathan",
                new State("Wander",
                        new Swirl(),
                        new Shoot(10, 2, 10, 1, cooldown: 500),
                        new TimedTransition("Triangle", 5000)
                        ),
                new State("Triangle",
                        new State("1",
                            new MoveLine(.7, 40),
                            new Shoot(1, 3, 120, fixedAngle: 34, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 38, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 42, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 46, cooldown: 300),
                            new TimedTransition("2", 1500)
                            ),
                        new State("2",
                            new MoveLine(.7, 160),
                            new Shoot(1, 3, 120, fixedAngle: 94, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 98, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 102, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 106, cooldown: 300),
                            new TimedTransition("3", 1500)
                            ),
                        new State("3",
                            new MoveLine(.7, 280),
                            new Shoot(1, 3, 120, fixedAngle: 274, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 278, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 282, cooldown: 300),
                            new Shoot(1, 3, 120, fixedAngle: 286, cooldown: 300),
                            new TimedTransition("Wander", 1500))
                        ),
                new Threshold(.01f,
                    new ItemLoot("Potion of Defense", 0.07f),
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.01f),
                    new ItemLoot("Health Potion", 0.04f),
                    new ItemLoot("Magic Potion", 0.01f)
                    ));
        }
    }
}
