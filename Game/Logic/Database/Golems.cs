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
    public class Golems : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Red Satellite",
                new Prioritize(
                        new Orbit(1.7, 2, target: "Fire Golem", acquireRange: 15, speedVariance: 0, radiusVariance: 0),
                        new Orbit(1.7, 2, target: "Metal Golem", acquireRange: 15, speedVariance: 0, radiusVariance: 0)
                        ),
                new Decay(16000));
            db.Init("Green Satellite",
                new Prioritize(
                        new Orbit(1.1, 2, target: "Darkness Golem", acquireRange: 15, speedVariance: 0,
                            radiusVariance: 0),
                        new Orbit(1.1, 2, target: "Earth Golem", acquireRange: 15, speedVariance: 0, radiusVariance: 0)
                        ),
                new Decay(16000));
            db.Init("Blue Satellite",
                new Prioritize(
                        new Orbit(1.1, 2, target: "Clockwork Golem", acquireRange: 15, speedVariance: 0,
                            radiusVariance: 0),
                        new Orbit(1.1, 2, target: "Paper Golem", acquireRange: 15, speedVariance: 0, radiusVariance: 0)
                        ),
                new Decay(16000));
            db.Init("Gray Satellite 1",
                new Shoot(6, count: 3, shootAngle: 34, predictive: 0.3f, cooldown: 850),
                new Prioritize(
                        new Orbit(2.2, 0.75, target: "Red Satellite", acquireRange: 15, speedVariance: 0,
                            radiusVariance: 0),
                        new Orbit(2.2, 0.75, target: "Blue Satellite", acquireRange: 15, speedVariance: 0,
                            radiusVariance: 0)
                        ),
                new Decay(16000));
            db.Init("Gray Satellite 2",
                new Shoot(7, predictive: 0.3f, cooldown: 600),
                new Prioritize(
                        new Orbit(2.2, 0.75, target: "Green Satellite", acquireRange: 15, speedVariance: 0,
                            radiusVariance: 0),
                        new Orbit(2.2, 0.75, target: "Blue Satellite", acquireRange: 15, speedVariance: 0,
                            radiusVariance: 0)
                        ),
                new Decay(16000));
            db.Init("Gray Satellite 3",
                new Shoot(7, count: 5, shootAngle: 72, cooldown: 3200, cooldownOffset: 600),
                new Shoot(7, count: 4, shootAngle: 90, cooldown: 3200, cooldownOffset: 1400),
                new Shoot(7, count: 5, shootAngle: 72, defaultAngle: 36, cooldown: 3200, cooldownOffset: 2200),
                new Shoot(7, count: 4, shootAngle: 90, defaultAngle: 45, cooldown: 3200, cooldownOffset: 3000),
                new Prioritize(
                        new Orbit(2.2, 0.75, target: "Red Satellite", acquireRange: 15, speedVariance: 0,
                            radiusVariance: 0),
                        new Orbit(2.2, 0.75, target: "Green Satellite", acquireRange: 15, speedVariance: 0,
                            radiusVariance: 0)
                        ),
                new Decay(16000));
            db.Init("Earth Golem",
                new Reproduce(densityMax: 1),
                new State("idle",
                        new PlayerWithinTransition(11, "player_nearby")
                        ),
                new State("player_nearby",
                        new Shoot(8, count: 2, shootAngle: 12, cooldown: 600),
                        new State("first_satellites",
                            new Spawn("Green Satellite", maxChildren: 1, cooldown: 200),
                            new Spawn("Gray Satellite 3", maxChildren: 1, cooldown: 200),
                            new TimedTransition("next_satellite", 300)
                            ),
                        new State("next_satellite",
                            new Spawn("Gray Satellite 3", maxChildren: 1, cooldown: 200),
                            new TimedTransition("follow", 200)
                            ),
                        new State("follow",
                            new Prioritize(
                                new StayAbove(1.4, 65),
                                new Follow(1.4, range: 3),
                                new Wander(0.8f)
                                ),
                            new TimedTransition("wander1", 2000)
                            ),
                        new State("wander1",
                            new Prioritize(
                                new StayAbove(1.55, 65),
                                new Wander(0.55f)
                                ),
                            new TimedTransition("circle", 4000)
                            ),
                        new State("circle",
                            new Prioritize(
                                new StayAbove(1.2, 65),
                                new Orbit(1.2, 5.4, acquireRange: 11)
                                ),
                            new TimedTransition("wander2", 4000)
                            ),
                        new State("wander2",
                            new Prioritize(
                                new StayAbove(0.55, 65),
                                new Wander(0.55f)
                                ),
                            new TimedTransition("back_and_forth", 3000)
                            ),
                        new State("back_and_forth",
                            new Prioritize(
                                new StayAbove(0.55, 65),
                                new BackAndForth(0.8)
                                ),
                            new TimedTransition("first_satellites", 3000)
                            )
                        ),
                new TierLoot(2, TierLoot.LootType.Ring, 0.02f),
                new ItemLoot("Health Potion", 0.03f));
            db.Init("Paper Golem",
                new Reproduce(densityMax: 1),
                new State("idle",
                        new PlayerWithinTransition(11, "player_nearby")
                        ),
                new State("player_nearby",
                        new Spawn("Blue Satellite", maxChildren: 1, cooldown: 200),
                        new Spawn("Gray Satellite 1", maxChildren: 1, cooldown: 200),
                        new Shoot(10, predictive: 0.5f, cooldown: 700),
                        new Prioritize(
                            new StayAbove(1.4, 65),
                            new Follow(1, range: 3, duration: 3000, cooldown: 3000),
                            new Wander(0.4f)
                            ),
                        new TimedTransition("idle", 12000)
                        ),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.02f),
                new ItemLoot("Health Potion", 0.03f));
            db.Init("Fire Golem",
                new Reproduce(densityMax: 1),
                new State("idle",
                        new PlayerWithinTransition(11, "player_nearby")
                        ),
                new State("player_nearby",
                        new Prioritize(
                            new StayAbove(1.4, 65),
                            new Follow(1, range: 3, duration: 3000, cooldown: 3000),
                            new Wander(0.4f)
                            ),
                        new Spawn("Red Satellite", maxChildren: 1, cooldown: 200),
                        new Spawn("Gray Satellite 1", maxChildren: 1, cooldown: 200),
                        new State("slowshot",
                            new Shoot(10, index: 0, predictive: 0.5f, cooldown: 300, cooldownOffset: 600),
                            new TimedTransition("megashot", 5000)
                            ),
                        new State("megashot",
                            new Flash(0xffffffff, 0.2, 5),
                            new Shoot(10, index: 1, predictive: 0.2f, cooldown: 90, cooldownOffset: 1000),
                            new TimedTransition("slowshot", 1200)
                            )
                        ),
                new TierLoot(6, TierLoot.LootType.Armor, 0.015f),
                new ItemLoot("Health Potion", 0.03f));
            db.Init("Darkness Golem",
                new Reproduce(densityMax: 1),
                new State("idle",
                        new PlayerWithinTransition(11, "player_nearby")
                        ),
                new State("player_nearby",
                        new State("first_satellites",
                            new Spawn("Green Satellite", maxChildren: 1, cooldown: 200),
                            new Spawn("Gray Satellite 2", maxChildren: 1, cooldown: 200),
                            new TimedTransition("next_satellite", 200)
                            ),
                        new State("next_satellite",
                            new Spawn("Gray Satellite 2", maxChildren: 1, cooldown: 200),
                            new TimedTransition("follow", 200)
                            ),
                        new State("follow",
                            new Shoot(6, index: 0, cooldown: 200),
                            new Prioritize(
                                new StayAbove(1.2, 65),
                                new Follow(1.2, range: 1),
                                new Wander(0.5f)
                                ),
                            new TimedTransition("wander1", 3000)
                            ),
                        new State("wander1",
                            new Shoot(6, index: 0, cooldown: 200),
                            new Prioritize(
                                new StayAbove(0.65, 65),
                                new Wander(0.65f)
                                ),
                            new TimedTransition("back_up", 3800)
                            ),
                        new State("back_up",
                            new Flash(0xffffffff, 0.2, 25),
                            new Shoot(9, index: 1, cooldown: 1400, cooldownOffset: 1000),
                            new Prioritize(
                                new StayAbove(0.4, 65),
                                new StayBack(0.4, 4),
                                new Wander(0.4f)
                                ),
                            new TimedTransition("wander2", 5400)
                            ),
                        new State("wander2",
                            new Shoot(6, index: 0, cooldown: 200),
                            new Prioritize(
                                new StayAbove(0.65, 65),
                                new Wander(0.65f)
                                ),
                            new TimedTransition("first_satellites", 3800)
                            )
                        ),
                new TierLoot(2, TierLoot.LootType.Ring, 0.02f),
                new ItemLoot("Magic Potion", 0.03f));
        }
    }
}
