using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Game.Entities.Vendors;

namespace RotMG.Networking
{
    //Incoming handlers and outgoing builders for guilds, vendors and
    //miscellaneous packets. Packet IDs and layouts must match realm-client
    //(src/kabam/rotmg/messaging/impl/); that client has no trade, ping,
    //update-ack or ground-damage messages, so there are deliberately no
    //handlers or builders for those here.
    public static partial class GameServer
    {
        public static void Teleport(Client client, PacketReader rdr)
        {
            int objectId = rdr.ReadInt32();
            if (client.Player == null || client.Player.Parent == null)
                return;
            client.Player.TryTeleportTo(objectId);
        }

        public static void Buy(Client client, PacketReader rdr)
        {
            int objectId = rdr.ReadInt32();
            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;
            SellableObject obj = player.Parent.GetEntity(objectId) as SellableObject;
            obj?.Buy(player);
        }

        public static void RequestTrade(Client client, PacketReader rdr)
        {
            string name = rdr.ReadString();
            if (client.Player == null || client.Player.Parent == null)
                return;
            client.Player.RequestTrade(name);
        }

        public static void ChangeTrade(Client client, PacketReader rdr)
        {
            int count = rdr.ReadInt16();
            bool[] offer = new bool[count];
            for (int i = 0; i < count; i++)
                offer[i] = rdr.ReadBoolean();
            if (client.Player == null || client.Player.Parent == null)
                return;
            client.Player.ChangeTrade(offer);
        }

        public static void AcceptTrade(Client client, PacketReader rdr)
        {
            int myCount = rdr.ReadInt16();
            bool[] myOffer = new bool[myCount];
            for (int i = 0; i < myCount; i++)
                myOffer[i] = rdr.ReadBoolean();
            int yourCount = rdr.ReadInt16();
            bool[] yourOffer = new bool[yourCount];
            for (int i = 0; i < yourCount; i++)
                yourOffer[i] = rdr.ReadBoolean();
            if (client.Player == null || client.Player.Parent == null)
                return;
            client.Player.AcceptTrade(myOffer, yourOffer);
        }

        public static void CancelTrade(Client client, PacketReader rdr)
        {
            if (client.Player == null)
                return;
            client.Player.CancelTrade();
        }

        public static void CreateGuild(Client client, PacketReader rdr)
        {
            string name = rdr.ReadString();
            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;

            AccountModel acc = client.Account;
            if (acc.Stats.Fame < Database.GuildCreationFameCost)
            {
                client.Send(GuildResult(false, "Guild Creation Error: Insufficient funds"));
                return;
            }
            if (!player.NameChosen)
            {
                client.Send(GuildResult(false, "Guild Creation Error: Must pick a character name\nbefore creating a guild"));
                return;
            }
            if (!string.IsNullOrWhiteSpace(acc.GuildName))
            {
                client.Send(GuildResult(false, "Guild Creation Error: Already in a guild"));
                return;
            }

            Database.GuildResult result = Database.CreateGuild(name, acc);
            if (result != Database.GuildResult.OK)
            {
                client.Send(GuildResult(false, "Guild Creation Error: " + result));
                return;
            }

            acc.Stats.Fame -= Database.GuildCreationFameCost;
            acc.Save();
            player.Fame = acc.Stats.Fame;
            player.GuildName = acc.GuildName;
            player.GuildRank = acc.GuildRank;
            client.Send(GuildResult(true, "Success!"));
        }

