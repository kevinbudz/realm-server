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
    public class HauntedCemetery : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Area 1 Controller",
                new ConditionalEffect(ConditionEffectIndex.Invincible, perm: true),
                new TransformOnDeath("Haunted Cemetery Gates Portal"),
                new State("Start",
                    new PlayerWithinTransition(4, "1")
                ),
                new State("1",
                    new TimedTransition(0, "2")
                ),
                new State("2",
                    new SetAltTexture(2),
                    new TimedTransition(100, "3")
                ),
                new State("3",
                    new SetAltTexture(1),
                    new Taunt(true, "Welcome to my domain. l challenge you, warrior, to defeat my undead hordes and claim your prize...."),
                    new TimedTransition(2000, "4")
                ),
                new State("4",
                    new Taunt(true, "Say,'READY' when you are ready to face your opponeants.", "Prepare yourself...Say, 'READY' when you wish the battle to begin!"),
                    new TimedTransition(0, "5")
                ),
                new State("5",
                    new PlayerTextTransition("7", "ready"),
                    new SetAltTexture(3),
                    new TimedTransition(150, "6")
                ),
                new State("6",
                    new PlayerTextTransition("7", "ready"),
                    new SetAltTexture(4),
                    new TimedTransition(150, "6_1")
                ),
                new State("6_1",
                    new PlayerTextTransition("7", "ready"),
                    new SetAltTexture(3),
                    new TimedTransition(150, "6_2")
                ),
                new State("6_2",
                    new PlayerTextTransition("7", "ready"),
                    new SetAltTexture(4),
                    new TimedTransition(150, "6_3")
                ),
                new State("6_3",
                    new SetAltTexture(3),
                    new PlayerTextTransition("7", "ready"),
                    new TimedTransition(150, "6_4")
                ),
                new State("6_4",
                    new SetAltTexture(1),
                    new PlayerTextTransition("7", "ready")
                ),
                new State("7",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 1"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 1"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 1"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 1"),
                    new EntityExistsTransition("Arena Skeleton", 999, "Check 1")
                ),
                new State("Check 1",
                    new EntitiesNotExistsTransition(100, "8", "Arena Skeleton", "Troll 1", "Troll 2")
                ),
                new State("8",
                    new SetAltTexture(2),
                    new TimedTransition(0, "9")
                ),
                new State("9",
                    new SetAltTexture(1),
                    new TimedTransition(500, "10")
                ),
                new State("10",
                    new Taunt(true, "The next wave will appear in 3 seconds. Prepare yourself!", "l hope you're prepared because the next wave is in 3 seconds.", "The next onslaught will begin in 3 seconds!", "You have 3 seconds until your next challenge!", "3 seconds until the next attack!"),
                    new TimedTransition(0, "11")
                ),
                new State("11",
                    new SetAltTexture(3),
                    new TimedTransition(150, "12")
                ),
                new State("12",
                    new SetAltTexture(4),
                    new TimedTransition(150, "13")
                ),
                new State("13",
                    new SetAltTexture(3),
                    new TimedTransition(150, "14")
                ),
                new State("14",
                    new SetAltTexture(4),
                    new TimedTransition(150, "15")
                ),
                new State("15",
                    new SetAltTexture(3),
                    new TimedTransition(150, "16")
                ),
                new State("16",
                    new SetAltTexture(4),
                    new TimedTransition(150, "17")
                ),
                new State("17",
                    new SetAltTexture(3),
                    new TimedTransition(150, "18")
                ),
                new State("18",
                    new SetAltTexture(4),
                    new TimedTransition(150, "19")
                ),
                new State("19",
                    new SetAltTexture(3),
                    new TimedTransition(150, "20")
                ),
                new State("20",
                    new SetAltTexture(4),
                    new TimedTransition(150, "21")
                ),
                new State("21",
                    new SetAltTexture(3),
                    new TimedTransition(150, "22")
                ),
                new State("22",
                    new SetAltTexture(4),
                    new TimedTransition(150, "23")
                ),
                new State("23",
                    new SetAltTexture(3),
                    new TimedTransition(150, "24")
                ),
                new State("24",
                    new SetAltTexture(4),
                    new TimedTransition(150, "25")
                ),
                new State("25",
                    new SetAltTexture(1),
                    new TimedTransition(150, "26")
                ),
                new State("26",
                    new SetAltTexture(2),
                    new TimedTransition(100, "27")
                ),
                new State("27",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 2"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 2"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 2"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 2"),
                    new EntityExistsTransition("Arena Skeleton", 999, "Check 2")
                ),
                new State("Check 2",
                    new EntitiesNotExistsTransition(100, "28", "Arena Skeleton", "Troll 1", "Troll 2")
                ),
                new State("28",
                    new SetAltTexture(2),
                    new TimedTransition(0, "29")
                ),
                new State("29",
                    new SetAltTexture(1),
                    new TimedTransition(150, "30")
                ),
                new State("30",
                    new Taunt(true, "The next wave will appear in 3 seconds. Prepare yourself!", "l hope you're prepared because the next wave is in 3 seconds.", "The next onslaught will begin in 3 seconds!", "You have 3 seconds until your next challenge!", "3 seconds until the next attack!"),
                    new TimedTransition(0, "31")
                ),
                new State("31",
                    new SetAltTexture(3),
                    new TimedTransition(150, "32")
                ),
                new State("32",
                    new SetAltTexture(4),
                    new TimedTransition(150, "33")
                ),
                new State("33",
                    new SetAltTexture(3),
                    new TimedTransition(150, "34")
                ),
                new State("34",
                    new SetAltTexture(4),
                    new TimedTransition(150, "35")
                ),
                new State("35",
                    new SetAltTexture(3),
                    new TimedTransition(150, "36")
                ),
                new State("36",
                    new SetAltTexture(4),
                    new TimedTransition(150, "37")
                ),
                new State("37",
                    new SetAltTexture(3),
                    new TimedTransition(150, "38")
                ),
                new State("38",
                    new SetAltTexture(4),
                    new TimedTransition(150, "39")
                ),
                new State("39",
                    new SetAltTexture(3),
                    new TimedTransition(150, "40")
                ),
                new State("40",
                    new SetAltTexture(4),
                    new TimedTransition(150, "41")
                ),
                new State("41",
                    new SetAltTexture(3),
                    new TimedTransition(150, "42")
                ),
                new State("42",
                    new SetAltTexture(4),
                    new TimedTransition(150, "43")
                ),
                new State("43",
                    new SetAltTexture(3),
                    new TimedTransition(150, "44")
                ),
                new State("44",
                    new SetAltTexture(4),
                    new TimedTransition(150, "45")
                ),
                new State("45",
                    new SetAltTexture(1),
                    new TimedTransition(100, "46")
                ),
                new State("46",
                    new SetAltTexture(2),
                    new TimedTransition(100, "47")
                ),
                new State("47",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 3"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 3"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 3"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 3"),
                    new EntityExistsTransition("Arena Skeleton", 999, "Check 3")
                ),
                new State("Check 3",
                    new EntitiesNotExistsTransition(100, "48", "Arena Skeleton", "Troll 1", "Troll 2")
                ),
                new State("48",
                    new SetAltTexture(2),
                    new TimedTransition(0, "49")
                ),
                new State("49",
                    new SetAltTexture(1),
                    new TimedTransition(500, "50")
                ),
                new State("50",
                    new Taunt(true, "The next wave will appear in 3 seconds. Prepare yourself!", "l hope you're prepared because the next wave is in 3 seconds.", "The next onslaught will begin in 3 seconds!", "You have 3 seconds until your next challenge!", "3 seconds until the next attack!"),
                    new TimedTransition(0, "51")
                ),
                new State("51",
                    new SetAltTexture(3),
                    new TimedTransition(150, "52")
                ),
                new State("52",
                    new SetAltTexture(4),
                    new TimedTransition(150, "53")
                ),
                new State("53",
                    new SetAltTexture(3),
                    new TimedTransition(150, "54")
                ),
                new State("54",
                    new SetAltTexture(4),
                    new TimedTransition(150, "55")
                ),
                new State("55",
                    new SetAltTexture(3),
                    new TimedTransition(150, "56")
                ),
                new State("56",
                    new SetAltTexture(4),
                    new TimedTransition(150, "57")
                ),
                new State("57",
                    new SetAltTexture(3),
                    new TimedTransition(150, "58")
                ),
                new State("58",
                    new SetAltTexture(4),
                    new TimedTransition(150, "59")
                ),
                new State("59",
                    new SetAltTexture(3),
                    new TimedTransition(150, "60")
                ),
                new State("60",
                    new SetAltTexture(4),
                    new TimedTransition(150, "61")
                ),
                new State("61",
                    new SetAltTexture(3),
                    new TimedTransition(150, "62")
                ),
                new State("62",
                    new SetAltTexture(4),
                    new TimedTransition(150, "63")
                ),
                new State("63",
                    new SetAltTexture(3),
                    new TimedTransition(150, "64")
                ),
                new State("64",
                    new SetAltTexture(4),
                    new TimedTransition(150, "65")
                ),
                new State("65",
                    new SetAltTexture(1),
                    new TimedTransition(150, "66")
                ),
                new State("66",
                    new SetAltTexture(2),
                    new TimedTransition(150, "67")
                ),
                new State("67",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 4"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 4"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 4"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 4"),
                    new EntityExistsTransition("Arena Skeleton", 999, "Check 4")
                ),
                new State("Check 4",
                    new EntitiesNotExistsTransition(100, "68", "Arena Skeleton", "Troll 1", "Troll 2")
                ),
                new State("68",
                    new SetAltTexture(2),
                    new TimedTransition(0, "69")
                ),
                new State("69",
                    new SetAltTexture(1),
                    new TimedTransition(500, "70")
                ),
                new State("70",
                    new Taunt(true, "The next wave will appear in 3 seconds. Prepare yourself!", "l hope you're prepared because the next wave is in 3 seconds.", "The next onslaught will begin in 3 seconds!", "You have 3 seconds until your next challenge!", "3 seconds until the next attack!"),
                    new TimedTransition(0, "71")
                ),
                new State("71",
                    new SetAltTexture(3),
                    new TimedTransition(150, "72")
                ),
                new State("72",
                    new SetAltTexture(4),
                    new TimedTransition(150, "73")
                ),
                new State("73",
                    new SetAltTexture(3),
                    new TimedTransition(150, "74")
                ),
                new State("74",
                    new SetAltTexture(4),
                    new TimedTransition(150, "75")
                ),
                new State("75",
                    new SetAltTexture(3),
                    new TimedTransition(150, "76")
                ),
                new State("76",
                    new SetAltTexture(4),
                    new TimedTransition(150, "77")
                ),
                new State("77",
                    new SetAltTexture(3),
                    new TimedTransition(150, "78")
                ),
                new State("78",
                    new SetAltTexture(4),
                    new TimedTransition(150, "79")
                ),
                new State("79",
                    new SetAltTexture(3),
                    new TimedTransition(150, "80")
                ),
                new State("80",
                    new SetAltTexture(4),
                    new TimedTransition(150, "81")
                ),
                new State("81",
                    new SetAltTexture(3),
                    new TimedTransition(150, "82")
                ),
                new State("82",
                    new SetAltTexture(4),
                    new TimedTransition(150, "83")
                ),
                new State("83",
                    new SetAltTexture(3),
                    new TimedTransition(150, "84")
                ),
                new State("84",
                    new SetAltTexture(4),
                    new TimedTransition(150, "85")
                ),
                new State("85",
                    new SetAltTexture(1),
                    new TimedTransition(150, "86")
                ),
                new State("86",
                    new SetAltTexture(2),
                    new TimedTransition(150, "87")
                ),
                new State("87",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 5"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 5"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 5"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 5"),
                    new EntityExistsTransition("Troll 3", 999, "Check 5")
                ),
                new State("Check 5",
                    new EntityNotExistsTransition("Troll 3", 100, "88")
                ),
                new State("88",
                    new Suicide()
                )
            );
            db.Init("Arena South Gate Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("Leech"),
                new State("Stage 1",
                    new Spawn("Arena Skeleton", maxChildren: 2)
                ),
                new State("Stage 2",
                    new Spawn("Arena Skeleton", maxChildren: 2)
                ),
                new State("Stage 3",
                    new Spawn("Troll 1", maxChildren: 1)
                ),
                new State("Stage 4",
                    new Spawn("Troll 1", maxChildren: 1)
                ),
                new State("Stage 5",
                    new Suicide()
                ),
                new State("Stage 6",
                    new Spawn("Arena Ghost 2", maxChildren: 1)
                ),
                new State("Stage 7",
                    new Spawn("Arena Ghost 2", maxChildren: 1)
                ),
                new State("Stage 8",
                    new Spawn("Arena Ghost 2", maxChildren: 1)
                ),
                new State("Stage 9",
                    new Spawn("Arena Possessed Girl", maxChildren: 1)
                ),
                new State("Stage 10",
                    new Suicide()
                ),
                new State("Stage 11",
                    new Spawn("Arena Risen Brawler", maxChildren: 1)
                ),
                new State("Stage 12",
                    new Spawn("Arena Risen Warrior", maxChildren: 1)
                ),
                new State("Stage 13",
                    new Spawn("Arena Risen Warrior", maxChildren: 1)
                ),
                new State("Stage 14",
                    new Spawn("Arena Risen Warrior", maxChildren: 1),
                    new Spawn("Arena Risen Archer", maxChildren: 1)
                ),
                new State("Stage 15",
                    new Suicide()
                )
            );
            db.Init("Arena East Gate Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("Leech"),
                new State("Stage 1",
                    new Spawn("Arena Skeleton", maxChildren: 1)
                ),
                new State("Stage 2",
                    new Spawn("Arena Skeleton", maxChildren: 2)
                ),
                new State("Stage 3",
                    new Spawn("Arena Skeleton", maxChildren: 1),
                    new Spawn("Troll 2", maxChildren: 1)
                ),
                new State("Stage 4",
                    new Spawn("Arena Skeleton", maxChildren: 1),
                    new Spawn("Troll 2", maxChildren: 1)
                ),
                new State("Stage 5",
                    new Suicide()
                ),
                new State("Stage 6",
                    new Spawn("Arena Ghost 1", maxChildren: 1)
                ),
                new State("Stage 7",
                    new Spawn("Arena Ghost 1", maxChildren: 1)
                ),
                new State("Stage 8",
                    new Spawn("Arena Ghost 2", maxChildren: 1)
                ),
                new State("Stage 9",
                    new Spawn("Arena Ghost 2", maxChildren: 1)
                ),
                new State("Stage 10",
                    new Spawn("Arena Possessed Girl", maxChildren: 1)
                ),
                new State("Stage 11",
                    new Spawn("Arena Risen Warrior", maxChildren: 1)
                ),
                new State("Stage 12",
                    new Spawn("Arena Risen Brawler", maxChildren: 1)
                ),
                new State("Stage 13",
                    new Spawn("Arena Risen Brawler", maxChildren: 1)
                ),
                new State("Stage 14",
                    new Spawn("Arena Risen Brawler", maxChildren: 1)
                ),
                new State("Stage 15",
                    new Suicide()
                )
            );
            db.Init("Arena North Gate Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("Leech"),
                new State("Stage 1",
                    new Spawn("Arena Skeleton", maxChildren: 1)
                ),
                new State("Stage 2",
                    new Spawn("Troll 1", maxChildren: 1)
                ),
                new State("Stage 3",
                    new Spawn("Troll 2", maxChildren: 1)
                ),
                new State("Stage 4",
                    new Spawn("Troll 2", maxChildren: 1)
                ),
                new State("Stage 5",
                    new Spawn("Troll 3", maxChildren: 1)
                ),
                new State("Stage 6",
                    new Spawn("Arena Ghost 1", maxChildren: 1)
                ),
                new State("Stage 7",
                    new Spawn("Arena Possessed Girl", maxChildren: 1)
                ),
                new State("Stage 8",
                    new Spawn("Arena Ghost 2", maxChildren: 1)
                ),
                new State("Stage 9",
                    new Spawn("Arena Ghost 1", maxChildren: 1)
                ),
                new State("Stage 10",
                    new Spawn("Arena Ghost Bride", maxChildren: 1)
                ),
                new State("Stage 11",
                    new Spawn("Arena Risen Mage", maxChildren: 1)
                ),
                new State("Stage 12",
                    new Spawn("Arena Risen Warrior", maxChildren: 2)
                ),
                new State("Stage 13",
                    new Spawn("Arena Risen Warrior", maxChildren: 1),
                    new Spawn("Arena Risen Mage", maxChildren: 1)
                ),
                new State("Stage 14",
                    new Spawn("Arena Risen Warrior", maxChildren: 2)
                ),
                new State("Stage 15",
                    new Spawn("Arena Grave Caretaker", maxChildren: 1)
                )
            );
            db.Init("Arena Skeleton",
                new Prioritize(
                    new Follow(0.5, 8, 2, 2000, 3500),
                    new Wander(0.5f)
                ),
                new Shoot(8, 1, index: 0, cooldown: 800)
            );
            db.Init("Troll 1",
                new Prioritize(
                    new Charge(1.1, 8, 3000),
                    new Follow(0.5, 15, 2, 4000, 2000)
                ),
                new Shoot(5, 1, index: 0, cooldown: 1000)
            );
            db.Init("Troll 2",
                new Orbit(0.5, 5, 10),
                new Prioritize(
                    new Follow(1.1, 15, 6, 4000, 5000)
                ),
                new Shoot(8, 1, index: 0, predictive: 1, cooldown: 1600),
                new Grenade(radius: 3, range: 6, damage: 85, cooldown: 2000)
            );
            db.Init("Troll 3",
                new State("Ini",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new State("Check1",
                        new EntityExistsTransition("Area 1 Controller", 999, "2")
                    ),
                    new State("2",
                        new MoveTo(0.9f, 21f, 21f)
                    ),
                    new Taunt("This forest will be your tomb!"),
                    new TossObject("Arena Mushroom", 7, cooldown: 3000),
                    new TimedTransition(2000, "Normal")
                ),
                new State("Normal",
                    new Prioritize(
                        new Wander(0.3f)
                    ),
                    new Follow(0.6, 10, 3, 5000, 5500),
                    new Shoot(8, 1, index: 0, cooldown: 1000),
                    new Shoot(24, 6, 60, 1, fixedAngle: 30, cooldown: 2000),
                    new TossObject("Arena Mushroom", 7, cooldown: 3000),
                    new HpLessTransition(0.7, "Summon")
                ),
                new State("Summon",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Taunt("I call upon the aid of warriors past! Smite these trespassers!"),
                    new Spawn("Arena Skeleton", maxChildren: 5, cooldown: 4000),
                    new Shoot(24, 6, 60, 1, fixedAngle: 0, cooldown: 1500),
                    new Shoot(24, 6, 60, 1, fixedAngle: 30, cooldown: 1500),
                    new TossObject("Arena Mushroom", 7, cooldown: 3000),
                    new EntitiesNotExistsTransition(99, "Enrage", "Arena Skeleton")
                ),
                new State("Enrage",
                    new Flash(0xFFFFFF, 0.1, 15),
                    new ChangeSize(1, 200),
                    new Follow(1.1, 10, 3, 5000, 5500),
                    new Charge(1.3, 7, 3000),
                    new Shoot(24, 6, 60, 1, fixedAngle: 0, cooldown: 900),
                    new TossObject("Arena Mushroom", 7, cooldown: 1500),
                    new TimedTransition(15000, "Normal 2")
                ),
                new State("Normal 2",
                    new Wander(0.3f),
                    new ChangeSize(1, 150),
                    new Follow(1.1, 10, 3, 5000, 5500),
                    new TossObject("Arena Mushroom", 7, cooldown: 3000),
                    new Shoot(8, 1, index: 0, cooldown: 1000),
                    new HpLessTransition(0.4, "Summon")
                ),
                new Threshold(0.005f,
                    new TierLoot(10, TierLoot.LootType.Weapon, 0.07f),
                    new TierLoot(11, TierLoot.LootType.Weapon, 0.07f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.07f),
                    new TierLoot(5, TierLoot.LootType.Ability, 0.07f),
                    new TierLoot(11, TierLoot.LootType.Armor, 0.07f),
                    new TierLoot(12, TierLoot.LootType.Armor, 0.07f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.07f),
                    new TierLoot(5, TierLoot.LootType.Ring, 0.07f)
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Speed", 0.8f),
                    new ItemLoot("Potion of Wisdom", 0.8f)
                )
            );
            db.Init("Arena Mushroom",
                new State("Ini",
                    new PlayerWithinTransition(3, "A1", seeInvis: true)
                ),
                new State("A1",
                    new TimedTransition(750, "A2")
                ),
                new State("A2",
                    new SetAltTexture(1),
                    new TimedTransition(750, "A3")
                ),
                new State("A3",
                    new SetAltTexture(2),
                    new TimedTransition(1000, "Explode")
                ),
                new State("Explode",
                    new Flash(0xFFFFFF, 0.1, 30),
                    new TimedTransition(3000, "Suicide")
                ),
                new State("Suicide",
                    new Shoot(24, 6, 60f, index: 0, fixedAngle: 30),
                    new Suicide()
                )
            );
            db.Init("Arena West Gate Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("Leech"),
                new State("Stage 1",
                    new Spawn("Arena Skeleton", maxChildren: 1)
                ),
                new State("Stage 2",
                    new Spawn("Arena Skeleton", maxChildren: 2)
                ),
                new State("Stage 3",
                    new Spawn("Arena Skeleton", maxChildren: 1),
                    new Spawn("Troll 2", maxChildren: 1)
                ),
                new State("Stage 4",
                    new Spawn("Arena Skeleton", maxChildren: 1),
                    new Spawn("Troll 2", maxChildren: 1)
                ),
                new State("Stage 5",
                    new Suicide()
                ),
                new State("Stage 6",
                    new Spawn("Arena Ghost 1", maxChildren: 1)
                ),
                new State("Stage 7",
                    new Spawn("Arena Ghost 2", maxChildren: 1)
                ),
                new State("Stage 8",
                    new Spawn("Arena Ghost 2", maxChildren: 1)
                ),
                new State("Stage 9",
                    new Spawn("Arena Possessed Girl", maxChildren: 1)
                ),
                new State("Stage 10",
                    new Spawn("Arena Possessed Girl", maxChildren: 1)
                ),
                new State("Stage 11",
                    new Spawn("Arena Risen Archer", maxChildren: 1)
                ),
                new State("Stage 12",
                    new Spawn("Arena Risen Brawler", maxChildren: 1),
                    new Spawn("Arena Risen Warrior", maxChildren: 1)
                ),
                new State("Stage 13",
                    new Spawn("Arena Risen Archer", maxChildren: 1)
                ),
                new State("Stage 14",
                    new Spawn("Arena Risen Archer", maxChildren: 1),
                    new Spawn("Arena Risen Mage", maxChildren: 1)
                ),
                new State("Stage 15",
                    new Suicide()
                )
            );
        }
    }
}
