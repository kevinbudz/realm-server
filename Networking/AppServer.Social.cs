using RotMG.Common;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace RotMG.Networking
{
    //Account, guild and app endpoints ported from realm-src-master
    //server/account/setName.cs, server/char/purchaseClassUnlock.cs,
    //server/app/*.cs and server/guild/*.cs.
    public static partial class AppServer
    {
        private static byte[] AccountSetName(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            string name = query["name"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else if (string.IsNullOrWhiteSpace(name) || !Database.IsValidUsername(name))
                    data = WriteError("Invalid name");
                else
                {
                    int existingId = Database.IdFromUsername(name);
                    if (existingId != -1 && existingId != acc.Id)
                        data = WriteError("Duplicated name");
                    else if (!string.IsNullOrWhiteSpace(acc.Name) && acc.Stats.Credits < 1000)
                        data = WriteError("Not enough credits");
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(acc.Name))
                        {
                            acc.Stats.Credits -= 1000;
                            Database.DeleteKey($"login.username.{acc.Name}");
                        }
                        Database.SetKey($"login.username.{name}", acc.Id.ToString());
                        Database.SetKey($"login.id.{acc.Id}", name);
                        acc.Save();
                        data = WriteSuccess();
                    }
                }
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] CharPurchaseClassUnlock(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = WriteError("Bad input to character unlock");
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] AppInit(HttpListenerContext context, NameValueCollection query)
        {
            XElement root = new XElement("AppSettings",
                new XElement("BuildVersion", "1.0.0"),
                new XElement("ServerAddress", Settings.Address),
                new XElement("AppPort", Settings.Ports[0]),
                new XElement("GamePort", Settings.Ports[1]));
            return Write(root.ToString());
        }

        private static byte[] AppGlobalNews(HttpListenerContext context, NameValueCollection query)
        {
            try
            {
                string path = Resources.CombineResourcePath("News.xml");
                if (System.IO.File.Exists(path))
                    return Write(System.IO.File.ReadAllText(path));
            }
            catch { }
            return Write("<News/>");
        }

        private static byte[] GuildGetBoard(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (string.IsNullOrWhiteSpace(acc.GuildName))
                    data = WriteError("Not in guild");
                else
                    data = Write(Database.GetGuildBoard(acc.GuildName));
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] GuildSetBoard(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            string board = query["board"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (string.IsNullOrWhiteSpace(acc.GuildName) || acc.GuildRank < 20)
                    data = WriteError("No permission");
                else
                {
                    string text = HttpUtility.UrlDecode(board ?? "");
                    Database.SetGuildBoard(acc.GuildName, text);
                    data = Write(text);
                }
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] GuildListMembers(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (string.IsNullOrWhiteSpace(acc.GuildName))
                    data = WriteError("Not in guild");
                else
                {
                    XElement root = new XElement("Members",
                        new XElement("CurrentFame", Database.GetGuildFame(acc.GuildName)),
                        new XElement("GuildLevel", Database.GetGuildLevel(acc.GuildName)));
                    foreach (int id in Database.GetGuildMemberIds(acc.GuildName))
                    {
                        AccountModel member = new AccountModel(id);
                        member.Load();
                        root.Add(new XElement("Member",
                            new XElement("Name", member.Name ?? ""),
                            new XElement("Rank", member.GuildRank),
                            new XElement("Fame", member.Stats != null ? member.Stats.Fame : 0)));
                    }
                    data = Write(root.ToString());
                }
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }
    }
}
