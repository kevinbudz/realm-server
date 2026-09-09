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
    public class SnakePit : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Stheno the Snake Queen",
                new State("Idle",
                        new PlayerWithinTransition(20, "Silver Blasts")
                    ),
                new State("Silver Blasts",
                        new State("Silver Blasts 1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 45, index: 0),
                            new Shoot(10, count: 1, angleOffset: 135, index: 0),
                            new Shoot(10, count: 1, angleOffset: 225, index: 0),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 315, index: 0),
                            new TimedTransition("Silver Blasts 2", 1000)
                        ),
                        new State("Silver Blasts 2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 45, index: 0),
                            new Shoot(10, count: 1, angleOffset: 135, index: 0),
                            new Shoot(10, count: 1, angleOffset: 225, index: 0),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 315, index: 0),
                            new TimedTransition("Silver Blasts 3", 1000)
                        ),
                        new State("Silver Blasts 3",
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 45, index: 0),
                            new Shoot(10, count: 1, angleOffset: 135, index: 0),
                            new Shoot(10, count: 1, angleOffset: 225, index: 0),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 315, index: 0),
                            new TimedTransition("Silver Blasts 4", 1000)
                        ),
                        new State("Silver Blasts 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 45, index: 0),
                            new Shoot(10, count: 1, angleOffset: 135, index: 0),
                            new Shoot(10, count: 1, angleOffset: 225, index: 0),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 315, index: 0),
                            new TimedTransition("Silver Blasts 5", 1000)
                        ),
                        new State("Silver Blasts 5",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 45, index: 0),
                            new Shoot(10, count: 1, angleOffset: 135, index: 0),
                            new Shoot(10, count: 1, angleOffset: 225, index: 0),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 315, index: 0),
                            new TimedTransition("Silver Blasts 6", 1000)
                        ),
                        new State("Silver Blasts 6",
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 45, index: 0),
                            new Shoot(10, count: 1, angleOffset: 135, index: 0),
                            new Shoot(10, count: 1, angleOffset: 225, index: 0),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 315, index: 0),
                            new TimedTransition("Silver Blasts 7", 1000)
                        ),
                        new State("Silver Blasts 7",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 45, index: 0),
                            new Shoot(10, count: 1, angleOffset: 135, index: 0),
                            new Shoot(10, count: 1, angleOffset: 225, index: 0),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 315, index: 0),
                            new TimedTransition("Silver Blasts 8", 1000)
                        ),
                        new State("Silver Blasts 8",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 45, index: 0),
                            new Shoot(10, count: 1, angleOffset: 135, index: 0),
                            new Shoot(10, count: 1, angleOffset: 225, index: 0),
                            new Shoot(10, count: 2, shootAngle: 10, angleOffset: 315, index: 0),
                            new TimedTransition("Spawn Stheno Swarm", 1000)
                        )
                    ),
                new State("Spawn Stheno Swarm",
                        new Prioritize(
                            new StayCloseToSpawn(0.4, 2),
                            new Wander(0.4f)
                        ),
                        new Reproduce("Stheno Swarm", 2.5, 8, cooldown: 750),
                        new State("Silver Blast 1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 2", 1000)
                        ),
                        new State("Silver Blast 2",
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 3", 1000)
                        ),
                        new State("Silver Blast 3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 4", 1000)
                        ),
                        new State("Silver Blast 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 5", 1000)
                        ),
                        new State("Silver Blast 5",
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 6", 1000)
                        ),
                        new State("Silver Blast 6",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 7", 1000)
                        ),
                        new State("Silver Blast 7",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 8", 1000)
                        ),
                        new State("Silver Blast 8",
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 9", 1000)
                        ),
                        new State("Silver Blast 9",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 10", 1000)
                        ),
                        new State("Silver Blast 10",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 11", 1000)
                        ),
                        new State("Silver Blast 11",
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 12", 1000)
                        ),
                        new State("Silver Blast 12",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 13", 1000)
                        ),
                        new State("Silver Blast 13",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 14", 1000)
                        ),
                        new State("Silver Blast 14",
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 15", 1000)
                        ),
                        new State("Silver Blast 15",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Silver Blast 16", 1000)
                        ),
                        new State("Silver Blast 16",
                            new Shoot(10, count: 1, index: 0),
                            new Shoot(10, count: 1, angleOffset: 270, index: 0),
                            new Shoot(10, count: 1, angleOffset: 90, index: 0),
                            new TimedTransition("Leave me", 1000)
                        ),
                        new State("Leave me",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Order(100, "Stheno Swarm", "Despawn"),
                            new TimedTransition("Blind Ring Attack + ThrowAttack", 1000)
                        )
                    ),
                new State("Blind Ring Attack + ThrowAttack",
                        new ReturnToSpawn(speed: 0.3),
                        new State("Blind Ring Attack + ThrowAttack 1",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 2", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 2",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 3", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 3",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 4", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 5", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 5",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 6", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 6",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 7", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 7",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 8", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 8",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 9", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 9",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 10", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 10",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 11", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 11",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable, true),
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Blind Ring Attack + ThrowAttack 12", 500)
                        ),
                        new State("Blind Ring Attack + ThrowAttack 12",
                            new Shoot(10, count: 6, index: 1),
                            new Grenade(radius: 2.5f, damage: 100, range: 10),
                            new TimedTransition("Silver Blasts", 500)
                        )
                    ),
                new Threshold(0.1f,
                    new ItemLoot("Potion of Speed", 1),
                    new ItemLoot("Wand of the Bulwark", 0.005f),
                    new ItemLoot("Snake Skin Armor", 0.1f),
                    new ItemLoot("Snake Skin Shield", 0.1f),
                    new ItemLoot("Snake Eye Ring", 0.1f),
                    new ItemLoot("Wine Cellar Incantation", 0.05f),
                    new TierLoot(9, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(10, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.3f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(10, TierLoot.LootType.Armor, 0.1f)
                ));
            db.Init("Stheno Swarm",
                new State("Protect",
                        new Prioritize(
                            new Protect(0.3, "Stheno the Snake Queen"),
                            new Wander(0.3f)
                        ),
                        new Shoot(10, cooldown: 750, cooldownVariance: 250)
                    ),
                new State("Despawn",
                        new Suicide()
                    ));
            db.Init("Stheno Pet",
                new State("Protect",
                        new Shoot(25, cooldown: 1000),
                        new State("Protect",
                            new EntityNotExistsTransition("Stheno the Snake Queen", 100, "Wander"),
                            new Orbit(7.5, 10, acquireRange: 50, target: "Stheno the Snake Queen")
                        ),
                        new State("Wander",
                            new Prioritize(
                                new Wander(1)
                            )
                        )
                    ));
            db.Init("Pit Snake",
                new Prioritize(
                        new StayCloseToSpawn(1),
                        new Wander(1)
                    ),
                new Shoot(20, cooldown: 1000));
            db.Init("Pit Viper",
                new Prioritize(
                        new StayCloseToSpawn(1),
                        new Wander(1)
                    ),
                new Shoot(20, cooldown: 1000));
            db.Init("Yellow Python",
                new Prioritize(
                        new Follow(1, 10, 1),
                        new StayCloseToSpawn(1),
                        new Wander(1)
                    ),
                new Shoot(20, cooldown: 1000),
                new ItemLoot("Snake Oil", 0.1f),
                new ItemLoot("Ring of Speed", 0.1f),
                new ItemLoot("Ring of Vitality", 0.1f));
            db.Init("Brown Python",
                new Prioritize(
                        new StayCloseToSpawn(1),
                        new Wander(1)
                    ),
                new Shoot(20, cooldown: 1000),
                new ItemLoot("Snake Oil", 0.1f),
                new ItemLoot("Leather Armor", 0.1f),
                new ItemLoot("Ring of Wisdom", 0.1f));
            db.Init("Fire Python",
                new Prioritize(
                        new Follow(1, 10, 1, cooldown: 2000),
                        new Wander(1)
                    ),
                new Shoot(15, count: 3, shootAngle: 5, cooldown: 1000),
                new ItemLoot("Snake Oil", 0.1f),
                new ItemLoot("Fire Bow", 0.1f),
                new ItemLoot("Fire Nova Spell", 0.1f));
            db.Init("Greater Pit Snake",
                new Prioritize(
                        new Follow(1, 10, 5),
                        new Wander(1)
                    ),
                new Shoot(15, count: 3, shootAngle: 5, cooldown: 1000),
                new ItemLoot("Snake Oil", 0.1f),
                new ItemLoot("Glass Sword", 0.1f),
                new ItemLoot("Avenger Staff", 0.1f),
                new ItemLoot("Wand of Dark Magic", 0.1f));
            db.Init("Greater Pit Viper",
                new Prioritize(
                        new Follow(1, 10, 5),
                        new Wander(1)
                    ),
                new Shoot(15, cooldown: 300),
                new ItemLoot("Snake Oil", 0.1f),
                new Threshold(0.1f,
                    new ItemLoot("Ring of Greater Attack", 0.1f),
                    new ItemLoot("Ring of Greater Health", 0.1f)
                ));
            db.Init("Snakepit Guard",
                new ChangeSize(100, 100),
                new Shoot(25, count: 3, shootAngle: 25, index: 0, cooldown: 1000, cooldownVariance: 200),
                new Shoot(10, count: 6, index: 1, cooldown: 1000),
                new State("Phase 1",
                        new Prioritize(
                            new StayCloseToSpawn(0.2, 4),
                            new Wander(0.2f)
                        ),
                        new HpLessTransition(0.6, "Phase 2")
                    ),
                new State("Phase 2",
                        new Prioritize(
                            new Follow(0.2, acquireRange: 10, range: 3),
                            new Wander(0.2f)
                        ),
                        new Shoot(15, count: 3, index: 2, cooldown: 2000)
                    ),
                new Threshold(0.32f,
                    new ItemLoot("Potion of Speed", 1)
                ),
                new Threshold(0.1f,
                    new ItemLoot("Wand of the Bulwark", 0.005f),
                    new ItemLoot("Snake Skin Armor", 0.1f),
                    new ItemLoot("Snake Skin Shield", 0.1f),
                    new ItemLoot("Snake Eye Ring", 0.1f),
                    new ItemLoot("Wine Cellar Incantation", 0.05f),
                    new TierLoot(9, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(10, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.3f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(10, TierLoot.LootType.Armor, 0.1f)
                ));
            db.Init("Snakepit Dart Thrower",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Idle"),
                new State("Protect the Guard",
                        new EntityNotExistsTransition("Snakepit Guard", 40, "Idle")
                    ));
            db.Init("Snakepit Button",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Idle",
                        new PlayerWithinTransition(0.5, "Order")
                    ),
                new State("Order",
                        new Order(15, "Snakepit Guard Spawner", "Spawn the Guard"),
                        new SetAltTexture(1),
                        new TimedTransition("I am out", 0)
                    ),
                new State("I am out"));
            db.Init("Snakepit Guard Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Idle"),
                new State("Spawn the Guard",
                        new Order(15, "Snakepit Dart Thrower", "Protect the Guard"),
                        new Spawn("Snakepit Guard", maxChildren: 1, initialSpawn: 1),
                        new TimedTransition("Idle", 0)
                    ));
            db.Init("Snake Grate",
                new State("Idle",
                        new EntityNotExistsTransition("Pit Snake", 5, "Spawn Pit Snake"),
                        new EntityNotExistsTransition("Pit Viper", 5, "Spawn Pit Viper")
                    ),
                new State("Spawn Pit Snake",
                        new Spawn("Pit Snake", 1, 1),
                        new TimedTransition("Idle", 2000)
                    ),
                new State("Spawn Pit Viper",
                        new Spawn("Pit Viper", 1, 1),
                        new TimedTransition("Idle", 2000)
                    ));
        }
    }
}
