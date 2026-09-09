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
    public class Pentaract : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Pentaract Eye",
                new Prioritize(
                    new Swirl(2, 8, 20, true),
                    new Protect(2, "Pentaract Tower", 20, 6, 4)
                ),
                new Shoot(9, 1, cooldown: 1000)
            );
            db.Init("Pentaract Tower",
                new Spawn("Pentaract Eye", 5, cooldown: 5000),
                new Grenade(radius: 4, damage: 100, range: 8, cooldown: 5000),
                new TransformOnDeath("Pentaract Tower Corpse"),
                new TransferDamageOnDeath("Pentaract"),
                // needed to avoid crash, Oryx.cs needs player name otherwise hangs server (will patch that later)
                new TransferDamageOnDeath("Pentaract Tower Corpse")
            );
            db.Init("Pentaract",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("Waiting",
                    new EntityNotExistsTransition("Pentaract Tower", 50, "Die")
                ),
                new State("Die",
                    new Suicide()
                )
            );
            db.Init("Pentaract Tower Corpse",
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("Waiting",
                    new TimedTransition(15000, "Spawn"),
                    new EntityNotExistsTransition("Pentaract Tower", 50, "Die")
                ),
                new State("Spawn",
                    new Transform("Pentaract Tower")
                ),
                new State("Die",
                    new Suicide()
                ),
                new Threshold(0.01f,
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
                    new ItemLoot("Seal of Blasphemous Prayer", 0.004f)
                )
            );
        }
    }
}
