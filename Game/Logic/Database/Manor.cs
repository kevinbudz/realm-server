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
    public class Manor : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Lord Ruthven",
                new DropPortalOnDeath("Realm Portal", probability: 1, timeout: null),
                new State("default",
                    new PlayerWithinTransition(8, "spooksters")
                ),
                new State("spooksters",
                    new Wander(0.2f),
                    new Shoot(10, 5, 2, index: 0, cooldown: 900),
                    new TimedTransition(6000, "spooksters2")
                ),
                new State("spooksters2",
                    new Wander(0.15f),
                    new Shoot(8.4f, 40, index: 1, cooldown: 2750),
                    new Shoot(10, 5, 2, index: 0, cooldown: 900),
                    new TimedTransition(4000, "spooksters3")
                ),
                new State("spooksters3",
                    new HealSelf(2250, 0, 4),
                    new Shoot(8.4f, 40, index: 1, cooldown: 1500),
                    new TimedTransition(4000, "spooksters")
                ),
                new Threshold(0.01f,
                    new ItemLoot("Potion of Attack", 0.5f),
                    new ItemLoot("Potion of Dexterity", 0.5f),
                    new ItemLoot("Potion of Wisdom", 0.5f),
                    new ItemLoot("Manor Key", 0.01f, 0.03f),
                    new ItemLoot("Holy Water", 0.5f),
                    new ItemLoot("Chasuble of Holy Light", 0.003f),
                    new ItemLoot("St. Abraham's Wand", 0.003f),
                    new ItemLoot("Tome of Purification", 0.003f),
                    new ItemLoot("Ring of Divine Faith", 0.003f),
                    new ItemLoot("Bone Dagger", 0.008f)
                ),
                new Threshold(0.005f,
                    new TierLoot(10, TierLoot.LootType.Weapon, 0.07f),
                    new TierLoot(11, TierLoot.LootType.Weapon, 0.07f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.07f),
                    new TierLoot(5, TierLoot.LootType.Ability, 0.07f),
                    new TierLoot(11, TierLoot.LootType.Armor, 0.07f),
                    new TierLoot(12, TierLoot.LootType.Armor, 0.07f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.07f),
                    new TierLoot(5, TierLoot.LootType.Ring, 0.07f)
                )
            );
            db.Init("Hellhound",
                new Follow(1.25, 8, 1, cooldown: 275),
                new Shoot(10, 5, 7, cooldown: 2000),
                new ItemLoot("Magic Potion", 0.05f),
                new Threshold(0.5f,
                    new ItemLoot("Timelock Orb", 0.01f)
                )
            );
            db.Init("Vampire Bat Swarmer",
                new Follow(1.5, 8, 1),
                new Shoot(10, 1, cooldown: 6)
            );
            db.Init("Lil Feratu",
                new Follow(0.35, 8, 1),
                new Shoot(10, 6, 2, cooldown: 900),
                new ItemLoot("Health Potion", 0.05f),
                new Threshold(0.5f,
                    new ItemLoot("Steel Helm", 0.01f)
                )
            );
            db.Init("Lesser Bald Vampire",
                new Follow(0.35, 8, 1),
                new Shoot(10, 5, 6, cooldown: 1000),
                new ItemLoot("Health Potion", 0.05f),
                new Threshold(0.5f,
                    new ItemLoot("Steel Helm", 0.01f)
                )
            );
            db.Init("Nosferatu",
                new Wander(0.25f),
                new Shoot(10, 5, 2, index: 1, cooldown: 1000),
                new Shoot(10, 6, 90, index: 0, cooldown: 1500),
                new ItemLoot("Health Potion", 0.05f),
                new Threshold(0.5f,
                    new ItemLoot("Bone Dagger", 0.01f),
                    new ItemLoot("Wand of Death", 0.05f),
                    new ItemLoot("Golden Bow", 0.04f),
                    new ItemLoot("Steel Helm", 0.05f),
                    new ItemLoot("Ring of Paramount Defense", 0.09f)
                )
            );
            db.Init("Armor Guard",
                new Wander(0.2f),
                new TossObject("RockBomb", 7, cooldown: 3000),
                new Shoot(10, 1, index: 0, predictive: 7, cooldown: 1000),
                new Shoot(10, 1, index: 1, cooldown: 750),
                new ItemLoot("Magic Potion", 0.05f),
                new Threshold(0.5f,
                    new ItemLoot("Glass Sword", 0.01f),
                    new ItemLoot("Staff of Destruction", 0.01f),
                    new ItemLoot("Golden Shield", 0.01f),
                    new ItemLoot("Ring of Paramount Speed", 0.01f)
                )
            );
            db.Init("Coffin Creature",
                new Spawn("Lil Feratu", 2, 0.5, cooldown: 2250),
                new Shoot(10, 1, index: 0, cooldown: 700),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("RockBomb",
                new State("BOUTTOEXPLODE",
                    new TimedTransition(1111, "boom")
                ),
                new State("boom",
                    new Shoot(8.4f, 1, index: 0, fixedAngle: 0, cooldown: 1000),
                    new Shoot(8.4f, 1, index: 0, fixedAngle: 90, cooldown: 1000),
                    new Shoot(8.4f, 1, index: 0, fixedAngle: 180, cooldown: 1000),
                    new Shoot(8.4f, 1, index: 0, fixedAngle: 270, cooldown: 1000),
                    new Shoot(8.4f, 1, index: 0, fixedAngle: 45, cooldown: 1000),
                    new Shoot(8.4f, 1, index: 0, fixedAngle: 135, cooldown: 1000),
                    new Shoot(8.4f, 1, index: 0, fixedAngle: 235, cooldown: 1000),
                    new Shoot(8.4f, 1, index: 0, fixedAngle: 315, cooldown: 1000),
                    new Suicide()
                )
            );
            db.Init("Coffin",
                new State("Coffin1",
                    new HpLessTransition(0.75, "Coffin2")
                ),
                new State("Coffin2",
                    new Spawn("Vampire Bat Swarmer", 15, 0.067, cooldown: 99999),
                    new HpLessTransition(0.40, "Coffin3")
                ),
                new State("Coffin3",
                    new Spawn("Vampire Bat Swarmer", 8, 0.125, cooldown: 99999),
                    new Spawn("Nosferatu", 2, 0.5, cooldown: 99999)
                ),
                new Threshold(0.5f,
                    new ItemLoot("Holy Water", 1.00f),
                    new ItemLoot("Potion of Attack", 0.5f),
                    new ItemLoot("Chasuble of Holy Light", 0.01f),
                    new ItemLoot("St. Abraham's Wand", 0.01f),
                    new ItemLoot("Tome of Purification", 0.001f),
                    new ItemLoot("Ring of Divine Faith", 0.01f),
                    new ItemLoot("Bone Dagger", 0.08f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.2f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.15f)
                )
            );
        }
    }
}
