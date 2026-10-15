// Behavior registrations adapted from ossimc82/fabiano-swagger-of-doom
// (commit 6fd20aad4a7905b13f25389c68368a942a2b68cb, 2015-11-09),
// licensed under AGPL-3.0; see the upstream repository LICENSE.
using RotMG.Game.Logic.Behaviors;

namespace RotMG.Game.Logic.Database
{
    public class Specialized : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {
            db.Init("Zombie Trickster",
                
                    new Wander(1)
                
            );
        }
    }
}
