using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RotMG.Common
{
    //Guild persistence on top of the key-value store, mirroring the guild
    //API of realm-src-master common/Database.cs (CreateGuild, AddGuildMember,
    //RemoveFromGuild, ChangeGuildRank, guild board).
    public static partial class Database
    {
        public const int GuildCreationFameCost = 1000;
        public const int GuildMaxMembers = 50;

        public enum GuildResult
        {
            OK,
            InvalidName,
            UsedName,
            InGuild,
            NotInGuild,
            GuildFull,
            NoPermission,
            MemberNotFound,
            Error
        }

        public static bool IsValidGuildName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            if (name.Length < 3 || name.Length > 20)
                return false;
            return Regex.IsMatch(name, @"^[a-zA-Z0-9 ]+$");
        }

        private static string GuildKey(string guildName) => $"guild.{guildName.ToUpperInvariant()}";

        public static bool GuildExists(string guildName)
        {
            if (string.IsNullOrWhiteSpace(guildName))
                return false;
            return GetKey(GuildKey(guildName)) != null;
        }

        public static GuildResult CreateGuild(string guildName, AccountModel founder)
        {
            if (!IsValidGuildName(guildName))
                return GuildResult.InvalidName;
            if (!string.IsNullOrWhiteSpace(founder.GuildName))
                return GuildResult.InGuild;
            if (GuildExists(guildName))
                return GuildResult.UsedName;

            //Guild keys, membership list and the founder's account row commit
            //together: a crash halfway used to leave a guild with no founder
            //row or a founder pointing at a guild with no member list.
            founder.GuildName = guildName;
            founder.GuildRank = 40;
            string accountXml = founder.Export(false).ToString();
            WriteAtomically(new Dictionary<string, string>
            {
                { GuildKey(guildName), founder.Id.ToString() },
                { GuildKey(guildName) + ".board", "" },
                { GuildKey(guildName) + ".members", founder.Id.ToString() },
                { AccountKey(founder.Id), accountXml }
            });
            founder.Data = System.Xml.Linq.XElement.Parse(accountXml);
            return GuildResult.OK;
        }

        public static GuildResult AddGuildMember(string guildName, AccountModel acc, int rank = 0)
        {
            if (!GuildExists(guildName))
                return GuildResult.Error;
            if (!string.IsNullOrWhiteSpace(acc.GuildName))
                return GuildResult.InGuild;

            List<int> members = GetGuildMemberIds(guildName);
            if (members.Count >= GuildMaxMembers)
                return GuildResult.GuildFull;
            if (!members.Contains(acc.Id))
                members.Add(acc.Id);

            acc.GuildName = guildName;
            acc.GuildRank = rank;
            string accountXml = acc.Export(false).ToString();
            WriteAtomically(new Dictionary<string, string>
            {
                { GuildKey(guildName) + ".members", string.Join(",", members) },
                { AccountKey(acc.Id), accountXml }
            });
            acc.Data = System.Xml.Linq.XElement.Parse(accountXml);
            return GuildResult.OK;
        }

        public static GuildResult RemoveFromGuild(AccountModel acc)
        {
            if (string.IsNullOrWhiteSpace(acc.GuildName))
                return GuildResult.NotInGuild;

            string key = GuildKey(acc.GuildName);
            List<int> members = GetGuildMemberIds(acc.GuildName);
            members.Remove(acc.Id);

            acc.GuildName = null;
            acc.GuildRank = 0;
            string accountXml = acc.Export(false).ToString();
            if (members.Count == 0)
            {
                WriteAtomically(
                    new Dictionary<string, string> { { AccountKey(acc.Id), accountXml } },
                    new[] { key, key + ".board", key + ".members", key + ".fame", key + ".totalFame", key + ".level" });
            }
            else
            {
                WriteAtomically(new Dictionary<string, string>
                {
                    { key + ".members", string.Join(",", members) },
                    { AccountKey(acc.Id), accountXml }
                });
            }
            acc.Data = System.Xml.Linq.XElement.Parse(accountXml);
            return GuildResult.OK;
        }

        public static GuildResult ChangeGuildRank(AccountModel acc, int rank)
        {
            if (string.IsNullOrWhiteSpace(acc.GuildName))
                return GuildResult.NotInGuild;
            if (rank != 0 && rank != 10 && rank != 20 && rank != 30 && rank != 40)
                return GuildResult.Error;

            acc.GuildRank = rank;
            acc.Save();
            return GuildResult.OK;
        }

        public static List<int> GetGuildMemberIds(string guildName)
        {
            List<int> ids = new List<int>();
            string raw = GetKey(GuildKey(guildName) + ".members");
            if (string.IsNullOrWhiteSpace(raw))
                return ids;
            foreach (string part in raw.Split(','))
                if (int.TryParse(part, out int id))
                    ids.Add(id);
            return ids;
        }

        public static string GetGuildBoard(string guildName)
        {
            return GetKey(GuildKey(guildName) + ".board") ?? "";
        }

        public static void SetGuildBoard(string guildName, string text)
        {
            SetKey(GuildKey(guildName) + ".board", text ?? "");
        }

        public static string ResolveGuildRank(int rank)
        {
            switch (rank)
            {
                case 0: return "Initiate";
                case 10: return "Member";
                case 20: return "Officer";
                case 30: return "Leader";
                case 40: return "Founder";
                default: return "";
            }
        }

        //Guild fame mirrors realm-src-master DbGuild fame/totalFame: death
        //fame accrues to the pool, hall upgrades spend from it.
        public static int GetGuildFame(string guildName)
        {
            if (!int.TryParse(GetKey(GuildKey(guildName) + ".fame"), out int fame))
                return 0;
            return Math.Max(0, fame);
        }

        public static int GetGuildTotalFame(string guildName)
        {
            if (!int.TryParse(GetKey(GuildKey(guildName) + ".totalFame"), out int fame))
                return 0;
            return Math.Max(0, fame);
        }

        public static void AddGuildFame(string guildName, int amount)
        {
            if (string.IsNullOrWhiteSpace(guildName))
                return;
            //Read-modify-write inside one transaction so two concurrent
            //spenders (or a death credit racing a hall purchase) cannot both
            //read the same balance and conjure fame from nothing.
            Transact(conn =>
            {
                if (string.IsNullOrWhiteSpace(GetKeyInTx(conn, GuildKey(guildName))))
                    return;
                int fame = 0;
                int.TryParse(GetKeyInTx(conn, GuildKey(guildName) + ".fame"), out fame);
                UpsertKeyInTx(conn, GuildKey(guildName) + ".fame", Math.Max(0, fame + amount).ToString());
                if (amount > 0)
                {
                    int total = 0;
                    int.TryParse(GetKeyInTx(conn, GuildKey(guildName) + ".totalFame"), out total);
                    UpsertKeyInTx(conn, GuildKey(guildName) + ".totalFame", Math.Max(0, total + amount).ToString());
                }
            });
        }

        public static int GetGuildLevel(string guildName)
        {
            if (!int.TryParse(GetKey(GuildKey(guildName) + ".level"), out int level))
                return 0;
            return Math.Max(0, Math.Min(3, level));
        }

        //Upgrades apply sequentially (level N vendor buys level N), mirroring
        //ChangeGuildLevel upstream.
        public static bool ChangeGuildLevel(string guildName, int level)
        {
            if (level < 1 || level > 3 || GetGuildLevel(guildName) != level - 1)
                return false;
            SetKey(GuildKey(guildName) + ".level", level.ToString());
            return true;
        }
    }
}
