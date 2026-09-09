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
                new ConditionalEffect(ConditionEffectIndex.Invincible)
            );
            db.Init("Arena Headless Horseman",
                new Spawn("Arena Horseman Anchor", 1, 1),
                new State("EverythingIsCool",
                    new HpLessTransition(0.1f, "End"),
                    new State("Circle",
                        new Shoot(15, 3, shootAngle: 25, index: 0, cooldown: 1000),
                        new Shoot(15, index: 1, cooldown: 1000),
                        new Orbit(1, 5, 10, "Arena Horseman Anchor"),
                        new TimedTransition(8000, "Shoot")
                    ),
                    new State("Shoot",
                        new ReturnToSpawn(1.5f),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Flash(0xF0E68C, 1, 6),
                        new Shoot(15, 8, index: 2, cooldown: 1500),
                        new Shoot(15, index: 1, cooldown: 2500),
                        new TimedTransition(6000, "Circle")
                    )
                ),
                new State("End",
                    new Prioritize(
                        new Follow(1.5f, 20, 1),
                        new Wander(1.5f)
                    ),
                    new Flash(0xF0E68C, 1, 1000),
                    new Shoot(15, 3, shootAngle: 25, index: 0, cooldown: 1000),
                    new Shoot(15, index: 1, cooldown: 1000)
                ),
                new DropPortalOnDeath("Haunted Cemetery Portal", 0.4f)
            );
            db.Init("White Demon",
                new DropPortalOnDeath("Abyss of Demons Portal", 0.17f),
                new Prioritize(
                    new StayAbove(1, 200),
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(10, count: 3, shootAngle: 20, predictive: 1, cooldown: 500),
                new Reproduce(densityMax: 3),
                new Threshold(0.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.01f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.01f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.015f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.005f)
                ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Attack", 0.07f)
                )
            );
            db.Init("Sprite God",
                new Prioritize(
                    new StayAbove(1, 200),
                    new Wander(0.4f)
                ),
                new Shoot(12, index: 0, count: 4, shootAngle: 10),
                new Shoot(10, index: 1, predictive: 1),
                new Reproduce(densityMax: 3),
                new ReproduceChildren(5, 0.5f, cooldown: 5000, children: "Sprite Child"),
                new Threshold(0.01f,
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
                )
            );
            db.Init("Sprite Child",
                new Prioritize(
                    new StayAbove(1, 200),
                    new Protect(0.4f, "Sprite God", protectionRange: 1),
                    new Wander(0.4f)
                ),
                new DropPortalOnDeath("Glowing Portal", 0.11f)
            );
            db.Init("Medusa",
                new DropPortalOnDeath("Snake Pit Portal", 0.17f),
                new Prioritize(
                    new StayAbove(1, 200),
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(12, count: 5, shootAngle: 10, cooldown: 1000),
                new Grenade(radius: 4, damage: 150, range: 8, cooldown: 3000),
                new Reproduce(densityMax: 3),
                new Threshold(0.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.01f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.01f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.015f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.005f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.02f)
                ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Speed", 0.07f)
                )
            );
            db.Init("Ent God",
                new Prioritize(
                    new StayAbove(1, 200),
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(12, count: 5, shootAngle: 10, predictive: 1, cooldown: 1250),
                new Reproduce(densityMax: 3),
                new Threshold(0.01f,
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
                )
            );
            db.Init("Beholder",
                new Prioritize(
                    new StayAbove(1, 200),
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(12, index: 0, count: 5, shootAngle: 72, predictive: 0.5f, cooldown: 750),
                new Shoot(10, index: 1, predictive: 1),
                new Reproduce(densityMax: 3),
                new Threshold(0.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.01f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.01f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.015f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.005f)
                ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Defense", 0.07f)
                )
            );
            db.Init("Flying Brain",
                new Prioritize(
                    new StayAbove(1, 200),
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(12, count: 5, shootAngle: 72, cooldown: 500),
                new Reproduce(densityMax: 3),
                new DropPortalOnDeath("Mad Lab Portal", 0.17f),
                new Threshold(0.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.04f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.02f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.02f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.015f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.005f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.02f)
                ),
                new Threshold(0.07f,
                    new ItemLoot("Potion of Attack", 0.07f)
                )
            );
            db.Init("Slime God",
                new Prioritize(
                    new StayAbove(1, 200),
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(12, index: 0, count: 5, shootAngle: 10, predictive: 1, cooldown: 1000),
                new Shoot(10, index: 1, predictive: 1, cooldown: 650),
                new Reproduce(densityMax: 2),
                new Threshold(0.01f,
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
                )
            );
            db.Init("Ghost God",
                new Prioritize(
                    new StayAbove(1, 200),
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(12, count: 7, shootAngle: 25, predictive: 0.5f, cooldown: 900),
                new Reproduce(densityMax: 3),
                new DropPortalOnDeath("Undead Lair Portal", 0.17f),
                new Threshold(0.01f,
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
                )
            );
            db.Init("Rock Bot",
                new Spawn("Paper Bot", maxChildren: 1, initialSpawn: 1, cooldown: 10000),
                new Spawn("Steel Bot", maxChildren: 1, initialSpawn: 1, cooldown: 10000),
                new Swirl(speed: 0.6f, radius: 3, targeted: false),
                new State("Waiting",
                    new PlayerWithinTransition(15, "Attacking")
                ),
                new State("Attacking",
                    new Shoot(8, cooldown: 2000),
                    new HealGroup(8, "Papers", cooldown: 1000),
                    new Taunt(0.5f, "We are impervious to non-mystic attacks!"),
                    new TimedTransition(10000, "Waiting")
                ),
                new Threshold(0.01f,
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
                )
            );
            db.Init("Paper Bot",
                new DropPortalOnDeath("Puppet Theatre Portal", 0.15f),
                new Prioritize(
                    new Orbit(0.4f, 3, target: "Rock Bot"),
                    new Wander(0.8f)
                ),
                new State("Idle",
                    new PlayerWithinTransition(15, "Attack")
                ),
                new State("Attack",
                    new Shoot(8, count: 3, shootAngle: 20, cooldown: 800),
                    new HealGroup(8, "Steels", cooldown: 1000),
                    new NoPlayerWithinTransition(30, "Idle"),
                    new HpLessTransition(0.2f, "Explode")
                ),
                new State("Explode",
                    new Shoot(0, count: 10, shootAngle: 36, fixedAngle: 0),
                    new Decay(0)
                ),
                new Threshold(0.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.01f),
                    new ItemLoot("Health Potion", 0.04f),
                    new ItemLoot("Magic Potion", 0.01f),
                    new ItemLoot("Tincture of Life", 0.01f)
                ),
                new Threshold(0.04f,
                    new ItemLoot("Potion of Attack", 0.03f)
                )
            );
            db.Init("Steel Bot",
                new Prioritize(
                    new Orbit(0.4f, 3, target: "Rock Bot"),
                    new Wander(0.8f)
                ),
                new State("Idle",
                    new PlayerWithinTransition(15, "Attack")
                ),
                new State("Attack",
                    new Shoot(8, count: 3, shootAngle: 20, cooldown: 800),
                    new HealGroup(8, "Rocks", cooldown: 1000),
                    new Taunt(0.5f, "Silly squishy. We heal our brothers in a circle."),
                    new NoPlayerWithinTransition(30, "Idle"),
                    new HpLessTransition(0.2f, "Explode")
                ),
                new State("Explode",
                    new Shoot(0, count: 10, shootAngle: 36, fixedAngle: 0),
                    new Decay(0)
                ),
                new Threshold(0.01f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.01f),
                    new ItemLoot("Health Potion", 0.04f),
                    new ItemLoot("Magic Potion", 0.01f)
                ),
                new Threshold(0.04f,
                    new ItemLoot("Potion of Attack", 0.03f)
                )
            );
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
                        new TimedTransition(2000, "Wait")
                    ),
                    new State("Wait",
                        new Follow(0.7f, range: 0.5f),
                        new Flash(0xff00ff00, 0.1f, 20),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(2000, "Bullet")
                    ),
                    new NoPlayerWithinTransition(13, "Idle"),
                    new HpLessTransition(0.5f, "FlashBeforeExplode")
                ),
                new State("FlashBeforeExplode",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff0000, 0.3f, 3),
                    new TimedTransition(1000, "Explode")
                ),
                new State("Explode",
                    new Shoot(0, count: 10, shootAngle: 36, fixedAngle: 0),
                    new Suicide()
                ),
                new Threshold(0.01f,
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
                )
            );
            db.Init("Leviathan",
                new State("Wander",
                    new Swirl(),
                    new Shoot(10, 2, 10, 1, cooldown: 500),
                    new TimedTransition(5000, "Triangle")
                ),
                new State("Triangle",
                    new State("1",
                        new MoveLine(0.7f, 40),
                        new Shoot(1, 3, 120, fixedAngle: 34, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 38, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 42, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 46, cooldown: 300),
                        new TimedTransition(1500, "2")
                    ),
                    new State("2",
                        new MoveLine(0.7f, 160),
                        new Shoot(1, 3, 120, fixedAngle: 94, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 98, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 102, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 106, cooldown: 300),
                        new TimedTransition(1500, "3")
                    ),
                    new State("3",
                        new MoveLine(0.7f, 280),
                        new Shoot(1, 3, 120, fixedAngle: 274, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 278, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 282, cooldown: 300),
                        new Shoot(1, 3, 120, fixedAngle: 286, cooldown: 300),
                        new TimedTransition(1500, "Wander"))
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Defense", 0.07f),
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.01f),
                    new ItemLoot("Health Potion", 0.04f),
                    new ItemLoot("Magic Potion", 0.01f)
                )
            );
        }
    }
}
