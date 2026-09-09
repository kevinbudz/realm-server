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
    public class HauntedCemeteryGates : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Area 2 Controller",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new TransformOnDeath("Haunted Cemetery Graves Portal"),
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
                    new Taunt(true, "Congratulations on making it past the first area! This area will not be so easy!"),
                    new TimedTransition(500, "4")
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
                    new PlayerTextTransition("7", "ready"),
                    new SetAltTexture(3),
                    new TimedTransition(150, "6_4")
                ),
                new State("6_4",
                    new SetAltTexture(1),
                    new PlayerTextTransition("7", "ready")
                ),
                new State("7",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 6"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 6"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 6"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 6"),
                    new EntityExistsTransition("Arena Ghost 2", 999, "Check 1")
                ),
                new State("Check 1",
                    new EntitiesNotExistsTransition(100, "8", "Arena Ghost 1", "Arena Ghost 2", "Arena Possessed Girl")
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
                    new TimedTransition(100, "26")
                ),
                new State("26",
                    new SetAltTexture(2),
                    new TimedTransition(100, "27")
                ),
                new State("27",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 7"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 7"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 7"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 7"),
                    new EntityExistsTransition("Arena Ghost 2", 999, "Check 2")
                ),
                new State("Check 2",
                    new EntitiesNotExistsTransition(100, "28", "Arena Ghost 1", "Arena Ghost 2", "Arena Possessed Girl")
                ),
                new State("28",
                    new SetAltTexture(2),
                    new TimedTransition(0, "29")
                ),
                new State("29",
                    new SetAltTexture(1),
                    new TimedTransition(500, "30")
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
                    new TimedTransition(150, "46")
                ),
                new State("46",
                    new SetAltTexture(2),
                    new TimedTransition(150, "47")
                ),
                new State("47",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 8"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 8"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 8"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 8"),
                    new EntityExistsTransition("Arena Ghost 2", 999, "Check 3")
                ),
                new State("Check 3",
                    new EntitiesNotExistsTransition(100, "48", "Arena Ghost 1", "Arena Ghost 2", "Arena Possessed Girl")
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
                    new TimedTransition(100, "66")
                ),
                new State("66",
                    new SetAltTexture(2),
                    new TimedTransition(100, "67")
                ),
                new State("67",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 9"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 9"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 9"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 9"),
                    new EntityExistsTransition("Arena Ghost 2", 999, "Check 4")
                ),
                new State("Check 4",
                    new EntitiesNotExistsTransition(100, "68", "Arena Ghost 1", "Arena Ghost 2", "Arena Possessed Girl")
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
                    new TimedTransition(100, "86")
                ),
                new State("86",
                    new SetAltTexture(2),
                    new TimedTransition(100, "87")
                ),
                new State("87",
                    new SetAltTexture(0),
                    new OrderOnce(100, "Arena South Gate Spawner", "Stage 10"),
                    new OrderOnce(100, "Arena West Gate Spawner", "Stage 10"),
                    new OrderOnce(100, "Arena East Gate Spawner", "Stage 10"),
                    new OrderOnce(100, "Arena North Gate Spawner", "Stage 10"),
                    new EntityExistsTransition("Arena Possessed Girl", 999, "Check 5")
                ),
                new State("Check 5",
                    new EntitiesNotExistsTransition(100, "88", "Arena Ghost Bride", "Arena Possessed Girl")
                ),
                new State("88",
                    new Suicide()
                )
            );
            db.Init("Arena Ghost 1",
                new Prioritize(
                    new Orbit(0.5, 2.8, 10),
                    new Charge(0.8, 11, 2000)
                ),
                new Wander(0.6f),
                new Shoot(5.5f, 1, index: 0, cooldown: 1000)
            );
            db.Init("Arena Ghost 2",
                new State("Ini",
                    new SetAltTexture(0),
                    new Prioritize(
                        new Wander(0.3f),
                        new Charge(2.6, 12, 2000),
                        new StayBack(0.8, 4)
                    ),
                    new Shoot(5, 3, 20, 0, cooldown: 1000),
                    new TimedTransition(2000, "Disappear")
                ),
                new State("Disappear",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new SetAltTexture(1, 2, 300, loop: true),
                    new Wander(0.5f),
                    new TimedTransition(1500, "Ini")
                )
            );
            db.Init("Arena Possessed Girl",
                new Prioritize(
                    new Follow(0.5, 15, 3, 5000, 3000),
                    new Wander(0.3f)
                ),
                new Shoot(24, 8, 45, 0, fixedAngle: 22.5f, cooldown: 600)
            );
            db.Init("Arena Ghost Bride",
                new ScaleHP2(35),
                new State("Ini",
                    new Prioritize(
                        new Wander(0.3f),
                        new Follow(0.6, 8, 3, 3000, 4000),
                        new StayBack(0.4, 4)
                    ),
                    new Shoot(8, 1, index: 0, predictive: 0.9f, cooldown: 3000),
                    new Shoot(8, 2, 30, 1, fixedAngle: 0, cooldown: 1500, cooldownOffset: 0),
                    new Shoot(8, 2, 30, 1, fixedAngle: 180, cooldown: 1500, cooldownOffset: 0),
                    new Shoot(8, 2, 30, 1, fixedAngle: 90, cooldown: 1500, cooldownOffset: 300),
                    new Shoot(8, 2, 30, 1, fixedAngle: 270, cooldown: 1500, cooldownOffset: 300),
                    new HpLessTransition(0.75, "ActivateBigDemon")
                ),
                new State("ActivateBigDemon",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new SetAltTexture(1),
                    new OrderOnce(100, "Arena Statue Left", "Active"),
                    new EntityNotExistsTransition("Arena Statue Left", 100, "Attack1")
                ),
                new State("Attack1",
                    new SetAltTexture(0),
                    new Prioritize(
                        new Wander(0.3f),
                        new Follow(0.6, 8, 3, 3000, 4000),
                        new StayBack(0.4, 4)
                    ),
                    new Shoot(8, 1, index: 0, predictive: 0.9f, cooldown: 3000),
                    new Shoot(8, 2, 30, 1, fixedAngle: 0, cooldown: 1500, cooldownOffset: 0),
                    new Shoot(8, 2, 30, 1, fixedAngle: 180, cooldown: 1500, cooldownOffset: 0),
                    new Shoot(8, 2, 30, 1, fixedAngle: 90, cooldown: 1500, cooldownOffset: 300),
                    new Shoot(8, 2, 30, 1, fixedAngle: 270, cooldown: 1500, cooldownOffset: 300),
                    new HpLessTransition(0.5, "ActivateWerewolf")
                ),
                new State("ActivateWerewolf",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new SetAltTexture(1),
                    new OrderOnce(100, "Arena Statue Right", "Active"),
                    new EntityNotExistsTransition("Arena Statue Right", 100, "Attack2")
                ),
                new State("Attack2",
                    new SetAltTexture(0),
                    new Prioritize(
                        new Wander(0.3f),
                        new Follow(0.6, 8, 3, 3000, 4000),
                        new StayBack(0.4, 4)
                    ),
                    new Shoot(8, 1, index: 0, predictive: 0.9f, cooldown: 3000),
                    new Shoot(8, 2, 30, 1, fixedAngle: 0, cooldown: 1500, cooldownOffset: 0),
                    new Shoot(8, 2, 30, 1, fixedAngle: 180, cooldown: 1500, cooldownOffset: 0),
                    new Shoot(8, 2, 30, 1, fixedAngle: 90, cooldown: 1500, cooldownOffset: 300),
                    new Shoot(8, 2, 30, 1, fixedAngle: 270, cooldown: 1500, cooldownOffset: 300)
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
                    new ItemLoot("Potion of Speed", 0.3f, min: 3),
                    new ItemLoot("Potion of Wisdom", 0.3f)
                )
            );
            db.Init("Arena Statue Right",
                new ScaleHP2(35),
                new State("Ini",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new SetAltTexture(1)
                ),
                new State("Active",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(0, "Active_fix")
                ),
                new State("Active_fix",
                    new SetAltTexture(0),
                    new Prioritize(
                        new Follow(0.7, 10, 2, 6000, 3000),
                        new Orbit(0.6, 3, 10)
                    ),
                    new Shoot(8, 1, index: 0, predictive: 1, cooldown: 700),
                    new Shoot(9, 2, 35, 1, predictive: 1, cooldown: 1500, cooldownOffset: 0),
                    new HpLessTransition(0.75, "Phase2")
                ),
                new State("Phase2",
                    new Prioritize(
                        new Follow(0.7, 10, 2, 6000, 3000),
                        new Orbit(0.6, 3, 10)
                    ),
                    new Shoot(8, 1, index: 0, predictive: 1, cooldown: 700),
                    new Shoot(9, 1, index: 1, fixedAngle: 35, cooldown: 1500, cooldownOffset: 0),
                    new Shoot(9, 1, index: 1, fixedAngle: 70, cooldown: 1500, cooldownOffset: 200),
                    new Shoot(9, 1, index: 1, fixedAngle: 105, cooldown: 1500, cooldownOffset: 400),
                    new Shoot(9, 1, index: 1, fixedAngle: 140, cooldown: 1500, cooldownOffset: 600),
                    new Shoot(9, 1, index: 1, fixedAngle: 175, cooldown: 1500, cooldownOffset: 800),
                    new Shoot(9, 1, index: 1, fixedAngle: 210, cooldown: 1500, cooldownOffset: 1000),
                    new Shoot(9, 1, index: 1, fixedAngle: 245, cooldown: 1500, cooldownOffset: 1200),
                    new Shoot(9, 1, index: 1, fixedAngle: 280, cooldown: 1500, cooldownOffset: 1400),
                    new Shoot(9, 1, index: 1, fixedAngle: 315, cooldown: 1500, cooldownOffset: 1600),
                    new Shoot(9, 1, index: 1, fixedAngle: 359, cooldown: 1500, cooldownOffset: 1800),
                    new HpLessTransition(0.5, "Active2")
                ),
                new State("Active2",
                    new SetAltTexture(0),
                    new Prioritize(
                        new Follow(0.7, 10, 2, 6000, 3000),
                        new Orbit(0.6, 3, 10)
                    ),
                    new Shoot(8, 1, index: 0, predictive: 1, cooldown: 700),
                    new Shoot(9, 2, 35, 1, predictive: 1, cooldown: 1500, cooldownOffset: 0),
                    new HpLessTransition(0.25, "Phase4")
                ),
                new State("Phase4",
                    new Prioritize(
                        new Follow(0.7, 10, 2, 6000, 3000),
                        new Orbit(0.6, 3, 10)
                    ),
                    new Shoot(8, 1, index: 0, predictive: 1, cooldown: 700),
                    new Shoot(9, 1, index: 1, fixedAngle: 35, cooldown: 1500, cooldownOffset: 0),
                    new Shoot(9, 1, index: 1, fixedAngle: 70, cooldown: 1500, cooldownOffset: 200),
                    new Shoot(9, 1, index: 1, fixedAngle: 105, cooldown: 1500, cooldownOffset: 400),
                    new Shoot(9, 1, index: 1, fixedAngle: 140, cooldown: 1500, cooldownOffset: 600),
                    new Shoot(9, 1, index: 1, fixedAngle: 175, cooldown: 1500, cooldownOffset: 800),
                    new Shoot(9, 1, index: 1, fixedAngle: 210, cooldown: 1500, cooldownOffset: 1000),
                    new Shoot(9, 1, index: 1, fixedAngle: 245, cooldown: 1500, cooldownOffset: 1200),
                    new Shoot(9, 1, index: 1, fixedAngle: 280, cooldown: 1500, cooldownOffset: 1400),
                    new Shoot(9, 1, index: 1, fixedAngle: 315, cooldown: 1500, cooldownOffset: 1600),
                    new Shoot(9, 1, index: 1, fixedAngle: 359, cooldown: 1500, cooldownOffset: 1800)
                )
            );
            db.Init("Arena Statue Left",
                new ScaleHP2(35),
                new State("Ini",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new SetAltTexture(1)
                ),
                new State("Active",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(0, "Active_fix")
                ),
                new State("Active_fix",
                    new SetAltTexture(0),
                    new ChangeSize(1, 200),
                    new Prioritize(
                        new Follow(0.7, 10, 2, 6000, 3000),
                        new Orbit(0.6, 3, 10)
                    ),
                    new Shoot(9, 2, 35, 0, predictive: 0.7f, cooldown: 1000),
                    new PlayerWithinTransition(1, "SpiralBlast", seeInvis: true),
                    new TimedTransition(2000, "SpiralBlast")
                ),
                new State("SpiralBlast",
                    new Shoot(24, 1, index: 0, fixedAngle: 0, cooldown: 10000, cooldownOffset: 0),
                    new Shoot(24, 1, index: 0, fixedAngle: 45, cooldown: 10000, cooldownOffset: 100),
                    new Shoot(24, 1, index: 0, fixedAngle: 90, cooldown: 10000, cooldownOffset: 200),
                    new Shoot(24, 1, index: 0, fixedAngle: 135, cooldown: 10000, cooldownOffset: 300),
                    new Shoot(24, 1, index: 0, fixedAngle: 180, cooldown: 10000, cooldownOffset: 400),
                    new Shoot(24, 1, index: 0, fixedAngle: 225, cooldown: 10000, cooldownOffset: 500),
                    new Shoot(24, 1, index: 0, fixedAngle: 270, cooldown: 10000, cooldownOffset: 600),
                    new Shoot(24, 1, index: 0, fixedAngle: 315, cooldown: 10000, cooldownOffset: 700),
                    new Shoot(24, 1, index: 0, fixedAngle: 0, cooldown: 10000, cooldownOffset: 800),
                    new Shoot(24, 1, index: 0, fixedAngle: 45, cooldown: 10000, cooldownOffset: 900),
                    new Shoot(24, 1, index: 0, fixedAngle: 90, cooldown: 10000, cooldownOffset: 1000),
                    new Shoot(24, 1, index: 0, fixedAngle: 135, cooldown: 10000, cooldownOffset: 1100),
                    new Shoot(24, 1, index: 0, fixedAngle: 180, cooldown: 10000, cooldownOffset: 1200),
                    new Shoot(24, 1, index: 0, fixedAngle: 225, cooldown: 10000, cooldownOffset: 1300),
                    new Shoot(24, 1, index: 0, fixedAngle: 270, cooldown: 10000, cooldownOffset: 1400),
                    new Shoot(24, 1, index: 0, fixedAngle: 315, cooldown: 10000, cooldownOffset: 1500),
                    new TimedTransition(1800, "Active")
                )
            );
        }
    }
}
