using RotMG.Common;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Behaviors
{
    public class WanderState
    {
        public float Angle;
        public float RemainingDistance;
        public Position Origin;
        public int Cooldown;
        public int LastMoveTick = -1;
    }

    public class Wander : Behavior
    {
        public readonly float Speed;
        private readonly float _distance;
        private readonly float _distanceFromSpawn;
        private readonly int _cooldownMs;

        public Wander(float speed, float distance = 1f, float distanceFromSpawn = 5f, int cooldownMs = 0)
        {
            Speed = speed;
            _distance = distance;
            _distanceFromSpawn = distanceFromSpawn;
            _cooldownMs = cooldownMs;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new WanderState { Origin = host.Position };
        }

        public override bool Tick(Entity host)
        {
            WanderState state = host.StateObject[Id] as WanderState;

            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            //Prioritize can tick this again in the same tick after a leg ended, never move twice in one tick.
            if (state.LastMoveTick == Manager.TotalTicks)
                return true;

            //Idle between legs, so other behaviors get to run.
            if (state.RemainingDistance <= 0 && state.Cooldown > 0)
            {
                state.LastMoveTick = Manager.TotalTicks;
                state.Cooldown -= Settings.MillisecondsPerTick;
                return false;
            }

            if (state.RemainingDistance <= 0)
            {
                state.Angle = PickAngle(host.Position, state.Origin);
                state.RemainingDistance = _distance;
            }

            float step = MathF.Min(host.GetSpeed(Speed) * Settings.SecondsPerTick, state.RemainingDistance);
            Position from = host.Position;
            host.ValidateAndMove(from + new Position(MathF.Cos(state.Angle) * step, MathF.Sin(state.Angle) * step));
            state.LastMoveTick = Manager.TotalTicks;

            state.RemainingDistance -= step;
            //Blocked by a wall or obstacle: end the leg early instead of pushing into it.
            if (host.Position.Distance(from) < step * 0.5f)
                state.RemainingDistance = 0;

            if (state.RemainingDistance <= 0)
            {
                state.Cooldown = _cooldownMs;
                return false;
            }
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject[Id] = null;
        }

        public override void Death(Entity host) { }

        //Angle for a leg of length _distance whose endpoint stays within _distanceFromSpawn of origin.
        private float PickAngle(Position pos, Position origin)
        {
            float d = pos.Distance(origin);
            if (d < 0.001f || _distance <= 0 || d + _distance <= _distanceFromSpawn)
                return MathUtils.NextAngle();

            float toOrigin = pos.Angle(origin);
            float cosHalf = (_distance * _distance + d * d - _distanceFromSpawn * _distanceFromSpawn) / (2 * _distance * d);
            if (cosHalf <= -1)
                return MathUtils.NextAngle();

            //Origin is out of reach for one leg: head back toward it.
            if (cosHalf >= 1)
                return toOrigin + MathUtils.NextFloat(-MathF.PI / 6, MathF.PI / 6);

            float half = MathF.Acos(cosHalf);
            return toOrigin + MathUtils.NextFloat(-half, half);
        }
    }
}
