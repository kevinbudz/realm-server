using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    // Oryx's Chamber — ported from fabianos / betterskillys BehaviorDb.Oryx (Oryx 1 phase).
    public class OryxChamber : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Oryx the Mad God 1",
                new DropPortalOnDeath("Locked Wine Cellar Portal", probability: 100, timeout: 120),
                new State("main",
                    new HpLessTransition(0.2f, "rage"),
                    new State("Slow",
                        new Taunt("Fools! I still have {HP} hitpoints!"),
                        new Spawn("Minion of Oryx", maxChildren: 5, initialSpawn: 0, cooldown: 350000),
                        new Reproduce("Minion of Oryx", densityRadius: 10, densityMax: 5, cooldown: 1500),
                        new Shoot(25, count: 4, shootAngle: 10, index: 4, cooldown: 1000),
                        new TimedTransition(20000, "Dance 1")
                    ),
                    new State("Dance 1",
                        new Flash(0xf389E13, flashPeriod: 0.5, flashRepeats: 60),
                        new Taunt("BE SILENT!!!"),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(50, count: 8, index: 6, cooldown: 700, cooldownOffset: 200),
                        new TossObject("Ring Element", range: 9, angle: 24, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 48, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 72, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 96, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 120, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 144, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 168, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 192, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 216, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 240, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 264, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 288, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 312, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 336, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 360, cooldown: 320000),
                        new TimedTransition(25000, "artifacts")
                    ),
                    new State("artifacts",
                        new Taunt("My Artifacts will protect me!"),
                        new Flash(0xf389E13, flashPeriod: 0.5, flashRepeats: 60),
                        new Shoot(50, count: 3, index: 9, cooldown: 1500, cooldownOffset: 200),
                        new Shoot(50, count: 10, index: 8, cooldown: 2000, cooldownOffset: 200),
                        new Shoot(50, count: 10, index: 7, cooldown: 500, cooldownOffset: 200),
                        new TossObject("Guardian Element 1", range: 1, angle: 0, cooldown: 90000001),
                        new TossObject("Guardian Element 1", range: 1, angle: 90, cooldown: 90000001),
                        new TossObject("Guardian Element 1", range: 1, angle: 180, cooldown: 90000001),
                        new TossObject("Guardian Element 1", range: 1, angle: 270, cooldown: 90000001),
                        new TossObject("Guardian Element 2", range: 9, angle: 0, cooldown: 90000001),
                        new TossObject("Guardian Element 2", range: 9, angle: 90, cooldown: 90000001),
                        new TossObject("Guardian Element 2", range: 9, angle: 180, cooldown: 90000001),
                        new TossObject("Guardian Element 2", range: 9, angle: 270, cooldown: 90000001),
                        new TimedTransition(25000, "gaze")
                    ),
                    new State("gaze",
                        new Taunt("All who looks upon my face shall die."),
                        new Shoot(range: 7, count: 2, shootAngle: 10, index: 1, cooldown: 1000, cooldownOffset: 800),
                        new TimedTransition(10000, "Dance 2")
                    ),
                    new State("Dance 2",
                        new Flash(0xf389E13, flashPeriod: 0.5, flashRepeats: 60),
                        new Taunt("Time for more dancing!"),
                        new Shoot(50, count: 8, index: 6, cooldown: 700, cooldownOffset: 200),
                        new TossObject("Ring Element", range: 9, angle: 24, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 48, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 72, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 96, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 120, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 144, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 168, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 192, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 216, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 240, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 264, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 288, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 312, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 336, cooldown: 320000),
                        new TossObject("Ring Element", range: 9, angle: 360, cooldown: 320000),
                        new TimedTransition(1000, "Dance2, 1")
                    ),
                    new State("Dance2, 1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(range: 0, count: 4, shootAngle: 90, index: 8, fixedAngle: 0, cooldown: 170),
                        new TimedTransition(200, "Dance2, 2")
                    ),
                    new State("Dance2, 2",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(range: 0, count: 4, shootAngle: 90, index: 8, fixedAngle: 30, cooldown: 170),
                        new TimedTransition(200, "Dance2, 3")
                    ),
                    new State("Dance2, 3",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(range: 0, count: 4, shootAngle: 90, index: 8, fixedAngle: 15, cooldown: 170),
                        new TimedTransition(200, "Dance2, 4")
                    ),
                    new State("Dance2, 4",
                        new Shoot(range: 0, count: 4, shootAngle: 90, index: 8, fixedAngle: 45, cooldown: 170),
                        new TimedTransition(200, "Dance2, 1")
                    ),
                    new State("rage",
                        new ChangeSize(rate: 10, target: 200),
                        new Taunt(0.3f, "I HAVE HAD ENOUGH OF YOU!!!", "ENOUGH!!!", "DIE!!!"),
                        new Spawn("Minion of Oryx", maxChildren: 10, initialSpawn: 0, cooldown: 350000),
                        new Reproduce("Minion of Oryx", densityRadius: 10, densityMax: 5, cooldown: 1500),
                        new Shoot(range: 7, count: 2, shootAngle: 10, index: 1, cooldown: 1500, cooldownOffset: 2000),
                        new Shoot(range: 7, count: 5, shootAngle: 10, index: 16, cooldown: 1500, cooldownOffset: 2000),
                        new Follow(0.85, acquireRange: 15, range: 1),
                        new Flash(0xfFF0000, flashPeriod: 0.5, flashRepeats: 9000001)
                    )
                ),
                new Threshold(0.05f,
                    new ItemLoot("Potion of Attack", 0.3f),
                    new ItemLoot("Potion of Defense", 0.3f)
                ),
                new Threshold(0.1f,
                    new TierLoot(10, TierLoot.LootType.Weapon, 0.07f),
                    new TierLoot(11, TierLoot.LootType.Weapon, 0.06f),
                    new TierLoot(5, TierLoot.LootType.Ability, 0.07f),
                    new TierLoot(11, TierLoot.LootType.Armor, 0.07f),
                    new TierLoot(5, TierLoot.LootType.Ring, 0.06f)
                )
            );

            db.Init("Ring Element",
                new State("main",
                    new State("attack",
                        new Shoot(50, count: 12, index: 0, cooldown: 700, cooldownOffset: 200),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(20000, "Despawn")
                    ),
                    new State("Despawn",
                        new Suicide()
                    )
                )
            );

            db.Init("Minion of Oryx",
                new Wander(0.4f),
                new Shoot(3, count: 3, shootAngle: 10, index: 0, cooldown: 1000),
                new Shoot(3, count: 3, shootAngle: 10, index: 1, cooldown: 1000),
                new TierLoot(7, TierLoot.LootType.Weapon, 0.2f),
                new ItemLoot("Health Potion", 0.03f)
            );

            db.Init("Guardian Element 1",
                new State("main",
                    new State("orbit",
                        new Orbit(1, 1, target: "Oryx the Mad God 1", radiusVariance: 0),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(25, count: 3, shootAngle: 10, index: 0, cooldown: 1000),
                        new TimedTransition(10000, "Grow")
                    ),
                    new State("Grow",
                        new ChangeSize(rate: 100, target: 200),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Orbit(1, 1, target: "Oryx the Mad God 1", radiusVariance: 0),
                        new Shoot(3, count: 1, shootAngle: 10, index: 0, cooldown: 700),
                        new TimedTransition(10000, "Despawn")
                    ),
                    new State("Despawn",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Orbit(1, 1, target: "Oryx the Mad God 1", radiusVariance: 0),
                        new ChangeSize(rate: 100, target: 100),
                        new Suicide()
                    )
                )
            );

            db.Init("Guardian Element 2",
                new State("main",
                    new State("orbit",
                        new Orbit(1.3, 9, target: "Oryx the Mad God 1", radiusVariance: 0),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(25, count: 3, shootAngle: 10, index: 0, cooldown: 1000),
                        new TimedTransition(20000, "Despawn")
                    ),
                    new State("Despawn",
                        new Suicide()
                    )
                )
            );
        }
    }
}
