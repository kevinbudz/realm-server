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
    public class Phoenix : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Phoenix Lord",
                new Shoot(10, count: 4, shootAngle: 7, predictive: 0.5f, cooldown: 600),
                new Prioritize(
                        new StayCloseToSpawn(0.3, 2),
                        new Wander(0.4f)
                        ),
                new SpawnGroup("Pyre", maxChildren: 16, cooldown: 5000),
                new Taunt(0.7, 10000,
                        "Alas, {PLAYER}, you will taste death but once!",
                        "I have met many like you, {PLAYER}, in my thrice thousand years!",
                        "Purge yourself, {PLAYER}, in the heat of my flames!",
                        "The ashes of past heroes cover my plains!",
                        "Some die and are ashes, but I am ever reborn!"
                        ),
                new TransformOnDeath("Phoenix Egg"));
            db.Init("Birdman Chief",
                new Prioritize(
                        new Protect(0.5, "Phoenix Lord", acquireRange: 15, protectionRange: 10, reprotectRange: 3),
                        new Follow(1, range: 9),
                        new Wander(0.5f)
                        ),
                new Shoot(10),
                new ItemLoot("Magic Potion", 0.05f));
            db.Init("Birdman",
                new Prioritize(
                        new Protect(0.5, "Phoenix Lord", acquireRange: 15, protectionRange: 11, reprotectRange: 3),
                        new Follow(1, range: 7),
                        new Wander(0.5f)
                        ),
                new Shoot(10, predictive: 0.5f),
                new ItemLoot("Health Potion", 0.05f));
            db.Init("Phoenix Egg",
                new TransformOnDeath("Phoenix Reborn"),
                new State("shielded",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition("unshielded", 2000)
                        ),
                new State("unshielded",
                        new Flash(0xff0000, 1, 5000),
                        new State("grow",
                            new ChangeSize(20, 150),
                            new TimedTransition("shrink", 800)
                            ),
                        new State("shrink",
                            new ChangeSize(-20, 130),
                            new TimedTransition("grow", 800)
                            )
                        ));
            db.Init("Phoenix Reborn",
                new Prioritize(
                        new StayCloseToSpawn(0.9, 5),
                        new Wander(0.9f)
                        ),
                new SpawnGroup("Pyre", maxChildren: 4, cooldown: 1000),
                new State("born_anew",
                        new Shoot(10, index: 0, count: 12, shootAngle: 30, fixedAngle: 10, cooldown: 100000,
                            cooldownOffset: 500),
                        new Shoot(10, index: 0, count: 12, shootAngle: 30, fixedAngle: 25, cooldown: 100000,
                            cooldownOffset: 1000),
                        new TimedTransition("xxx", 1800)
                        ),
                new State("xxx",
                        new Shoot(10, index: 1, count: 4, shootAngle: 7, predictive: 0.5f, cooldown: 600),
                        new TimedTransition("yyy", 2800)
                        ),
                new State("yyy",
                        new Shoot(10, index: 0, count: 12, shootAngle: 30, fixedAngle: 10, cooldown: 100000,
                            cooldownOffset: 500),
                        new Shoot(10, index: 0, count: 12, shootAngle: 30, fixedAngle: 25, cooldown: 100000,
                            cooldownOffset: 1000),
                        new Shoot(10, index: 1, predictive: 0.5f, cooldown: 350),
                        new TimedTransition("xxx", 4500)
                        ),
                new Threshold(0.1f,
                new ItemLoot("Large Stony Cloth", 0.03f),
                new ItemLoot("Small Stony Cloth", 0.03f),
                new ItemLoot("Large Tan Diamond Cloth", 0.03f),
                new ItemLoot("Small Tan Diamond Cloth", 0.03f),
                new ItemLoot("Large Smiley Cloth", 0.03f),
                new ItemLoot("Small Smiley Cloth", 0.03f)
                    ));
        }
    }
}
