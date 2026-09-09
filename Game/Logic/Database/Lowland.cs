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
    public class Lowland : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Hobbit Mage",
                new State("idle",
                    new PlayerWithinTransition(12, "ring1")
                ),
                new State("ring1",
                    new Shoot(1, fixedAngle: 0, count: 15, shootAngle: 24, cooldown: 1200, index: 0),
                    new TimedTransition(400, "ring2")
                ),
                new State("ring2",
                    new Shoot(1, fixedAngle: 8, count: 15, shootAngle: 24, cooldown: 1200, index: 1),
                    new TimedTransition(400, "ring3")
                ),
                new State("ring3",
                    new Shoot(1, fixedAngle: 16, count: 15, shootAngle: 24, cooldown: 1200, index: 2),
                    new TimedTransition(400, "idle")
                ),
                new Prioritize(
                    new StayAbove(0.4f, 9),
                    new Follow(0.75f, range: 6),
                    new Wander(0.4f)
                ),
                new Spawn("Hobbit Archer", maxChildren: 4, cooldown: 12000),
                new Spawn("Hobbit Rogue", maxChildren: 3, cooldown: 6000),
                new TierLoot(2, TierLoot.LootType.Weapon, 0.3f),
                new TierLoot(2, TierLoot.LootType.Armor, 0.3f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.11f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.39f),
                new ItemLoot("Health Potion", 0.02f),
                new ItemLoot("Magic Potion", 0.02f)
            );
            db.Init("Hobbit Archer",
                new Shoot(10),
                new State("run1",
                    new Prioritize(
                        new Protect(1.1f, "Hobbit Mage", acquireRange: 12, protectionRange: 10, reprotectRange: 1),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(400, "run2")
                ),
                new State("run2",
                    new Prioritize(
                        new StayBack(0.8f, 4),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(600, "run3")
                ),
                new State("run3",
                    new Prioritize(
                        new Protect(1, "Hobbit Archer", acquireRange: 16, protectionRange: 2, reprotectRange: 2),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(400, "run1")
                ),
                new ItemLoot("Health Potion", 0.04f)
            );
            db.Init("Hobbit Rogue",
                new Shoot(3),
                new Prioritize(
                    new Protect(1.2f, "Hobbit Mage", acquireRange: 15, protectionRange: 9, reprotectRange: 2.5f),
                    new Follow(0.85f, range: 1),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.04f)
            );
            db.Init("Undead Hobbit Mage",
                new Shoot(10, index: 3),
                new State("idle",
                    new PlayerWithinTransition(12, "ring1")
                ),
                new State("ring1",
                    new Shoot(1, fixedAngle: 0, count: 15, shootAngle: 24, cooldown: 1200, index: 0),
                    new TimedTransition(400, "ring2")
                ),
                new State("ring2",
                    new Shoot(1, fixedAngle: 8, count: 15, shootAngle: 24, cooldown: 1200, index: 1),
                    new TimedTransition(400, "ring3")
                ),
                new State("ring3",
                    new Shoot(1, fixedAngle: 16, count: 15, shootAngle: 24, cooldown: 1200, index: 2),
                    new TimedTransition(400, "idle")
                ),
                new Prioritize(
                    new StayAbove(0.4f, 20),
                    new Follow(0.75f, range: 6),
                    new Wander(0.4f)
                ),
                new Spawn("Undead Hobbit Archer", maxChildren: 4, cooldown: 12000),
                new Spawn("Undead Hobbit Rogue", maxChildren: 3, cooldown: 6000),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.3f),
                new TierLoot(3, TierLoot.LootType.Armor, 0.3f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.12f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.39f),
                new ItemLoot("Magic Potion", 0.03f)
            );
            db.Init("Undead Hobbit Archer",
                new Shoot(10),
                new State("run1",
                    new Prioritize(
                        new Protect(1.1f, "Undead Hobbit Mage", acquireRange: 12, protectionRange: 10,
                            reprotectRange: 1),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(400, "run2")
                ),
                new State("run2",
                    new Prioritize(
                        new StayBack(0.8f, 4),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(600, "run3")
                ),
                new State("run3",
                    new Prioritize(
                        new Protect(1, "Undead Hobbit Archer", acquireRange: 16, protectionRange: 2,
                            reprotectRange: 2),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(400, "run1")
                ),
                new ItemLoot("Magic Potion", 0.03f)
            );
            db.Init("Undead Hobbit Rogue",
                new Shoot(3),
                new Prioritize(
                    new Protect(1.2f, "Undead Hobbit Mage", acquireRange: 15, protectionRange: 9, reprotectRange: 2.5f),
                    new Follow(0.85f, range: 1),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.04f)
            );
            db.Init("Sumo Master",
                new State("sleeping1",
                    new SetAltTexture(0),
                    new TimedTransition(1000, "sleeping2"),
                    new HpLessTransition(0.99f, "hurt")
                ),
                new State("sleeping2",
                    new SetAltTexture(3),
                    new TimedTransition(1000, "sleeping1"),
                    new HpLessTransition(0.99f, "hurt")
                ),
                new State("hurt",
                    new SetAltTexture(2),
                    new Spawn("Lil Sumo", cooldown: 200),
                    new TimedTransition(1000, "awake")
                ),
                new State("awake",
                    new SetAltTexture(1),
                    new Shoot(3, cooldown: 250),
                    new Prioritize(
                        new Follow(0.05f, range: 1),
                        new Wander(0.05f)
                    ),
                    new HpLessTransition(0.5f, "rage")
                ),
                new State("rage",
                    new SetAltTexture(4),
                    new Taunt("Engaging Super-Mode!!!"),
                    new Prioritize(
                        new Follow(0.6f, range: 1),
                        new Wander(0.6f)
                    ),
                    new State("shoot",
                        new Shoot(8, index: 1, cooldown: 150),
                        new TimedTransition(700, "rest")
                    ),
                    new State("rest",
                        new TimedTransition(400, "shoot")
                    )
                ),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lil Sumo",
                new Shoot(8),
                new Prioritize(
                    new Orbit(0.4f, 2, target: "Sumo Master"),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.02f),
                new ItemLoot("Magic Potion", 0.02f)
            );
            db.Init("Elf Wizard",
                new State("idle",
                    new Wander(0.4f),
                    new PlayerWithinTransition(11, "move1")
                ),
                new State("move1",
                    new Shoot(10, count: 3, shootAngle: 14, predictive: 0.3f),
                    new Prioritize(
                        new StayAbove(0.4f, 14),
                        new BackAndForth(0.8f)
                    ),
                    new TimedTransition(2000, "move2")
                ),
                new State("move2",
                    new Shoot(10, count: 3, shootAngle: 10, predictive: 0.5f),
                    new Prioritize(
                        new StayAbove(0.4f, 14),
                        new Follow(0.6f, acquireRange: 10.5f, range: 3),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(2000, "move3")
                ),
                new State("move3",
                    new Prioritize(
                        new StayAbove(0.4f, 14),
                        new StayBack(0.6f, distance: 5),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(2000, "idle")
                ),
                new Spawn("Elf Archer", maxChildren: 2, cooldown: 15000),
                new Spawn("Elf Swordsman", maxChildren: 4, cooldown: 7000),
                new Spawn("Elf Mage", maxChildren: 1, cooldown: 8000),
                new TierLoot(2, TierLoot.LootType.Weapon, 0.36f),
                new TierLoot(2, TierLoot.LootType.Armor, 0.36f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.11f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.39f),
                new ItemLoot("Health Potion", 0.02f),
                new ItemLoot("Magic Potion", 0.02f)
            );
            db.Init("Elf Archer",
                new Shoot(10, predictive: 1),
                new Prioritize(
                    new Orbit(0.5f, 3, speedVariance: 0.1f, radiusVariance: 0.5f),
                    new Protect(1.2f, "Elf Wizard", acquireRange: 30, protectionRange: 10, reprotectRange: 1),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.04f)
            );
            db.Init("Elf Swordsman",
                new Shoot(10, predictive: 1),
                new Prioritize(
                    new Protect(1.2f, "Elf Wizard", acquireRange: 15, protectionRange: 10, reprotectRange: 5),
                    new Buzz(1, dist: 1),
                    new Orbit(0.6f, 3, speedVariance: 0.1f, radiusVariance: 0.5f),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.04f)
            );
            db.Init("Elf Mage",
                new Shoot(8, cooldown: 300),
                new Prioritize(
                    new Orbit(0.5f, 3),
                    new Protect(1.2f, "Elf Wizard", acquireRange: 30, protectionRange: 10, reprotectRange: 1),
                    new Wander(0.4f)
                ),
                new ItemLoot("Magic Potion", 0.03f)
            );
            db.Init("Goblin Rogue",
                new State("protect",
                    new Protect(0.8f, "Goblin Mage", acquireRange: 12, protectionRange: 1.5f, reprotectRange: 1.5f),
                    new TimedTransition(1200, "scatter", randomized: true)
                ),
                new State("scatter",
                    new Orbit(0.8f, 7, target: "Goblin Mage", radiusVariance: 1),
                    new TimedTransition(2400, "protect")
                ),
                new Shoot(3),
                new State("help",
                    new Protect(0.8f, "Goblin Mage", acquireRange: 12, protectionRange: 6, reprotectRange: 3),
                    new Follow(0.8f, acquireRange: 10.5f, range: 1.5f),
                    new EntityNotExistsTransition("Goblin Mage", 15, "protect")
                ),
                new ItemLoot("Health Potion", 0.04f)
            );
            db.Init("Goblin Warrior",
                new State("protect",
                    new Protect(0.8f, "Goblin Mage", acquireRange: 12, protectionRange: 1.5f, reprotectRange: 1.5f),
                    new TimedTransition(1200, "scatter", randomized: true)
                ),
                new State("scatter",
                    new Orbit(0.8f, 7, target: "Goblin Mage", radiusVariance: 1),
                    new TimedTransition(2400, "protect")
                ),
                new Shoot(3),
                new State("help",
                    new Protect(0.8f, "Goblin Mage", acquireRange: 12, protectionRange: 6, reprotectRange: 3),
                    new Follow(0.8f, acquireRange: 10.5f, range: 1.5f),
                    new EntityNotExistsTransition("Goblin Mage", 15, "protect")
                ),
                new DropPortalOnDeath("Pirate Cave Portal", 0.01f),
                new ItemLoot("Health Potion", 0.04f)
            );
            db.Init("Goblin Mage",
                new State("unharmed",
                    new Shoot(8, index: 0, predictive: 0.35f, cooldown: 1000),
                    new Shoot(8, index: 1, predictive: 0.35f, cooldown: 1300),
                    new Prioritize(
                        new StayAbove(0.4f, 16),
                        new Follow(0.5f, acquireRange: 10.5f, range: 4),
                        new Wander(0.4f)
                    ),
                    new HpLessTransition(0.65f, "activate_horde")
                ),
                new State("activate_horde",
                    new Shoot(8, index: 0, predictive: 0.25f, cooldown: 1000),
                    new Shoot(8, index: 1, predictive: 0.25f, cooldown: 1000),
                    new Flash(0xff484848, 0.6f, 5000),
                    new Order(12, "Goblin Rogue", "help"),
                    new Order(12, "Goblin Warrior", "help"),
                    new Prioritize(
                        new StayAbove(0.4f, 16),
                        new StayBack(0.5f, distance: 6)
                    )
                ),
                new Spawn("Goblin Rogue", maxChildren: 7, cooldown: 12000),
                new Spawn("Goblin Warrior", maxChildren: 7, cooldown: 12000),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.3f),
                new TierLoot(3, TierLoot.LootType.Armor, 0.3f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.09f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.38f),
                new ItemLoot("Health Potion", 0.02f),
                new ItemLoot("Magic Potion", 0.02f)
            );
            db.Init("Easily Enraged Bunny",
                new Prioritize(
                    new StayAbove(0.4f, 15),
                    new Follow(0.7f, acquireRange: 9.5f, range: 1)
                ),
                new TransformOnDeath("Enraged Bunny")
            );
            db.Init("Enraged Bunny",
                new Shoot(9, predictive: 0.5f, cooldown: 400),
                new State("red",
                    new Flash(0xff0000, 1.5f, 1),
                    new TimedTransition(1600, "yellow")
                ),
                new State("yellow",
                    new Flash(0xffff33, 1.5f, 1),
                    new TimedTransition(1600, "orange")
                ),
                new State("orange",
                    new Flash(0xff9900, 1.5f, 1),
                    new TimedTransition(1600, "red")
                ),
                new Prioritize(
                    new StayAbove(0.4f, 15),
                    new Follow(0.85f, acquireRange: 9, range: 2.5f),
                    new Wander(0.85f)
                ),
                new ItemLoot("Health Potion", 0.01f),
                new ItemLoot("Magic Potion", 0.02f)
            );
            db.Init("Forest Nymph",
                new State("circle",
                    new Shoot(4, index: 0, count: 1, predictive: 0.1f, cooldown: 900),
                    new Prioritize(
                        new StayAbove(0.4f, 25),
                        new Follow(0.9f, acquireRange: 11, range: 3.5f, duration: 1000, cooldown: 5000),
                        new Orbit(1.3f, 3.5f, acquireRange: 12),
                        new Wander(0.7f)
                    ),
                    new TimedTransition(4000, "dart_away")
                ),
                new State("dart_away",
                    new Shoot(9, index: 1, count: 6, fixedAngle: 20, shootAngle: 60, cooldown: 1400),
                    new Wander(0.4f),
                    new TimedTransition(3600, "circle")
                ),
                new DropPortalOnDeath("Pirate Cave Portal", 0.01f),
                new ItemLoot("Health Potion", 0.03f),
                new ItemLoot("Magic Potion", 0.02f)
            );
            db.Init("Sandsman King",
                new Shoot(10, cooldown: 10000),
                new Prioritize(
                    new StayAbove(0.4f, 15),
                    new Follow(0.6f, range: 4),
                    new Wander(0.4f)
                ),
                new Spawn("Sandsman Archer", maxChildren: 2, cooldown: 10000),
                new Spawn("Sandsman Sorcerer", maxChildren: 3, cooldown: 8000),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.3f),
                new TierLoot(3, TierLoot.LootType.Armor, 0.3f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.11f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.39f),
                new ItemLoot("Health Potion", 0.04f)
            );
            db.Init("Sandsman Sorcerer",
                new Shoot(10, index: 0, cooldown: 5000),
                new Shoot(5, index: 1, cooldown: 400),
                new Prioritize(
                    new Protect(1.2f, "Sandsman King", acquireRange: 15, protectionRange: 6, reprotectRange: 5),
                    new Wander(0.4f)
                ),
                new ItemLoot("Magic Potion", 0.03f)
            );
            db.Init("Sandsman Archer",
                new Shoot(10, predictive: 0.5f),
                new Prioritize(
                    new Orbit(0.8f, 3.25f, acquireRange: 15, target: "Sandsman King", radiusVariance: 0.5f),
                    new Wander(0.4f)
                ),
                new ItemLoot("Magic Potion", 0.03f)
            );
            db.Init("Giant Crab",
                new State("idle",
                    new Prioritize(
                        new StayAbove(0.6f, 13),
                        new Wander(0.6f)
                    ),
                    new PlayerWithinTransition(11, "scuttle")
                ),
                new State("scuttle",
                    new Shoot(9, index: 0, cooldown: 1000),
                    new Shoot(9, index: 1, cooldown: 1000),
                    new Shoot(9, index: 2, cooldown: 1000),
                    new Shoot(9, index: 3, cooldown: 1000),
                    new State("move",
                        new Prioritize(
                            new Follow(1, acquireRange: 10.6f, range: 2),
                            new StayAbove(1, 25),
                            new Wander(0.6f)
                        ),
                        new TimedTransition(400, "pause")
                    ),
                    new State("pause",
                        new TimedTransition(200, "move")
                    ),
                    new TimedTransition(4700, "tri-spit")
                ),
                new State("tri-spit",
                    new Shoot(9, index: 4, predictive: 0.5f, cooldownOffset: 1200, cooldown: 90000),
                    new Shoot(9, index: 4, predictive: 0.5f, cooldownOffset: 1800, cooldown: 90000),
                    new Shoot(9, index: 4, predictive: 0.5f, cooldownOffset: 2400, cooldown: 90000),
                    new State("move",
                        new Prioritize(
                            new Follow(1, acquireRange: 10.6f, range: 2),
                            new StayAbove(1, 25),
                            new Wander(0.6f)
                        ),
                        new TimedTransition(400, "pause")
                    ),
                    new State("pause",
                        new TimedTransition(200, "move")
                    ),
                    new TimedTransition(3200, "idle")
                ),
                new DropPortalOnDeath("Pirate Cave Portal", 0.01f),
                new TierLoot(2, TierLoot.LootType.Weapon, 0.14f),
                new TierLoot(2, TierLoot.LootType.Armor, 0.19f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.05f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.28f),
                new ItemLoot("Health Potion", 0.02f),
                new ItemLoot("Magic Potion", 0.02f)
            );
            db.Init("Sand Devil",
                new State("wander",
                    new Shoot(8, predictive: 0.3f, cooldown: 700),
                    new Prioritize(
                        new StayAbove(0.7f, 10),
                        new Follow(0.7f, acquireRange: 10, range: 2.2f),
                        new Wander(0.7f)
                    ),
                    new TimedTransition(3000, "circle")
                ),
                new State("circle",
                    new Shoot(8, predictive: 0.3f, cooldownOffset: 1000, cooldown: 1000),
                    new Orbit(0.7f, 2, acquireRange: 9),
                    new TimedTransition(3100, "wander")
                ),
                new DropPortalOnDeath("Pirate Cave Portal", 0.01f)
            );
        }
    }
}
