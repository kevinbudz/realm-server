using System;
using System.Collections.Generic;
using System.IO;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Game.Setpieces;

namespace RotMG.Game.Logic.Behaviors
{
    public class PlaceMapState
    {
        public int PlaceLeft;
        public int CooldownLeft;
    }

    //Port of the reference PlaceMapBehavior. MapPath is relative to the setpiece folder (Resources/Worlds/Setpieces), the
    //only folder SetPieces.RenderSubMap can project from. The map is centered on the host, like addToMapCenter.
    //MaxPlace limits placements per entity (-1 = unlimited). The cooldown applies only when more than one placement
    //is allowed. A missing file is logged once in DEBUG and nothing is placed.
    public class PlaceMap : Behavior
    {
        private static readonly HashSet<string> Reported = new HashSet<string>();

        public readonly string MapPath;
        public readonly int MaxPlace;
        public readonly int Cooldown;

        private readonly int _places;
        private readonly int _cooldown;

        public PlaceMap(string map, int maxPlace = 1, int cooldown = 5000)
        {
            MapPath = map;
            MaxPlace = maxPlace;
            Cooldown = cooldown;

            //Reference normalization: only multi-placement maps use the cooldown, and a non-positive cooldown means one placement.
            _cooldown = maxPlace > 1 || maxPlace < 0 ? cooldown : 0;
            _places = maxPlace;
            if (_cooldown <= 0)
            {
                _places = 1;
                _cooldown = 0;
            }
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new PlaceMapState { PlaceLeft = _places };
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is PlaceMapState state))
                return false;
            if (state.PlaceLeft == 0 || host.Parent == null)
                return false;

            if (_cooldown != 0)
            {
                if (state.CooldownLeft > 0)
                {
                    state.CooldownLeft -= Settings.MillisecondsPerTick;
                    return false;
                }
                state.CooldownLeft = _cooldown;
            }

            if (!Place(host))
            {
                //Closed for this entity, as in the reference.
                state.PlaceLeft = 0;
                return false;
            }

            if (state.PlaceLeft > 0)
                state.PlaceLeft--;
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }

        private bool Place(Entity host)
        {
            if (string.IsNullOrWhiteSpace(MapPath) || !File.Exists(Resources.CombineResourcePath($"Worlds/Setpieces/{MapPath}")))
            {
                Report(MapPath);
                return false;
            }

            JSMap map = SetPieces.GetSubMap(MapPath);
            IntPoint topLeft = new IntPoint(
                (int)Math.Floor(host.Position.X) - map.Width / 2,
                (int)Math.Floor(host.Position.Y) - map.Height / 2);
            SetPieces.RenderSubMap(host.Parent, topLeft, MapPath);
            return true;
        }

        private static void Report(string map)
        {
#if DEBUG
            bool first;
            lock (Reported)
                first = Reported.Add(map ?? string.Empty);
            if (first)
                Program.Print(PrintType.Debug, $"PlaceMap: <{map}> not found under Worlds/Setpieces; nothing placed.");
#endif
        }
    }
}
