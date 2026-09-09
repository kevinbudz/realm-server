using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Game.Setpieces;
using SP = RotMG.Game.Setpieces.SetPieces;
using RotMG.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game
{
    //The mad god who looks after the realm, adapted from realm-src-master
    //wServer/realm/Oryx.cs to this codebase's static-map World model
    //(no generated terrain: population is tracked per object type and event
    //setpieces render onto the live map).
    public class Oryx
    {
        private struct TauntData
        {
            public string[] Spawn;
            public string[] NumberOfEnemies;
            public string[] Final;
            public string[] Killed;
        }

        private static readonly Tuple<string, TauntData>[] CriticalEnemies =
        {
            Tuple.Create("Lich", new TauntData()
            {
                NumberOfEnemies = new string[] {
                    "I am invincible while my {COUNT} Liches still stand!",
                    "My {COUNT} Liches will feast on your essence!"
                },
                Final = new string[] {
                    "My final Lich shall consume your souls!",
                    "My final Lich will protect me forever!"
                }
            }),
            Tuple.Create("Ent Ancient", new TauntData()
            {
                NumberOfEnemies = new string[] {
                    "Mortal scum! My {COUNT} Ent Ancients will defend me forever!",
                    "My forest of {COUNT} Ent Ancients is all the protection I need!"
                },
                Final = new string[] {
                    "My final Ent Ancient will destroy you all!",
                    "My final Ent Ancient shall crush you!"
                }
            }),
            Tuple.Create("Oasis Giant", new TauntData()
            {
                NumberOfEnemies = new string[] {
                    "My {COUNT} Oasis Giants will feast on your flesh!",
                    "You have no hope against my {COUNT} Oasis Giants!"
                },
                Final = new string[] {
                    "A powerful Oasis Giant still fights for me!",
                    "You will never defeat me while an Oasis Giant remains!"
                }
            }),
            Tuple.Create("Phoenix Lord", new TauntData()
            {
                NumberOfEnemies = new string[] {
                    "Maggots! My {COUNT} Phoenix Lord will burn you to ash!",
                    "My {COUNT} Phoenix Lords will serve me forever!"
                },
                Final = new string[] {
                    "My final Phoenix Lord will never fall!",
                    "My last Phoenix Lord will blacken your bones!"
                }
            }),
            Tuple.Create("Ghost King", new TauntData()
            {
                NumberOfEnemies = new string[] {
                    "My {COUNT} Ghost Kings give me more than enough protection!",
                    "Pathetic humans! My {COUNT} Ghost Kings shall destroy you utterly!"
                },
                Final = new string[] {
                    "A mighty Ghost King remains to guard me!",
                    "My final Ghost King is untouchable!"
                }
            }),
            Tuple.Create("Cyclops God", new TauntData()
            {
                NumberOfEnemies = new string[] {
                    "Cretins! I have {COUNT} Cyclops Gods to guard me!",
                    "My {COUNT} powerful Cyclops Gods will smash you!"
                },
                Final = new string[] {
                    "My last Cyclops God will smash you to pieces!",
                    "My final Cyclops God shall crush your puny skulls!"
                }
            }),
            Tuple.Create("Red Demon", new TauntData()
            {
                NumberOfEnemies = new string[] {
                    "Fools! There is no escape from my {COUNT} Red Demons!",
                    "My legion of {COUNT} Red Demons live only to serve me!"
                },
                Final = new string[] {
                    "My final Red Demon is unassailable!",
                    "A Red Demon still guards me!"
                }
            }),
            Tuple.Create("Skull Shrine", new TauntData()
            {
                Spawn = new string[] {
                    "Your futile efforts are no match for a Skull Shrine!"
                },
                NumberOfEnemies = new string[] {
                    "Insects! {COUNT} Skull Shrines still protect me!",
                    "Imbeciles! My {COUNT} Skull Shrines make me invincible!"
                },
                Final = new string[] {
                    "Pathetic fools! A Skull Shrine guards me!",
                    "Miserable scum! My Skull Shrine is invincible!"
                },
                Killed = new string[] {
                    "You defaced a Skull Shrine! Minions, to arms!",
                    "{PLAYER} razed one of my Skull Shrines -- I WILL HAVE MY REVENGE!",
                    "{PLAYER}, you will rue the day you dared to defile my Skull Shrine!"
                }
            }),
            Tuple.Create("Cube God", new TauntData()
            {
                Spawn = new string[] {
                    "Your meager abilities cannot possibly challenge a Cube God!"
                },
                NumberOfEnemies = new string[] {
                    "Filthy vermin! My {COUNT} Cube Gods will exterminate you!",
                    "You piteous cretins! {COUNT} Cube Gods still guard me!"
                },
                Final = new string[] {
                    "Worthless mortals! A mighty Cube God defends me!",
                    "Wretched mongrels! An unconquerable Cube God is my bulwark!"
                },
                Killed = new string[] {
                    "You have dispatched my Cube God, but you will never escape my Realm!",
                    "{PLAYER}, you pathetic swine! How dare you assault my Cube God?",
                    "I have many more Cube Gods, {PLAYER}!"
                }
            }),
            Tuple.Create("Pentaract", new TauntData()
            {
                Spawn = new string[] {
                    "Behold my Pentaract, and despair!"
                },
                NumberOfEnemies = new string[] {
                    "Wretched creatures! {COUNT} Pentaracts remain!",
                    "My {COUNT} Pentaracts will protect me forever!"
                },
                Final = new string[] {
                    "I am invincible while my Pentaract stands!",
                    "Ignorant fools! A Pentaract guards me still!"
                },
                Killed = new string[] {
                    "That was but one of many Pentaracts!",
                    "You have razed my Pentaract, but you will die here in my Realm!",
                    "{PLAYER}, by destroying my Pentaract you have sealed your own doom!"
                }
            }),
            Tuple.Create("Grand Sphinx", new TauntData()
            {
                Spawn = new string[] {
                    "At last, a Grand Sphinx will teach you to respect!"
                },
                NumberOfEnemies = new string[] {
                    "My {COUNT} Grand Sphinxes protect my Chamber with their lives!",
                    "My Grand Sphinxes will bewitch you with their beauty!"
                },
                Final = new string[] {
                    "A Grand Sphinx is more than a match for this rabble.",
                    "Gaze upon the beauty of the Grand Sphinx and feel your last hopes drain away."
                },
                Killed = new string[] {
                    "The death of my Grand Sphinx shall be avenged!",
                    "My Grand Sphinx, she was so beautiful. I will kill you myself, {PLAYER}!"
                }
            }),
            Tuple.Create("Lord of the Lost Lands", new TauntData()
            {
                Spawn = new string[] {
                    "Cower in fear of my Lord of the Lost Lands!",
                    "My Lord of the Lost Lands will make short work of you!"
                },
                NumberOfEnemies = new string[] {
                    "Feel the awesome might of my {COUNT} Lords of the Lost Lands!",
                    "Together, my {COUNT} Lords of the Lost Lands will squash you like a bug!"
                },
                Final = new string[] {
                    "Give up now! You stand no chance against a Lord of the Lost Lands!",
                    "Pathetic fools! My Lord of the Lost Lands will crush you all!"
                },
                Killed = new string[] {
                    "What trickery is this?! My Lord of the Lost Lands was invincible!",
                    "You got lucky this time {PLAYER}, but you stand no chance against me!"
                }
            }),
            Tuple.Create("Hermit God", new TauntData()
            {
                Spawn = new string[] {
                    "My Hermit God's thousand tentacles shall drag you to a watery grave!"
                },
                NumberOfEnemies = new string[] {
                    "You will make a tasty snack for my Hermit Gods!",
                    "I will enjoy watching my {COUNT} Hermit Gods fight over your corpse!"
                },
                Final = new string[] {
                    "Flee from my Hermit God, unless you desire a watery grave!",
                    "My Hermit God will pull you beneath the waves!"
                },
                Killed = new string[] {
                    "This is preposterous! There is no way you could have defeated my Hermit God!",
                    "My Hermit God was more than you'll ever be, {PLAYER}. I will kill you myself!"
                }
            }),
            Tuple.Create("Ghost Ship", new TauntData()
            {
                Spawn = new string[] {
                    "My Ghost Ship will terrorize you pathetic peasants!",
                    "A Ghost Ship has entered the Realm."
                },
                Final = new string[] {
                    "My Ghost Ship will send you to a watery grave.",
                    "My Ghost Ship's cannonballs will crush your pathetic Knights!"
                },
                Killed = new string[] {
                    "Alas, my beautiful Ghost Ship has sunk!",
                    "{PLAYER}, you foul creature. I shall see to your death personally!"
                }
            }),
            //The entries below reference content packs absent from this
            //project's GameData (Draconis, Shatters, Cemetery bosses and the
            //empty Boshy trio upstream). They are inert by construction: the
            //announcer skips entries with no live matches, and the kill hook
            //only fires on real kills. They activate automatically if the
            //matching enemies ever gain descriptors and behaviors.
            Tuple.Create("Dragon Head", new TauntData()
            {
                Spawn = new string[] {
                    "The Rock Dragon has been summoned.",
                    "Beware my Rock Dragon. All who face him shall perish."
                },
                Final = new string[] {
                    "My Rock Dragon will end your pathetic existence!",
                    "Fools, no one can withstand the power of my Rock Dragon!",
                    "The Rock Dragon will guard his post until the bitter end.",
                    "The Rock Dragon will never let you enter the Lair of Draconis."
                },
                Killed = new string[] {
                    "My Rock Dragon will return!",
                    "The Rock Dragon has failed me!",
                    "{PLAYER} knows not what he has done.  That Lair was guarded for the Realm's own protection!",
                    "{PLAYER}, you have angered me for the last time!",
                    "{PLAYER} will never survive the trials that lie ahead.",
                    "A filthy weakling like {PLAYER} could never have defeated my Rock Dragon!!!",
                    "You shall not live to see the next sunrise, {PLAYER}!"
                }
            }),
            Tuple.Create("shtrs Defense System", new TauntData()
            {
                Spawn = new string[] {
                    "The Shatters has been discovered!?!",
                    "The Forgotten King has raised his Avatar!"
                },
                Final = new string[] {
                    "Attacking the Avatar of the Forgotten King would be...unwise.",
                    "Kill the Avatar, and you risk setting free an abomination.",
                    "Before you enter the Shatters you must defeat the Avatar of the Forgotten King!"
                },
                Killed = new string[] {
                    "The Avatar has been defeated!",
                    "How could simpletons kill The Avatar of the Forgotten King!?",
                    "{PLAYER} has unleashed an evil upon this Realm.",
                    "{PLAYER}, you have awoken the Forgotten King. Enjoy a slow death!",
                    "{PLAYER} will never survive what lies in the depths of the Shatters.",
                    "Enjoy your little victory while it lasts, {PLAYER}!"
                }
            }),
            Tuple.Create("Zombie Horde", new TauntData()
            {
                Spawn = new string[] {
                    "At last, my Zombie Horde will eradicate you like the vermin that you are!",
                    "The full strength of my Zombie Horde has been unleashed!",
                    "Let the apocalypse begin!",
                    "Quiver with fear, peasants, my Zombie Horde has arrived!"
                },
                Final = new string[] {
                    "A small taste of my Zombie Horde should be enough to eliminate you!",
                    "My Zombie Horde will teach you the meaning of fear!"
                },
                Killed = new string[] {
                    "The death of my Zombie Horde is unacceptable! You will pay for your insolence!",
                    "{PLAYER}, I will kill you myself and turn you into the newest member of my Zombie Horde!"
                }
            }),
            Tuple.Create("Boshy", new TauntData()),
            Tuple.Create("The Kid", new TauntData()),
            Tuple.Create("Sanic", new TauntData())
        };

        private const int TauntIntervalMS = 20000;
        private const int PopulationIntervalMS = 60000;
        private const int EventIntervalMS = 8 * 60 * 1000;
        private const int FirstEventDelayMS = 3 * 60 * 1000;
        private const int RealmLifetimeMS = 30 * 60 * 1000;
        private const int CloseWarningMS = 60000;
        private const int MaxRespawnsPerPass = 20;
        private const int PortalIntervalMS = 2 * 60 * 1000;

        private readonly RealmWorld _world;
        private readonly Random _rand = new Random();
        private Dictionary<ushort, int> _initialCounts = new Dictionary<ushort, int>();
        private Dictionary<ushort, List<TerrainType>> _seedTerrains = new Dictionary<ushort, List<TerrainType>>();
        private IntPoint _spawn;
        private int _bornAt;
        private int _nextTaunt;
        private int _nextPopulation;
        private int _nextEvent;
        private int _nextPortals;

        // Terrain-anchored seed population adapted from the RegionMobs table in
        // realm-src-master wServer/realm/Oryx.cs. Each entry pairs a terrain
        // with a density divisor (one mob per <divisor> tiles of that
        // terrain) and the weighted mob pool for it. The .jm map carries no
        // painted terrain channel, so tiles are classified by
        // TerrainClassifier (ground tile id + distance from spawn). Divisors
        // are scaled from the reference to match this map's measured tile
        // coverage, yielding roughly the old SeedTarget of 2200 mobs.
        // Mob pools are unchanged; only placement is terrain-anchored.
        private static readonly Tuple<string, double>[] MidMobs = new Tuple<string, double>[]
        {
            Tuple.Create("Red Demon", 0.2),
            Tuple.Create("Ogre", 0.2),
            Tuple.Create("Lizard God", 0.1),
            Tuple.Create("Ghost God", 0.1),
            Tuple.Create("Medusa", 0.1),
            Tuple.Create("Slime God", 0.1),
            Tuple.Create("Sprite God", 0.1),
            Tuple.Create("White Demon", 0.1)
        };

        private static readonly Tuple<string, double>[] HighMobs = new Tuple<string, double>[]
        {
            Tuple.Create("Cyclops God", 0.2),
            Tuple.Create("Phoenix Lord", 0.2),
            Tuple.Create("Ghost King", 0.2),
            Tuple.Create("Lich", 0.1),
            Tuple.Create("Ent Ancient", 0.1),
            Tuple.Create("Oasis Giant", 0.1),
            Tuple.Create("Red Demon", 0.1)
        };

        // MidPlains extends the shared Mid pool with the sprites, matching
        // their placement in the reference RegionMobs table.
        private static readonly Tuple<string, double>[] MidPlainsMobs = new Tuple<string, double>[]
        {
            Tuple.Create("Red Demon", 0.2),
            Tuple.Create("Ogre", 0.2),
            Tuple.Create("Lizard God", 0.1),
            Tuple.Create("Ghost God", 0.1),
            Tuple.Create("Medusa", 0.1),
            Tuple.Create("Slime God", 0.1),
            Tuple.Create("Sprite God", 0.1),
            Tuple.Create("White Demon", 0.1),
            Tuple.Create("Fire Sprite", 0.1),
            Tuple.Create("Ice Sprite", 0.1),
            Tuple.Create("Magic Sprite", 0.1)
        };

        private static readonly Dictionary<TerrainType, Tuple<int, Tuple<string, double>[]>> RegionMobs =
            new Dictionary<TerrainType, Tuple<int, Tuple<string, double>[]>>()
        {
            { TerrainType.ShoreSand, Tuple.Create(500, new Tuple<string, double>[]
                {
                    Tuple.Create("Pirate", 0.3),
                    Tuple.Create("Piratess", 0.1),
                    Tuple.Create("Snake", 0.2),
                    Tuple.Create("Scorpion Queen", 0.4)
                })
            },
            { TerrainType.ShorePlains, Tuple.Create(750, new Tuple<string, double>[]
                {
                    Tuple.Create("Bandit Leader", 0.4),
                    Tuple.Create("Red Gelatinous Cube", 0.2),
                    Tuple.Create("Purple Gelatinous Cube", 0.2),
                    Tuple.Create("Green Gelatinous Cube", 0.2)
                })
            },
            { TerrainType.LowPlains, Tuple.Create(1000, new Tuple<string, double>[]
                {
                    Tuple.Create("Hobbit Mage", 0.5),
                    Tuple.Create("Undead Hobbit Mage", 0.4),
                    Tuple.Create("Sumo Master", 0.1)
                })
            },
            { TerrainType.LowForest, Tuple.Create(1000, new Tuple<string, double>[]
                {
                    Tuple.Create("Elf Wizard", 0.2),
                    Tuple.Create("Goblin Mage", 0.2),
                    Tuple.Create("Forest Nymph", 0.3)
                })
            },
            { TerrainType.LowSand, Tuple.Create(1000, new Tuple<string, double>[]
                {
                    Tuple.Create("Sandsman King", 0.4),
                    Tuple.Create("Giant Crab", 0.2),
                    Tuple.Create("Sand Devil", 0.4)
                })
            },
            { TerrainType.MidPlains, Tuple.Create(750, MidPlainsMobs) },
            { TerrainType.MidForest, Tuple.Create(750, MidMobs) },
            { TerrainType.MidSand, Tuple.Create(1500, MidMobs) },
            { TerrainType.HighPlains, Tuple.Create(1500, HighMobs) },
            { TerrainType.HighForest, Tuple.Create(1500, HighMobs) },
            { TerrainType.HighSand, Tuple.Create(1250, HighMobs) },
            { TerrainType.Mountains, Tuple.Create(500, HighMobs) }
        };

        public Oryx(RealmWorld world)
        {
            _world = world;
            _bornAt = Manager.TotalTime;
            _spawn = _world.Map.Regions.TryGetValue(Region.Spawn, out List<IntPoint> spawns) && spawns.Count > 0
                ? spawns[0]
                : new IntPoint(_world.Width / 2, _world.Height / 2);
            SeedPopulation();
            SnapshotPopulation();
            _nextTaunt = _bornAt + TauntIntervalMS;
            _nextPopulation = _bornAt + PopulationIntervalMS;
            _nextEvent = _bornAt + FirstEventDelayMS;
            _nextPortals = _bornAt + PortalIntervalMS;
            MaintainDungeonPortals();
        }

        public void Tick()
        {
            int now = Manager.TotalTime;

            if (_world.Closed)
            {
                if (_world.Players.Count == 0)
                    Reopen();
                return;
            }

            if (now - _bornAt >= RealmLifetimeMS - CloseWarningMS && !_world.Closing)
            {
                _world.Closing = true;
                Announce(_world.Name + " closing in 1 minute.");
            }

            if (now - _bornAt >= RealmLifetimeMS)
            {
                CloseRealm();
                return;
            }

            if (_world.Players.Count == 0)
                return;

            if (now >= _nextTaunt)
            {
                _nextTaunt = now + TauntIntervalMS;
                HandleAnnouncements();
            }

            if (now >= _nextPopulation)
            {
                _nextPopulation = now + PopulationIntervalMS;
                EnsurePopulation();
            }

            if (now >= _nextEvent)
            {
                _nextEvent = now + EventIntervalMS;
                SpawnRandomEvent();
            }

            if (now >= _nextPortals)
            {
                _nextPortals = now + PortalIntervalMS;
                MaintainDungeonPortals();
            }
        }

        //Keeps a rotating set of unlocked dungeon portals in the realm,
        //mirroring the dungeon-portal upkeep of the master PortalMonitor.
        private void MaintainDungeonPortals()
        {
            int count = 0;
            foreach (Entity en in _world.Statics.Values)
            {
                if (en is Portal portal && !string.IsNullOrWhiteSpace(portal.Desc.DungeonName))
                    count++;
            }
            if (count >= 3)
                return;

            List<Dungeons.DungeonDef> defs = new List<Dungeons.DungeonDef>(Dungeons.DungeonDefs.All);
            Dungeons.DungeonDef def = defs[_rand.Next(defs.Count)];

            ObjectDesc portalDesc;
            if (!Resources.Id2Object.TryGetValue(def.PortalObject, out portalDesc))
                return;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x = _rand.Next(2, Math.Max(3, _world.Width - 2));
                int y = _rand.Next(2, Math.Max(3, _world.Height - 2));
                Tile tile = _world.GetTile(x, y);
                if (tile == null || tile.StaticObject != null)
                    continue;
                TileDesc ground = Resources.Type2Tile[tile.Type];
                if (ground.NoWalk || ground.Damage > 0)
                    continue;

                Portal portal = new Portal(portalDesc.Type);
                if (_world.AddEntity(portal, new Position(x + 0.5f, y + 0.5f)) == -1)
                    return;
                tile.StaticObject = portal;
                tile.UpdateCount++;
                _world.UpdateCount++;
                return;
            }
        }

        private string PickMob(Tuple<string, double>[] table)
        {
            double roll = _rand.NextDouble() * table.Sum(t => t.Item2);
            foreach (Tuple<string, double> entry in table)
            {
                roll -= entry.Item2;
                if (roll <= 0)
                    return entry.Item1;
            }
            return table[table.Length - 1].Item1;
        }

        private bool IsSpawnableGround(Tile tile)
        {
            if (tile == null || tile.StaticObject != null)
                return false;
            TileDesc ground = Resources.Type2Tile[tile.Type];
            if (ground.NoWalk || ground.Damage > 0 || ground.Sinking)
                return false;
            return ground.Id.IndexOf("water", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private TerrainType GetTileTerrain(int x, int y)
        {
            Tile tile = _world.GetTile(x, y);
            if (tile == null)
                return TerrainType.None;
            if (!Resources.Type2Tile.TryGetValue(tile.Type, out TileDesc ground))
                return TerrainType.None;
            float dx = x - _spawn.X;
            float dy = y - _spawn.Y;
            return TerrainClassifier.GetTerrain(ground.Id, (float)Math.Sqrt(dx * dx + dy * dy));
        }

        private void SeedPopulation()
        {
            // Collect spawnable tiles per terrain in a single pass, mirroring
            // the reference Init(). Sampling the lists directly guarantees
            // exact per-terrain targets with no rejection-sampling shortfall.
            Dictionary<TerrainType, List<IntPoint>> candidates = new Dictionary<TerrainType, List<IntPoint>>();
            foreach (KeyValuePair<TerrainType, Tuple<int, Tuple<string, double>[]>> kv in RegionMobs)
                candidates[kv.Key] = new List<IntPoint>();
            for (int y = 2; y < _world.Height - 2; y++)
                for (int x = 2; x < _world.Width - 2; x++)
                {
                    TerrainType terrain = GetTileTerrain(x, y);
                    if (terrain == TerrainType.None || !IsSpawnableGround(_world.GetTile(x, y)))
                        continue;
                    candidates[terrain].Add(new IntPoint(x, y));
                }

            int seeded = 0;
            _seedTerrains.Clear();
            foreach (KeyValuePair<TerrainType, Tuple<int, Tuple<string, double>[]>> kv in RegionMobs)
            {
                TerrainType terrain = kv.Key;
                List<IntPoint> spots = candidates[terrain];
                int target = spots.Count / kv.Value.Item1;
                for (int i = 0; i < target && spots.Count > 0; i++)
                {
                    int idx = _rand.Next(spots.Count);
                    IntPoint spot = spots[idx];
                    spots[idx] = spots[spots.Count - 1];
                    spots.RemoveAt(spots.Count - 1);

                    Entity en = SetPieces.SpawnEnemy(_world, PickMob(kv.Value.Item2), spot.X + 0.5f, spot.Y + 0.5f);
                    if (en is Enemy enemy)
                    {
                        enemy.Terrain = terrain;
                        if (!_seedTerrains.TryGetValue(enemy.Type, out List<TerrainType> terrains))
                            _seedTerrains[enemy.Type] = terrains = new List<TerrainType>();
                        terrains.Add(terrain);
                        seeded++;
                    }
                }
            }

            // Anchor map-placed mobs (painted packs and their behavior-spawned
            // children) to the terrain under them so upkeep respawns them
            // near home instead of anywhere on the map.
            foreach (Entity en in _world.Entities.Values)
            {
                if (!(en is Enemy enemy) || enemy.Terrain != TerrainType.None)
                    continue;
                TerrainType terrain = GetTileTerrain((int)enemy.Position.X, (int)enemy.Position.Y);
                if (terrain == TerrainType.None)
                    continue;
                enemy.Terrain = terrain;
                if (!_seedTerrains.TryGetValue(enemy.Type, out List<TerrainType> terrains))
                    _seedTerrains[enemy.Type] = terrains = new List<TerrainType>();
                terrains.Add(terrain);
            }
#if DEBUG
            Program.Print(PrintType.Debug, $"Oryx seeded <{seeded}> realm minions.");
#endif
        }

        private void SnapshotPopulation()
        {
            _initialCounts.Clear();
            foreach (Entity en in _world.Entities.Values)
            {
                if (!(en is Enemy))
                    continue;
                if (_initialCounts.TryGetValue(en.Type, out int count))
                    _initialCounts[en.Type] = count + 1;
                else
                    _initialCounts[en.Type] = 1;
            }
        }

        private void EnsurePopulation()
        {
            Dictionary<ushort, int> current = new Dictionary<ushort, int>();
            foreach (Entity en in _world.Entities.Values)
            {
                if (!(en is Enemy))
                    continue;
                if (current.TryGetValue(en.Type, out int count))
                    current[en.Type] = count + 1;
                else
                    current[en.Type] = 1;
            }

            int spawned = 0;
            foreach (KeyValuePair<ushort, int> kv in _initialCounts)
            {
                current.TryGetValue(kv.Key, out int have);
                int deficit = kv.Value - have;
                for (int i = 0; i < deficit && spawned < MaxRespawnsPerPass; i++)
                    if (Respawn(kv.Key))
                        spawned++;
                if (spawned >= MaxRespawnsPerPass)
                    break;
            }
        }

        private bool Respawn(ushort type)
        {
            // Respawn into one of the terrains this type was seeded in (one
            // entry per seeded mob, so common terrains are picked
            // proportionally), keeping populations terrain-anchored instead
            // of diffusing map-wide.
            bool anchored = _seedTerrains.TryGetValue(type, out List<TerrainType> terrains) && terrains.Count > 0;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                int x = _rand.Next(2, Math.Max(3, _world.Width - 2));
                int y = _rand.Next(2, Math.Max(3, _world.Height - 2));
                if (!IsSpawnableGround(_world.GetTile(x, y)))
                    continue;

                TerrainType terrain = TerrainType.None;
                if (anchored)
                {
                    terrain = terrains[_rand.Next(terrains.Count)];
                    if (GetTileTerrain(x, y) != terrain)
                        continue;
                }

                bool nearPlayer = false;
                foreach (Player player in _world.Players.Values)
                {
                    float dx = player.Position.X - (x + 0.5f);
                    float dy = player.Position.Y - (y + 0.5f);
                    if (dx * dx + dy * dy < 100)
                    {
                        nearPlayer = true;
                        break;
                    }
                }
                if (nearPlayer)
                    continue;

                Enemy enemy = new Enemy(type) { Terrain = terrain };
                return _world.AddEntity(enemy, new Position(x + 0.5f, y + 0.5f)) != -1;
            }
            return false;
        }

        private void HandleAnnouncements()
        {
            Tuple<string, TauntData> taunt = CriticalEnemies[_rand.Next(CriticalEnemies.Length)];
            int count = 0;
            foreach (Entity en in _world.Entities.Values)
            {
                if (en is Enemy && en.Desc != null && en.Desc.Id == taunt.Item1)
                    count++;
            }
            if (count == 0)
                return;

            string msg;
            if (count == 1 && taunt.Item2.Final != null)
                msg = taunt.Item2.Final[_rand.Next(taunt.Item2.Final.Length)];
            else if (taunt.Item2.NumberOfEnemies == null)
                return;
            else
                msg = taunt.Item2.NumberOfEnemies[_rand.Next(taunt.Item2.NumberOfEnemies.Length)]
                    .Replace("{COUNT}", count.ToString());
            OryxSay(msg);
        }

        public void OnPlayerEntered(Player player)
        {
            player.SendInfo("Welcome to Realm of the Mad God");
            player.SendEnemy("Oryx the Mad God", "You are food for my minions!");
            player.SendInfo("Use [WASDQE] to move; click to shoot!");
            player.SendInfo("Type \"/help\" for more help");
        }

        public void OnEnemyKilled(Enemy enemy, Player killer)
        {
            if (enemy.Desc == null || !enemy.Desc.Quest)
                return;

            TauntData? dat = null;
            foreach (Tuple<string, TauntData> entry in CriticalEnemies)
                if (enemy.Desc.Id == entry.Item1)
                {
                    dat = entry.Item2;
                    break;
                }
            if (dat == null)
                return;

            if (dat.Value.Killed != null)
            {
                List<string> lines = dat.Value.Killed
                    .Where(m => killer != null || !m.Contains("{PLAYER}")).ToList();
                if (lines.Count > 0)
                    OryxSay(lines[_rand.Next(lines.Count)]
                        .Replace("{PLAYER}", killer != null ? killer.Name : ""));
            }

            Tuple<string, ISetPiece> evt = SP.Events[_rand.Next(SP.Events.Count)];
            SpawnEvent(evt.Item1, evt.Item2);

            foreach (Tuple<string, TauntData> entry in CriticalEnemies)
            {
                if (evt.Item1 != entry.Item1 || entry.Item2.Spawn == null)
                    continue;
                OryxSay(entry.Item2.Spawn[_rand.Next(entry.Item2.Spawn.Length)]);
                break;
            }
        }

        private void SpawnRandomEvent()
        {
            Tuple<string, ISetPiece> evt = SP.Events[_rand.Next(SP.Events.Count)];
            SpawnEvent(evt.Item1, evt.Item2);
        }

        private void SpawnEvent(string name, ISetPiece setpiece)
        {
            int margin = setpiece.Size / 2 + 2;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int cx = _rand.Next(margin, Math.Max(margin + 1, _world.Width - margin));
                int cy = _rand.Next(margin, Math.Max(margin + 1, _world.Height - margin));

                bool blocked = false;
                for (int x = cx - margin; x <= cx + margin && !blocked; x++)
                    for (int y = cy - margin; y <= cy + margin && !blocked; y++)
                    {
                        Tile tile = _world.GetTile(x, y);
                        if (tile == null || tile.StaticObject != null)
                            blocked = true;
                    }
                if (blocked)
                    continue;

                foreach (Player player in _world.Players.Values)
                {
                    float dx = player.Position.X - cx;
                    float dy = player.Position.Y - cy;
                    if (dx * dx + dy * dy < (margin + 5) * (margin + 5))
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked)
                    continue;

                setpiece.RenderSetPiece(_world, new IntPoint(cx - (setpiece.Size - 1) / 2, cy - (setpiece.Size - 1) / 2));
#if DEBUG
                Program.Print(PrintType.Debug, $"Oryx spawned <{name}> at <{cx},{cy}>.");
#endif
                return;
            }
        }

        private void CloseRealm()
        {
            _world.Closed = true;
            OryxSay("I HAVE CLOSED THIS REALM!");
            OryxSay("YOU WILL NOT LIVE TO SEE THE LIGHT OF DAY!");

            foreach (Player player in _world.Players.Values.ToArray())
            {
                Client client = player.Client;
                client.Active = false;
                client.Send(GameServer.Reconnect(Manager.NexusId));
                Manager.AddTimedAction(2000, client.Disconnect);
            }
        }

        private void Reopen()
        {
            foreach (Entity en in _world.Entities.Values.ToArray())
                if (en is Enemy)
                    _world.RemoveEntity(en);
            _world.Closed = false;
            _world.Closing = false;
            _bornAt = Manager.TotalTime;
            SnapshotPopulation();
            _nextTaunt = _bornAt + TauntIntervalMS;
            _nextPopulation = _bornAt + PopulationIntervalMS;
            _nextEvent = _bornAt + FirstEventDelayMS;
            _nextPortals = Manager.TotalTime + PortalIntervalMS;
        }

        private void OryxSay(string message)
        {
            byte[] packet = GameServer.Text("Oryx the Mad God", 0, -1, 0, "", message);
            foreach (Player player in _world.Players.Values)
                player.Client.Send(packet);
        }

        private void Announce(string message)
        {
            byte[] packet = GameServer.Text("", 0, -1, 0, "", message);
            foreach (Client client in Manager.Clients.Values.ToArray())
                if (client.Player != null)
                    client.Send(packet);
        }
    }
}
