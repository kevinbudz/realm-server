using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Game.Entities.Vendors;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RotMG.Game
{
    public class Tile
    {
        public int UpdateCount;
        public ushort Type;
        public Region Region;
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
        public JSMap Map;

        public int Width;
        public int Height;

        public int Background;
        public bool ShowDisplays;
        public bool AllowTeleport;
        public int BlockSight;

        public string Name;
        public string DisplayName;

        private const int SightChunkRadius = (Player.SightRadius + ChunkController.Size - 1) / ChunkController.Size;
        private readonly HashSet<Chunk> _activeChunks;
        private readonly HashSet<Entity> _activeEntities;

        public World(JSMap map, WorldDesc desc)
            : this(map, desc.Id, desc.DisplayName, desc.Background)
        {
            ShowDisplays = desc.ShowDisplays;
            AllowTeleport = desc.AllowTeleport;
            BlockSight = desc.BlockSight;
        }

        //Generated dungeons bypass WorldDesc (see Game/Dungeons).
        protected World(JSMap map, string name, string displayName, int background)
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
                    JSTile js = map.Tiles[x, y];
                    Tile tile = Tiles[x, y] = new Tile()
                    {
                        Type = js.GroundType,
                        Region = js.Region,
                        UpdateCount = int.MaxValue / 2
                    };

                    if (js.ObjectType != 0xff)
                    {
                        Entity entity = Entity.Resolve(js.ObjectType);
                        if (entity is StaticObject staticObject)
                        {
                            if (entity.Desc.BlocksSight)
                                tile.BlocksSight = true;
                            tile.StaticObject = staticObject;
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

        public void UpdateTile(int x, int y, ushort type)
        {
            Tile tile = GetTile(x, y);
            if (tile != null)
            {
                tile.Type = type;
                tile.UpdateCount++;

                UpdateCount++;
            }
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
            Tile tile = GetTile(x, y);
            if (tile != null)
            {
                if (tile.StaticObject != null)
                {
                    RemoveEntity(tile.StaticObject);
                    tile.StaticObject = null;
                }
                tile.StaticObject = new StaticObject(type);
                tile.BlocksSight = tile.StaticObject.Desc.BlocksSight;
                tile.UpdateCount++;
                AddEntity(tile.StaticObject, new Position(x + 0.5f, y + 0.5f));

                UpdateCount++;
            }
        }

        public void RemoveStatic(int x, int y)
        {
            Tile tile = GetTile(x, y);
            if (tile != null)
            {
                if (tile.StaticObject != null)
                {
                    RemoveEntity(tile.StaticObject);
                    tile.StaticObject = null;
                    tile.BlocksSight = false;
                    tile.UpdateCount++;

                    UpdateCount++;
                }
            }
        }

        public bool BlocksSight(int x, int y)
        {
            Tile tile = GetTile(x, y);
            return tile == null || tile.BlocksSight;
        }

        public Tile GetTileF(float x, float y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return null;
            return Tiles[(int)x, (int)y];
        }

        public Tile GetTile(int x, int y)
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

            if (en is StaticObject)
            {
                Statics.Add(en.Id, en as StaticObject);
                return en.Id;
            }

            if (en is Player)
            {
                Players.Add(en.Id, en as Player);
                PlayerChunks.Insert(en);
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
            if (en is StaticObject)
            {
                Statics.Remove(en.Id);
                en.Dispose();
                return;
            }

            if (en is Player)
            {
                Players.Remove(en.Id);
                PlayerChunks.Remove(en);
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

            //Send Updates to players
            foreach (Player player in Players.Values)
                player.SendUpdate();

            //Tick logic first
            foreach (Entity en in _activeEntities) 
                if (en.TickEntity())
                    en.Tick();

            //Send NewTick to players
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
