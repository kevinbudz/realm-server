using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class StayInTerrainState
    {
        public bool Engaged;
        public int LookRadius;
        public int TimeLeft;
        public int DestX;
        public int DestY;
    }

    //Port of the reference StayInTerrainBehavior. The C# map has no water or biome bitmask, so the terrain names are
    //matched against ground tile ids (the reference XML uses ground names such as "Dark Water"). Altitude bounds are
    //not ported. The nearest matching tile within the search radius is the destination.
    public class StayInTerrain : Behavior
    {
        private const int LookInterval = 20000;
        private const int InitialLookRadius = 5;
        private const int LookIncrement = 3;

        public readonly string Terrain;
        public readonly float Speed;

        private HashSet<ushort> _groundTypes;

        public StayInTerrain(string terrain, double speed = 0.5)
        {
            Terrain = terrain;
            Speed = (float)speed;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new StayInTerrainState { LookRadius = InitialLookRadius };
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is StayInTerrainState state))
                return false;
            HashSet<ushort> ground = GroundTypes();
            if (ground.Count == 0 || host.Parent == null || host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            state.TimeLeft -= Settings.MillisecondsPerTick;
            if (!state.Engaged && state.TimeLeft > 0)
                return false;

            if (state.TimeLeft <= 0)
            {
                state.TimeLeft = LookInterval;
                if (IsAcceptable(host.Parent, ground, (int)host.Position.X, (int)host.Position.Y))
                {
                    //Back on acceptable ground: go idle and reset the search radius.
                    state.LookRadius = InitialLookRadius;
                    state.Engaged = false;
                    return false;
                }
                state.Engaged = true;
                FindBestPoint(host.Parent, ground, host.Position, state);
            }

            if (!state.Engaged)
                return false;

            int maxRadius = Math.Max(host.Parent.Width, host.Parent.Height);
            if ((int)host.Position.X == state.DestX && (int)host.Position.Y == state.DestY)
            {
                //At the destination without acceptable ground in reach: widen the search (reference lookRadius_ += lookIncrement_).
                //Once the whole map is covered, wait a full interval instead of rescanning every tick.
                if (state.LookRadius >= maxRadius)
                    state.TimeLeft = LookInterval;
                else
                {
                    state.LookRadius = Math.Min(state.LookRadius + LookIncrement, maxRadius);
                    state.TimeLeft = 0;
                }
                return false;
            }

            float step = host.GetSpeed(Speed) * Settings.SecondsPerTick;
            return BehaviorHelpers.MoveToward(host, new Position(state.DestX + 0.5f, state.DestY + 0.5f), step);
        }

        private HashSet<ushort> GroundTypes()
        {
            if (_groundTypes != null)
                return _groundTypes;

            _groundTypes = new HashSet<ushort>();
            if (string.IsNullOrWhiteSpace(Terrain))
                return _groundTypes;
            foreach (string raw in Terrain.Split(','))
            {
                string name = raw.Trim();
                if (name.Length == 0)
                    continue;
                if (Resources.Id2Tile.TryGetValue(name, out TileDesc desc))
                    _groundTypes.Add(desc.Type);
                else
                {
#if DEBUG
                    Program.Print(PrintType.Error, $"StayInTerrain: unknown ground <{name}>.");
#endif
                }
            }
            return _groundTypes;
        }

        private static bool IsAcceptable(World world, HashSet<ushort> ground, int x, int y)
        {
            Tile? tile = world.GetTile(x, y);
            return tile != null && ground.Contains(tile.Value.Type);
        }

        private static void FindBestPoint(World world, HashSet<ushort> ground, Position at, StayInTerrainState state)
        {
            int cx = (int)at.X;
            int cy = (int)at.Y;
            int r = state.LookRadius;
            int bestX = cx;
            int bestY = cy;
            float bestDist = float.MaxValue;
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (!IsAcceptable(world, ground, x, y))
                        continue;
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = dx * dx + dy * dy;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestX = x;
                        bestY = y;
                    }
                }
            state.DestX = bestX;
            state.DestY = bestY;
        }
    }
}
