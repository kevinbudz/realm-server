using RotMG.Common;
using System.Collections.Generic;

namespace RotMG.Game
{
    // Map abstraction shared by the .jm (JSMap) and .wmap (Wmap) formats.
    // .jm carries ground/obj/region only; .wmap additionally carries the
    // painted terrain + elevation channels from realm-src-master.
    public interface IGameMap
    {
        int Width { get; }
        int Height { get; }
        Dictionary<Region, List<IntPoint>> Regions { get; }

        ushort GetGroundType(int x, int y);
        ushort GetObjectType(int x, int y);
        Region GetRegion(int x, int y);
        string GetObjCfg(int x, int y);
        TerrainType GetTerrain(int x, int y);
        byte GetElevation(int x, int y);
        bool HasPaintedTerrain { get; }
    }
}
