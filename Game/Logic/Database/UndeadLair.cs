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
    public class UndeadLair : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Septavius the Ghost God",
                new PlayerWithinTransition(8, "transition1"),
                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new Flash(0x00FF00, 0.25f, 12),
                new Wander(0.1f),
                new State("transition1",
                    new TimedTransition(3000, "spiral")
                ),
                new State("transition2",
                    new TimedTransition(3000, "ring")
                ),
                new State("transition3",
                    new TimedTransition(3000, "quiet")
                ),
                new State("transition4",
                    new TimedTransition(3000, "spawn")
                ),
                new State("spiral",
                    new Spawn("Lair Ghost Archer", 1, 1),
                    new Spawn("Lair Ghost Knight", 2, 2),
                    new Spawn("Lair Ghost Mage", 1, 1),
                    new Spawn("Lair Ghost Rogue", 2, 2),
                    new Spawn("Lair Ghost Paladin", 1, 1),
                    new Spawn("Lair Ghost Warrior", 2, 2),
                    new Shoot(10, 3, fixedAngle: 0, cooldownOffset: 0, cooldown: 1000),
                    new Shoot(10, 3, fixedAngle: 24, cooldownOffset: 200, cooldown: 1000),
                    new Shoot(10, 3, fixedAngle: 48, cooldownOffset: 400, cooldown: 1000),
                    new Shoot(10, 3, fixedAngle: 72, cooldownOffset: 600, cooldown: 1000),
                    new Shoot(10, 3, fixedAngle: 96, cooldownOffset: 800, cooldown: 1000),
                    new TimedTransition(10000, "transition2")
                ),
                new State("ring",
                    new Wander(0.1f),
                    new Shoot(10, 12, index: 4, cooldown: 2000),
                    new TimedTransition(10000, "transition3")
                ),
                new State("quiet",
                    new Wander(0.1f),
                    new Shoot(10, 8, index: 1, cooldown: 1000),
                    new Shoot(10, 8, index: 1, cooldownOffset: 500, angleOffset: 22.5f, cooldown: 1000),
                    new Shoot(8, 3, shootAngle: 20, index: 2, cooldown: 2000),
                    new TimedTransition(10000, "transition4")
                ),
                new State("spawn",
                    new Wander(0.1f),
                    new Spawn("Ghost Mage of Septavius", 2, 2),
                    new Spawn("Ghost Rogue of Septavius", 2, 2),
                    new Spawn("Ghost Warrior of Septavius", 2, 2),
                    new Reproduce("Ghost Mage of Septavius", densityMax: 2, cooldown: 1000),
                    new Reproduce("Ghost Rogue of Septavius", densityMax: 2, cooldown: 1000),
                    new Reproduce("Ghost Warrior of Septavius", densityMax: 2, cooldown: 1000),
                    new Shoot(8, 3, shootAngle: 10, index: 1, cooldown: 1000),
                    new TimedTransition(10000, "transition1")
                ),
                new Threshold(0.32f, /* Maximum 3 wis, minimum 0 wis */
                    new ItemLoot("Potion of Wisdom", 1)
                ),
                new Threshold(0.1f,
                    new ItemLoot("Doom Bow", 0.012f),
                    new ItemLoot("Wine Cellar Incantation", 0.005f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.2f),
                    new TierLoot(4, TierLoot.LootType.Ring, 0.1f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.2f),
                    new TierLoot(4, TierLoot.LootType.Ability, 0.15f),
                    new TierLoot(5, TierLoot.LootType.Ability, 0.1f)
                ),
                new Threshold(0.2f
                )
            );
            db.Init("Ghost Mage of Septavius",
                new Prioritize(
                    new Protect(0.625f, "Septavius the Ghost God", protectionRange: 6),
                    new Follow(0.75f, range: 7)
                ),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );
            db.Init("Ghost Rogue of Septavius",
                new Follow(0.75f, range: 1),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );
            db.Init("Ghost Warrior of Septavius",
                new Follow(0.75f, range: 1),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );
            db.Init("Lair Ghost Archer",
                new Prioritize(
                    new Protect(0.625f, "Septavius the Ghost God", protectionRange: 6),
                    new Follow(0.75f, range: 7)
                ),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );
            db.Init("Lair Ghost Knight",
                new Follow(0.75f, range: 1),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );
            db.Init("Lair Ghost Mage",
                new Prioritize(
                    new Protect(0.625f, "Septavius the Ghost God", protectionRange: 6),
                    new Follow(0.75f, range: 7)
                ),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );
            db.Init("Lair Ghost Paladin",
                new Follow(0.75f, range: 1),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new HealGroup(5, "Lair Ghost", cooldown: 5000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );
            db.Init("Lair Ghost Rogue",
                new Follow(0.75f, range: 1),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );
            db.Init("Lair Ghost Warrior",
                new Follow(0.75f, range: 1),
                new Wander(0.25f),
                new Shoot(8, 1, cooldown: 1000),
                new ItemLoot("Health Potion", 0.25f),
                new ItemLoot("Magic Potion", 0.25f)
            );

            db.Init("Lair Skeleton",
                new Shoot(6),
                new Prioritize(
                    new Follow(1, range: 1),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Skeleton King",
                new Shoot(10, 3, shootAngle: 10),
                new Prioritize(
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new TierLoot(5, TierLoot.LootType.Armor, 0.2f),
                new Threshold(0.5f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.1f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.1f)
                )
            );
            db.Init("Lair Skeleton Mage",
                new Shoot(10),
                new Prioritize(
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Skeleton Swordsman",
                new Shoot(5),
                new Prioritize(
                    new Follow(1, range: 1),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Skeleton Veteran",
                new Shoot(5),
                new Prioritize(
                    new Follow(1, range: 1),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Mummy",
                new Shoot(10),
                new Prioritize(
                    new Follow(0.9f, range: 7),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Mummy King",
                new Shoot(10),
                new Prioritize(
                    new Follow(0.9f, range: 7),
                    new Wander(0.4f)
                ),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Mummy Pharaoh",
                new Shoot(10),
                new Prioritize(
                    new Follow(0.9f, range: 7),
                    new Wander(0.4f)
                ),
                new TierLoot(5, TierLoot.LootType.Armor, 0.2f),
                new Threshold(0.5f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.1f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.1f)
                )
            );

            db.Init("Lair Big Brown Slime",
                new Shoot(10, 3, shootAngle: 10, cooldown: 500),
                new Wander(0.1f),
                new TransformOnDeath("Lair Little Brown Slime", 1, 6, 1)
                // new SpawnOnDeath("Lair Little Brown Slime", 1.0f, 6)
            );
            db.Init("Lair Little Brown Slime",
                new Shoot(10, 3, shootAngle: 10, cooldown: 500),
                new Protect(0.1f, "Lair Big Brown Slime", acquireRange: 5, protectionRange: 2),
                new Wander(0.1f),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Big Black Slime",
                new Shoot(10, cooldown: 1000),
                new Wander(0.1f),
                new TransformOnDeath("Lair Little Black Slime", 1, 4, 1)
                //new SpawnOnDeath("Lair Medium Black Slime", 1.0f, 4)
            );
            db.Init("Lair Medium Black Slime",
                new Shoot(10, cooldown: 1000),
                new Wander(0.1f),
                new TransformOnDeath("Lair Little Black Slime", 1, 4, 1)
                // new SpawnOnDeath("Lair Little Black Slime", 1.0f, 4)
            );
            db.Init("Lair Little Black Slime",
                new Shoot(10, cooldown: 1000),
                new Wander(0.1f),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );

            db.Init("Lair Construct Giant",
                new Prioritize(
                    new Follow(0.8f, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(10, 3, shootAngle: 20, cooldown: 1000),
                new Shoot(10, index: 1, cooldown: 1000),
                new TierLoot(5, TierLoot.LootType.Armor, 0.2f),
                new Threshold(0.5f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.1f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.1f)
                )
            );
            db.Init("Lair Construct Titan",
                new Prioritize(
                    new Follow(0.8f, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(10, 3, shootAngle: 20, cooldown: 1000),
                new Shoot(10, 3, shootAngle: 20, index: 1, cooldownOffset: 100, cooldown: 2000),
                new TierLoot(5, TierLoot.LootType.Armor, 0.2f),
                new Threshold(0.5f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.1f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.1f)
                )
            );

            db.Init("Lair Brown Bat",
                new Wander(0.1f),
                new Charge(3, 8, 2000),
                new Shoot(3, cooldown: 1000),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Ghost Bat",
                new Wander(0.1f),
                new Charge(3, 8, 2000),
                new Shoot(3, cooldown: 1000),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );

            db.Init("Lair Reaper",
                new Shoot(3),
                new Follow(1.3f, range: 1),
                new Wander(0.1f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.2f),
                new Threshold(0.5f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.1f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.1f)
                )
            );
            db.Init("Lair Vampire",
                new Shoot(10, cooldown: 500),
                new Shoot(3, cooldown: 1000),
                new Follow(1.3f, range: 1),
                new Wander(0.1f),
                new ItemLoot("Health Potion", 0.05f),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Lair Vampire King",
                new Shoot(10, cooldown: 500),
                new Shoot(3, cooldown: 1000),
                new Follow(1.3f, range: 1),
                new Wander(0.1f),
                new TierLoot(5, TierLoot.LootType.Armor, 0.2f),
                new Threshold(0.5f,
                    new TierLoot(6, TierLoot.LootType.Weapon, 0.2f),
                    new TierLoot(7, TierLoot.LootType.Weapon, 0.1f),
                    new TierLoot(8, TierLoot.LootType.Weapon, 0.05f),
                    new TierLoot(6, TierLoot.LootType.Armor, 0.1f),
                    new TierLoot(7, TierLoot.LootType.Armor, 0.05f),
                    new TierLoot(3, TierLoot.LootType.Ring, 0.1f),
                    new TierLoot(3, TierLoot.LootType.Ability, 0.1f)
                )
            );

            db.Init("Lair Grey Spectre",
                new Wander(0.1f),
                new Shoot(10, cooldown: 1000),
                new Grenade(radius: 2.5f, damage: 50, range: 8, cooldown: 1000)
            );
            db.Init("Lair Blue Spectre",
                new Wander(0.1f),
                new Shoot(10, cooldown: 1000),
                new Grenade(radius: 2.5f, damage: 70, range: 8, cooldown: 1000)
            );
            db.Init("Lair White Spectre",
                new Wander(0.1f),
                new Shoot(10, cooldown: 1000),
                new Grenade(radius: 2.5f, damage: 90, range: 8, cooldown: 1000),
                new Threshold(0.5f,
                    new TierLoot(4, TierLoot.LootType.Ability, 0.15f)
                )
            );
            db.Init("Lair Burst Trap",
                new State("FinnaBustANut",
                    new PlayerWithinTransition(3, "Aaa")
                ),
                new State("Aaa",
                    new Shoot(8.4f, count: 12, index: 0),
                    new Suicide()
            ));
            db.Init("Lair Blast Trap",
                new State("FinnaBustANut",
                    new PlayerWithinTransition(3, "Aaa")
                ),
                new State("Aaa",
                    new Shoot(25, index: 0, count: 12, cooldown: 3000),
                    new Suicide()
                )

            );
        }
    }
}
