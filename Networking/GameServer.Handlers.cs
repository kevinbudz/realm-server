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
            GlobalNotification = 60,
            Ping = 61,
            Pong = 62
        }

        //Must match realm-client incoming/Failure.as. 0 is informational
        //(chat only); 1/2 close the game; 3 is a teleport reject.
        public const int FailureDefault = 0;
        public const int FailureIncorrectVersion = 1;
        public const int FailureForceCloseGame = 2;
        public const int FailureInvalidTeleportTarget = 3;

        //[W06] data is the frame body: a span over the pooled receive array, valid for this call only.
        public static void Read(Client client, int id, ReadOnlySpan<byte> data)
        {
#if DEBUG
            Program.Print(PrintType.Debug, $"Packet received <{id}> sendQueue={client.PendingCount} packets/{client.PendingBytes}B <{string.Join(" ,", DebugBytes(data))}>");
#endif

            if (!client.Active || client.Closing)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Didn't process packet, client not active");
#endif
                return;
            }

            //Handshake window: a fresh connection has no Player until
            //Load/Create runs, but stale client packets (a hit queued
            //before a portal transfer, a shoot sent before MapInfo) can
            //arrive first. Every game handler below dereferences
            //client.Player; drop those instead of NRE-ing into a
            //disconnect (this turned nexus keybinds into disconnects).
            //Pong stays allowed so keepalives keep working mid-handshake.
            if (client.Player == null)
            {
                switch (id)
                {
                case (int)PacketId.Hello:
                case (int)PacketId.Load:
                case (int)PacketId.Create:
                case (int)PacketId.Pong:
                    break;
                default:
                    return;
                }
            }

            {
                SpanReader rdr = new SpanReader(data); //[W06]
                //No handler below length-checks its reads, so a short body
                //(wrong client build, crafted frame) threw EndOfStreamException
                //out of Read into Client.Tick, which disconnected the client
                //with a stack-trace print. Drop the packet instead; this one
                //guard covers every current and future handler.
                try
                {
                    switch (id)
                    {
                    case (int)PacketId.Hello:
                        Hello(client, ref rdr);
                        break;
                    case (int)PacketId.Create:
                        Create(client, ref rdr);
                        break;
                    case (int)PacketId.Load:
                        Load(client, ref rdr);
                        break;
                    case (int)PacketId.Move:
                        Move(client, ref rdr);
                        break;
                    case (int)PacketId.InvSwap:
                        InvSwap(client, ref rdr);
                        break;
                    case (int)PacketId.ShootAck:
                        ShootAck(client, ref rdr);
                        break;
                    case (int)PacketId.AoeAck:
                        AoeAck(client, ref rdr);
                        break;
                    case (int)PacketId.PlayerHit:
                        PlayerHit(client, ref rdr);
                        break;
                    case (int)PacketId.SquareHit:
                        SquareHit(client, ref rdr);
                        break;
                    case (int)PacketId.PlayerShoot:
                        PlayerShoot(client, ref rdr);
                        break;
                    case (int)PacketId.EnemyHit:
                        EnemyHit(client, ref rdr);
                        break;
                    case (int)PacketId.PlayerText:
                        PlayerText(client, ref rdr);
                        break;
                    case (int)PacketId.EditAccountList:
                        EditAccountList(client, ref rdr);
                        break;
                    case (int)PacketId.UseItem:
                        UseItem(client, ref rdr);
                        break;
                    case (int)PacketId.GotoAck:
                        GotoAck(client, ref rdr);
                        break;
                    case (int)PacketId.Escape:
                        Escape(client, ref rdr);
                        break;
                    case (int)PacketId.UsePortal:
                        UsePortal(client, ref rdr);
                        break;
                    case (int)PacketId.ChooseName:
                        ChooseName(client, ref rdr);
                        break;
                    case (int)PacketId.InvDrop:
                        InvDrop(client, ref rdr);
                        break;
                    case (int)PacketId.Teleport:
                        Teleport(client, ref rdr);
                        break;
                    case (int)PacketId.Buy:
                        Buy(client, ref rdr);
                        break;
                    case (int)PacketId.RequestTrade:
                        RequestTrade(client, ref rdr);
                        break;
                    case (int)PacketId.ChangeTrade:
                        ChangeTrade(client, ref rdr);
                        break;
                    case (int)PacketId.AcceptTrade:
                        AcceptTrade(client, ref rdr);
                        break;
                    case (int)PacketId.CancelTrade:
                        CancelTrade(client, ref rdr);
                        break;
                    case (int)PacketId.CreateGuild:
                        CreateGuild(client, ref rdr);
                        break;
                    case (int)PacketId.GuildRemove:
                        GuildRemove(client, ref rdr);
                        break;
                    case (int)PacketId.GuildInvite:
                        GuildInvite(client, ref rdr);
                        break;
                    case (int)PacketId.JoinGuild:
                        JoinGuild(client, ref rdr);
                        break;
                    case (int)PacketId.ChangeGuildRank:
                        ChangeGuildRank(client, ref rdr);
                        break;
                    case (int)PacketId.Reskin:
                        Reskin(client, ref rdr);
                        break;
                    case (int)PacketId.Pong:
                        Pong(client, ref rdr);
                        break;
                    }
                }
                catch (EndOfStreamException)
                {
#if DEBUG
                    Program.Print(PrintType.Error, $"Dropped truncated packet <{id}> ({data.Length}B)");
#endif
                }
                catch (IOException)
                {
#if DEBUG
                    Program.Print(PrintType.Error, $"Dropped unreadable packet <{id}> ({data.Length}B)");
#endif
                }
                catch (MalformedPacketException e)
                {
                    //[W06] A length prefix is negative or runs past the frame: the frame is malformed, so this client is
                    //disconnected. RequestDisconnect is per client; other clients are not affected.
                    client.RequestDisconnect($"malformed packet <{id}>: {e.Message}");
                }
            }
        }

