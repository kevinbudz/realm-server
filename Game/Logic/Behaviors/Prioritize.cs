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
            //Mirrors realm-src Prioritize: once a child is running it keeps running
            //until it reports finished (false) instead of re-selecting every tick.
            int index = (int)host.StateObject[Id];
            if (index >= 0)
            {
                if (Behaviors[index].Tick(host))
                    return true;
                index = -1;
            }

            for (int k = 0; k < Behaviors.Length; k++)
                if (Behaviors[k].Tick(host))
                {
                    host.StateObject[Id] = k;
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
