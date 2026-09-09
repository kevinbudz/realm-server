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
    public class OceanTrench : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Coral Gift",
                //credits to GhostMaree, ???
                
                    new State("Texture1",
                        new SetAltTexture(1),
                        new TimedTransition("Texture2", 500)
                        ),
                new State("Texture2",
                        new SetAltTexture(2),
                        new TimedTransition("Texture0", 500)
                        ),
                new State("Texture0",
                        new SetAltTexture(0),
                        new TimedTransition("Texture1", 500)
                        ),
                new Threshold(0.01f,
                            new ItemLoot("Coral Juice", 0.3f),
                            new ItemLoot("Potion of Mana", 0.04f),
                            new ItemLoot("Coral Bow", 0.01f),
                            new ItemLoot("Coral Venom Trap", 0.03f),
                            new ItemLoot("Wine Cellar Incantation", 0.02f),
                            new ItemLoot("Coral Silk Armor", 0.04f),
                            new ItemLoot("Coral Ring", 0.04f)
                            ));
            db.Init("Coral Bomb Big",
                new State("Spawning",
                        new TossObject("Coral Bomb Small", 1, angle: 30, cooldown: 500),
                        new TossObject("Coral Bomb Small", 1, angle: 90, cooldown: 500),
                        new TossObject("Coral Bomb Small", 1, angle: 150, cooldown: 500),
                        new TossObject("Coral Bomb Small", 1, angle: 210, cooldown: 500),
                        new TossObject("Coral Bomb Small", 1, angle: 270, cooldown: 500),
                        new TossObject("Coral Bomb Small", 1, angle: 330, cooldown: 500),
                        new TimedTransition("Attack", 500)
                        ),
                new State("Attack",
                        new Shoot(4.4f, count: 5, fixedAngle: 0, shootAngle: 70),
                        new Suicide()
                        ));
            db.Init("Coral Bomb Small",
                new Shoot(3.8f, count: 5, fixedAngle: 0, shootAngle: 70),
                new Suicide());
            db.Init("Deep Sea Beast",
                new ChangeSize(11, 100),
                new Prioritize(
                        new StayCloseToSpawn(0.2, 2),
                        new Follow(0.2, acquireRange: 4, range: 1)
                            ),
                new Shoot(1.8f, count: 1, index: 0, cooldown: 1000),
                new Shoot(2.5f, count: 1, index: 1, cooldown: 1000),
                new Shoot(3.3f, count: 1, index: 2, cooldown: 1000),
                new Shoot(4.2f, count: 1, index: 3, cooldown: 1000));
            db.Init("Thessal the Mermaid Goddess",
                new TransformOnDeath("Thessal the Mermaid Goddess Wounded", probability: 0.1),
                new TransformOnDeath("Thessal Dropper"),
                new State("Start",
                        new Prioritize(
                            new Wander(0.3f),
                            new Follow(0.3, acquireRange: 10, range: 2)
                        ),
                        new EntityNotExistsTransition("Deep Sea Beast", 20, "Spawning Deep"),
                        new HpLessTransition(1, "Attack1")
                        ),
                new State("Main",
                        new Prioritize(
                            new Wander(0.3f),
                            new Follow(0.3, acquireRange: 10, range: 2)
                        ),
                        new TimedTransition("Attack1", 0)
                        ),
                new State("Main 2",
                        new Prioritize(
                            new Wander(0.3f),
                            new Follow(0.3, acquireRange: 10, range: 2)
                        ),
                        new TimedTransition("Attack2", 0)
                        ),
                new State("Spawning Bomb",
                        new TossObject("Coral Bomb Big", angle: 45),
                        new TossObject("Coral Bomb Big", angle: 135),
                        new TossObject("Coral Bomb Big", angle: 225),
                        new TossObject("Coral Bomb Big", angle: 315),
                        new TimedTransition("Main", 1000)
                        ),
                new State("Spawning Bomb Attack2",
                        new TossObject("Coral Bomb Big", angle: 45),
                        new TossObject("Coral Bomb Big", angle: 135),
                        new TossObject("Coral Bomb Big", angle: 225),
                        new TossObject("Coral Bomb Big", angle: 315),
                        new TimedTransition("Attack2", 1000)
                        ),
                new State("Spawning Deep",
                        new TossObject("Deep Sea Beast", 14, angle: 0, cooldownOffset: 0),
                        new TossObject("Deep Sea Beast", 14, angle: 90, cooldownOffset: 0),
                        new TossObject("Deep Sea Beast", 14, angle: 180, cooldownOffset: 0),
                        new TossObject("Deep Sea Beast", 14, angle: 270, cooldownOffset: 0),
                        new TimedTransition("Start", 1000)
                        ),
                new State("Attack1",
                        new HpLessTransition(0.5, "Attack2"),
                        //new TimedTransition(3000, "Trident", randomized: true),
                        new TimedTransition("Yellow Wall", 3000),
                        new TimedTransition("Super Trident", 3000),
                        new TimedTransition("Thunder Swirl", 3000),
                        new TimedTransition("Spawning Bomb", 3000)
                    ),
                new State("Thunder Swirl",
                        new Shoot(8.8f, count: 8, shootAngle: 360 / 8, index: 0),
                        new TimedTransition("Thunder Swirl 2", 500)
                    ),
                new State("Thunder Swirl 2",
                        new Shoot(8.8f, count: 8, shootAngle: 360 / 8, index: 0),
                        new TossObject("Coral Bomb Big"),
                        new TimedTransition("Thunder Swirl 3", 500)
                    ),
                new State("Thunder Swirl 3",
                        new Shoot(8.8f, count: 8, shootAngle: 360 / 8, index: 0),
                        new TimedTransition("Main", 100)
                    ),
                new State("Thunder Swirl Attack2",
                        new Shoot(8.8f, count: 16, shootAngle: 360 / 16, index: 0),
                        new TimedTransition("Thunder Swirl 2 Attack2", 500)
                    ),
                new State("Thunder Swirl 2 Attack2",
                        new Shoot(8.8f, count: 16, shootAngle: 360 / 16, index: 0),
                        new TossObject("Coral Bomb Big"),
                        new TimedTransition("Thunder Swirl 3 Attack2", 500)
                    ),
                new State("Thunder Swirl 3 Attack2",
                        new Shoot(8.8f, count: 16, shootAngle: 360 / 16, index: 0),
                        new TimedTransition("Main 2", 100)
                    ),
                //new State("Trident",
                    //new Shoot(21, count: 8, shootAngle: 360 / 4, projectileIndex: 1),
                    //new TimedTransition(100, "Start")
                    //),
                    new State("Super Trident",
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 0),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 90),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 180),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 270),
                        new TossObject("Coral Bomb Big"),
                        new TimedTransition("Super Trident 2", 250)
                    ),
                new State("Super Trident 2",
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 45),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 135),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 225),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 315),
                        new TossObject("Coral Bomb Big"),
                        new TimedTransition("Main", 100)
                    ),
                new State("Super Trident Attack2",
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 0),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 90),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 180),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 270),
                        new TossObject("Coral Bomb Big"),
                        new TimedTransition("Super Trident 2 Attack2", 250)
                    ),
                new State("Super Trident 2 Attack2",
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 45),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 135),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 225),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 315),
                        new TimedTransition("Super Trident 3 Attack2", 250)
                    ),
                new State("Super Trident 3 Attack2",
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 0),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 90),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 180),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 270),
                        new TossObject("Coral Bomb Big"),
                        new TimedTransition("Super Trident 4 Attack2", 250)
                    ),
                new State("Super Trident 4 Attack2",
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 45),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 135),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 225),
                        new Shoot(21, count: 2, shootAngle: 25, index: 2, angleOffset: 315),
                        new TimedTransition("Main 2", 100)
                    ),
                new State("Yellow Wall",
                        new Flash(0xFFFF00, .1, 15),
                        new Prioritize(
                            new StayCloseToSpawn(0.3, 1)
                        ),
                        new Shoot(18, count: 30, fixedAngle: 6, index: 3),
                        new TimedTransition("Yellow Wall 2", 500)
                    ),
                new State("Yellow Wall 2",
                        new Flash(0xFFFF00, .1, 15),
                        new Shoot(18, count: 30, fixedAngle: 6, index: 3),
                        new TimedTransition("Yellow Wall 3", 500)
                    ),
                new State("Yellow Wall 3",
                        new Flash(0xFFFF00, .1, 15),
                        new Shoot(18, count: 30, fixedAngle: 6, index: 3),
                        new TimedTransition("Main", 100)
                    ),
                new State("Yellow Wall Attack2",
                        new Flash(0xFFFF00, .1, 15),
                        new Prioritize(
                            new StayCloseToSpawn(0.3, 1)
                        ),
                        new Shoot(18, count: 30, fixedAngle: 6, index: 3),
                        new TimedTransition("Yellow Wall 2 Attack2", 500)
                    ),
                new State("Yellow Wall 2 Attack2",
                        new Flash(0xFFFF00, .1, 15),
                        new Shoot(18, count: 30, fixedAngle: 6, index: 3),
                        new TimedTransition("Yellow Wall 3 Attack2", 500)
                    ),
                new State("Yellow Wall 3 Attack2",
                        new Flash(0xFFFF00, .1, 15),
                        new Shoot(18, count: 30, fixedAngle: 6, index: 3),
                        new TimedTransition("Main 2", 100)
                    ),
                new State("Attack2",
                        //new TimedTransition(500, "Trident", randomized: true),
                        new TimedTransition("Yellow Wall Attack2", 500),
                        new TimedTransition("Super Trident Attack2", 500),
                        new TimedTransition("Thunder Swirl Attack2", 500),
                        new TimedTransition("Spawning Bomb", 500)
                    ),
                new Threshold(0.32f,
                            new ItemLoot("Potion of Mana", 1)
                            ),
                new Threshold(0.1f,
                            new ItemLoot("Coral Juice", 0.3f),
                            new ItemLoot("Coral Bow", 0.01f),
                            new ItemLoot("Coral Venom Trap", 0.03f),
                            new ItemLoot("Wine Cellar Incantation", 0.02f),
                            new ItemLoot("Coral Silk Armor", 0.04f),
                            new ItemLoot("Coral Ring", 0.04f)
                            ));
            db.Init("Thessal Dropper",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new TransformOnDeath("Ocean Vent"),
                new State("Idle",
                        new EntityNotExistsTransition("Thessal the Mermaid Goddess", 100, "Suicide")
                        ),
                new State("Suicide",
                        new Suicide()
                        ));
            db.Init("Thessal the Mermaid Goddess Wounded",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new Taunt("Is King Alexander alive?"),
                new State("Texture1",
                        new SetAltTexture(1),
                        new TimedTransition("Texture2", 250), new TimedTransition("Fail", 12000)),
                new State("Texture2",
                        new SetAltTexture(0),
                        new TimedTransition("Texture1", 250), new TimedTransition("Fail", 12000)),
                new State("Prize",
                        new Taunt("Thank you kind sailor."),
                        new TossObject("Coral Gift", range: 5, angle: 45),
                        new TossObject("Coral Gift", range: 5, angle: 135),
                        new TossObject("Coral Gift", range: 5, angle: 235),
                        new TimedTransition("Suicide", 0), new TimedTransition("Fail", 12000)),
                new State("Fail",
                        new Taunt("You speak LIES!!"),
                        new TimedTransition("Suicide", 0), new TimedTransition("Fail", 12000)),
                new State("Suicide",
                        new Suicide(), new TimedTransition("Fail", 12000)));
            db.Init("Fishman Warrior",
                new State("Start",
                    new Prioritize(
                        new Follow(0.6, acquireRange: 9, range: 2)
                            ),
                   new Orbit(0.6, 5, 9, target: null),
                   new Shoot(9, 3, index: 0, shootAngle: 10, cooldown: 500),
                   new Shoot(9, count: 6, fixedAngle: 0, index: 2, cooldown: 2000),
                   new NoPlayerWithinTransition(9, "Range Shoot")
                        ),
                new State("Range Shoot",
                        new Prioritize(
                            new StayCloseToSpawn(0.2, 3),
                            new Wander(0.3f)
                            ),
                        new Shoot(12, 1, index: 1, cooldownOffset: 250),
                        new PlayerWithinTransition(9, "Start")
                        ));
            db.Init("Fishman",
                new Prioritize(
                        new Follow(0.7, acquireRange: 9, range: 1)
                            ),
                new Shoot(9, count: 1, index: 1, cooldown: 2000),
                new Shoot(9, 1, index: 0, cooldownOffset: 250),
                new Shoot(9, 3, index: 0, shootAngle: 10, cooldownOffset: 500));
            db.Init("Sea Mare",
                new Charge(2.0, 8, 4000),
                new Wander(0.4f),
                new State("Shoot 1",
                        new Shoot(9, count: 3, index: 1, cooldown: 500),
                        new TimedTransition("Shoot 2", 5000)
                        ),
                new State("Shoot 2",
                        new Shoot(10, count: 8, shootAngle: 10, index: 0, cooldownOffset: 500),
                        new Shoot(10, count: 8, shootAngle: 10, angleOffset: 45, index: 0, cooldownOffset: 1000),
                        new Shoot(10, count: 8, shootAngle: 10, angleOffset: 135, index: 0, cooldownOffset: 1500),
                        new TimedTransition("Shoot 1", 3000)
                    ));
            db.Init("Sea Horse",
                new Orbit(0.2, 2, acquireRange: 10, target: "Sea Mare"),
                new Wander(0.2f),
                new State("Shoot 1",
                        new Shoot(9, count: 1, index: 0, cooldownOffset: 250),
                        new Shoot(9, count: 2, shootAngle: 5, index: 0, cooldownOffset: 500),
                        new Shoot(9, count: 3, shootAngle: 5, index: 0, cooldownOffset: 750)
                        ));
            db.Init("Giant Squid",
                new Shoot(10, 1, index: 0, cooldown: 100),
                new Follow(0.8, acquireRange: 12, range: 1),
                new State("Toss",
                        new TossObject("Ink Bubble"),
                        new TimedTransition("Toss 2", 100)
                        ),
                new State("Toss 2",
                        new TossObject("Ink Bubble"),
                        new TimedTransition("Attack 1", 100)
                        ),
                new State("Attack 1",
                        new Shoot(10, 4, shootAngle: 15, index: 1, cooldown: 250),
                        new TimedTransition("Toss", 20000)
                        ));
            db.Init("Ink Bubble",
                new Shoot(10, 1, index: 0, cooldown: 100));
            db.Init("Sea Slurp Home",
                new Spawn("Grey Sea Slurp", maxChildren: 8, initialSpawn: 0.5));
            db.Init("Grey Sea Slurp",
                new StayCloseToSpawn(0.5, 10),
                new State("Shoot and Move",
                        new Prioritize(
                            new Follow(0.3, acquireRange: 10, range: 4),
                            new Wander(0.2f)
                            ),
                        new Shoot(8, 1, index: 0, cooldown: 300),
                        new TimedTransition("Wall Shoot", 900)
                        ),
                new State("Wall Shoot",
                        new Shoot(8, 6, index: 1, fixedAngle: 2, cooldown: 750),
                        new TimedTransition("Shoot and Move", 1500)
                        ));
        }
    }
}
