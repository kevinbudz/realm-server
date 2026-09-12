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
            Program.PushWork(() =>
            {
                RegisterStatus status = Database.RegisterAccount(newUsername, newPassword, GetIPFromContext(context));
                if (status == RegisterStatus.Success)
                    data = WriteSuccess();
                else data = WriteError(status.ToString());
            }, () => _listenEvent.Set());
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

        private static byte[] CharDelete(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            int charId = int.Parse(query["charId"]);

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = Database.DeleteCharacter(acc, charId) ? WriteSuccess() : WriteError("Issue deleting character");
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne(30000);

            return data;
        }

        private static byte[] AccountPurchaseCharSlot(HttpListenerContext context, NameValueCollection query)
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
                    data = Database.BuyCharSlot(acc) ? WriteSuccess() : WriteError("Not enough fame");
            }, () => _listenEvent.Set());
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
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = Database.BuySkin(acc, skinType) ? WriteSuccess() : WriteError("Could not buy skin");
            }, () => _listenEvent.Set());
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
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = Database.ChangePassword(acc, newPassword) ? WriteSuccess() : WriteError("Could not change password");
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne(30000);

            return data;
        }

        public static bool VerifyCharListCredentials()
        {
            static bool Fail(string msg)
            {
                Program.Print(PrintType.Error, "P22 verify: " + msg);
                return false;
            }

            string guestErr = TryFillCharList(null, null, "p22-verify-guest", out XElement guest);
            if (guestErr != null)
                return Fail($"empty credentials returned '{guestErr}'");
            if (guest == null || guest.Name != "Chars")
                return Fail("empty credentials did not return a guest <Chars> list");
            Program.Print(PrintType.Info, "P22 verify: missing credentials still yield a guest character list");

            string badErr = TryFillCharList("p22nosuchuser", "wrong-password", "p22-verify-bad", out _);
            if (badErr != "Account credentials not valid")
                return Fail($"wrong password returned '{badErr}', expected 'Account credentials not valid'");
            Program.Print(PrintType.Info, "P22 verify: wrong password returns Account credentials not valid");

            AccountModel acc = new AccountModel(910022, skipReload: true)
            {
                Connected = true,
                Stats = new StatsInfo { ClassStats = new ClassStatsInfo[0] }
            };
            Client live = new Client(new SendState(), new ReceiveState());
            live.Id = 910022;
            live.Active = false;
            live.Reconnecting = true;
            live.State = ProtocolState.Connected;
            lock (Manager.SyncRoot)
            {
                Manager.Clients[live.Id] = live;
                Manager.AccountIdToClientId[acc.Id] = live.Id;
            }
            try
            {
                if (!Database.IsAccountInUse(acc))
                    return Fail("strict IsAccountInUse allowed a live transferring client");
                if (Database.IsAccountInUse(acc, charListGrace: true))
                    return Fail("char-list grace still treated a transferring client as in use");
                live.Reconnecting = false;
                live.Active = true;
                if (!Database.IsAccountInUse(acc, charListGrace: true))
                    return Fail("char-list grace ignored an Active session");
            }
            finally
            {
                lock (Manager.SyncRoot)
                {
                    Manager.Clients.Remove(live.Id);
                    Manager.AccountIdToClientId.Remove(acc.Id);
                }
            }
            Program.Print(PrintType.Info, "P22 verify: transferring (Active=false) clients do not block /char/list");
            return true;
        }
    }
}
