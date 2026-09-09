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
    public class CubeGod : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Cube God",
                new Wander(0.3f),
                new Shoot(30, 9, 10, 0, predictive: 0.5f, cooldown: 750),
                new Shoot(30, 4, 10, 1, predictive: 0.5f, cooldown: 1500),
                new Reproduce("Cube Overseer", 30, 10, cooldown: 1500),
                new Threshold(0.05f,
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.15f),
                    new TierLoot(9, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(10, TierLoot.LootType.Weapon, 0.07f),
                    new TierLoot(11, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.15f),
                    new TierLoot(5, TierLoot.LootType.Ability, 0.07f),
                    new TierLoot(8, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(9, TierLoot.LootType.Armor, 0.15f),
                    new TierLoot(10, TierLoot.LootType.Armor, 0.10f),
                    new TierLoot(11, TierLoot.LootType.Armor, 0.07f),
                    new TierLoot(12, TierLoot.LootType.Armor, 0.04f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.15f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.07f),
                    new TierLoot(5, TierLoot.LootType.Ring, 0.03f),
                    new ItemLoot("Potion of Defense", 0.1f),
                    new ItemLoot("Potion of Attack", 0.1f),
                    new ItemLoot("Potion of Vitality", 0.1f),
                    new ItemLoot("Potion of Wisdom", 0.1f),
                    new ItemLoot("Potion of Speed", 0.1f),
                    new ItemLoot("Potion of Dexterity", 0.1f),
                    new ItemLoot("Dirk of Cronus", 0.001f)
                )
            );
            db.Init("Cube Overseer",
                new Prioritize(
                    new Orbit(0.375f, 10, 30, "Cube God", 0.075f, 5),
                    new Wander(0.375f)
                ),
                new Reproduce("Cube Defender", 12, 10, cooldown: 1000),
                new Reproduce("Cube Blaster", 30, 10, cooldown: 1000),
                new Shoot(10, 4, 10, 0, cooldown: 750),
                new Shoot(10, index: 1, cooldown: 1500),
                new Threshold(0.01f,
                    new ItemLoot("Fire Sword", 0.05f)
                )
            );
            db.Init("Cube Defender",
                new Prioritize(
                    new Orbit(1.05f, 5, 15, "Cube Overseer", 0.15f, 3),
                    new Wander(1.05f)
                ),
                new Shoot(10, cooldown: 500)
            );
            db.Init("Cube Blaster",
                new State("Orbit",
                    new Prioritize(
                        new Orbit(1.05f, 7.5f, 40, "Cube Overseer", 0.15f, 3),
                        new Wander(1.05f)
                    ),
                    new EntityNotExistsTransition("Cube Overseer", 10, "Follow")
                ),
                new State("Follow",
                    new Prioritize(
                        new Follow(0.75f, 10, 1, 5000),
                        new Wander(1.05f)
                    ),
                    new EntityNotExistsTransition("Cube Defender", 10, "Orbit"),
                    new TimedTransition(5000, "Orbit")
                ),
                new Shoot(10, 2, 10, 1, predictive: 1, cooldown: 500),
                new Shoot(10, predictive: 1, cooldown: 1500)
            );
        }
    }
}
