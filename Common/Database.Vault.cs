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
            string[] parts = new string[types.Length * 2];
            for (int i = 0; i < types.Length; i++)
            {
                parts[i * 2] = types[i].ToString();
                parts[i * 2 + 1] = datas[i].ToString();
            }
            SetKey($"vault.{accountId}.{index}", string.Join(",", parts));
        }
    }
}
