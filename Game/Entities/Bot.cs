using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Entities
{
    public enum BotMode
    {
        Follow,
        Stay,
        Wander
    }

    //Server-side stress-test dummy. A Bot is a Player in every way real
    //clients can observe (world Players dict, PlayerChunks, Update/NewTick
    //serialization, enemy AI activation, damage attribution), but it owns
    //no account, no character row and no socket: its Client is an
    //unregistered stub whose outbound queue is never flushed, and whose
    //State is Disconnected so even a stray Disconnect() is a no-op instead
    //of returning the stub to the socket pool.
    //
    //Consequences of having no client:
    //- Bots never receive Update/NewTick (SendUpdate/SendNewTick are no-ops).
    //  They still generate both for every real observer, which is the load
    //  being measured.
    //- Enemy projectiles can never hit bots: hits are client-acknowledged
    //  (see Player.VerifyProjectiles) and bots send no acks. Bots also spawn
    //  Invulnerable, so trap/poison/melee Damage() calls fizzle too.
    //- Bots gain no EXP and earn no loot (see Enemy.Death): their damage
    //  still kills and drives behaviors/overseers, but Min-slot guaranteed
    //  drops stay with real players and no bot-owned bags flood the world.
    public class Bot : Player
    {
        private const float ShootRange = 10f;
        private const int ShootCooldownTicks = 5;
        private const int WanderRetargetTicks = 30;

        public int OwnerAccountId;
        public string OwnerName;
        public Player Owner;
        public BotMode Mode = BotMode.Follow;
        public bool ShootEnabled = true;

        private readonly Random _rand;
        private Position? _wanderTarget;
        private int _wanderStale;
        private int _shootCooldown;
        private int _projId = -1;

        public Bot(ushort classType, int seq, Player owner) : base(classType)
        {
            Owner = owner;
            OwnerAccountId = owner.Client.Account.Id;
            OwnerName = owner.Name;
            _rand = new Random(unchecked(seq * 7919 + 13));

            Client = CreateStubClient(seq);

            PlayerDesc pdesc = Resources.Type2Player[classType];
            Stats = new int[8];
            Boosts = new int[8];
            ActivateBoosts = new int[8];
            for (int i = 0; i < 8; i++)
                Stats[i] = pdesc.Stats[i].MaxValue;
            Inventory = (int[])pdesc.Equipment.Clone();
            ItemDatas = (int[])pdesc.ItemDatas.Clone();
            FameStats = new FameStatsInfo();
            Level = MaxLevel;
            EXP = GetLevelEXP(MaxLevel);
            NextLevelEXP = GetNextLevelEXP(MaxLevel);
            AccountId = -(1000000 + seq);
            Name = "Bot" + seq;
            NameChosen = true;
            Tex1 = owner.Tex1;
            Tex2 = owner.Tex2;
            SkinType = owner.SkinType;
            RecalculateEquipBonuses();
            HP = GetStat(0);
            MP = GetStat(1);
        }

        //Copy for cross-world moves: the failed alternative (RemoveEntity,
        //reset Id, re-AddEntity) resurrects a disposed object — Dispose
        //nulls TileUpdates and wipes SVs — and mutating Id while observers
        //track the old one corrupts their Entities hash set, breaking their
        //NewTick every tick thereafter. A fresh object sidesteps both.
        internal Bot(Bot src, int seq) : base(src.Type)
        {
            PrivateSVs = new Dictionary<StatType, object>();
            Owner = src.Owner;
            OwnerAccountId = src.OwnerAccountId;
            OwnerName = src.OwnerName;
            Mode = src.Mode;
            ShootEnabled = src.ShootEnabled;
            _rand = new Random(unchecked(seq * 7919 + 13));
            Client = CreateStubClient(seq);

            Stats = (int[])src.Stats.Clone();
            Boosts = new int[Stats.Length];
            ActivateBoosts = (int[])src.ActivateBoosts.Clone();
            Inventory = (int[])src.Inventory.Clone();
            ItemDatas = (int[])src.ItemDatas.Clone();
            FameStats = src.FameStats;
            Level = src.Level;
            EXP = src.EXP;
            NextLevelEXP = src.NextLevelEXP;
            CharFame = src.CharFame;
            AccountId = src.AccountId;
            Name = src.Name;
            NameChosen = true;
            Tex1 = src.Tex1;
            Tex2 = src.Tex2;
            SkinType = src.SkinType;
            HasBackpack = src.HasBackpack;
            HealthPotions = src.HealthPotions;
            MagicPotions = src.MagicPotions;
            PetId = src.PetId;
            RecalculateEquipBonuses();
            HP = Math.Min(src.HP, GetStat(0));
            MP = Math.Min(src.MP, GetStat(1));
        }

        //Unregistered stub: never added to Manager.Clients, never ticked
        //or flushed as a connection. All broadcast gates
        //(AllyShots/AllyDamage/Effects/Sounds/Notifications/IgnoredIds)
        //read real values below, so generic player loops stay null-safe.
        //State is Disconnected as a fuse: even a stray Disconnect() on the
        //stub returns immediately instead of recycling it into the pool.
        private static Client CreateStubClient(int seq)
        {
            Client stub = new Client(new SendState(), new ReceiveState());
            stub.State = ProtocolState.Disconnected;
            stub.Account = new AccountModel()
            {
                LockedIds = new List<int>(),
                IgnoredIds = new List<int>(),
                Stats = new StatsInfo() { ClassStats = new ClassStatsInfo[0] }
            };
            stub.Random = new wRandom((uint)(seq * 7919 + 13));
            return stub;
        }

        public override void Init()
        {
            //Player.Init without the AccountList sends (no client to send to).
            TileUpdates = new Dictionary<long, int>(1024);
            EntityUpdates = new Dictionary<int, int>();
            Entities = new HashSet<Entity>();
            CalculatedSightCircle = new HashSet<IntPoint>();
            AwaitingProjectiles = new Queue<AwaitingShots>();
            AckedProjectiles = new Dictionary<int, ProjectileAck>();
            ShotProjectiles = new Dictionary<int, Projectile>();
            AwaitingAoes = new Queue<AoeAck>();
            ShootAEs = new Queue<ushort>();
            AwaitingGoto = new Queue<AwaitingGotoWait>();

            SpeedHistory = new List<float>(10);
            MultiplierHistory = new List<float>(10);
            for (int i = 0; i < 10; i++)
                PushSpeedToHistory(GetMovementSpeed() * 1.5f, 1f);

            ApplyConditionEffect(ConditionEffectIndex.Invincible, -1);
            ApplyConditionEffect(ConditionEffectIndex.Invulnerable, -1);
        }

        public override void Tick()
        {
            //Player.Tick without validation disconnects, trade timeouts and
            //client-ack sweeps: bots have no inbox, so those are empty.
            if (Dead)
                return;
            TickRegens();
            base.Tick();
            BrainTick();
        }

        //Bots receive nothing; their cost to the server is being simulated
        //and serialized to everyone else, not the other way around. These
        //must stay overrides (not hides): worlds hold bots as Player and
        //dispatch SendUpdate/SendNewTick/GainEXP through that type, and a
        //stub-queued full update per tick would pile megabytes of dropped
        //packets per bot.
        public override void SendUpdate() { }
        public override void SendNewTick() { }

        //Bots bank no character row.
        public override void SaveToCharacter() { }

        //Bots stay maxed; nearby kills must not touch the stub account's
        //(empty) class stats.
        public override bool GainEXP(int exp) { return false; }

        //Lethal damage heals and repositions instead of writing a death row
        //and disconnecting. In practice unreachable (invulnerable + no
        //enemy-bullet path), but admin damage sources share Damage().
        public override void Death(string killer)
        {
            if (Dead)
                return;
            HP = GetStat(0);
            MP = GetStat(1);
            foreach (ConditionEffectIndex eff in new ConditionEffectIndex[]
                { ConditionEffectIndex.Bleeding, ConditionEffectIndex.Sick })
                if (HasConditionEffect(eff))
                    RemoveConditionEffect(eff);
            Position home = BotManager.HomeFor(this);
            if (Parent != null)
                Parent.MoveEntity(this, home);
        }

        private void BrainTick()
        {
            Player owner = Owner;
            if (owner == null || owner.Parent == null || owner.Dead)
                owner = null;

            switch (Mode)
            {
                case BotMode.Follow:
                    if (owner != null && owner.Parent == Parent)
                    {
                        //Ring slot around the owner so a swarm does not stack
                        //on one tile.
                        float slot = (Id % 8) * (MathF.PI * 2f / 8f);
                        Position target = new Position(
                            owner.Position.X + MathF.Cos(slot) * 2f,
                            owner.Position.Y + MathF.Sin(slot) * 2f);
                        if (Position.Distance(target) > 1f)
                            StepToward(target);
                    }
                    break;
                case BotMode.Wander:
                    _wanderStale++;
                    if (_wanderTarget == null || Position.Distance(_wanderTarget.Value) < 0.5f ||
                        _wanderStale > WanderRetargetTicks * 10)
                    {
                        _wanderTarget = PickWanderTarget();
                        _wanderStale = 0;
                    }
                    if (_wanderTarget != null)
                        StepToward(_wanderTarget.Value);
                    break;
                case BotMode.Stay:
                    break;
            }

            if (ShootEnabled && --_shootCooldown <= 0)
            {
                _shootCooldown = ShootCooldownTicks;
                TryBotShoot();
            }
        }

        private void StepToward(Position target)
        {
            float dx = target.X - Position.X;
            float dy = target.Y - Position.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.05f)
                return;
            float step = Math.Min(GetMovementSpeed() * Settings.MillisecondsPerTick, dist);
            ValidateAndMove(new Position(
                Position.X + dx / dist * step,
                Position.Y + dy / dist * step));
        }

        private Position? PickWanderTarget()
        {
            for (int i = 0; i < 8; i++)
            {
                float x = Position.X + (float)(_rand.NextDouble() * 16 - 8);
                float y = Position.Y + (float)(_rand.NextDouble() * 16 - 8);
                if (Parent != null && Parent.IsPassable((int)x, (int)y, true))
                    return new Position(x, y);
            }
            return null;
        }

        //Direct-damage analogue of Player.TryShoot: player bullets are
        //client-simulated, so bots (clientless by design) apply validated
        //weapon damage straight to the nearest enemy and broadcast the same
        //AllyShoot observers expect from real players.
        private void TryBotShoot()
        {
            ItemDesc weapon;
            try { weapon = GetItem(0); }
            catch { return; }
            if (weapon == null || weapon.Projectile == null)
                return;

            Enemy best = null;
            float bestD2 = ShootRange * ShootRange;
            foreach (Entity en in Parent.EntityChunks.HitTest(Position, ShootRange))
            {
                if (!(en is Enemy enemy) || enemy.Dead || enemy.Parent == null)
                    continue;
                //Same targeting rule as Player.TryHitEnemy: Character-class
                //props (sheep, pets, summoners, placeholders) resolve as
                //Enemy instances but are not hittable.
                if (!enemy.Desc.Enemy)
                    continue;
                if (enemy.HasConditionEffect(ConditionEffectIndex.Invincible) ||
                    enemy.HasConditionEffect(ConditionEffectIndex.Stasis))
                    continue;
                float dx = enemy.Position.X - Position.X;
                float dy = enemy.Position.Y - Position.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = enemy;
                }
            }
            if (best == null)
                return;

            float angle = MathF.Atan2(best.Position.Y - Position.Y, best.Position.X - Position.X);
            int numShots = weapon.NumProjectiles;
            float arcGap = weapon.ArcGap * MathUtils.ToRadians;
            float totalArc = arcGap * (numShots - 1);
            for (int i = 0; i < numShots; i++)
            {
                int damage = (int)(NextDamage(
                    weapon.Projectile.MinDamage, weapon.Projectile.MaxDamage,
                    ItemDatas[0]) * GetAttackMultiplier());
                Projectile proj = new Projectile(this, weapon.Projectile,
                    _projId--, Manager.TotalTime, angle - totalArc / 2f + arcGap * i,
                    Position, damage);
                if (!best.Dead && best.Parent != null)
                    best.HitByProjectile(proj);
            }
            FameStats.Shots += numShots;

            byte[] packet = GameServer.AllyShoot(Id, weapon.Type, angle);
            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
                if (en is Player player && !(player is Bot) && player.Client.Account.AllyShots)
                    player.Client.Send(packet);
        }

        private int NextDamage(int min, int max, int data)
        {
            float dmgMod = ItemDesc.GetStat(data, ItemData.Damage, ItemDesc.DamageMultiplier);
            return MathUtils.NextInt(min + (int)(min * dmgMod), max + (int)(max * dmgMod));
        }
    }

    //Owns every live bot. All mutations run on the main tick thread (chat
    //commands and timer actions drain before worlds tick in parallel), so
    //the lock only defends against world-worker reads of membership during
    //parallel ticks; per-bot brain fields are word-sized and lock-free.
    public static class BotManager
    {
        public const int MaxBotsPerOwner = 100;
        public const int MaxBotsTotal = 300;

        private static readonly object _lock = new object();
        private static readonly HashSet<Bot> _bots = new HashSet<Bot>();
        private static readonly Dictionary<int, HashSet<Bot>> _byOwner =
            new Dictionary<int, HashSet<Bot>>();
        private static int _seq;

        public static int Total
        {
            get { lock (_lock) return _bots.Count; }
        }

        public static int OwnedBy(int accountId)
        {
            lock (_lock)
                return _byOwner.TryGetValue(accountId, out HashSet<Bot> set) ? set.Count : 0;
        }

        public static List<Bot> SnapshotOwner(int accountId)
        {
            lock (_lock)
                return _byOwner.TryGetValue(accountId, out HashSet<Bot> set)
                    ? set.ToList() : new List<Bot>();
        }

        public static string Spawn(Player owner, int count, ushort? classType)
        {
            if (owner.Parent == null)
                return "You are not in a world.";
            if (count < 1)
                return "Usage: /bot spawn <count> [class]";
            ushort type = classType ?? owner.Type;
            if (!Resources.Type2Player.ContainsKey(type))
                return "Unknown player class.";
            lock (_lock)
            {
                if (_bots.Count + count > MaxBotsTotal)
                    return $"Bot cap reached ({MaxBotsTotal} total).";
                if (OwnedByLocked(owner.Client.Account.Id) + count > MaxBotsPerOwner)
                    return $"Bot cap reached ({MaxBotsPerOwner} per account).";
                int spawned = 0;
                for (int i = 0; i < count; i++)
                {
                    Bot bot;
                    try
                    {
                        bot = new Bot(type, ++_seq, owner);
                    }
                    catch
                    {
                        break;
                    }
                    if (owner.Parent.AddEntity(bot, SpawnNear(owner)) == -1)
                        break;
                    _bots.Add(bot);
                    if (!_byOwner.TryGetValue(bot.OwnerAccountId, out HashSet<Bot> set))
                        _byOwner[bot.OwnerAccountId] = set = new HashSet<Bot>();
                    set.Add(bot);
                    spawned++;
                }
                return spawned == count
                    ? $"{spawned} bot(s) spawned."
                    : $"{spawned}/{count} bot(s) spawned (world full?).";
            }
        }

        public static string Remove(Player owner, int count)
        {
            List<Bot> owned = SnapshotOwner(owner.Client.Account.Id);
            if (owned.Count == 0)
                return "You have no bots.";
            int n = Math.Min(count, owned.Count);
            for (int i = 0; i < n; i++)
                Destroy(owned[i]);
            return $"{n} bot(s) removed ({owned.Count - n} remain).";
        }

        public static string ClearAll()
        {
            List<Bot> all;
            lock (_lock) all = _bots.ToList();
            foreach (Bot bot in all)
                Destroy(bot);
            return $"{all.Count} bot(s) removed.";
        }

        public static string SetMode(Player owner, BotMode mode)
        {
            List<Bot> owned = SnapshotOwner(owner.Client.Account.Id);
            if (owned.Count == 0)
                return "You have no bots.";
            foreach (Bot bot in owned)
            {
                bot.Mode = mode;
                bot.Owner = owner;
            }
            return $"{owned.Count} bot(s) set to {mode.ToString().ToLower()}.";
        }

        public static string SetShoot(Player owner, bool on)
        {
            List<Bot> owned = SnapshotOwner(owner.Client.Account.Id);
            if (owned.Count == 0)
                return "You have no bots.";
            foreach (Bot bot in owned)
            {
                bot.ShootEnabled = on;
                bot.Owner = owner;
            }
            return on
                ? $"{owned.Count} bot(s) will shoot (engaging enemies within 10 tiles)."
                : $"{owned.Count} bot(s) will not shoot.";
        }

        //Brings every owned bot to the owner's side, crossing worlds.
        public static string Recall(Player owner)
        {
            if (owner.Parent == null)
                return "You are not in a world.";
            List<Bot> owned = SnapshotOwner(owner.Client.Account.Id);
            if (owned.Count == 0)
                return "You have no bots.";
            int moved = 0;
            foreach (Bot bot in owned)
            {
                bot.Owner = owner;
                if (bot.Parent == owner.Parent)
                {
                    if (bot.Teleport(owner.Position, ignoreSeen: true))
                        moved++;
                }
                else if (MoveBotToWorld(bot, owner.Parent, owner.Position))
                    moved++;
            }
            return $"{moved}/{owned.Count} bot(s) recalled.";
        }

        public static string Send(Player owner, World target)
        {
            if (target == null)
                return "Target world not found.";
            List<Bot> owned = SnapshotOwner(owner.Client.Account.Id);
            if (owned.Count == 0)
                return "You have no bots.";
            Position at = SpawnPointOf(target);
            int moved = 0;
            foreach (Bot bot in owned)
            {
                bot.Owner = owner;
                if (MoveBotToWorld(bot, target, at))
                    moved++;
            }
            return $"{moved}/{owned.Count} bot(s) sent to {target.GetDisplayName()}.";
        }

        //Quake path: bots cannot Reconnect (no socket), so they are carried
        //into the new world at its spawn. Runs on the main thread.
        public static void CarryInto(Bot bot, World target)
        {
            MoveBotToWorld(bot, target, SpawnPointOf(target));
        }

        //Home for Death recovery: the owner's side when shared, else the
        //current world's spawn.
        public static Position HomeFor(Bot bot)
        {
            Player owner = bot.Owner;
            if (owner != null && owner.Parent == bot.Parent && !owner.Dead)
                return owner.Position;
            return bot.Parent != null ? SpawnPointOf(bot.Parent) : new Position(0.5f, 0.5f);
        }

        public static string List(Player requester)
        {
            List<Bot> owned = SnapshotOwner(requester.Client.Account.Id);
            int total = Total;
            if (owned.Count == 0)
                return $"You have no bots. ({total} total)";
            var byWorld = owned.GroupBy(b => b.Parent != null
                    ? $"{b.Parent.GetDisplayName()} [{b.Parent.Id}]" : "(limbo)")
                .Select(g => $"{g.Key} x{g.Count()}");
            Bot sample = owned[0];
            return $"Your bots: {owned.Count} ({string.Join(", ", byWorld)}). " +
                $"Mode: {sample.Mode.ToString().ToLower()}, " +
                $"shoot: {(sample.ShootEnabled ? "on" : "off")}. ({total} total)";
        }

        public static World ResolveWorld(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return null;
            Player target = Manager.GetPlayer(arg.Trim());
            if (target != null && target.Parent != null)
                return target.Parent;
            if (int.TryParse(arg.Trim(), out int id))
            {
                lock (Manager.SyncRoot)
                    if (Manager.Worlds.TryGetValue(id, out World byId))
                        return byId;
            }
            lock (Manager.SyncRoot)
            {
                foreach (World world in Manager.Worlds.Values)
                    if (world.Name.Equals(arg.Trim(), StringComparison.InvariantCultureIgnoreCase) ||
                        (world.DisplayName != null &&
                         world.DisplayName.Equals(arg.Trim(), StringComparison.InvariantCultureIgnoreCase)))
                        return world;
            }
            return null;
        }

        private static int OwnedByLocked(int accountId)
        {
            return _byOwner.TryGetValue(accountId, out HashSet<Bot> set) ? set.Count : 0;
        }

        private static void Destroy(Bot bot)
        {
            lock (_lock)
            {
                _bots.Remove(bot);
                if (_byOwner.TryGetValue(bot.OwnerAccountId, out HashSet<Bot> set))
                {
                    set.Remove(bot);
                    if (set.Count == 0)
                        _byOwner.Remove(bot.OwnerAccountId);
                }
            }
            try { bot.Parent?.RemoveEntity(bot); }
            catch { }
        }

        //Cross-world move as copy-and-swap: the copy is added to the target
        //first, and the original is only removed (with its Id untouched, so
        //observers drop it cleanly) once the copy is live. Any failure
        //leaves the original exactly where it was: no limbo, no Id reuse.
        private static bool MoveBotToWorld(Bot bot, World target, Position at)
        {
            if (bot.Parent == null || target == null)
                return false;
            Bot fresh;
            try
            {
                fresh = new Bot(bot, NextSeq());
            }
            catch (Exception e)
            {
                Program.Print(PrintType.Error, $"Bot copy failed: {e.Message}");
                return false;
            }
            int id;
            try
            {
                id = target.AddEntity(fresh, at);
            }
            catch (Exception e)
            {
                Program.Print(PrintType.Error,
                    $"Bot move to <{target.GetDisplayName()}> failed: {e.Message}");
                return false;
            }
            if (id == -1)
                return false;
            lock (_lock)
            {
                _bots.Remove(bot);
                _bots.Add(fresh);
                if (_byOwner.TryGetValue(bot.OwnerAccountId, out HashSet<Bot> set))
                {
                    set.Remove(bot);
                    set.Add(fresh);
                }
            }
            try
            {
                bot.Parent?.RemoveEntity(bot);
            }
            catch (Exception e)
            {
                Program.Print(PrintType.Error, $"Bot retire failed: {e.Message}");
            }
            return true;
        }

        private static int NextSeq()
        {
            lock (_lock)
                return ++_seq;
        }

        private static Position SpawnNear(Player owner)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = (float)(i * Math.PI / 4);
                Position p = new Position(
                    owner.Position.X + MathF.Cos(a) * 2f,
                    owner.Position.Y + MathF.Sin(a) * 2f);
                if (owner.Parent.IsPassable((int)p.X, (int)p.Y, true))
                    return p;
            }
            return owner.Position;
        }

        private static Position SpawnPointOf(World world)
        {
            List<IntPoint> spawns = world.GetSpawnPoints();
            if (spawns.Count > 0)
                return new Position(spawns[0].X + 0.5f, spawns[0].Y + 0.5f);
            return new Position(world.Width / 2f, world.Height / 2f);
        }
    }
}
