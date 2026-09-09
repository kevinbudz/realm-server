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
    public class SkullShrine : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Skull Shrine",
                new Shoot(30, 9, 10, cooldown: 750, predictive: 1), // add prediction after fixing it...
                new Reproduce("Red Flaming Skull", 40, 20, cooldown: 500),
                new Reproduce("Blue Flaming Skull", 40, 20, cooldown: 500),
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
                    new ItemLoot("Large Cloud Cloth", 0.01f),
                    new ItemLoot("Small Cloud Cloth", 0.01f),
                    new ItemLoot("Large Plaid Cloth", 0.01f),
                    new ItemLoot("Small Plaid Cloth", 0.01f),
                    new ItemLoot("Large Skull Cloth", 0.01f),
                    new ItemLoot("Small Skull Cloth", 0.01f),
                    new ItemLoot("Orb of Conflict", 0.001f)
                )
            );
            db.Init("Red Flaming Skull",
                new State("Orbit Skull Shrine",
                    new Prioritize(
                        new Protect(0.3f, "Skull Shrine", 30, 15, 15),
                        new Wander(0.3f)
                    ),
                    new EntityNotExistsTransition("Skull Shrine", 40, "Wander")
                ),
                new State("Wander",
                    new Wander(0.3f)
                ),
                new Shoot(12, 2, 10, cooldown: 750)
            );
            db.Init("Blue Flaming Skull",
                new State("Orbit Skull Shrine",
                    new Orbit(1.5f, 15, 40, "Skull Shrine", 0.6f, 10, orbitClockwise: null),
                    new EntityNotExistsTransition("Skull Shrine", 40, "Wander")
                ),
                new State("Wander",
                    new Wander(1.5f)
                ),
                new Shoot(12, 2, 10, cooldown: 750)
            );
        }
    }
}
