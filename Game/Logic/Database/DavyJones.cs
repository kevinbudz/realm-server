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
    public class DavyJones : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Davy Jones",
                new DropPortalOnDeath("Realm Portal", probability: 1.0, timeout: null),
                new State("Waiting",
                    new PlayerWithinTransition(10, "Floating")
                ),
                new State("Floating",
                    new StayCloseToSpawn(0.1, 3),
                    new ChangeSize(100, 100),
                    new SetAltTexture(1),
                    new SetAltTexture(3),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new TimedTransition(200, "buffer")
                ),
                new State("buffer",
                    new StayCloseToSpawn(0.1, 3),
                    new Wander(0.3f),
                    new Shoot(10, 5, 10, 0, cooldown: 1000),
                    new Shoot(10, 1, 10, 1, cooldown: 2000),
                    new EntityNotExistsTransition("Ghost Lanturn Off", 30, "Vunerable")
                ),
                new State("CheckOffLanterns",
                    new SetAltTexture(2),
                    new StayCloseToSpawn(0.1, 3),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntityNotExistsTransition("Ghost Lanturn Off", 30, "Vunerable")
                ),
                new State("Vunerable",
                    new SetAltTexture(5),
                    new StayCloseToSpawn(0.1, 0),
                    new TimedTransition(2500, "deactivate")
                ),
                new State("deactivate",
                    new SetAltTexture(4),
                    new StayCloseToSpawn(0.1, 0),
                    new EntityNotExistsTransition("Ghost Lanturn On", 30, "Floating")
                ),
                new Threshold(0.03f,
                    new ItemLoot("Spirit Dagger", 0.003f),
                    new ItemLoot("Spectral Cloth Armor", 0.01f),
                    new ItemLoot("Captain's Ring", 0.01f),
                    new ItemLoot("Ghostly Prism", 0.01f)
                ),
                new Threshold(0.005f,
                    new TierLoot(tier: 10, type: TierLoot.LootType.Weapon, chance: 0.07f),
                    new TierLoot(tier: 11, type: TierLoot.LootType.Weapon, chance: 0.07f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ability, chance: 0.07f),
                    new TierLoot(tier: 5, type: TierLoot.LootType.Ability, chance: 0.07f),
                    new TierLoot(tier: 11, type: TierLoot.LootType.Armor, chance: 0.07f),
                    new TierLoot(tier: 12, type: TierLoot.LootType.Armor, chance: 0.07f),
                    new TierLoot(tier: 4, type: TierLoot.LootType.Ring, chance: 0.07f),
                    new TierLoot(tier: 5, type: TierLoot.LootType.Ring, chance: 0.07f)
                ),
                new Threshold(0.005f,
                    new ItemLoot("Potion of Wisdom", 1),
                    new ItemLoot("Potion of Attack", 1),
                    new ItemLoot("Davy's Key", 0.009f, 0.03f),
                    new ItemLoot("Ruby Gemstone", 0.02f),
                    new ItemLoot("Golden Chalice", 0.025f),
                    new ItemLoot("Pearl Necklace", 0.035f)
                )
            );
            db.Init("Ghost Lanturn Off",
                new State("default",
                    new HpLessTransition(0.25, "transform")
                ),
                new State("transform",
                    new Transform("Ghost Lanturn On")
                )
            );
            db.Init("Ghost Lanturn On",
                new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                new State("idle",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntitiesNotExistsTransition(40, "Wait", "Ghost Lanturn Off")
                ),
                new State("Wait",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new TimedTransition(4000, "deactivate")
                ),
                new State("deactivate",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new EntitiesNotExistsTransition(40, "shoot", "Ghost Lanturn Off"),
                    new TimedTransition(10000, "gone")
                ),
                new State("shoot",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Shoot(10, 6, cooldown: 9000001, cooldownOffset: 100),
                    new TimedTransition(1000, "gone")
                ),
                new State("gone",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Transform("Ghost Lanturn Off")
                )
            );
            db.Init("Lost Soul",
                new State("Default",
                    new Prioritize(
                        new Orbit(2, 3, 20, "Ghost of Roger"),
                        new Wander(0.3f)
                    ),
                    new PlayerWithinTransition(4, "Default1")
                ),
                new State("Default1",
                    new Charge(4, 8, cooldown: 2000),
                    new TimedTransition(2200, "Blammo")
                ),
                new State("Blammo",
                    new Shoot(10, count: 6, index: 0, cooldown: 2000),
                    new Suicide()
                )
            );
            db.Init("Ghost of Roger",
                new State("spawn",
                    new Spawn("Lost Soul", 3, 1, 5000),
                    new TimedTransition(100, "Attack")
                ),
                new State("Attack",
                    new Shoot(13, 1, 0, 0, cooldown: 10),
                    new TimedTransition(20, "Attack2")
                ),
                new State("Attack2",
                    new Shoot(13, 1, 0, 0, cooldown: 10),
                    new TimedTransition(20, "Attack3")
                ),
                new State("Attack3",
                    new Shoot(13, 1, 0, 0, cooldown: 10),
                    new TimedTransition(20, "Wait")
                ),
                new State("Wait",
                    new TimedTransition(1000, "Attack")
                )
            );
            db.Init("Purple Key",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new PlayerWithinTransition(1, "Cycle")
                ),
                new State("Cycle",
                    new RemoveTileObject("GhostShip PurpleDoor Lf", 200),
                    new RemoveTileObject("GhostShip PurpleDoor Rt", 200),
                    new Taunt(true, "Purple Key has been found!"),
                    new GlobalNotification("purple"),
                    new Suicide()
                )
            );
            db.Init("Red Key",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new PlayerWithinTransition(1, "Cycle")
                ),
                new State("Cycle",
                    new RemoveTileObject("GhostShip RedDoor Lf", 200),
                    new RemoveTileObject("GhostShip RedDoor Rt", 200),
                    new Taunt(true, "Red Key has been found!"),
                    new GlobalNotification("red"),
                    new Suicide()
                )
            );
            db.Init("Green Key",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new PlayerWithinTransition(1, "Cycle")
                ),
                new State("Cycle",
                    new RemoveTileObject("GhostShip GreenDoor Lf", 200),
                    new RemoveTileObject("GhostShip GreenDoor Rt", 200),
                    new Taunt(true, "Green Key has been found!"),
                    new GlobalNotification("green"),
                    new Suicide()
                )
            );
            db.Init("Yellow Key",
                new State("Idle",
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new PlayerWithinTransition(1, "Cycle")
                ),
                new State("Cycle",
                    new Taunt(true, "Yellow Key has been found!"),
                    new GlobalNotification("yellow"),
                    new RemoveTileObject("GhostShip YellowDoor Lf", 200),
                    new RemoveTileObject("GhostShip YellowDoor Rt", 200),
                    new Suicide()
                )
            );
            db.Init("Lil' Ghost Pirate",
                new ChangeSize(30, 120),
                new Shoot(10, count: 1, index: 0, cooldown: 2000),
                new State("Default",
                    new Prioritize(
                        new Follow(1, 8, 1),
                        new Wander(0.3f)
                    ),
                    new TimedTransition(2850, "Default1")
                ),
                new State("Default1",
                    new StayBack(0.2, 3),
                    new TimedTransition(1850, "Default")
                )
            );
            db.Init("Zombie Pirate Sr",
                new Shoot(10, count: 1, index: 0, cooldown: 2000),
                new State("Default",
                    new Prioritize(
                        new Follow(1, 8, 1),
                        new Wander(0.3f)
                    ),
                    new TimedTransition(2850, "Default1")
                ),
                new State("Default1",
                    new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Prioritize(
                        new Follow(1.2, 8, 1),
                        new Wander(0.3f)
                    ),
                    new TimedTransition(2850, "Default")
                )
            );
            db.Init("Zombie Pirate Jr",
                new Shoot(10, count: 1, index: 0, cooldown: 2500),
                new State("Default",
                    new Prioritize(
                        new Follow(1.2, 8, 1),
                        new Wander(0.3f)
                    ),
                    new TimedTransition(2850, "Default1")
                ),
                new State("Default1",
                    new Swirl(0.2, 3),
                    new TimedTransition(1850, "Default")
                )
            );
            db.Init("Captain Summoner",
                new State("Default",
                    new ConditionalEffect(ConditionEffectIndex.Invincible)
                )
            );
            db.Init("GhostShip Rat",
                new State("Default",
                    new Shoot(10, count: 1, index: 0, cooldown: 1750),
                    new Prioritize(
                        new Follow(1, 8, 1),
                        new Wander(0.3f)
                    )
                )
            );
            db.Init("Violent Spirit",
                new State("Default",
                    new ChangeSize(35, 120),
                    new Shoot(10, count: 3, index: 0, cooldown: 1750),
                    new Prioritize(
                        new Follow(1, 8, 1),
                        new Wander(0.3f)
                    )
                )
            );
            db.Init("School of Ghostfish",
                new State("Default",
                    new Shoot(10, count: 3, shootAngle: 18, index: 0, cooldown: 4000),
                    new Wander(0.3f)
                )
            );
        }
    }
}
