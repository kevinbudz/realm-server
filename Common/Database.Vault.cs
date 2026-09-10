using System;
using System.Linq;

namespace RotMG.Common
{
    //Per-account vault chest persistence, mirroring realm-src-master
    //common/DbModels.cs DbVault/DbVaultSingle (vault.<accountId> records
    //holding one item array per chest).
    public static partial class Database
    {
        public const int VaultSlots = 8;
        public const int VaultChestPrice = 750;

        public static int GetVaultCount(AccountModel acc)
        {
            string raw = GetKey($"vault.{acc.Id}.count");
            if (int.TryParse(raw, out int count) && count > 0)
                return count;
            return 1;
        }

        public static void SetVaultCount(AccountModel acc, int count)
        {
            SetKey($"vault.{acc.Id}.count", Math.Max(1, count).ToString());
        }

        public static void GetVaultItems(int accountId, int index, int[] types, int[] datas)
        {
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = -1;
                datas[i] = -1;
            }

            string raw = GetKey($"vault.{accountId}.{index}");
            if (string.IsNullOrWhiteSpace(raw))
                return;

            string[] parts = raw.Split(',');
            for (int i = 0; i < types.Length && i * 2 + 1 < parts.Length; i++)
            {
                if (int.TryParse(parts[i * 2], out int type))
                    types[i] = type;
                if (int.TryParse(parts[i * 2 + 1], out int data))
                    datas[i] = data;
            }
        }

        public static void SetVaultItems(int accountId, int index, int[] types, int[] datas)
        {
            SetKey($"vault.{accountId}.{index}", VaultValue(types, datas));
        }

        //Buys one more vault chest: fame check, deduct, chest-count bump and
        //the account/character rows commit in ONE transaction, with the count
        //read inside the transaction. Two rapid Buy packets used to both read
        //the same count and both write count+1 (double charge, one chest);
        //a crash between deduct and count used to charge for nothing.
        //Returns the new chest index, or -1 when funds are insufficient.
        public static int BuyVaultChestSlot(AccountModel acc, CharacterModel ch, int price)
        {
            if (acc.Stats.Fame < price)
                return -1;
            acc.Stats.Fame -= price;
            string accountXml = acc.Export(false).ToString();
            string charXml = ch.Export(false).ToString();
            int index = -1;
            Transact(conn =>
            {
                int count = 1;
                int.TryParse(GetKeyInTx(conn, $"vault.{acc.Id}.count"), out count);
                if (count < 1)
                    count = 1;
                index = count;
                UpsertKeyInTx(conn, $"vault.{acc.Id}.count", (count + 1).ToString());
                UpsertKeyInTx(conn, AccountKey(acc.Id), accountXml);
                UpsertKeyInTx(conn, CharacterKey(acc.Id, ch.Id), charXml);
            });
            acc.Data = System.Xml.Linq.XElement.Parse(accountXml);
            ch.Data = System.Xml.Linq.XElement.Parse(charXml);
            return index;
        }
    }
}
