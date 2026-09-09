using RotMG.Common;
using RotMG.Networking;
using System;
using System.Linq;

namespace RotMG.Game.Entities
{
    //Guild membership state, guild chat and teleport-to-player, ported from
    //realm-src-master wServer/realm/entities/player/Player.cs.
    public partial class Player
    {
        //Name of the guild this player has been invited to (null when none).
        public string GuildInvite;

        public void TryTeleportTo(int objectId)
        {
            if (Parent == null)
                return;

            Entity target = Parent.GetEntity(objectId);
            if (target == null || !(target is Player) || target == this)
                return;

            FameStats.Teleports++;
            Teleport(Manager.TotalTimeUnsynced, target.Position);
        }

        public void SendGuild(string text)
        {
            if (string.IsNullOrWhiteSpace(GuildName))
                return;
            byte[] packet = GameServer.Text("@" + GuildName, Id, NumStars, 0, "", text);
            foreach (Client client in Manager.Clients.Values.ToArray())
            {
                Player player = client.Player;
                if (player != null && player.GuildName == GuildName)
                    client.Send(packet);
            }
        }

        public int GetCurrency(CurrencyType currency)
        {
            switch (currency)
            {
                case CurrencyType.Gold: return Client.Account.Stats.Credits;
                case CurrencyType.Fame: return Client.Account.Stats.Fame;
                case CurrencyType.GuildFame: return Database.GetGuildFame(Client.Account.GuildName);
                default: return 0;
            }
        }

        public bool TryDeduct(CurrencyType currency, int amount)
        {
            if (GetCurrency(currency) < amount)
                return false;
            if (currency == CurrencyType.Gold)
            {
                Client.Account.Stats.Credits -= amount;
                Credits = Client.Account.Stats.Credits;
                Client.Account.Save();
            }
            else if (currency == CurrencyType.GuildFame)
            {
                Database.AddGuildFame(Client.Account.GuildName, -amount);
            }
            else
            {
                Client.Account.Stats.Fame -= amount;
                Fame = Client.Account.Stats.Fame;
                Client.Account.Save();
            }
            return true;
        }
    }

    public enum CurrencyType
    {
        Gold = 0,
        Fame = 1,
        GuildFame = 2
    }
}
