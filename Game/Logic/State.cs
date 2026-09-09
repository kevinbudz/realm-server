using System;
using System.Collections.Generic;
using System.Text;
using RotMG;

namespace RotMG.Game.Logic
{
    public class State : IBehavior
    {
        public string StringId; //Only used for parsing.
        public int Id;

        public State Parent;
        public List<Behavior> Behaviors;
        public List<Transition> Transitions;
        public Dictionary<int, State> States;

        public State(string id, params IBehavior[] behaviors)
        {
            StringId = id.ToLower();
            Id = ++BehaviorDb.NextId;

            Behaviors = new List<Behavior>();
            Transitions = new List<Transition>();
            States = new Dictionary<int, State>();

            foreach (IBehavior bh in behaviors)
            {
#if DEBUG
                if (bh is Loot) throw new Exception("Loot should not be initialized in a substate.");
#endif
                if (bh is Behavior) Behaviors.Add(bh as Behavior);
                if (bh is Transition) Transitions.Add(bh as Transition);
                if (bh is State)
                {
                    State state = bh as State;
                    state.Parent = this;
                    States.Add(state.Id, state);
                }
            }
        }

        public void FindStateTransitions()
        {
            foreach (Transition transition in Transitions)
                ResolveTransition(Parent.States.Values, transition);

            foreach (State state in States.Values)
                state.FindStateTransitions();
        }

        public static void ResolveTransition(IEnumerable<State> states, Transition transition)
        {
            List<int> targets = new List<int>();
            foreach (string name in transition.StringTargetStates)
                foreach (State state in states)
                    if (state.StringId == name)
                    {
                        targets.Add(state.Id);
                        break;
                    }

            transition.TargetStates = targets.ToArray();
            if (transition.TargetStates.Length > 0 && transition.TargetState == 0)
                transition.TargetState = transition.TargetStates[transition.SelectedState];
#if DEBUG
            else if (transition.TargetStates.Length == 0)
                Program.Print(PrintType.Error, $"Unresolved transition target <{string.Join(",", transition.StringTargetStates)}>.");
#endif
        }
    }
}
