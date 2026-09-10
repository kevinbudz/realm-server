using RotMG.Common;
using RotMG.Utils;
using System;

namespace RotMG.Game.Dungeons
{
    //Instanced dungeon built from a Worlds.xml entry. The map comes from
    //the entry's <Maps> file (.jm/.wmap, which already holds tiles, walls
    //and enemies); entries without one are built by their DungeonGen
    //template when the dungeon has one (see Game/DungeonGen). Entry
    //points: dungeon portals in the realm and locked-portal keys
    //(see Player AEUnlockPortal).
    public class DungeonWorld : World
    {
        public WorldDesc Desc;

        public DungeonWorld(WorldDesc desc, int seed)
            : base(BuildMap(desc, seed), desc)
        {
            Desc = desc;
        }

        //Enterable once a map file or generator template exists for it.
        public static bool IsSupported(WorldDesc desc)
        {
            if (desc == null)
                return false;
            if (desc.Maps != null && desc.Maps.Length > 0)
                return true;
            return DungeonGen.DungeonTemplates.GetTemplate(desc.Id) != null;
        }

        private static IGameMap BuildMap(WorldDesc desc, int seed)
        {
            if (desc.Maps != null && desc.Maps.Length > 0)
                return desc.Maps[MathUtils.Next(desc.Maps.Length)];
            DungeonGen.Templates.DungeonTemplate template = DungeonGen.DungeonTemplates.GetTemplate(desc.Id);
            if (template != null)
            {
                DungeonGen.Generator gen = new DungeonGen.Generator(seed, template);
                gen.Generate();
                DungeonGen.Rasterizer ras = new DungeonGen.Rasterizer(seed, gen.ExportGraph());
                ras.Rasterize();
                return DungeonGen.DungeonMapBuilder.BuildMap(ras.ExportMap());
            }
            throw new InvalidOperationException($"Dungeon '{desc.Id}' has no map or generator template.");
        }
    }
}
