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
    public class EntAncient : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Ent Ancient",
                new State("Idle",
                    new StayCloseToSpawn(0.1f, range: 6),
                    new Wander(0.1f),
                    new HpLessTransition(0.99999f, "EvaluationStart1")
                ),
                new State("EvaluationStart1",
                    new Taunt("Uhh. So... sleepy..."),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Prioritize(
                        new StayCloseToSpawn(0.2f, range: 3),
                        new Wander(0.2f)
                    ),
                    new TimedTransition(2500, "EvaluationStart2")
                ),
                new State("EvaluationStart2",
                    new Flash(0x0000ff, 0.1f, 60),
                    new Shoot(10, count: 2, shootAngle: 180, cooldown: 800),
                    new Prioritize(
                        new StayCloseToSpawn(0.3f, range: 5),
                        new Wander(0.3f)
                    ),
                    new HpLessTransition(0.87f, "EvaluationEnd"),
                    new TimedTransition(6000, "EvaluationEnd")
                ),
                new State("EvaluationEnd",
                    new HpLessTransition(0.875f, "HugeMob"),
                    new HpLessTransition(0.952f, "Mob"),
                    new HpLessTransition(0.985f, "SmallGroup"),
                    new HpLessTransition(0.99999f, "Solo")
                ),
                new State("HugeMob",
                    new Taunt("You are many, yet the sum of your years is nothing."),
                    new Spawn("Greater Nature Sprite", maxChildren: 6, initialSpawn: 0, cooldown: 400),
                    new TossObject("Ent", range: 3, angle: 0, cooldown: 100000),
                    new TossObject("Ent", range: 3, angle: 180, cooldown: 100000),
                    new TossObject("Ent", range: 5, angle: 10, cooldown: 100000),
                    new TossObject("Ent", range: 5, angle: 190, cooldown: 100000),
                    new TossObject("Ent", range: 5, angle: 70, cooldown: 100000),
                    new TossObject("Ent", range: 7, angle: 20, cooldown: 100000),
                    new TossObject("Ent", range: 7, angle: 200, cooldown: 100000),
                    new TossObject("Ent", range: 7, angle: 80, cooldown: 100000),
                    new TossObject("Ent", range: 10, angle: 30, cooldown: 100000),
                    new TossObject("Ent", range: 10, angle: 210, cooldown: 100000),
                    new TossObject("Ent", range: 10, angle: 90, cooldown: 100000),
                    new TimedTransition(5000, "Wait")
                ),
                new State("Mob",
                    new Taunt("Little flies, little flies... we will swat you."),
                    new Spawn("Greater Nature Sprite", maxChildren: 3, initialSpawn: 0, cooldown: 1000),
                    new TossObject("Ent", range: 3, angle: 0, cooldown: 100000),
                    new TossObject("Ent", range: 4, angle: 180, cooldown: 100000),
                    new TossObject("Ent", range: 5, angle: 10, cooldown: 100000),
                    new TossObject("Ent", range: 6, angle: 190, cooldown: 100000),
                    new TossObject("Ent", range: 7, angle: 20, cooldown: 100000),
                    new TimedTransition(5000, "Wait")
                ),
                new State("SmallGroup",
                    new Taunt("It will be trivial to dispose of you."),
                    new Spawn("Greater Nature Sprite", maxChildren: 1, initialSpawn: 1, cooldown: 100000),
                    new TossObject("Ent", range: 3, angle: 0, cooldown: 100000),
                    new TossObject("Ent", range: 4.5f, angle: 180, cooldown: 100000),
                    new TimedTransition(3000, "Wait")
                ),
                new State("Solo",
                    new Taunt("Mmm? Did you say something, mortal?"),
                    new TimedTransition(3000, "Wait")
                ),
                new State("Wait",
                    new Transform("Actual Ent Ancient")
                )
            );
            db.Init("Actual Ent Ancient",
                new Prioritize(
                    new StayCloseToSpawn(0.2f, range: 6),
                    new Wander(0.2f)
                ),
                new Spawn("Ent Sapling", maxChildren: 3, initialSpawn: 0, cooldown: 3000),
                new State("Start",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 160),
                    new Shoot(10, index: 0, count: 1),
                    new TimedTransition(1600, "Growing1"),
                    new HpLessTransition(0.9f, "Growing1")
                ),
                new State("Growing1",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 180),
                    new Shoot(10, index: 1, count: 3, shootAngle: 120),
                    new TimedTransition(1600, "Growing2"),
                    new HpLessTransition(0.8f, "Growing2")
                ),
                new State("Growing2",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 200),
                    new Taunt(0.35f, "Little mortals, your years are my minutes."),
                    new Shoot(10, index: 2, count: 1),
                    new TimedTransition(1600, "Growing3"),
                    new HpLessTransition(0.7f, "Growing3")
                ),
                new State("Growing3",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 220),
                    new Shoot(10, index: 3, count: 1),
                    new TimedTransition(1600, "Growing4"),
                    new HpLessTransition(0.6f, "Growing4")
                ),
                new State("Growing4",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 240),
                    new Taunt(0.35f, "No axe can fell me!"),
                    new Shoot(10, index: 4, count: 3, shootAngle: 120),
                    new TimedTransition(1600, "Growing5"),
                    new HpLessTransition(0.5f, "Growing5")
                ),
                new State("Growing5",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 260),
                    new Shoot(10, index: 5, count: 1),
                    new TimedTransition(1600, "Growing6"),
                    new HpLessTransition(0.45f, "Growing6")
                ),
                new State("Growing6",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 280),
                    new Taunt(0.35f, "Yes, YES..."),
                    new Shoot(10, index: 6, count: 1),
                    new TimedTransition(1600, "Growing7"),
                    new HpLessTransition(0.4f, "Growing7")
                ),
                new State("Growing7",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 300),
                    new Shoot(10, index: 7, count: 3, shootAngle: 120),
                    new TimedTransition(1600, "Growing8"),
                    new HpLessTransition(0.36f, "Growing8")
                ),
                new State("Growing8",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new ChangeSize(11, 320),
                    new Taunt(0.35f, "I am the FOREST!!"),
                    new Shoot(10, index: 8, count: 1),
                    new TimedTransition(1600, "Growing9"),
                    new HpLessTransition(0.32f, "Growing9")
                ),
                new State("Growing9",
                    new ChangeSize(11, 340),
                    new Taunt(1.0f, "YOU WILL DIE!!!"),
                    new Shoot(10, index: 9, count: 1),
                    new State("convert_sprites",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Order(50, "Greater Nature Sprite", "Transform"),
                        new TimedTransition(2000, "shielded")
                    ),
                    new State("received_armor",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new ConditionalEffect(ConditionEffectIndex.Armored, perm: true),
                        new TimedTransition(1000, "shielded")
                    ),
                    new State("shielded",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(4000, "unshielded")
                    ),
                    new State("unshielded",
                        new Shoot(10, index: 3, count: 3, shootAngle: 120, cooldown: 700),
                        new TimedTransition(4000, "shielded")
                    )
                ),
                new Threshold(0.001f,
                    new TierLoot(2, TierLoot.LootType.Ring, 0.15f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.04f),
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.3f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.3f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(1, TierLoot.LootType.Ability, 0.95f),
                    new TierLoot(2, TierLoot.LootType.Ability, 0.25f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.05f)
                ),
                new ItemLoot("Health Potion", 0.7f),
                new ItemLoot("Magic Potion", 0.7f)
            );
            db.Init("Ent",
                new Prioritize(
                    new Protect(0.25f, "Ent Ancient", acquireRange: 12, protectionRange: 7, reprotectRange: 7),
                    new Follow(0.25f, range: 1, acquireRange: 9),
                    new Shoot(10, count: 5, shootAngle: 72, fixedAngle: 30, cooldown: 1600, cooldownOffset: 800)
                ),
                new Shoot(10, predictive: 0.4f, cooldown: 600),
                new Decay(90000),
                new ItemLoot("Tincture of Dexterity", 0.02f)
            );
            db.Init("Ent Sapling",
                new Prioritize(
                    new Protect(0.55f, "Ent Ancient", acquireRange: 10, protectionRange: 4, reprotectRange: 4),
                    new Wander(0.55f)
                ),
                new Shoot(10, cooldown: 1000)
            );
            db.Init("Greater Nature Sprite",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new Shoot(10, count: 4, shootAngle: 10),
                new Prioritize(
                    new StayCloseToSpawn(1.5f, 11),
                    new Orbit(1.5f, 4, acquireRange: 7),
                    new Follow(200, acquireRange: 7, range: 2),
                    new Follow(0.3f, acquireRange: 7, range: 0.2f)
                ),
                new State("Idle"),
                new State("Transform",
                    new Transform("Actual Greater Nature Sprite")
                ),
                new Decay(90000)
            );
            db.Init("Actual Greater Nature Sprite",
                new Flash(0xff484848, 0.6f, 1000),
                new Spawn("Ent", maxChildren: 2, initialSpawn: 0, cooldown: 3000),
                new HealGroup(15, "Heros", cooldown: 200),
                new State("armor_ent_ancient",
                    new Order(30, "Actual Ent Ancient", "received_armor"),
                    new TimedTransition(1000, "last_fight")
                ),
                new State("last_fight",
                    new Shoot(10, count: 4, shootAngle: 10),
                    new Prioritize(
                        new StayCloseToSpawn(1.5f, 11),
                        new Orbit(1.5f, 4, acquireRange: 7),
                        new Follow(200, acquireRange: 7, range: 2),
                        new Follow(0.3f, acquireRange: 7, range: 0.2f)
                    )
                ),
                new Decay(60000),
                new ItemLoot("Magic Potion", 0.25f),
                new Threshold(0.01f,
                    new ItemLoot("Tincture of Life", 0.06f),
                    new ItemLoot("Green Drake Egg", 0.08f),
                    new ItemLoot("Quiver of Thunder", 0.002f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.3f)
                )
            );
        }
    }
}
