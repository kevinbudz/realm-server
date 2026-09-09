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
    public class RedDemon : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Red Demon",
                new Shoot(10, index: 0, count: 5, shootAngle: 5, predictive: 1, cooldown: 1200),
                new Shoot(11, index: 1, cooldown: 1400),
                new Prioritize(
                    new StayCloseToSpawn(0.8f, 5),
                    new Follow(1, range: 7),
                    new Wander(0.4f)
                ),
                new Spawn("Imp", maxChildren: 5, cooldown: 10000),
                new Spawn("Demon", maxChildren: 3, cooldown: 14000),
                new Spawn("Demon Warrior", maxChildren: 3, cooldown: 18000),
                new Taunt(0.7f, 10000,
                    "I will deliver your soul to Oryx, {PLAYER}!",
                    "Oryx will not end our pain. We can only share it... with you!",
                    "Our anguish is endless, unlike your lives!",
                    "There can be no forgiveness!",
                    "What do you know of suffering? I can teach you much, {PLAYER}",
                    "Would you attempt to destroy us? I know your name, {PLAYER}!",
                    "You cannot hurt us. You cannot help us. You will feed us.",
                    "Your life is an affront to Oryx. You will die."
                ),
                new Threshold(0.01f,
                    new ItemLoot("Golden Sword", 0.04f),
                    new ItemLoot("Steel Helm", 0.04f)
                )
            );
            db.Init("Imp",
                new Prioritize(
                    new StayCloseToSpawn(1.4f, 15),
                    new Wander(0.8f)
                ),
                new Shoot(10, predictive: 0.5f, cooldown: 200),
                new ItemLoot("Missile Wand", 0.02f),
                new ItemLoot("Serpentine Staff", 0.02f),
                new ItemLoot("Fire Bow", 0.02f)
            );
            db.Init("Demon",
                new Prioritize(
                    new StayCloseToSpawn(1.4f, 15),
                    new Follow(1.4f, range: 7),
                    new Wander(0.4f)
                ),
                new Shoot(10, count: 2, shootAngle: 7, predictive: 0.5f),
                new ItemLoot("Fire Bow", 0.03f)
            );
            db.Init("Demon Warrior",
                new Prioritize(
                    new StayCloseToSpawn(1.4f, 15),
                    new Follow(1, range: 2.8f),
                    new Wander(0.4f)
                ),
                new Shoot(10, count: 3, shootAngle: 7, predictive: 0.5f),
                new ItemLoot("Obsidian Dagger", 0.03f),
                new ItemLoot("Steel Shield", 0.02f)
            );
        }
    }
}
