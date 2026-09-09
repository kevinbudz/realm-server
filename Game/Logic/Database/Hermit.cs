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
    public class Hermit : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Hermit God",
                new TransferDamageOnDeath("Hermit God Drop"),
                new OrderOnDeath(20, "Hermit God Tentacle Spawner", "Die", 1),
                new OrderOnDeath(20, "Hermit God Drop", "Die", 1),
                new State("Spawn Tentacle",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new SetAltTexture(2),
                        new Order(20, "Hermit God Tentacle Spawner", "Tentacle"),
                        new EntityExistsTransition("Hermit God Tentacle", 20, "Sleep")
                        ),
                new State("Sleep",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Order(20, "Hermit God Tentacle Spawner", "Minions"),
                        new TimedTransition("Waiting", 1000)
                        ),
                new State("Waiting",
                        new SetAltTexture(3),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new EntityNotExistsTransition("Hermit God Tentacle", 20, "Wake")
                        ),
                new State("Wake",
                        new SetAltTexture(2),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new TossObject("Hermit Minion", 10, angle: 0),
                        new TossObject("Hermit Minion", 10, angle: 45),
                        new TossObject("Hermit Minion", 10, angle: 90),
                        new TossObject("Hermit Minion", 10, angle: 135),
                        new TossObject("Hermit Minion", 10, angle: 180),
                        new TossObject("Hermit Minion", 10, angle: 225),
                        new TossObject("Hermit Minion", 10, angle: 270),
                        new TossObject("Hermit Minion", 10, angle: 315),
                        new TimedTransition("Spawn Whirlpool", 100)
                        ),
                new State("Spawn Whirlpool",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Order(20, "Hermit God Tentacle Spawner", "Whirlpool"),
                        new EntityExistsTransition("Whirlpool", 20, "Attack1")
                        ),
                new State("Attack1",
                        new SetAltTexture(0),
                        new Prioritize(
                            new Wander(0.3f),
                            new StayCloseToSpawn(0.5, 5)
                            ),
                        new Shoot(20, count: 3, shootAngle: 5, cooldown: 300),
                        new TimedTransition("Attack2", 6000)
                        ),
                new State("Attack2",
                        new Prioritize(
                            new Wander(0.3f),
                            new StayCloseToSpawn(0.5, 5)
                            ),
                        new Order(20, "Whirlpool", "Die"),
                        new Shoot(20, count: 1, defaultAngle: 0, fixedAngle: 0, rotateAngke: 45, index: 1,
                            cooldown: 1000),
                        new Shoot(20, count: 1, defaultAngle: 0, fixedAngle: 180, rotateAngke: 45, index: 1,
                            cooldown: 1000),
                        new TimedTransition("Spawn Tentacle", 6000)
                        ));
            db.Init("Hermit Minion",
                new Prioritize(
                        new Follow(0.6, 4, 1),
                        new Orbit(0.6, 10, 15, "Hermit God", speedVariance: .2, radiusVariance: 1.5),
                        new Wander(0.6f)
                        ),
                new Shoot(6, count: 3, shootAngle: 10, cooldown: 1000),
                new Shoot(6, count: 2, shootAngle: 20, index: 1, cooldown: 2600, predictive: 0.8f),
                new ItemLoot("Health Potion", 0.1f),
                new ItemLoot("Magic Potion", 0.1f));
            db.Init("Whirlpool",
                new State("Attack",
                        new EntityNotExistsTransition("Hermit God", 100, "Die"),
                        new Prioritize(
                            new Orbit(0.3, 6, 10, "Hermit God")
                            ),
                        new Shoot(0, 1, fixedAngle: 0, rotateAngke: 30, cooldown: 400)
                        ),
                new State("Die",
                        new Shoot(0, 8, fixedAngle: 360 / 8),
                        new Suicide()
                        ));
            db.Init("Hermit God Tentacle",
                new Prioritize(
                        new Follow(0.6, 4, 1),
                        new Orbit(0.6, 6, 15, "Hermit God", speedVariance: .2, radiusVariance: .5)
                        ),
                new Shoot(3, count: 8, shootAngle: 360 / 8, cooldown: 500));
            db.Init("Hermit God Tentacle Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Waiting Order"),
                new State("Tentacle",
                        new Reproduce("Hermit God Tentacle", 3, 1, cooldown: 2000),
                        new EntityExistsTransition("Hermit God Tentacle", 1, "Waiting Order")
                        ),
                new State("Whirlpool",
                        new Reproduce("Whirlpool", 3, 1, cooldown: 2000),
                        new EntityExistsTransition("Whirlpool", 1, "Waiting Order")
                        ),
                new State("Minions",
                        new Reproduce("Hermit Minion", 40, 20, cooldown: 1000),
                        new TimedTransition("Waiting Order", 2000)
                        ),
                new State("Die",
                        new Suicide()
                        ));
            db.Init("Hermit God Drop",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Waiting"),
                new State("Die",
                        new Suicide()
                        ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Vitality", 0.1f, 1),
                    new ItemLoot("Potion of Dexterity", 0.1f, 1),
                    new ItemLoot("Helm of the Juggernaut", 0.004f)
                    ));
            db.Init("Hermit portal maker",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new DropPortalOnDeath("Ocean Trench Portal", 1),
                new State("Wait",
                        new EntityNotExistsTransition("Hermit God", 50, "Transform")
                        ),
                new State("Transform",
                        new Suicide()
                        ));
        }
    }
}
