//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zlib;
using Newtonsoft.Json;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen
{
    //Map-fragment codec for the generator's embedded template.jm resources.
    //Same .jm layout as the world's JSMap (dict + zlib tile indices); the
    //only difference is that tiles keep their string ids/regions/attributes
    //so templates resolve against GameData at dungeon build time.
    public static class JsonMap
    {
        private struct TileComparer : IEqualityComparer<DungeonTile>
        {
            public bool Equals(DungeonTile x, DungeonTile y)
            {
                return x.TileType == y.TileType && x.Region == y.Region && x.Object == y.Object;
            }

            public int GetHashCode(DungeonTile obj)
            {
                int code = (int)obj.TileType.Id;
                if (obj.Region != null)
                    code = code * 7 + obj.Region.GetHashCode();
                if (obj.Object != null)
                    code = code * 13 + obj.Object.GetHashCode();
                return code;
            }
        }

        private struct JsonDat
        {
            public byte[] data { get; set; }
            public JsonLoc[] dict { get; set; }
            public int height { get; set; }
            public int width { get; set; }
        }

        private struct JsonLoc
        {
            public string ground { get; set; }
            public JsonObj[] objs { get; set; }
            public JsonObj[] regions { get; set; }
        }

        private struct JsonObj
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public static DungeonTile[,] Load(string json)
        {
            JsonDat map = JsonConvert.DeserializeObject<JsonDat>(json);
            int w = map.width, h = map.height;
            DungeonTile[,] result = new DungeonTile[w, h];

            Dictionary<ushort, DungeonTile> tiles = new Dictionary<ushort, DungeonTile>();
            ushort id = 0;
            foreach (JsonLoc tile in map.dict)
            {
                DungeonTile mapTile = new DungeonTile();
                string tileType = tile.ground ?? "Space";
                mapTile.TileType = new TileType(tileType == "Space" ? 0xfeu : 0u, tileType);

                mapTile.Region = tile.regions != null && tile.regions.Length > 0 ? tile.regions[0].id : null;
                if (tile.objs != null && tile.objs.Length > 0)
                {
                    JsonObj obj = tile.objs[0];
                    DungeonObject tileObj = new DungeonObject();
                    tileObj.ObjectType = new ObjectType(0, obj.id);
                    if (!string.IsNullOrEmpty(obj.name))
                    {
                        tileObj.Attributes = obj.name
                            .Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(attr => attr.Split(':'))
                            .Where(attr => attr.Length >= 2)
                            .Select(attr => new KeyValuePair<string, string>(attr[0], attr[1]))
                            .ToArray();
                    }
                    else
                        tileObj.Attributes = Empty<KeyValuePair<string, string>>.Array;

                    mapTile.Object = tileObj;
                }
                else
                    mapTile.Object = null;
                tiles[id++] = mapTile;
            }

            byte[] data = ZlibStream.UncompressBuffer(map.data);
            int index = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    result[x, y] = tiles[(ushort)((data[index++] << 8) | data[index++])];
                }

            return result;
        }

        public static string Save(DungeonTile[,] map)
        {
            int w = map.GetUpperBound(0) + 1, h = map.GetUpperBound(1) + 1;

            List<object> tiles = new List<object>();
            Dictionary<DungeonTile, short> indexLookup = new Dictionary<DungeonTile, short>(new TileComparer());
            byte[] data = new byte[w * h * 2];
            int ptr = 0;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    DungeonTile tile = map[x, y];
                    short index;
                    if (!indexLookup.TryGetValue(tile, out index))
                    {
                        indexLookup.Add(tile, index = (short)tiles.Count);
                        tiles.Add(tile);
                    }
                    data[ptr++] = (byte)(index >> 8);
                    data[ptr++] = (byte)(index & 0xff);
                }

            List<object> dict = new List<object>();
            for (int i = 0; i < tiles.Count; i++)
            {
                DungeonTile tile = (DungeonTile)tiles[i];

                Dictionary<string, object> jsonTile = new Dictionary<string, object>();
                jsonTile["ground"] = tile.TileType.Name;
                if (!string.IsNullOrEmpty(tile.Region))
                {
                    jsonTile["regions"] = new object[]
                    {
                        new Dictionary<string, object> { { "id", tile.Region } }
                    };
                }
                if (tile.Object != null)
                {
                    Dictionary<string, object> obj = new Dictionary<string, object>
                    {
                        { "id", tile.Object.ObjectType.Name }
                    };
                    if (tile.Object.Attributes.Length > 0)
                    {
                        string[] objAttrs = tile.Object.Attributes
                            .Select(kvp => kvp.Key + ":" + kvp.Value).ToArray();
                        obj["name"] = string.Join(";", objAttrs) + ";";
                    }
                    jsonTile["objs"] = new object[] { obj };
                }

                dict.Add(jsonTile);
            }

            Dictionary<string, object> mapObj = new Dictionary<string, object>();
            mapObj["width"] = w;
            mapObj["height"] = h;
            mapObj["dict"] = dict;
            mapObj["data"] = Convert.ToBase64String(ZlibStream.CompressBuffer(data));

            return JsonConvert.SerializeObject(mapObj);
        }
    }
}
