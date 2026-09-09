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
    public class Midland : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Fire Sprite",
                new Reproduce(densityMax: 2),
                new Shoot(10, count: 2, shootAngle: 7, cooldown: 300),
                new Prioritize(
                        new StayAbove(1.4, 55),
                        new Wander(1.4f)
                        ),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.02f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Ice Sprite",
                new Reproduce(densityMax: 2),
                new Shoot(10, count: 3, shootAngle: 7),
                new Prioritize(
                        new StayAbove(1.4, 60),
                        new Wander(1.4f)
                        ),
                new TierLoot(2, TierLoot.LootType.Ability, 0.04f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Magic Sprite",
                new Reproduce(densityMax: 2),
                new Shoot(10, count: 4, shootAngle: 7),
                new Prioritize(
                        new StayAbove(1.4, 60),
                        new Wander(1.4f)
                        ),
                new TierLoot(6, TierLoot.LootType.Armor, 0.01f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Orc King",
                new DropPortalOnDeath("Spider Den Portal", 0.1),
                new Shoot(3),
                new Spawn("Orc Queen", maxChildren: 2, cooldown: 60000),
                new Prioritize(
                        new StayAbove(1.4, 60),
                        new Follow(0.6, range: 1, duration: 3000, cooldown: 3000),
                        new Wander(0.6f)
                        ),
                new TierLoot(4, TierLoot.LootType.Weapon, 0.18f),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.21f),
                new TierLoot(6, TierLoot.LootType.Armor, 0.035f),
                new ItemLoot("Magic Potion", 0.03f),
                new TierLoot(2, TierLoot.LootType.Ring, 0.07f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.17f));
            db.Init("Orc Queen",
                new Spawn("Orc Mage", maxChildren: 2, cooldown: 8000),
                new Spawn("Orc Warrior", maxChildren: 3, cooldown: 8000),
                new Prioritize(
                        new StayAbove(1.4, 60),
                        new Protect(0.8, "Orc King", acquireRange: 11, protectionRange: 7, reprotectRange: 5.4),
                        new Wander(0.8f)
                        ),
                new HealGroup(10, "OrcKings",cooldown:  300),
                new ItemLoot("Health Potion", 0.03f));
            db.Init("Orc Mage",
                new State("circle_player",
                        new Shoot(8, predictive: 0.3f, cooldown: 1000, cooldownOffset: 500),
                        new Prioritize(
                            new StayAbove(1.4, 60),
                            new Protect(0.7, "Orc Queen", acquireRange: 11, protectionRange: 10, reprotectRange: 3),
                            new Orbit(0.7, 3.5, acquireRange: 11)
                            ),
                        new TimedTransition("circle_queen", 3500)
                        ),
                new State("circle_queen",
                        new Shoot(8, count: 3, predictive: 0.3f, shootAngle: 120, cooldown: 1000, cooldownOffset: 500),
                        new Prioritize(
                            new StayAbove(1.4, 60),
                            new Orbit(1.2, 2.5, target: "Orc Queen", acquireRange: 12, speedVariance: 0.1,
                                radiusVariance: 0.1)
                            ),
                        new TimedTransition("circle_player", 3500)
                        ),
                new ItemLoot("Magic Potion", 0.03f));
            db.Init("Orc Warrior",
                new Shoot(3, predictive: 1, cooldown: 500),
                new Prioritize(
                        new StayAbove(1.4, 60),
                        new Orbit(1.35, 2.5, target: "Orc Queen", acquireRange: 12, speedVariance: 0.1,
                            radiusVariance: 0.1)
                        ),
                new ItemLoot("Health Potion", 0.03f));
            db.Init("Pink Blob",
                new StayAbove(0.4, 50),
                new Shoot(6, count: 3, shootAngle: 7),
                new Prioritize(
                        new Follow(0.8, acquireRange: 15, range: 5),
                        new Wander(0.4f)
                        ),
                new Reproduce(densityMax: 5, densityRadius: 10),
                new ItemLoot("Health Potion", 0.03f),
                new ItemLoot("Magic Potion", 0.03f));
            db.Init("Gray Blob",
                new State("searching",
                        new StayAbove(0.2, 50),
                        new Prioritize(
                            new Charge(2),
                            new Wander(0.4f)
                            ),
                        new Reproduce(densityMax: 5, densityRadius: 10),
                        new PlayerWithinTransition(2, "creeping")
                        ),
                new State("creeping",
                        new Shoot(0, count: 10, shootAngle: 36, fixedAngle: 0),
                        new Decay(0)
                        ),
                new ItemLoot("Health Potion", 0.03f),
                new ItemLoot("Magic Potion", 0.03f),
                new ItemLoot("Magic Mushroom", 0.005f));
            db.Init("Big Green Slime",
                new StayAbove(0.4, 50),
                new Shoot(9),
                new Wander(0.4f),
                new Reproduce(densityMax: 5, densityRadius: 10),
                new TransformOnDeath("Little Green Slime"),
                new TransformOnDeath("Little Green Slime"),
                new TransformOnDeath("Little Green Slime"),
                new TransformOnDeath("Little Green Slime"));
            db.Init("Little Green Slime",
                new StayAbove(0.4, 50),
                new Shoot(6),
                new Wander(0.4f),
                new Protect(0.4, "Big Green Slime"),
                new ItemLoot("Health Potion", 0.03f),
                new ItemLoot("Magic Potion", 0.03f));
            db.Init("Wasp Queen",
                new Spawn("Worker Wasp", maxChildren: 5, cooldown: 3400),
                new Spawn("Warrior Wasp", maxChildren: 2, cooldown: 4400),
                new State("idle",
                        new StayAbove(0.4, 60),
                        new Wander(0.55f),
                        new PlayerWithinTransition(10, "froth")
                        ),
                new State("froth",
                        new Shoot(8, predictive: 0.1f, cooldown: 1600),
                        new Prioritize(
                            new StayAbove(0.4, 60),
                            new Wander(0.55f)
                            )
                        ),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.14f),
                new TierLoot(6, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.19f),
                new TierLoot(6, TierLoot.LootType.Armor, 0.02f),
                new TierLoot(2, TierLoot.LootType.Ring, 0.07f),
                new TierLoot(3, TierLoot.LootType.Ring, 0.001f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.28f),
                new TierLoot(3, TierLoot.LootType.Ability, 0.01f),
                new ItemLoot("Health Potion", 0.15f),
                new ItemLoot("Magic Potion", 0.07f));
            db.Init("Worker Wasp",
                new Shoot(8, cooldown: 4000),
                new Prioritize(
                        new Orbit(1, 2, target: "Wasp Queen", radiusVariance: 0.5),
                        new Wander(0.75f)
                        ));
            db.Init("Warrior Wasp",
                new Shoot(8, predictive: 200, cooldown: 1000),
                new State("protecting",
                        new Prioritize(
                            new Orbit(1, 2, target: "Wasp Queen", radiusVariance: 0),
                            new Wander(0.75f)
                            ),
                        new TimedTransition("attacking", 3000)
                        ),
                new State("attacking",
                        new Prioritize(
                            new Follow(0.8, acquireRange: 9, range: 3.4),
                            new Orbit(1, 2, target: "Wasp Queen", radiusVariance: 0),
                            new Wander(0.75f)
                            ),
                        new TimedTransition("protecting", 2200)
                        ));
            db.Init("Shambling Sludge",
                new State("idle",
                        new StayAbove(0.5, 55),
                        new PlayerWithinTransition(10, "toss_sludge")
                        ),
                new State("toss_sludge",
                        new Prioritize(
                            new StayAbove(0.5, 55),
                            new Wander(0.5f)
                            ),
                        new Shoot(8, cooldown: 1200),
                        new TossObject("Sludget", range: 3, angle: 20, cooldown: 100000),
                        new TossObject("Sludget", range: 3, angle: 92, cooldown: 100000),
                        new TossObject("Sludget", range: 3, angle: 164, cooldown: 100000),
                        new TossObject("Sludget", range: 3, angle: 236, cooldown: 100000),
                        new TossObject("Sludget", range: 3, angle: 308, cooldown: 100000),
                        new TimedTransition("pause", 8000)
                        ),
                new State("pause",
                        new Prioritize(
                            new StayAbove(0.5, 55),
                            new Wander(0.5f)
                            ),
                        new TimedTransition("idle", 1000)
                        ),
                new TierLoot(4, TierLoot.LootType.Weapon, 0.14f),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.20f),
                new TierLoot(6, TierLoot.LootType.Armor, 0.02f),
                new TierLoot(2, TierLoot.LootType.Ring, 0.07f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.27f),
                new ItemLoot("Health Potion", 0.15f),
                new ItemLoot("Magic Potion", 0.10f));
            db.Init("Sludget",
                new Decay(9000),
                new State("idle",
                        new Shoot(8, predictive: 0.5f, cooldown: 600),
                        new Prioritize(
                            new Protect(0.5, "Shambling Sludge", 11, 7.5, 7.4),
                            new Wander(0.5f)
                            ),
                        new TimedTransition("wander", 1400)
                        ),
                new State("wander",
                        new Prioritize(
                            new Protect(0.5, "Shambling Sludge", 11, 7.5, 7.4),
                            new Wander(0.5f)
                            ),
                        new TimedTransition("jump", 5400)
                        ),
                new State("jump",
                        new Prioritize(
                            new Protect(0.5, "Shambling Sludge", 11, 7.5, 7.4),
                            new Follow(7, acquireRange: 6, range: 1),
                            new Wander(0.5f)
                            ),
                        new TimedTransition("attack", 200)
                        ),
                new State("attack",
                        new Shoot(8, predictive: 0.5f, cooldown: 600, cooldownOffset: 300),
                        new Prioritize(
                            new Protect(0.5, "Shambling Sludge", 11, 7.5, 7.4),
                            new Follow(0.5, acquireRange: 6, range: 1),
                            new Wander(0.5f)
                            ),
                        new TimedTransition("idle", 4000)
                        ));
            db.Init("Swarm",
                new Reproduce(densityMax: 1, densityRadius: 100),
                new State("circle",
                        new Prioritize(
                            new StayAbove(0.4, 60),
                            new Follow(4, acquireRange: 11, range: 3.5, duration: 1000, cooldown: 5000),
                            new Orbit(1.9, 3.5, acquireRange: 12),
                            new Wander(0.4f)
                            ),
                        new Shoot(4, predictive: 0.1f, cooldown: 500),
                        new TimedTransition("dart_away", 3000)
                        ),
                new State("dart_away",
                        new Prioritize(
                            new StayAbove(0.4, 60),
                            new StayBack(2, distance: 5),
                            new Wander(0.4f)
                            ),
                        new Shoot(8, count: 5, shootAngle: 72, fixedAngle: 20, cooldown: 100000, cooldownOffset: 800),
                        new Shoot(8, count: 5, shootAngle: 72, fixedAngle: 56, cooldown: 100000, cooldownOffset: 1400),
                        new TimedTransition("circle", 1600)
                        ),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.22f),
                new TierLoot(4, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(3, TierLoot.LootType.Armor, 0.22f),
                new TierLoot(4, TierLoot.LootType.Armor, 0.12f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.02f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.1f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.21f),
                new ItemLoot("Health Potion", 0.24f),
                new ItemLoot("Magic Potion", 0.07f));
            db.Init("Black Bat",
                new Prioritize(
                        new Charge(),
                        new Wander(0.4f)
                        ),
                new Shoot(1),
                new Reproduce(densityMax: 5, densityRadius: 20, cooldown: 20000),
                new ItemLoot("Health Potion", 0.01f),
                new ItemLoot("Magic Potion", 0.01f),
                new TierLoot(2, TierLoot.LootType.Armor, 0.01f));
            db.Init("Red Spider",
                new Wander(0.8f),
                new Shoot(9),
                new Reproduce(densityMax: 3, densityRadius: 15, cooldown: 45000),
                new ItemLoot("Health Potion", 0.03f),
                new ItemLoot("Magic Potion", 0.03f));
            db.Init("Dwarf Axebearer",
                new Shoot(3.4f),
                new State("Default",
                        new Wander(0.4f)
                        ),
                new State("Circling",
                        new Prioritize(
                            new Orbit(0.4, 2.7, acquireRange: 11),
                            new Protect(1.2, "Dwarf King", acquireRange: 15, protectionRange: 6, reprotectRange: 3),
                            new Wander(0.4f)
                            ),
                        new TimedTransition("Default", 3300),
                        new EntityNotExistsTransition("Dwarf King", 8, "Default")
                        ),
                new State("Engaging",
                        new Prioritize(
                            new Follow(1.0, acquireRange: 15, range: 1),
                            new Protect(1.2, "Dwarf King", acquireRange: 15, protectionRange: 6, reprotectRange: 3),
                            new Wander(0.4f)
                            ),
                        new TimedTransition("Circling", 2500),
                        new EntityNotExistsTransition("Dwarf King", 8, "Default")
                        ),
                new ItemLoot("Health Potion", 0.04f));
            db.Init("Dwarf Mage",
                new State("Default",
                        new Prioritize(
                            new Protect(1.2, "Dwarf King", acquireRange: 15, protectionRange: 6, reprotectRange: 3),
                            new Wander(0.6f)
                            ),
                        new State("fire1_def",
                            new Shoot(10, predictive: 0.2f, cooldown: 100000),
                            new TimedTransition("fire2_def", 1500)
                            ),
                        new State("fire2_def",
                            new Shoot(5, predictive: 0.2f, cooldown: 100000),
                            new TimedTransition("fire1_def", 1500)
                            )
                        ),
                new State("Circling",
                        new Prioritize(
                            new Orbit(0.4, 2.7, acquireRange: 11),
                            new Protect(1.2, "Dwarf King", acquireRange: 15, protectionRange: 6, reprotectRange: 3),
                            new Wander(0.6f)
                            ),
                        new State("fire1_cir",
                            new Shoot(10, predictive: 0.2f, cooldown: 100000),
                            new TimedTransition("fire2_cir", 1500)
                            ),
                        new State("fire2_cir",
                            new Shoot(5, predictive: 0.2f, cooldown: 100000),
                            new TimedTransition("fire1_cir", 1500)
                            ),
                        new TimedTransition("Default", 3300),
                        new EntityNotExistsTransition("Dwarf King", 8, "Default")
                        ),
                new State("Engaging",
                        new Prioritize(
                            new Follow(1.0, acquireRange: 15, range: 1),
                            new Protect(1.2, "Dwarf King", acquireRange: 15, protectionRange: 6, reprotectRange: 3),
                            new Wander(0.4f)
                            ),
                        new State("fire1_eng",
                            new Shoot(10, predictive: 0.2f, cooldown: 100000),
                            new TimedTransition("fire2_eng", 1500)
                            ),
                        new State("fire2_eng",
                            new Shoot(5, predictive: 0.2f, cooldown: 100000),
                            new TimedTransition("fire1_eng", 1500)
                            ),
                        new TimedTransition("Circling", 2500),
                        new EntityNotExistsTransition("Dwarf King", 8, "Default")
                        ),
                new ItemLoot("Magic Potion", 0.03f));
            db.Init("Dwarf Veteran",
                new Shoot(4),
                new State("Default",
                        new Prioritize(
                            new Follow(1.0, acquireRange: 9, range: 2, duration: 3000, cooldown: 1000),
                            new Wander(0.4f)
                            )
                        ),
                new State("Circling",
                        new Prioritize(
                            new Orbit(0.4, 2.7, acquireRange: 11),
                            new Protect(1.2, "Dwarf King", acquireRange: 15, protectionRange: 6, reprotectRange: 3),
                            new Wander(0.4f)
                            ),
                        new TimedTransition("Default", 3300),
                        new EntityNotExistsTransition("Dwarf King", 8, "Default")
                        ),
                new State("Engaging",
                        new Prioritize(
                            new Follow(1.0, acquireRange: 15, range: 1),
                            new Protect(1.2, "Dwarf King", acquireRange: 15, protectionRange: 6, reprotectRange: 3),
                            new Wander(0.4f)
                            ),
                        new TimedTransition("Circling", 2500),
                        new EntityNotExistsTransition("Dwarf King", 8, "Default")
                        ),
                new ItemLoot("Health Potion", 0.04f));
            db.Init("Dwarf King",
                new SpawnGroup("Dwarves", maxChildren: 10, cooldown: 8000),
                new Shoot(4, cooldown: 2000),
                new State("Circling",
                        new Prioritize(
                            new Orbit(0.4, 2.7, acquireRange: 11),
                            new Wander(0.4f)
                            ),
                        new TimedTransition("Engaging", 3400)
                        ),
                new State("Engaging",
                        new Taunt(0.2, "You'll taste my axe!"),
                        new Prioritize(
                            new Follow(1.0, acquireRange: 15, range: 1),
                            new Wander(0.4f)
                            ),
                        new TimedTransition("Circling", 2600)
                        ),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.2f),
                new TierLoot(4, TierLoot.LootType.Weapon, 0.12f),
                new TierLoot(3, TierLoot.LootType.Armor, 0.2f),
                new TierLoot(4, TierLoot.LootType.Armor, 0.15f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.02f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.11f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.38f),
                new ItemLoot("Magic Potion", 0.03f));
            db.Init("Werelion",
                new DropPortalOnDeath("Spider Den Portal", 0.1),
                new Spawn("Weretiger", maxChildren: 1, cooldown: 23000),
                new Spawn("Wereleopard", maxChildren: 2, cooldown: 9000),
                new Spawn("Werepanther", maxChildren: 3, cooldown: 15000),
                new Shoot(4, cooldown: 2000),
                new State("idle",
                        new Prioritize(
                            new StayAbove(0.6, 60),
                            new Wander(0.6f)
                            ),
                        new PlayerWithinTransition(11, "player_nearby")
                        ),
                new State("player_nearby",
                        new State("normal_attack",
                            new Shoot(10, count: 3, shootAngle: 15, predictive: 1, cooldown: 10000),
                            new TimedTransition("if_cloaked", 900)
                            ),
                        new State("if_cloaked",
                            new Shoot(10, count: 8, shootAngle: 45, defaultAngle: 20, cooldown: 1600,
                                cooldownOffset: 400),
                            new Shoot(10, count: 8, shootAngle: 45, defaultAngle: 42, cooldown: 1600,
                                cooldownOffset: 1200),
                            new PlayerWithinTransition(10, "normal_attack")
                            ),
                        new Prioritize(
                            new StayAbove(0.6, 60),
                            new Follow(0.4, acquireRange: 7, range: 3),
                            new Wander(0.6f)
                            ),
                        new TimedTransition("idle", 30000)
                        ),
                new TierLoot(4, TierLoot.LootType.Weapon, 0.18f),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.24f),
                new TierLoot(6, TierLoot.LootType.Armor, 0.03f),
                new TierLoot(2, TierLoot.LootType.Ring, 0.07f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.2f),
                new ItemLoot("Health Potion", 0.04f),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Weretiger",
                new Shoot(8, predictive: 0.3f, cooldown: 1000),
                new Prioritize(
                        new StayAbove(0.6, 60),
                        new Protect(1.1, "Werelion", acquireRange: 12, protectionRange: 10, reprotectRange: 5),
                        new Follow(0.8, range: 6.3),
                        new Wander(0.6f)
                        ),
                new ItemLoot("Health Potion", 0.03f));
            db.Init("Wereleopard",
                new Shoot(4.5f, predictive: 0.4f, cooldown: 900),
                new Prioritize(
                        new Protect(1.1, "Werelion", acquireRange: 12, protectionRange: 10, reprotectRange: 5),
                        new Follow(1.1, range: 3),
                        new Wander(1)
                        ),
                new ItemLoot("Health Potion", 0.03f));
            db.Init("Werepanther",
                new State("idle",
                        new Protect(0.65, "Werelion", acquireRange: 11, protectionRange: 7.5, reprotectRange: 7.4),
                        new PlayerWithinTransition(9.5, "wander")
                        ),
                new State("wander",
                        new Prioritize(
                            new Protect(0.65, "Werelion", acquireRange: 11, protectionRange: 7.5, reprotectRange: 7.4),
                            new Follow(0.65, range: 5, acquireRange: 10),
                            new Wander(0.65f)
                            ),
                        new PlayerWithinTransition(4, "jump")
                        ),
                new State("jump",
                        new Prioritize(
                            new Protect(0.65, "Werelion", acquireRange: 11, protectionRange: 7.5, reprotectRange: 7.4),
                            new Follow(7, range: 1, acquireRange: 6),
                            new Wander(0.55f)
                            ),
                        new TimedTransition("attack", 200)
                        ),
                new State("attack",
                        new Prioritize(
                            new Protect(0.65, "Werelion", acquireRange: 11, protectionRange: 7.5, reprotectRange: 7.4),
                            new Follow(0.5, range: 1, acquireRange: 6),
                            new Wander(0.5f)
                            ),
                        new Shoot(4, predictive: 0.5f, cooldown: 800, cooldownOffset: 300),
                        new TimedTransition("idle", 4000)
                        ),
                new ItemLoot("Magic Potion", 0.03f));
            db.Init("Horned Drake",
                new Spawn("Drake Baby", maxChildren: 1, initialSpawn: 1, cooldown: 50000),
                new State("idle",
                        new StayAbove(0.8, 60),
                        new PlayerWithinTransition(10, "get_player")
                        ),
                new State("get_player",
                        new Prioritize(
                            new StayAbove(0.8, 60),
                            new Follow(0.8, range: 2.7, acquireRange: 10, duration: 5000, cooldown: 1800),
                            new Wander(0.8f)
                            ),
                        new State("one_shot",
                            new Shoot(8, predictive: 0.1f, cooldown: 800),
                            new TimedTransition("three_shot", 900)
                            ),
                        new State("three_shot",
                            new Shoot(8, count: 3, shootAngle: 40, predictive: 0.1f, cooldown: 100000,
                                cooldownOffset: 800),
                            new TimedTransition("one_shot", 2000)
                            )
                        ),
                new State("protect_me",
                        new Protect(0.8, "Drake Baby", acquireRange: 12, protectionRange: 2.5, reprotectRange: 1.5),
                        new State("one_shot",
                            new Shoot(8, predictive: 0.1f, cooldown: 700),
                            new TimedTransition("three_shot", 800)
                            ),
                        new State("three_shot",
                            new Shoot(8, count: 3, shootAngle: 40, predictive: 0.1f, cooldown: 100000,
                                cooldownOffset: 700),
                            new TimedTransition("one_shot", 1800)
                            ),
                        new EntityNotExistsTransition("Drake Baby", 8, "idle")
                        ),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.14f),
                new TierLoot(6, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.19f),
                new TierLoot(6, TierLoot.LootType.Armor, 0.02f),
                new TierLoot(2, TierLoot.LootType.Ring, 0.07f),
                new TierLoot(3, TierLoot.LootType.Ring, 0.001f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.28f),
                new TierLoot(3, TierLoot.LootType.Ability, 0.001f),
                new ItemLoot("Health Potion", 0.09f),
                new ItemLoot("Magic Potion", 0.12f));
            db.Init("Drake Baby",
                new State("unharmed",
                        new Shoot(8, cooldown: 1500),
                        new State("wander",
                            new Prioritize(
                                new StayAbove(0.8, 60),
                                new Wander(0.8f)
                                ),
                            new TimedTransition("find_mama", 2000)
                            ),
                        new State("find_mama",
                            new Prioritize(
                                new StayAbove(0.8, 60),
                                new Protect(1.4, "Horned Drake", acquireRange: 15, protectionRange: 4, reprotectRange: 4)
                                ),
                            new TimedTransition("wander", 2000)
                            ),
                        new HpLessTransition(0.65, "call_mama")
                        ),
                new State("call_mama",
                        new Flash(0xff484848, 0.6, 5000),
                        new State("get_close_to_mama",
                            new Taunt("Awwwk! Awwwk!"),
                            new Protect(1.4, "Horned Drake", acquireRange: 15, protectionRange: 1, reprotectRange: 1),
                            new TimedTransition("cry_for_mama", 1500)
                            ),
                        new State("cry_for_mama",
                            new StayBack(0.65, 8),
                            new Order(8, "Horned Drake", "protect_me")
                            )
                        ));
            db.Init("Nomadic Shaman",
                new Prioritize(
                        new StayAbove(0.8, 55),
                        new Wander(0.7f)
                        ),
                new State("fire1",
                        new Shoot(10, index: 0, count: 3, shootAngle: 11, cooldown: 500, cooldownOffset: 500),
                        new TimedTransition("fire2", 3100)
                        ),
                new State("fire2",
                        new Shoot(10, index: 1, cooldown: 700, cooldownOffset: 700),
                        new TimedTransition("fire1", 2200)
                        ),
                new ItemLoot("Magic Potion", 0.04f));
            db.Init("Sand Phantom",
                new Prioritize(
                        new StayAbove(0.85, 60),
                        new Follow(0.85, acquireRange: 10.5, range: 1),
                        new Wander(0.85f)
                        ),
                new Shoot(8, predictive: 0.4f, cooldown: 400, cooldownOffset: 600),
                new State("follow_player",
                        new PlayerWithinTransition(4.4, "sneak_away_from_player")
                        ),
                new State("sneak_away_from_player",
                        new Transform("Sand Phantom Wisp")
                        ));
            db.Init("Sand Phantom Wisp",
                new Shoot(8, predictive: 0.4f, cooldown: 400, cooldownOffset: 600),
                new State("move_away_from_player",
                        new State("keep_back",
                            new Prioritize(
                                new StayBack(0.6, distance: 5),
                                new Wander(0.9f)
                                ),
                            new TimedTransition("wander", 800)
                            ),
                        new State("wander",
                            new Wander(0.9f),
                            new TimedTransition("keep_back", 800)
                            ),
                        new TimedTransition("wisp_finished", 6500)
                        ),
                new State("wisp_finished",
                        new Transform("Sand Phantom")
                        ));
            db.Init("Great Lizard",
                new State("idle",
                        new StayAbove(0.6, 60),
                        new Wander(0.6f),
                        new PlayerWithinTransition(10, "charge")
                        ),
                new State("charge",
                        new Prioritize(
                            new StayAbove(0.6, 60),
                            new Follow(6, acquireRange: 11, range: 1.5)
                            ),
                        new TimedTransition("spit", 200)
                        ),
                new State("spit",
                        new Shoot(8, index: 0, count: 1, cooldown: 100000, cooldownOffset: 1000),
                        new Shoot(8, index: 0, count: 2, shootAngle: 16, cooldown: 100000,
                            cooldownOffset: 1200),
                        new Shoot(8, index: 0, count: 1, predictive: 0.2f, cooldown: 100000,
                            cooldownOffset: 1600),
                        new Shoot(8, index: 0, count: 2, shootAngle: 24, cooldown: 100000,
                            cooldownOffset: 2200),
                        new Shoot(8, index: 0, count: 1, predictive: 0.2f, cooldown: 100000,
                            cooldownOffset: 2800),
                        new Shoot(8, index: 0, count: 2, shootAngle: 16, cooldown: 100000,
                            cooldownOffset: 3200),
                        new Shoot(8, index: 0, count: 1, predictive: 0.1f, cooldown: 100000,
                            cooldownOffset: 3800),
                        new Prioritize(
                            new StayAbove(0.6, 60),
                            new Wander(0.6f)
                            ),
                        new TimedTransition("flame_ring", 5000)
                        ),
                new State("flame_ring",
                        new Shoot(7, index: 1, count: 30, shootAngle: 12, cooldown: 400, cooldownOffset: 600),
                        new Prioritize(
                            new StayAbove(0.6, 60),
                            new Follow(0.6, acquireRange: 9, range: 1),
                            new Wander(0.6f)
                            ),
                        new TimedTransition("pause", 3500)
                        ),
                new State("pause",
                        new Prioritize(
                            new StayAbove(0.6, 60),
                            new Wander(0.6f)
                            ),
                        new TimedTransition("idle", 1000)
                        ),
                new TierLoot(4, TierLoot.LootType.Weapon, 0.14f),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.19f),
                new TierLoot(6, TierLoot.LootType.Armor, 0.02f),
                new TierLoot(2, TierLoot.LootType.Ring, 0.07f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.27f),
                new ItemLoot("Health Potion", 0.12f),
                new ItemLoot("Magic Potion", 0.10f));
            db.Init("Tawny Warg",
                new Shoot(3.4f),
                new Prioritize(
                        new Protect(1.2, "Desert Werewolf", acquireRange: 14, protectionRange: 8, reprotectRange: 5),
                        new Follow(0.7, acquireRange: 9, range: 2),
                        new Wander(0.8f)
                        ),
                new ItemLoot("Health Potion", 0.04f));
            db.Init("Demon Warg",
                new Shoot(4.5f),
                new Prioritize(
                        new Protect(1.2, "Desert Werewolf", acquireRange: 14, protectionRange: 8, reprotectRange: 5),
                        new Wander(0.8f)
                        ),
                new ItemLoot("Health Potion", 0.04f));
            db.Init("Desert Werewolf",
                new SpawnGroup("Wargs", maxChildren: 8, cooldown: 8000),
                new State("unharmed",
                        new Shoot(8, index: 0, predictive: 0.3f, cooldown: 1000, cooldownOffset: 500),
                        new Prioritize(
                            new Follow(0.5, acquireRange: 10.5, range: 2.5),
                            new Wander(0.5f)
                            ),
                        new HpLessTransition(0.75, "enraged")
                        ),
                new State("enraged",
                        new Shoot(8, index: 0, predictive: 0.3f, cooldown: 1000, cooldownOffset: 500),
                        new Taunt(0.7, "GRRRRAAGH!"),
                        new ChangeSize(20, 170),
                        new Flash(0xffff0000, 0.4, 5000),
                        new Prioritize(
                            new Follow(0.65, acquireRange: 9, range: 2),
                            new Wander(0.65f)
                            )
                        ),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.2f),
                new TierLoot(4, TierLoot.LootType.Weapon, 0.12f),
                new TierLoot(3, TierLoot.LootType.Armor, 0.2f),
                new TierLoot(4, TierLoot.LootType.Armor, 0.15f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.02f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.11f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.38f),
                new ItemLoot("Magic Potion", 0.03f));
        }
    }
}
