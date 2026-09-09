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
    public class TombMobs : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Eagle Sentry",
                new NoPlayerWithinTransition(12, "Idle"),
                new PlayerWithinTransition(12, "Chase"),
                new State("Idle",
                    new Wander(0.03f)
                ),
                new State("Chase",
                    new Follow(0.7f, 7, 0),
                    new Shoot(25, 12, index: 1, cooldown: 3000)
                )
            );
            db.Init("Bloated Mummy",
                new NoPlayerWithinTransition(12, "Idle"),
                new PlayerWithinTransition(12, "Chase"),
                new State("Idle",
                    new Wander(0.03f)
                ),
                new State("Chase",
                    new Follow(0.6f, 7, 0),
                    new Reproduce("Scarab", 10, 3, 3000),
                    new Shoot(25, 22, index: 0, cooldown: 2250)
                )
            );
            db.Init("Lion Archer",
                new NoPlayerWithinTransition(12, "Idle"),
                new PlayerWithinTransition(12, "Chase"),
                new State("Idle",
                    new Wander(0.03f)
                ),
                new State("Chase",
                    new Follow(0.4f, 7, 0),
                    new Shoot(25, 3, index: 1, cooldown: 1250),
                    new Shoot(25, 1, index: 3, fixedAngle: 0, cooldown: 6000),
                    new Shoot(25, 1, index: 3, fixedAngle: 90, cooldown: 6000),
                    new Shoot(25, 1, index: 3, fixedAngle: 180, cooldown: 6000),
                    new Shoot(25, 1, index: 3, fixedAngle: 270, cooldown: 6000)
                )
            );
            db.Init("Jackal Warrior",
                new NoPlayerWithinTransition(12, "Idle"),
                new PlayerWithinTransition(12, "Chase"),
                new State("Idle",
                    new Wander(0.03f)
                ),
                new State("Chase",
                    new Follow(0.9f, 7, 0),
                    new Shoot(25, 1, 25, 0, cooldown: 1250)
                )
            );
            db.Init("Jackal Assassin",
                new NoPlayerWithinTransition(12, "Idle"),
                new PlayerWithinTransition(12, "Chase"),
                new State("Idle",
                    new Wander(0.03f)
                ),
                new State("Chase",
                    new Follow(0.9f, 7, 0),
                    new Shoot(25, 1, 25, 0, cooldown: 1250)
                )
            );
            db.Init("Jackal Veteran",
                new NoPlayerWithinTransition(12, "Idle"),
                new PlayerWithinTransition(12, "Chase"),
                new State("Idle",
                    new Wander(0.03f)
                ),
                new State("Chase",
                    new Follow(0.9f, 7, 0),
                    new Shoot(25, 1, 25, 0, cooldown: 1250)
                )
            );
            db.Init("Jackal Lord",
                new NoPlayerWithinTransition(12, "Idle"),
                new PlayerWithinTransition(12, "Chase"),
                new State("Idle",
                    new Wander(0.03f)
                ),
                new State("Chase",
                    new Follow(0.9f, 7, 0),
                    new Reproduce("Jackal Warrior", 10, 2, 10000),
                    new Reproduce("Jackal Veteran", 10, 1, 10000),
                    new Reproduce("Jackal Assassin", 10, 1, 10000),
                    new Shoot(25, 4, 25, 0, cooldown: 1250)
                )
            );
            db.Init("Beam Priest",
                new State("weakning",
                    new Orbit(0.4f, 6, target: "Active Sarcophagus", radiusVariance: 0.5f),
                    new Shoot(50, 3, index: 1, cooldown: 3500),
                    new Shoot(50, 6, index: 0, cooldown: 7210)
                )
            );
            db.Init("Tomb Thunder Turret",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(2500, "Spin")
                ),
                new State("Spin",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(2000, "Pause"),
                    new State("Quadforce1",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 0, cooldown: 300),
                        new TimedTransition(150, "Quadforce2")
                    ),
                    new State("Quadforce2",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 15, cooldown: 300),
                        new TimedTransition(150, "Quadforce3")
                    ),
                    new State("Quadforce3",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 30, cooldown: 300),
                        new TimedTransition(150, "Quadforce4")
                    ),
                    new State("Quadforce4",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 45, cooldown: 300),
                        new TimedTransition(150, "Quadforce5")
                    ),
                    new State("Quadforce5",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 60, cooldown: 300),
                        new TimedTransition(150, "Quadforce6")
                    ),
                    new State("Quadforce6",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 75, cooldown: 300),
                        new TimedTransition(150, "Quadforce7")
                    ),
                    new State("Quadforce7",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 90, cooldown: 300),
                        new TimedTransition(150, "Quadforce8")
                    ),
                    new State("Quadforce8",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 105, cooldown: 300),
                        new TimedTransition(150, "Quadforce1")
                    )
                ),
                new State("Pause",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(5000, "Spin")
                )
            );
            db.Init("Tomb Fire Turret",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(2500, "Spin")
                ),
                new State("Spin",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(2000, "Pause"),
                    new State("Quadforce1",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 0, cooldown: 300),
                        new TimedTransition(150, "Quadforce2")
                    ),
                    new State("Quadforce2",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 15, cooldown: 300),
                        new TimedTransition(150, "Quadforce3")
                    ),
                    new State("Quadforce3",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 30, cooldown: 300),
                        new TimedTransition(150, "Quadforce4")
                    ),
                    new State("Quadforce4",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 45, cooldown: 300),
                        new TimedTransition(150, "Quadforce5")
                    ),
                    new State("Quadforce5",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 60, cooldown: 300),
                        new TimedTransition(150, "Quadforce6")
                    ),
                    new State("Quadforce6",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 75, cooldown: 300),
                        new TimedTransition(150, "Quadforce7")
                    ),
                    new State("Quadforce7",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 90, cooldown: 300),
                        new TimedTransition(150, "Quadforce8")
                    ),
                    new State("Quadforce8",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 105, cooldown: 300),
                        new TimedTransition(150, "Quadforce1")
                    )
                ),
                new State("Pause",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(5000, "Spin")
                )
            );
            db.Init("Tomb Frost Turret",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(2500, "Spin")
                ),
                new State("Spin",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(2000, "Pause"),
                    new State("Quadforce1",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 0, cooldown: 300),
                        new TimedTransition(150, "Quadforce2")
                    ),
                    new State("Quadforce2",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 15, cooldown: 300),
                        new TimedTransition(150, "Quadforce3")
                    ),
                    new State("Quadforce3",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 30, cooldown: 300),
                        new TimedTransition(150, "Quadforce4")
                    ),
                    new State("Quadforce4",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 45, cooldown: 300),
                        new TimedTransition(150, "Quadforce5")
                    ),
                    new State("Quadforce5",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 60, cooldown: 300),
                        new TimedTransition(150, "Quadforce6")
                    ),
                    new State("Quadforce6",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 75, cooldown: 300),
                        new TimedTransition(150, "Quadforce7")
                    ),
                    new State("Quadforce7",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 90, cooldown: 300),
                        new TimedTransition(150, "Quadforce8")
                    ),
                    new State("Quadforce8",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(0, 5, 60, 0, fixedAngle: 105, cooldown: 300),
                        new TimedTransition(150, "Quadforce1")
                    )
                ),
                new State("Pause",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new TimedTransition(5000, "Spin")
                )
            );
            db.Init("Beam Priestess",
                new State("weakning",
                    new Prioritize(
                        new Orbit(0.6f, 9, target: "Active Sarcophagus", radiusVariance: 0.5f)
                    ),
                    new Shoot(50, 6, index: 1, cooldown: 3500),
                    new Shoot(50, 2, index: 0, cooldown: 7210)
                )
            );
        }
    }
}
