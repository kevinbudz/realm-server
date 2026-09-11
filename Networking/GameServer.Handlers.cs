using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace RotMG.Networking
{
    public static partial class GameServer
    {
        //Wire values must match realm-client
        //(src/kabam/rotmg/messaging/impl/GameServerConnection.as). Do NOT
        //reorder or renumber: the client parses by raw byte value, so every
        //value is explicit and insertions must never shift later IDs.
        public enum PacketId
        {
            Failure = 0,
            CreateSuccess = 1,
            Create = 2,
            PlayerShoot = 3,
            Move = 4,
            PlayerText = 5,
            Text = 6,
            ServerPlayerShoot = 7,
            Damage = 8,
            Update = 9,
            Notification = 10,
            NewTick = 11,
            InvSwap = 12,
            UseItem = 13,
            ShowEffect = 14,
            Hello = 15,
            Goto = 16,
            InvDrop = 17,
            InvResult = 18,
            Reconnect = 19,
            MapInfo = 20,
            Load = 21,
            Teleport = 22,
            UsePortal = 23,
            Death = 24,
            Buy = 25,
            BuyResult = 26,
            Aoe = 27,
            PlayerHit = 28,
            EnemyHit = 29,
            AoeAck = 30,
            ShootAck = 31,
            SquareHit = 32,
            EditAccountList = 33,
            AccountList = 34,
            QuestObjId = 35,
            CreateGuild = 36,
            GuildResult = 37,
            GuildRemove = 38,
            GuildInvite = 39,
            AllyShoot = 40,
            EnemyShoot = 41,
            Escape = 42,
            InvitedToGuild = 43,
            JoinGuild = 44,
            ChangeGuildRank = 45,
            PlaySound = 46,
            Reskin = 47,
            GotoAck = 48,
            ChooseName = 49,
            NameResult = 50,
            RequestTrade = 51,
            TradeRequested = 52,
            TradeStart = 53,
            ChangeTrade = 54,
            TradeChanged = 55,
            AcceptTrade = 56,
            CancelTrade = 57,
            TradeDone = 58,
            TradeAccepted = 59,
            GlobalNotification = 60
        }

        public static void Read(Client client, int id, byte[] data)
        {
#if DEBUG
            Program.Print(PrintType.Debug, $"Packet received <{id}> <{string.Join(" ,",data.Select(k => k.ToString()).ToArray())}>");
#endif

            if (!client.Active)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Didn't process packet, client not active");
#endif
                return;
            }

            using (PacketReader rdr = new PacketReader(new MemoryStream(data)))
            {
                switch (id)
                {
                    case (int)PacketId.Hello:
                        Hello(client, rdr);
                        break;
                    case (int)PacketId.Create:
                        Create(client, rdr);
                        break;
                    case (int)PacketId.Load:
                        Load(client, rdr);
                        break;
                    case (int)PacketId.Move:
                        Move(client, rdr);
                        break;
                    case (int)PacketId.InvSwap:
                        InvSwap(client, rdr);
                        break;
                    case (int)PacketId.ShootAck:
                        ShootAck(client, rdr);
                        break;
                    case (int)PacketId.AoeAck:
                        AoeAck(client, rdr);
                        break;
                    case (int)PacketId.PlayerHit:
                        PlayerHit(client, rdr);
                        break;
                    case (int)PacketId.SquareHit:
                        SquareHit(client, rdr);
                        break;
                    case (int)PacketId.PlayerShoot:
                        PlayerShoot(client, rdr);
                        break;
                    case (int)PacketId.EnemyHit:
                        EnemyHit(client, rdr);
                        break;
                    case (int)PacketId.PlayerText:
                        PlayerText(client, rdr);
                        break;
                    case (int)PacketId.EditAccountList:
                        EditAccountList(client, rdr);
                        break;
                    case (int)PacketId.UseItem:
                        UseItem(client, rdr);
                        break;
                    case (int)PacketId.GotoAck:
                        GotoAck(client, rdr);
                        break;
                    case (int)PacketId.Escape:
                        Escape(client, rdr);
                        break;
                    case (int)PacketId.UsePortal:
                        UsePortal(client, rdr);
                        break;
                    case (int)PacketId.ChooseName:
                        ChooseName(client, rdr);
                        break;
                    case (int)PacketId.InvDrop:
                        InvDrop(client, rdr);
                        break;
                    case (int)PacketId.Teleport:
                        Teleport(client, rdr);
                        break;
                    case (int)PacketId.Buy:
                        Buy(client, rdr);
                        break;
                    case (int)PacketId.RequestTrade:
                        RequestTrade(client, rdr);
                        break;
                    case (int)PacketId.ChangeTrade:
                        ChangeTrade(client, rdr);
                        break;
                    case (int)PacketId.AcceptTrade:
                        AcceptTrade(client, rdr);
                        break;
                    case (int)PacketId.CancelTrade:
                        CancelTrade(client, rdr);
                        break;
                    case (int)PacketId.CreateGuild:
                        CreateGuild(client, rdr);
                        break;
                    case (int)PacketId.GuildRemove:
                        GuildRemove(client, rdr);
                        break;
                    case (int)PacketId.GuildInvite:
                        GuildInvite(client, rdr);
                        break;
                    case (int)PacketId.JoinGuild:
                        JoinGuild(client, rdr);
                        break;
                    case (int)PacketId.ChangeGuildRank:
                        ChangeGuildRank(client, rdr);
                        break;
                    case (int)PacketId.Reskin:
                        Reskin(client, rdr);
                        break;
                }
            }
        }

        public static void InvDrop(Client client, PacketReader rdr)
        {
            byte slot = rdr.ReadByte();
            client.Player.DropItem(slot);
        }

        public static void Escape(Client client, PacketReader rdr)
        {
            client.Active = false;
            client.Player.CancelTradeIfTrading();
            client.Player.FameStats.Escapes++;
            if (client.Player.HP <= 10)
                client.Player.FameStats.NearDeathEscapes++;
            client.Send(Reconnect(Manager.NexusId));
            Manager.AddTimedAction(2000, client.Disconnect);
        }

        public static void GotoAck(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32(); 
            client.Player.TryGotoAck(time);
        }

        public static void ChooseName(Client client, PacketReader rdr)
        {
            string name = rdr.ReadString();

            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;

            if (client.Account == null || string.IsNullOrWhiteSpace(client.Account.Name))
            {
                client.Send(NameResult(false, "Not registered."));
                return;
            }

            if (!Database.IsValidUsername(name))
            {
                client.Send(NameResult(false, "Invalid name."));
                return;
            }

            int existingId = Database.IdFromUsername(name);
            if (existingId != -1 && existingId != client.Account.Id)
            {
                client.Send(NameResult(false, "Name already taken."));
                return;
            }

            if (name == client.Account.Name)
            {
                client.Send(NameResult(true, ""));
                return;
            }

            const int price = 1000;
            if (client.Account.Stats.Credits < price)
            {
                client.Send(NameResult(false, "Not enough gold."));
                return;
            }

            client.Account.Stats.Credits -= price;
            //Old name key, new name keys and the account row commit as one
            //transaction: a crash between them used to orphan the account
            //under two names or none.
            Database.RenameAccountKeys(client.Account.Id, client.Account.Name, name, client.Account);

            AccountModel fresh = new AccountModel(client.Account.Id);
            fresh.Load();
            client.Account = fresh;

            player.Name = name;
            player.Credits = fresh.Stats.Credits;
            player.NameChosen = true;
            client.Send(NameResult(true, ""));
        }

        public static void UsePortal(Client client, PacketReader rdr)
        {
            int objectId = rdr.ReadInt32();

            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;

            Portal portal = player.Parent.GetEntity(objectId) as Portal;
            if (portal == null || !portal.Usable)
                return;

            //Drop instances reclaimed while empty (see Manager dungeon sweep).
            if (portal.WorldInstance != null && Manager.GetWorld(portal.WorldInstance.Id) != portal.WorldInstance)
                portal.WorldInstance = null;

            World world = portal.WorldInstance ?? ResolvePortalWorld(player, portal);
            if (world == null)
                return;

            client.Active = false;
            player.CancelTradeIfTrading();
            client.Send(Reconnect(world.Id));
            Manager.AddTimedAction(2000, client.Disconnect);
        }

        private static World ResolvePortalWorld(Player player, Portal portal)
        {
            switch (portal.Type)
            {
                case 0x0704: //Realm Portal
                case 0x070e: //Glowing Realm Portal
                    return Manager.GetWorld(Manager.RealmId);
                case 0x0703: //Portal of Cowardice
                case 0x070d: //Glowing Portal of Cowardice
                case 0x0712: //Nexus Portal
                case 0x071d: //Portal to Nexus
                    return Manager.GetWorld(Manager.NexusId);
                case 0x0720: //Vault Portal
                    return Manager.GetVaultWorld(player.Client);
                case 0x072f: //Guild Hall Portal
                    if (string.IsNullOrWhiteSpace(player.GuildName))
                    {
                        player.SendError("You are not in a guild.");
                        return null;
                    }
                    return Manager.GetGuildHallWorld(player.GuildName);
                default:
                    break;
            }

            //Key-unlocked portals remember their dungeon per instance.
            if (Manager.TryGetPortalDungeon(portal.Id, out string mappedName))
            {
                if (Resources.Worlds.TryGetValue(mappedName, out WorldDesc mapped) && Game.Dungeons.DungeonWorld.IsSupported(mapped))
                    return Manager.GetDungeonWorld(portal, mapped);
            }

            //Dungeon portals carry their dungeon in the object descriptor;
            //dungeons are Worlds.xml entries whose map comes from <Maps>.
            if (!string.IsNullOrWhiteSpace(portal.Desc.DungeonName))
            {
                if (!Resources.Worlds.TryGetValue(portal.Desc.DungeonName, out WorldDesc desc) || !Game.Dungeons.DungeonWorld.IsSupported(desc))
                {
                    player.SendInfo("Portal not implemented.");
                    return null;
                }
                return Manager.GetDungeonWorld(portal, desc);
            }

            player.SendInfo("Portal not implemented.");
            return null;
        }

        public static void UseItem(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            SlotData slot = new SlotData(rdr);
            Position usePos = new Position(rdr);
            client.Player.TryUseItem(time, slot, usePos);
        }

        public static void EditAccountList(Client client, PacketReader rdr)
        {
            int accountListId = rdr.ReadInt32();
            bool add = rdr.ReadBoolean();
            int objectId = rdr.ReadInt32();
            Entity en = client.Player.Parent.GetEntity(objectId);
            if (en != null && en is Player target) 
            {
                if (target.AccountId == client.Player.AccountId)
                    return;

                switch (accountListId)
                {
                    case 0: //Lock
                        if (add) client.Account.LockedIds.Add(target.AccountId);
                        else client.Account.LockedIds.Remove(target.AccountId);
                        client.Send(AccountList(0, client.Account.LockedIds));
                        break;
                    case 1: //Ignore
                        if (add) client.Account.IgnoredIds.Add(target.AccountId);
                        else client.Account.IgnoredIds.Remove(target.AccountId);
                        client.Send(AccountList(1, client.Account.IgnoredIds));
                        break;
                }
            }
        }

        public static void PlayerText(Client client, PacketReader rdr)
        {
            string text = rdr.ReadString(); 
            client.Player.Chat(text);
        }

        public static void EnemyHit(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            int bulletId = rdr.ReadInt32();
            int targetId = rdr.ReadInt32(); 
            client.Player.TryHitEnemy(time, bulletId, targetId);
        }

        public static void PlayerShoot(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            Position pos = new Position(rdr);
            float angle = rdr.ReadSingle();
            bool ability = rdr.ReadBoolean();
            byte numShots = rdr.PeekChar() != -1 ? rdr.ReadByte() : (byte)1; 
            client.Player.TryShoot(time, pos, angle, ability, numShots);
        }

        public static void SquareHit(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            int bulletId = rdr.ReadInt32(); 
            client.Player.TryHitSquare(time, bulletId);
        }

        public static void PlayerHit(Client client, PacketReader rdr)
        {
            int bulletId = rdr.ReadInt32(); 
            client.Player.TryHit(bulletId);
        }

        public static void ShootAck(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32(); 
            client.Player.TryShootAck(time);
        }

        public static void AoeAck(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            Position pos = new Position(rdr); 
            client.Player.TryAckAoe(time, pos);
        }

        public static void Hello(Client client, PacketReader rdr)
        {
            string buildVersion = rdr.ReadString();
            int gameId = rdr.ReadInt32();
            string username = rdr.ReadString();
            string password = rdr.ReadString();
            byte[] mapJson = rdr.ReadBytes(rdr.ReadInt32());

            if (client.State == ProtocolState.Handshaked) //Only allow Hello to be processed once.
            {
                AccountModel acc = Database.Verify(username, password, client.IP);
                if (acc == null)
                {
                    client.Send(Failure(0, "Invalid account."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                if (acc.Banned)
                {
                    client.Send(Failure(0, "Banned."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                if (!acc.Ranked && gameId == Manager.EditorId)
                {
                    client.Send(Failure(0, "Not ranked."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                }

                Manager.GetClient(acc.Id)?.Disconnect();

                if (Database.IsAccountInUse(acc))
                {
                    client.Send(Failure(0, "Account in use!"));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                client.Account = acc;
                client.Account.Connected = true;
                client.Account.Save();
                client.TargetWorldId = gameId;

                Manager.LinkClient(client.Account.Id, client.Id);
                World world = Manager.GetWorld(gameId);

#if DEBUG
                if (client.TargetWorldId == Manager.EditorId)
                {
                    Program.Print(PrintType.Debug, "Loading editor world");
                    JSMap map = new JSMap(Encoding.UTF8.GetString(mapJson));
                    world = new World(map, Resources.Worlds["Dreamland"]);
                    client.TargetWorldId = Manager.AddWorld(world);
                }
#endif

                if (world == null)
                {
                    client.Send(Failure(0, "Invalid world!"));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                if (world is RealmWorld && !world.AllowedAccess(client))
                {
                    client.Send(Failure(0, "Realm closed."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }

                uint seed = (uint)MathUtils.NextInt(1, int.MaxValue - 1);
                client.Random = new wRandom(seed);
                client.Send(MapInfo(world.Width, world.Height, world.Name, world.GetDisplayName(), seed, world.Background, world.ShowDisplays, world.AllowTeleport));
                client.State = ProtocolState.Awaiting; //Allow the processing of Load/Create.
            }
        }

        public static void Create(Client client, PacketReader rdr)
        {
            int classType = rdr.ReadInt16();
            int skinType = rdr.ReadInt16();

            if (client.State == ProtocolState.Awaiting)
            {
                CharacterModel character = Database.CreateCharacter(client.Account, classType, skinType);
                if (character == null)
                {
                    client.Send(Failure(0, "Failed to create character."));
                    client.Disconnect();
                    return;
                }

                World world = Manager.GetWorld(client.TargetWorldId);
                if (world is RealmWorld && !world.AllowedAccess(client))
                {
                    client.Send(Failure(0, "Realm closed."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }
                client.Character = character;
                client.Player = new Player(client);
                client.State = ProtocolState.Connected;
                client.Send(CreateSuccess(world.AddEntity(client.Player, PickSpawnPosition(world)), client.Character.Id));
            }
        }

        //Join position from the world's spawn list, mirroring
        //realm-src-master Player.Init (random entry of GetSpawnPoints).
        //CastleWorld narrows that list by raid size so small raids share
        //one tile and large raids fan out.
        private static Position PickSpawnPosition(World world)
        {
            List<IntPoint> spawns = world.GetSpawnPoints();
            if (spawns.Count > 0)
                return spawns[MathUtils.Next(spawns.Count)].ToPosition();
            return world.GetRegion(Region.Spawn).ToPosition();
        }

        public static void Load(Client client, PacketReader rdr)
        {
            int charId = rdr.ReadInt32();

            if (client.State == ProtocolState.Awaiting)
            {
                CharacterModel character = Database.LoadCharacter(client.Account, charId);
                if (character.IsNull || character.Dead || character.Deleted)
                {
                    client.Send(Failure(0, "Failed to load character."));
                    client.Disconnect();
                    return;
                }

                World world = Manager.GetWorld(client.TargetWorldId);
                if (world is RealmWorld && !world.AllowedAccess(client))
                {
                    client.Send(Failure(0, "Realm closed."));
                    Manager.AddTimedAction(1000, client.Disconnect);
                    return;
                }
                client.Character = character;
                client.Player = new Player(client);
                client.State = ProtocolState.Connected;
                client.Send(CreateSuccess(world.AddEntity(client.Player, PickSpawnPosition(world)), client.Character.Id));
            }
        }

        public static void Move(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            Position position = new Position(rdr); 
            client.Player.TryMove(time, position);
        }

        public static void InvSwap(Client client, PacketReader rdr)
        {
            int time = rdr.ReadInt32();
            Position position = new Position(rdr);
            SlotData slot1 = new SlotData(rdr);
            SlotData slot2 = new SlotData(rdr); 
            client.Player.SwapItem(slot1, slot2);
        }

        public static int Write(Client client, byte[] buffer, int offset, byte[] packet)
        {
            int length = packet.Length + 5;
            buffer[offset] = (byte)(length >> 24);
            buffer[offset + 1] = (byte)(length >> 16);
            buffer[offset + 2] = (byte)(length >> 8);
            buffer[offset + 3] = (byte)length;
            Buffer.BlockCopy(packet, 0, buffer, offset + 4, packet.Length);
            return length;
        }

        public static byte[] MapInfo(int width, int height, string name, string displayName, uint seed, int background, bool showDisplays, bool allowPlayerTeleport)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.MapInfo);
            wtr.Write(width);
            wtr.Write(height);
            wtr.Write(name);
            wtr.Write(displayName);
            wtr.Write(seed);
            wtr.Write(background);
            wtr.Write(showDisplays);
            wtr.Write(allowPlayerTeleport);
            return PacketWriter.RentedBytes();
        }

        public static byte[] InvResult(int result)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.InvResult);
            wtr.Write(result);
            return PacketWriter.RentedBytes();
        }

        public static byte[] Failure(int errorId, string description)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Failure);
            wtr.Write(errorId);
            wtr.Write(description);
            return PacketWriter.RentedBytes();
        }

        public static byte[] NameResult(bool success, string errorText)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.NameResult);
            wtr.Write(success);
            wtr.Write(errorText);
            return PacketWriter.RentedBytes();
        }

        public static byte[] CreateSuccess(int objectId, int charId)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.CreateSuccess);
            wtr.Write(objectId);
            wtr.Write(charId);
            return PacketWriter.RentedBytes();
        }

        public static byte[] Update(List<TileData> tiles, List<ObjectDefinition> adds, List<ObjectDrop> drops)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Update);
            wtr.Write((short)tiles.Count);
            foreach (TileData k in tiles)
                k.Write(wtr);

            wtr.Write((short)adds.Count);
            foreach (ObjectDefinition k in adds)
                k.Write(wtr);

            wtr.Write((short)drops.Count);
            foreach (ObjectDrop k in drops)
                k.Write(wtr);

            return PacketWriter.RentedBytes();
        }

        public static byte[] NewTick(List<ObjectStatus> statuses, Dictionary<StatType, object> playerStats)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.NewTick);
            wtr.Write((short)statuses.Count);
            foreach (ObjectStatus k in statuses)
                k.Write(wtr);
            if (playerStats.Count > 0)
            {
                wtr.Write((byte)playerStats.Count);
                foreach (KeyValuePair<StatType, object> k in playerStats)
                {
                    wtr.Write((byte)k.Key);
                    if (ObjectStatus.IsStringStat(k.Key))
                        wtr.Write((string)k.Value);
                    else
                        wtr.Write((int)k.Value);
                }
            }
            return PacketWriter.RentedBytes();
        }

        public static byte[] EnemyShoot(int bulletId, int ownerId, byte bulletType, Position startPos, float angle, short damage, byte numShots, float angleInc)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.EnemyShoot);
            wtr.Write(bulletId);
            wtr.Write(ownerId);
            wtr.Write(bulletType);
            startPos.Write(wtr);
            wtr.Write(angle);
            wtr.Write(damage);
            if (numShots > 1)
            {
                wtr.Write(numShots);
                wtr.Write(angleInc);
            }
            return PacketWriter.RentedBytes();
        }

        public static byte[] ShowEffect(ShowEffectIndex effect, int targetObjectId, uint color, Position pos1 = new Position(), Position pos2 = new Position())
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.ShowEffect);
            wtr.Write((byte)effect);
            wtr.Write(targetObjectId);
            wtr.Write((int)color);
            pos1.Write(wtr);
            if (pos2.X != 0 || pos2.Y != 0)
                pos2.Write(wtr);
            return PacketWriter.RentedBytes();
        }

        //Throw visual whose particle flies for travelTime ms. The client flies
        //1500ms unless pos2 carries another duration, so the default sends
        //the legacy layout and only custom times add the second position.
        public static byte[] ThrowEffect(int targetObjectId, uint color, Position target, int travelTime)
        {
            Position duration = travelTime == 1500 ? new Position() : new Position(travelTime, 0);
            return ShowEffect(ShowEffectIndex.Throw, targetObjectId, color, target, duration);
        }

        public static byte[] Goto(int objectId, Position pos)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Goto);
            wtr.Write(objectId);
            pos.Write(wtr);
            return PacketWriter.RentedBytes();
        }

        public static byte[] Aoe(Position pos, float radius, int damage, ConditionEffectIndex effect, uint color)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Aoe);
            pos.Write(wtr);
            wtr.Write(radius);
            wtr.Write((short)damage);
            wtr.Write((byte)effect);
            wtr.Write((int)color);
            return PacketWriter.RentedBytes();
        }

        public static byte[] Damage(int targetId, ConditionEffectIndex[] effects, int damage)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Damage);
            wtr.Write(targetId);
            wtr.Write((byte)effects.Length);
            for (int i = 0; i < effects.Length; i++)
                wtr.Write((byte)(effects[i]));
            wtr.Write((ushort)damage);
            return PacketWriter.RentedBytes();
        }

        public static byte[] Death(int accountId, int charId, string killer)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Death);
            wtr.Write(accountId);
            wtr.Write(charId);
            wtr.Write(killer);
            return PacketWriter.RentedBytes();
        }

        public static byte[] AllyShoot(int ownerId, int containerType, float angle)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.AllyShoot);
            wtr.Write(ownerId);
            wtr.Write((short)containerType);
            wtr.Write(angle);
            return PacketWriter.RentedBytes();
        }
        
        public static byte[] PlaySound(string sound)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.PlaySound);
            wtr.Write(sound);
            return PacketWriter.RentedBytes();
        }

        public static byte[] Text(string name, int objectId, int numStars, int bubbleTime, string recipent, string text)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Text);
            wtr.Write(name);
            wtr.Write(objectId);
            wtr.Write(numStars);
            wtr.Write((byte)bubbleTime);
            wtr.Write(recipent);
            wtr.Write(text);
            return PacketWriter.RentedBytes();
        }

        public static byte[] AccountList(int accountListId, List<int> accountIds)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.AccountList);
            wtr.Write(accountListId);
            wtr.Write((short)accountIds.Count);
            for (int i = 0; i < accountIds.Count; i++)
                wtr.Write(accountIds[i]);
            return PacketWriter.RentedBytes();
        }

        public static byte[] ServerPlayerShoot(int bulletId, int ownerId, int containerType, Position startPos, float angle, float angleInc, List<Projectile> projs)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.ServerPlayerShoot);
            wtr.Write(bulletId);
            wtr.Write(ownerId);
            wtr.Write((short)containerType);
            startPos.Write(wtr);
            wtr.Write(angle);
            wtr.Write(angleInc);
            wtr.Write((byte)projs.Count);
            for (int i = 0; i < projs.Count; i++)
                wtr.Write((short)projs[i].Damage);
            return PacketWriter.RentedBytes();
        }

        public static byte[] Reconnect(int gameId)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Reconnect);
            wtr.Write(gameId);
            return PacketWriter.RentedBytes();
        }

        public static byte[] Notification(int objectId, string text, uint color)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Notification);
            wtr.Write(objectId);
            wtr.Write(text);
            wtr.Write((int)color);
            return PacketWriter.RentedBytes();
        }

        //Davy Jones key HUD channel (see KeysView in realm-client).
        //Payload mirrors the reference implementation: int type, UTF string.
        public static byte[] GlobalNotification(int type, string text)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.GlobalNotification);
            wtr.Write(type);
            wtr.Write(text);
            return PacketWriter.RentedBytes();
        }

        public static byte[] PolicyFile = _policyFile();
        static byte[] _policyFile()
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.WriteNullTerminatedString(
                @"<cross-domain-policy>" +
                @"<allow-access-from domain=""*"" to-ports=""*"" />" +
                @"</cross-domain-policy>");
            wtr.Write((byte)'\r');
            wtr.Write((byte)'\n');
            return PacketWriter.RentedBytes();
        }
    }
}
