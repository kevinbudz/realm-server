using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Game.Entities.Vendors;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotMG.Game
{
    //Value type by design: a 2048x2048 world holds 4.2M of these, so a
    //class costs millions of long-lived heap objects per world (plus a
    //pointer chase on every tile touch). All mutations go through the
    //Tiles array indexer below or the Update*/RemoveStatic helpers;
    //never hold a copy in a local and write through it.
    public struct Tile
    {
        public int UpdateCount;
        public ushort Type;
        public Region Region;
        public TerrainType Terrain;
        public byte Elevation;
        public StaticObject StaticObject;
        public bool BlocksSight;
    }

    public class World
    {
        public int Id;
        public int NextObjectId;
        public int NextProjectileId;

        public Dictionary<int, Entity> Entities;
        public Dictionary<int, Entity> Quests;
        public Dictionary<int, Entity> Constants;
        public Dictionary<int, Player> Players;
        public Dictionary<int, StaticObject> Statics;

        public ChunkController EntityChunks;
        public ChunkController PlayerChunks;

        public int UpdateCount;
        public List<string> ChatMessages;

        public Tile[,] Tiles;
        public IGameMap Map;

        public int Width;
        public int Height;

        public int Background;
        public bool ShowDisplays;
        public bool AllowTeleport;
        public int BlockSight;

        public string Name;
        public string DisplayName;
        public string SBName;

        private const int SightChunkRadius = (Player.SightRadius + ChunkController.Size - 1) / ChunkController.Size;
        //Player counts below this broadcast sequentially: waking the pool
        //costs more than it saves for solo/small worlds. Tunable.
        private const int ParallelBroadcastThreshold = 4;
        private readonly HashSet<Chunk> _activeChunks;
        private readonly HashSet<Entity> _activeEntities;

        public World(IGameMap map, WorldDesc desc)
            : this(map, desc.Id, desc.DisplayName, desc.Background)
        {
            ShowDisplays = desc.ShowDisplays;
            AllowTeleport = desc.AllowTeleport;
            BlockSight = desc.BlockSight;
        }

        //Direct construction without a WorldDesc.
        protected World(IGameMap map, string name, string displayName, int background)
        {
            Map = map;
            Width = map.Width;
            Height = map.Height;

            Background = background;
            ShowDisplays = false;
            AllowTeleport = true;
            BlockSight = 0;

            Name = name;
            DisplayName = displayName;

            Entities = new Dictionary<int, Entity>();
            Quests = new Dictionary<int, Entity>();
            Constants = new Dictionary<int, Entity>();
            Players = new Dictionary<int, Player>();
            Statics = new Dictionary<int, StaticObject>();

            EntityChunks = new ChunkController(Width, Height);
            PlayerChunks = new ChunkController(Width, Height);

            _activeChunks = new HashSet<Chunk>((SightChunkRadius * 2 + 1) * (SightChunkRadius * 2 + 1));
            _activeEntities = new HashSet<Entity>(256);

            ChatMessages = new List<string>();

            Tiles = new Tile[Width, Height];

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    //Stored to the array first: Tile is a struct, so the
                    //link below must write through the indexer, and
                    //AddEntity's tile link must observe the array.
                    Tiles[x, y] = new Tile()
                    {
                        Type = map.GetGroundType(x, y),
                        Region = map.GetRegion(x, y),
                        Terrain = map.GetTerrain(x, y),
                        Elevation = map.GetElevation(x, y),
                        UpdateCount = int.MaxValue / 2
                    };

                    ushort objType = map.GetObjectType(x, y);
                    if (objType != 0xff)
                    {
                        Entity entity = Entity.Resolve(objType);
                        Wmap.ApplyObjCfg(entity, map.GetObjCfg(x, y));
                        if (entity is StaticObject staticObject)
                        {
                            if (entity.Desc.BlocksSight)
                                Tiles[x, y].BlocksSight = true;
                            Tiles[x, y].StaticObject = staticObject;
                        }

                        AddEntity(entity, new Position(x + 0.5f, y + 0.5f));
                    }
                }
            InitMerchants();
            UpdateCount = int.MaxValue / 2;
        }
        
        //Spawns rotating-stock shopkeepers on Store_* regions, mirroring
        //realm-src-master wServer/realm/worlds/World.cs merchant init.
        private void InitMerchants()
        {
            ObjectDesc merchantDesc;
            if (!Resources.Id2Object.TryGetValue("Merchant", out merchantDesc))
                return;

            foreach (KeyValuePair<Region, List<ShopItem>> shop in MerchantLists.Shops)
            {
                List<IntPoint> locations;
                if (!Map.Regions.TryGetValue(shop.Key, out locations) || locations.Count == 0)
                    continue;

                List<ShopItem> stock = shop.Value.FindAll(i => i.ItemId != ushort.MaxValue);
                if (stock.Count == 0)
                    continue;

                CurrencyType currency = CurrencyType.Gold;
                MerchantLists.ShopCurrency.TryGetValue(shop.Key, out currency);

                bool rotate = stock.Count > locations.Count;
                Queue<ShopItem> queue = new Queue<ShopItem>(stock);
                int reloadOffset = 0;
                foreach (IntPoint loc in locations)
                {
                    if (queue.Count == 0)
                        foreach (ShopItem restock in stock)
                            queue.Enqueue(restock);

                    ShopItem item = queue.Dequeue();
                    reloadOffset += 500;
                    WorldMerchant merchant = new WorldMerchant(merchantDesc.Type)
                    {
                        ShopItem = item,
                        Item = item.ItemId,
                        Price = item.Price,
                        Count = item.Count,
                        Currency = currency,
                        RankReq = 0,
                        ItemList = shop.Value,
                        ReloadOffset = reloadOffset,
                        Rotate = rotate
                    };
                    merchant.SyncMerchantStats();
                    merchant.SyncStockStats();
                    AddEntity(merchant, new Position(loc.X + 0.5f, loc.Y + 0.5f));
                }
            }
        }

        public IntPoint GetRegion(Region region)
        {
            if (!Map.Regions.ContainsKey(region))
                return new IntPoint(0, 0);
            return Map.Regions[region][MathUtils.Next(Map.Regions[region].Count)];
        }

        //Spawn tiles for joining players, mirroring realm-src-master
        //wServer/realm/worlds/World.cs GetSpawnPoints (all Spawn tiles;
        //CastleWorld narrows it by raid size).
        public virtual List<IntPoint> GetSpawnPoints()
        {
            if (Map.Regions.TryGetValue(Region.Spawn, out List<IntPoint> spawns))
                return new List<IntPoint>(spawns);
            return new List<IntPoint>();
        }

        public virtual bool AllowedAccess(Client client)
        {
            return true;
        }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(SBName))
                return SBName;
            return DisplayName;
        }

        //Reference semantics from realm-src-master
        //wServer/realm/worlds/World.cs: in-bounds, walkable ground
        //(Resources.Type2Tile NoWalk), no blocking static
        //(FullOccupy/EnemyOccupySquare always block; OccupySquare
        //blocks only when spawning).
        public bool IsPassable(int x, int y, bool spawning = false)
        {
            Tile? tile = GetTile(x, y);
            if (tile == null)
                return false;
            if (Resources.Type2Tile.TryGetValue(tile.Value.Type, out TileDesc ground) && ground.NoWalk)
                return false;
            ObjectDesc blocking = tile.Value.StaticObject?.Desc;
            if (blocking != null && (blocking.FullOccupy || blocking.EnemyOccupySquare || (spawning && blocking.OccupySquare)))
                return false;
            return true;
        }

        //Chunk-routed proximity check: scans only the chunk neighborhood
        //instead of all players, turning the per-entity idle gate from
        //O(P) into O(local players). Decoy entries in PlayerChunks are
        //skipped so semantics match the old Players.Values scan.
        public bool AnyPlayerNearby(double x, double y, double radius = 10)
        {
            Chunk[,] chunks = PlayerChunks?.Chunks;
            if (chunks == null)
            {
                foreach (Player player in Players.Values)
                {
                    double ldx = player.Position.X - x;
                    double ldy = player.Position.Y - y;
                    if (ldx * ldx + ldy * ldy < radius * radius)
                        return true;
                }
                return false;
            }

            int size = ChunkController.Convert((float)radius);
            int cx = ChunkController.Convert((float)x);
            int cy = ChunkController.Convert((float)y);
            int startX = Math.Max(0, cx - size);
            int startY = Math.Max(0, cy - size);
            int endX = Math.Min(chunks.GetLength(0) - 1, cx + size);
            int endY = Math.Min(chunks.GetLength(1) - 1, cy + size);
            double r2 = radius * radius;

            for (int ix = startX; ix <= endX; ix++)
                for (int iy = startY; iy <= endY; iy++)
                    foreach (Entity en in chunks[ix, iy].Entities)
                    {
                        if (!(en is Player))
                            continue;
                        double dx = en.Position.X - x;
                        double dy = en.Position.Y - y;
                        if (dx * dx + dy * dy < r2)
                            return true;
                    }
            return false;
        }

        //Moves everyone to another world, adapted from realm-src-master
        //World.QuakeToWorld. There is no earthquake ShowEffect locally
        //(ShowEffectIndex has no Earthquake member), so the warning
        //broadcast is skipped; reconnect/disconnect below mirrors
        //GameServer.Escape and Oryx.CloseRealm. The reference diverts
        //Paused players to the Nexus, but this codebase has no Paused
        //condition effect, so everyone goes to newWorld.
        public void QuakeToWorld(World newWorld)
        {
            if (this is RealmWorld realm)
                realm.Closed = true;

            Manager.AddTimedAction(8000, () =>
            {
                foreach (Player player in Players.Values.ToArray())
                {
                    Client client = player.Client;
                    if (client == null)
                        continue;
                    client.Active = false;
                    client.Send(GameServer.Reconnect(newWorld.Id));
                }
            });
            Manager.AddTimedAction(20000, () =>
            {
                //Ensure stragglers still here leave the world.
                foreach (Player player in Players.Values.ToArray())
                    if (player.Parent == this && player.Client != null)
                        player.Client.Disconnect();
            });
        }

        public void UpdateTile(int x, int y, ushort type)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;
            //Through the indexer: Tile is a struct, so a GetTile local
            //would be a copy and writes to it would be lost.
            Tiles[x, y].Type = type;
            Tiles[x, y].UpdateCount++;

            UpdateCount++;
        }

        //public IntPoint CastLine(int x, int y, int x2, int y2)
        //{
        //    int w = x2 - x;
        //    int h = y2 - y;

        //    int dx1 = w < 0 ? -1 : w > 0 ? 1 : 0;
        //    int dy1 = h < 0 ? -1 : h > 0 ? 1 : 0;
        //    int dx2 = dx1;
        //    int dy2 = 0;

        //    int longest = w < 0 ? -w : w;
        //    int shortest = h < 0 ? -h : h;

        //    if (!(longest > shortest))
        //    {
        //        longest = h < 0 ? -h : h;
        //        shortest = w < 0 ? -w : w;
        //        if (h < 0)
        //            dy2 = -1;
        //        else if (h > 0)
        //            dy2 = 1;
        //        dx2 = 0;
        //    }

        //    int numerator = longest >> 1;
        //    for (int i = 0; i <= longest; i++)
        //    {
        //        if (BlocksSight(x, y))
        //            return new IntPoint(x, y);

        //        numerator += shortest;
        //        if (!(numerator < longest))
        //        {
        //            numerator -= longest;
        //            x += dx1;
        //            y += dy1;
        //        }
        //        else
        //        {
        //            x += dx2;
        //            y += dy2;
        //        }
        //    }

        //    return new IntPoint(-1, -1);
        //}

        public void UpdateStatic(int x, int y, ushort type)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;
            StaticObject prev = Tiles[x, y].StaticObject;
            if (prev != null)
            {
                RemoveEntity(prev);
                Tiles[x, y].StaticObject = null;
            }
            StaticObject next = new StaticObject(type);
            Tiles[x, y].StaticObject = next;
            Tiles[x, y].BlocksSight = next.Desc.BlocksSight;
            Tiles[x, y].UpdateCount++;
            AddEntity(next, new Position(x + 0.5f, y + 0.5f));

            UpdateCount++;
        }

        public void RemoveStatic(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;
            StaticObject prev = Tiles[x, y].StaticObject;
            if (prev != null)
            {
                RemoveEntity(prev);
                Tiles[x, y].StaticObject = null;
                Tiles[x, y].BlocksSight = false;
                Tiles[x, y].UpdateCount++;

                UpdateCount++;
            }
        }

        public bool BlocksSight(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return true;
            return Tiles[x, y].BlocksSight;
        }

        public Tile? GetTileF(float x, float y)
        {
            //NaN compares false against every bound, so check finiteness
            //first: otherwise (int)NaN indexes Tiles and AddEntity accepts
            //positions that later hang movement. Callees treat null as blocked.
            if (!float.IsFinite(x) || !float.IsFinite(y))
                return null;
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return null;
            return Tiles[(int)x, (int)y];
        }

        public Tile? GetTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return null;
            return Tiles[x, y];
        }

        public Entity GetEntity(int id)
        {
            if (Entities.TryGetValue(id, out Entity en))
                return en;
            if (Players.TryGetValue(id, out Player player))
                return player as Entity;
            if (Statics.TryGetValue(id, out StaticObject st))
                return st as Entity;
            return null;
        }

        public void MoveEntity(Entity en, Position to)
        {
#if DEBUG
            if (en == null)
                throw new Exception("Undefined entity.");
#endif
            //Refuse non-finite destinations so a NaN can never poison an
            //entity's position or chunk (which hung the main thread pump).
            if (!float.IsFinite(to.X) || !float.IsFinite(to.Y))
                return;
            if (en.Position != to)
            {
                en.Position = to;
                en.UpdateCount++;

                if (en is StaticObject)
                    return;

                ChunkController controller = (en is Player || en is Decoy) 
                    ? PlayerChunks : EntityChunks;
                controller.Insert(en);
            }
        }

        public int AddEntity(Entity en, Position at)
        {
#if DEBUG
            if (en == null)
                throw new Exception("Entity is null.");
            if (en.Id != 0)
                throw new Exception("Entity has already been added.");
#endif

            if (GetTileF(at.X, at.Y) == null)
                return -1;

            en.Id = ++NextObjectId;
            en.Parent = this;
            en.Position = at;
            MoveEntity(en, en.Position);

            if (en is StaticObject staticObj)
            {
                Statics.Add(en.Id, staticObj);
                //Clients discover statics through their tile (see Player
                //updates); without this link runtime-spawned portals and
                //other statics stay invisible (mirrors PlacePortal).
                //Indexed directly: Tile is a struct, so a GetTile local
                //would be a copy and writes to it would be lost.
                //GetTileF above already bounds-checked at.X/at.Y.
                if (Tiles[(int)at.X, (int)at.Y].StaticObject == null)
                {
                    Tiles[(int)at.X, (int)at.Y].StaticObject = staticObj;
                    Tiles[(int)at.X, (int)at.Y].UpdateCount++;
                    UpdateCount++;
                }
                return en.Id;
            }

            if (en is Player)
            {
                Players.Add(en.Id, en as Player);
                PlayerChunks.Insert(en);
                //Davy Jones' Locker tracks collected keys in a client-side
                //HUD (KeysView); show it on arrival, key pickups light the
                //individual keys (see DavyJones.cs).
                if (Name == "Davy Jones's Locker")
                    (en as Player).Client.Send(GameServer.GlobalNotification(0, "showKeyUI"));
            }
            else if (en is Decoy)
            {
                Entities.Add(en.Id, en);
                PlayerChunks.Insert(en);
            }
            else
            {
                Entities.Add(en.Id, en);
                EntityChunks.Insert(en);

                if (en.Desc.Quest)
                    Quests.Add(en.Id, en);
            }

            if (en.Constant)
            {
                Constants.Add(en.Id, en);
            }

            en.Init();
            if (en is Player entered)
                OnPlayerEntered(entered);
            return en.Id;
        }

        public void RemoveEntity(Entity en)
        {
#if DEBUG
            if (en == null)
                throw new Exception("Entity is null.");
            if (en.Id == 0)
                throw new Exception("Entity has not been added yet.");
#endif     
            if (en is StaticObject staticObj)
            {
                Statics.Remove(en.Id);
                //Clear the tile link so removed statics (e.g. expired
                //portals) disappear instead of lingering as ghosts.
                //Indexed directly: Tile is a struct, so a GetTile local
                //would be a copy and writes to it would be lost.
                //GetTileF already bounds-checked this position.
                Tile? tile = GetTileF(en.Position.X, en.Position.Y);
                if (tile != null && tile.Value.StaticObject == staticObj)
                {
                    Tiles[(int)en.Position.X, (int)en.Position.Y].StaticObject = null;
                    Tiles[(int)en.Position.X, (int)en.Position.Y].UpdateCount++;
                    UpdateCount++;
                }
                en.Dispose();
                return;
            }

            if (en is Player player)
            {
                Players.Remove(en.Id);
                PlayerChunks.Remove(en);
                //Owned vanity pets leave with their owner so world changes
                //and deaths never orphan them (see SpawnPetIfAttached).
                if (player.Pet != null)
                {
                    if (player.Pet.Parent == this)
                        RemoveEntity(player.Pet);
                    player.Pet = null;
                }
            }
            else if (en is Decoy)
            {
                Entities.Remove(en.Id);
                PlayerChunks.Remove(en);
            }
            else
            {
                Entities.Remove(en.Id);
                EntityChunks.Remove(en);

                if (en.Desc.Quest)
                    Quests.Remove(en.Id);
            }

            if (Constants.ContainsKey(en.Id))
            {
                Constants.Remove(en.Id);
            }

            en.Dispose();
        }

        public void Tick()
        {
            OnTick();

            if (Players.Count == 0)
                return;

            _activeChunks.Clear();
            foreach (Entity en in Players.Values)
            {
                for (int k = -SightChunkRadius; k <= SightChunkRadius; k++)
                    for (int j = -SightChunkRadius; j <= SightChunkRadius; j++)
                    {
                        Chunk chunk = EntityChunks.GetChunk(en.CurrentChunk.X + k, en.CurrentChunk.Y + j);
                        if (chunk != null)
                            _activeChunks.Add(chunk);
                    }
            }

            _activeEntities.Clear();
            _activeEntities.UnionWith(Players.Values);
            _activeEntities.UnionWith(Constants.Values);
            foreach (Chunk chunk in _activeChunks)
                _activeEntities.UnionWith(chunk.Entities);
            //GameObject enemies (e.g. Oryx's Living Floor) live in Statics,
            //not EntityChunks, so they would never run behaviors otherwise.
            foreach (StaticObject st in Statics.Values)
                if (st.Behavior != null)
                    _activeEntities.Add(st);

            //Player broadcast parallelizes cleanly: each worker touches only
            //its own player's mutable state while world state is read-only
            //here (entity ticks run sequentially below). PacketWriter.Rent
            //is per-thread, Client.Send is ConcurrentQueue-backed, and
            //overflow disconnects are deferred to the main thread's Tick.
            if (Players.Count >= ParallelBroadcastThreshold)
                Parallel.ForEach(Players.Values, player => player.SendUpdate());
            else
                foreach (Player player in Players.Values)
                    player.SendUpdate();

            //Tick logic first
            foreach (Entity en in _activeEntities) 
                if (en.TickEntity())
                    en.Tick();

            //Send NewTick to players (same independence as SendUpdate above)
            if (Players.Count >= ParallelBroadcastThreshold)
                Parallel.ForEach(Players.Values, player => player.SendNewTick());
            else
                foreach (Player player in Players.Values)
                    player.SendNewTick();

            //Clear new stats
            foreach (Entity en in _activeEntities)
                if (en.TickEntity())
                    en.NewSVs.Clear();

            ChatMessages.Clear();
        }

        protected virtual void OnTick()
        {
        }

        protected virtual void OnPlayerEntered(Player player)
        {
        }

        public void Dispose()
        {
            foreach (Entity en in Entities.Values) RemoveEntity(en);
            foreach (Entity en in Players.Values) RemoveEntity(en);
            foreach (Entity en in Statics.Values) RemoveEntity(en);

            PlayerChunks.Dispose();
            EntityChunks.Dispose();

            ChatMessages.Clear();

            Tiles = null;
        }
    }
}