#if DEBUG
        private static string[] DebugBytes(ReadOnlySpan<byte> data)
        {
            string[] parts = new string[data.Length];
            for (int i = 0; i < data.Length; i++)
                parts[i] = data[i].ToString();
            return parts;
        }
#endif

        public static void InvDrop(Client client, ref SpanReader rdr)
        {
            byte slot = rdr.ReadByte();
            client.Player.DropItem(slot);
        }

        public static void Escape(Client client, ref SpanReader rdr)
        {
            long hopT0 = HopTrace.Enabled ? HopTrace.Now() : 0; //[hop]
            Player player = client.Player;
            if (player == null || player.Parent == null || player.IsTransferring)
                return;

            player.FameStats.Escapes++;
            if (player.HP <= 10)
                player.FameStats.NearDeathEscapes++;
            World nexus = Manager.GetWorld(Manager.NexusId);
            if (HopTrace.Enabled) HopTrace.EscapeRx(client, hopT0); //[hop]
            player.BeginTransfer(nexus);
            if (HopTrace.Enabled) HopTrace.Mark(client.HopAccount, "BeginTransfer returned (Reconnect queued)"); //[hop]
        }

        public static void GotoAck(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32(); 
            client.Player.TryGotoAck(time);
        }

        //Keepalive reply. Serial/time are consumed so the stream stays
        //aligned; liveness is recorded in Client.DrainInbound for every
        //inbound packet (this one included).
        public static void Pong(Client client, ref SpanReader rdr)
        {
            rdr.ReadInt32();
            rdr.ReadInt32();
        }

        public static void ChooseName(Client client, ref SpanReader rdr)
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

            //The debit is taken now, so a second request sees it. The name, the reply and the entity update wait for
            //the commit (one bundle: old key removal, new keys, account row). A failed commit refunds the debit.
            AccountModel acc = client.Account;
            string oldName = acc.Name;
            acc.Stats.Credits -= price;
            Database.RenameAccountKeys(acc.Id, oldName, name, acc, committed =>
            {
                if (committed)
                {
                    acc.Name = name;
                    player.Name = name;
                    player.Credits = acc.Stats.Credits;
                    player.NameChosen = true;
                    if (client.Active)
                        client.Send(NameResult(true, ""));
                }
                else
                {
                    acc.Stats.Credits += price;
                    if (client.Active)
                        client.Send(NameResult(false, "Save failed."));
                }
            });
        }

        public static void UsePortal(Client client, ref SpanReader rdr)
        {
            int objectId = rdr.ReadInt32();
            TryUsePortal(client, objectId);
        }

        //Distance-checked before ResolvePortalWorld so a crafted distant
        //id cannot spawn a dungeon.
        public static void TryUsePortal(Client client, int objectId)
        {
            Player player = client.Player;
            if (player == null || player.Parent == null || player.IsTransferring)
                return;

            Portal portal = player.Parent.GetEntity(objectId) as Portal;
            if (portal == null || !portal.Usable)
                return;

            float dist = player.Position.Distance(portal);
            if (dist > Player.MaxPortalInteractDistance)
            {
                Program.Print(PrintType.Error,
                    $"UsePortal rejected: {player.Name} (acc {client.Account?.Id}) too far from portal {objectId} " +
                    $"at {portal.Position} (player {player.Position}, dist={dist:F2})");
                return;
            }

            //Drop instances reclaimed while empty (see Manager dungeon sweep).
            if (portal.WorldInstance != null && Manager.GetWorld(portal.WorldInstance.Id) != portal.WorldInstance)
                portal.WorldInstance = null;

            World world = portal.WorldInstance ?? ResolvePortalWorld(player, portal);
            if (world == null)
                return;

            player.BeginTransfer(world);
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
                    return GetDungeonWorldOrNull(player, portal, mapped);
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
                return GetDungeonWorldOrNull(player, portal, desc);
            }

            player.SendInfo("Portal not implemented.");
            return null;
        }

        //A generated dungeon can exceed its work budget on every seed (DungeonGen.DungeonGenerationException).
        //The player stays where they are and is told, instead of the packet being dropped by the handler guard.
        private static World GetDungeonWorldOrNull(Player player, Portal portal, WorldDesc desc)
        {
            try
            {
                return Manager.GetDungeonWorld(portal, desc);
            }
            catch (Game.DungeonGen.DungeonGenerationException e)
            {
                Program.Print(PrintType.Error, $"Dungeon '{desc.Id}' failed to generate for portal {portal.Id}: {e.Message}");
                player.SendError("This dungeon could not be generated. Please try again.");
                return null;
            }
        }

        public static void UseItem(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32();
            SlotData slot = new SlotData(ref rdr);
            Position usePos = new Position(ref rdr);
            client.Player.TryUseItem(time, slot, usePos);
        }

        public static void EditAccountList(Client client, ref SpanReader rdr)
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
                        if (add) AddAccountListId(client, client.Account.LockedIds, target.AccountId);
                        else client.Account.LockedIds.RemoveAll(id => id == target.AccountId);
                        client.Send(AccountList(0, client.Account.LockedIds));
                        break;
                    case 1: //Ignore
                        if (add) AddAccountListId(client, client.Account.IgnoredIds, target.AccountId);
                        else client.Account.IgnoredIds.RemoveAll(id => id == target.AccountId);
                        client.Send(AccountList(1, client.Account.IgnoredIds));
                        break;
                }
            }
        }

        //Lock and ignore lists never hold duplicates (an add of an id already present is a no-op), so one
        //unlock always clears the id. Removal drops every copy, which also cleans up duplicates stored
        //before this check. The cap is the AccountList wire limit: its count is written as a signed short
        //(AccountList below), so a longer list would be sent with a negative count. The client keeps no
        //limit of its own (realm-cpp Party.cpp uses an unordered set), so this is the only bound on the list.
        public const int MaxAccountListEntries = short.MaxValue;

        private static void AddAccountListId(Client client, List<int> ids, int accountId)
        {
            if (ids.Contains(accountId))
                return;
            if (ids.Count >= MaxAccountListEntries)
            {
                client.Player.SendError("Your list is full.");
                return;
            }
            ids.Add(accountId);
        }

        public static void PlayerText(Client client, ref SpanReader rdr)
        {
            string text = rdr.ReadString(); 
            client.Player.Chat(text);
        }

        public static void EnemyHit(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32();
            int bulletId = rdr.ReadInt32();
            int targetId = rdr.ReadInt32(); 
            client.Player.TryHitEnemy(time, bulletId, targetId);
        }

        public static void PlayerShoot(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32();
            Position pos = new Position(ref rdr);
            float angle = rdr.ReadSingle();
            bool ability = rdr.ReadBoolean();
            byte numShots = rdr.HasMore ? rdr.ReadByte() : (byte)1; 
            client.Player.TryShoot(time, pos, angle, ability, numShots);
        }

        public static void SquareHit(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32();
            int bulletId = rdr.ReadInt32(); 
            client.Player.TryHitSquare(time, bulletId);
        }

        public static void PlayerHit(Client client, ref SpanReader rdr)
        {
            int bulletId = rdr.ReadInt32(); 
            client.Player.TryHit(bulletId);
        }

        public static void ShootAck(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32(); 
            client.Player.TryShootAck(time);
        }

        public static void AoeAck(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32();
            Position pos = new Position(ref rdr); 
            client.Player.TryAckAoe(time, pos);
        }

        public static void Hello(Client client, ref SpanReader rdr)
        {
            if (HopTrace.Enabled) HopTrace.Mark(client.HopAccount, "Hello rx"); //[hop]
            string buildVersion = rdr.ReadString();
            int gameId = rdr.ReadInt32();
            string username = rdr.ReadString();
            string password = rdr.ReadString();
            //Pre-auth and attacker-controlled: ReadBytes(n) allocates n bytes before reading, so a
            //small frame naming a 2 GB body would allocate 2 GB. Reject negatives, anything above the
            //server's frame cap (GameServer.BufferSize), and lengths past the bytes left in this packet.
            int mapJsonLength = rdr.ReadInt32();
            long mapJsonRemaining = rdr.Remaining;
            if (mapJsonLength < 0 || mapJsonLength > GameServer.BufferSize || mapJsonLength > mapJsonRemaining)
                return;
            byte[] mapJson = rdr.ReadBytes(mapJsonLength);

            if (client.State == ProtocolState.Handshaked) //Only allow Hello to be processed once.
            {
                if (buildVersion != Settings.BuildVersion)
                {
                    client.SendFailureAndClose(FailureIncorrectVersion, Settings.BuildVersion);
                    return;
                }

                int accountId = Database.Authenticate(username, password, client.IP);
                if (accountId == -1)
                {
                    client.SendFailureAndClose(FailureForceCloseGame, "Invalid account.");
                    return;
                }
                if (HopTrace.Enabled) HopTrace.Mark(client.HopAccount, "after Database.Authenticate"); //[hop]

                //Kick the live session before any account Load so the
                //authoritative models land in the handoff (or a waited
                //SQLite commit) instead of racing a stale Verify snapshot.
                Client previous = Manager.GetClient(accountId);
                if (previous != null)
                {
                    if (previous.Reconnecting)
                        previous.Disconnect();
                    else
                        previous.DisconnectWaitingForSave();
                }
                if (HopTrace.Enabled) HopTrace.Mark(client.HopAccount, "after old-session kick"); //[hop]

                AccountModel acc;
                CharacterModel handedCharacter = null;
                bool fromHandoff = Manager.TryTakeHandoff(accountId, out Manager.SessionHandoff handoff);
                if (fromHandoff)
                {
                    acc = handoff.Account;
                    handedCharacter = handoff.Character;
                }
                else
                {
                    acc = new AccountModel(accountId);
                    if (acc.IsNull)
                    {
                        client.SendFailureAndClose(FailureForceCloseGame, "Invalid account.");
                        return;
                    }
                    acc.Load();
                }
                if (HopTrace.Enabled) HopTrace.Mark(client.HopAccount, fromHandoff ? "after handoff take (no DB load)" : "after acc.Load"); //[hop]

                if (acc.Banned)
                {
                    if (fromHandoff)
                    {
                        try
                        {
                            if (handedCharacter != null && !handedCharacter.Dead)
                                Database.SaveAccountAndCharacter(acc, handedCharacter);
                            else
                                acc.Save();
                        }
                        catch { }
                    }
                    client.SendFailureAndClose(FailureForceCloseGame, "Banned.");
                    return;
                }

                if (!acc.Ranked && gameId == Manager.EditorId)
                {
                    if (fromHandoff)
                        Manager.StoreHandoff(acc, handedCharacter);
                    client.SendFailureAndClose(FailureForceCloseGame, "Not ranked.");
                    return;
                }

                if (Database.IsAccountInUse(acc))
                {
                    if (fromHandoff)
                        Manager.StoreHandoff(acc, handedCharacter);
                    client.SendFailureAndClose(FailureForceCloseGame, "Account in use!");
                    return;
                }

                client.Account = acc;
                client.HandoffCharacter = handedCharacter;

                World world = null;
#if DEBUG
                if (gameId == Manager.EditorId)
                {
                    Program.Print(PrintType.Debug, "Loading editor world");
                    JSMap map = new JSMap(Encoding.UTF8.GetString(mapJson));
                    world = new World(map, Resources.Worlds["Dreamland"]);
                    Manager.AddWorld(world);
                }
#endif
                if (world == null)
                    world = Manager.ResolveHelloWorld(client, gameId);

                if (world == null)
                {
                    if (fromHandoff)
                        Manager.StoreHandoff(acc, handedCharacter);
                    client.SendFailureAndClose(FailureForceCloseGame, "Invalid world!");
                    return;
                }

                if (world is RealmWorld && !world.AllowedAccess(client))
                {
                    if (fromHandoff)
                        Manager.StoreHandoff(acc, handedCharacter);
                    client.SendFailureAndClose(FailureForceCloseGame, "Realm closed.");
                    return;
                }

                client.Account.Connected = true;
                client.Account.Save(); //queued FIFO on the writer; LinkClient below is the in-memory login gate
                if (HopTrace.Enabled) HopTrace.Mark(client.HopAccount, "after acc save enqueued"); //[hop]
                client.TargetWorldId = world.Id;
                Manager.LinkClient(client.Account.Id, client.Id);

                uint seed = (uint)MathUtils.NextInt(1, int.MaxValue - 1);
                client.Random = new wRandom(seed);
                if (HopTrace.Enabled) HopTrace.Watch(client, HopTrace.WatchMapInfo); //[hop]
                client.Send(MapInfo(world.Width, world.Height, world.Name, world.GetDisplayName(), seed, world.Background, world.ShowDisplays, world.AllowTeleport));
                client.State = ProtocolState.Awaiting; //Allow the processing of Load/Create.
            }
        }

        public static void Create(Client client, ref SpanReader rdr)
        {
            int classType = rdr.ReadInt16();
            int skinType = rdr.ReadInt16();

            if (client.State == ProtocolState.Awaiting && client.PendingJoin == null)
            {
                World world = Manager.GetWorld(client.TargetWorldId);
                if (world is RealmWorld && !world.AllowedAccess(client))
                {
                    client.SendFailureAndClose(FailureForceCloseGame, "Realm closed.");
                    return;
                }
                CharacterModel character = Database.PrepareCharacter(client.Account, classType, skinType);
                if (character == null)
                {
                    client.SendFailureAndClose(FailureForceCloseGame, "Failed to create character.");
                    return;
                }
                //W03: CreateSuccess and the first frame wait for the commit, so the client never hears of a character
                //the store does not have. The token keeps a late completion from joining a different connection.
                object token = new object();
                client.PendingJoin = token;
                Database.CommitCharacter(client.Account, character, committed => FinishCreateJoin(client, token, character, committed));
            }
        }

        //Main thread, after the character row committed (or the commit failed).
        private static void FinishCreateJoin(Client client, object token, CharacterModel character, bool committed)
        {
            if (!ReferenceEquals(client.PendingJoin, token))
                return; //the connection went away, or the join was superseded
            client.PendingJoin = null;
            if (!client.Active || client.State != ProtocolState.Awaiting)
                return;
            if (!committed)
            {
                client.SendFailureAndClose(FailureForceCloseGame, "Failed to create character.");
                return;
            }

            World world = Manager.GetWorld(client.TargetWorldId);
            if (world is RealmWorld && !world.AllowedAccess(client))
            {
                client.SendFailureAndClose(FailureForceCloseGame, "Realm closed.");
                return;
            }
            client.HandoffCharacter = null;
            client.Character = character;
            client.Player = new Player(client);
            client.State = ProtocolState.Connected;
            client.Send(CreateSuccess(world.AddEntity(client.Player, PickSpawnPosition(world)), client.Character.Id));
            client.Player.SendJoinFrame(); //W04: first Update + NewTick now, not at the next world tick
            if (client.Account != null)
                Manager.CompletePendingTransfer(client.Account.Id);
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

        public static void Load(Client client, ref SpanReader rdr)
        {
            int charId = rdr.ReadInt32();
            if (HopTrace.Enabled) HopTrace.Mark(client.HopAccount, "Load rx"); //[hop]

            if (client.State == ProtocolState.Awaiting && client.PendingJoin == null)
            {
                CharacterModel character = null;
                if (client.HandoffCharacter != null && client.HandoffCharacter.Id == charId)
                    character = client.HandoffCharacter;
                else
                {
                    if (client.HandoffCharacter != null)
                    {
                        try { Database.SaveAccountAndCharacter(client.Account, client.HandoffCharacter); }
                        catch { }
                    }
                    character = Database.LoadCharacter(client.Account, charId);
                }
                client.HandoffCharacter = null;
                if (HopTrace.Enabled) HopTrace.Mark(client.HopAccount, "Load: character resolved"); //[hop]
                if (character == null || character.IsNull || character.Dead || character.Deleted)
                {
                    client.SendFailureAndClose(FailureForceCloseGame, "Failed to load character.");
                    return;
                }

                World world = Manager.GetWorld(client.TargetWorldId);
                if (world is RealmWorld && !world.AllowedAccess(client))
                {
                    client.SendFailureAndClose(FailureForceCloseGame, "Realm closed.");
                    return;
                }
                client.Character = character;
                client.Player = new Player(client);
                client.State = ProtocolState.Connected;
                if (HopTrace.Enabled) HopTrace.Watch(client, HopTrace.WatchCreateSuccess); //[hop]
                client.Send(CreateSuccess(world.AddEntity(client.Player, PickSpawnPosition(world)), client.Character.Id));
                if (HopTrace.Enabled) HopTrace.Joined(client); //[hop]
                client.Player.SendJoinFrame(); //W04: first Update + NewTick now, not at the next world tick
                if (client.Account != null)
                    Manager.CompletePendingTransfer(client.Account.Id);
            }
        }

        public static void Move(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32();
            Position position = new Position(ref rdr); 
            client.Player.TryMove(time, position);
        }

        public static void InvSwap(Client client, ref SpanReader rdr)
        {
            int time = rdr.ReadInt32();
            Position position = new Position(ref rdr);
            SlotData slot1 = new SlotData(ref rdr);
            SlotData slot2 = new SlotData(ref rdr); 
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

        public static ReadOnlySpan<byte> MapInfo(int width, int height, string name, string displayName, uint seed, int background, bool showDisplays, bool allowPlayerTeleport)
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

        public static ReadOnlySpan<byte> InvResult(int result)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.InvResult);
            wtr.Write(result);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> Failure(int errorId, string description)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Failure);
            wtr.Write(errorId);
            wtr.Write(description);
            return PacketWriter.RentedBytes();
        }

        //[W06] Failure as an owned array, built on a detached writer. Client.BeginSlowDisconnect calls it from inside Send,
        //which can run in the middle of a caller's send loop; the shared scratch used by Failure would rewind that caller's span.
        public static byte[] OwnedFailure(int errorId, string description)
        {
            PacketWriter wtr = PacketWriter.RentDetached();
            wtr.Write((byte)PacketId.Failure);
            wtr.Write(errorId);
            wtr.Write(description);
            return wtr.ToArray();
        }

        public static ReadOnlySpan<byte> NameResult(bool success, string errorText)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.NameResult);
            wtr.Write(success);
            wtr.Write(errorText);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> CreateSuccess(int objectId, int charId)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.CreateSuccess);
            wtr.Write(objectId);
            wtr.Write(charId);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> Update(List<TileData> tiles, List<ObjectDefinition> adds, List<ObjectDrop> drops)
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

        //[W08] Update whose add statuses are pre-serialized and shared between receivers (Entity.SharedStatus). The bytes
        //are the same as Update(tiles, ObjectDefinition list, drops).
        public static ReadOnlySpan<byte> UpdateShared(List<TileData> tiles, List<AddEntry> adds, List<ObjectDrop> drops)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Update);
            wtr.Write((short)tiles.Count);
            foreach (TileData k in tiles)
                k.Write(wtr);

            wtr.Write((short)adds.Count);
            foreach (AddEntry k in adds)
            {
                wtr.Write(k.ObjectType);
                k.Status.Write(wtr);
            }

            wtr.Write((short)drops.Count);
            foreach (ObjectDrop k in drops)
                k.Write(wtr);

            return PacketWriter.RentedBytes();
        }

        //[W08] NewTick whose statuses are pre-serialized and shared (Entity.SharedStatus). The bytes are the same as
        //NewTick(statuses, playerStats).
        public static ReadOnlySpan<byte> NewTickShared(List<StatusEntry> statuses, StatTable playerStats)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.NewTick);
            wtr.Write((short)statuses.Count);
            foreach (StatusEntry k in statuses)
                k.Write(wtr);
            StatMask changed = playerStats.Dirty;
            int count = changed.Count;
            if (count > 0)
            {
                wtr.Write((byte)count);
                for (int s = changed.NextSet(0); s >= 0; s = changed.NextSet(s + 1))
                    playerStats.WriteStat(wtr, s);
            }
            return PacketWriter.RentedBytes();
        }

        //playerStats is the owner's private block: its Dirty mask (the changes since the caller last cleared it).
        public static ReadOnlySpan<byte> NewTick(List<ObjectStatus> statuses, StatTable playerStats)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.NewTick);
            wtr.Write((short)statuses.Count);
            foreach (ObjectStatus k in statuses)
                k.Write(wtr);
            StatMask changed = playerStats.Dirty;
            int count = changed.Count;
            if (count > 0)
            {
                wtr.Write((byte)count);
                for (int s = changed.NextSet(0); s >= 0; s = changed.NextSet(s + 1))
                    playerStats.WriteStat(wtr, s);
            }
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> EnemyShoot(int bulletId, int ownerId, byte bulletType, Position startPos, float angle, short damage, byte numShots, float angleInc)
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

        public static ReadOnlySpan<byte> ShowEffect(ShowEffectIndex effect, int targetObjectId, uint color, Position pos1 = new Position(), Position pos2 = new Position())
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
        public static ReadOnlySpan<byte> ThrowEffect(int targetObjectId, uint color, Position target, int travelTime)
        {
            Position duration = travelTime == 1500 ? new Position() : new Position(travelTime, 0);
            return ShowEffect(ShowEffectIndex.Throw, targetObjectId, color, target, duration);
        }

        public static ReadOnlySpan<byte> Goto(int objectId, Position pos)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Goto);
            wtr.Write(objectId);
            pos.Write(wtr);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> Ping(int serial)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Ping);
            wtr.Write(serial);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> Aoe(Position pos, float radius, int damage, ConditionEffectIndex effect, uint color)
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

        public static ReadOnlySpan<byte> Damage(int targetId, ConditionEffectIndex[] effects, int damage)
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

        public static ReadOnlySpan<byte> Death(int accountId, int charId, string killer)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Death);
            wtr.Write(accountId);
            wtr.Write(charId);
            wtr.Write(killer);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> AllyShoot(int ownerId, int containerType, float angle)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.AllyShoot);
            wtr.Write(ownerId);
            wtr.Write((short)containerType);
            wtr.Write(angle);
            return PacketWriter.RentedBytes();
        }
        
        public static ReadOnlySpan<byte> PlaySound(string sound)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.PlaySound);
            wtr.Write(sound);
            return PacketWriter.RentedBytes();
        }

        //nameColor and textColor follow betterSkillys: 0 keeps the client's default color. Clients that predate
        //them stop reading after text, so the two trailing ints are ignored there.
        public static ReadOnlySpan<byte> Text(string name, int objectId, int numStars, int bubbleTime, string recipent, string text, int nameColor = 0, int textColor = 0)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Text);
            wtr.Write(name);
            wtr.Write(objectId);
            wtr.Write(numStars);
            wtr.Write((byte)bubbleTime);
            wtr.Write(recipent);
            wtr.Write(text);
            wtr.Write(nameColor);
            wtr.Write(textColor);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> AccountList(int accountListId, List<int> accountIds)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.AccountList);
            wtr.Write(accountListId);
            wtr.Write((short)accountIds.Count);
            for (int i = 0; i < accountIds.Count; i++)
                wtr.Write(accountIds[i]);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> ServerPlayerShoot(int bulletId, int ownerId, int containerType, Position startPos, float angle, float angleInc, List<Projectile> projs)
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

        //Wire: a single int gameId. Authorization is server-side
        //(Manager.PendingTransfers); a random 32-bit key can be appended
        //on both sides when a client rebuild is planned.
        public static ReadOnlySpan<byte> Reconnect(int gameId)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.Reconnect);
            wtr.Write(gameId);
            return PacketWriter.RentedBytes();
        }

        public static ReadOnlySpan<byte> Notification(int objectId, string text, uint color)
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
        public static ReadOnlySpan<byte> GlobalNotification(int type, string text)
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
            return PacketWriter.RentedBytes().ToArray();
        }
    }
}
