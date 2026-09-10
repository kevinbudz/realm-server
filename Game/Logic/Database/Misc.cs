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
    public class Misc : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("White Fountain",
                new HealPlayer(5, 1000, 100)
            );
            db.Init("Sheep",
                new PlayerWithinTransition(15, "player_nearby"),
                new State("player_nearby",
                    new Prioritize(
                        new StayCloseToSpawn(0.1f, 2),
                        new Wander(0.1f)
                    ),
                    new Taunt(0.001f, 1000, "baa", "baa baa")
                )
            );
            db.InitMany("Black Cat", "Snowman", name =>
                new IBehavior[]
                {
                    new PetFollow()
                }
            );
        }
    }
}
