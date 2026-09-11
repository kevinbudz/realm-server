using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;

namespace RotMG.Game.Logic.Database
{
    //Oryx's Castle AI. Minions and the Chamber portal come from
    //fabianos (this 7.0 XML still has Oryx Pet; betterskillys dropped
    //it). Guardian phases come from betterskillys so they are not
    //tied to fabianos' 256x256 map coordinates — this Castle/0.jm
    //matches the betterskillys/skillys 216x237 layout.
    public class OryxCastle : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Oryx Stone Guardian Right",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new PlayerWithinTransition(14, "Start")
                ),
                new State("Start",
                    new Order(30, "Oryx Stone Guardian Left", "Start"),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xC0C0C0, 0.5, 3),
                    new TimedTransition(1500, "Attack")
                ),
                new State("Attack",
                    new StayCloseToSpawn(0.5, 3),
                    new Wander(0.25f),
                    new Shoot(15, count: 5, shootAngle: 7, index: 0, cooldown: 1200),
                    new Shoot(range: 0, count: 12, index: 1, fixedAngle: 0, rotateAngle: 25, cooldown: 3000),
                    new HpLessTransition(0.75, "Attack2")
                ),
                new State("Attack2",
                    new Follow(0.75, 10, 0),
                    new Shoot(15, count: 12, shootAngle: 12, index: 0, cooldown: 2500),
                    new Shoot(10, count: 4, shootAngle: 90, angleOffset: 0, index: 2, cooldown: 1000),
                    new Shoot(10, count: 4, shootAngle: 90, angleOffset: 18, index: 2, cooldown: 1000, cooldownOffset: 200),
                    new Shoot(10, count: 4, shootAngle: 90, angleOffset: 36, index: 2, cooldown: 1000, cooldownOffset: 400),
                    new Shoot(10, count: 4, shootAngle: 90, angleOffset: 54, index: 2, cooldown: 1000, cooldownOffset: 600),
                    new Shoot(10, count: 4, shootAngle: 90, angleOffset: 72, index: 2, cooldown: 1000, cooldownOffset: 800),
                    new HpLessTransition(0.50, "Attack3")
                ),
                new State("Attack3",
                    new Orbit(1.0, 6, acquireRange: 20, target: "Oryx Stone Guardian Left", speedVariance: 0, radiusVariance: 0),
                    new Shoot(15, count: 3, shootAngle: 7, index: 0, cooldown: 400),
                    new HpLessTransition(0.25, "Attack4")
                ),
                new State("Attack4",
                    new Follow(0.5, 10, 0),
                    new Shoot(range: 0, count: 3, index: 0, fixedAngle: 0, rotateAngle: 12, cooldown: 200)
                ),
                new Threshold(0.01f,
                    new ItemLoot("Ancient Stone Sword", 0.003f)
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Defense", 1)
                )
            );
            db.Init("Oryx Stone Guardian Left",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new PlayerWithinTransition(14, "Start")
                ),
                new State("Start",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xC0C0C0, 0.5, 3),
                    new TimedTransition(1500, "Attack")
                ),
                new State("Attack",
                    new Follow(0.35, 10, 0),
                    new Shoot(15, count: 2, shootAngle: 12, index: 0, cooldown: 500),
                    new HpLessTransition(0.75, "Attack2")
                ),
                new State("Attack2",
                    new Charge(speed: 2, range: 20, cooldown: 1800),
                    new Shoot(range: 0, count: 16, index: 1, fixedAngle: 0, rotateAngle: 25, cooldown: 5000),
                    new HpLessTransition(0.50, "Return1")
                ),
                new State("Return1",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new ReturnToSpawn(speed: 1.40),
                    new TimedTransition(2000, "Attack3")
                ),
                new State("Attack3",
                    new Charge(speed: 2, range: 20, cooldown: 1800),
                    new Shoot(15, count: 4, shootAngle: 12, index: 0, cooldown: 3500),
                    new Shoot(15, count: 4, shootAngle: 12, index: 1, cooldown: 3500, cooldownOffset: 300),
                    new Shoot(15, count: 4, shootAngle: 12, index: 2, cooldown: 3500, cooldownOffset: 600),
                    new HpLessTransition(0.25, "Attack4")
                ),
                new State("Attack4",
                    new StayCloseToSpawn(0.5, 6),
                    new Wander(0.25f),
                    new Shoot(15, count: 3, shootAngle: 14, index: 1, cooldown: 1000),
                    new Shoot(range: 0, count: 10, index: 2, fixedAngle: 0, rotateAngle: 12, cooldown: 3000)
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Defense", 1)
                )
            );
            db.Init("Oryx Guardian TaskMaster",
                new ConditionalEffect(ConditionEffectIndex.Invincible, perm: true),
                new State("Idle",
                    new EntitiesNotExistsTransition(100, "Death", "Oryx Stone Guardian Right", "Oryx Stone Guardian Left")
                ),
                new State("Death",
                    new Spawn("Oryx's Chamber Portal", 1, 1),
                    new Suicide()
                )
            );
            db.Init(new[] { "Oryx's Living Floor Fire Down", "Oryx's Living Floor" },
                new State("Idle",
                    new PlayerWithinTransition(20, "Toss")
                ),
                new State("Toss",
                    new TossObject("Quiet Bomb", 10, cooldown: 1000),
                    new NoPlayerWithinTransition(21, "Idle"),
                    new PlayerWithinTransition(5, "Shoot and Toss")
                ),
                new State("Shoot and Toss",
                    new NoPlayerWithinTransition(21, "Idle"),
                    new NoPlayerWithinTransition(6, "Toss"),
                    new Shoot(range: 0, count: 18, fixedAngle: 0, cooldown: 750, cooldownVariance: 250),
                    new TossObject("Quiet Bomb", 10, cooldown: 1000)
                )
            );
            db.Init("Oryx Knight",
                new State("waiting",
                    new PlayerWithinTransition(10, "rekkings")
                ),
                new State("rekkings",
                    new Prioritize(
                        new Follow(0.6, 10, 3),
                        new Wander(0.2f)
                    ),
                    new Shoot(10, count: 3, shootAngle: 20, index: 0, cooldown: 350),
                    new TimedTransition(5000, "singular")
                ),
                new State("singular",
                    new Prioritize(
                        new Follow(0.7, 10, 3),
                        new Wander(0.2f)
                    ),
                    new Shoot(10, count: 1, index: 0, cooldown: 50),
                    new Shoot(10, count: 1, index: 1, cooldown: 1000),
                    new Shoot(10, count: 1, index: 2, cooldown: 450),
                    new TimedTransition(2500, "rekkings")
                )
            );
            db.Init("Oryx Pet",
                new State("idle",
                    new PlayerWithinTransition(10, "attack")
                ),
                new State("attack",
                    new Prioritize(
                        new Follow(0.6, 10, 0),
                        new Wander(0.2f)
                    ),
                    new Shoot(10, count: 2, shootAngle: 20, index: 0, cooldown: 1),
                    new Shoot(10, count: 1, index: 0, cooldown: 1)
                )
            );
            db.Init("Oryx Insect Commander",
                new State("swarm",
                    new Wander(0.2f),
                    new Reproduce("Oryx Insect Minion", 10, 20, cooldown: 2000),
                    new Shoot(10, count: 1, index: 0, cooldown: 900)
                )
            );
            db.Init("Oryx Insect Minion",
                new State("swarming",
                    new Prioritize(
                        new StayCloseToSpawn(0.4, 8),
                        new Follow(0.8, 10, 1),
                        new Wander(0.2f)
                    ),
                    new Shoot(10, count: 5, index: 0, cooldown: 1500),
                    new Shoot(10, count: 1, index: 0, cooldown: 230)
                )
            );
            db.Init("Oryx Suit of Armor",
                new State("idle",
                    new PlayerWithinTransition(8, "wake")
                ),
                new State("wake",
                    new HpLessTransition(0.99, "attack")
                ),
                new State("attack",
                    new Prioritize(
                        new Follow(0.4, 10, 2),
                        new Wander(0.2f)
                    ),
                    new SetAltTexture(1),
                    new Shoot(10, count: 2, shootAngle: 15, index: 0, cooldown: 600),
                    new HpLessTransition(0.2, "heal")
                ),
                new State("heal",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new SetAltTexture(0),
                    new Shoot(10, count: 6, index: 0, cooldown: 200),
                    new HealSelf(cooldown: 200, amount: 200),
                    new TimedTransition(1500, "attack")
                )
            );
            db.Init("Oryx Eye Warrior",
                new State("idle",
                    new PlayerWithinTransition(10, "attack")
                ),
                new State("attack",
                    new Follow(0.6, 10, 0),
                    new Shoot(10, count: 5, index: 0, cooldown: 1000),
                    new Shoot(10, count: 1, index: 1, cooldown: 500)
                )
            );
            db.Init("Oryx Brute",
                new State("idle",
                    new PlayerWithinTransition(10, "piddle")
                ),
                new State("piddle",
                    new Prioritize(
                        new Follow(0.4, 10, 1),
                        new Wander(0.2f)
                    ),
                    new Shoot(10, count: 5, index: 1, cooldown: 1000),
                    new Reproduce("Oryx Eye Warrior", 10, 4, cooldown: 1750),
                    new TimedTransition(5000, "charge")
                ),
                new State("charge",
                    new Prioritize(
                        new Follow(1.2, 10, 1),
                        new Wander(0.3f)
                    ),
                    new Shoot(10, count: 5, index: 1, cooldown: 1000),
                    new Shoot(10, count: 5, index: 2, cooldown: 750),
                    new Reproduce("Oryx Eye Warrior", 10, 4, cooldown: 1750),
                    new Shoot(10, count: 3, shootAngle: 10, index: 0, cooldown: 300),
                    new TimedTransition(4000, "piddle")
                )
            );
            db.Init("Quiet Bomb",
                new ConditionalEffect(ConditionEffectIndex.Invincible, perm: true),
                new State("Idle",
                    new State("Tex1",
                        new TimedTransition(250, "Tex2")
                    ),
                    new State("Tex2",
                        new SetAltTexture(1),
                        new TimedTransition(250, "Tex3")
                    ),
                    new State("Tex3",
                        new SetAltTexture(0),
                        new TimedTransition(250, "Tex4")
                    ),
                    new State("Tex4",
                        new SetAltTexture(1),
                        new TimedTransition(250, "Explode")
                    )
                ),
                new State("Explode",
                    new SetAltTexture(0),
                    new Shoot(range: 0, count: 18, fixedAngle: 0),
                    new Suicide()
                )
            );
        }
    }
}
