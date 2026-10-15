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
    public class Highland : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Minotaur",
                            
                                new DropPortalOnDeath("Snake Pit Portal", 0.2f, timeout: 100),
                                new State("idle",
                                    new StayAbove(0.6f, 160),
                                    new PlayerWithinTransition(10, "charge")
                                    ),
                                new State("charge",
                                    new Prioritize(
                                        new StayAbove(0.6f, 160),
                                        new Follow(6, 11, 1.6f)
                                        ),
                                    new TimedTransition(200, "spam_blades")
                                    ),
                                new State("spam_blades",
                                    new Shoot(8, index: 0, count: 1, cooldown: 100000, cooldownOffset: 1000),
                                    new Shoot(8, index: 0, count: 2, shootAngle: 16, cooldown: 100000,
                                        cooldownOffset: 1200),
                                    new Shoot(8, index: 0, count: 3, predictive: 0.2f, cooldown: 100000,
                                        cooldownOffset: 1600),
                                    new Shoot(8, index: 0, count: 1, shootAngle: 24, cooldown: 100000,
                                        cooldownOffset: 2200),
                                    new Shoot(8, index: 0, count: 2, predictive: 0.2f, cooldown: 100000,
                                        cooldownOffset: 2800),
                                    new Shoot(8, index: 0, count: 3, shootAngle: 16, cooldown: 100000,
                                        cooldownOffset: 3200),
                                    new Prioritize(
                                        new StayAbove(0.6f, 160),
                                        new Wander(0.6f)
                                        ),
                                    new TimedTransition(4400, "blade_ring")
                                    ),
                                new State("blade_ring",
                                    new Shoot(7, fixedAngle: 0, count: 12, shootAngle: 30, cooldown: 800, index: 1,
                                        cooldownOffset: 600),
                                    new Shoot(7, fixedAngle: 15, count: 6, shootAngle: 60, cooldown: 800, index: 2,
                                        cooldownOffset: 1000),
                                    new Prioritize(
                                        new StayAbove(0.6f, 160),
                                        new Follow(0.6f, 10, 1),
                                        new Wander(0.6f)
                                        ),
                                    new TimedTransition(3500, "pause")
                                    ),
                                new State("pause",
                                    new Prioritize(
                                        new StayAbove(0.6f, 160),
                                        new Wander(0.6f)
                                        ),
                                    new TimedTransition(1000, "idle")
                                    )
                                ,
                            new TierLoot(5, TierLoot.LootType.Weapon, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Weapon, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Weapon, 0.04f),
                            new TierLoot(5, TierLoot.LootType.Armor, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Armor, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                            new TierLoot(3, TierLoot.LootType.Ring, 0.05f),
                            new TierLoot(3, TierLoot.LootType.Ability, 0.2f),
                            new ItemLoot("Purple Drake Egg", 0.005f)
                        );

            db.Init("Ogre King",
                            
                                new DropPortalOnDeath("Snake Pit Portal", 0.2f, timeout: 100),
                                new Spawn("Ogre Warrior", 4, cooldown: 12000),
                                new Spawn("Ogre Mage", 2, cooldown: 16000),
                                new Spawn("Ogre Wizard", 2, cooldown: 20000),
                                new State("idle",
                                    new Prioritize(
                                        new StayAbove(0.3f, 160),
                                        new Wander(0.3f)
                                        ),
                                    new PlayerWithinTransition(10, "grenade_blade_combo")
                                    ),
                                new State("grenade_blade_combo",
                                    new State("grenade1",
                                        new Grenade(range: 5, damage: 60, radius: 3, cooldown: 100000),
                                        new Prioritize(
                                            new StayAbove(0.3f, 160),
                                            new Wander(0.3f)
                                            ),
                                        new TimedTransition(2000, "grenade2")
                                        ),
                                    new State("grenade2",
                                        new Grenade(range: 5, damage: 60, radius: 3, cooldown: 100000),
                                        new Prioritize(
                                            new StayAbove(0.5f, 160),
                                            new Wander(0.5f)
                                            ),
                                        new TimedTransition(3000, "slow_follow")
                                        ),
                                    new State("slow_follow",
                                        new Shoot(13, cooldown: 1000),
                                        new Prioritize(
                                            new StayAbove(0.4f, 160),
                                            new Follow(0.4f, 9, 3.5f, 4),
                                            new Wander(0.4f)
                                            ),
                                        new TimedTransition(4000, "grenade1")
                                        ),
                                    new HealthTransition(0.45f, "furious")
                                    ),
                                new State("furious",
                                    new Grenade(range: 9, damage: 55, radius: 2.4f, cooldown: 1500),
                                    new Prioritize(
                                        new StayAbove(0.6f, 160),
                                        new Wander(0.6f)
                                        ),
                                    new TimedTransition(12000, "idle")
                                    )
                                ,
                            new TierLoot(4, TierLoot.LootType.Weapon, 0.2f),
                            new TierLoot(5, TierLoot.LootType.Weapon, 0.02f),
                            new TierLoot(4, TierLoot.LootType.Armor, 0.2f),
                            new TierLoot(5, TierLoot.LootType.Armor, 0.12f),
                            new TierLoot(6, TierLoot.LootType.Armor, 0.02f),
                            new TierLoot(2, TierLoot.LootType.Ring, 0.1f),
                            new TierLoot(2, TierLoot.LootType.Ability, 0.18f)
                        );

            db.Init("Ogre Warrior",
                            
                                new Shoot(3, predictive: 0.5f),
                                new Prioritize(
                                    new StayAbove(1.2f, 160),
                                    new Protect(1.2f, "Ogre King", 15, 10, 5),
                                    new Follow(1.4f, 10.5f, 1.6f, 2600, 2200),
                                    new Orbit(0.6f, 6),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Magic Potion", 0.03f)
                        );

            db.Init("Ogre Mage",
                            
                                new Shoot(10, predictive: 0.3f),
                                new Prioritize(
                                    new StayAbove(1.2f, 160),
                                    new Protect(1.2f, "Ogre King", 30, 10, reprotectRange: 1),
                                    new Orbit(0.5f, 6),
                                    new Wander(0.4f)
                                    )
                                
                        );

            db.Init("Ogre Wizard",
                            
                                new Shoot(10, cooldown: 300),
                                new Prioritize(
                                    new StayAbove(1.2f, 160),
                                    new Protect(1.2f, "Ogre King", 30, 10, reprotectRange: 1),
                                    new Orbit(0.5f, 6),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Magic Potion", 0.03f)
                        );

            db.Init("Lizard God",
                            
                                new DropPortalOnDeath("Snake Pit Portal", 0.2f, timeout: 100),
                                new Spawn("Night Elf Archer", 4),
                                new Spawn("Night Elf Warrior", 3),
                                new Spawn("Night Elf Mage", 2),
                                new Spawn("Night Elf Veteran", 2),
                                new Spawn("Night Elf King", 1),
                                new Prioritize(
                                    new StayAbove(0.3f, 160),
                                    new Follow(1, range: 7),
                                    new Wander(0.4f)
                                    ),
                                new State("idle",
                                    new PlayerWithinTransition(10.2f, "normal_attack")
                                    ),
                                new State("normal_attack",
                                    new Shoot(10, 3, 3, predictive: 0.5f),
                                    new TimedTransition(4000, "if_cloaked")
                                    ),
                                new State("if_cloaked",
                                    new Shoot(10, 8, 45, fixedAngle: 20, cooldown: 1600, cooldownOffset: 400),
                                    new Shoot(10, 8, 45, fixedAngle: 42, cooldown: 1600, cooldownOffset: 1200),
                                    new PlayerWithinTransition(10, "normal_attack")
                                    )
                                ,
                            new TierLoot(5, TierLoot.LootType.Weapon, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Weapon, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Weapon, 0.04f),
                            new TierLoot(5, TierLoot.LootType.Armor, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Armor, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                            new TierLoot(3, TierLoot.LootType.Ring, 0.05f),
                            new TierLoot(3, TierLoot.LootType.Ability, 0.15f),
                            new ItemLoot("Purple Drake Egg", 0.005f)
                        );

            db.Init("Night Elf Archer",
                            
                                new Shoot(10, predictive: 1),
                                new Prioritize(
                                    new StayAbove(0.4f, 160),
                                    new Follow(1.5f, range: 7),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.03f)
                        );

            db.Init("Night Elf Warrior",
                            
                                new Shoot(3, predictive: 1),
                                new Prioritize(
                                    new StayAbove(0.4f, 160),
                                    new Follow(1.5f, range: 1),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.03f)
                        );

            db.Init("Night Elf Mage",
                            
                                new Shoot(10, predictive: 1),
                                new Prioritize(
                                    new StayAbove(0.4f, 160),
                                    new Follow(1.5f, range: 7),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.03f)
                        );

            db.Init("Night Elf Veteran",
                            
                                new Shoot(10, predictive: 1),
                                new Prioritize(
                                    new StayAbove(0.4f, 160),
                                    new Follow(1.5f, range: 7),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.03f)
                        );

            db.Init("Night Elf King",
                            
                                new Shoot(10, predictive: 1),
                                new Prioritize(
                                    new StayAbove(0.4f, 160),
                                    new Follow(1.5f, range: 7),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.03f)
                        );

            db.Init("Undead Dwarf God",
                            
                                new Spawn("Undead Dwarf Warrior", 3),
                                new Spawn("Undead Dwarf Axebearer", 3),
                                new Spawn("Undead Dwarf Mage", 3),
                                new Spawn("Undead Dwarf King", 2),
                                new Spawn("Soulless Dwarf", 1),
                                new Prioritize(
                                    new StayAbove(0.3f, 160),
                                    new Follow(1, range: 7),
                                    new Wander(0.4f)
                                    ),
                                new Shoot(10, index: 0, count: 3, shootAngle: 15),
                                new Shoot(10, index: 1, predictive: 0.5f, cooldown: 1200)
                                ,
                            new TierLoot(5, TierLoot.LootType.Weapon, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Weapon, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Weapon, 0.04f),
                            new TierLoot(5, TierLoot.LootType.Armor, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Armor, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                            new TierLoot(3, TierLoot.LootType.Ring, 0.05f),
                            new TierLoot(3, TierLoot.LootType.Ability, 0.2f),
                            new ItemLoot("Purple Drake Egg", 0.005f)
                        );

            db.Init("Undead Dwarf Warrior",
                            
                                new DropPortalOnDeath("Spider Den Portal", 0.2f, timeout: 100),
                                new Shoot(3),
                                new Prioritize(
                                    new StayAbove(1, 160),
                                    new Follow(1, range: 1),
                                    new Wander(0.4f)
                                    )
                                
                        );

            db.Init("Undead Dwarf Axebearer",
                            
                                new Shoot(3),
                                new Prioritize(
                                    new StayAbove(1, 160),
                                    new Follow(1, range: 1),
                                    new Wander(0.4f)
                                    )
                                
                        );

            db.Init("Undead Dwarf Mage",
                            
                                new State("circle_player",
                                    new Shoot(8, predictive: 0.3f, cooldown: 1000, cooldownOffset: 500),
                                    new Prioritize(
                                        new StayAbove(0.7f, 160),
                                        new Protect(0.7f, "Undead Dwarf King", 11, 10, 3),
                                        new Orbit(0.7f, 3.5f, 11),
                                        new Wander(0.7f)
                                        ),
                                    new TimedTransition(3500, "circle_king")
                                    ),
                                new State("circle_king",
                                    new Shoot(8, 5, 72, defaultAngle: 20, predictive: 0.3f, cooldown: 1600, cooldownOffset: 500),
                                    new Shoot(8, 5, 72, defaultAngle: 33, predictive: 0.3f, cooldown: 1600, cooldownOffset: 1300),
                                    new Prioritize(
                                        new StayAbove(0.7f, 160),
                                        new Orbit(1.2f, 2.5f, target: "Undead Dwarf King", acquireRange: 12, radiusVariance: 0.1f,
                                            speedVariance: 0.1f),
                                        new Wander(0.7f)
                                        ),
                                    new TimedTransition(3500, "circle_player")
                                    )
                                ,
                            new ItemLoot("Magic Potion", 0.03f)
                        );

            db.Init("Undead Dwarf King",
                            
                                new Shoot(3),
                                new Prioritize(
                                    new StayAbove(1, 160),
                                    new Follow(0.8f, range: 1.4f),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.03f)
                        );

            db.Init("Soulless Dwarf",
                            
                                new Shoot(10),
                                new State("idle",
                                    new PlayerWithinTransition(10.5f, "run1")
                                    ),
                                new State("run1",
                                    new Prioritize(
                                        new StayAbove(0.4f, 160),
                                        new Protect(1.1f, "Undead Dwarf God", 16, 10, reprotectRange: 1),
                                        new Wander(0.4f)
                                        ),
                                    new TimedTransition(2000, "run2")
                                    ),
                                new State("run2",
                                    new Prioritize(
                                        new StayAbove(0.4f, 160),
                                        new StayBack(0.8f, 4),
                                        new Wander(0.4f)
                                        ),
                                    new TimedTransition(1400, "run3")
                                    ),
                                new State("run3",
                                    new Prioritize(
                                        new StayAbove(0.4f, 160),
                                        new Protect(1, "Undead Dwarf King", 16, 2, 2),
                                        new Protect(1, "Undead Dwarf Axebearer", 16, 2, 2),
                                        new Protect(1, "Undead Dwarf Warrior", 16, 2, 2),
                                        new Wander(0.4f)
                                        ),
                                    new TimedTransition(2000, "idle")
                                    )
                                ,
                            new ItemLoot("Magic Potion", 0.03f)
                        );

            db.Init("Flayer God",
                            
                                new DropPortalOnDeath("Snake Pit Portal", 0.2f, timeout: 100),
                                new Spawn("Flayer", 2),
                                new Spawn("Flayer Veteran", 3),
                                new Reproduce("Flayer God", densityMax: 2),
                                new Prioritize(
                                    new StayAbove(0.4f, 155),
                                    new Follow(1, range: 7),
                                    new Wander(0.4f)
                                    ),
                                new Shoot(10, index: 0, predictive: 0.5f, cooldown: 400),
                                new Shoot(10, index: 1, predictive: 1)
                                ,
                            new TierLoot(5, TierLoot.LootType.Weapon, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Weapon, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Weapon, 0.04f),
                            new TierLoot(5, TierLoot.LootType.Armor, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Armor, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                            new TierLoot(3, TierLoot.LootType.Ring, 0.05f),
                            new TierLoot(3, TierLoot.LootType.Ability, 0.15f),
                            new ItemLoot("Purple Drake Egg", 0.005f)
                        );

            db.Init("Flayer",
                            
                                new Shoot(10, predictive: 0.5f),
                                new Prioritize(
                                    new StayAbove(1, 155),
                                    new Follow(1.2f, range: 7),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Magic Potion", 0.03f)
                        );

            db.Init("Flayer Veteran",
                            
                                new Shoot(10, predictive: 0.5f),
                                new Prioritize(
                                    new StayAbove(1, 155),
                                    new Follow(1.2f, range: 7),
                                    new Wander(0.4f)
                                    )
                                ,
                            new ItemLoot("Magic Potion", 0.03f)
                        );

            db.Init("Flamer King",
                            
                                new Spawn("Flamer", 5, cooldown: 10000),
                                new State("Attacking",
                                    new State("Charge",
                                        new Follow(0.7f, range: 0.1f),
                                        new PlayerWithinTransition(2, "Bullet")
                                        ),
                                    new State("Bullet",
                                        new Flash(0xffffaa00, 0.2f, 20),
                                        new ChangeSize(20, 140),
                                        new Shoot(8, cooldown: 200),
                                        new TimedTransition(4000, "Wait")
                                        ),
                                    new State("Wait",
                                        new ChangeSize(-20, 80),
                                        new TimedTransition(500, "Charge")
                                        ),
                                    new HealthTransition(0.2f, "FlashBeforeExplode")
                                    ),
                                new State("FlashBeforeExplode",
                                    new Flash(0xffff0000, 1, 1),
                                    new TimedTransition(300, "Explode")
                                    ),
                                new State("Explode",
                                    new Shoot(0, 10, 36, fixedAngle: 0),
                                    new Decay(0)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.01f),
                            new ItemLoot("Magic Potion", 0.01f),
                            new TierLoot(2, TierLoot.LootType.Ring, 0.04f)
                        );

            db.Init("Flamer",
                            
                                new State("Attacking",
                                    new State("Charge",
                                        new Prioritize(
                                            new Protect(0.7f, "Flamer King"),
                                            new Follow(0.7f, range: 0.1f)
                                            ),
                                        new PlayerWithinTransition(2, "Bullet")
                                        ),
                                    new State("Bullet",
                                        new Flash(0xffffaa00, 0.2f, 20),
                                        new ChangeSize(20, 130),
                                        new Shoot(8, cooldown: 200),
                                        new TimedTransition(4000, "Wait")
                                        ),
                                    new State("Wait",
                                        new ChangeSize(-20, 70),
                                        new TimedTransition(600, "Charge")
                                        ),
                                    new HealthTransition(0.2f, "FlashBeforeExplode")
                                    ),
                                new State("FlashBeforeExplode",
                                    new Flash(0xffff0000, 1, 1),
                                    new TimedTransition(300, "Explode")
                                    ),
                                new State("Explode",
                                    new Shoot(0, 10, 36, fixedAngle: 0),
                                    new Decay(0)
                                    )
                                ,
                            new ItemLoot("Magic Potion", 0.2f),
                            new TierLoot(5, TierLoot.LootType.Weapon, 0.04f)
                        );

            db.Init("Dragon Egg",
                            
                                new TransformOnDeath("White Dragon Whelp", probability: 0.3f),
                                new TransformOnDeath("Juvenile White Dragon", probability: 0.2f),
                                new TransformOnDeath("Adult White Dragon", probability: 0.1f)
                                
                        );

            db.Init("White Dragon Whelp",
                            
                                new Shoot(10, 2, 20, predictive: 0.3f, cooldown: 750),
                                new Prioritize(
                                    new StayAbove(1, 150),
                                    new Follow(2, range: 2.5f, acquireRange: 10.5f, duration: 2200, cooldown: 3200),
                                    new Wander(0.9f)
                                    )
                                
                        );

            db.Init("Juvenile White Dragon",
                            
                                new Shoot(10, 2, 20, predictive: 0.3f, cooldown: 750),
                                new Prioritize(
                                    new StayAbove(9, 150),
                                    new Follow(1.8f, range: 2.2f, acquireRange: 10.5f, duration: 3000, cooldown: 3000),
                                    new Wander(0.75f)
                                    )
                                ,
                            new TierLoot(7, TierLoot.LootType.Weapon, 0.01f),
                            new TierLoot(7, TierLoot.LootType.Armor, 0.02f),
                            new TierLoot(6, TierLoot.LootType.Armor, 0.07f)
                        );

            db.Init("Adult White Dragon",
                            
                                new Shoot(10, 3, 15, predictive: 0.3f, cooldown: 750),
                                new Prioritize(
                                    new StayAbove(9, 150),
                                    new Follow(1.4f, range: 1.8f, acquireRange: 10.5f, duration: 4000, cooldown: 2000),
                                    new Wander(0.75f)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.03f),
                            new ItemLoot("Magic Potion", 0.03f),
                            new TierLoot(7, TierLoot.LootType.Armor, 0.05f),
                            new ItemLoot("Seal of the Divine", 0.015f),
                            new ItemLoot("White Drake Egg", 0.004f)
                        );

            db.Init("Shield Orc Shield",
                            
                                new Prioritize(
                                    new Orbit(1, 3, target: "Shield Orc Flooder"),
                                    new Wander(0.1f)
                                    ),
                                new State("Attacking",
                                    new State("Attack",
                                        new Flash(0xff000000, 10, 100),
                                        new Shoot(10, cooldown: 500),
                                        new HealthTransition(0.5f, "Heal"),
                                        new EntityNotExistsTransition("Shield Orc Key", 7, "Idling")
                                        ),
                                    new State("Heal",
                                        new HealGroup(7, "Shield Orcs", cooldown: 500),
                                        new TimedTransition(500, "Attack"),
                                        new EntityNotExistsTransition("Shield Orc Key", 7, "Idling")
                                        )
                                    ),
                                new State("Flash",
                                    new Flash(0xff0000, 1, 1),
                                    new TimedTransition(300, "Idling")
                                    ),
                                new State("Idling")
                                ,
                            new ItemLoot("Health Potion", 0.04f),
                            new ItemLoot("Magic Potion", 0.01f),
                            new TierLoot(2, TierLoot.LootType.Ring, 0.01f)
                        );

            db.Init("Shield Orc Flooder",
                            
                                new Prioritize(
                                    new Wander(0.1f)
                                    ),
                                new State("Attacking",
                                    new State("Attack",
                                        new Flash(0xff000000, 10, 100),
                                        new Shoot(10, cooldown: 500),
                                        new HealthTransition(0.5f, "Heal"),
                                        new EntityNotExistsTransition("Shield Orc Key", 7, "Idling")
                                        ),
                                    new State("Heal",
                                        new HealGroup(7, "Shield Orcs", cooldown: 500),
                                        new TimedTransition(500, "Attack"),
                                        new EntityNotExistsTransition("Shield Orc Key", 7, "Idling")
                                        )
                                    ),
                                new State("Flash",
                                    new Flash(0xff0000, 1, 1),
                                    new TimedTransition(300, "Idling")
                                    ),
                                new State("Idling")
                                ,
                            new ItemLoot("Health Potion", 0.04f),
                            new ItemLoot("Magic Potion", 0.01f),
                            new TierLoot(4, TierLoot.LootType.Ability, 0.01f)
                        );

            db.Init("Shield Orc Key",
                            
                                new Spawn("Shield Orc Flooder", 1, 1, 10000),
                                new Spawn("Shield Orc Shield", 1, 1, 10000),
                                new Spawn("Shield Orc Shield", 1, 1, 10000),
                                new State("Start",
                                    new TimedTransition(500, "Attacking")
                                    ),
                                new State("Attacking",
                                    new Orbit(1, 3, target: "Shield Orc Flooder"),
                                    new Order(7, "Shield Orc Flooder", "Attacking"),
                                    new Order(7, "Shield Orc Shield", "Attacking"),
                                    new HealthTransition(0.5f, "FlashBeforeExplode")
                                    ),
                                new State("FlashBeforeExplode",
                                    new Order(7, "Shield Orc Flooder", "Flash"),
                                    new Order(7, "Shield Orc Shield", "Flash"),
                                    new Flash(0xff0000, 1, 1),
                                    new TimedTransition(300, "Explode")
                                    ),
                                new State("Explode",
                                    new Shoot(0, 10, 36, fixedAngle: 0),
                                    new Decay(0)
                                    )
                                ,
                            new ItemLoot("Health Potion", 0.04f),
                            new ItemLoot("Magic Potion", 0.01f),
                            new TierLoot(4, TierLoot.LootType.Armor, 0.01f)
                        );

            db.Init("Left Horizontal Trap",
                            
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                new State("weak_effect",
                                    new Shoot(1, fixedAngle: 0, index: 0, cooldown: 200),
                                    new TimedTransition(2000, "blind_effect")
                                    ),
                                new State("blind_effect",
                                    new Shoot(1, fixedAngle: 0, index: 1, cooldown: 200),
                                    new TimedTransition(2000, "pierce_effect")
                                    ),
                                new State("pierce_effect",
                                    new Shoot(1, fixedAngle: 0, index: 2, cooldown: 200),
                                    new TimedTransition(2000, "weak_effect")
                                    ),
                                new Decay(6000)
                                
                        );

            db.Init("Top Vertical Trap",
                            
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                new State("weak_effect",
                                    new Shoot(1, fixedAngle: 90, index: 0, cooldown: 200),
                                    new TimedTransition(2000, "blind_effect")
                                    ),
                                new State("blind_effect",
                                    new Shoot(1, fixedAngle: 90, index: 1, cooldown: 200),
                                    new TimedTransition(2000, "pierce_effect")
                                    ),
                                new State("pierce_effect",
                                    new Shoot(1, fixedAngle: 90, index: 2, cooldown: 200),
                                    new TimedTransition(2000, "weak_effect")
                                    ),
                                new Decay(6000)
                                
                        );

            db.Init("45-225 Diagonal Trap",
                            
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                new State("weak_effect",
                                    new Shoot(1, fixedAngle: 45, index: 0, cooldown: 200),
                                    new TimedTransition(2000, "blind_effect")
                                    ),
                                new State("blind_effect",
                                    new Shoot(1, fixedAngle: 45, index: 1, cooldown: 200),
                                    new TimedTransition(2000, "pierce_effect")
                                    ),
                                new State("pierce_effect",
                                    new Shoot(1, fixedAngle: 45, index: 2, cooldown: 200),
                                    new TimedTransition(2000, "weak_effect")
                                    ),
                                new Decay(6000)
                                
                        );

            db.Init("135-315 Diagonal Trap",
                            
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                new State("weak_effect",
                                    new Shoot(1, fixedAngle: 135, index: 0, cooldown: 200),
                                    new TimedTransition(2000, "blind_effect")
                                    ),
                                new State("blind_effect",
                                    new Shoot(1, fixedAngle: 135, index: 1, cooldown: 200),
                                    new TimedTransition(2000, "pierce_effect")
                                    ),
                                new State("pierce_effect",
                                    new Shoot(1, fixedAngle: 135, index: 2, cooldown: 200),
                                    new TimedTransition(2000, "weak_effect")
                                    ),
                                new Decay(6000)
                                
                        );

            db.Init("Urgle",
                            
                                new DropPortalOnDeath("Spider Den Portal", 0.2f, timeout: 100),
                                new Prioritize(
                                    new StayCloseToSpawn(0.8f, 3),
                                    new Wander(0.5f)
                                    ),
                                new Shoot(8, predictive: 0.3f),
                                new State("idle",
                                    new PlayerWithinTransition(10.5f, "toss_horizontal_traps")
                                    ),
                                new State("toss_horizontal_traps",
                                    new TossObject("Left Horizontal Trap", 9, 230, 100000),
                                    new TossObject("Left Horizontal Trap", 10, 180, 100000),
                                    new TossObject("Left Horizontal Trap", 9, 140, 100000),
                                    new TimedTransition(1000, "toss_vertical_traps")
                                    ),
                                new State("toss_vertical_traps",
                                    new TossObject("Top Vertical Trap", 8, 200, 100000),
                                    new TossObject("Top Vertical Trap", 10, 240, 100000),
                                    new TossObject("Top Vertical Trap", 10, 280, 100000),
                                    new TossObject("Top Vertical Trap", 8, 320, 100000),
                                    new TimedTransition(1000, "toss_diagonal_traps")
                                    ),
                                new State("toss_diagonal_traps",
                                    new TossObject("45-225 Diagonal Trap", 2, 45, 100000),
                                    new TossObject("45-225 Diagonal Trap", 7, 45, 100000),
                                    new TossObject("45-225 Diagonal Trap", 11, 225, 100000),
                                    new TossObject("45-225 Diagonal Trap", 6, 225, 100000),
                                    new TossObject("135-315 Diagonal Trap", 2, 135, 100000),
                                    new TossObject("135-315 Diagonal Trap", 7, 135, 100000),
                                    new TossObject("135-315 Diagonal Trap", 11, 315, 100000),
                                    new TossObject("135-315 Diagonal Trap", 6, 315, 100000),
                                    new TimedTransition(1000, "wait")
                                    ),
                                new State("wait",
                                    new TimedTransition(2400, "idle")
                                    )
                                ,
                            new TierLoot(5, TierLoot.LootType.Weapon, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Weapon, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Weapon, 0.04f),
                            new TierLoot(5, TierLoot.LootType.Armor, 0.16f),
                            new TierLoot(6, TierLoot.LootType.Armor, 0.08f),
                            new TierLoot(7, TierLoot.LootType.Armor, 0.04f),
                            new TierLoot(3, TierLoot.LootType.Ring, 0.05f),
                            new TierLoot(3, TierLoot.LootType.Ability, 0.1f)
                        );
        }
    }
}
