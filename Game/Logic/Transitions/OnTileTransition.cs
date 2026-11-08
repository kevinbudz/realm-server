using RotMG.Common;
using System.Collections.Generic;

namespace RotMG.Game.Logic.Transitions
{
    //Old onTile: holds while the host stands on any of the listed ground types (comma-separated ground ids).
    public class OnTileTransition : Transition
    {
        public readonly string Grounds;

        //Resolved once on the first tick. Unknown ground ids are dropped; if none resolve, nothing matches.
        private ushort[] _types;

        public OnTileTransition(string grounds, string targetState) : base(targetState)
        {
            Grounds = grounds;
        }

        public override bool Tick(Entity host)
        {
            World world = host.Parent;
            if (world == null)
                return false;
            if (_types == null)
                _types = ResolveTypes(Grounds);
            if (_types.Length == 0)
                return false;

            Tile? tile = world.GetTile((int)host.Position.X, (int)host.Position.Y);
            if (tile == null)
                return false;

            foreach (ushort type in _types)
                if (tile.Value.Type == type)
                    return true;
            return false;
        }

        private static ushort[] ResolveTypes(string grounds)
        {
            List<ushort> types = new List<ushort>();
            if (string.IsNullOrWhiteSpace(grounds))
                return types.ToArray();

            foreach (string raw in grounds.Split(','))
            {
                string id = raw.Trim();
                if (id.Length == 0)
                    continue;
                if (Resources.Id2Tile.TryGetValue(id, out TileDesc desc))
                {
                    types.Add(desc.Type);
                    continue;
                }
#if DEBUG
                Program.Print(PrintType.Debug, $"OnTileTransition: unknown ground <{id}>, ignored.");
#endif
            }
            return types.ToArray();
        }
    }
}
