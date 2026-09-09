//Dungeon-generator support written for this project. The generation engine
//it hosts is a port of realm-src-master's DungeonGen (see note on the
//ported files); this file itself is original.
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen
{
    //Converts generator output into the world's JSMap model: ground/object
    //names resolve against GameData (unknowns become empty, same as .jm
    //map loads), regions parse by name, and object attributes become the
    //spawn-config string the world applies to entities.
    public static class DungeonMapBuilder
    {
        public static JSMap BuildMap(DungeonTile[,] tiles)
        {
            int w = tiles.GetLength(0);
            int h = tiles.GetLength(1);
            JSMap map = new JSMap(w, h);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    DungeonTile src = tiles[x, y];
                    JSTile tile = map.Tiles[x, y];
                    tile.GroundType = JSMap.ResolveGround(src.TileType.Name);
                    if (src.Object != null)
                    {
                        tile.ObjectType = JSMap.ResolveObject(src.Object.ObjectType.Name);
                        tile.Key = src.Object.ToCfgString();
                    }
                    else
                    {
                        tile.ObjectType = 0xff;
                        tile.Key = null;
                    }
                    tile.Region = JSMap.ParseRegion(src.Region);
                    map.Tiles[x, y] = tile;
                }
            map.InitRegions();
            return map;
        }
    }
}
