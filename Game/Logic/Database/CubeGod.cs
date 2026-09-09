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
                new Wander(.3f),
                new Shoot(30, 9, 10, 0, predictive: .5f, cooldown: 750),
                new Shoot(30, 4, 10, 1, predictive: .5f, cooldown: 1500),
                new Reproduce("Cube Overseer", 30, 10, cooldown: 1500),
                new Threshold(.05f,
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
                    new ItemLoot("Dirk of Cronus", .001f)
                    ));
            db.Init("Cube Overseer",
                new Prioritize(
                        new Orbit(.375, 10, 30, "Cube God", .075, 5),
                        new Wander(.375f)
                        ),
                new Reproduce("Cube Defender", 12, 10, cooldown: 1000),
                new Reproduce("Cube Blaster", 30, 10, cooldown: 1000),
                new Shoot(10, 4, 10, 0, cooldown: 750),
                new Shoot(10, index: 1, cooldown: 1500),
                new Threshold(.01f,
                    new ItemLoot("Fire Sword", .05f)
                    ));
            db.Init("Cube Defender",
                new Prioritize(
                        new Orbit(1.05, 5, 15, "Cube Overseer", .15, 3),
                        new Wander(1.05f)
                        ),
                new Shoot(10, cooldown: 500));
            db.Init("Cube Blaster",
                new Shoot(10, 2, 10, 1, predictive: 1, cooldown: 500),
                new Shoot(10, predictive: 1, cooldown: 1500),
                new State("Orbit",
                        new Prioritize(
                            new Orbit(1.05, 7.5, 40, "Cube Overseer", .15, 3),
                            new Wander(1.05f)
                            ),
                        new EntityNotExistsTransition("Cube Overseer", 10, "Follow")
                        ),
                new State("Follow",
                        new Prioritize(
                            new Follow(.75, 10, 1, 5000),
                            new Wander(1.05f)
                            ),
                        new EntityNotExistsTransition("Cube Defender", 10, "Orbit"),
                        new TimedTransition("Orbit", 5000)
                        ));
        }
    }
}
