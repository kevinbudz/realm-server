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
    public class Oasis : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Oasis Giant",
                new Shoot(10, count: 4, shootAngle: 7, predictive: 1),
                new Prioritize(
                    new StayCloseToSpawn(0.3f, 2),
                    new Wander(0.4f)
                ),
                new SpawnGroup("Oasis", maxChildren: 20, cooldown: 5000),
                new Taunt(0.7f, 10000,
                    "Come closer, {PLAYER}! Yes, closer!",
                    "I rule this place, {PLAYER}!",
                    "Surrender to my aquatic army, {PLAYER}!",
                    "You must be thirsty, {PLAYER}. Enter my waters!",
                    "Minions! We shall have {PLAYER} for dinner!"
                )
            );
            db.Init("Oasis Ruler",
                new Prioritize(
                    new Protect(0.5f, "Oasis Giant", acquireRange: 15, protectionRange: 10, reprotectRange: 3),
                    new Follow(1, range: 9),
                    new Wander(0.5f)
                ),
                new Shoot(10),
                new ItemLoot("Magic Potion", 0.05f)
            );
            db.Init("Oasis Soldier",
                new Prioritize(
                    new Protect(0.5f, "Oasis Giant", acquireRange: 15, protectionRange: 11, reprotectRange: 3),
                    new Follow(1, range: 7),
                    new Wander(0.5f)
                ),
                new Shoot(10, predictive: 0.5f),
                new ItemLoot("Health Potion", 0.05f)
            );
            db.Init("Oasis Creature",
                new Prioritize(
                    new Protect(0.5f, "Oasis Giant", acquireRange: 15, protectionRange: 12, reprotectRange: 3),
                    new Follow(1, range: 5),
                    new Wander(0.5f)
                ),
                new Shoot(10, cooldown: 400),
                new ItemLoot("Health Potion", 0.05f)
            );
            db.Init("Oasis Monster",
                new Prioritize(
                    new Protect(0.5f, "Oasis Giant", acquireRange: 15, protectionRange: 13, reprotectRange: 3),
                    new Follow(1, range: 3),
                    new Wander(0.5f)
                ),
                new Shoot(10, predictive: 0.5f),
                new ItemLoot("Magic Potion", 0.05f)
            );
        }
    }
}
