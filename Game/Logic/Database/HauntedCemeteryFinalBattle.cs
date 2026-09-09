using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Conditionals;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    public class HauntedCemeteryFinalBattle : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Zombie Hulk",
                new Wander(0.35f),
                new State("Attack2",
                    new SetAltTexture(0),
                    new StayBack(0.55, 7),
                    new Follow(0.3, 11, 5),
                    new Shoot(10, 1, index: 1, cooldown: 400, cooldownOffset: 500),
                    new TimedRandomTransition(3000, true, "Attack1", "Attack3")
                ),
                new State("Attack1",
                    new SetAltTexture(1),
                    new Shoot(8, 3, 40, 0, cooldown: 400, cooldownOffset: 250),
                    new Shoot(8, 2, 60, 0, cooldown: 400, cooldownOffset: 250),
                    new Charge(1.1, 11, 200),
                    new TimedRandomTransition(3000, true, "Attack2", "Attack3")
                ),
                new State("Attack3",
                    new SetAltTexture(0),
                    new Wander(0.4f),
                    new Shoot(10, 3, 30, 1, cooldown: 400, cooldownOffset: 500),
                    new TimedRandomTransition(3000, true, "Attack1", "Attack2")
                ),
                new Threshold(0.01f,
                    new TierLoot(5, TierLoot.LootType.Weapon, 0.4f),
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.4f),
                    new TierLoot(5, TierLoot.LootType.Armor, 0.4f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.4f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.25f)
                )
            );
            db.Init("Classic Ghost",
                new Wander(0.4f),
                new Follow(0.55, 10, 3, 1000, 2000),
                new Orbit(0.55, 4, 7),
                new Shoot(5, 4, 16, 0, cooldown: 1000)
            );
            db.Init("Werewolf",
                new Spawn("Mini Werewolf", 2, 0, 5400),
                new Spawn("Mini Werewolf", 3, 0, 5400),
                new StayCloseToSpawn(0.8, 6),
                new State("Circling",
                    new Shoot(10, 3, 20, 0, cooldown: 1600),
                    new Prioritize(
                        new Orbit(0.4, 5.4, 8),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(3400, "Engaging")
                ),
                new State("Engaging",
                    new Shoot(5.5f, 5, 13, 0, cooldown: 1600),
                    new Follow(0.6, 10, 1),
                    new TimedTransition(2600, "Circling")
                )
            );
            db.Init("Mini Werewolf",
                new Shoot(4, 1, index: 0, cooldown: 1000),
                new Prioritize(
                    new Follow(0.6, 15, 1),
                    new Protect(0.8, "Werewolf", 15, 6, 3),
                    new Wander(0.4f)
                )
            );
            db.Init("Ghost of Skuld",
                new State("wait1",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Taunt("Hello Heroes!"),
                    new TimedTransition(4500, "wait2")
                ),
                new State("wait2",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Taunt("Surviving every wave of undead that came at you, its impressive!"),
                    new TimedTransition(4500, "wait3")
                ),
                new State("wait3",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Taunt("You deserve a reward!"),
                    new TimedTransition(4500, "skuld1")
                ),
                new State("skuld1",
                    new SetAltTexture(1),
                    new Taunt(1, "Your reward is....A SWIFT DEATH!"),
                    new RingAttack(30, 20, 0, 2, 10, 10, 2000),
                    new TossObject("Flying Flame Skull", 6, 270, 9999),
                    new TossObject("Flying Flame Skull", 6, 90, 9999),
                    new TimedTransition(4000, "gotem")
                ),
                new State("gotem",
                    new Shoot(10, 7, 5, 0, cooldown: 700),
                    new RingAttack(30, 1, 0, 2, 0.4, 0, 150),
                    new Shoot(10, 1, index: 0, cooldown: 1200),
                    new Shoot(12, 1, index: 2, fixedAngle: 0, cooldown: 4000),
                    new Shoot(12, 1, index: 2, fixedAngle: 90, cooldown: 4000),
                    new Shoot(12, 1, index: 2, fixedAngle: 180, cooldown: 4000),
                    new Shoot(12, 1, index: 2, fixedAngle: 270, cooldown: 4000),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(8000, "telestart")
                ),
                new State("blam1",
                    new SetAltTexture(1),
                    new Shoot(10, 7, 12, 0, cooldown: 1000),
                    new Shoot(10, 2, 16, 2, cooldown: 2000),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 0, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 90, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 180, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 270, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 45, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 135, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 235, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 315, cooldown: 1400),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(4500, "tele7")
                ),
                new State("blam2",
                    new SetAltTexture(1),
                    new Shoot(10, 7, 12, 0, cooldown: 1000),
                    new Shoot(10, 2, 16, 2, cooldown: 2000),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 0, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 90, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 180, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 270, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 45, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 135, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 235, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 315, cooldown: 1400),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(4500, "tele5")
                ),
                new State("blam3",
                    new SetAltTexture(1),
                    new Shoot(10, 7, 12, 0, cooldown: 1000),
                    new Shoot(10, 2, 16, 2, cooldown: 2000),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 0, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 90, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 180, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 270, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 45, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 135, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 235, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 315, cooldown: 1400),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(4500, "tele4")
                ),
                new State("blam4",
                    new SetAltTexture(1),
                    new Shoot(10, 7, 12, 0, cooldown: 1000),
                    new Shoot(10, 2, 16, 2, cooldown: 2000),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 0, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 90, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 180, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 270, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 45, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 135, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 235, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 315, cooldown: 1400),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(4500, "tele6")
                ),
                new State("blam5",
                    new SetAltTexture(1),
                    new Shoot(10, 7, 12, 0, cooldown: 1000),
                    new Shoot(10, 2, 16, 2, cooldown: 2000),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 0, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 90, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 180, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 270, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 45, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 135, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 235, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 315, cooldown: 1400),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(4500, "tele2")
                ),
                new State("blam6",
                    new SetAltTexture(1),
                    new Shoot(10, 7, 12, 0, cooldown: 1000),
                    new Shoot(10, 2, 16, 2, cooldown: 2000),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 0, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 90, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 180, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 270, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 45, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 135, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 235, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 315, cooldown: 1400),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(4500, "tele3")
                ),
                new State("blam7",
                    new SetAltTexture(1),
                    new Shoot(10, 7, 12, 0, cooldown: 1000),
                    new Shoot(10, 2, 16, 2, cooldown: 2000),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 0, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 90, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 180, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 270, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 45, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 135, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 235, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 315, cooldown: 1400),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(4500, "tele8")
                ),
                new State("blam8",
                    new SetAltTexture(1),
                    new Shoot(10, 7, 12, 0, cooldown: 1000),
                    new Shoot(10, 2, 16, 2, cooldown: 2000),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 0, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 90, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 180, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 270, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 45, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 135, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 235, cooldown: 1400),
                    new Shoot(8.4f, 1, index: 1, fixedAngle: 315, cooldown: 1400),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(4500, "tele8")
                ),
                new State("telestart",
                    new SetAltTexture(1),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(2500, "tele1")
                ),
                new State("tele1",
                    new SetAltTexture(11),
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new MoveTo(2, 24, 18),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(1000, "blam2")
                ),
                new State("tele2",
                    new SetAltTexture(11),
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new MoveTo(2, 17, 26),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(1000, "blam3")
                ),
                new State("tele3",
                    new SetAltTexture(11),
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new MoveTo(2, 29, 19),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(1000, "blam4")
                ),
                new State("tele4",
                    new SetAltTexture(11),
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new MoveTo(2, 29, 29),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(1000, "blam5")
                ),
                new State("tele5",
                    new SetAltTexture(11),
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new MoveTo(2, 24, 18),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(1000, "blam6")
                ),
                new State("tele6",
                    new SetAltTexture(11),
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new MoveTo(2, 25, 35),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(1000, "blam7")
                ),
                new State("tele7",
                    new SetAltTexture(11),
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new MoveTo(2, 20, 29),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(1000, "blam8")
                ),
                new State("tele8",
                    new SetAltTexture(11),
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new MoveTo(2, 24, 24),
                    new HpLessTransition(0.1, "deadTask"),
                    new TimedTransition(1000, "gotem")
                ),
                new State("deadTask",
                    new RemoveEntity(9999, "Flying Flame Skull"),
                    new Suicide()
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
                new Threshold(0.001f,
                    new ItemLoot("RogueST0", 0.008f),
                    new ItemLoot("Potion of Vitality", 1),
                    new ItemLoot("Potion of Wisdom", 1),
                    new ItemLoot("Resurrected Warrior's Armor", 0.002f),
                    new ItemLoot("Plague Poison", 0.002f),
                    new ItemLoot("Cemetery Key", 0.008f)
                )
            );
            db.Init("Halloween Zombie Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("Leech"),
                new State("1",
                    new Spawn("Zombie Rise", 1),
                    new EntityNotExistsTransition("Ghost of Skuld", 100, "2")
                ),
                new State("2",
                    new Suicide()
                )
            );
            db.Init("Blue Zombie",
                new Follow(0.03, 100, 1),
                new State("1",
                    new Shoot(10, 1, index: 0, cooldown: 1000),
                    new EntityNotExistsTransition("Ghost of Skuld", 100, "2")
                ),
                new State("2",
                    new Suicide()
                )
            );
            db.Init("Flying Flame Skull",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new Orbit(1, 5, 20, "Ghost of Skuld"),
                new State("1",
                    new Shoot(100, 10, 36, 0, cooldown: 1000),
                    new EntityNotExistsTransition("Ghost of Skuld", 100, "2")
                ),
                new State("2",
                    new Suicide()
                )
            );
            db.Init("Zombie Rise",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new TransformOnDeath("Blue Zombie"),
                new State("1",
                    new SetAltTexture(1),
                    new TimedTransition(750, "2")
                ),
                new State("2",
                    new SetAltTexture(2),
                    new TimedTransition(750, "3")
                ),
                new State("3",
                    new SetAltTexture(3),
                    new TimedTransition(750, "4")
                ),
                new State("4",
                    new SetAltTexture(4),
                    new TimedTransition(750, "5")
                ),
                new State("5",
                    new SetAltTexture(5),
                    new TimedTransition(750, "6")
                ),
                new State("6",
                    new SetAltTexture(6),
                    new TimedTransition(750, "7")
                ),
                new State("7",
                    new SetAltTexture(7),
                    new TimedTransition(750, "8")
                ),
                new State("8",
                    new SetAltTexture(8),
                    new TimedTransition(750, "9")
                ),
                new State("9",
                    new SetAltTexture(9),
                    new TimedTransition(750, "10")
                ),
                new State("10",
                    new SetAltTexture(10),
                    new TimedTransition(750, "11")
                ),
                new State("11",
                    new SetAltTexture(11),
                    new TimedTransition(750, "12")
                ),
                new State("12",
                    new SetAltTexture(12),
                    new TimedTransition(750, "13")
                ),
                new State("13",
                    new SetAltTexture(13),
                    new TimedTransition(750, "14")
                ),
                new State("14",
                    new Suicide()
                )
            );
        }
    }
}
