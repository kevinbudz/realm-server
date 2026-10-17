using RotMG.Common;
using RotMG.Game;
using RotMG.Utils;
using System.Collections.Specialized;
using System.Net;
using System.Xml.Linq;

namespace RotMG.Networking
{
    public static partial class AppServer
    {
        private static byte[] CharList(HttpListenerContext context, NameValueCollection query)
        {
            return BuildCharList(query["username"], query["password"], GetIPFromContext(context));
        }

        //Only GuestAccount when no credentials were sent. A wrong password
        //must be "Account credentials not valid" so the client clears them
        //instead of silently showing the guest list.
        internal static byte[] BuildCharList(string username, string password, string ip)
        {
            XElement data = null;
            string error = null;

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                error = TryFillCharList(username, password, ip, out data);
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne(30000);

            return error != null ? WriteError(error) : Write(data.ToString());
        }

        internal static string TryFillCharList(string username, string password, string ip, out XElement data)
        {
            data = new XElement("Chars");
            bool credentialsSupplied = !string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password);
            AccountModel acc;
            if (!credentialsSupplied)
                acc = Database.GuestAccount();
            else
            {
                acc = Database.Verify(username, password, ip);
                if (acc == null)
                    return "Account credentials not valid";
            }

            if (Database.IsAccountInUse(acc, charListGrace: true))
                return "Account in use!";

            data.Add(new XAttribute("nextCharId", acc.NextCharId));
            data.Add(new XAttribute("maxNumChars", acc.MaxNumChars));
            data.Add(acc.Export());
            data.Add(Database.GetNews(acc));
            data.Add(new XElement("OwnedSkins", string.Join(",", acc.OwnedSkins)));
            foreach (int charId in acc.AliveChars)
            {
                CharacterModel character = Database.LoadCharacter(acc, charId);
                XElement export = character.Export();
                export.Add(new XAttribute("id", charId));
                data.Add(export);
            }
            return null;
        }

        private static byte[] Verify(HttpListenerContext context, NameValueCollection query)
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
                    data = WriteSuccess();
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne(30000);

