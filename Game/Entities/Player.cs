using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Linq;

namespace RotMG.Game.Entities
{
    public partial class Player : Entity, IContainer
    {
        public const int MaxPotions = 6;
        //Client interact reach is 1 tile; 1.5 leaves a small latency buffer
        //and rejects UsePortal for any other portal id in the world.
        public const float MaxPortalInteractDistance = 1.5f;

        public static int[] Stars = 
        {
            20,
            150,
            400,
            800,
            2000
        };

        public Client Client;

        private int _accountId;
        public int AccountId
        {
            get { return _accountId; }
            set { TrySetSV(StatType.AccountId, _accountId = value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { TrySetSV(StatType.Name, _name = value); }
        }

        private bool _nameChosen;
        public bool NameChosen
        {
            get { return _nameChosen; }
            set { TrySetSV(StatType.NameChosen, (_nameChosen = value) ? 1 : 0); }
        }

        private int _exp;
        public int EXP
        {
            get { return _exp; }
            set { TrySetSV(StatType.EXP, _exp = value); }
        }

        private int _nextLevelExp;
        public int NextLevelEXP
        {
            get { return _nextLevelExp; }
            set { SetPrivateSV(StatType.NextLevelEXP, _nextLevelExp = value); }
        }

        private int _level;
        public int Level
        {
            get { return _level; }
            set { TrySetSV(StatType.Level, _level = value); }
        }

        private int _charFame;
        public int CharFame
        {
            get { return _charFame; }
            set { TrySetSV(StatType.CharFame, _charFame = value); }
        }

        private int _fame;
        public int Fame
        {
            get { return _fame; }
            set { SetPrivateSV(StatType.Fame, _fame = value); }
        }

        private int _nextClassQuestFame;
        public int NextClassQuestFame
        {
            get { return _nextClassQuestFame; }
            set { SetPrivateSV(StatType.NextClassQuestFame, _nextClassQuestFame = value); }
        }

        private int _numStars;
        public int NumStars
        {
            get { return _numStars; }
            set { TrySetSV(StatType.NumStars, _numStars = value); }
        }

        private string _guildName;
        public string GuildName
        {
            get { return _guildName; }
            set { TrySetSV(StatType.GuildName, _guildName = value); }
        }

        private int _guildRank;
        public int GuildRank
        {
            get { return _guildRank; }
            set { TrySetSV(StatType.GuildRank, _guildRank = value); }
        }

        private int _credits;
        public int Credits
        {
            get { return _credits; }
            set { SetPrivateSV(StatType.Credits, _credits = value); }
        }

        private int _tex1;
        public int Tex1
        {
            get { return _tex1; }
            set { TrySetSV(StatType.Tex1, _tex1 = value); }
        }

        private int _tex2;
        public int Tex2
        {
            get { return _tex2; }
            set { TrySetSV(StatType.Tex2, _tex2 = value); }
        }

        private int _skinType;
        public int SkinType
        {
            get { return _skinType; }
            set { TrySetSV(StatType.Texture, _skinType = value); }
        }

        private bool _hasBackpack;
        public bool HasBackpack
        {
            get { return _hasBackpack; }
            set { SetPrivateSV(StatType.HasBackpack, (_hasBackpack = value).GetHashCode()); }
        }

        private int _mp;
        public int MP
        {
            get { return _mp; }
            set { TrySetSV(StatType.MP, _mp = value); }
        }

        private int _maxMp;
        public int MaxMP
        {
            get { return _maxMp; }
            set { TrySetSV(StatType.MaxMP, _maxMp = value); }
        }

        private int _oxygen;
        public int Oxygen
        {
            get { return _oxygen; }
            set { SetPrivateSV(StatType.Breath, _oxygen = value); }
        }

        private int _healthPotions;
        public int HealthPotions
        {
            get { return _healthPotions; }
            set { SetPrivateSV(StatType.HealthPotionStack, _healthPotions = value); }
        }

        private int _magicPotions;
        public int MagicPotions
        {
            get { return _magicPotions; }
            set { SetPrivateSV(StatType.MagicPotionStack, _magicPotions = value); }
        }

        private int _sinkLevel;
        public int SinkLevel
        {
            get { return _sinkLevel; }
            set { TrySetSV(StatType.SinkLevel, _sinkLevel = value); }
        }

        public FameStatsInfo FameStats;

        //Attached vanity-pet object type (0 = none), persisted on the
        //character and mirrored from realm-src-master Player.PetId.
        public int PetId;
        public Pet Pet;

        //Subclass entry for server-side players without an account (see
        //Bot): initializes only the client-independent plumbing. The
        //subclass sets stats, inventory and the stub Client itself before
        //AddEntity/Init.
        protected Player(ushort type) : base(type)
        {
            PrivateSVs = new Dictionary<StatType, object>();
        }

        public Player(Client client) : base((ushort)client.Character.ClassType)
        {
            PrivateSVs = new Dictionary<StatType, object>();

            Client = client;
            HP = client.Character.HP;
            MP = client.Character.MP;
            AccountId = client.Account.Id;
            Name = client.Account.Name;
            NameChosen = !string.IsNullOrWhiteSpace(client.Account.Name);
            Level = client.Character.Level;

            if (client.Character.HealthPotions != 0) HealthPotions = client.Character.HealthPotions;
            if (client.Character.MagicPotions != 0) MagicPotions = client.Character.MagicPotions;
            if (client.Character.HasBackpack) HasBackpack = client.Character.HasBackpack;
            PetId = client.Character.PetId;
            if (client.Character.SkinType != 0) SkinType = client.Character.SkinType;
            if (client.Character.Size != 0) Size = client.Character.Size;
            if (client.Character.Tex1 != 0) Tex1 = client.Character.Tex1;
            if (client.Character.Tex2 != 0) Tex2 = client.Character.Tex2;
            if (client.Account.Stats.Credits != 0) Credits = client.Account.Stats.Credits;
            if (client.Account.Stats.Fame != 0) Fame = client.Account.Stats.Fame;

            if (!string.IsNullOrWhiteSpace(client.Account.GuildName))
            {
                GuildName = client.Account.GuildName;
                GuildRank = client.Account.GuildRank;
            }

            int stars = Database.GetStars(client.Account);
            if (stars != 0) NumStars = stars;

            if (Database.IsLegend(client.Account.Id))
                SetSV(StatType.LegendaryRank, 0);

            FameStats = client.Character.FameStats;
            InitInventory(client.Character);
            InitStats(client.Character);
            InitLevel(client.Character);

            RecalculateEquipBonuses();
        }

        //Reconnect in flight: inbound packets are already dropped
        //(Client.Active=false). Tick/Damage/Death must also ignore this
        //entity so a body left in-world cannot die after Reconnect.
        public bool IsTransferring => Client != null && Client.Reconnecting;

        //Leave the current world immediately, persist into Character, and
        //tell the client to open a new socket to target. Callers must not
        //send Reconnect or schedule Disconnect themselves.
        public bool BeginTransfer(World target)
        {
            if (target == null || Client == null || Dead || Client.Reconnecting)
                return false;

            CancelTradeIfTrading();
            if (Client.Character != null)
                SaveToCharacter();

            Client.BeginReconnect();
            if (Client.Account != null)
                Manager.RegisterPendingTransfer(Client.Account.Id, target);

            if (Parent != null)
                Parent.RemoveEntity(this);

            Client.Send(GameServer.Reconnect(target.Id));
            Client.ScheduleReconnectDisconnect();
            return true;
        }

        public virtual void SaveToCharacter()
        {
            Client.Character.HP = HP;
            Client.Character.MP = MP;
            Client.Character.Level = Level;
            Client.Character.HealthPotions = HealthPotions;
            Client.Character.MagicPotions = MagicPotions;
            Client.Character.HasBackpack = HasBackpack;
            Client.Character.PetId = PetId;
            Client.Character.SkinType = SkinType;
            Client.Character.Size = Size;
            Client.Character.Tex1 = Tex1;
            Client.Character.Tex2 = Tex2;
            Client.Character.Experience = EXP;
            Client.Character.Fame = CharFame;
            Client.Character.Inventory = Inventory.ToArray();
            Client.Character.ItemDatas = ItemDatas.ToArray();
            Client.Character.Stats = Stats.ToArray();
        }

        public override void Init()
        {
            base.Init();

            TileUpdates = new Dictionary<long, int>(1024);
            EntityUpdates = new Dictionary<int, int>();
            Entities = new HashSet<Entity>();
            _nearPlayerIds.Clear();
            CalculatedSightCircle = new HashSet<IntPoint>();
            AwaitingProjectiles = new Queue<AwaitingShots>();
            AckedProjectiles = new Dictionary<int, ProjectileAck>();
            ShotProjectiles = new Dictionary<int, Projectile>();
            AwaitingAoes = new Queue<AoeAck>();
            ShootAEs = new Queue<ushort>();
            AwaitingGoto = new Queue<AwaitingGotoWait>();

            SpeedHistory = new List<float>(SpeedHistoryCount);
            MultiplierHistory = new List<float>(SpeedHistoryCount);
            for (int i = 0; i < SpeedHistoryCount; i++) //Just make some temporary history when player is first initialized
                PushSpeedToHistory(GetMovementSpeed() * 1.5f, 1f);

            Client.Send(GameServer.AccountList(0, Client.Account.LockedIds));
            Client.Send(GameServer.AccountList(1, Client.Account.IgnoredIds));

            ApplyConditionEffect(ConditionEffectIndex.Invulnerable, 3000);
            ApplyConditionEffect(ConditionEffectIndex.Invisible, 3000);

            SpawnPetIfAttached();
        }

        //Spawns the attached vanity pet at the player's position,
        //mirroring realm-src-master Player.SpawnPetIfAttached: any active
        //pet is despawned first so re-using a generator (or re-entering a
        //world) never duplicates it. AddEntity runs Init after positioning,
        //so this is safe to call from Init on every world enter.
        public void SpawnPetIfAttached()
        {
            if (Pet != null)
            {
                if (Pet.Parent != null)
                    Pet.Parent.RemoveEntity(Pet);
                Pet = null;
            }

            if (PetId == 0 || Parent == null)
                return;

            if (!Resources.Type2Object.ContainsKey((ushort)PetId))
                return;

            Pet pet = new Pet((ushort)PetId, this);
            if (Parent.AddEntity(pet, Position) != -1)
                Pet = pet;
        }

        public void Heal(int amount, bool magic)
        {
            int heal = 0;
            if (magic)
            {
                int mp = MP;
                MP = Math.Max(1, Math.Min(GetStat(1), MP + amount));
                heal = MP - mp;
            }
            else
            {
                int hp = HP;
                HP = Math.Max(0, Math.Min(GetStat(0), HP + amount));
                heal = HP - hp;
            }

            if (heal <= 0) 
                return;

            byte[] notification = GameServer.Notification(Id, $"+{heal}", magic ? 0xff6084e0 : 0xff00ff00);
            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
            {
                if (en is Player player && 
                    (player.Client.Account.Notifications || player.Equals(this)))
                {
                    player.Client.Send(notification);
                }
            }
        }

        public virtual void Death(string killer)
        {
            //Must run before any Parent dereference: BeginTransfer removes
            //the entity (Parent == null) and a reconnecting character must
            //never be deleted after Reconnect has already gone out.
            if (IsTransferring || Parent == null)
                return;
#if DEBUG
            if (Parent.Name.Equals("Dreamland"))
                return;
#endif
            if (Dead) 
                return;

            Client.Active = false;
            Dead = true;
            CancelTradeIfTrading();

            SaveToCharacter();
            Database.Death(killer, Client.Account, Client.Character);

            byte[] death = GameServer.Death(Client.Account.Id, Client.Character.Id, killer);
            Client.Send(death);

            byte[] text = GameServer.Text("", 0, -1, 0, "", $"{Name} died at level {Level} killed by {killer}!");
            byte[] sound = GameServer.PlaySound("quack");

            foreach (Player p in Parent.Players.Values)
            {
                p.Client.Send(text);
                if (p.Client.Account.Sounds)
                    p.Client.Send(sound);
            }

            ushort type;
            int time;
            switch (GetMaxedStats())
            {
                case 8: type = 0x0735; time = 600000; break;
                case 7: type = 0x0734; time = 600000; break;
                case 6: type = 0x072b; time = 600000; break;
                case 5: type = 0x072a; time = 600000; break;
                case 4: type = 0x0729; time = 600000; break;
                case 3: type = 0x0728; time = 600000; break;
                case 2: type = 0x0727; time = 600000; break;
                case 1: type = 0x0726; time = 600000; break;
                default:
                    type = 0x0725; time = 300000;
                    if (Level < 20) { type = 0x0724; time = 60000; }
                    if (Level <= 1) { type = 0x0723; time = 30000; }
                    break;
            }

            Entity grave = new Entity(type, time);
            grave.TrySetSV(StatType.Name, Name);
            Parent.AddEntity(grave, Position);
            Parent.RemoveEntity(this);

            Manager.AddTimedAction(1500, () => 
            {
                Client.Disconnect();
            });
        }

        public bool Damage(string hitter, int damage, ConditionEffectDesc[] effects, bool pierces)
        {
            if (IsTransferring || Parent == null || Dead)
                return false;
#if DEBUG
            if (HasConditionEffect(ConditionEffectIndex.Invincible))
                throw new Exception("Entity should not be damaged if invincible");
            if (effects == null)
                throw new Exception("Null effects");
            if (string.IsNullOrWhiteSpace(hitter))
                throw new Exception("Undefined hitter");
#endif

            //Projectiles never have null effects. But other sources of damage might.
            foreach (ConditionEffectDesc eff in effects)
                ApplyConditionEffect(eff.Effect, eff.DurationMS);

            //Force pierce if armor broken
            if (HasConditionEffect(ConditionEffectIndex.ArmorBroken))
                pierces = true;

            //Calculate damage with defense
            int damageWithDefense = this.GetDefenseDamage(damage, Stats[3] + Boosts[3], pierces);

            //Nullify damage if invulnerable
            if (HasConditionEffect(ConditionEffectIndex.Invulnerable))
                damageWithDefense = 0;

            HP -= damageWithDefense;
            FameStats.DamageTaken += damageWithDefense;
            if (HP <= 0)
            {
                Death(hitter);
                return true;
            }

            byte[] packet = GameServer.Damage(Id, effects.Select(k => k.Effect).ToArray(), damageWithDefense);
            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
                if (en is Player player && player.Client.Account.AllyDamage && !player.Equals(this))
                    player.Client.Send(packet);
            return false;
        }

        public override void Tick()
        {
            if (IsTransferring)
                return;

            if (Client != null && Client.State == ProtocolState.Connected)
            {
                if (Client.IdleTimedOut(IdleTimeoutMS))
                {
                    Client.RequestDisconnect("Idle timeout");
                    return;
                }

                if (Manager.TotalTime % PingIntervalMS == 0)
                    Client.Send(GameServer.Ping(++_pingSerial));
            }

            if (Manager.TotalTime % 60000 == 0)
                FameStats.MinutesActive++;

            TickRegens();
            TickProjectiles();
            TickGotoAcks();
            //Standing players send no Moves, so the move-driven sweep
            //would never run for them: the tick sweep covers stationary
            //players with the same recording and expiry logic.
            SweepAckedProjectilesTick();
            CheckTradeTimeout();
            base.Tick();
        }

        public override string ToString()
        {
            return $"<{Name}> <{Parent.Name}:{Parent.Id}> <{Position.ToIntPoint()}>";
        }

        public override void Dispose()
        {
            try { CancelTradeIfTrading(); } catch { }
            TileUpdates = null;
            EntityUpdates.Clear();
            Entities.Clear();
            CalculatedSightCircle.Clear();
            AwaitingProjectiles.Clear();
            AckedProjectiles.Clear();
            _contactBullets.Clear();
            ShotProjectiles.Clear();
            AwaitingAoes.Clear();
            ShootAEs.Clear();
            AwaitingGoto.Clear();
            PrivateSVs.Clear();
            SpeedHistory.Clear();
            MultiplierHistory.Clear();
            base.Dispose();
        }

        //Headless stand-in for "nexus in a pack then UsePortal a distant
        //id": the body leaves the world before Reconnect, Death cannot
        //delete the character, and a far portal id is ignored.
        public static bool VerifyWorldTransfer()
        {
            World world = Manager.GetWorld(Manager.NexusId);
            if (world == null)
            {
                Program.Print(PrintType.Error, "P9 verify: Nexus world missing");
                return false;
            }

            List<IntPoint> spawns = world.GetSpawnPoints();
            Position spawn = spawns.Count > 0
                ? spawns[0].ToPosition()
                : world.GetRegion(Region.Spawn).ToPosition();

            Position far = default;
            bool foundFar = false;
            for (int x = 0; x < world.Width && !foundFar; x++)
                for (int y = 0; y < world.Height && !foundFar; y++)
                {
                    if (world.GetTile(x, y) == null)
                        continue;
                    Position p = new Position(x + 0.5f, y + 0.5f);
                    if (spawn.Distance(p) > 10f)
                    {
                        far = p;
                        foundFar = true;
                    }
                }
            if (!foundFar)
            {
                Program.Print(PrintType.Error, "P9 verify: no tile 10+ from spawn");
                return false;
            }

            Player player = CreateVerifyPlayer("P9NearDeath");
            if (world.AddEntity(player, spawn) == -1)
            {
                Program.Print(PrintType.Error, "P9 verify: failed to add player at spawn");
                return false;
            }
            player.HP = 5;
            player.Client.Character.HP = 999;

            Portal farPortal = new Portal(0x0712);
            if (world.AddEntity(farPortal, far) == -1)
            {
                Program.Print(PrintType.Error, "P9 verify: failed to add distant portal");
                world.RemoveEntity(player);
                return false;
            }
            float farDist = player.Position.Distance(farPortal);
            if (farDist <= MaxPortalInteractDistance)
            {
                Program.Print(PrintType.Error, $"P9 verify: distant portal is too close ({farDist:F2})");
                world.RemoveEntity(player);
                world.RemoveEntity(farPortal);
                return false;
            }

            byte[] usePortalBody = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(farPortal.Id));
            using (PacketReader rdr = new PacketReader(new MemoryStream(usePortalBody)))
                GameServer.UsePortal(player.Client, rdr);
            if (player.Parent != world || player.IsTransferring || !world.Players.ContainsKey(player.Id))
            {
                Program.Print(PrintType.Error, "P9 verify FAIL: distant UsePortal was accepted");
                player.Client.State = ProtocolState.Disconnected;
                if (player.Parent == world)
                    world.RemoveEntity(player);
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                return false;
            }
            Program.Print(PrintType.Info, $"P9 verify: distant UsePortal ignored (dist={farDist:F2})");

            Player nearPlayer = CreateVerifyPlayer("P9NearPortal");
            if (world.AddEntity(nearPlayer, spawn) == -1)
            {
                Program.Print(PrintType.Error, "P9 verify: failed to add nearby-portal player");
                world.RemoveEntity(player);
                world.RemoveEntity(farPortal);
                return false;
            }
            Portal nearPortal = new Portal(0x0712);
            if (world.AddEntity(nearPortal, spawn) == -1)
            {
                Program.Print(PrintType.Error, "P9 verify: failed to add nearby portal");
                world.RemoveEntity(player);
                world.RemoveEntity(nearPlayer);
                world.RemoveEntity(farPortal);
                return false;
            }
            GameServer.TryUsePortal(nearPlayer.Client, nearPortal.Id);
            if (nearPlayer.Parent != null || !nearPlayer.IsTransferring)
            {
                Program.Print(PrintType.Error, "P9 verify FAIL: in-range UsePortal did not transfer");
                nearPlayer.Client.State = ProtocolState.Disconnected;
                player.Client.State = ProtocolState.Disconnected;
                if (nearPlayer.Parent == world)
                    world.RemoveEntity(nearPlayer);
                world.RemoveEntity(player);
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                if (nearPortal.Parent == world)
                    world.RemoveEntity(nearPortal);
                return false;
            }
            nearPlayer.Client.State = ProtocolState.Disconnected;
            Program.Print(PrintType.Info, "P9 verify: in-range UsePortal removed the entity and sent Reconnect");

            if (!player.BeginTransfer(world))
            {
                Program.Print(PrintType.Error, "P9 verify FAIL: BeginTransfer returned false");
                player.Client.State = ProtocolState.Disconnected;
                if (player.Parent == world)
                    world.RemoveEntity(player);
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                return false;
            }
            if (player.Parent != null || world.Players.ContainsKey(player.Id) || !player.IsTransferring)
            {
                Program.Print(PrintType.Error, "P9 verify FAIL: player still in world after BeginTransfer");
                player.Client.State = ProtocolState.Disconnected;
                if (player.Parent == world)
                    world.RemoveEntity(player);
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                return false;
            }
            if (player.Client.Character.HP != 5)
            {
                Program.Print(PrintType.Error,
                    $"P9 verify FAIL: Character.HP={player.Client.Character.HP} after transfer, expected 5");
                player.Client.State = ProtocolState.Disconnected;
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                return false;
            }
            Program.Print(PrintType.Info, "P9 verify: BeginTransfer removed the body and saved HP=5");

            player.HP = 1;
            bool died = false;
            try
            {
                died = player.Damage("Oryx the Mad God", 10000, new ConditionEffectDesc[0], true);
                player.Death("Oryx the Mad God");
            }
            catch (Exception e)
            {
                Program.Print(PrintType.Error, $"P9 verify FAIL: Damage/Death threw {e}");
                player.Client.State = ProtocolState.Disconnected;
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                return false;
            }
            if (died || player.Dead || player.Client.Character.Dead)
            {
                Program.Print(PrintType.Error, "P9 verify FAIL: transferring player died after Reconnect");
                player.Client.State = ProtocolState.Disconnected;
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                return false;
            }

            try { player.Tick(); }
            catch (Exception e)
            {
                Program.Print(PrintType.Error, $"P9 verify FAIL: Tick threw after transfer ({e.Message})");
                player.Client.State = ProtocolState.Disconnected;
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                return false;
            }

            //Same persist rule as Client.FinishDisconnect: Parent == null
            //and Reconnecting means Character was already captured.
            if (player.Parent != null)
                player.SaveToCharacter();
            else if (!player.Client.Reconnecting)
                player.SaveToCharacter();
            if (player.Client.Character.HP != 5)
            {
                Program.Print(PrintType.Error,
                    $"P9 verify FAIL: disconnect persist overwrote Character.HP to {player.Client.Character.HP}");
                player.Client.State = ProtocolState.Disconnected;
                if (farPortal.Parent == world)
                    world.RemoveEntity(farPortal);
                return false;
            }

            player.Client.State = ProtocolState.Disconnected;
            if (farPortal.Parent == world)
                world.RemoveEntity(farPortal);
            if (nearPortal.Parent == world)
                world.RemoveEntity(nearPortal);
            Program.Print(PrintType.Info, "P9 verify: Death/Damage/Tick cannot kill after Reconnect; disconnect save kept Character.HP=5");
            return true;
        }

        //Headless stand-in for /size persistence and the quake warning:
        //Size must survive SaveToCharacter, world re-entry (a new Player
        //over the same Character) and a database Export/Load round-trip,
        //and the quake ShowEffect must serialize the exact id the client
        //shakes on (14: Earthquake in the reference, Jitter locally and
        //client-side).
        public static bool VerifySizeQuake()
        {
            Player player = CreateVerifyPlayer("SizeQuake");
            try
            {
                if (player.Size != 0 || player.Client.Character.Size != 0)
                {
                    Program.Print(PrintType.Error, "SizeQuake verify FAIL: fresh player/character Size is not 0");
                    return false;
                }

                player.Size = 200;
                player.SaveToCharacter();
                if (player.Client.Character.Size != 200)
                {
                    Program.Print(PrintType.Error, $"SizeQuake verify FAIL: Character.Size={player.Client.Character.Size} after save, expected 200");
                    return false;
                }
                Program.Print(PrintType.Info, "SizeQuake verify: SaveToCharacter stored Size=200");

                Player reentry = new Player(player.Client);
                if (reentry.Size != 200)
                {
                    Program.Print(PrintType.Error, $"SizeQuake verify FAIL: re-entry Size={reentry.Size}, expected 200");
                    return false;
                }
                Program.Print(PrintType.Info, "SizeQuake verify: world re-entry restored Size=200");

                XElement db = player.Client.Character.Export(false);
                CharacterModel reloaded = new CharacterModel(0, 0);
                reloaded.Data = db;
                reloaded.Load();
                if (reloaded.Size != 200)
                {
                    Program.Print(PrintType.Error, $"SizeQuake verify FAIL: database round-trip Size={reloaded.Size}, expected 200");
                    return false;
                }
                db.Element("Size")?.Remove();
                CharacterModel legacy = new CharacterModel(0, 0);
                legacy.Data = db;
                legacy.Load();
                if (legacy.Size != 0)
                {
                    Program.Print(PrintType.Error, $"SizeQuake verify FAIL: legacy row Size={legacy.Size}, expected 0");
                    return false;
                }
                Program.Print(PrintType.Info, "SizeQuake verify: database round-trip kept Size=200, legacy rows default 0");

                byte[] quake = GameServer.ShowEffect(ShowEffectIndex.Jitter, 7, 0);
                if (quake == null || quake.Length < 2 || quake[1] != 14)
                {
                    Program.Print(PrintType.Error, "SizeQuake verify FAIL: quake ShowEffect did not serialize effect id 14");
                    return false;
                }
                Program.Print(PrintType.Info, "SizeQuake verify: quake ShowEffect serializes effect id 14");
                return true;
            }
            catch (Exception e)
            {
                Program.Print(PrintType.Error, $"SizeQuake verify FAIL: threw {e.Message}");
                return false;
            }
            finally
            {
                player.Client.State = ProtocolState.Disconnected;
            }
        }

        private static Player CreateVerifyPlayer(string name)
        {
            PlayerDesc pdesc = Resources.Type2Player.Values.First();
            CharacterModel ch = new CharacterModel(0, 0)
            {
                ClassType = pdesc.Type,
                Level = 20,
                HP = 100,
                MP = 100,
                Stats = new int[8],
                Inventory = (int[])pdesc.Equipment.Clone(),
                ItemDatas = (int[])pdesc.ItemDatas.Clone(),
                FameStats = new FameStatsInfo()
            };
            for (int i = 0; i < 8; i++)
                ch.Stats[i] = pdesc.Stats[i].StartingValue;

            Client client = new Client(new SendState(), new ReceiveState());
            client.State = ProtocolState.Connected;
            client.Active = true;
            client.Account = new AccountModel()
            {
                Name = name,
                LockedIds = new List<int>(),
                IgnoredIds = new List<int>(),
                Stats = new StatsInfo { ClassStats = Database.CreateClassStats() }
            };
            client.Character = ch;
            Player created = new Player(client);
            client.Player = created;
            return created;
        }
    }
}
