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
    public class GhostKing : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Ghost King",
                new State("Idle",
                    new BackAndForth(0.3f, 3),
                    new HpLessTransition(0.99999f, "EvaluationStart1")
                ),
                new State("EvaluationStart1",
                    new Taunt("No corporeal creature can kill my sorrow"),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Prioritize(
                        new StayCloseToSpawn(0.4f, range: 3),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(2500, "EvaluationStart2")
                ),
                new State("EvaluationStart2",
                    new Flash(0x0000ff, 0.1f, 60),
                    new ChangeSize(20, 140),
                    new Shoot(10, count: 4, shootAngle: 30, defaultAngle: 0, cooldown: 1000),
                    new Prioritize(
                        new StayCloseToSpawn(0.4f, range: 3),
                        new Wander(0.4f)
                    ),
                    new HpLessTransition(0.87f, "EvaluationEnd"),
                    new TimedTransition(6000, "EvaluationEnd")
                ),
                new State("EvaluationEnd",
                    new Taunt(0.5f, "Aye, let's be miserable together"),
                    new HpLessTransition(0.875f, "HugeMob"),
                    new HpLessTransition(0.952f, "Mob"),
                    new HpLessTransition(0.985f, "SmallGroup"),
                    new HpLessTransition(0.99999f, "Solo")
                ),
                new State("HugeMob",
                    new Taunt("What a HUGE MOB!"),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0x00ff00, 0.2f, 300),
                    new TossObject("Small Ghost", range: 4, angle: 0, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 60, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 120, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 180, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 240, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 300, cooldown: 100000),
                    new TimedTransition(30000, "HugeMob2")
                ),
                new State("HugeMob2",
                    new Taunt("I feel almost manic!"),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0x00ff00, 0.2f, 300),
                    new TossObject("Small Ghost", range: 4, angle: 0, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 60, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 120, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 180, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 240, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 300, cooldown: 100000),
                    new TimedTransition(30000, "Company")
                ),
                new State("Mob",
                    new Taunt("There's a MOB of you."),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0x00ff00, 0.2f, 300),
                    new TossObject("Small Ghost", range: 4, angle: 0, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 60, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 120, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 180, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 240, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 300, cooldown: 100000),
                    new TimedTransition(30000, "Company")
                ),
                new State("Company",
                    new Taunt("Misery loves company!"),
                    new TossObject("Ghost Master", range: 4, angle: 0, cooldown: 100000),
                    new TossObject("Medium Ghost", range: 4, angle: 60, cooldown: 100000),
                    new TossObject("Medium Ghost", range: 4, angle: 120, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 180, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 240, cooldown: 100000),
                    new TossObject("Large Ghost", range: 4, angle: 300, cooldown: 100000),
                    new TimedTransition(2000, "Wait")
                ),
                new State("SmallGroup",
                    new Taunt("Such a small party."),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0x00ff00, 0.2f, 300),
                    new TossObject("Small Ghost", range: 4, angle: 0, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 60, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 120, cooldown: 100000),
                    new TossObject("Medium Ghost", range: 4, angle: 180, cooldown: 100000),
                    new TossObject("Medium Ghost", range: 4, angle: 240, cooldown: 100000),
                    new TossObject("Medium Ghost", range: 4, angle: 300, cooldown: 100000),
                    new TimedTransition(30000, "SmallGroup2")
                ),
                new State("SmallGroup2",
                    new Taunt("Misery loves company!"),
                    new TossObject("Ghost Master", range: 4, angle: 0, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 60, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 120, cooldown: 100000),
                    new TossObject("Medium Ghost", range: 4, angle: 180, cooldown: 100000),
                    new TossObject("Medium Ghost", range: 4, angle: 240, cooldown: 100000),
                    new TossObject("Medium Ghost", range: 4, angle: 300, cooldown: 100000),
                    new TimedTransition(2000, "Wait")
                ),
                new State("Solo",
                    new Taunt("Just you?  I guess you don't have any friends to play with."),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0x00ff00, 0.2f, 10),
                    new TossObject("Ghost Master", range: 4, angle: 0, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 70, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 140, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 210, cooldown: 100000),
                    new TossObject("Small Ghost", range: 4, angle: 280, cooldown: 100000),
                    new TimedTransition(1000, "Wait")
                ),
                new State("Wait",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0x00ff00, 0.2f, 10000),
                    new Prioritize(
                        new StayCloseToSpawn(1, range: 8),
                        new Follow(0.6f, range: 2, duration: 2000, cooldown: 2000)
                    ),
                    new Shoot(10, cooldown: 1000),
                    new State("Speak",
                        new Taunt("I cannot be defeated while my loyal subjects sustain me!"),
                        new TimedTransition(1000, "Quiet")
                    ),
                    new State("Quiet",
                        new TimedTransition(22000, "Speak")
                    ),
                    new TimedTransition(140000, "Overly_long_combat")
                ),
                new State("Overly_long_combat",
                    new Taunt("You have sapped my energy. A curse on you!"),
                    new Prioritize(
                        new StayCloseToSpawn(1, range: 8),
                        new Follow(0.6f, range: 2, duration: 2000, cooldown: 2000)
                    ),
                    new Shoot(10, cooldown: 1000),
                    new Order(30, "Ghost Master", "Decay"),
                    new Order(30, "Small Ghost", "Decay"),
                    new Order(30, "Medium Ghost", "Decay"),
                    new Order(30, "Large Ghost", "Decay"),
                    new Transform("Actual Ghost King")
                ),
                new State("Killed",
                    new Taunt("I feel my flesh again! For the first time in a 1000 years I LIVE!"),
                    new Taunt(0.5f, "Will you release me?"),
                    new Transform("Actual Ghost King")
                )
            );
            db.Init("Ghost Master",
                new State("Attack1",
                    new State("NewLocation1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Flash(0xff00ff00, 0.2f, 10),
                        new Prioritize(
                            new StayCloseToSpawn(2, range: 7),
                            new Wander(2)
                        ),
                        new TimedTransition(1000, "Att1")
                    ),
                    new State("Att1",
                        new Shoot(10, count: 4, shootAngle: 90, fixedAngle: 0, cooldown: 400),
                        new TimedTransition(9000, "NewLocation1")
                    ),
                    new HpLessTransition(0.99f, "Attack2")
                ),
                new State("Attack2",
                    new State("Intro",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Flash(0xff00ff00, 0.2f, 10),
                        new ChangeSize(20, 140),
                        new TimedTransition(1000, "NewLocation2")
                    ),
                    new State("NewLocation2",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Flash(0xff00ff00, 0.2f, 10),
                        new Prioritize(
                            new StayCloseToSpawn(2, range: 7),
                            new Wander(2)
                        ),
                        new TimedTransition(1000, "Att2")
                    ),
                    new State("Att2",
                        new Shoot(10, count: 4, shootAngle: 90, fixedAngle: 45, cooldown: 400),
                        new TimedTransition(6000, "NewLocation2")
                    ),
                    new HpLessTransition(0.98f, "Attack3")
                ),
                new State("Attack3",
                    new State("Intro",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Flash(0xff00ff00, 0.2f, 10),
                        new ChangeSize(20, 180),
                        new TimedTransition(1000, "NewLocation3")
                    ),
                    new State("NewLocation3",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Flash(0xff00ff00, 0.2f, 10),
                        new Prioritize(
                            new StayCloseToSpawn(2, range: 7),
                            new Wander(2)
                        ),
                        new TimedTransition(1000, "Att3")
                    ),
                    new State("Att3",
                        new Shoot(10, count: 4, shootAngle: 90, fixedAngle: 22.5f, cooldown: 400),
                        new TimedTransition(3000, "NewLocation3")
                    ),
                    new HpLessTransition(0.94f, "KillKing")
                ),
                new State("KillKing",
                    new Taunt("Your secret soul master is dying, Your Majesty"),
                    new Order(30, "Ghost King", "Killed"),
                    new TimedTransition(3000, "Suicide")
                ),
                new State("Suicide",
                    new Taunt("I cannot live with my betrayal..."),
                    new Shoot(0, count: 8, shootAngle: 45, fixedAngle: 22.5f),
                    new Decay(0)
                ),
                new State("Decay",
                    new Decay(0)
                ),
                new ItemLoot("Purple Drake Egg", 0.03f),
                new ItemLoot("White Drake Egg", 0.001f),
                new ItemLoot("Tincture of Dexterity", 0.02f)
            );
            db.Init("Actual Ghost King",
                new Taunt(0.9f, "I am still so very alone"),
                new ChangeSize(-20, 95),
                new Flash(0xff000000, 0.4f, 100),
                new BackAndForth(0.5f, distance: 3),
                new TierLoot(2, TierLoot.LootType.Ring, 0.25f),
                new TierLoot(3, TierLoot.LootType.Ring, 0.08f),
                new TierLoot(7, TierLoot.LootType.Weapon, 0.3f),
                new TierLoot(8, TierLoot.LootType.Weapon, 0.1f),
                new TierLoot(7, TierLoot.LootType.Armor, 0.3f),
                new TierLoot(8, TierLoot.LootType.Armor, 0.1f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.7f),
                new TierLoot(3, TierLoot.LootType.Ability, 0.16f),
                new TierLoot(4, TierLoot.LootType.Ability, 0.02f),
                new ItemLoot("Health Potion", 0.7f),
                new ItemLoot("Magic Potion", 0.7f)
            );
            db.Init("Small Ghost",
                new TransformOnDeath("Medium Ghost"),
                new State("NewLocation",
                    new Taunt(0.1f, "Switch!"),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff00ff00, 0.2f, 10),
                    new Prioritize(
                        new StayCloseToSpawn(2, range: 7),
                        new Wander(2)
                    ),
                    new TimedTransition(1000, "Attack")
                ),
                new State("Attack",
                    new Taunt(0.1f, "Save the King's Soul!"),
                    new Shoot(10, count: 4, shootAngle: 90, fixedAngle: 0, cooldown: 400),
                    new TimedTransition(9000, "NewLocation")
                ),
                new State("Decay",
                    new Decay(0)
                ),
                new Decay(160000),
                new ItemLoot("Magic Potion", 0.02f),
                new ItemLoot("Ring of Magic", 0.02f),
                new ItemLoot("Ring of Attack", 0.02f)
            );
            db.Init("Medium Ghost",
                new TransformOnDeath("Large Ghost"),
                new State("Intro",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff00ff00, 0.2f, 10),
                    new ChangeSize(20, 140),
                    new TimedTransition(1000, "NewLocation")
                ),
                new State("NewLocation",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff00ff00, 0.2f, 10),
                    new Prioritize(
                        new StayCloseToSpawn(2, range: 7),
                        new Wander(2)
                    ),
                    new TimedTransition(1000, "Attack")
                ),
                new State("Attack",
                    new Taunt(0.02f, "I come back more powerful than you could ever imagine"),
                    new Shoot(10, count: 4, shootAngle: 90, fixedAngle: 45, cooldown: 800),
                    new TimedTransition(6000, "NewLocation")
                ),
                new State("Decay",
                    new Decay(0)
                ),
                new Decay(160000),
                new ItemLoot("Magic Potion", 0.02f),
                new ItemLoot("Ring of Speed", 0.02f),
                new ItemLoot("Ring of Attack", 0.02f),
                new ItemLoot("Iron Quiver", 0.02f)
            );
            db.Init("Large Ghost",
                new State("Intro",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff00ff00, 0.2f, 10),
                    new ChangeSize(20, 180),
                    new TimedTransition(1000, "NewLocation")
                ),
                new State("NewLocation",
                    new Taunt(0.01f,
                        "The Ghost King protects this sacred ground",
                        "The Ghost King gave his heart to the Ghost Master.  He cannot die.",
                        "Only the Secret Ghost Master can kill the King."),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff00ff00, 0.2f, 10),
                    new Prioritize(
                        new StayCloseToSpawn(2, range: 7),
                        new Wander(2)
                    ),
                    new TimedTransition(1000, "Attack")
                ),
                new State("Attack",
                    new Taunt(0.01f, "The King's wife died here.  For her memory."),
                    new Shoot(10, count: 8, shootAngle: 45, fixedAngle: 22.5f, cooldown: 800),
                    new TimedTransition(3000, "NewLocation"),
                    new EntityNotExistsTransition("Ghost King", 30, "AttackKingGone")
                ),
                new State("AttackKingGone",
                    new Taunt(0.01f, "The King's wife died here.  For her memory."),
                    new Shoot(10, count: 8, shootAngle: 45, fixedAngle: 22.5f, cooldown: 800, cooldownOffset: 800),
                    new TransformOnDeath("Imp", min: 2, max: 3),
                    new TimedTransition(3000, "NewLocation")
                ),
                new State("Decay",
                    new Decay(0)
                ),
                new Decay(160000),
                new ItemLoot("Magic Potion", 0.02f),
                new ItemLoot("Tincture of Defense", 0.02f),
                new ItemLoot("Blue Drake Egg", 0.02f),
                new ItemLoot("Yellow Drake Egg", 0.02f)
            );
        }
    }
}
