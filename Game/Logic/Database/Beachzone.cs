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
    public class Beachzone : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Masked Party God",
                new State("1",
                        new Taunt(true, "Oh no, Mixcoatl is my brother, I prefer partying to fighting."),
                        new SetAltTexture(1),
                        new TimedTransition("2", 500)
                    ),
                new State("2",
                        new Taunt(true, "Lets have a fun-time in the sun-shine!"),
                        new SetAltTexture(2),
                        new TimedTransition("3", 500)
                    ),
                new State("3",
                        new Taunt(true, "Nothing like relaxin' on the beach."),
                        new SetAltTexture(3),
                        new TimedTransition("4", 500)
                    ),
                new State("4",
                        new Taunt(true, "Chillin' is the name of the game!"),
                        new SetAltTexture(1),
                        new TimedTransition("5", 500)
                    ),
                new State("5",
                        new Taunt(true, "I hope you're having a good time!"),
                        new SetAltTexture(2),
                        new TimedTransition("6", 500)
                    ),
                new State("6",
                        new Taunt(true, "How do you like my shades?"),
                        new SetAltTexture(3),
                        new TimedTransition("7", 500)
                    ),
                new State("7",
                        new Taunt(true, "EVERYBODY BOOGEY!"),
                        new SetAltTexture(1),
                        new TimedTransition("8", 500)
                    ),
                new State("8",
                        new Taunt(true, "What a beautiful day!"),
                        new SetAltTexture(2),
                        new TimedTransition("9", 500)
                    ),
                new State("9",
                        new Taunt(true, "Whoa there!"),
                        new SetAltTexture(3),
                        new TimedTransition("10", 500)
                    ),
                new State("10",
                        new Taunt(true, "Oh SNAP!"),
                        new SetAltTexture(1),
                        new TimedTransition("11", 500)
                    ),
                new State("11",
                        new Taunt(true, "Ho!"),
                        new SetAltTexture(2),
                        new TimedTransition("end", 500)
                    ),
                new State("end",
                        new SetAltTexture(3),
                        new TimedTransition("1", 500)
                    ),
                new Threshold(0.1f,
                    new ItemLoot("Blue Paradise", 0.2f),
                    new ItemLoot("Pink Passion Breeze", 0.2f),
                    new ItemLoot("Bahama Sunrise", 0.2f),
                    new ItemLoot("Lime Jungle Bay", 0.2f)
                ));
        }
    }
}
