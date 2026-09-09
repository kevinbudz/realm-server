using System;

namespace RotMG.Game
{
    // Difficulty-biome classification for realm mob seeding, mirroring the
    // TerrainType/RegionMobs design of realm-src-master wServer/realm/Oryx.cs.
    // The .jm map format carries no painted terrain channel, so terrain is
    // derived per tile: the ground tile id selects the biome
    // (Sand/Plains/Forest/Mountains) and distance from the realm spawn
    // selects the tier (Shore/Low/Mid/High).
    public enum TerrainType
    {
        None,
        ShoreSand,
        ShorePlains,
        LowSand,
        LowPlains,
        LowForest,
        MidSand,
        MidPlains,
        MidForest,
        HighSand,
        HighPlains,
        HighForest,
        Mountains
    }

    public static class TerrainClassifier
    {
        // Tier radii preserved from the old distance-band seeding.
        private const float ShoreRadius = 200;
        private const float LowRadius = 600;
        private const float MidRadius = 1200;

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
