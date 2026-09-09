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
    public class CaveTTMobs : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Treasure Flame Trap 1.7 Sec",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Wait",
                    new SetAltTexture(0, 0),
                    new TimedTransition(1000, "Start")
                ),
                new State("Start",
                    new Shoot(100, 1, index: 0, cooldown: 200),
                    new SetAltTexture(1, 1),
                    new TimedTransition(140, "Start 2")
                ),
                new State("Start 2",
                    new Shoot(100, 1, index: 0, cooldown: 200),
                    new SetAltTexture(2, 2),
                    new TimedTransition(140, "Start 3")
                ),
                new State("Start 3",
                    new Shoot(100, 1, index: 0, cooldown: 200),
                    new SetAltTexture(3, 3),
                    new TimedTransition(140, "Start 4")
                ),
                new State("Start 4",
                    new Shoot(100, 1, index: 0, cooldown: 200),
                    new SetAltTexture(4, 4),
                    new TimedTransition(140, "Start 5")
                ),
                new State("Start 5",
                    new Shoot(100, 1, index: 0, cooldown: 200),
                    new SetAltTexture(5, 5),
                    new TimedTransition(140, "Wait")
                )
            );
            db.Init("Log Trap Clockwise",
                new Shoot(20, 1, index: 0, cooldown: 200),
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new SetAltTexture(0, 3, 100, loop: true),
                new State("Check Ground",
                    new GroundTransition("Track N End", "Move Down"),
                    new GroundTransition("Track S End", "Move Up"),
                    new GroundTransition("Track W End", "Move Left"),
                    new GroundTransition("Track E End", "Move Right")
                ),
                new State("Move Down",
                    new GroundTransition("Track N End", "Move Down"),
                    new GroundTransition("Track S End", "Move Up"),
                    new GroundTransition("Track W End", "Move Left"),
                    new GroundTransition("Track E End", "Move Right"),
                    new MoveLine(0.5f, 90)
                ),
                new State("Move Up",
                    new GroundTransition("Track N End", "Move Down"),
                    new GroundTransition("Track S End", "Move Up"),
                    new GroundTransition("Track W End", "Move Left"),
                    new GroundTransition("Track E End", "Move Right"),
                    new MoveLine(0.5f, -90)
                ),
                new State("Move Left",
                    new GroundTransition("Track N End", "Move Down"),
                    new GroundTransition("Track S End", "Move Up"),
                    new GroundTransition("Track W End", "Move Left"),
                    new GroundTransition("Track E End", "Move Right"),
                    new MoveLine(0.5f, 0)
                ),
                new State("Move Right",
                    new GroundTransition("Track N End", "Move Down"),
                    new GroundTransition("Track S End", "Move Up"),
                    new GroundTransition("Track W End", "Move Left"),
                    new GroundTransition("Track E End", "Move Right"),
                    new MoveLine(0.5f, 180)
                )
            );
            db.Init("Boulder",
                new SetAltTexture(0, 3, 100, loop: true),
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("Move",
                    new Shoot(20, 1, index: 0, cooldown: 200),
                    new MoveLine(3, 90),
                    new GroundTransition("Tunnel Ground M", "Suicide")
                ),
                new State("Suicide",
                    new Suicide()
                )
            );
            db.Init("Boulder Spawner",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("CHeck Player",
                    new PlayerWithinTransition(100, "Start")
                ),
                new State("Start",
                    new Reproduce("Boulder", 20, 1, cooldown: 200)
                )
            );
            db.Init("Treasure Pot",
                new TierLoot(1, TierLoot.LootType.Ability, 0.12f),
                new TierLoot(2, TierLoot.LootType.Ability, 0.08f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.1f),
                new TierLoot(6, TierLoot.LootType.Weapon, 0.12f),
                new ItemLoot("Magic Potion", 0.8f),
                new ItemLoot("Health Potion", 0.8f),
                new Threshold(0.01f,
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.07f),
                    new TierLoot(9, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.12f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.07f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(3, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(4, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(2, TierLoot.LootType.Ring, 0.07f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.05f)
                )
            );
            db.Init("Treasure Plunderer",
                new State("Player",
                    new PlayerWithinTransition(15, "Start")
                ),
                new State("Start",
                    new Shoot(7, 1, index: 0, cooldown: 1),
                    new Grenade(radius: 3, damage: 75, range: 7, cooldown: 1000),
                    new Wander(0.4f)
                ),
                new ItemLoot("Magic Potion", 0.3f),
                new ItemLoot("Health Potion", 0.3f),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Defense", 0.03f)
                )
            );
            db.Init("Treasure Robber",
                new State("Player",
                    new PlayerWithinTransition(10, "Start")
                ),
                new State("Start",
                    new SetAltTexture(0, 0),
                    new Shoot(15, 3, 20, 0, cooldown: 1000),
                    new TimedTransition(2500, "Invisible"),
                    new Wander(0.4f)
                ),
                new State("Invisible",
                    new SetAltTexture(0, 7, 140),
                    new Shoot(15, 3, 20, 0, cooldown: 1000),
                    new Wander(0.4f),
                    new TimedTransition(6000, "Start")
                ),
                new ItemLoot("Magic Potion", 0.3f),
                new ItemLoot("Health Potion", 0.3f)
            );
            db.Init("Treasure Thief",
                new State("Player",
                    new PlayerWithinTransition(10, "Start")
                ),
                new State("Start",
                    new Wander(0.6f),
                    new StayBack(0.6f, 6)
                ),
                new ItemLoot("Magic Potion", 0.3f),
                new ItemLoot("Health Potion", 0.3f),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Attack", 0.01f),
                    new ItemLoot("Potion of Dexterity", 0.01f)
                )
            );
            db.Init("Treasure Enemy",
                new State("Player",
                    new PlayerWithinTransition(4, "Start")
                ),
                new State("Start",
                    new Shoot(20, 2, 20, 0, cooldown: 1000),
                    new Shoot(20, 1, index: 1, cooldown: 1000),
                    new NoPlayerWithinTransition(4, "Player"),
                    new Follow(0.4f, 6, 1),
                    new Wander(0.4f)
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Speed", 0.01f),
                    new ItemLoot("Potion of Defense", 0.01f),
                    new ItemLoot("Potion of Dexterity", 0.01f),
                    new ItemLoot("Potion of Attack", 0.01f)
                )
            );
            db.Init("Treasure Rat",
                new State("Player",
                    new PlayerWithinTransition(7, "Start")
                ),
                new State("Start",
                    new SetAltTexture(0, 1, cooldown: 140),
                    new ChangeSize(20, 200),
                    new Shoot(10, 1, index: 0, cooldown: 1500),
                    new Follow(0.3f, 10, 1),
                    new Wander(0.3f)
                ),
                new ItemLoot("Health Potion", 0.3f),
                new ItemLoot("Magic Potion", 0.3f)
            );
        }
    }
}
