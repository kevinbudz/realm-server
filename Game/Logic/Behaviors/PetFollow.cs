using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class PetFollowState
    {
        public PetFollowPhase Phase;
        public int RemainingTime;
    }

    public enum PetFollowPhase
    {
        DontKnowWhere,
        Acquired
    }

    public class PetFollow : Behavior
    {
        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new PetFollowState
            {
                Phase = PetFollowPhase.DontKnowWhere,
                RemainingTime = 1000
            };
        }

        public override bool Tick(Entity host)
        {
            PetFollowState state = host.StateObject[Id] as PetFollowState;

            Entity player = host.GetNearestPlayer(Player.SightRadius);
            if (player == null)
                return false;

            bool moving = false;
            switch (state.Phase)
            {
                case PetFollowPhase.DontKnowWhere:
                    if (state.RemainingTime > 0)
                        state.RemainingTime -= Settings.MillisecondsPerTick;
                    else
                        state.Phase = PetFollowPhase.Acquired;
                    break;
                case PetFollowPhase.Acquired:
                    Position vect = player.Position - host.Position;
                    float length = host.Position.Distance(player.Position);
                    if (length > 20)
                    {
                        host.Parent.MoveEntity(host, player.Position);
                        moving = true;
                    }
                    else if (length > 1)
                    {
                        float dist = host.GetSpeed(0.5f) * Settings.SecondsPerTick;
                        if (length > 2)
                            dist = host.GetSpeed(0.7f) * Settings.SecondsPerTick;

                        vect.Normalize();
                        host.ValidateAndMove(host.Position + vect * dist);
                        moving = true;
                    }
                    break;
            }

            return moving;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
