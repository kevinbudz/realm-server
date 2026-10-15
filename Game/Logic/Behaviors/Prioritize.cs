using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Behaviors
{
    public class Prioritize : Behavior
    {
        public readonly Behavior[] Behaviors;

        public Prioritize(params Behavior[] behaviors)
        {
            Behaviors = behaviors;
        }

        public override void Enter(Entity host)
        {
            for (int k = 0; k < Behaviors.Length; k++)
                Behaviors[k].Enter(host);
            host.StateObject[Id] = -1;
        }

        public override bool Tick(Entity host)
        {
            //Keep a running child selected until it stops or reports a natural
            //cycle boundary. Reconsider priorities on the next tick so a child
            //that moved on its completion tick is never run twice.
            int index = (int)host.StateObject[Id];
            if (index >= 0)
            {
                Behavior active = Behaviors[index];
                if (active.Tick(host))
                {
                    if (active.IsCycleComplete(host))
                        host.StateObject[Id] = -1;
                    return true;
                }
                index = -1;
            }

            for (int k = 0; k < Behaviors.Length; k++)
                if (Behaviors[k].Tick(host))
                {
                    host.StateObject[Id] = Behaviors[k].IsCycleComplete(host) ? -1 : k;
                    return true;
                }

            host.StateObject[Id] = -1;
            return false;
        }

        public override void Exit(Entity host)
        {
            for (int k = 0; k < Behaviors.Length; k++)
                Behaviors[k].Exit(host);
            host.StateObject.Remove(Id);
        }

        public override void Death(Entity host)
        {
            for (int k = 0; k < Behaviors.Length; k++)
                Behaviors[k].Death(host);
        }
    }
}
