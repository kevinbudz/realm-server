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
    public class PirateCave : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Dreadstump the Pirate King",
                new DropPortalOnDeath("Realm Portal", probability: 1, timeout: null),
                new State("Idle",
                    new PlayerWithinTransition(15, "swiggity")
                ),
                new State("swiggity",
                    new StayCloseToSpawn(1, 7),
                    new Wander(0.3f),
                    new Shoot(8, 1, index: 1, predictive: 0.9f, cooldown: 2000),
                    new Shoot(8, 1, index: 0, predictive: 0.9f, cooldown: 1000),
                    new Taunt(0.3, 14000,
                        "Hah! I'll drink my rum out of your skull!",
                        "Eat cannonballs!",
                        "Arrrr..."
                    )
                ),
                new Threshold(0.03f,
                    new ItemLoot("Pirate Cave Key", 0.01f)
                ),
                new TierLoot(4, TierLoot.LootType.Weapon, 0.01f),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(2, TierLoot.LootType.Weapon, 0.15f),
                new TierLoot(1, TierLoot.LootType.Armor, 0.2f),
                new TierLoot(2, TierLoot.LootType.Armor, 0.1f),
                new TierLoot(3, TierLoot.LootType.Armor, 0.05f),
                new TierLoot(4, TierLoot.LootType.Armor, 0.01f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.1f),
                new ItemLoot("Pirate Rum", 0.01f)
            );
            db.Init("Pirate Lieutenant",
                new Prioritize(
                    new Protect(0.4, "Dreadstump the Pirate King", protectionRange: 6),
                    new Wander(0.5f),
                    new Follow(1, 6, 1, -1, 0)
                ),
                new Shoot(7, index: 0, predictive: 1, cooldown: 1500),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(2, TierLoot.LootType.Weapon, 0.15f),
                new TierLoot(1, TierLoot.LootType.Armor, 0.2f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.1f),
                new ItemLoot("Pirate Rum", 0.01f)
            );
            db.Init("Pirate Captain",
                new Prioritize(
                    new Protect(0.4, "Dreadstump the Pirate King", protectionRange: 6),
                    new Wander(0.5f),
                    new Follow(1, 6, 1, -1, 0)
                ),
                new Shoot(7, index: 0, predictive: 1, cooldown: 1500),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(2, TierLoot.LootType.Weapon, 0.15f),
                new TierLoot(1, TierLoot.LootType.Armor, 0.2f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.1f),
                new ItemLoot("Pirate Rum", 0.01f)
            );
            db.Init("Pirate Commander",
                new Prioritize(
                    new Protect(0.4, "Dreadstump the Pirate King", protectionRange: 6),
                    new Wander(0.5f)
                ),
                new Shoot(7, index: 0, predictive: 1, cooldown: 1500),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(2, TierLoot.LootType.Weapon, 0.15f),
                new TierLoot(1, TierLoot.LootType.Armor, 0.2f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.1f),
                new ItemLoot("Pirate Rum", 0.01f)
            );
            db.Init("Cave Pirate Brawler",
                new State("that",
                    new Follow(1, 6, 1, -1, 0),
                    new Wander(0.3f),
                    new Shoot(5, 1, index: 0, cooldown: 1000)
                ),
                new ItemLoot("Magic Potion", 0.2f)
            );
            db.Init("Cave Pirate Sailor",
                new State("booty",
                    new Wander(0.4f),
                    new Follow(0.8, 6, 1, -1, 0),
                    new Shoot(5, 1, index: 0, cooldown: 1000)
                ),
                new ItemLoot("Magic Potion", 0.2f)
            );
            db.Init("Cave Pirate Cabin Boy",
                new Prioritize(
                    new Wander(0.5f)
                ),
                new TierLoot(1, TierLoot.LootType.Weapon, 0.4f)
            );
            db.Init("Cave Pirate Macaw",
                new Prioritize(
                    new Wander(0.5f)
                ),
                new TierLoot(1, TierLoot.LootType.Ability, 0.2f)
            );
            db.Init("Cave Pirate Moll",
                new Prioritize(
                    new Wander(0.1f)
                ),
                new TierLoot(1, TierLoot.LootType.Ability, 0.2f)
            );
            db.Init("Cave Pirate Monkey",
                new Prioritize(
                    new Wander(0.1f)
                ),
                new TierLoot(1, TierLoot.LootType.Ability, 0.2f)
            );
            db.Init("Cave Pirate Parrot",
                new Prioritize(
                    new Wander(0.1f)
                ),
                new TierLoot(1, TierLoot.LootType.Ability, 0.2f)
            );
            db.Init("Cave Pirate Hunchback",
                new Prioritize(
                    new Wander(0.5f)
                ),
                new TierLoot(1, TierLoot.LootType.Ability, 0.2f)
            );
            db.Init("Cave Pirate Veteran",
                new State("woot",
                    new Follow(1, 6, 1, -1, 0),
                    new Wander(0.4f),
                    new Shoot(5, 1, index: 0, cooldown: 1000)
                ),
                new ItemLoot("Magic Potion", 0.2f)
            );
            db.Init("Pirate Admiral",
                new Prioritize(
                    new Protect(0.4, "Dreadstump the Pirate King", protectionRange: 6),
                    new Wander(0.5f),
                    new Follow(1, 6, 1, -1, 0)
                ),
                new Shoot(7, index: 0, predictive: 1, cooldown: 1500),
                new TierLoot(3, TierLoot.LootType.Weapon, 0.05f),
                new TierLoot(2, TierLoot.LootType.Weapon, 0.15f),
                new TierLoot(1, TierLoot.LootType.Armor, 0.2f),
                new TierLoot(2, TierLoot.LootType.Armor, 0.1f),
                new TierLoot(1, TierLoot.LootType.Ring, 0.1f),
                new ItemLoot("Pirate Rum", 0.01f)
            );
        }
    }
}
