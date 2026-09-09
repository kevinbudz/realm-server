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
                    new Orbit(1.7f, 2, target: "Fire Golem", acquireRange: 15, speedVariance: 0, radiusVariance: 0),
                    new Orbit(1.7f, 2, target: "Metal Golem", acquireRange: 15, speedVariance: 0, radiusVariance: 0)
                ),
                new Decay(16000)
            );
            db.Init("Green Satellite",
                new Prioritize(
                    new Orbit(1.1f, 2, target: "Darkness Golem", acquireRange: 15, speedVariance: 0,
                        radiusVariance: 0),
                    new Orbit(1.1f, 2, target: "Earth Golem", acquireRange: 15, speedVariance: 0, radiusVariance: 0)
                ),
                new Decay(16000)
            );
            db.Init("Blue Satellite",
                new Prioritize(
                    new Orbit(1.1f, 2, target: "Clockwork Golem", acquireRange: 15, speedVariance: 0,
                        radiusVariance: 0),
                    new Orbit(1.1f, 2, target: "Paper Golem", acquireRange: 15, speedVariance: 0, radiusVariance: 0)
                ),
                new Decay(16000)
            );
            db.Init("Gray Satellite 1",
                new Shoot(6, count: 3, shootAngle: 34, predictive: 0.3f, cooldown: 850),
                new Prioritize(
                    new Orbit(2.2f, 0.75f, target: "Red Satellite", acquireRange: 15, speedVariance: 0,
                        radiusVariance: 0),
                    new Orbit(2.2f, 0.75f, target: "Blue Satellite", acquireRange: 15, speedVariance: 0,
                        radiusVariance: 0)
                ),
                new Decay(16000)
            );
            db.Init("Gray Satellite 2",
                new Shoot(7, predictive: 0.3f, cooldown: 600),
                new Prioritize(
                    new Orbit(2.2f, 0.75f, target: "Green Satellite", acquireRange: 15, speedVariance: 0,
                        radiusVariance: 0),
                    new Orbit(2.2f, 0.75f, target: "Blue Satellite", acquireRange: 15, speedVariance: 0,
                        radiusVariance: 0)
                ),
                new Decay(16000)
            );
            db.Init("Gray Satellite 3",
                new Shoot(7, count: 5, shootAngle: 72, cooldown: 3200, cooldownOffset: 600),
                new Shoot(7, count: 4, shootAngle: 90, cooldown: 3200, cooldownOffset: 1400),
                new Shoot(7, count: 5, shootAngle: 72, defaultAngle: 36, cooldown: 3200, cooldownOffset: 2200),
                new Shoot(7, count: 4, shootAngle: 90, defaultAngle: 45, cooldown: 3200, cooldownOffset: 3000),
                new Prioritize(
                    new Orbit(2.2f, 0.75f, target: "Red Satellite", acquireRange: 15, speedVariance: 0,
                        radiusVariance: 0),
                    new Orbit(2.2f, 0.75f, target: "Green Satellite", acquireRange: 15, speedVariance: 0,
                        radiusVariance: 0)
                ),
                new Decay(16000)
            );
            db.Init("Earth Golem",
                new State("idle",
                    new PlayerWithinTransition(11, "player_nearby")
                ),
                new State("player_nearby",
                    new Shoot(8, count: 2, shootAngle: 12, cooldown: 600),
                    new State("first_satellites",
                        new Spawn("Green Satellite", maxChildren: 1, cooldown: 200),
                        new Spawn("Gray Satellite 3", maxChildren: 1, cooldown: 200),
                        new TimedTransition(300, "next_satellite")
                    ),
                    new State("next_satellite",
                        new Spawn("Gray Satellite 3", maxChildren: 1, cooldown: 200),
                        new TimedTransition(200, "follow")
                    ),
                    new State("follow",
                        new Prioritize(
                            new StayAbove(1.4f, 65),
                            new Follow(1.4f, range: 3),
                            new Wander(0.8f)
                        ),
                        new TimedTransition(2000, "wander1")
                    ),
                    new State("wander1",
                        new Prioritize(
                            new StayAbove(1.55f, 65),
                            new Wander(0.55f)
                        ),
                        new TimedTransition(4000, "circle")
                    ),
                    new State("circle",
                        new Prioritize(
                            new StayAbove(1.2f, 65),
                            new Orbit(1.2f, 5.4f, acquireRange: 11)
                        ),
                        new TimedTransition(4000, "wander2")
                    ),
                    new State("wander2",
                        new Prioritize(
                            new StayAbove(0.55f, 65),
                            new Wander(0.55f)
                        ),
                        new TimedTransition(3000, "back_and_forth")
                    ),
                    new State("back_and_forth",
                        new Prioritize(
                            new StayAbove(0.55f, 65),
                            new BackAndForth(0.8f)
                        ),
                        new TimedTransition(3000, "first_satellites")
                    )
                ),
                new Reproduce(densityMax: 1),
                new TierLoot(2, TierLoot.LootType.Ring, 0.02f),
                new ItemLoot("Health Potion", 0.03f)
            );
            db.Init("Paper Golem",
                new State("idle",
                    new PlayerWithinTransition(11, "player_nearby")
                ),
                new State("player_nearby",
                    new Spawn("Blue Satellite", maxChildren: 1, cooldown: 200),
                    new Spawn("Gray Satellite 1", maxChildren: 1, cooldown: 200),
                    new Shoot(10, predictive: 0.5f, cooldown: 700),
                    new Prioritize(
                        new StayAbove(1.4f, 65),
                        new Follow(1, range: 3, duration: 3000, cooldown: 3000),
                        new Wander(0.4f)
                    ),
                    new TimedTransition(12000, "idle")
                ),
                new Reproduce(densityMax: 1),
                new TierLoot(5, TierLoot.LootType.Weapon, 0.02f),
                new ItemLoot("Health Potion", 0.03f)
            );
            db.Init("Fire Golem",
                new State("idle",
                    new PlayerWithinTransition(11, "player_nearby")
                ),
                new State("player_nearby",
                    new Prioritize(
                        new StayAbove(1.4f, 65),
                        new Follow(1, range: 3, duration: 3000, cooldown: 3000),
                        new Wander(0.4f)
                    ),
                    new Spawn("Red Satellite", maxChildren: 1, cooldown: 200),
                    new Spawn("Gray Satellite 1", maxChildren: 1, cooldown: 200),
                    new State("slowshot",
                        new Shoot(10, index: 0, predictive: 0.5f, cooldown: 300, cooldownOffset: 600),
                        new TimedTransition(5000, "megashot")
                    ),
                    new State("megashot",
                        new Flash(0xffffffff, 0.2f, 5),
                        new Shoot(10, index: 1, predictive: 0.2f, cooldown: 90, cooldownOffset: 1000),
                        new TimedTransition(1200, "slowshot")
                    )
                ),
                new Reproduce(densityMax: 1),
                new TierLoot(6, TierLoot.LootType.Armor, 0.015f),
                new ItemLoot("Health Potion", 0.03f)
            );
            db.Init("Darkness Golem",
                new State("idle",
                    new PlayerWithinTransition(11, "player_nearby")
                ),
                new State("player_nearby",
                    new State("first_satellites",
                        new Spawn("Green Satellite", maxChildren: 1, cooldown: 200),
                        new Spawn("Gray Satellite 2", maxChildren: 1, cooldown: 200),
                        new TimedTransition(200, "next_satellite")
                    ),
                    new State("next_satellite",
                        new Spawn("Gray Satellite 2", maxChildren: 1, cooldown: 200),
                        new TimedTransition(200, "follow")
                    ),
                    new State("follow",
                        new Shoot(6, index: 0, cooldown: 200),
                        new Prioritize(
                            new StayAbove(1.2f, 65),
                            new Follow(1.2f, range: 1),
                            new Wander(0.5f)
                        ),
                        new TimedTransition(3000, "wander1")
                    ),
                    new State("wander1",
                        new Shoot(6, index: 0, cooldown: 200),
                        new Prioritize(
                            new StayAbove(0.65f, 65),
                            new Wander(0.65f)
                        ),
                        new TimedTransition(3800, "back_up")
                    ),
                    new State("back_up",
                        new Flash(0xffffffff, 0.2f, 25),
                        new Shoot(9, index: 1, cooldown: 1400, cooldownOffset: 1000),
                        new Prioritize(
                            new StayAbove(0.4f, 65),
                            new StayBack(0.4f, 4),
                            new Wander(0.4f)
                        ),
                        new TimedTransition(5400, "wander2")
                    ),
                    new State("wander2",
                        new Shoot(6, index: 0, cooldown: 200),
                        new Prioritize(
                            new StayAbove(0.65f, 65),
                            new Wander(0.65f)
                        ),
                        new TimedTransition(3800, "first_satellites")
                    )
                ),
                new Reproduce(densityMax: 1),
                new TierLoot(2, TierLoot.LootType.Ring, 0.02f),
                new ItemLoot("Magic Potion", 0.03f)
            );
        }
    }
}
