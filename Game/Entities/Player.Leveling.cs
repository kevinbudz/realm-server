using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        public const int MaxLevel = 20;
        public const int EXPPerFame = 2000;

        public static int GetNextLevelEXP(int level)
        {
            return 50 + (level - 1) * 100;
        }

        public static int GetLevelEXP(int level)
        {
            if (level == 1) return 0;
            return 50 * (level - 1) + (level - 2) * (level - 1) * 50;
        }

        public static int GetNextClassQuestFame(int fame)
        {
            for (int i = 0; i < Stars.Length; i++)
            {
                if (fame >= Stars[i] && i == Stars.Length - 1)
                    return 0;
                if (fame < Stars[i])
                    return Stars[i];
            }
            return -1;
        }

        public Entity Quest;

        public void InitLevel(CharacterModel character)
        {
            if (character.Experience != 0) EXP = character.Experience;
            if (character.Fame != 0) CharFame = character.Fame;
            ClassStatsInfo classStat = Client.Account.Stats.GetClassStats((int)Type);
            NextClassQuestFame = GetNextClassQuestFame(classStat.BestFame > CharFame ? classStat.BestFame : CharFame);
            NextLevelEXP = GetNextLevelEXP(Level);
            GainEXP(0);
        }

        public virtual bool GainEXP(int exp)
        {
            EXP += exp;

            int newFame = EXP / EXPPerFame;
            if (newFame != CharFame)
                CharFame = newFame;

            ClassStatsInfo classStat = Client.Account.Stats.GetClassStats((int)Type);
            int newClassQuestFame = GetNextClassQuestFame(classStat.BestFame > newFame ? classStat.BestFame : newFame);
            if (newClassQuestFame > NextClassQuestFame)
            {
                byte[] notification = GameServer.Notification(Id, "Class Quest Complete!", 0xFF00FF00);
                foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
                {
                    if (en is Player player && 
                        (player.Client.Account.Notifications || player.Equals(this)))
                        player.Client.Send(notification);
                }
                NextClassQuestFame = newClassQuestFame;
            }

            bool levelledUp = false;
            if (EXP - GetLevelEXP(Level) >= NextLevelEXP && Level < MaxLevel)
            {
                levelledUp = true;
                Level++;
                //Level ranges gate FindQuest: drop the old target so the
                //next NewTick picks a level-appropriate quest and sends a
                //fresh QuestObjId (mirrors realm-src-master CheckLevelUp
                //clearing questEntity on level-up).
                Quest = null;
                NextLevelEXP = GetNextLevelEXP(Level);
                StatDesc[] stats = Resources.Type2Player[Type].Stats;
                for (int i = 0; i < stats.Length; i++)
                {
                    int min = stats[i].MinIncrease;
                    int max = stats[i].MaxIncrease;
                    Stats[i] += MathUtils.NextInt(min, max);
                    if (Stats[i] > stats[i].MaxValue)
                        Stats[i] = stats[i].MaxValue;
                }

                HP = Stats[0];
                MP = Stats[1];

                if (Level == 20)
                {
                    byte[] text = GameServer.Text("", 0, -1, 0, "", $"{Name} achieved level 20");
                    foreach (Player player in Parent.Players.Values)
                        player.Client.Send(text);
                }

                RecalculateEquipBonuses();
            }

            TrySetSV(StatType.EXP, EXP - GetLevelEXP(Level));
            return levelledUp;
        }

        //Ported from realm-src-master wServer/realm/entities/player/Player.Leveling.cs.
        //QuestDat: (priority, minLevel, maxLevel) keyed by ObjectDesc.Id.
        //Entries without a row here can never become the quest target:
        //FindQuest skips them, so no QuestObjId is sent and the client
        //shows no QuestArrow (this previously dropped every dungeon boss).
        private static readonly Dictionary<string, Tuple<int, int, int>> QuestDat =
            new Dictionary<string, Tuple<int, int, int>>()  //Priority, Min, Max
        {
            // wandering quest enemies
            { "Scorpion Queen",                 Tuple.Create(1, 1, 6) },
            { "Bandit Leader",                  Tuple.Create(1, 1, 6) },
            { "Hobbit Mage",                    Tuple.Create(3, 3, 8) },
            { "Undead Hobbit Mage",             Tuple.Create(3, 3, 8) },
            { "Giant Crab",                     Tuple.Create(3, 3, 8) },
            { "Desert Werewolf",                Tuple.Create(3, 3, 8) },
            { "Sandsman King",                  Tuple.Create(4, 4, 9) },
            { "Goblin Mage",                    Tuple.Create(4, 4, 9) },
            { "Elf Wizard",                     Tuple.Create(4, 4, 9) },
            { "Dwarf King",                     Tuple.Create(5, 5, 10) },
            { "Swarm",                          Tuple.Create(6, 6, 11) },
            { "Shambling Sludge",               Tuple.Create(6, 6, 11) },
            { "Great Lizard",                   Tuple.Create(7, 7, 12) },
            { "Wasp Queen",                     Tuple.Create(8, 7, 20) },
            { "Horned Drake",                   Tuple.Create(8, 7, 20) },

            // setpiece bosses
            { "Deathmage",                      Tuple.Create(5, 6, 11) },
            { "Great Coil Snake",               Tuple.Create(6, 6, 12) },
            { "Lich",                           Tuple.Create(8, 6, 20) },
            { "Actual Lich",                    Tuple.Create(8, 7, 20) },
            { "Ent Ancient",                    Tuple.Create(9, 7, 20) },
            { "Actual Ent Ancient",             Tuple.Create(9, 7, 20) },
            { "Oasis Giant",                    Tuple.Create(10, 8, 20) },
            { "Phoenix Lord",                   Tuple.Create(10, 9, 20) },
            { "Ghost King",                     Tuple.Create(11,10, 20) },
            { "Actual Ghost King",              Tuple.Create(11,10, 20) },
            { "Cyclops God",                    Tuple.Create(12,10, 20) },
            { "Kage Kami",                      Tuple.Create(12,10, 20) },
            { "Red Demon",                      Tuple.Create(13,15, 20) },

                // events
            { "shtrs Defense System",           Tuple.Create(14,15, 20) },
            { "Fanatic of Chaos",               Tuple.Create(14,15, 20) },
            { "Skull Shrine",                   Tuple.Create(14,15, 20) },
            { "Pentaract",                      Tuple.Create(14,15, 20) },
            { "Cube God",                       Tuple.Create(14,15, 20) },
            { "Grand Sphinx",                   Tuple.Create(14,15, 20) },
            { "Lord of the Lost Lands",         Tuple.Create(14,15, 20) },
            { "Hermit God",                     Tuple.Create(14,15, 20) },
            { "Ghost Ship",                     Tuple.Create(14,15, 20) },
            { "Dragon Head",                    Tuple.Create(14,15, 20) },
            { "Lucky Ent God",                  Tuple.Create(14,15, 20) },
            { "Lucky Djinn",                    Tuple.Create(14,15, 20) },
            { "Zombie Horde",                   Tuple.Create(14,15, 20) },

                // dungeon bosses
            { "Evil Chicken God",               Tuple.Create(15,1, 20) },
            { "Bonegrind the Butcher",          Tuple.Create(15,1, 20) },
            { "Dreadstump the Pirate King",     Tuple.Create(15,1, 20) },
            { "Mama Megamoth",                  Tuple.Create(15,1, 20) },
            { "Arachna the Spider Queen",       Tuple.Create(15,1, 20) },
            { "Stheno the Snake Queen",         Tuple.Create(15,1, 20) },
            { "Mixcoatl the Masked God",        Tuple.Create(15,1, 20) },
            { "Limon the Sprite God",           Tuple.Create(15,1, 20) },
            { "Septavius the Ghost God",        Tuple.Create(15,1, 20) },
            { "Davy Jones",                     Tuple.Create(15,1, 20) },
            { "Lord Ruthven",                   Tuple.Create(15,1, 20) },
            { "Archdemon Malphas",              Tuple.Create(15,1, 20) },
            { "Thessal the Mermaid Goddess",    Tuple.Create(15,1, 20) },
            { "Dr Terrible",                    Tuple.Create(15,1, 20) },
            { "Horrific Creation",              Tuple.Create(15,1, 20) },
            { "Masked Party God",               Tuple.Create(15,1, 20) },
            { "Oryx Stone Guardian Left",       Tuple.Create(15,1, 20) },
            { "Oryx Stone Guardian Right",      Tuple.Create(15,1, 20) },
            { "Oryx the Mad God 1",             Tuple.Create(15,1, 20) },
            { "Oryx the Mad God 2",             Tuple.Create(15,1, 20) },
            { "Oryx the Mad God 3",             Tuple.Create(15,1, 20) },
            { "Oryx the Mad God 4",             Tuple.Create(15,1, 20) },
            { "Gigacorn",                       Tuple.Create(15,1, 20) },
            { "Desire Troll",                   Tuple.Create(15,1, 20) },
            { "Spoiled Creampuff",              Tuple.Create(15,1, 20) },
            { "MegaRototo",                     Tuple.Create(15,1, 20) },
            { "Swoll Fairy",                    Tuple.Create(15,1, 20) },
            { "BedlamGod",                      Tuple.Create(15,1, 20) },
            { "Troll 3",                        Tuple.Create(15,1, 20) },
            { "Arena Ghost Bride",              Tuple.Create(15,1, 20) },
            { "Arena Statue Left",              Tuple.Create(15,1, 20) },
            { "Arena Statue Right",             Tuple.Create(15,1, 20) },
            { "Arena Grave Caretaker",          Tuple.Create(15,1, 20) },
            { "Ghost of Skuld",                 Tuple.Create(15,1, 20) },
            { "Tomb Defender",                  Tuple.Create(15,1, 20) },
            { "Tomb Support",                   Tuple.Create(15,1, 20) },
            { "Tomb Attacker",                  Tuple.Create(15,1, 20) },
            { "Active Sarcophagus",             Tuple.Create(15,1, 20) },
            { "shtrs Bridge Sentinel",          Tuple.Create(15,1, 20) },
            { "shtrs The Forgotten King",       Tuple.Create(15,1, 20) },
            { "shtrs Twilight Archmage",        Tuple.Create(15,1, 20) },
            { "NM Black Dragon God",            Tuple.Create(15,1, 20) },
            { "NM Black Dragon God Hardmode",   Tuple.Create(15,1, 20) },
            { "NM Red Dragon God",              Tuple.Create(15,1, 20) },
            { "NM Red Dragon God Hardmode",     Tuple.Create(15,1, 20) },
            { "NM Blue Dragon God",             Tuple.Create(15,1, 20) },
            { "NM Blue Dragon God Hardmode",    Tuple.Create(15,1, 20) },
            { "NM Green Dragon God",            Tuple.Create(15,1, 20) },
            { "NM Green Dragon God Hardmode",   Tuple.Create(15,1, 20) },
            { "lod Ivory Wyvern",               Tuple.Create(15,1, 20) },
            { "The Puppet Master",              Tuple.Create(15,1, 20) },
            { "Jon Bilgewater the Pirate King", Tuple.Create(15,1, 20) },
            { "Epic Larva",                     Tuple.Create(15,1, 20) },
            { "Epic Mama Megamoth",             Tuple.Create(15,1, 20) },
            { "Murderous Megamoth",             Tuple.Create(15,1, 20) },
            { "Son of Arachna",                 Tuple.Create(15,1, 20) },
            { "Golden Oryx Effigy",             Tuple.Create(15,1, 20) },
            { "Murderous Megamoth Deux",        Tuple.Create(15,1, 20) },
            { "Lord Ruthven Deux",              Tuple.Create(15,1, 20) },
            { "NM Green Dragon God Deux",       Tuple.Create(15,1, 20) },
            { "Archdemon Malphas Deux",         Tuple.Create(15,1, 20) },
            { "Stheno the Snake Queen Deux",    Tuple.Create(15,1, 20) },
            { "Golden Oryx Effigy Deux",        Tuple.Create(15,1, 20) },
            { "Oryx the Mad God Deux",          Tuple.Create(15,1, 20) },
            { "vlntns Botany Bella",            Tuple.Create(15,1, 20) },
            { "md1 Head of Shaitan",            Tuple.Create(15,1, 20) },
            { "Queen of Hearts",                Tuple.Create(15,1, 20) },
            { "Fabian the King of the Ossis",   Tuple.Create(15,1, 20) },
            { "TestChicken 2",                  Tuple.Create(15,1, 20) },

            // special events
            { "Megaman",                        Tuple.Create(50,20, 20) },
            { "Boshy",                          Tuple.Create(50,20, 20) },
            { "The Kid",                        Tuple.Create(50,20, 20) },
            { "Sanic",                          Tuple.Create(50,20, 20) }

        };

        private Entity FindQuest()
        {
            //Single pass, no OrderBy sort: max-tracking is order-independent,
            //and the explicit distance tie-break reproduces the old stable
            //sort's "nearest wins score ties" (dict order on full ties).
            Entity ret = null;
            double? bestScore = null;
            double bestDist2 = 0;
            foreach (Entity quest in Parent.Quests.Values)
            {
                if (quest.Desc == null || !quest.Desc.Quest)
                    continue;
                if (!QuestDat.TryGetValue(quest.Desc.Id, out Tuple<int, int, int> range))
                    continue;
                if (Level < range.Item2 || Level > range.Item3)
                    continue;
                double score = (20 - Math.Abs(quest.Desc.Level - Level)) * range.Item1
                    - Position.Distance(quest.Position) / 100;
                double d2 = Position.DistanceSquared(quest.Position);
                if (bestScore == null || score > bestScore || (score == bestScore && d2 < bestDist2))
                {
                    bestScore = score;
                    bestDist2 = d2;
                    ret = quest;
                }
            }
            return ret;
        }

        public void HandleQuest(bool force = false)
        {
            if (force || Manager.TotalTicks % 500 == 0 || Quest == null || Quest.Parent == null)
            {
                Entity newQuest = FindQuest();
                if (newQuest != null && newQuest != Quest)
                {
                    Quest = newQuest;
                    Client.Send(GameServer.QuestObjId(newQuest.Id));
                }
            }
        }
    }
}
