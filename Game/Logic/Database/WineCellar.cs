using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    // Wine Cellar — ported from betterskillys BehaviorDb.Oryx (Oryx 2 + henchmen).
    public class WineCellar : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Oryx the Mad God 2",
                new ScaleHP(amount: 30),
                new State("main",
                    new State("Attack",
                        new Wander(0.05f),
                        new Shoot(25, count: 8, shootAngle: 45, index: 0, cooldown: 1500, cooldownOffset: 1500),
                        new Shoot(25, count: 3, shootAngle: 10, index: 1, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 3, shootAngle: 10, index: 2, predictive: 0.2f, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 2, shootAngle: 10, index: 3, predictive: 0.4f, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 3, shootAngle: 10, index: 4, predictive: 0.6f, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 2, shootAngle: 10, index: 5, predictive: 0.8f, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 3, shootAngle: 10, index: 6, predictive: 1f, cooldown: 1000, cooldownOffset: 1000),
                        new Taunt(probability: 1, cooldown: 6000, "Puny mortals! My {HP} HP will annihilate you!"),
                        new Spawn("Henchman of Oryx", maxChildren: 5, cooldown: 5000),
                        new HpLessTransition(0.2f, "prepareRage")
                    ),
                    new State("prepareRage",
                        new Follow(0.1, acquireRange: 15, range: 3),
                        new Taunt("Can't... keep... henchmen... alive... anymore! ARGHHH!!!"),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(25, count: 30, fixedAngle: 0, index: 13, cooldown: 4000, cooldownOffset: 4000),
                        new Shoot(25, count: 30, fixedAngle: 30, index: 14, cooldown: 4000, cooldownOffset: 4000),
                        new TimedTransition(10000, "rage")
                    ),
                    new State("rage",
                        new Follow(0.1, acquireRange: 15, range: 3),
                        new Shoot(25, count: 30, index: 13, cooldown: 90000001, cooldownOffset: 8000),
                        new Shoot(25, count: 30, index: 14, cooldown: 90000001, cooldownOffset: 8500),
                        new Shoot(25, count: 8, shootAngle: 45, index: 0, cooldown: 1500, cooldownOffset: 1500),
                        new Shoot(25, count: 3, shootAngle: 10, index: 1, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 3, shootAngle: 10, index: 2, predictive: 0.2f, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 2, shootAngle: 10, index: 3, predictive: 0.4f, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 3, shootAngle: 10, index: 4, predictive: 0.6f, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 2, shootAngle: 10, index: 5, predictive: 0.8f, cooldown: 1000, cooldownOffset: 1000),
                        new Shoot(25, count: 3, shootAngle: 10, index: 6, predictive: 1f, cooldown: 1000, cooldownOffset: 1000),
                        new TossObject("Monstrosity Scarab", range: 7, angle: 0, cooldown: 1000),
                        new Taunt(probability: 1, cooldown: 6000, "Puny mortals! My {HP} HP will annihilate you!")
                    )
                ),
                new Threshold(0.29f,
                    new ItemLoot("Potion of Vitality", 1)
                ),
                new Threshold(0.05f,
                    new ItemLoot("Potion of Attack", 0.3f),
                    new ItemLoot("Potion of Defense", 0.3f),
                    new ItemLoot("Potion of Wisdom", 0.3f)
                ),
                new Threshold(0.1f,
                    new TierLoot(10, TierLoot.LootType.Weapon, 0.07f),
                    new TierLoot(11, TierLoot.LootType.Weapon, 0.06f),
                    new TierLoot(12, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(5, TierLoot.LootType.Ability, 0.07f),
                    new TierLoot(6, TierLoot.LootType.Ability, 0.05f),
                    new TierLoot(11, TierLoot.LootType.Armor, 0.07f),
                    new TierLoot(12, TierLoot.LootType.Armor, 0.06f),
                    new TierLoot(13, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(5, TierLoot.LootType.Ring, 0.06f)
                )
            );

            db.Init("Henchman of Oryx",
                new State("main",
                    new State("Attack",
                        new Prioritize(
                            new Orbit(0.2, 2, target: "Oryx the Mad God 2", radiusVariance: 1),
                            new Wander(0.3f)
                        ),
                        new Shoot(15, predictive: 1, cooldown: 2500),
                        new Shoot(10, count: 3, shootAngle: 10, index: 1, cooldown: 2500),
                        new Spawn("Vintner of Oryx", maxChildren: 1, initialSpawn: 1, cooldown: 5000),
                        new Spawn("Aberrant of Oryx", maxChildren: 1, initialSpawn: 1, cooldown: 5000),
                        new Spawn("Monstrosity of Oryx", maxChildren: 1, initialSpawn: 1, cooldown: 5000),
                        new Spawn("Abomination of Oryx", maxChildren: 1, initialSpawn: 1, cooldown: 5000)
                    ),
                    new State("Suicide",
                        new Decay(0)
                    )
                )
            );

            db.Init("Monstrosity of Oryx",
                new State("main",
                    new State("Wait",
                        new PlayerWithinTransition(15, "Attack")
                    ),
                    new State("Attack",
                        new TimedTransition(10000, "Wait"),
                        new Prioritize(
                            new Orbit(0.1, 6, target: "Oryx the Mad God 2", radiusVariance: 3),
                            new Follow(0.1, acquireRange: 15),
                            new Wander(0.2f)
                        ),
                        new TossObject("Monstrosity Scarab", range: 1, angle: 0, cooldown: 10000, cooldownOffset: 1000)
                    )
                )
            );

            db.Init("Monstrosity Scarab",
                new State("main",
                    new State("Charge",
                        new Prioritize(
                            new Charge(range: 25, cooldown: 1000),
                            new Wander(0.3f)
                        ),
                        new PlayerWithinTransition(1, "Boom")
                    ),
                    new State("Boom",
                        new Shoot(1, count: 16, shootAngle: 360 / 16, fixedAngle: 0),
                        new Decay(0)
                    )
                )
            );

            db.Init("Vintner of Oryx",
                new State("Attack",
                    new Prioritize(
                        new Protect(1, "Oryx the Mad God 2", protectionRange: 4, reprotectRange: 3),
                        new Charge(speed: 1, range: 15, cooldown: 2000),
                        new Protect(1, "Henchman of Oryx"),
                        new StayBack(1, 15),
                        new Wander(1)
                    ),
                    new Shoot(10, cooldown: 250)
                )
            );

            db.Init("Aberrant of Oryx",
                new Prioritize(
                    new Protect(0.2, "Oryx the Mad God 2"),
                    new Wander(0.7f)
                ),
                new State("Wait",
                    new PlayerWithinTransition(15, "Attack")
                ),
                new State("Attack",
                    new TimedTransition(10000, "Wait"),
                    new State("Randomize",
                        new TimedTransition(100, "Toss1", randomized: true),
                        new TimedTransition(100, "Toss2", randomized: true),
                        new TimedTransition(100, "Toss3", randomized: true),
                        new TimedTransition(100, "Toss4", randomized: true),
                        new TimedTransition(100, "Toss5", randomized: true),
                        new TimedTransition(100, "Toss6", randomized: true),
                        new TimedTransition(100, "Toss7", randomized: true),
                        new TimedTransition(100, "Toss8", randomized: true)
                    ),
                    new State("Toss1",
                        new TossObject("Aberrant Blaster", range: 5, angle: 0, cooldown: 40000),
                        new TimedTransition(4900, "Randomize")
                    ),
                    new State("Toss2",
                        new TossObject("Aberrant Blaster", range: 5, angle: 45, cooldown: 40000),
                        new TimedTransition(4900, "Randomize")
                    ),
                    new State("Toss3",
                        new TossObject("Aberrant Blaster", range: 5, angle: 90, cooldown: 40000),
                        new TimedTransition(4900, "Randomize")
                    ),
                    new State("Toss4",
                        new TossObject("Aberrant Blaster", range: 5, angle: 135, cooldown: 40000),
                        new TimedTransition(4900, "Randomize")
                    ),
                    new State("Toss5",
                        new TossObject("Aberrant Blaster", range: 5, angle: 180, cooldown: 40000),
                        new TimedTransition(4900, "Randomize")
                    ),
                    new State("Toss6",
                        new TossObject("Aberrant Blaster", range: 5, angle: 225, cooldown: 40000),
                        new TimedTransition(4900, "Randomize")
                    ),
                    new State("Toss7",
                        new TossObject("Aberrant Blaster", range: 5, angle: 270, cooldown: 40000),
                        new TimedTransition(4900, "Randomize")
                    ),
                    new State("Toss8",
                        new TossObject("Aberrant Blaster", range: 5, angle: 315, cooldown: 40000),
                        new TimedTransition(4900, "Randomize")
                    )
                )
            );

            db.Init("Aberrant Blaster",
                new State("Wait",
                    new PlayerWithinTransition(3, "Boom")
                ),
                new State("Boom",
                    new Shoot(10, count: 5, shootAngle: 7),
                    new Decay(0)
                )
            );

            db.Init("Bile of Oryx",
                new Prioritize(
                    new Protect(0.4, "Oryx the Mad God 2", protectionRange: 5, reprotectRange: 4),
                    new Wander(0.5f)
                )
            );

            db.Init("Abomination of Oryx",
                new State("Shoot",
                    new Shoot(1, count: 3, shootAngle: 5, index: 0),
                    new Shoot(1, count: 5, shootAngle: 5, index: 1),
                    new Shoot(1, count: 7, shootAngle: 5, index: 2),
                    new Shoot(1, count: 5, shootAngle: 5, index: 3),
                    new Shoot(1, count: 3, shootAngle: 5, index: 4),
                    new TimedTransition(1000, "Wait")
                ),
                new State("Wait",
                    new PlayerWithinTransition(2, "Shoot")
                ),
                new Prioritize(
                    new Charge(speed: 3, range: 10, cooldown: 3000),
                    new Wander(0.5f)
                )
            );
        }
    }
}
