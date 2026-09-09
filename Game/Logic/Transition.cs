using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game.Logic
{
    public abstract class Transition : IBehavior
    {
        public readonly int Id;

        public Transition(string targetState)
        {
            StringTargetState = targetState.ToLower();
            StringTargetStates = new string[] { StringTargetState };
            Id = ++BehaviorDb.NextId;
        }

        protected Transition(params string[] targetStates)
        {
            StringTargetStates = targetStates.Select(s => s.ToLower()).ToArray();
            StringTargetState = StringTargetStates[0];
            Id = ++BehaviorDb.NextId;
        }

        public string StringTargetState; //Only used for parsing.
        public string[] StringTargetStates; //Only used for parsing.
        public int TargetState;
        public int[] TargetStates;
        public int SelectedState;

        public virtual void Enter(Entity host) { }
        public virtual bool Tick(Entity host) => false;
        public virtual void Exit(Entity host) { }
    }
}
