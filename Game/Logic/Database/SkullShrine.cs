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
                new Shoot(30, 9, 10, cooldown: 750, predictive: 1),
                // add prediction after fixing it...
                    new Reproduce("Red Flaming Skull", 40, 20, cooldown: 500),
                new Reproduce("Blue Flaming Skull", 40, 20, cooldown: 500),
                new Threshold(0.05f,
                    new TierLoot(8, TierLoot.LootType.Weapon, .15f),
                    new TierLoot(9, TierLoot.LootType.Weapon, .1f),
                    new TierLoot(10, TierLoot.LootType.Weapon, .07f),
                    new TierLoot(11, TierLoot.LootType.Weapon, .05f),
                    new TierLoot(4, TierLoot.LootType.Ability, .15f),
                    new TierLoot(5, TierLoot.LootType.Ability, .07f),
                    new TierLoot(8, TierLoot.LootType.Armor, .2f),
                    new TierLoot(9, TierLoot.LootType.Armor, .15f),
                    new TierLoot(10, TierLoot.LootType.Armor, .10f),
                    new TierLoot(11, TierLoot.LootType.Armor, .07f),
                    new TierLoot(12, TierLoot.LootType.Armor, .04f),
                    new TierLoot(3, TierLoot.LootType.Ring, .15f),
                    new TierLoot(4, TierLoot.LootType.Ring, .07f),
                    new TierLoot(5, TierLoot.LootType.Ring, .03f),
                    new ItemLoot("Potion of Defense", .1f),
                    new ItemLoot("Potion of Attack", .1f),
                    new ItemLoot("Potion of Vitality", .1f),
                    new ItemLoot("Potion of Wisdom", .1f),
                    new ItemLoot("Potion of Speed", .1f),
                    new ItemLoot("Potion of Dexterity", .1f),
                    new ItemLoot("Large Cloud Cloth", .01f),
                    new ItemLoot("Small Cloud Cloth", .01f),
                    new ItemLoot("Large Plaid Cloth", .01f),
                    new ItemLoot("Small Plaid Cloth", .01f),
                    new ItemLoot("Large Skull Cloth", .01f),
                    new ItemLoot("Small Skull Cloth", .01f),
                    new ItemLoot("Orb of Conflict", .001f)
                    ));
            db.Init("Red Flaming Skull",
                new Shoot(12, 2, 10, cooldown: 750),
                new State("Orbit Skull Shrine",
                        new Prioritize(
                            new Protect(.3, "Skull Shrine", 30, 15, 15),
                            new Wander(.3f)
                            ),
                        new EntityNotExistsTransition("Skull Shrine", 40, "Wander")
                        ),
                new State("Wander",
                        new Wander(.3f)
                        ));
            db.Init("Blue Flaming Skull",
                new Shoot(12, 2, 10, cooldown: 750),
                new State("Orbit Skull Shrine",
                        new Orbit(1.5, 15, 40, "Skull Shrine", .6, 10, orbitClockwise: null),
                        new EntityNotExistsTransition("Skull Shrine", 40, "Wander")
                        ),
                new State("Wander",
                        new Wander(1.5f)
                        ));
        }
    }
}
