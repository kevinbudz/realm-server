//Dungeon-generator support written for this project. The generation engine
//it hosts is a port of realm-src-master's DungeonGen (see note on the
//ported files); this file itself is original.
using RotMG.Game.DungeonGen.Templates;
using RotMG.Game.DungeonGen.Templates.Abyss;
using RotMG.Game.DungeonGen.Templates.Lab;
using RotMG.Game.DungeonGen.Templates.PirateCave;

namespace RotMG.Game.DungeonGen
{
    //Maps dungeon names onto reference generator templates. The reference
    //only ships three templates (see its DungeonTemplates, which reflects
    //over "{world}Template" types); dungeons without a template keep the
    //local room-and-corridor generator (see Game/Dungeons).
    public static class DungeonTemplates
    {
        public static DungeonTemplate GetTemplate(string dungeonName)
        {
            switch (dungeonName)
            {
                case "Abyss of Demons": return new AbyssTemplate();
                case "Mad Lab": return new LabTemplate();
                case "Pirate Cave": return new PirateCaveTemplate();
                default: return null;
            }
        }
    }
}