            return data;
        }

        private static byte[] Register(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;
            string newUsername = query["newUsername"];
            string newPassword = query["newPassword"];

            if (!Database.IsValidUsername(newUsername))
                return WriteError("Invalid username.");

            if (!Database.IsValidPassword(newPassword))
                return WriteError("Invalid password.");

            _listenEvent.Reset();
            bool queued = false;
            Program.PushWork(() =>
            {
                //The reply waits for the commit, so the client never logs in before the account exists.
                RegisterStatus refusal;
                queued = Database.TryRegisterAccount(newUsername, newPassword, GetIPFromContext(context), out refusal, committed =>
                {
                    data = committed ? WriteSuccess() : WriteError("StorageError");
                    _listenEvent.Set();
                });
                if (!queued)
                    data = WriteError(refusal.ToString());
            }, () => { if (!queued) _listenEvent.Set(); });
            _listenEvent.WaitOne(30000);

            return data;
        }

        private static byte[] FameList(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;
            _listenEvent.Reset(); 
            Program.PushWork(() =>
            {
                data = Write(Database.GetLegends(query["timespan"]).ToString());
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne(30000);
            return data;
        }

        private static byte[] CharFame(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;
            int accId = int.Parse(query["accountId"]);
            int charId = int.Parse(query["charId"]);
            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                string legend = Database.GetLegend(accId, charId);
                data = string.IsNullOrWhiteSpace(legend) ? WriteError("Invalid character") : Write(legend);
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne(30000);
            return data;
        }

        private static byte[] CharFameStats(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            int charId;
            if (!int.TryParse(query["charId"], out charId))
                return WriteError("Invalid character");

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                {
                    data = WriteError("Account credentials not valid");
                    return;
                }

                //Prefer the live character so unsaved progress is included.
                AccountModel account;
                CharacterModel character;
                Client live = Manager.GetClient(acc.Id);
                if (live != null && live.Character != null && live.Character.Id == charId && live.Player != null)
                {
                    live.Player.SaveToCharacter();
                    account = live.Account;
                    character = live.Character;
                }
                else if (acc.AliveChars.Contains(charId))
                {
                    account = acc;
                    character = Database.LoadCharacter(acc, charId);
                }
                else
                {
                    data = WriteError("Invalid character");
                    return;
                }

                FameStats fameStats = Database.CalculateStats(account, character, "");

                XElement xml = new XElement("CharFameStats");
                XElement export = character.Export();
                export.Add(new XAttribute("id", charId));
                xml.Add(export);
                xml.Add(new XElement("Name", account.Name));
                xml.Add(new XElement("CreatedOn", character.CreationTime));
                xml.Add(character.FameStats.Export());

                //Every tracked dungeon, including zero counts, so the client shows only what this server version tracks.
                XElement dungeons = new XElement("Dungeons");
                foreach (string label in FameStatsInfo.DungeonLabels)
                {
                    int count;
                    character.FameStats.Dungeons.TryGetValue(label, out count);
                    dungeons.Add(new XElement("Dungeon", new XAttribute("id", label), count));
                }
                xml.Add(dungeons);

                xml.Add(new XElement("BaseFame", fameStats.BaseFame));
                xml.Add(new XElement("TotalFame", fameStats.TotalFame));

                XElement bonuses = new XElement("Bonuses");
                foreach (FameBonusLine line in Database.EvaluateBonuses(account, character, ""))
                    bonuses.Add(new XElement("Bonus",
                        new XAttribute("id", line.Name),
                        new XAttribute("level", line.MinLevel),
                        new XAttribute("percent", line.Percent),
                        new XAttribute("earned", line.Earned ? 1 : 0),
                        new XAttribute("desc", line.Description),
                        line.Fame));
                xml.Add(bonuses);

                data = Write(xml.ToString());
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne(30000);
            return data;
        }

        private static byte[] CharDelete(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            int charId = int.Parse(query["charId"]);

            _listenEvent.Reset();
            bool queued = false;
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                {
                    //The reply waits for the commit: the re-list that follows must not see the character still alive.
                    queued = Database.DeleteCharacter(acc, charId, ok =>
                    {
                        data = ok ? WriteSuccess() : WriteError("Issue deleting character");
                        _listenEvent.Set();
                    });
                    if (!queued)
                        data = WriteError("Issue deleting character");
                }
            }, () => { if (!queued) _listenEvent.Set(); });
            _listenEvent.WaitOne(30000);

            return data;
        }

        private static byte[] AccountPurchaseCharSlot(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];

            _listenEvent.Reset();
            bool queued = false;
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                {
                    queued = Database.BuyCharSlot(acc, ok =>
                    {
                        data = ok ? WriteSuccess() : WriteError("Could not buy character slot");
                        _listenEvent.Set();
                    });
                    if (!queued)
                        data = WriteError("Not enough fame");
                }
            }, () => { if (!queued) _listenEvent.Set(); });
            _listenEvent.WaitOne(30000);

            return data;
        }

        private static byte[] AccountPurchaseSkin(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            int skinType = int.Parse(query["skinType"]);

            _listenEvent.Reset();
            bool queued = false;
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                {
                    queued = Database.BuySkin(acc, skinType, ok =>
                    {
                        data = ok ? WriteSuccess() : WriteError("Could not buy skin");
                        _listenEvent.Set();
                    });
                    if (!queued)
                        data = WriteError("Could not buy skin");
                }
            }, () => { if (!queued) _listenEvent.Set(); });
            _listenEvent.WaitOne(30000);

            return data;
        }

        private static byte[] AccountChangePassword(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            string newPassword = query["newPassword"];

            _listenEvent.Reset();
            bool queued = false;
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                {
                    queued = Database.ChangePassword(acc, newPassword, ok =>
                    {
                        data = ok ? WriteSuccess() : WriteError("Could not change password");
                        _listenEvent.Set();
                    });
                    if (!queued)
                        data = WriteError("Could not change password");
                }
            }, () => { if (!queued) _listenEvent.Set(); });
            _listenEvent.WaitOne(30000);

            return data;
        }
    }
}
