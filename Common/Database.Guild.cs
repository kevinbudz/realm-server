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

        //Returns the synchronous verdict. OK means the change is queued and onDone(committed) reports the commit. The
        //in-memory change is reverted when the commit fails. Without onDone the call is fire-and-forget (chat callers).
        public static GuildResult CreateGuild(string guildName, AccountModel founder, Action<bool> onDone = null)
        {
            if (!IsValidGuildName(guildName))
                return GuildResult.InvalidName;
            if (!string.IsNullOrWhiteSpace(founder.GuildName))
                return GuildResult.InGuild;
            if (GuildExists(guildName))
                return GuildResult.UsedName;
            //Guild keys, membership list and the founder's account row commit together.
            string prevGuild = founder.GuildName;
            int prevRank = founder.GuildRank;
            founder.GuildName = guildName;
            founder.GuildRank = 40;
            string accountXml = founder.Export(false).ToString();
            WriteAtomicallyThen(new Dictionary<string, string>
            {
                { GuildKey(guildName), founder.Id.ToString() },
                { GuildKey(guildName) + ".board", "" },
                { GuildKey(guildName) + ".members", founder.Id.ToString() },
                { AccountKey(founder.Id), accountXml }
            }, null, committed =>
            {
                if (!committed)
                {
                    founder.GuildName = prevGuild;
                    founder.GuildRank = prevRank;
                }
                onDone?.Invoke(committed);
            });
            founder.Data = System.Xml.Linq.XElement.Parse(accountXml);
            return GuildResult.OK;
        }

        public static GuildResult AddGuildMember(string guildName, AccountModel acc, int rank = 0, Action<bool> onDone = null)
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
            string prevGuild = acc.GuildName;
            int prevRank = acc.GuildRank;
            acc.GuildName = guildName;
            acc.GuildRank = rank;
            string accountXml = acc.Export(false).ToString();
            WriteAtomicallyThen(new Dictionary<string, string>
            {
                { GuildKey(guildName) + ".members", string.Join(",", members) },
                { AccountKey(acc.Id), accountXml }
            }, null, committed =>
            {
                if (!committed)
                {
                    acc.GuildName = prevGuild;
                    acc.GuildRank = prevRank;
                }
                onDone?.Invoke(committed);
            });
            acc.Data = System.Xml.Linq.XElement.Parse(accountXml);
            return GuildResult.OK;
        }

        public static GuildResult RemoveFromGuild(AccountModel acc, Action<bool> onDone = null)
        {
            if (string.IsNullOrWhiteSpace(acc.GuildName))
                return GuildResult.NotInGuild;

            string key = GuildKey(acc.GuildName);
            List<int> members = GetGuildMemberIds(acc.GuildName);
            members.Remove(acc.Id);

            string prevGuild = acc.GuildName;
            int prevRank = acc.GuildRank;
            acc.GuildName = null;
            acc.GuildRank = 0;
            string accountXml = acc.Export(false).ToString();
            Action<bool> outcome = committed =>
            {
                if (!committed)
                {
                    acc.GuildName = prevGuild;
                    acc.GuildRank = prevRank;
                }
                onDone?.Invoke(committed);
            };
            if (members.Count == 0)
            {
                WriteAtomicallyThen(
                    new Dictionary<string, string> { { AccountKey(acc.Id), accountXml } },
                    new[] { key, key + ".board", key + ".members", key + ".fame", key + ".totalFame", key + ".level" },
                    outcome);
            }
            else
            {
                WriteAtomicallyThen(new Dictionary<string, string>
                {
                    { key + ".members", string.Join(",", members) },
                    { AccountKey(acc.Id), accountXml }
                }, null, outcome);
            }
            acc.Data = System.Xml.Linq.XElement.Parse(accountXml);
            return GuildResult.OK;
        }

        public static GuildResult ChangeGuildRank(AccountModel acc, int rank, Action<bool> onDone = null)
        {
            if (string.IsNullOrWhiteSpace(acc.GuildName))
                return GuildResult.NotInGuild;
            if (rank != 0 && rank != 10 && rank != 20 && rank != 30 && rank != 40)
                return GuildResult.Error;
            int prevRank = acc.GuildRank;
            acc.GuildRank = rank;
            acc.SaveThen(committed =>
            {
                if (!committed)
                    acc.GuildRank = prevRank;
                onDone?.Invoke(committed);
            });
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

        //Queued; onDone(committed) runs after the commit, so the HTTP reply waits for it (W03).
        public static void SetGuildBoard(string guildName, string text, Action<bool> onDone)
        {
            WriteAtomicallyThen(new Dictionary<string, string> { { GuildKey(guildName) + ".board", text ?? "" } }, null, onDone);
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

        //Main-thread callers only: the spend is checked by GetCurrency, which reads the overlay, so the check and this
        //queued read-modify-write cannot race. (It used to be a blocking transaction on the tick thread.)
        public static void AddGuildFame(string guildName, int amount)
        {
            if (string.IsNullOrWhiteSpace(guildName))
                return;
            if (string.IsNullOrWhiteSpace(GetKey(GuildKey(guildName))))
                return;
            int fame = 0;
            int.TryParse(GetKey(GuildKey(guildName) + ".fame"), out fame);
            Dictionary<string, string> writes = new Dictionary<string, string>
            {
                { GuildKey(guildName) + ".fame", Math.Max(0, fame + amount).ToString() }
            };
            if (amount > 0)
            {
                int total = 0;
                int.TryParse(GetKey(GuildKey(guildName) + ".totalFame"), out total);
                writes[GuildKey(guildName) + ".totalFame"] = Math.Max(0, total + amount).ToString();
            }
            WriteAtomically(writes);
        }

        public static int GetGuildLevel(string guildName)
        {
            if (!int.TryParse(GetKey(GuildKey(guildName) + ".level"), out int level))
                return 0;
            return Math.Max(0, Math.Min(3, level));
        }

        //Hall upgrade (W03): the new level and the guild-fame spend commit as ONE bundle. Upgrades apply in order (level
        //N buys level N). Returns false, queuing nothing, when the order or the balance is wrong. onDone(committed) runs
        //after the commit, so the vendor's success message waits for it.
        public static bool UpgradeGuildHall(string guildName, int level, int cost, Action<bool> onDone)
        {
            if (level < 1 || level > 3 || GetGuildLevel(guildName) != level - 1)
                return false;
            int fame = GetGuildFame(guildName);
            if (fame < cost)
                return false;
            WriteAtomicallyThen(new Dictionary<string, string>
            {
                { GuildKey(guildName) + ".level", level.ToString() },
                { GuildKey(guildName) + ".fame", Math.Max(0, fame - cost).ToString() }
            }, null, onDone);
            return true;
        }
    }
}
