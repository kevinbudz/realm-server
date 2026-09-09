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
    public class Tomb : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Tomb Defender",
                new State("idle",
                        new Taunt(true, "THIS WILL NOW BE YOUR TOMB!"),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new HpLessTransition(.989, "weakning")
                        ),
                new State("weakning",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Taunt(true, "Impudence! I am an Immortal, I needn't waste time on you!"),
                        new Shoot(50, 20, index: 3, cooldown: 6000),
                        new State("blue shield 1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 1", 3000)
                            ),
                        new State("unset blue shield 1"),
                        new HpLessTransition(.979, "active")
                        ),
                new State("active",
                        new Orbit(.7, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Shoot(50, 8, 45, 2, 0, 0, cooldown: 1000),
                        new Shoot(50, 3, 120, 1, 0, 0, cooldown: 5000),
                        new Shoot(50, 5, 72, 0, 0, 0, cooldown: 5000),
                        new HpLessTransition(.7, "boomerang")
                        ),
                new State("boomerang",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Orbit(.6, 3, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Nut, disable our foes!"),
                        new Shoot(50, 1, index: 0, cooldown: 3000),
                        new Shoot(50, 8, index: 2, cooldown: 1000),
                        new Shoot(50, 3, 15, 1, cooldown: 3000),
                        new Shoot(50, 2, 90, 1, cooldown: 3000),
                        new State("blue shield 2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 2", 3000)
                            ),
                        new State("unset blue shield 2"),
                        new HpLessTransition(.55, "double shot")

                        ),
                new State("double shot",
                        new Taunt(true, "Geb, eradicate these cretins from our tomb!"),
                        new Orbit(.7, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(50, 8, index: 2, cooldown: 1000),
                        new Shoot(50, 2, 10, 0, cooldown: 3000),
                        new Shoot(50, 4, 15, 1, cooldown: 3000),
                        new Shoot(50, 2, 90, 1, cooldown: 3000),
                        new HpLessTransition(.4, "artifacts")
                        ),
                new State("artifacts",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Taunt(true, "Nut, let them wish they were dead!"),
                        new Orbit(.6, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(50, 8, index: 2, cooldown: 1000),
                        new Shoot(50, 2, 10, 0, cooldown: 3000),
                        new Shoot(50, 4, 15, 1, cooldown: 3000),
                        new Shoot(50, 2, 90, 1, cooldown: 3000),
                        new Spawn("Pyramid Artifact 1", 1, 0),
                        new Spawn("Pyramid Artifact 2", 1, 0),
                        new State("blue shield 3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 3", 3000)
                            ),
                        new State("unset blue shield 3"),
                        new HpLessTransition(.25, "artifacts 2")
                        ),
                new State("artifacts 2",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Taunt(true, "My artifacts shall prove my wall of defense is impenetrable!"),
                        new Orbit(.6, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(50, 8, index: 2, cooldown: 1000),
                        new Shoot(50, 3, 10, 0, cooldown: 3000),
                        new Shoot(50, 5, 15, 1, cooldown: 3000),
                        new Shoot(50, 2, 80, 1, cooldown: 3000),
                        new Shoot(50, 2, 90, 1, cooldown: 3000),
                        new Spawn("Pyramid Artifact 1", 2, 0),
                        new State("blue shield 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 4", 3000)
                            ),
                        new State("unset blue shield 4"),
                        new HpLessTransition(.06, "rage")
                        ),
                new State("rage",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Taunt(true, "The end of your path is here!"),
                        new Follow(0.6, range: 1, duration: 5000, cooldown: 0),
                        new Flash(0xfFF0000, 1, 9000001),
                        new Shoot(50, 10, 10, 4, cooldown: 750, cooldownOffset: 750),
                        new Shoot(50, 5, 10, 4, angleOffset: 180, cooldown: 500, cooldownOffset: 500),
                        new Shoot(50, 1, index: 0, cooldown: 1000),
                        new Shoot(50, 3, 15, 1, cooldown: 2000),
                        new Shoot(50, 2, 90, 1, cooldown: 2000),
                        new Spawn("Pyramid Artifact 1", 1, 0),
                        new Spawn("Pyramid Artifact 2", 1, 0),
                        new Spawn("Pyramid Artifact 3", 1, 0),
                        new State("blue shield 5",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 5", 3000)
                            ),
                        new State("unset blue shield 5")
                        ),
                new Threshold(0.01f,
                        new ItemLoot("Potion of Life", 1),
                        new ItemLoot("Ring of the Pyramid", 0.04f),
                        new ItemLoot("Tome of Holy Protection", 0.01f),
                        new ItemLoot("Wine Cellar Incantation", 0.05f)
                    ));
            db.Init("Tomb Support",
                new State("idle",
                        new Taunt(true, "ENOUGH OF YOUR VANDALISM!"),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new HpLessTransition(.9875, "weakning")
                        ),
                new State("weakning",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt("Impudence! I am an immortal, I needn't take your seriously."),
                        new Shoot(50, 20, index: 7, cooldown: 6000, cooldownOffset: 2000),
                        new State("blue shield 1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 1", 3000)
                            ),
                        new State("unset blue shield 1"),
                        new HpLessTransition(.97875, "active")
                        ),
                new State("active",
                        new Orbit(.7, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(20, 1, index: 5, cooldown: 1000),
                        new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                        new State("blue shield 2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 2", 3000)
                            ),
                        new State("unset blue shield 2"),
                        new HpLessTransition(.9, "boomerang")
                        ),
                new State("boomerang",
                        new Orbit(.6, 6, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Bes, protect me at once!"),
                        new Shoot(20, 1, index: 5, cooldown: 1000),
                        new Shoot(20, 1, index: 6, cooldown: 3000),
                        new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                        new HpLessTransition(.7, "paralyze")
                        ),
                new State("paralyze",
                        new Orbit(.6, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Geb, eradicate these cretins from our tomb!"),
                        new Shoot(20, 1, index: 5, cooldown: 1000),
                        new Shoot(20, 1, index: 6, cooldown: 3000),
                        new Shoot(999, 2, 10, 8, 0, 180, cooldown: 1000),
                        new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                        new HpLessTransition(.5, "artifacts")
                        ),
                new State("artifacts",
                        new Orbit(.6, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "My artifacts shall make your lethargic lives end much more swiftly!"),
                        new Shoot(20, 1, index: 5, cooldown: 1000),
                        new Shoot(20, 1, index: 6, cooldown: 3000),
                        new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                        new Spawn("Sphinx Artifact 1", 1, 0),
                        new State("blue shield 3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 3", 3000)
                            ),
                        new State("unset blue shield 3"),
                        new HpLessTransition(.3, "double shoot")
                        ),
                new State("double shoot",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(20, 2, 15, 5, cooldown: 1000),
                        new Shoot(20, 2, 15, 6, cooldown: 3000),
                        new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                        new Spawn("Sphinx Artifact 1", 2, 0),
                        new State("blue shield 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 4", 3000)
                            ),
                        new State("unset blue shield 4"),
                        new HpLessTransition(.06, "rage")
                        ),
                new State("rage",
                        new Taunt(true, "This cannot be! You shall not succeed!"),
                        new Follow(0.6, range: 1, duration: 5000, cooldown: 0),
                        new Flash(0xfFF0000, 1, 9000001),
                        new Shoot(20, 1, index: 5, cooldown: 1000),
                        new Shoot(20, 1, 15, 0, cooldown: 750),
                        new Shoot(12, 4, 90, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                        new Shoot(12, 5, 72, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                        new Shoot(12, 6, 60, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                        new Shoot(12, 8, 45, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                        new Shoot(999, 6, 10, 8, angleOffset: 180, cooldown: 500),
                        new Spawn("Sphinx Artifact 1", 1, 0),
                        new State("blue shield 5",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 5", 3000)
                            ),
                        new State("unset blue shield 5")
                        ),
                new Threshold(0.01f,
                        new ItemLoot("Potion of Life", 1),
                        new ItemLoot("Ring of the Sphinx", 0.04f),
                        new ItemLoot("Wine Cellar Incantation", 0.05f)
                    ));
            db.Init("Tomb Attacker",
                new State("idle",
                        new Taunt(true, "ENOUGH OF YOUR VANDALISM!"),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new HpLessTransition(.988, "weakning")
                    ),
                new State("weakning",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(50, 20, index: 3, cooldown: 6000, cooldownOffset: 2000),
                        new State("blue shield 1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 1", 3000)
                            ),
                        new State("unset blue shield 1"),
                        new HpLessTransition(.9788, "active")
                    ),
                new State("active",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(14, 2, 10, 2, cooldown: 500),
                        new Shoot(12, 1, index: 0, cooldown: 2000),
                        new State("Grenade 1",
                            new Grenade(radius: 3, damage: 160, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 2", 1500)
                            ),
                        new State("Grenade 2",
                            new Grenade(radius: 4, damage: 120, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 1", 1500)
                            ),
                        new HpLessTransition(.72, "lets dance")
                        ),
                new State("lets dance",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Bes, protect me at once!"),
                        new Shoot(14, 1, index: 2, cooldown: 500),
                        new Shoot(14, 2, 90, 2, cooldown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, cooldown: 1000),
                        new Shoot(11 + 1 / 5, 8, 45, 1, 0, cooldown: 5000),
                        new Shoot(12, 2, 45, 0, cooldown: 1500),
                        new Shoot(99, 1, index: 4, cooldown: 500),
                        new Spawn("Scarab", 3, 0, cooldown: 10000),
                        new State("blue shield 2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 2", 3000)
                            ),
                        new State("unset blue shield 2",
                            new TimedTransition("Grenade 3", 3000)
                            ),
                        new State("Grenade 3",
                            new Grenade(radius: 3, damage: 160, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 4", 1500)
                            ),
                        new State("Grenade 4",
                            new Grenade(radius: 4, damage: 120, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 3", 1500)
                            ),
                        new HpLessTransition(.675, "more muthafucka")
                        ),
                new State("more muthafucka",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Nut, disable our foes!"),
                        new Spawn("Scarab", 3, 0, cooldown: 10000),
                        new Shoot(14, 2, 10, 2, cooldown: 500),
                        new Shoot(14, 1, index: 2, cooldown: 500),
                        new Shoot(14, 2, 90, 2, cooldown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, cooldown: 1000),
                        new Shoot(11 + 1 / 5, 10, 36, 1, 0, cooldown: 5000),
                        new Shoot(12, 1, index: 0, cooldown: 2000),
                        new Shoot(12, 2, 45, 0, cooldown: 2000),
                        new Shoot(99, 1, index: 4, cooldown: 500),
                        new Shoot(99, 1, index: 4, angleOffset: 90, cooldown: 500),
                        new State("Grenade 5",
                            new Grenade(radius: 3, damage: 160, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 6", 1500)
                            ),
                        new State("Grenade 6",
                            new Grenade(radius: 4, damage: 120, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 5", 1500)
                            ),
                        new HpLessTransition(.4, "artifacts")
                        ),
                new State("artifacts",
                        new Orbit(.6, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "My artifacts shall destroy you from your soul your flesh!"),
                        new Spawn("Scarab", 3, 0, cooldown: 10000),
                        new Shoot(14, 2, 10, 2, cooldown: 500),
                        new Shoot(14, 1, index: 2, cooldown: 500),
                        new Shoot(14, 2, 90, 2, cooldown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, cooldown: 1000),
                        new Shoot(11 + 1 / 5, 10, 36, 1, 0, cooldown: 5000),
                        new Shoot(12, 1, index: 0, cooldown: 2000),
                        new Shoot(12, 2, 45, 0, cooldown: 2000),
                        new Shoot(99, 1, index: 4, cooldown: 500),
                        new Shoot(99, 1, index: 4, angleOffset: 90, cooldown: 500),
                        new Spawn("Nile Artifact 1", 1, 0),
                        new State("blue shield 3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 3", 3000)
                            ),
                        new State("unset blue shield 3",
                            new TimedTransition("Grenade 7", 3000)
                            ),
                        new State("Grenade 7",
                            new Grenade(radius: 5, damage: 45, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 8", 1500)
                            ),
                        new State("Grenade 8",
                            new Grenade(radius: 4, damage: 100, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 9", 1500)
                            ),
                        new State("Grenade 9",
                            new Grenade(radius: 3, damage: 120, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 7", 1500)
                            ),
                        new HpLessTransition(.2, "artifacts 2")
                        ),
                new State("artifacts 2",
                        new Orbit(.6, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "My artifacts shall destroy you from your soul your flesh!"),
                        new Spawn("Scarab", 3, 0, cooldown: 10000),
                        new Shoot(14, 2, 10, 2, cooldown: 500),
                        new Shoot(14, 1, index: 2, cooldown: 500),
                        new Shoot(14, 2, 90, 2, cooldown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, cooldown: 1000),
                        new Shoot(11 + 1 / 5, 10, 36, 1, 0, cooldown: 5000),
                        new Shoot(12, 1, index: 0, cooldown: 2000),
                        new Shoot(12, 2, 45, 0, cooldown: 2000),
                        new Shoot(99, 1, index: 4, cooldown: 500),
                        new Shoot(99, 1, index: 4, angleOffset: 90, cooldown: 500),
                        new Spawn("Nile Artifact 1", 2, 0),
                        new State("blue shield 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 4", 3000)
                            ),
                        new State("unset blue shield 4",
                            new TimedTransition("Grenade 10", 3000)
                            ),
                        new State("Grenade 10",
                            new Grenade(radius: 5, damage: 45, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 11", 1500)
                            ),
                        new State("Grenade 11",
                            new Grenade(radius: 4, damage: 100, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 12", 1500)
                            ),
                        new State("Grenade 12",
                            new Grenade(radius: 3, damage: 120, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 10", 1500)
                            ),
                        new HpLessTransition(.06, "rage")
                        ),
                new State("rage",
                        new Taunt(true, "This cannot be! You shall not succeed!"),
                        new Flash(0xfFF0000, 1, 9000001),
                        new StayBack(.5, 6),
                        new Shoot(11 + 1 / 5, 10, 36, 1, 0, cooldown: 5000),
                        new Shoot(14, 2, 10, 2, cooldown: 500),
                        new Shoot(14, 1, index: 2, cooldown: 500),
                        new Shoot(14, 2, 90, 2, cooldown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, cooldown: 1000),
                        new Shoot(12, 1, index: 0, cooldown: 2000),
                        new Shoot(12, 2, 45, 0, cooldown: 2000),
                        new Spawn("Scarab", 3, 0, cooldown: 10000),
                        new Spawn("Nile Artifact 1", 1, 0),
                        new State("blue shield 5",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition("unset blue shield 5", 3000)
                            ),
                        new State("unset blue shield 5",
                            new TimedTransition("Grenade 13", 3000)
                            ),
                        new State("Grenade 13",
                            new Grenade(radius: 3, damage: 150, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 14", 1500)
                            ),
                        new State("Grenade 14",
                            new Grenade(radius: 4, damage: 120, range: 10, cooldown: 1500),
                            new TimedTransition("Grenade 13", 1500)
                            )
                    ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Life", 1),
                    new ItemLoot("Ring of the Nile", 0.04f),
                    new ItemLoot("Wine Cellar Incantation", 0.05f)
                ));
            db.Init("Pyramid Artifact 1",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(3, 3, 120, cooldown: 2500));
            db.Init("Pyramid Artifact 2",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(3, 3, 120, cooldown: 2500));
            db.Init("Pyramid Artifact 3",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(3, 3, 120, cooldown: 2500));
            db.Init("Sphinx Artifact 1",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(12, 3, 120, cooldown: 2500));
            db.Init("Sphinx Artifact 2",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(12, 3, 120, cooldown: 2500));
            db.Init("Sphinx Artifact 3",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(12, 3, 120, cooldown: 2500));
            db.Init("Nile Artifact 1",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(12, 3, 120, cooldown: 2500));
            db.Init("Nile Artifact 2",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(12, 3, 120, cooldown: 2500));
            db.Init("Nile Artifact 3",
                new Prioritize(
                        new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, cooldown: 0)
                        ),
                new Shoot(12, 3, 120, cooldown: 2500));
            db.Init("Tomb Defender Statue",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("checkActive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")),
                new State("checkInactive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")),
                new State("ItsGoTime",
                        new Transform("Tomb Defender"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")));
            db.Init("Tomb Support Statue",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("checkActive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")),
                new State("checkInactive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")),
                new State("ItsGoTime",
                        new Transform("Tomb Support"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")));
            db.Init("Tomb Attacker Statue",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("checkActive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")),
                new State("checkInactive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")),
                new State("ItsGoTime",
                        new Transform("Tomb Attacker"), new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"), new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")));
            db.Init("Inactive Sarcophagus",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("checkPriest",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Beam Priest", 1000, "activate"), new EntityNotExistsTransition("Beam Priestess", 14, "checkPriest"), new EntityNotExistsTransition("Beam Priest", 1000, "checkPriestess")),
                new State("checkPriestess",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Beam Priestess", 1000, "activate"), new EntityNotExistsTransition("Beam Priestess", 14, "checkPriest"), new EntityNotExistsTransition("Beam Priest", 1000, "checkPriestess")),
                new State("activate",
                        new Transform("Active Sarcophagus"), new EntityNotExistsTransition("Beam Priestess", 14, "checkPriest"), new EntityNotExistsTransition("Beam Priest", 1000, "checkPriestess")));
            db.Init("Scarab",
                new State("Idle",
                        new Wander(.1f), new NoPlayerWithinTransition(7, "Idle"), new PlayerWithinTransition(7, "Chase")),
                new State("Chase",
                        new Follow(1.5, 7, 0),
                        new Shoot(3, index: 1, cooldown: 500), new NoPlayerWithinTransition(7, "Idle"), new PlayerWithinTransition(7, "Chase")));
            db.Init("Active Sarcophagus",
                new State("stun",
                        new Shoot(50, 8, 10, 0, cooldown: 9999999, cooldownOffset: 500),
                        new Shoot(50, 8, 10, 0, cooldown: 9999999, cooldownOffset: 1000),
                        new Shoot(50, 8, 10, 0, cooldown: 9999999, cooldownOffset: 1500),
                        new TimedTransition("idle", 1500), new HpLessTransition(60, "stun")),
                new State("idle",
                        new ChangeSize(100, 100), new HpLessTransition(60, "stun")),
                new ItemLoot("Magic Potion", 0.002f),
                new ItemLoot("Health Potion", 0.15f),
                new Threshold(0.32f,
                        new ItemLoot("Tincture of Mana", 0.15f),
                        new ItemLoot("Tincture of Dexterity", 0.15f),
                        new ItemLoot("Tincture of Life", 0.15f)
                    ));
            db.Init("Tomb Boss Anchor",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new DropPortalOnDeath("Realm Portal", 100),
                new State("Idle",
                        new EntitiesNotExistsTransition(300, "Death", "Tomb Support", "Tomb Attacker", "Tomb Defender",
                            "Active Sarcophagus", "Tomb Defender Statue", "Tomb Support Statue", "Tomb Attacker Statue")
                    ),
                new State("Death",
                        new Suicide()
                    ));
        }
    }
}
