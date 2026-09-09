using RotMG.Common;
using System;
using System.Collections.Generic;

namespace RotMG.Game
{
    // Difficulty-biome classification for realm mob seeding, mirroring the
    // TerrainType/RegionMobs design of realm-src-master wServer/realm/Oryx.cs
    // (enum order kept identical so tier-band checks port verbatim).
    // .wmap maps carry the painted terrain channel and it is used verbatim;
    // the .jm format has none, so terrain is derived per tile instead: the
    // ground tile id selects the biome (Sand/Plains/Forest/Mountains) and
    // distance from the nearest realm spawn selects the tier
    // (Shore/Low/Mid/High).
    public enum TerrainType
    {
        None,
        Mountains,
        HighSand,
        HighPlains,
        HighForest,
        MidSand,
        MidPlains,
        MidForest,
        LowSand,
        LowPlains,
        LowForest,
        ShoreSand,
        ShorePlains,
        BeachTowels
    }

    public static class TerrainClassifier
    {
        // Tier radii preserved from the old distance-band seeding.
        private const float ShoreRadius = 200;
        private const float LowRadius = 600;
        private const float MidRadius = 1200;

        // Single entry point for tile terrain: painted .wmap terrain wins
        // verbatim (as in the reference, which reads Map[x, y].Terrain
        // directly); .jm maps fall back to the derived bands below.
        public static TerrainType GetTileTerrain(IGameMap map, string groundId, int x, int y, List<IntPoint> spawns)
        {
            if (map.HasPaintedTerrain)
                return map.GetTerrain(x, y);
            return GetRealmTerrain(groundId, x, y, spawns);
        }

        // .jm fallback: the realm map has one Spawn beachhead per island, so
        // the tier is measured from the NEAREST spawn. A single-spawn
        // distance mislabels every other island's beaches as mid/high tier,
        // seeding e.g. MidPlains mobs onto the shore. Callers must pass the
        // full Spawn region list (non-empty; they fall back to the map center).
        public static TerrainType GetRealmTerrain(string groundId, int x, int y, List<IntPoint> spawns)
        {
            float best = float.MaxValue;
            foreach (IntPoint spawn in spawns)
            {
                float dx = x - spawn.X;
                float dy = y - spawn.Y;
                float d = dx * dx + dy * dy;
                if (d < best)
                    best = d;
            }
            return GetTerrain(groundId, (float)Math.Sqrt(best));
        }

        public static TerrainType GetTerrain(string groundId, float distFromSpawn)
        {
            string biome = GetBiome(groundId);
            if (biome == null)
                return TerrainType.None;
            if (biome == "Mountains")
                return TerrainType.Mountains;

            // Explicit mapping: not every tier x biome combo exists (there is
            // no ShoreForest, mirroring the reference table), so shore forest
            // folds into ShorePlains rather than throwing.
            if (distFromSpawn < ShoreRadius)
                return biome == "Sand" ? TerrainType.ShoreSand : TerrainType.ShorePlains;
            if (distFromSpawn < LowRadius)
                return biome == "Sand" ? TerrainType.LowSand
                    : biome == "Forest" ? TerrainType.LowForest : TerrainType.LowPlains;
            if (distFromSpawn < MidRadius)
                return biome == "Sand" ? TerrainType.MidSand
                    : biome == "Forest" ? TerrainType.MidForest : TerrainType.MidPlains;
            return biome == "Sand" ? TerrainType.HighSand
                : biome == "Forest" ? TerrainType.HighForest : TerrainType.HighPlains;
        }

        private static string GetBiome(string groundId)
        {
            if (string.IsNullOrEmpty(groundId))
                return null;
            if (groundId.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;
            if (groundId.IndexOf("sand", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Sand";
            if (groundId.IndexOf("rock", StringComparison.OrdinalIgnoreCase) >= 0 ||
                groundId.IndexOf("mountain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                groundId.IndexOf("lava", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Mountains";
            if (groundId.IndexOf("forest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                groundId.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0 ||
                groundId.Equals("Dark Grass", StringComparison.Ordinal) ||
                groundId.Equals("Blue Grass", StringComparison.Ordinal))
                return "Forest";
            return "Plains";
        }
    }
}