        public static void GuildInvite(Client client, PacketReader rdr)
        {
            string name = rdr.ReadString();
            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;

            if (client.Account.GuildRank < 20)
            {
                player.SendError("Insufficient privileges.");
                return;
            }

            Player target = Manager.GetPlayer(name);
            if (target == null)
            {
                player.SendError("Could not find the player to invite.");
                return;
            }
            if (!target.NameChosen)
            {
                player.SendError("Player needs to choose a name first.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(target.Client.Account.GuildName))
            {
                player.SendError("Player is already in a guild.");
                return;
            }

            target.GuildInvite = player.GuildName;
            target.Client.Send(InvitedToGuild(client.Account.Name, player.GuildName));
        }

        public static void GuildRemove(Client client, PacketReader rdr)
        {
            string name = rdr.ReadString();
            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;

            //Resigning.
            if (client.Account.Name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase))
            {
                player.SendGuild(player.Name + " has left the guild.");
                if (Database.RemoveFromGuild(client.Account) != Database.GuildResult.OK)
                {
                    player.SendError("Guild not found.");
                    return;
                }
                player.GuildName = null;
                player.GuildRank = 0;
                return;
            }

            int targetId = Database.IdFromUsername(name);
            if (targetId == -1)
            {
                player.SendError("Player not found");
                return;
            }

            Client targetClient = Manager.GetClient(targetId);
            AccountModel targetAcc = targetClient != null ? targetClient.Account : new AccountModel(targetId);
            if (targetClient == null)
                targetAcc.Load();

            if (client.Account.GuildRank >= 20 &&
                string.Equals(client.Account.GuildName, targetAcc.GuildName, System.StringComparison.Ordinal) &&
                client.Account.GuildRank > targetAcc.GuildRank)
            {
                if (Database.RemoveFromGuild(targetAcc) != Database.GuildResult.OK)
                {
                    player.SendError("Guild not found.");
                    return;
                }
                if (targetClient != null && targetClient.Player != null)
                {
                    targetClient.Player.GuildName = null;
                    targetClient.Player.GuildRank = 0;
                    targetClient.Player.SendInfo("You have been kicked from the guild.");
                }
                player.SendGuild(targetAcc.Name + " has been kicked from the guild by " + player.Name);
                return;
            }

            player.SendError("Can't remove member. Insufficient privileges.");
        }

        public static void JoinGuild(Client client, PacketReader rdr)
        {
            string guildName = rdr.ReadString();
            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;

            if (string.IsNullOrWhiteSpace(player.GuildInvite))
            {
                player.SendError("You have not been invited to a guild.");
                return;
            }
            if (!player.GuildInvite.Equals(guildName, System.StringComparison.InvariantCultureIgnoreCase))
            {
                player.SendError("You have not been invited to join " + guildName + ".");
                return;
            }
            if (!Database.GuildExists(player.GuildInvite))
            {
                player.SendError("Internal server error.");
                return;
            }

            Database.GuildResult result = Database.AddGuildMember(player.GuildInvite, client.Account);
            if (result != Database.GuildResult.OK)
            {
                player.SendError("Could not join guild. (" + result + ")");
                return;
            }

            player.GuildName = client.Account.GuildName;
            player.GuildRank = client.Account.GuildRank;
            player.GuildInvite = null;
            player.SendGuild(player.Name + " has joined the guild!");
        }

        public static void ChangeGuildRank(Client client, PacketReader rdr)
        {
            string name = rdr.ReadString();
            int rank = rdr.ReadInt32();
            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;

            int targetId = Database.IdFromUsername(name);
            if (targetId == -1)
            {
                player.SendError("A player with that name does not exist.");
                return;
            }

            Client targetClient = Manager.GetClient(targetId);
            AccountModel targetAcc = targetClient != null ? targetClient.Account : new AccountModel(targetId);
            if (targetClient == null)
                targetAcc.Load();

            AccountModel src = client.Account;
            if (string.IsNullOrWhiteSpace(src.GuildName) ||
                src.GuildRank < 20 ||
                src.GuildRank <= targetAcc.GuildRank ||
                src.GuildRank < rank ||
                rank == 40 ||
                !src.GuildName.Equals(targetAcc.GuildName, System.StringComparison.Ordinal))
            {
                player.SendError("No permission");
                return;
            }

            if (targetAcc.GuildRank == rank)
            {
                player.SendError("Player is already a " + Database.ResolveGuildRank(rank));
                return;
            }

            bool wasPromotion = targetAcc.GuildRank < rank;
            if (Database.ChangeGuildRank(targetAcc, rank) != Database.GuildResult.OK)
            {
                player.SendError("Failed to change rank.");
                return;
            }
            if (targetClient != null && targetClient.Player != null)
                targetClient.Player.GuildRank = rank;

            player.SendGuild(targetAcc.Name + " has been " +
                (wasPromotion ? "promoted to " : "demoted to ") +
                Database.ResolveGuildRank(rank) + ".");
        }

        public static void Reskin(Client client, PacketReader rdr)
        {
            int skinId = rdr.ReadInt32();
            Player player = client.Player;
            if (player == null || player.Parent == null)
                return;

            if (skinId == 0)
            {
                player.SkinType = 0;
                return;
            }

            SkinDesc skinDesc;
            if (!Resources.Type2Skin.TryGetValue((ushort)skinId, out skinDesc))
            {
                player.SendError("Unknown skin type.");
                return;
            }
            if (!client.Account.OwnedSkins.Contains(skinId))
            {
                player.SendError("Skin not owned.");
                return;
            }
            if (skinDesc.PlayerClassType != player.Type)
            {
                player.SendError("Skin is for different class.");
                return;
            }
            player.SkinType = skinId;
        }

        public static byte[] QuestObjId(int objectId)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.QuestObjId);
            wtr.Write(objectId);
            return PacketWriter.RentedBytes();
        }

        public static byte[] BuyResult(int result, string resultString)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.BuyResult);
            wtr.Write(result);
            wtr.Write(resultString);
            return PacketWriter.RentedBytes();
        }

        public static byte[] GuildResult(bool success, string errorText)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.GuildResult);
            wtr.Write(success);
            wtr.Write(errorText);
            return PacketWriter.RentedBytes();
        }

        public static byte[] InvitedToGuild(string name, string guildName)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.InvitedToGuild);
            wtr.Write(name);
            wtr.Write(guildName);
            return PacketWriter.RentedBytes();
        }

        public static byte[] TradeRequested(string name)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.TradeRequested);
            wtr.Write(name);
            return PacketWriter.RentedBytes();
        }

        public static byte[] TradeStart(TradeItem[] myItems, string yourName, TradeItem[] yourItems)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.TradeStart);
            wtr.Write((short)myItems.Length);
            foreach (TradeItem item in myItems)
                item.Write(wtr);
            wtr.Write(yourName);
            wtr.Write((short)yourItems.Length);
            foreach (TradeItem item in yourItems)
                item.Write(wtr);
            return PacketWriter.RentedBytes();
        }

        public static byte[] TradeChanged(bool[] offer)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.TradeChanged);
            wtr.Write((short)offer.Length);
            foreach (bool b in offer)
                wtr.Write(b);
            return PacketWriter.RentedBytes();
        }

        public static byte[] TradeAccepted(bool[] myOffer, bool[] yourOffer)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.TradeAccepted);
            wtr.Write((short)myOffer.Length);
            foreach (bool b in myOffer)
                wtr.Write(b);
            wtr.Write((short)yourOffer.Length);
            foreach (bool b in yourOffer)
                wtr.Write(b);
            return PacketWriter.RentedBytes();
        }

        public static byte[] TradeDone(int code, string description)
        {
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)PacketId.TradeDone);
            wtr.Write(code);
            wtr.Write(description);
            return PacketWriter.RentedBytes();
        }
    }
}
