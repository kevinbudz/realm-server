using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class ProtectState
    {
        public ProtectPhase Phase;
    }

    public enum ProtectPhase
    {
        DontKnowWhere,
        Protecting,
        Protected
    }

    public class Protect : Behavior
    {
        public readonly float Speed;
        public readonly string Protectee;
        public readonly float AcquireRange;
        public readonly float ProtectionRange;
        public readonly float ReprotectRange;

        public Protect(double speed, string protectee, double acquireRange = 10, double protectionRange = 2, double reprotectRange = 1)
        {
            Speed = (float)speed;
            Protectee = protectee;
            AcquireRange = (float)acquireRange;
            ProtectionRange = (float)protectionRange;
            ReprotectRange = (float)reprotectRange;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new ProtectState();
        }

        public override bool Tick(Entity host)
        {
            ProtectState state = host.StateObject[Id] as ProtectState;

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            Entity entity = BehaviorHelpers.NearestEntityByName(host, AcquireRange, Protectee);
            bool moving = false;

            switch (state.Phase)
            {
                case ProtectPhase.DontKnowWhere:
                    if (entity != null)
                    {
                        state.Phase = ProtectPhase.Protecting;
                        goto case ProtectPhase.Protecting;
                    }
                    break;
                case ProtectPhase.Protecting:
                    if (entity == null)
                    {
                        state.Phase = ProtectPhase.DontKnowWhere;
                        break;
                    }
                    Position vect = entity.Position - host.Position;
                    if (host.Position.Distance(entity.Position) > ReprotectRange)
                    {
                        vect.Normalize();
                        float dist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
                        host.ValidateAndMove(host.Position + vect * dist);
                        moving = true;
                    }
                    else
                        state.Phase = ProtectPhase.Protected;
                    break;
                case ProtectPhase.Protected:
                    if (entity == null)
                    {
                        state.Phase = ProtectPhase.DontKnowWhere;
                        break;
                    }
                    vect = entity.Position - host.Position;
                    if (host.Position.Distance(entity.Position) > ProtectionRange)
                    {
                        state.Phase = ProtectPhase.Protecting;
                        goto case ProtectPhase.Protecting;
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
