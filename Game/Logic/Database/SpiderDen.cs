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
    public class SpiderDen : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Arachna the Spider Queen",
                new State("idle",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new PlayerWithinTransition(12, "WEB!")
                         ),
                new State("WEB!",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new TossObject("Arachna Web Spoke 7", 6, 0,cooldown:  100000),
                         new TossObject("Arachna Web Spoke 8", 6, 120,cooldown:  100000),
                         new TossObject("Arachna Web Spoke 9", 6, 240,cooldown:  100000),
                         new TossObject("Arachna Web Spoke 1", 10, 0,cooldown:  100000),
                         new TossObject("Arachna Web Spoke 2", 10, 60,cooldown:  100000),
                         new TossObject("Arachna Web Spoke 3", 10, 120,cooldown:  100000),
                         new TossObject("Arachna Web Spoke 4", 10, 180,cooldown:  100000),
                         new TossObject("Arachna Web Spoke 5", 10, 240,cooldown:  100000),
                         new TossObject("Arachna Web Spoke 6", 10, 300,cooldown:  100000),
                         new TimedTransition("attack", 2000)
                         ),
                new State("attack",
                         new Wander(1.0f),
                         new Shoot(3000, count: 12, index: 0, fixedAngle: 22.5f),
                         new Shoot(10, 1, 0, defaultAngle: 0, angleOffset: 0, index: 0, predictive: 1,
                         cooldown: 1000, cooldownOffset: 0),
                         new Shoot(10, 1, 0, defaultAngle: 0, angleOffset: 0, index: 1, predictive: 1,
                         cooldown: 2000, cooldownOffset: 0)
                         ),
                new ItemLoot("Golden Dagger", 0.2f),
                new ItemLoot("Spider's Eye Ring", 0.2f),
                new ItemLoot("Poison Fang Dagger", 0.2f),
                new Threshold(0.32f,
                    new ItemLoot("Healing Ichor", 1)
                     ));
            db.Init("Arachna Web Spoke 1",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 120, fixedAngle: 120, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 180, fixedAngle: 180, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 240, fixedAngle: 240, cooldown: 5)
                    ));
            db.Init("Arachna Web Spoke 2",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 240, fixedAngle: 240, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 180, fixedAngle: 180, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 300, fixedAngle: 300, cooldown: 5)
                    ));
            db.Init("Arachna Web Spoke 3",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 300, fixedAngle: 300, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 240, fixedAngle: 240, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 0, fixedAngle: 0, cooldown: 5)
                    ));
            db.Init("Arachna Web Spoke 4",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 0, fixedAngle: 0, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 60, fixedAngle: 60, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 300, fixedAngle: 300, cooldown: 5)
                    ));
            db.Init("Arachna Web Spoke 5",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 60, fixedAngle: 60, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 0, fixedAngle: 0, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 120, fixedAngle: 120, cooldown: 5)
                    ));
            db.Init("Arachna Web Spoke 6",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 120, fixedAngle: 120, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 60, fixedAngle: 60, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 180, fixedAngle: 180, cooldown: 5)
                    ));
            db.Init("Arachna Web Spoke 7",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 180, fixedAngle: 180, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 120, fixedAngle: 120, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 240, fixedAngle: 240, cooldown: 5)
                    ));
            db.Init("Arachna Web Spoke 8",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 360, fixedAngle: 360, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 240, fixedAngle: 240, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 300, fixedAngle: 300, cooldown: 5)
                    ));
            db.Init("Arachna Web Spoke 9",
                new State(":D",
                         new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                         new Shoot(0, index: 0, count: 1, shootAngle: 0, fixedAngle: 0, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 60, fixedAngle: 60, cooldown: 5),
                         new Shoot(0, index: 0, count: 1, shootAngle: 120, fixedAngle: 120, cooldown: 5)
                    ));
            db.Init("Black Den Spider",
                new State("idle",
                    new Wander(0.8f),
                    new Charge(0.9, 20f, 2000),
                    new Shoot(10, 1, 0, defaultAngle: 0, angleOffset: 0, index: 0, predictive: 1,
                    cooldown: 500, cooldownOffset: 0)
                         ),
                new ItemLoot("Healing Ichor", 0.2f));
            db.Init("Black Spotted Den Spider",
                new State("idle",
                    new Wander(0.8f),
                    new Charge(0.9, 40f, 2000),
                    new Shoot(10, 1, 0, defaultAngle: 0, angleOffset: 0, index: 0, predictive: 1,
                    cooldown: 500, cooldownOffset: 0)
                         ),
                new ItemLoot("Healing Ichor", 0.2f));
            db.Init("Brown Den Spider",
                new State("idle",
                    new Wander(0.8f),
                    new Follow(0.8, 0.3, 0),
                    new Shoot(10, 3, 20, angleOffset: 0 / 3, index: 0, cooldown: 500)
                    ),
                new ItemLoot("Healing Ichor", 0.2f));
            db.Init("Green Den Spider Hatchling",
                new State("idle",
                    new Wander(0),
                    new Follow(0.8, 0.8, 0),
                    new Shoot(10, 1, 0, defaultAngle: 0, angleOffset: 0, index: 0, predictive: 1,
                    cooldown: 1000, cooldownOffset: 0)
                    ));
            db.Init("Spider Egg Sac",
                new TransformOnDeath("Green Den Spider Hatchling", 2, 7),
                new State("idle",
                    new PlayerWithinTransition(0.5, "suicide")
                    ),
                new State("suicide",
                    new Suicide()
                    ));
            db.Init("Red Spotted Den Spider",
                new State("idle",
                    new Wander(0),
                    new Follow(1.0, 0.8, 0),
                    new Shoot(10, 1, 0, defaultAngle: 0, angleOffset: 0, index: 0, predictive: 1,
                    cooldown: 500, cooldownOffset: 0)
                    ),
                new ItemLoot("Healing Ichor", 0.2f));
            db.Init("Arachna Summoner",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("idle",
                     new EntitiesNotExistsTransition(300, "Death", "Arachna the Spider Queen")
                    ),
                new State("Death",
                    new Suicide()
                    ));
        }
    }
}
