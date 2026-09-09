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
    public class WineCellar : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Monstrosity Scarab",
                new State("Attack",
                    new State("Charge",
                        new Prioritize(
                            new Charge(range: 25, cooldown: 1000),
                            new Wander(0.3f)
                        ),
                        new PlayerWithinTransition(1, "Boom")
                    ),
                    new State("Boom",
                        new Shoot(1, count: 16, shootAngle: 360 / 16, fixedAngle: 0),
                        new Decay(0)
                    )
                )
            );
            db.Init("Vintner of Oryx",
                new State("Attack",
                    new Prioritize(
                        new Protect(1, "Oryx the Mad God 2", protectionRange: 4, reprotectRange: 3),
                        new Charge(speed: 1, range: 15, cooldown: 2000),
                        new Protect(1, "Henchman of Oryx"),
                        new StayBack(1, 15),
                        new Wander(1)
                    ),
                    new Shoot(10, cooldown: 250)
                )
            );
            db.Init("Aberrant Blaster",
                new State("Wait",
                    new PlayerWithinTransition(3, "Boom")
                ),
                new State("Boom",
                    new Shoot(10, count: 5, shootAngle: 7),
                    new Decay(0)
                )
            );
            db.Init("Abomination of Oryx",
                new State("Shoot",
                    new Shoot(1, 3, shootAngle: 5, index: 0),
                    new Shoot(1, 5, shootAngle: 5, index: 1),
                    new Shoot(1, 7, shootAngle: 5, index: 2),
                    new Shoot(1, 5, shootAngle: 5, index: 3),
                    new Shoot(1, 3, shootAngle: 5, index: 4),
                    new TimedTransition(1000, "Wait")
                ),
                new State("Wait",
                    new PlayerWithinTransition(2, "Shoot")
                ),
                new Prioritize(
                    new Charge(3, 10, 3000),
                    new Wander(0.5f)
                )
            );
        }
    }
}
