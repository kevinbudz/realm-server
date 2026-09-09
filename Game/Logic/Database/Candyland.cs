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
    public class Candyland : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Candy Gnome",
                new State("Ini",
                    new Wander(speed: 0.4f),
                    new PlayerWithinTransition(dist: 14, targetState: "Main", seeInvis: true)
                ),
                new State("Main",
                    new Follow(speed: 1.4f, acquireRange: 17, range: 6),
                    new TimedTransition(time: 1600, targetState: "Flee")
                ),
                new State("Flee",
                    new PlayerWithinTransition(dist: 11, targetState: "RunAwayMed", seeInvis: true),
                    new PlayerWithinTransition(dist: 8, targetState: "RunAwayMedFast", seeInvis: true),
                    new PlayerWithinTransition(dist: 5, targetState: "RunAwayFast", seeInvis: true),
                    new PlayerWithinTransition(dist: 16, targetState: "RunAwaySlow", seeInvis: true)
                ),
                new State("RunAwayFast",
                    new StayBack(speed: 1.9f, distance: 30, entity: null),
                    new TimedRandomTransition(1000, false, "RunAwayMedFast", "RunAwayMed", "RunAwaySlow")
                ),
                new State("RunAwayMedFast",
                    new StayBack(speed: 1.45f, distance: 30, entity: null),
                    new TimedRandomTransition(1000, false, "RunAwayMed", "RunAwaySlow")
                ),
                new State("RunAwayMed",
                    new StayBack(speed: 1.1f, distance: 30, entity: null),
                    new TimedRandomTransition(1000, false, "RunAwayMedFast", "RunAwaySlow")
                ),
                new State("RunAwaySlow",
                    new StayBack(speed: 0.8f, distance: 30, entity: null),
                    new TimedRandomTransition(1000, false, "RunAwayMedFast", "RunAwayMed")
                ),
                new DropPortalOnDeath(target: "Candyland Portal", probability: 0.5f, timeout: 30),
                new Threshold(0.01f,
                    new ItemLoot(item: "Rock Candy", chance: 0.15f),
                    new ItemLoot(item: "Red Gumball", chance: 0.15f),
                    new ItemLoot(item: "Purple Gumball", chance: 0.15f),
                    new ItemLoot(item: "Blue Gumball", chance: 0.15f),
                    new ItemLoot(item: "Green Gumball", chance: 0.15f),
                    new ItemLoot(item: "Yellow Gumball", chance: 0.15f)
                )
            );
            db.Init("MegaRototo",
                new Reproduce(children: "Tiny Rototo", densityRadius: 12, densityMax: 7, cooldown: 7000),
                new State("Follow",
                    new Shoot(range: 0, count: 4, shootAngle: 90, defaultAngle: 45, cooldown: 1400),
                    new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, cooldown: 1400),
                    new Follow(speed: 0.45f, acquireRange: 11, range: 5),
                    new TimedRandomTransition(4300, false, "FlameThrower", "StayBack")
                ),
                new State("StayBack",
                    new Shoot(range: 0, count: 3, shootAngle: 16, index: 1, predictive: 0.6f, cooldown: 1200),
                    new Shoot(range: 0, count: 3, shootAngle: 16, index: 0, predictive: 0.9f, cooldown: 600),
                    new StayBack(speed: 0.5f, distance: 10, entity: null),
                    new TimedTransition(time: 2400, targetState: "Follow")
                ),
                new State("FlameThrower",
                    new State("FB1ORFB2",
                        new TimedRandomTransition(0, false, "FB1", "FB2")
                    ),
                    new State("FB1",
                        new Shoot(range: 12, count: 2, shootAngle: 16, index: 2, cooldown: 400),
                        new Shoot(range: 12, count: 1, index: 3, cooldown: 200)
                    ),
                    new State("FB2",
                        new Shoot(range: 12, count: 2, shootAngle: 16, index: 3, cooldown: 400),
                        new Shoot(range: 12, count: 1, index: 2, cooldown: 200)
                    ),
                    new TimedTransition(time: 4000, targetState: "Follow")
                ),
                new Threshold(0.01f,
                    new ItemLoot(item: "Ring Pop", chance: 0.02f),
                    new ItemLoot(item: "Wine Cellar Incantation", chance: 0.02f),
                    new ItemLoot(item: "Rock Candy", chance: 0.08f),
                    new ItemLoot(item: "Candy-Coated Armor", chance: 0.01f),
                    new ItemLoot(item: "Red Gumball", chance: 0.15f),
                    new ItemLoot(item: "Purple Gumball", chance: 0.15f),
                    new ItemLoot(item: "Blue Gumball", chance: 0.15f),
                    new ItemLoot(item: "Green Gumball", chance: 0.15f),
                    new ItemLoot(item: "Yellow Gumball", chance: 0.15f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Weapon, chance: 0.04f),
                    new TierLoot(tier: 6, type: TierLoot.LootType.Weapon, chance: 0.06f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Armor, chance: 0.04f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Armor, chance: 0.06f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ability, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ring, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f)
                )
            );
            db.Init("Spoiled Creampuff",
                new Spawn(children: "Big Creampuff", maxChildren: 2, initialSpawn: 0),
                new Reproduce(children: "Big Creampuff", densityRadius: 24, densityMax: 2, cooldown: 25000),
                new Shoot(range: 10, count: 1, index: 0, predictive: 1, cooldown: 1400),
                new Shoot(range: 4.4f, count: 5, shootAngle: 12, index: 1, predictive: 0.6f, cooldown: 800),
                new Prioritize(
                    new Charge(speed: 1.4f, range: 11, cooldown: 4200),
                    new StayBack(speed: 1, distance: 4, entity: null),
                    new StayBack(speed: 0.5f, distance: 7, entity: null)
                ),
                new StayCloseToSpawn(speed: 1.35f, range: 13),
                new Wander(speed: 0.4f),
                new Threshold(0.01f,
                    new ItemLoot(item: "Potion of Attack", chance: 0.03f),
                    new ItemLoot(item: "Potion of Defense", chance: 0.03f),
                    new ItemLoot(item: "Ring Pop", chance: 0.015f),
                    new ItemLoot(item: "Wine Cellar Incantation", chance: 0.02f),
                    new ItemLoot(item: "Rock Candy", chance: 0.08f),
                    new ItemLoot(item: "Candy-Coated Armor", chance: 0.01f),
                    new ItemLoot(item: "Red Gumball", chance: 0.15f),
                    new ItemLoot(item: "Purple Gumball", chance: 0.15f),
                    new ItemLoot(item: "Blue Gumball", chance: 0.15f),
                    new ItemLoot(item: "Green Gumball", chance: 0.15f),
                    new ItemLoot(item: "Yellow Gumball", chance: 0.15f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Weapon, chance: 0.04f),
                    new TierLoot(tier: 6, type: TierLoot.LootType.Weapon, chance: 0.06f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Armor, chance: 0.04f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Armor, chance: 0.06f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ability, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ring, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f)
                )
            );
            db.Init("Desire Troll",
                new State("BaseAttack",
                    new Shoot(range: 10, count: 3, shootAngle: 15, index: 0, predictive: 1, cooldown: 1400),
                    new Grenade(radius: 5, damage: 200, range: 8, cooldown: 3000),
                    new Shoot(range: 10, count: 1, index: 1, predictive: 1, cooldown: 2000),
                    new State("Choose",
                        new TimedRandomTransition(3800, false, "Run", "Attack")
                    ),
                    new State("Run",
                        new StayBack(speed: 1.1f, distance: 10, entity: null),
                        new TimedTransition(time: 1200, targetState: "Choose")
                    ),
                    new State("Attack",
                        new Charge(speed: 1.2f, range: 11, cooldown: 1000),
                        new TimedTransition(time: 1000, targetState: "Choose")
                    ),
                    new HpLessTransition(threshold: 0.6f, targetState: "NextAttack")
                ),
                new State("NextAttack",
                    new Shoot(range: 10, count: 5, shootAngle: 10, index: 2, predictive: 0.5f, angleOffset: 0.4f, cooldown: 2000),
                    new Shoot(range: 10, count: 1, index: 1, predictive: 1, cooldown: 2000),
                    new Shoot(range: 10, count: 3, shootAngle: 15, index: 0, predictive: 1, angleOffset: 1, cooldown: 4000),
                    new Grenade(radius: 5, damage: 200, range: 8, cooldown: 3000),
                    new State("Choose2",
                        new TimedRandomTransition(3800, false, "Run2", "Attack2")
                    ),
                    new State("Run2",
                        new StayBack(speed: 1.5f, distance: 10, entity: null),
                        new TimedTransition(time: 1500, targetState: "Choose2"),
                        new PlayerWithinTransition(dist: 3.5f, targetState: "Boom", seeInvis: false)
                    ),
                    new State("Attack2",
                        new Charge(speed: 1.2f, range: 11, cooldown: 1000),
                        new TimedTransition(time: 1000, targetState: "Choose2"),
                        new PlayerWithinTransition(dist: 3.5f, targetState: "Boom", seeInvis: false)
                    ),
                    new State("Boom",
                        new Shoot(range: 0, count: 20, shootAngle: 18, index: 3, cooldown: 2000),
                        new TimedTransition(time: 200, targetState: "Choose2")
                    )
                ),
                new StayCloseToSpawn(speed: 1.5f, range: 15),
                new Prioritize(
                    new Follow(speed: 1, acquireRange: 11, range: 5)
                ),
                new Wander(speed: 0.4f),
                new Threshold(0.01f,
                    new ItemLoot(item: "Potion of Attack", chance: 0.05f),
                    new ItemLoot(item: "Potion of Wisdom", chance: 0.05f),
                    new ItemLoot(item: "Ring Pop", chance: 0.015f),
                    new ItemLoot(item: "Wine Cellar Incantation", chance: 0.02f),
                    new ItemLoot(item: "Rock Candy", chance: 0.08f),
                    new ItemLoot(item: "Candy-Coated Armor", chance: 0.01f),
                    new ItemLoot(item: "Red Gumball", chance: 0.15f),
                    new ItemLoot(item: "Purple Gumball", chance: 0.15f),
                    new ItemLoot(item: "Blue Gumball", chance: 0.15f),
                    new ItemLoot(item: "Green Gumball", chance: 0.15f),
                    new ItemLoot(item: "Yellow Gumball", chance: 0.15f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Weapon, chance: 0.04f),
                    new TierLoot(tier: 6, type: TierLoot.LootType.Weapon, chance: 0.06f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Armor, chance: 0.04f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Armor, chance: 0.06f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ability, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ring, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f)
                )
            );
            db.Init("Swoll Fairy",
                new Spawn(children: "Fairy", maxChildren: 6, initialSpawn: 0, cooldown: 10000),
                new StayCloseToSpawn(speed: 0.6f, range: 13),
                new Prioritize(
                    new Follow(speed: 0.3f, acquireRange: 10, range: 5)
                ),
                new State("Shoot",
                    new Shoot(range: 11, count: 2, shootAngle: 30, index: 0, predictive: 1, cooldown: 600),
                    new TimedTransition(time: 3000, targetState: "Pause")
                ),
                new State("ShootB",
                    new Shoot(range: 11, count: 8, shootAngle: 45, index: 1, cooldown: 1000),
                    new TimedTransition(time: 3000, targetState: "Pause")
                ),
                new State("Pause",
                    new TimedRandomTransition(3000, false, "Shoot", "ShootB")
                ),
                new Threshold(0.01f,
                    new ItemLoot(item: "Potion of Defense", chance: 0.05f),
                    new ItemLoot(item: "Potion of Wisdom", chance: 0.05f),
                    new ItemLoot(item: "Ring Pop", chance: 0.015f),
                    new ItemLoot(item: "Wine Cellar Incantation", chance: 0.02f),
                    new ItemLoot(item: "Rock Candy", chance: 0.08f),
                    new ItemLoot(item: "Candy-Coated Armor", chance: 0.01f),
                    new ItemLoot(item: "Red Gumball", chance: 0.15f),
                    new ItemLoot(item: "Purple Gumball", chance: 0.15f),
                    new ItemLoot(item: "Blue Gumball", chance: 0.15f),
                    new ItemLoot(item: "Green Gumball", chance: 0.15f),
                    new ItemLoot(item: "Yellow Gumball", chance: 0.15f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Weapon, chance: 0.04f),
                    new TierLoot(tier: 6, type: TierLoot.LootType.Weapon, chance: 0.06f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Armor, chance: 0.04f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Armor, chance: 0.06f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ability, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ring, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f)
                )
            );
            db.Init("Gigacorn",
                new StayCloseToSpawn(speed: 1, range: 13),
                new Prioritize(
                    new Charge(speed: 1.4f, range: 11, cooldown: 3800),
                    new StayBack(speed: 0.8f, distance: 6, entity: null)
                ),
                new State("Start",
                    new State("Shoot",
                        new Shoot(range: 10, count: 1, index: 0, predictive: 1, cooldown: 200),
                        new TimedTransition(time: 2850, targetState: "ShootPause")
                    ),
                    new State("ShootPause",
                        new Shoot(range: 4.5f, count: 3, shootAngle: 10, index: 1, predictive: 0.4f, cooldown: 3000, cooldownOffset: 500),
                        new Shoot(range: 4.5f, count: 3, shootAngle: 10, index: 1, predictive: 0.4f, cooldown: 3000, cooldownOffset: 1000),
                        new Shoot(range: 4.5f, count: 3, shootAngle: 10, index: 1, predictive: 0.4f, cooldown: 3000, cooldownOffset: 1500),
                        new TimedTransition(time: 5700, targetState: "Shoot")
                    )
                ),
                new Threshold(0.01f,
                    new ItemLoot(item: "Potion of Attack", chance: 0.05f),
                    new ItemLoot(item: "Potion of Wisdom", chance: 0.05f),
                    new ItemLoot(item: "Ring Pop", chance: 0.015f),
                    new ItemLoot(item: "Wine Cellar Incantation", chance: 0.02f),
                    new ItemLoot(item: "Rock Candy", chance: 0.08f),
                    new ItemLoot(item: "Candy-Coated Armor", chance: 0.01f),
                    new ItemLoot(item: "Red Gumball", chance: 0.15f),
                    new ItemLoot(item: "Purple Gumball", chance: 0.15f),
                    new ItemLoot(item: "Blue Gumball", chance: 0.15f),
                    new ItemLoot(item: "Green Gumball", chance: 0.15f),
                    new ItemLoot(item: "Yellow Gumball", chance: 0.15f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Weapon, chance: 0.04f),
                    new TierLoot(tier: 6, type: TierLoot.LootType.Weapon, chance: 0.06f),
                    new TierLoot(tier: 8, type: TierLoot.LootType.Armor, chance: 0.04f),
                    new TierLoot(tier: 7, type: TierLoot.LootType.Armor, chance: 0.06f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ability, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f),
                    new TierLoot(tier: 3, type: TierLoot.LootType.Ring, chance: 0.05f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.03f)
                )
            );
            db.Init("Candyland Boss Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("Ini",
                    new NoPlayerWithinTransition(dist: 16, targetState: "Ini2")
                ),
                new State("Ini2",
                    new TimedRandomTransition(0, false, "Creampuff", "Unicorn", "Troll", "Rototo", "Fairy", "Gumball Machine")
                ),
                new State("Ini3",
                    new EntitiesNotExistsTransition(16, "Ini", "Spoiled Creampuff", "Gigacorn", "Desire Troll", "MegaRototo", "Swoll Fairy", "Gumball Machine")
                ),
                new State("Creampuff",
                    new Spawn(children: "Spoiled Creampuff", maxChildren: 1, initialSpawn: 0),
                    new TimedTransition(time: 3000, targetState: "Ini3")
                ),
                new State("Unicorn",
                    new Spawn(children: "Gigacorn", maxChildren: 1, initialSpawn: 0),
                    new TimedTransition(time: 3000, targetState: "Ini3")
                ),
                new State("Troll",
                    new Spawn(children: "Desire Troll", maxChildren: 1, initialSpawn: 0),
                    new TimedTransition(time: 3000, targetState: "Ini3")
                ),
                new State("Rototo",
                    new Spawn(children: "MegaRototo", maxChildren: 1, initialSpawn: 0),
                    new TimedTransition(time: 3000, targetState: "Ini3")
                ),
                new State("Fairy",
                    new Spawn(children: "Swoll Fairy", maxChildren: 1, initialSpawn: 0),
                    new TimedTransition(time: 3000, targetState: "Ini3")
                ),
                new State("Gumball Machine",
                    new Spawn(children: "Gumball Machine", maxChildren: 1, initialSpawn: 0),
                    new TimedTransition(time: 3000, targetState: "Ini3")
                )
            );
            db.Init("Gumball Machine",
                new Threshold(0.01f,
                    new ItemLoot(item: "Ring Pop", chance: 0.015f),
                    new ItemLoot(item: "Rock Candy", chance: 0.15f),
                    new ItemLoot(item: "Red Gumball", chance: 0.15f),
                    new ItemLoot(item: "Purple Gumball", chance: 0.15f),
                    new ItemLoot(item: "Blue Gumball", chance: 0.15f),
                    new ItemLoot(item: "Green Gumball", chance: 0.15f),
                    new ItemLoot(item: "Yellow Gumball", chance: 0.15f)
                )
            );
            db.Init("Fairy",
                new StayCloseToSpawn(speed: 1, range: 13),
                new Prioritize(
                    new Protect(speed: 1.2f, protectee: "Beefy Fairy", acquireRange: 15, protectionRange: 8, reprotectRange: 6),
                    new Orbit(speed: 1.2f, radius: 4, acquireRange: 7)
                ),
                new Wander(speed: 0.6f),
                new Shoot(range: 10, count: 2, shootAngle: 30, index: 0, predictive: 1, cooldown: 2000),
                new Shoot(range: 10, count: 1, index: 0, predictive: 1, cooldown: 2000, cooldownOffset: 1000)
            );
            db.Init("Big Creampuff",
                new Spawn(children: "Small Creampuff", maxChildren: 4, initialSpawn: 0),
                new Shoot(range: 10, count: 1, index: 0, predictive: 1, cooldown: 1400),
                new Shoot(range: 4.4f, count: 5, shootAngle: 12, index: 1, predictive: 0.6f, cooldown: 800),
                new Prioritize(
                    new Charge(speed: 1.4f, range: 11, cooldown: 4200),
                    new StayBack(speed: 1, distance: 4, entity: null),
                    new StayBack(speed: 0.5f, distance: 7, entity: null)
                ),
                new StayCloseToSpawn(speed: 1.35f, range: 13),
                new Wander(speed: 0.4f)
            );
            db.Init("Small Creampuff",
                new Shoot(range: 5, count: 3, shootAngle: 12, index: 1, predictive: 0.6f, cooldown: 1000),
                new StayCloseToSpawn(speed: 1.3f, range: 13),
                new Prioritize(
                    new Charge(speed: 1.3f, range: 13, cooldown: 2500),
                    new Protect(speed: 0.8f, protectee: "Big Creampuff", acquireRange: 15, protectionRange: 7, reprotectRange: 6)
                ),
                new Wander(speed: 0.6f)
            );
            db.Init("Tiny Rototo",
                new Prioritize(
                    new Orbit(speed: 1.2f, radius: 4, acquireRange: 10, target: "MegaRototo"),
                    new Protect(speed: 0.8f, protectee: "Rototo", acquireRange: 15, protectionRange: 7, reprotectRange: 6)
                ),
                new State("Main",
                    new State("Unaware",
                        new Prioritize(
                            new Orbit(speed: 0.4f, radius: 2.6f, acquireRange: 8, target: "Rototo", speedVariance: 0.2f, radiusVariance: 0.2f, orbitClockwise: true),
                            new Wander(speed: 0.35f)
                        ),
                        new PlayerWithinTransition(dist: 3.4f, targetState: "Attack"),
                        new HpLessTransition(threshold: 0.999f, targetState: "Attack")
                    ),
                    new State("Attack",
                        new Shoot(range: 0, count: 4, shootAngle: 90, index: 1, defaultAngle: 45, cooldown: 1400),
                        new Shoot(range: 0, count: 4, shootAngle: 90, index: 0, cooldown: 1400),
                        new Prioritize(
                            new Follow(speed: 0.8f, acquireRange: 8, range: 3, duration: 3000, cooldown: 2000),
                            new Charge(speed: 1.35f, range: 11, cooldown: 1000),
                            new Wander(speed: 0.35f)
                        )
                    )
                )
            );
            db.Init("Butterfly",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new StayCloseToSpawn(speed: 0.3f, range: 6),
                new State("Moving",
                    new Wander(speed: 0.25f),
                    new PlayerWithinTransition(dist: 6, targetState: "Follow")
                ),
                new State("Follow",
                    new Prioritize(
                        new StayBack(speed: 0.23f, distance: 1.2f, entity: null),
                        new Orbit(speed: 0.2f, radius: 1.6f, acquireRange: 3)
                    ),
                    new Follow(speed: 0.2f, acquireRange: 7, range: 3),
                    new Wander(speed: 0.2f),
                    new NoPlayerWithinTransition(dist: 4, targetState: "Moving")
                )
            );
        }
    }
}
