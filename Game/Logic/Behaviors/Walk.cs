using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class WalkState
    {
        public float TimeLeft;
        public Position Target;
    }

    //Port of the reference WalkBehavior: picks a random point 3-6 tiles away and walks there for 10-20 s. A point is only
    //taken when it lies on one of GroundTypes (when that list is given). Ground names are resolved to tile ids once.
    public class Walk : Behavior
    {
        public readonly string GroundTypes;
        public readonly float Speed;

        private HashSet<ushort> _groundTypes;

        public Walk(string groundTypes, double speed = 0.4)
        {
            GroundTypes = groundTypes;
            Speed = (float)speed;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new WalkState();
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is WalkState state))
                return false;
            if (host.Parent == null || host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            float step = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            if (state.TimeLeft > 0)
            {
                state.TimeLeft -= Settings.MillisecondsPerTick;
                if (!BehaviorHelpers.MoveToward(host, state.Target, step))
                    state.TimeLeft = 0;
                return true;
            }

            float angle = MathUtils.NextAngle();
            float dist = MathUtils.NextFloat(3, 6);
            Position pos = host.Position + new Position(MathF.Cos(angle) * dist, MathF.Sin(angle) * dist);
            World world = host.Parent;
            if (pos.X < 0 || pos.Y < 0 || pos.X >= world.Width || pos.Y >= world.Height)
                return true;

            HashSet<ushort> allowed = Ground();
            if (allowed != null)
            {
                Tile? tile = world.GetTile((int)pos.X, (int)pos.Y);
                if (tile == null || !allowed.Contains(tile.Value.Type))
                    return true;
            }

            state.Target = pos;
            state.TimeLeft = 10000 + MathUtils.NextFloat(0, 10000);
            return true;
        }

        //Null when no ground list was given (any ground is walkable). Unknown names match nothing, as in the reference.
        private HashSet<ushort> Ground()
        {
            if (string.IsNullOrWhiteSpace(GroundTypes))
                return null;
            if (_groundTypes != null)
                return _groundTypes;

            _groundTypes = new HashSet<ushort>();
            foreach (string raw in GroundTypes.Split(','))
            {
                string name = raw.Trim();
                if (name.Length == 0)
                    continue;
                if (Resources.Id2Tile.TryGetValue(name, out TileDesc desc))
                    _groundTypes.Add(desc.Type);
                else
                {
#if DEBUG
                    Program.Print(PrintType.Error, $"Walk: unknown ground <{name}>.");
#endif
                }
            }
            return _groundTypes;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
