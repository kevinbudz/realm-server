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
    public class Crystal : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Mysterious Crystal",
                new State("Waiting",
                    new PlayerWithinTransition(10, "Idle", true)
                ),
                new State("Idle",
                    new Taunt(0.1f, "Break the crystal for great rewards..."),
                    new Taunt(0.1f, "Help me..."),
                    new HpLessTransition(0.9999f, "Instructions"),
                    new TimedTransition(10000, "Idle")
                ),
                new State("Instructions",
                    new Flash(0xffffffff, 2, 100),
                    new Taunt(0.8f, "Fire upon this crystal with all your might for 5 seconds"),
                    new Taunt(0.8f, "If your attacks are weak, the crystal magically heals"),
                    new Taunt(0.8f, "Gather a large group to smash it open"),
                    new HpLessTransition(0.998f, "Evaluation")
                ),
                new State("Evaluation",
                    new State("Comment1",
                        new Taunt(true, "Sweet treasure awaits for powerful adventurers!"),
                        new Taunt(0.4f, "Yes!  Smash my prison for great rewards!"),
                        new TimedTransition(5000, "Comment2")
                    ),
                    new State("Comment2",
                        new Taunt(0.3f, "If you are not very strong, this could kill you",
                            "If you are not yet powerful, stay away from the Crystal",
                            "New adventurers should stay away",
                            "That's the spirit. Lay your fire upon me.",
                            "So close..."
                        ),
                        new TimedTransition(5000, "Comment3")
                    ),
                    new State("Comment3",
                        new Taunt(0.4f, "I think you need more people...",
                            "Call all your friends to help you break the crystal!"
                        ),
                        new TimedTransition(10000, "Comment2")
                    ),
                    new HealGroup(1, "Crystals", cooldown: 5000),
                    new HpLessTransition(0.95f, "StartBreak"),
                    new TimedTransition(60000, "Fail")
                ),
                new State("Fail",
                    new Taunt("Perhaps you need a bigger group. Ask others to join you!"),
                    new Flash(0xff000000, 5, 1),
                    new Shoot(10, count: 16, shootAngle: 22.5f, fixedAngle: 0, cooldown: 100000),
                    new HealGroup(1, "Crystals", cooldown: 1000),
                    new TimedTransition(5000, "Idle")
                ),
                new State("StartBreak",
                    new Taunt("You cracked the crystal! Soon we shall emerge!"),
                    new ChangeSize(-2, 80),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff000000, 2, 10),
                    new TimedTransition(4000, "BreakCrystal")
                ),
                new State("BreakCrystal",
                    new Taunt("This your reward! Imagine what evil even Oryx needs to keep locked up!"),
                    new Shoot(0, count: 16, shootAngle: 22.5f, fixedAngle: 0, cooldown: 100000),
                    new Spawn("Crystal Prisoner", maxChildren: 1, initialSpawn: 1, cooldown: 100000),
                    new Decay(0)
                )
            );
            db.Init("Crystal Prisoner",
                new Spawn("Crystal Prisoner Steed", maxChildren: 3, initialSpawn: 0, cooldown: 200),
                new State("pause",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new TimedTransition(2000, "start_the_fun")
                ),
                new State("start_the_fun",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Taunt("I'm finally free! Yesss!!!"),
                    new TimedTransition(1500, "Daisy_attack")
                ),
                new State("Daisy_attack",
                    new Prioritize(
                        new StayCloseToSpawn(0.3f, range: 7),
                        new Wander(0.3f)
                    ),
                    new State("Quadforce1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(0, index: 0, count: 4, shootAngle: 90, fixedAngle: 0, cooldown: 300),
                        new TimedTransition(200, "Quadforce2")
                    ),
                    new State("Quadforce2",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(0, index: 0, count: 4, shootAngle: 90, fixedAngle: 15, cooldown: 300),
                        new TimedTransition(200, "Quadforce3")
                    ),
                    new State("Quadforce3",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(0, index: 0, count: 4, shootAngle: 90, fixedAngle: 30, cooldown: 300),
                        new TimedTransition(200, "Quadforce4")
                    ),
                    new State("Quadforce4",
                        new Shoot(10, index: 3, cooldown: 1000),
                        new Shoot(0, index: 0, count: 4, shootAngle: 90, fixedAngle: 45, cooldown: 300),
                        new TimedTransition(200, "Quadforce5")
                    ),
                    new State("Quadforce5",
                        new Shoot(0, index: 0, count: 4, shootAngle: 90, fixedAngle: 60, cooldown: 300),
                        new TimedTransition(200, "Quadforce6")
                    ),
                    new State("Quadforce6",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(0, index: 0, count: 4, shootAngle: 90, fixedAngle: 75, cooldown: 300),
                        new TimedTransition(200, "Quadforce7")
                    ),
                    new State("Quadforce7",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(0, index: 0, count: 4, shootAngle: 90, fixedAngle: 90, cooldown: 300),
                        new TimedTransition(200, "Quadforce8")
                    ),
                    new State("Quadforce8",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(10, index: 3, cooldown: 1000),
                        new Shoot(0, index: 0, count: 4, shootAngle: 90, fixedAngle: 105, cooldown: 300),
                        new TimedTransition(200, "Quadforce1")
                    ),
                    new HpLessTransition(0.3f, "Whoa_nelly"),
                    new TimedTransition(18000, "Warning")
                ),
                new State("Warning",
                    new Prioritize(
                        new StayCloseToSpawn(0.5f, range: 7),
                        new Wander(0.5f)
                    ),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff00ff00, 0.2f, 15),
                    new Follow(0.4f, acquireRange: 9, range: 2),
                    new TimedTransition(3000, "Summon_the_clones")
                ),
                new State("Summon_the_clones",
                    new Prioritize(
                        new StayCloseToSpawn(0.85f, range: 7),
                        new Wander(0.85f)
                    ),
                    new Shoot(10, index: 0, cooldown: 1000),
                    new Spawn("Crystal Prisoner Clone", maxChildren: 4, initialSpawn: 0, cooldown: 200),
                    new TossObject("Crystal Prisoner Clone", range: 5, angle: 0, cooldown: 100000),
                    new TossObject("Crystal Prisoner Clone", range: 5, angle: 240, cooldown: 100000),
                    new TossObject("Crystal Prisoner Clone", range: 7, angle: 60, cooldown: 100000),
                    new TossObject("Crystal Prisoner Clone", range: 7, angle: 300, cooldown: 100000),
                    new State("invulnerable_clone",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "vulnerable_clone")
                    ),
                    new State("vulnerable_clone",
                        new TimedTransition(1200, "invulnerable_clone")
                    ),
                    new TimedTransition(16000, "Warning2")
                ),
                new State("Warning2",
                    new Prioritize(
                        new StayCloseToSpawn(0.85f, range: 7),
                        new Wander(0.85f)
                    ),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff00ff00, 0.2f, 25),
                    new TimedTransition(5000, "Whoa_nelly")
                ),
                new State("Whoa_nelly",
                    new Prioritize(
                        new StayCloseToSpawn(0.6f, range: 7),
                        new Wander(0.6f)
                    ),
                    new Shoot(10, index: 3, count: 3, shootAngle: 120, cooldown: 900),
                    new Shoot(10, index: 2, count: 3, shootAngle: 15, fixedAngle: 40, cooldown: 1600,
                        cooldownOffset: 0),
                    new Shoot(10, index: 2, count: 3, shootAngle: 15, fixedAngle: 220, cooldown: 1600,
                        cooldownOffset: 0),
                    new Shoot(10, index: 2, count: 3, shootAngle: 15, fixedAngle: 130, cooldown: 1600,
                        cooldownOffset: 800),
                    new Shoot(10, index: 2, count: 3, shootAngle: 15, fixedAngle: 310, cooldown: 1600,
                        cooldownOffset: 800),
                    new State("invulnerable_whoa",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(2600, "vulnerable_whoa")
                    ),
                    new State("vulnerable_whoa",
                        new TimedTransition(1200, "invulnerable_whoa")
                    ),
                    new TimedTransition(10000, "Absolutely_Massive")
                ),
                new State("Absolutely_Massive",
                    new ChangeSize(13, 260),
                    new Prioritize(
                        new StayCloseToSpawn(0.2f, range: 7),
                        new Wander(0.2f)
                    ),
                    new Shoot(10, index: 1, count: 9, shootAngle: 40, fixedAngle: 40, cooldown: 2000,
                        cooldownOffset: 400),
                    new Shoot(10, index: 1, count: 9, shootAngle: 40, fixedAngle: 60, cooldown: 2000,
                        cooldownOffset: 800),
                    new Shoot(10, index: 1, count: 9, shootAngle: 40, fixedAngle: 50, cooldown: 2000,
                        cooldownOffset: 1200),
                    new Shoot(10, index: 1, count: 9, shootAngle: 40, fixedAngle: 70, cooldown: 2000,
                        cooldownOffset: 1600),
                    new State("invulnerable_mass",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(2600, "vulnerable_mass")
                    ),
                    new State("vulnerable_mass",
                        new TimedTransition(1000, "invulnerable_mass")
                    ),
                    new TimedTransition(14000, "Start_over_again")
                ),
                new State("Start_over_again",
                    new ChangeSize(-20, 100),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xff00ff00, 0.2f, 15),
                    new TimedTransition(3000, "Daisy_attack")
                ),
                new Threshold(0.015f,
                    new TierLoot(2, TierLoot.LootType.Potion, min: 3, threshold: 0.07f)
                ),
                new Threshold(0.03f,
                    new ItemLoot("Crystal Wand", 0.05f),
                    new ItemLoot("Crystal Sword", 0.06f)
                )
            );
            db.Init("Crystal Prisoner Clone",
                new Prioritize(
                    new StayCloseToSpawn(0.85f, range: 5),
                    new Wander(0.85f)
                ),
                new Shoot(10, cooldown: 1400),
                new State("taunt",
                    new Taunt(0.09f, "I am everywhere and nowhere!"),
                    new TimedTransition(1000, "no_taunt")
                ),
                new State("no_taunt",
                    new TimedTransition(1000, "taunt")
                ),
                new Decay(17000)
            );
            db.Init("Crystal Prisoner Steed",
                new State("change_position_fast",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Prioritize(
                        new StayCloseToSpawn(3.6f, range: 12),
                        new Wander(3.6f)
                    ),
                    new TimedTransition(800, "attack")
                ),
                new State("attack",
                    new Shoot(10, predictive: 0.3f, cooldown: 500),
                    new State("keep_distance",
                        new Prioritize(
                            new StayCloseToSpawn(1, range: 12),
                            new Orbit(1, 9, target: "Crystal Prisoner", radiusVariance: 0)
                        ),
                        new TimedTransition(2000, "go_anywhere")
                    ),
                    new State("go_anywhere",
                        new Prioritize(
                            new StayCloseToSpawn(1, range: 12),
                            new Wander(1)
                        ),
                        new TimedTransition(2000, "keep_distance")
                    )
                )
            );
        }
    }
}
