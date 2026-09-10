using Ionic.Zlib;
using RotMG.Common;
using RotMG.Game.Entities;
using System;
using System.Collections.Generic;
using System.IO;

namespace RotMG.Game
{
    //Value type by design: every .wmap map retains one of these per tile
    //(4.2M in a 2048x2048 Realm, ~12.6M across the three template maps),
    //so a class costs that many long-lived heap objects. Instances are
    //fully assigned during load and read-only afterwards (see the getters
    //below), which is exactly the safe shape for a struct.
    public struct WmapTile
    {
        public ushort TileType;
        public ushort ObjType;
        public string ObjCfg;
        public TerrainType Terrain;
        public Region Region;
        public byte Elevation;
    }

    // Binary .wmap loader, ported from realm-src-master
    // wServer/realm/terrain/Wmap.cs:Load(Stream, int). Object ids are not
    // preserved (this codebase assigns ids in World.AddEntity); everything
    // else — tile grounds, objects + cfgs, painted terrain, regions,
    // elevation — loads verbatim, v1 (elevation in dict) and v2 (per tile).
    public class Wmap : IGameMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Dictionary<Region, List<IntPoint>> Regions { get; private set; }

        private WmapTile[,] _tiles;

        public Wmap(byte[] data)
        {
            int ver = data[0];
            if (ver > 2)
                throw new NotSupportedException("WMap version " + ver);

            using (MemoryStream input = new MemoryStream(data, 1, data.Length - 1))
            using (BinaryReader rdr = new BinaryReader(new ZlibStream(input, CompressionMode.Decompress)))
            {
                int count = rdr.ReadInt16();
                WmapTile[] dict = new WmapTile[count];
                for (int i = 0; i < count; i++)
                {
                    WmapTile desc = new WmapTile();
                    desc.TileType = rdr.ReadUInt16();
                    if (!Resources.Type2Tile.ContainsKey(desc.TileType))
                    {
                        Program.Print(PrintType.Warn, $"Unknown wmap ground type <{desc.TileType}>, treating as <none>.");
                        desc.TileType = 255;
                    }
                    string obj = rdr.ReadString();
                    if (string.IsNullOrEmpty(obj))
                        desc.ObjType = 255;
                    else if (Resources.Id2Object.TryGetValue(obj, out ObjectDesc objDesc))
                        desc.ObjType = objDesc.Type;
                    else
                    {
                        Program.Print(PrintType.Warn, $"Unknown wmap object <{obj}>, skipping.");
                        desc.ObjType = 255;
                    }
                    desc.ObjCfg = rdr.ReadString();
                    desc.Terrain = (TerrainType)rdr.ReadByte();
                    desc.Region = MapRegion(rdr.ReadByte());
                    if (ver == 1)
                        desc.Elevation = rdr.ReadByte();
                    dict[i] = desc;
                }

                Width = rdr.ReadInt32();
                Height = rdr.ReadInt32();
                _tiles = new WmapTile[Width, Height];
                Regions = new Dictionary<Region, List<IntPoint>>();
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        short idx = rdr.ReadInt16();
                        WmapTile src = dict[idx];
                        WmapTile tile = _tiles[x, y] = new WmapTile()
                        {
                            TileType = src.TileType,
                            ObjType = src.ObjType,
                            ObjCfg = src.ObjCfg,
                            Terrain = src.Terrain,
                            Region = src.Region,
                            Elevation = ver == 2 ? rdr.ReadByte() : src.Elevation
                        };
                        if (tile.Region != Region.None)
                        {
                            if (!Regions.TryGetValue(tile.Region, out List<IntPoint> list))
                                Regions[tile.Region] = list = new List<IntPoint>();
                            list.Add(new IntPoint(x, y));
                        }
                    }
            }
        }

        // Only the regions overlapping this codebase's Region enum survive;
        // the rest (dungeon-oriented TileRegions like Hallway/Loot/Enemy and
        // Store_7+, none of which occur in the shipped realm maps) are
        // dropped with a warning instead of crashing the world load.
        private static Region MapRegion(byte region)
        {
            switch (region)
            {
                case 1: return Region.Spawn;
                case 2: return Region.Realm_Portals;
                case 3: return Region.Store_1;
                case 4: return Region.Store_2;
                case 5: return Region.Store_3;
                case 6: return Region.Store_4;
                case 7: return Region.Store_5;
                case 8: return Region.Store_6;
                case 9: return Region.Vault;
                case 0: return Region.None;
                default:
                    Program.Print(PrintType.Warn, $"Unknown wmap region <{region}>, treating as <None>.");
                    return Region.None;
            }
        }

        // Map-painted spawn stats, mirroring the cfg switch in reference
        // Wmap.InstantiateEntities. Only hp/size/eff map onto members this
        // codebase has (conn: is rendered live by ConnectedObject; entity
        // names, merchant fields and offsets do not occur in shipped maps).
        public static void ApplyObjCfg(Entity entity, string cfg)
        {
            if (entity == null || string.IsNullOrEmpty(cfg))
                return;
            foreach (string part in cfg.Split(';'))
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                string[] kv = part.Split(':');
                if (kv.Length < 2)
                    continue;
                switch (kv[0])
                {
                    case "size":
                        if (int.TryParse(kv[1], out int size))
                            entity.Size = Math.Min(500, size);
                        break;
                    case "hp":
                        if (entity is Enemy && int.TryParse(kv[1], out int hp))
                            entity.HP = entity.MaxHP = hp;
                        break;
                    case "eff":
                        if (ulong.TryParse(kv[1], out ulong eff))
                            entity.ConditionEffects = (ConditionEffects)eff;
                        break;
                }
            }
        }

        public ushort GetGroundType(int x, int y) => _tiles[x, y].TileType;
        public ushort GetObjectType(int x, int y) => _tiles[x, y].ObjType;
        public Region GetRegion(int x, int y) => _tiles[x, y].Region;
        public string GetObjCfg(int x, int y) => _tiles[x, y].ObjCfg;
        public TerrainType GetTerrain(int x, int y) => _tiles[x, y].Terrain;
        public byte GetElevation(int x, int y) => _tiles[x, y].Elevation;
        public bool HasPaintedTerrain => true;
    }
}
