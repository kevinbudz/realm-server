using RotMG.Common;
using RotMG.Game;
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
            bool queued = false;
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
                        //Credits, the new login key, the old login key's removal and the account row are ONE bundle
                        //(this used to be four separate writes). The reply waits for the commit.
                        if (!string.IsNullOrWhiteSpace(acc.Name))
                            acc.Stats.Credits -= 1000;
                        string oldName = acc.Name;
                        Database.RenameAccountKeys(acc.Id, oldName, name, acc, ok =>
                        {
                            if (ok)
                                acc.Name = name;
                            data = ok ? WriteSuccess() : WriteError("Save failed");
                            _listenEvent.Set();
                        });
                        queued = true;
                    }
                }
            }, () => { if (!queued) _listenEvent.Set(); });
            _listenEvent.WaitOne(30000);

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
            _listenEvent.WaitOne(30000);

            return data;
        }

        private static byte[] AppInit(HttpListenerContext context, NameValueCollection query)
        {
            XElement hashes = new XElement("GameDataHashes");
            foreach (var kv in Resources.GameDataHashes.OrderBy(k => k.Key))
                hashes.Add(new XElement("File", new XAttribute("name", kv.Key), kv.Value));
            XElement root = new XElement("AppSettings",
                new XElement("BuildVersion", Settings.BuildVersion),
                new XElement("ServerAddress", Settings.Address),
                new XElement("AppPort", Settings.Ports[0]),
                new XElement("GamePort", Settings.Ports[1]),
                hashes);
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
            _listenEvent.WaitOne(30000);

            return data;
        }

        private static byte[] GuildSetBoard(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            string board = query["board"];

            _listenEvent.Reset();
            bool queued = false;
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (string.IsNullOrWhiteSpace(acc.GuildName) || acc.GuildRank < 20)
                    data = WriteError("No permission");
                else
                {
                    //The reply waits for the commit (W03), so a GetGuildBoard right after sees the new text.
                    string text = HttpUtility.UrlDecode(board ?? "");
                    queued = true;
                    Database.SetGuildBoard(acc.GuildName, text, committed =>
                    {
                        data = committed ? Write(text) : WriteError("Save failed");
                        _listenEvent.Set();
                    });
                }
            }, () => { if (!queued) _listenEvent.Set(); });
            _listenEvent.WaitOne(30000);

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
                        AccountModel member = Manager.GetAuthoritativeAccount(id);
                        if (member == null)
                            continue;
                        root.Add(new XElement("Member",
                            new XElement("Name", member.Name ?? ""),
                            new XElement("Rank", member.GuildRank),
                            new XElement("Fame", member.Stats != null ? member.Stats.Fame : 0)));
                    }
                    data = Write(root.ToString());
                }
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne(30000);

            return data;
        }
    }
}
