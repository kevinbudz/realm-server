using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace RotMG.Game.Logic
{
    public abstract class Behavior : IBehavior
    {
        public readonly int Id;

        public Behavior()
        {
            Id = ++BehaviorDb.NextId;
        }

        public virtual void Enter(Entity host) { }
        public virtual bool Tick(Entity host) => true;
        // Some composites need to know when an otherwise-running behavior
        // reached a natural cycle boundary. Keep this result per host rather
        // than storing completion on shared behavior definitions.
        public virtual bool IsCycleComplete(Entity host) => false;
        public virtual void Exit(Entity host) { }
        public virtual void Death(Entity host) { }
    }
}
