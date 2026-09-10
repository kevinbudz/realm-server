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
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new HpLessTransition(0.989f, "weakning")
                ),
                new State("weakning",
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Taunt(true, "Impudence! I am an Immortal, I needn't waste time on you!"),
                    new Shoot(50, 20, index: 3, cooldown: 6000),
                    new State("blue shield 1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "unset blue shield 1")
                    ),
                    new State("unset blue shield 1"),
                    new HpLessTransition(0.979f, "active")
                ),
                new State("active",
                    new Orbit(0.7f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Shoot(50, 8, 45, 2, 0, 0, cooldown: 1000),
                    new Shoot(50, 3, 120, 1, 0, 0, cooldown: 5000),
                    new Shoot(50, 5, 72, 0, 0, 0, cooldown: 5000),
                    new HpLessTransition(0.7f, "boomerang")
                ),
                new State("boomerang",
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Orbit(0.6f, 3, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Taunt(true, "Nut, disable our foes!"),
                    new Shoot(50, 1, index: 0, cooldown: 3000),
                    new Shoot(50, 8, index: 2, cooldown: 1000),
                    new Shoot(50, 3, 15, 1, cooldown: 3000),
                    new Shoot(50, 2, 90, 1, cooldown: 3000),
                    new State("blue shield 2",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "unset blue shield 2")
                    ),
                    new State("unset blue shield 2"),
                    new HpLessTransition(0.55f, "double shot")

                ),
                new State("double shot",
                    new Taunt(true, "Geb, eradicate these cretins from our tomb!"),
                    new Orbit(0.7f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Shoot(50, 8, index: 2, cooldown: 1000),
                    new Shoot(50, 2, 10, 0, cooldown: 3000),
                    new Shoot(50, 4, 15, 1, cooldown: 3000),
                    new Shoot(50, 2, 90, 1, cooldown: 3000),
                    new HpLessTransition(0.4f, "artifacts")
                ),
                new State("artifacts",
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Taunt(true, "Nut, let them wish they were dead!"),
                    new Orbit(0.6f, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Shoot(50, 8, index: 2, cooldown: 1000),
                    new Shoot(50, 2, 10, 0, cooldown: 3000),
                    new Shoot(50, 4, 15, 1, cooldown: 3000),
                    new Shoot(50, 2, 90, 1, cooldown: 3000),
                    new Spawn("Pyramid Artifact 1", 1, 0),
                    new Spawn("Pyramid Artifact 2", 1, 0),
                    new State("blue shield 3",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "unset blue shield 3")
                    ),
                    new State("unset blue shield 3"),
                    new HpLessTransition(0.25f, "artifacts 2")
                ),
                new State("artifacts 2",
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Taunt(true, "My artifacts shall prove my wall of defense is impenetrable!"),
                    new Orbit(0.6f, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Shoot(50, 8, index: 2, cooldown: 1000),
                    new Shoot(50, 3, 10, 0, cooldown: 3000),
                    new Shoot(50, 5, 15, 1, cooldown: 3000),
                    new Shoot(50, 2, 80, 1, cooldown: 3000),
                    new Shoot(50, 2, 90, 1, cooldown: 3000),
                    new Spawn("Pyramid Artifact 1", 2, 0),
                    new State("blue shield 4",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "unset blue shield 4")
                    ),
                    new State("unset blue shield 4"),
                    new HpLessTransition(0.06f, "rage")
                ),
                new State("rage",
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Taunt(true, "The end of your path is here!"),
                    new Follow(0.6f, range: 1, duration: 5000, cooldown: 0),
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
                        new TimedTransition(3000, "unset blue shield 5")
                    ),
                    new State("unset blue shield 5")
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Life", 1),
                    new ItemLoot("Ring of the Pyramid", 0.04f),
                    new ItemLoot("Tome of Holy Protection", 0.01f),
                    new ItemLoot("Wine Cellar Incantation", 0.05f)
                )
            );
            db.Init("Tomb Support",
                new State("idle",
                    new Taunt(true, "ENOUGH OF YOUR VANDALISM!"),
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new HpLessTransition(0.9875f, "weakning")
                ),
                new State("weakning",
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Taunt("Impudence! I am an immortal, I needn't take your seriously."),
                    new Shoot(50, 20, index: 7, cooldown: 6000, cooldownOffset: 2000),
                    new State("blue shield 1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "unset blue shield 1")
                    ),
                    new State("unset blue shield 1"),
                    new HpLessTransition(0.97875f, "active")
                ),
                new State("active",
                    new Orbit(0.7f, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Shoot(20, 1, index: 5, cooldown: 1000),
                    new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                    new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                    new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                    new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                    new State("blue shield 2",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "unset blue shield 2")
                    ),
                    new State("unset blue shield 2"),
                    new HpLessTransition(0.9f, "boomerang")
                ),
                new State("boomerang",
                    new Orbit(0.6f, 6, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Taunt(true, "Bes, protect me at once!"),
                    new Shoot(20, 1, index: 5, cooldown: 1000),
                    new Shoot(20, 1, index: 6, cooldown: 3000),
                    new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                    new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                    new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                    new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                    new HpLessTransition(0.7f, "paralyze")
                ),
                new State("paralyze",
                    new Orbit(0.6f, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Taunt(true, "Geb, eradicate these cretins from our tomb!"),
                    new Shoot(20, 1, index: 5, cooldown: 1000),
                    new Shoot(20, 1, index: 6, cooldown: 3000),
                    new Shoot(999, 2, 10, 8, 0, 180, cooldown: 1000),
                    new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                    new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                    new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                    new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                    new HpLessTransition(0.5f, "artifacts")
                ),
                new State("artifacts",
                    new Orbit(0.6f, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
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
                        new TimedTransition(3000, "unset blue shield 3")
                    ),
                    new State("unset blue shield 3"),
                    new HpLessTransition(0.3f, "double shoot")
                ),
                new State("double shoot",
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Shoot(20, 2, 15, 5, cooldown: 1000),
                    new Shoot(20, 2, 15, 6, cooldown: 3000),
                    new Shoot(12, 3, 120, 1, 0, 0, cooldown: 2500, cooldownOffset: 1000),
                    new Shoot(12, 4, 90, 2, 0, 0, cooldown: 2500, cooldownOffset: 1500),
                    new Shoot(12, 5, 72, 3, 0, 0, cooldown: 2500, cooldownOffset: 2000),
                    new Shoot(12, 6, 60, 4, 0, 0, cooldown: 2500, cooldownOffset: 2500),
                    new Spawn("Sphinx Artifact 1", 2, 0),
                    new State("blue shield 4",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "unset blue shield 4")
                    ),
                    new State("unset blue shield 4"),
                    new HpLessTransition(0.06f, "rage")
                ),
                new State("rage",
                    new Taunt(true, "This cannot be! You shall not succeed!"),
                    new Follow(0.6f, range: 1, duration: 5000, cooldown: 0),
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
                        new TimedTransition(3000, "unset blue shield 5")
                    ),
                    new State("unset blue shield 5")
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Life", 1),
                    new ItemLoot("Ring of the Sphinx", 0.04f),
                    new ItemLoot("Wine Cellar Incantation", 0.05f)
                )
            );
            db.Init("Tomb Attacker",
                new State("idle",
                    new Taunt(true, "ENOUGH OF YOUR VANDALISM!"),
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new HpLessTransition(0.988f, "weakning")
                ),
                new State("weakning",
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Shoot(50, 20, index: 3, cooldown: 6000, cooldownOffset: 2000),
                    new State("blue shield 1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "unset blue shield 1")
                    ),
                    new State("unset blue shield 1"),
                    new HpLessTransition(0.9788f, "active")
                ),
                new State("active",
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
                    new Shoot(14, 2, 10, 2, cooldown: 500),
                    new Shoot(12, 1, index: 0, cooldown: 2000),
                    new State("Grenade 1",
                        new Grenade(radius: 3, damage: 160, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 2")
                    ),
                    new State("Grenade 2",
                        new Grenade(radius: 4, damage: 120, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 1")
                    ),
                    new HpLessTransition(0.72f, "lets dance")
                ),
                new State("lets dance",
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
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
                        new TimedTransition(3000, "unset blue shield 2")
                    ),
                    new State("unset blue shield 2",
                        new TimedTransition(3000, "Grenade 3")
                    ),
                    new State("Grenade 3",
                        new Grenade(radius: 3, damage: 160, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 4")
                    ),
                    new State("Grenade 4",
                        new Grenade(radius: 4, damage: 120, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 3")
                    ),
                    new HpLessTransition(0.675f, "more muthafucka")
                ),
                new State("more muthafucka",
                    new Orbit(0.6f, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
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
                        new TimedTransition(1500, "Grenade 6")
                    ),
                    new State("Grenade 6",
                        new Grenade(radius: 4, damage: 120, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 5")
                    ),
                    new HpLessTransition(0.4f, "artifacts")
                ),
                new State("artifacts",
                    new Orbit(0.6f, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
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
                        new TimedTransition(3000, "unset blue shield 3")
                    ),
                    new State("unset blue shield 3",
                        new TimedTransition(3000, "Grenade 7")
                    ),
                    new State("Grenade 7",
                        new Grenade(radius: 5, damage: 45, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 8")
                    ),
                    new State("Grenade 8",
                        new Grenade(radius: 4, damage: 100, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 9")
                    ),
                    new State("Grenade 9",
                        new Grenade(radius: 3, damage: 120, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 7")
                    ),
                    new HpLessTransition(0.2f, "artifacts 2")
                ),
                new State("artifacts 2",
                    new Orbit(0.6f, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5f),
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
                        new TimedTransition(3000, "unset blue shield 4")
                    ),
                    new State("unset blue shield 4",
                        new TimedTransition(3000, "Grenade 10")
                    ),
                    new State("Grenade 10",
                        new Grenade(radius: 5, damage: 45, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 11")
                    ),
                    new State("Grenade 11",
                        new Grenade(radius: 4, damage: 100, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 12")
                    ),
                    new State("Grenade 12",
                        new Grenade(radius: 3, damage: 120, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 10")
                    ),
                    new HpLessTransition(0.06f, "rage")
                ),
                new State("rage",
                    new Taunt(true, "This cannot be! You shall not succeed!"),
                    new Flash(0xfFF0000, 1, 9000001),
                    new StayBack(0.5f, 6),
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
                        new TimedTransition(3000, "unset blue shield 5")
                    ),
                    new State("unset blue shield 5",
                        new TimedTransition(3000, "Grenade 13")
                    ),
                    new State("Grenade 13",
                        new Grenade(radius: 3, damage: 150, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 14")
                    ),
                    new State("Grenade 14",
                        new Grenade(radius: 4, damage: 120, range: 10, cooldown: 1500),
                        new TimedTransition(1500, "Grenade 13")
                    )
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Life", 1),
                    new ItemLoot("Ring of the Nile", 0.04f),
                    new ItemLoot("Wine Cellar Incantation", 0.05f)
                )
            );
            //Minions
            db.Init("Pyramid Artifact 1",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(3, 3, 120, cooldown: 2500)
            );
            db.Init("Pyramid Artifact 2",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(3, 3, 120, cooldown: 2500)
            );
            db.Init("Pyramid Artifact 3",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(3, 3, 120, cooldown: 2500)
            );
            db.Init("Sphinx Artifact 1",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(12, 3, 120, cooldown: 2500)
            );
            db.Init("Sphinx Artifact 2",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(12, 3, 120, cooldown: 2500)
            );
            db.Init("Sphinx Artifact 3",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(12, 3, 120, cooldown: 2500)
            );
            db.Init("Nile Artifact 1",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(12, 3, 120, cooldown: 2500)
            );
            db.Init("Nile Artifact 2",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(12, 3, 120, cooldown: 2500)
            );
            db.Init("Nile Artifact 3",
                new Prioritize(
                    new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5f),
                    new Follow(0.85f, range: 1, duration: 5000, cooldown: 0)
                ),
                new Shoot(12, 3, 120, cooldown: 2500)
            );
            db.Init("Tomb Defender Statue",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"),
                new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive"),
                new State("checkActive",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime")
                ),
                new State("checkInactive",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime")
                ),
                new State("ItsGoTime",
                    new Transform("Tomb Defender")
                )
            );
            db.Init("Tomb Support Statue",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"),
                new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive"),
                new State("checkActive",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime")
                ),
                new State("checkInactive",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime")
                ),
                new State("ItsGoTime",
                    new Transform("Tomb Support")
                )
            );
            db.Init("Tomb Attacker Statue",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"),
                new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive"),
                new State("checkActive",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime")
                ),
                new State("checkInactive",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime")
                ),
                new State("ItsGoTime",
                    new Transform("Tomb Attacker")
                )
            );
            db.Init("Inactive Sarcophagus",
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new EntityNotExistsTransition("Beam Priestess", 14, "checkPriest"),
                new EntityNotExistsTransition("Beam Priest", 1000, "checkPriestess"),
                new State("checkPriest",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Beam Priest", 1000, "activate")
                ),
                new State("checkPriestess",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Beam Priestess", 1000, "activate")
                ),
                new State("activate",
                    new Transform("Active Sarcophagus")
                )
            );
            db.Init("Scarab",
                new NoPlayerWithinTransition(7, "Idle"),
                new PlayerWithinTransition(7, "Chase"),
                new State("Idle",
                    new Wander(0.1f)
                ),
                new State("Chase",
                    new Follow(1.5f, 7, 0),
                    new Shoot(3, index: 1, cooldown: 500)
                )
            );
            db.Init("Active Sarcophagus",
                new HpLessTransition(60, "stun"),
                new State("stun",
                    new Shoot(50, 8, 10, 0, cooldown: 9999999, cooldownOffset: 500),
                    new Shoot(50, 8, 10, 0, cooldown: 9999999, cooldownOffset: 1000),
                    new Shoot(50, 8, 10, 0, cooldown: 9999999, cooldownOffset: 1500),
                    new TimedTransition(1500, "idle")
                ),
                new State("idle",
                    new ChangeSize(100, 100)
                ),
                new ItemLoot("Magic Potion", 0.002f),
                new ItemLoot("Health Potion", 0.15f),
                new Threshold(0.32f,
                    new ItemLoot("Tincture of Mana", 0.15f),
                    new ItemLoot("Tincture of Dexterity", 0.15f),
                    new ItemLoot("Tincture of Life", 0.15f)
                )
            );
            db.Init("Tomb Boss Anchor",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new DropPortalOnDeath("Realm Portal", 100),
                new State("Idle",
                    new EntitiesNotExistsTransition(300, "Death", "Tomb Support", "Tomb Attacker", "Tomb Defender",
                        "Active Sarcophagus", "Tomb Defender Statue", "Tomb Support Statue", "Tomb Attacker Statue")
                ),
                new State("Death",
                    new Suicide()
                )
            );
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
