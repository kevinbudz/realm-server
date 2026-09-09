using System.Collections.Generic;

namespace RotMG.Game.Dungeons
{
    //Per-dungeon theme and roster. Mob names must have behaviors
    //(BehaviorDb) or they are skipped at spawn; the boss falls back to the
    //first quest-flagged roster entry when Boss is unknown.
    public class DungeonDef
    {
        public string Name;
        public string PortalObject;
        public string[] Grounds;
        public string RockGround;
        public string[] Walls;
        public string[] Mobs;
        public string Boss;
        public string MapFile;
        public int Width;
        public int Height;
        public int Rooms;
    }

    public static class DungeonDefs
    {
        public static readonly List<DungeonDef> All = new List<DungeonDef>
        {
            new DungeonDef
            {
                Name = "Snake Pit", PortalObject = "Snake Pit Portal",
                Grounds = new[] { "Dirt", "Grass" }, RockGround = "Dirt",
                Walls = new[] { "Brown Wall" },
                Mobs = new[] { "Pit Snake", "Pit Viper", "Greater Pit Snake", "Greater Pit Viper", "Fire Python", "Brown Python" },
                Boss = "Stheno the Snake Queen",
                Width = 64, Height = 64, Rooms = 8
            },
            new DungeonDef
            {
                Name = "Spider Den", PortalObject = "Spider Den Portal",
                Grounds = new[] { "Dirt", "Dark Grass" }, RockGround = "Dirt",
                Walls = new[] { "Brown Wall" },
                Mobs = new[] { "Black Den Spider", "Brown Den Spider", "Black Spotted Den Spider", "Green Den Spider Hatchling", "Arachna Summoner" },
                Boss = "Arachna the Spider Queen",
                MapFile = "Dungeons/Spider Den.jm",
                Width = 64, Height = 64, Rooms = 8
            },
            new DungeonDef
            {
                Name = "Undead Lair", PortalObject = "Undead Lair Portal",
                Grounds = new[] { "Gothic Floor Slabs", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Grey Wall", "Brown Wall" },
                Mobs = new[] { "Lair Skeleton", "Lair Ghost", "Lair Ghost Bat", "Lair Ghost Mage", "Lair Ghost Warrior", "Lair Ghost Knight", "Lair Mummy", "Lair Reaper", "Lair Brown Bat" },
                Boss = "Septavius the Ghost God",
                MapFile = "Dungeons/Undead Lair.jm",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Abyss of Demons", PortalObject = "Abyss of Demons Portal",
                Grounds = new[] { "Cracked Purple Stone", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Red Wall", "Brown Wall" },
                Mobs = new[] { "Imp", "Demon", "Red Demon", "White Demon" },
                Boss = "Archdemon Malphas",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Tomb of the Ancients", PortalObject = "Tomb of the Ancients Portal",
                Grounds = new[] { "Gold Sand", "Sand" }, RockGround = "Sand",
                Walls = new[] { "Tomb Wall" },
                Mobs = new[] { "Beam Priest", "Beam Priestess", "Tomb Defender", "Tomb Support", "Active Sarcophagus" },
                Boss = "Tomb Attacker",
                MapFile = "Dungeons/Tomb of the Ancients.jm",
                Width = 80, Height = 80, Rooms = 10
            },
            new DungeonDef
            {
                Name = "Sprite World", PortalObject = "Glowing Portal",
                Grounds = new[] { "Bright Grass", "Grass" }, RockGround = "Grass",
                Walls = new[] { "Blue Wall", "Brown Wall" },
                Mobs = new[] { "Native Fire Sprite", "Native Ice Sprite", "Native Magic Sprite", "Native Nature Sprite", "Native Darkness Sprite", "Fire Sprite", "Ice Sprite", "Magic Sprite", "Native Sprite God" },
                Boss = "Limon the Sprite God",
                MapFile = "Dungeons/Sprite World.jm",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Mad Lab", PortalObject = "Mad Lab Portal",
                Grounds = new[] { "Lab Floor", "Lab Grate Floor" }, RockGround = "Lab Floor",
                Walls = new[] { "Blue Wall" },
                Mobs = new[] { "Escaped Experiment", "Mini Bot", "Rampage Cyborg", "Crusher Abomination", "Tesla Coil", "Monster Cage" },
                Boss = "Dr Terrible",
                Width = 64, Height = 64, Rooms = 8
            },
            new DungeonDef
            {
                Name = "Ocean Trench", PortalObject = "Ocean Trench Portal",
                Grounds = new[] { "Sand", "Dirt" }, RockGround = "Sand",
                Walls = new[] { "Brown Wall", "Blue Wall" },
                Mobs = new[] { "Sea Horse", "Sea Mare", "Fishman Warrior", "Deep Sea Beast", "Giant Squid", "Grey Sea Slurp" },
                Boss = "Thessal the Mermaid Goddess",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Candyland Hunting Grounds", PortalObject = "Candyland Portal",
                Grounds = new[] { "Candy Grass", "Candy Grass Dark" }, RockGround = "Candy Grass Dark",
                Walls = new[] { "Candy Choc Column Whole", "Brown Wall" },
                Mobs = new[] { "Candy Gnome", "Red Gumball", "Blue Gumball", "Green Gumball", "Purple Gumball", "Yellow Gumball", "Big Creampuff", "Small Creampuff", "Spoiled Creampuff", "Beefy Fairy", "Swoll Fairy", "Desire Troll", "Tiny Rototo", "Rototo", "Gumball Machine" },
                Boss = "MegaRototo",
                MapFile = "Dungeons/Candyland Hunting Grounds.jm",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Cave of A Thousand Treasures", PortalObject = "Treasure Cave Portal",
                Grounds = new[] { "Gold Sand", "Sand" }, RockGround = "Sand",
                Walls = new[] { "Brown Wall" },
                Mobs = new[] { "Treasure Oryx Defender", "Gold Planet" },
                Boss = "Golden Oryx Effigy",
                MapFile = "Dungeons/Cave of a Thousand Treasures.jm",
                Width = 64, Height = 64, Rooms = 7
            },
            new DungeonDef
            {
                Name = "Beachzone", PortalObject = "Beachzone Portal",
                Grounds = new[] { "Sand", "Gold Sand" }, RockGround = "Sand",
                Walls = new[] { "Brown Wall" },
                Mobs = new[] { "Bahama Sunrise", "Blue Paradise", "Lime Jungle Bay", "Pink Passion Breeze" },
                Boss = "Masked Party God",
                MapFile = "Dungeons/Beachzone.jm",
                Width = 64, Height = 64, Rooms = 7
            },
            new DungeonDef
            {
                Name = "Pirate Cave", PortalObject = "Pirate Cave Portal",
                Grounds = new[] { "Sand", "Dirt" }, RockGround = "Sand",
                Walls = new[] { "Brown Wall" },
                Mobs = new[] { "Cave Pirate Brawler", "Cave Pirate Sailor", "Cave Pirate Veteran", "Pirate Lieutenant", "Pirate Commander" },
                Boss = "Dreadstump the Pirate King",
                Width = 64, Height = 64, Rooms = 8
            },
            new DungeonDef
            {
                Name = "Manor of the Immortals", PortalObject = "Manor of the Immortals Portal",
                Grounds = new[] { "Gothic Floor Slabs", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Grey Wall", "Brown Wall" },
                Mobs = new[] { "Vampire Bat", "Coffin Creature", "Nosferatu", "Armor Guard", "Hellhound", "Lesser Bald Vampire" },
                Boss = "Lord Ruthven",
                MapFile = "Dungeons/Manor of the Immortals.jm",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Forbidden Jungle", PortalObject = "Forbidden Jungle Portal",
                Grounds = new[] { "Grass", "Dark Grass" }, RockGround = "Grass",
                Walls = new[] { "Brown Wall" },
                Mobs = new[] { "Great Temple Snake", "Great Coil Snake", "Basilisk Baby", "Basilisk", "Mask Shaman", "Mask Warrior", "Mask Hunter" },
                Boss = "Mixcoatl the Masked God",
                MapFile = "Dungeons/Forbidden Jungle.jm",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Davy Jones's Locker", PortalObject = "Davy Jones's Locker Portal",
                Grounds = new[] { "Sand", "Dirt" }, RockGround = "Sand",
                Walls = new[] { "Blue Wall", "Brown Wall" },
                Mobs = new[] { "Fishman Warrior", "Deep Sea Beast", "Giant Squid", "Sea Horse", "Sea Mare", "Grey Sea Slurp" },
                Boss = "Davy Jones",
                MapFile = "Dungeons/Davy Jones' Locker.jm",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Wine Cellar", PortalObject = "Dungeon Portal",
                Grounds = new[] { "Castle Stone Floor Tile", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Brown Wall" },
                Mobs = new[] { "Aberrant Blaster", "Monstrosity Scarab", "Purple Goo", "Vintner of Oryx" },
                Boss = "Abomination of Oryx",
                Width = 64, Height = 64, Rooms = 8
            },
            new DungeonDef
            {
                Name = "Haunted Cemetery", PortalObject = "Haunted Cemetery Portal",
                Grounds = new[] { "Cemetery Grass", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Grey Wall", "Brown Wall" },
                Mobs = new[] { "Blue Zombie", "Zombie Hulk", "Classic Ghost", "Werewolf", "Ghost of Skuld", "Flying Flame Skull" },
                Boss = "Arena Headless Horseman",
                MapFile = "Dungeons/Haunted Cemetery.jm",
                Width = 72, Height = 72, Rooms = 9
            },
            new DungeonDef
            {
                Name = "Haunted Cemetery Gates", PortalObject = "Haunted Cemetery Gates Portal",
                Grounds = new[] { "Cemetery Grass", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Grey Wall", "Brown Wall" },
                Mobs = new[] { "Arena Ghost 1", "Arena Ghost 2", "Arena Ghost Bride", "Arena Possessed Girl" },
                Boss = null,
                MapFile = "Dungeons/Haunted Cemetery Gates.jm",
                Width = 46, Height = 44, Rooms = 6
            },
            new DungeonDef
            {
                Name = "Haunted Cemetery Graves", PortalObject = "Haunted Cemetery Graves Portal",
                Grounds = new[] { "Cemetery Grass", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Grey Wall", "Brown Wall" },
                Mobs = new[] { "Arena Risen Warrior", "Arena Risen Mage", "Arena Risen Archer", "Arena Risen Brawler", "Arena Grave Caretaker" },
                Boss = null,
                MapFile = "Dungeons/Haunted Cemetery Graves.jm",
                Width = 48, Height = 49, Rooms = 6
            },
            new DungeonDef
            {
                Name = "Haunted Cemetery Final Battle", PortalObject = "Haunted Cemetery Final Rest Portal",
                Grounds = new[] { "Cemetery Grass", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Grey Wall", "Brown Wall" },
                Mobs = new[] { "Ghost of Skuld", "Flying Flame Skull", "Blue Zombie", "Zombie Rise" },
                Boss = "Ghost of Skuld",
                MapFile = "Dungeons/Haunted Cemetery Final Battle.jm",
                Width = 49, Height = 49, Rooms = 6
            },
            new DungeonDef
            {
                Name = "Dreamscape Labyrinth", PortalObject = "Dreamscape Labyrinth Portal",
                Grounds = new[] { "Cracked Purple Stone", "Dirt" }, RockGround = "Dirt",
                Walls = new[] { "Blue Wall", "Brown Wall" },
                Mobs = new[] { "Assassin of Oryx", "Minion of Oryx" },
                Boss = "Oryx the Mad God 1",
                Width = 72, Height = 72, Rooms = 9
            }
        };

        public static DungeonDef ByName(string name)
        {
            foreach (DungeonDef def in All)
                if (def.Name == name)
                    return def;
            return null;
        }

        public static DungeonDef ByPortalObject(string portalObject)
        {
            foreach (DungeonDef def in All)
                if (def.PortalObject == portalObject)
                    return def;
            return null;
        }
    }
}
