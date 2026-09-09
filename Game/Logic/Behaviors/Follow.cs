using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class FollowState
    {
        public FollowPhase Phase;
        public int RemainingTime;
    }

    public enum FollowPhase
    {
        DontKnowWhere,
        Acquired,
        Resting
    }

    public class Follow : Behavior
    {
        public readonly float Speed;
        public readonly float AcquireRange;
        public readonly float Range;
        public readonly int Duration;
        public readonly int Cooldown;

        public Follow(double speed, double acquireRange = 10, double range = 6, int duration = 0, int cooldown = 1000)
        {
            Speed = (float)speed;
            AcquireRange = (float)acquireRange;
            Range = (float)range;
            Duration = duration;
            Cooldown = duration == 0 ? 0 : cooldown;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new FollowState();
        }

        public override bool Tick(Entity host)
        {
            FollowState state = host.StateObject[Id] as FollowState;

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            Entity player = host.GetNearestPlayer(AcquireRange) ?? host.GetNearestEntity(AcquireRange);
            bool moving = false;

            switch (state.Phase)
            {
                case FollowPhase.DontKnowWhere:
                    if (player != null && state.RemainingTime <= 0)
                    {
                        state.Phase = FollowPhase.Acquired;
                        if (Duration > 0)
                            state.RemainingTime = Duration;
                        goto case FollowPhase.Acquired;
                    }
                    else if (state.RemainingTime > 0)
                        state.RemainingTime -= Settings.MillisecondsPerTick;
                    break;
                case FollowPhase.Acquired:
                    if (player == null)
                    {
                        state.Phase = FollowPhase.DontKnowWhere;
                        state.RemainingTime = 0;
                        break;
                    }
                    else if (state.RemainingTime <= 0 && Duration > 0)
                    {
                        state.Phase = FollowPhase.DontKnowWhere;
                        state.RemainingTime = Cooldown;
                        break;
                    }
                    if (state.RemainingTime > 0)
                        state.RemainingTime -= Settings.MillisecondsPerTick;

                    Position vect = player.Position - host.Position;
                    if (host.Position.Distance(player.Position) > Range)
                    {
                        vect.X -= MathUtils.NextInt(-2, 2) / 2f;
                        vect.Y -= MathUtils.NextInt(-2, 2) / 2f;
                        vect.Normalize();
                        float dist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
                        host.ValidateAndMove(host.Position + vect * dist);
                        moving = true;
                    }
                    else
                    {
                        state.Phase = FollowPhase.Resting;
                        state.RemainingTime = 0;
                    }
                    break;
                case FollowPhase.Resting:
                    if (player == null)
                    {
                        state.Phase = FollowPhase.DontKnowWhere;
                        if (Duration > 0)
                            state.RemainingTime = Duration;
                        break;
                    }
                    vect = player.Position - host.Position;
                    if (host.Position.Distance(player.Position) > Range + 1)
                    {
                        state.Phase = FollowPhase.Acquired;
                        state.RemainingTime = Duration;
                        goto case FollowPhase.Acquired;
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
