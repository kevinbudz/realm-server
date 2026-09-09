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
    public class Lich : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Lich",
                new State("Idle",
                    new StayCloseToSpawn(0.5f, range: 5),
                    new Wander(0.5f),
                    new HpLessTransition(0.99999f, "EvaluationStart1")
                ),
                new State("EvaluationStart1",
                    new Taunt("New recruits for my undead army? How delightful!"),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Prioritize(
                        new StayCloseToSpawn(0.35f, range: 5),
                        new Wander(0.35f)
                    ),
                    new TimedTransition(2500, "EvaluationStart2")
                ),
                new State("EvaluationStart2",
                    new Flash(0x0000ff, 0.1f, 60),
                    new Prioritize(
                        new StayCloseToSpawn(0.35f, range: 5),
                        new Wander(0.35f)
                    ),
                    new Shoot(10, index: 1, count: 3, shootAngle: 120, cooldown: 100000,
                        cooldownOffset: 200),
                    new Shoot(10, index: 1, count: 3, shootAngle: 120, cooldown: 100000,
                        cooldownOffset: 400),
                    new Shoot(10, index: 1, count: 3, shootAngle: 120, cooldown: 100000,
                        cooldownOffset: 2200),
                    new Shoot(10, index: 1, count: 3, shootAngle: 120, cooldown: 100000,
                        cooldownOffset: 2400),
                    new Shoot(10, index: 1, count: 3, shootAngle: 120, cooldown: 100000,
                        cooldownOffset: 4200),
                    new Shoot(10, index: 1, count: 3, shootAngle: 120, cooldown: 100000,
                        cooldownOffset: 4400),
                    new HpLessTransition(0.87f, "EvaluationEnd"),
                    new TimedTransition(6000, "EvaluationEnd")
                ),
                new State("EvaluationEnd",
                    new Taunt("Time to meet your future brothers and sisters..."),
                    new HpLessTransition(0.875f, "HugeMob"),
                    new HpLessTransition(0.952f, "Mob"),
                    new HpLessTransition(0.985f, "SmallGroup"),
                    new HpLessTransition(0.99999f, "Solo")
                ),
                new State("HugeMob",
                    new Taunt("...there's an ARMY of them! HahaHahaaaa!!!"),
                    new Flash(0x00ff00, 0.2f, 300),
                    new Spawn("Haunted Spirit", maxChildren: 5, initialSpawn: 0, cooldown: 3000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 0, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 120, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 240, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 3, angle: 60, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 3, angle: 180, cooldown: 100000),
                    new Prioritize(
                        new Protect(0.9f, "Phylactery Bearer", acquireRange: 15, protectionRange: 2,
                            reprotectRange: 2),
                        new Wander(0.9f)
                    ),
                    new TimedTransition(25000, "HugeMob2")
                ),
                new State("HugeMob2",
                    new Taunt("My minions have stolen your life force and fed it to me!"),
                    new Flash(0x00ff00, 0.2f, 300),
                    new Spawn("Haunted Spirit", maxChildren: 5, initialSpawn: 0, cooldown: 3000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 0, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 120, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 240, cooldown: 100000),
                    new Prioritize(
                        new Protect(0.9f, "Phylactery Bearer", acquireRange: 15, protectionRange: 2,
                            reprotectRange: 2),
                        new Wander(0.9f)
                    ),
                    new TimedTransition(5000, "Wait")
                ),
                new State("Mob",
                    new Taunt("...there's a lot of them! Hahaha!!"),
                    new Flash(0x00ff00, 0.2f, 300),
                    new Spawn("Haunted Spirit", maxChildren: 2, initialSpawn: 0, cooldown: 2000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 0, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 120, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 240, cooldown: 100000),
                    new Prioritize(
                        new Protect(0.9f, "Phylactery Bearer", acquireRange: 15, protectionRange: 2,
                            reprotectRange: 2),
                        new Wander(0.9f)
                    ),
                    new TimedTransition(22000, "Mob2")
                ),
                new State("Mob2",
                    new Taunt("My minions have stolen your life force and fed it to me!"),
                    new Spawn("Haunted Spirit", maxChildren: 2, initialSpawn: 0, cooldown: 2000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 0, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 120, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 240, cooldown: 100000),
                    new Prioritize(
                        new Protect(0.9f, "Phylactery Bearer", acquireRange: 15, protectionRange: 2,
                            reprotectRange: 2),
                        new Wander(0.9f)
                    ),
                    new TimedTransition(5000, "Wait")
                ),
                new State("SmallGroup",
                    new Taunt("...and there's more where they came from!"),
                    new Flash(0x00ff00, 0.2f, 300),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 0, cooldown: 100000),
                    new TossObject("Phylactery Bearer", range: 5.5f, angle: 240, cooldown: 100000),
                    new Prioritize(
                        new Protect(0.9f, "Phylactery Bearer", acquireRange: 15, protectionRange: 2,
                            reprotectRange: 2),
                        new Wander(0.9f)
                    ),
                    new TimedTransition(15000, "SmallGroup2")
                ),
                new State("SmallGroup2",
                    new Taunt("My minions have stolen your life force and fed it to me!"),
                    new Spawn("Haunted Spirit", maxChildren: 1, initialSpawn: 1, cooldown: 9000),
                    new Prioritize(
                        new Protect(0.9f, "Phylactery Bearer", acquireRange: 15, protectionRange: 2,
                            reprotectRange: 2),
                        new Wander(0.9f)
                    ),
                    new TimedTransition(5000, "Wait")
                ),
                new State("Solo",
                    new Taunt("...it's a small family, but you'll enjoy being part of it!"),
                    new Flash(0x00ff00, 0.2f, 10),
                    new Wander(0.5f),
                    new TimedTransition(3000, "Wait")
                ),
                new State("Wait",
                    new Taunt("Kneel before me! I am the master of life and death!"),
                    new Transform("Actual Lich")
                )
            );
            db.Init("Actual Lich",
                new Prioritize(
                    new Protect(0.9f, "Phylactery Bearer", acquireRange: 15, protectionRange: 2, reprotectRange: 2),
                    new Wander(0.5f)
                ),
                new Spawn("Mummy", maxChildren: 4, cooldown: 4000),
                new Spawn("Mummy King", maxChildren: 2, cooldown: 4000),
                new Spawn("Mummy Pharaoh", maxChildren: 1, cooldown: 4000),
                new State("typeA",
                    new Shoot(10, index: 0, count: 2, shootAngle: 7, cooldown: 800),
                    new TimedTransition(8000, "typeB")
                ),
                new State("typeB",
                    new Taunt(0.7f, "All that I touch turns to dust!",
                        "You will drown in a sea of undead!"
                    ),
                    new Shoot(10, index: 1, count: 4, shootAngle: 7, cooldown: 1000),
                    new Shoot(10, index: 0, count: 2, shootAngle: 7, cooldown: 800),
                    new TimedTransition(6000, "typeA")
                ),
                new TierLoot(2, TierLoot.LootType.Ring, 0.11f),
                new TierLoot(3, TierLoot.LootType.Ring, 0.01f),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.3f),
                new TierLoot(6, TierLoot.LootType.Weapon, 0.2f),
                new TierLoot(7, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.3f),
                new TierLoot(6, TierLoot.LootType.Armor, 0.2f),
                new TierLoot(7, TierLoot.LootType.Armor, 0.05f),
                new TierLoot(1, TierLoot.LootType.Ability, 0.9f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.15f),
                new TierLoot(3, TierLoot.LootType.Ability, 0.02f),
                new ItemLoot("Health Potion", 0.4f),
                new ItemLoot("Magic Potion", 0.4f)
            );
            db.Init("Phylactery Bearer",
                new HealGroup(15, "Heros", cooldown: 200),
                new State("Attack1",
                    new Shoot(10, index: 0, count: 3, shootAngle: 120, cooldown: 900, cooldownOffset: 400),
                    new State("AttackX",
                        new Prioritize(
                            new StayCloseToSpawn(0.55f, range: 5),
                            new Orbit(0.55f, 4, acquireRange: 5)
                        ),
                        new TimedTransition(1500, "AttackY")
                    ),
                    new State("AttackY",
                        new Taunt(0.05f, "We feed the master!"),
                        new Prioritize(
                            new StayCloseToSpawn(0.55f, range: 5),
                            new StayBack(0.55f, distance: 2),
                            new Wander(0.55f)
                        ),
                        new TimedTransition(1500, "AttackX")
                    ),
                    new HpLessTransition(0.65f, "Attack2")
                ),
                new State("Attack2",
                    new Shoot(10, index: 0, count: 3, shootAngle: 15, predictive: 0.1f, cooldown: 600,
                        cooldownOffset: 200),
                    new State("AttackX",
                        new Prioritize(
                            new StayCloseToSpawn(0.65f, range: 5),
                            new Orbit(0.65f, 4, acquireRange: 10)
                        ),
                        new TimedTransition(1500, "AttackY")
                    ),
                    new State("AttackY",
                        new Taunt(0.05f, "We feed the master!"),
                        new Prioritize(
                            new StayCloseToSpawn(0.65f, range: 5),
                            new Buzz(),
                            new Wander(0.65f)
                        ),
                        new TimedTransition(1500, "AttackX")
                    ),
                    new HpLessTransition(0.3f, "Attack3")
                ),
                new State("Attack3",
                    new Shoot(10, index: 1, cooldown: 800),
                    new State("AttackX",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Prioritize(
                            new StayCloseToSpawn(1.3f, range: 5),
                            new Wander(1.3f)
                        ),
                        new TimedTransition(2500, "AttackY")
                    ),
                    new State("AttackY",
                        new Taunt(0.02f, "We feed the master!"),
                        new Prioritize(
                            new StayCloseToSpawn(1, range: 5),
                            new Wander(1)
                        ),
                        new TimedTransition(2500, "AttackX")
                    )
                ),
                new Decay(130000),
                new ItemLoot("Tincture of Defense", 0.02f),
                new ItemLoot("Orange Drake Egg", 0.06f),
                new ItemLoot("Magic Potion", 0.03f)
            );
            db.Init("Haunted Spirit",
                new State("NewLocation",
                    new Taunt(0.1f, "XxxXxxxXxXxXxxx..."),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Shoot(10, predictive: 0.2f, cooldown: 700),
                    new Prioritize(
                        new StayCloseToSpawn(1, range: 11),
                        new Wander(1)
                    ),
                    new TimedTransition(7000, "Attack")
                ),
                new State("Attack",
                    new Taunt(0.1f, "Hungry..."),
                    new Shoot(10, predictive: 0.3f, cooldown: 700),
                    new Shoot(10, count: 2, shootAngle: 70, cooldown: 700, cooldownOffset: 200),
                    new TimedTransition(3000, "NewLocation")
                ),
                new Decay(90000),
                new TierLoot(8, TierLoot.LootType.Weapon, 0.02f),
                new ItemLoot("Magic Potion", 0.02f),
                new ItemLoot("Ring of Magic", 0.02f),
                new ItemLoot("Ring of Attack", 0.02f),
                new ItemLoot("Tincture of Dexterity", 0.06f),
                new ItemLoot("Tincture of Mana", 0.09f),
                new ItemLoot("Tincture of Life", 0.04f)
            );
            db.Init("Mummy",
                new Prioritize(
                    new Protect(1, "Lich", protectionRange: 10),
                    new Follow(1.2f, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(10),
                new ItemLoot("Magic Potion", 0.02f),
                new ItemLoot("Spirit Salve Tome", 0.02f)
            );
            db.Init("Mummy King",
                new Prioritize(
                    new Protect(1, "Lich", protectionRange: 10),
                    new Follow(1.2f, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(10),
                new ItemLoot("Magic Potion", 0.02f),
                new ItemLoot("Spirit Salve Tome", 0.02f)
            );
            db.Init("Mummy Pharaoh",
                new Prioritize(
                    new Protect(1, "Lich", protectionRange: 10),
                    new Follow(1.2f, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(10),
                new ItemLoot("Hell's Fire Wand", 0.02f),
                new ItemLoot("Slayer Staff", 0.02f),
                new ItemLoot("Golden Sword", 0.02f),
                new ItemLoot("Golden Dagger", 0.02f)
            );
        }
    }
}
