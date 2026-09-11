using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Dungeons.Candyland
{
    //Candyland is a hunting ground, not a realm biome and not a camp around
    //the six clustered map spawners. Hunt enemies are placed on open
    //walkable tiles (see HuntLayout) when the first player joins. Each
    //hunt enemy brings 1-3 of its own minions once via SpawnOnce; the
    //Overseer never places minions. Killing a hunt enemy rolls a 10%
    //chance to spawn its mapped boss. Emptied slots refill 30-40s after
    //the kill, and only on tiles at least 25 tiles from any player.
    //Spawners stay on the map as boss candidate sites. Once one of each
    //boss type has fallen, the Cupcake
    //spawns in the starting room; killing it swaps the starting room's
    //Portal of Cowardice for a Realm Portal, once per instance.
    public partial class Overseer
    {
        private const float BossChance = 0.10f;
        private const int RespawnDelayMinMs = 30000;
        private const int RespawnDelayMaxMs = 40000;
        private const int RespawnRetryMs = 5000;
        private const float RespawnPlayerDistance = 18;
        private const float NearbyBoss = 16;
        private const int MinHuntSeparation = 6;

        private static readonly (string Hunt, string Boss)[] HuntBossPairs =
        {
            ("Big Creampuff", "Spoiled Creampuff"),
            ("Rototo", "MegaRototo"),
            ("Wishing Troll", "Desire Troll"),
            ("Unicorn", "Gigacorn"),
            ("Beefy Fairy", "Swoll Fairy")
        };

        private readonly World _world;
        private readonly Dictionary<ushort, ushort> _huntToBoss = new Dictionary<ushort, ushort>();
        private readonly List<HuntSlot> _slots = new List<HuntSlot>();
        private readonly Dictionary<int, int> _enemyToSlot = new Dictionary<int, int>();
        private readonly List<IntPoint> _huntTiles = new List<IntPoint>();
        private readonly HashSet<IntPoint> _usedHuntTiles = new HashSet<IntPoint>();
        private readonly List<Position> _spawnerSites = new List<Position>();

        private int _bossId;
        private bool _populated;
        private readonly HashSet<ushort> _defeatedBosses = new HashSet<ushort>();
        private int _cupcakeId;
        private bool _cupcakeDone;
        private int _cowardicePortalId;

        private struct HuntSlot
        {
            public ushort Type;
            public float X;
            public float Y;
            public int EnemyId;
            public long RefillAt;
        }

        public Overseer(World world)
        {
            _world = world;
            foreach ((string hunt, string boss) in HuntBossPairs)
            {
                if (!Resources.Id2Object.TryGetValue(hunt, out ObjectDesc huntDesc))
                    continue;
                if (!Resources.Id2Object.TryGetValue(boss, out ObjectDesc bossDesc))
                    continue;
                _huntToBoss[huntDesc.Type] = bossDesc.Type;
            }

            _huntTiles.AddRange(CollectHuntTiles(world));
            foreach ((string name, int count) in HuntMix)
            {
                if (!Resources.Id2Object.TryGetValue(name, out ObjectDesc desc))
                    continue;
                for (int i = 0; i < count; i++)
                {
                    _slots.Add(new HuntSlot
                    {
                        Type = desc.Type,
                        X = 0,
                        Y = 0,
                        EnemyId = 0
                    });
                }
            }

            ushort spawnerType = 0;
            if (Resources.Id2Object.TryGetValue("Candyland Spawner", out ObjectDesc spawnerDesc))
                spawnerType = spawnerDesc.Type;
            if (spawnerType == 0)
                return;
            foreach (Entity en in world.Entities.Values)
            {
                if (en is Enemy enemy && enemy.Type == spawnerType)
                    _spawnerSites.Add(enemy.Position);
            }
        }

        public void OnPlayerEntered(Player player)
        {
            if (_populated)
                return;
            _populated = true;
            PopulateHunt();
        }

        public void Tick()
        {
            if (_world.Players.Count == 0)
                return;
            if (_bossId != 0 && !(_world.GetEntity(_bossId) is Enemy))
                _bossId = 0;
            if (_cupcakeId != 0 && !(_world.GetEntity(_cupcakeId) is Enemy))
                _cupcakeId = 0;
            RefillDue();
        }

        public void OnEnemyKilled(Enemy enemy, Player killer)
        {
            if (enemy == null || enemy.Desc == null)
                return;

            if (_bossId != 0 && enemy.Id == _bossId)
            {
                _bossId = 0;
                _defeatedBosses.Add(enemy.Type);
                MaybeSpawnCupcake();
                return;
            }

            if (_cupcakeId != 0 && enemy.Id == _cupcakeId)
            {
                int cupcakeId = _cupcakeId;
                _cupcakeId = 0;
                CompleteDungeon(cupcakeId);
                return;
            }

            if (!_enemyToSlot.TryGetValue(enemy.Id, out int slotIndex))
                return;

            HuntSlot slot = _slots[slotIndex];
            _usedHuntTiles.Remove(new IntPoint((int)slot.X, (int)slot.Y));
            slot.EnemyId = 0;
            slot.RefillAt = Environment.TickCount64 + RespawnDelayMinMs +
                MathUtils.Next(RespawnDelayMaxMs - RespawnDelayMinMs + 1);
            _slots[slotIndex] = slot;
            _enemyToSlot.Remove(enemy.Id);

            //Only slot-tracked kills reach this point. Untracked minions
            //return early above, and tracked Spilled IceCream / Hard Candy
            //have no entry in the hunt-to-boss map, so only the 5 hunt
            //enemies can roll a boss.
            if (_huntToBoss.TryGetValue(enemy.Type, out ushort bossType) &&
                MathUtils.Chance(BossChance))
                TrySpawnBoss(bossType);
        }

        private void PopulateHunt()
        {
            for (int i = 0; i < _slots.Count; i++)
                TryFillSlot(i, skipNearPlayers: false);
            PlaceCowardicePortal();
        }

        //The map paints no exit, so the starting room gets a Portal of
        //Cowardice. Cupcake completion swaps this exact portal for a
        //Realm Portal.
        private void PlaceCowardicePortal()
        {
            if (_cowardicePortalId != 0)
                return;
            if (!Resources.Id2Object.TryGetValue("Portal of Cowardice", out ObjectDesc desc))
                return;
            if (!TryFindSpawnRoomPos(out Position at))
                return;
            Entity portal = Entity.Resolve(desc.Type);
            portal.Spawned = true;
            if (_world.AddEntity(portal, at) == -1)
                return;
            _cowardicePortalId = portal.Id;
        }

        private void MaybeSpawnCupcake()
        {
            if (_cupcakeDone || _cupcakeId != 0)
                return;
            foreach (ushort boss in _huntToBoss.Values)
            {
                if (!_defeatedBosses.Contains(boss))
                    return;
            }
            if (!Resources.Id2Object.TryGetValue("Cupcake", out ObjectDesc desc))
                return;
            if (!TryFindSpawnRoomPos(out Position at))
                return;
            Enemy spawned = SpawnEnemy(desc.Type, at);
            if (spawned == null)
                return;
            _cupcakeId = spawned.Id;
            string[] taunts =
            {
                "Even the happy-go-lucky Cupcake despises you!",
                "Your crazy hijinks even awakened the great Cupcake!"
            };
            BroadcastTaunt(spawned.Id, taunts[MathUtils.Next(taunts.Length)]);
        }

        private void CompleteDungeon(int cupcakeId)
        {
            _cupcakeDone = true;
            BroadcastTaunt(cupcakeId, "You're no fun! Just get out and leave us alone!");

            Position at;
            Entity cowardice = _cowardicePortalId != 0 ? _world.GetEntity(_cowardicePortalId) : null;
            if (cowardice != null && cowardice.Parent != null)
            {
                at = cowardice.Position;
                _world.RemoveEntity(cowardice);
            }
            else if (!TryFindSpawnRoomPos(out at))
                return;
            _cowardicePortalId = 0;

            if (!Resources.Id2Object.TryGetValue("Realm Portal", out ObjectDesc desc))
                return;
            Entity portal = Entity.Resolve(desc.Type);
            portal.Spawned = true;
            _world.AddEntity(portal, at);
        }

        private bool TryFindSpawnRoomPos(out Position at)
        {
            at = new Position();
            int cx = SpawnRoomX + SpawnRoomSize / 2;
            int cy = SpawnRoomY + SpawnRoomSize / 2;
            for (int r = 0; r <= 8; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                            continue;
                        int x = cx + dx;
                        int y = cy + dy;
                        if (x < 0 || y < 0 || x >= _world.Width || y >= _world.Height)
                            continue;
                        if (!_world.IsPassable(x, y, true))
                            continue;
                        at = new Position(x + 0.5f, y + 0.5f);
                        return true;
                    }
                }
            }
            return false;
        }

        private void BroadcastTaunt(int objectId, string text)
        {
            byte[] packet = GameServer.Text("#Cupcake", objectId, -1, 3, "", text);
            foreach (Player player in _world.Players.Values)
                player.Client.Send(packet);
        }

        private void TrySpawnBoss(ushort bossType)
        {
            if (_bossId != 0 && _world.GetEntity(_bossId) is Enemy)
                return;
            _bossId = 0;

            Position at;
            if (!TryPickBossSite(out at))
                return;

            Enemy spawned = SpawnEnemy(bossType, at);
            if (spawned == null)
                return;

            _bossId = spawned.Id;
        }

        private bool TryPickBossSite(out Position at)
        {
            at = new Position();
            if (_spawnerSites.Count > 0)
            {
                int n = _spawnerSites.Count;
                int start = MathUtils.Next(n);
                for (int i = 0; i < n; i++)
                {
                    Position site = _spawnerSites[(start + i) % n];
                    if (_world.AnyPlayerNearby(site.X, site.Y, NearbyBoss))
                        continue;
                    if (!_world.IsPassable((int)site.X, (int)site.Y, true))
                        continue;
                    at = site;
                    return true;
                }
            }

            for (int attempt = 0; attempt < 200; attempt++)
            {
                int x = MathUtils.Next(_world.Width);
                int y = MathUtils.Next(_world.Height);
                if (!_world.IsPassable(x, y, true))
                    continue;
                if (_world.AnyPlayerNearby(x + 0.5f, y + 0.5f, NearbyBoss))
                    continue;
                at = new Position(x + 0.5f, y + 0.5f);
                return true;
            }
            return false;
        }

        //Refills slots whose respawn timer has expired. A slot that
        //cannot be placed (no tile far enough from players) keeps
        //waiting and is retried, so nothing ever pops in view.
        private void RefillDue()
        {
            long now = Environment.TickCount64;
            for (int i = 0; i < _slots.Count; i++)
            {
                HuntSlot slot = _slots[i];
                if (slot.EnemyId != 0 || slot.RefillAt > now)
                    continue;
                if (!TryFillSlot(i, skipNearPlayers: true))
                {
                    slot = _slots[i];
                    slot.RefillAt = now + RespawnRetryMs;
                    _slots[i] = slot;
                }
            }
        }

        private bool TryFillSlot(int slotIndex, bool skipNearPlayers)
        {
            HuntSlot slot = _slots[slotIndex];
            if (slot.EnemyId != 0)
                return false;

            int n = _huntTiles.Count;
            if (n == 0)
                return false;

            int start = MathUtils.Next(n);
            for (int i = 0; i < n; i++)
            {
                IntPoint tile = _huntTiles[(start + i) % n];
                if (_usedHuntTiles.Contains(tile))
                    continue;
                if (IsNearUsedHuntTile(tile))
                    continue;

                float x = tile.X + 0.5f;
                float y = tile.Y + 0.5f;
                if (skipNearPlayers && _world.AnyPlayerNearby(x, y, RespawnPlayerDistance))
                    continue;
                if (!_world.IsPassable(tile.X, tile.Y, true))
                    continue;

                Enemy enemy = SpawnEnemy(slot.Type, new Position(x, y));
                if (enemy == null)
                    continue;

                slot.X = x;
                slot.Y = y;
                slot.EnemyId = enemy.Id;
                _slots[slotIndex] = slot;
                _enemyToSlot[enemy.Id] = slotIndex;
                _usedHuntTiles.Add(tile);
                return true;
            }
            return false;
        }

        private bool IsNearUsedHuntTile(IntPoint tile)
        {
            foreach (IntPoint used in _usedHuntTiles)
            {
                int dx = used.X - tile.X;
                int dy = used.Y - tile.Y;
                if (dx * dx + dy * dy < MinHuntSeparation * MinHuntSeparation)
                    return true;
            }
            return false;
        }

        private Enemy SpawnEnemy(ushort type, Position at)
        {
            Enemy enemy = Entity.Resolve(type) as Enemy;
            if (enemy == null)
                return null;
            if (_world.AddEntity(enemy, at) == -1)
                return null;
            return enemy;
        }

    }
}
