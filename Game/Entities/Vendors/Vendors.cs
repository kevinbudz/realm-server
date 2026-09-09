using RotMG.Common;
using RotMG.Networking;
using System.Collections.Generic;

namespace RotMG.Game.Entities.Vendors
{
    //Base class for purchasable objects, ported from realm-src-master
    //wServer/realm/entities/vendors/SellableObject.cs.
    public abstract class SellableObject : StaticObject
    {
        public int Price;
        public CurrencyType Currency;
        public int RankReq;

        protected SellableObject(ushort type) : base(type)
        {
            Price = 0;
            Currency = CurrencyType.Gold;
            RankReq = 0;
            SyncMerchantStats();
        }

        public void SyncMerchantStats()
        {
            SetSV(StatType.MerchandisePrice, Price);
            SetSV(StatType.MerchandiseCurrency, (int)Currency);
            SetSV(StatType.MerchandiseRankReq, RankReq);
        }

        public virtual void Buy(Player player)
        {
            SendFailed(player, "Purchase Error: Uninitialized.");
        }

        protected string ValidateCustomer(Player player)
        {
            if (Database.GetStars(player.Client.Account) < RankReq)
                return "Purchase Error: Insufficient Rank.";
            if (player.GetCurrency(Currency) < Price)
                return "Purchase Error: Insufficient Funds.";
            return null;
        }

        protected void SendFailed(Player player, string message)
        {
            player.Client.Send(GameServer.BuyResult(1, message));
        }

        protected void SendSuccess(Player player, string message)
        {
            player.Client.Send(GameServer.BuyResult(0, message));
        }
    }

    //Rotating-stock shop merchant, ported from
    //wServer/realm/entities/vendors/Merchant.cs.
    public class Merchant : SellableObject
    {
        public ushort Item;
        public int Count;
        public int ReloadOffset;
        public bool Rotate = true;
        public List<ShopItem> ItemList;
        public ShopItem ShopItem;

        protected volatile bool BeingPurchased;
        protected volatile bool AwaitingReload;

        public Merchant(ushort type) : base(type)
        {
            SyncStockStats();
        }

        public void SyncStockStats()
        {
            SetSV(StatType.MerchandiseType, (int)Item);
            SetSV(StatType.MerchandiseCount, Count);
            SetSV(StatType.MerchandiseMinsLeft, -1);
        }

        public override void Tick()
        {
            base.Tick();
            if (Parent == null || Parent.Players.Count == 0)
                return;

            int cycle = Manager.TotalTimeUnsynced % 20000;
            if (AwaitingReload || (cycle - Settings.MillisecondsPerTick <= ReloadOffset && cycle > ReloadOffset))
            {
                if (!AwaitingReload && !Rotate)
                    return;
                if (AnyPlayerNearby(2))
                {
                    AwaitingReload = true;
                    return;
                }
                if (BeingPurchased)
                {
                    AwaitingReload = true;
                    return;
                }
                Reload();
                AwaitingReload = false;
            }
        }

        protected bool AnyPlayerNearby(float radius)
        {
            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, radius))
                if (en is Player)
                    return true;
            return false;
        }

        public virtual void Reload()
        {
            if (ItemList == null || ItemList.Count == 0)
                return;
            ShopItem = ItemList[Utils.MathUtils.Next(ItemList.Count)];
            Item = ShopItem.ItemId;
            Price = ShopItem.Price;
            Count = ShopItem.Count;
            SyncMerchantStats();
            SyncStockStats();
        }

        public override void Buy(Player player)
        {
            if (BeingPurchased)
            {
                SendFailed(player, "Purchase Error: Item is currently being purchased.");
                return;
            }
            BeingPurchased = true;

            try
            {
                if (ShopItem == null || Item == 0)
                {
                    SendFailed(player, "Purchase Error: Uninitialized.");
                    return;
                }

                ItemDesc desc;
                if (!Resources.Type2Item.TryGetValue(Item, out desc))
                {
                    SendFailed(player, "Purchase Error: Transaction failed.");
                    return;
                }

                string error = ValidateCustomer(player);
                if (error != null)
                {
                    SendFailed(player, error);
                    return;
                }

                if (player.GetFreeInventorySlot() == -1)
                {
                    SendFailed(player, "Purchase Error: You don't have enough inventory slots.");
                    return;
                }

                if (!player.TryDeduct(Currency, Price))
                {
                    SendFailed(player, "Purchase Error: Insufficient Funds.");
                    return;
                }

                player.GiveItem(Item);
                player.Credits = player.Client.Account.Stats.Credits;
                player.Fame = player.Client.Account.Stats.Fame;
                SendSuccess(player, "Item purchased!");

                if (Count != -1 && --Count <= 0)
                {
                    Reload();
                    AwaitingReload = false;
                }
                SyncStockStats();
            }
            finally
            {
                BeingPurchased = false;
            }
        }
    }

    //Standard world shopkeeper, ported from WorldMerchant.cs.
    public class WorldMerchant : Merchant
    {
        public WorldMerchant(ushort type) : base(type) { }
    }

    //Guild hall upgrade vendor, ported from GuildMerchant.cs. Upgrades spend
    //guild fame and raise the hall level, which selects the hall map tier.
    public class GuildMerchant : SellableObject
    {
        private static readonly int[] HallPrices = { 10000, 100000, 250000 };
        private static readonly int[] HallLevels = { 1, 2, 3 };

        public int UpgradeLevel;

        public GuildMerchant(ushort type) : base(type)
        {
            Currency = CurrencyType.GuildFame;
            Price = int.MaxValue;
            UpgradeLevel = 0;
            int[] hallTypes = { 0x736, 0x737, 0x738 };
            for (int i = 0; i < hallTypes.Length; i++)
                if (type == hallTypes[i])
                {
                    Price = HallPrices[i];
                    UpgradeLevel = HallLevels[i];
                }
            SyncMerchantStats();
        }

        public override void Buy(Player player)
        {
            if (string.IsNullOrWhiteSpace(player.GuildName) || player.GuildRank < 30)
            {
                player.SendError("Verification failed.");
                return;
            }
            if (UpgradeLevel == 0)
            {
                SendFailed(player, "Purchase Error: Uninitialized.");
                return;
            }
            if (player.GetCurrency(CurrencyType.GuildFame) < Price)
            {
                player.Client.Send(GameServer.BuyResult(9, "Not enough Guild Fame!"));
                return;
            }
            if (!Database.ChangeGuildLevel(player.GuildName, UpgradeLevel))
            {
                player.SendError("Upgrade must be purchased in order.");
                return;
            }
            if (!player.TryDeduct(CurrencyType.GuildFame, Price))
            {
                player.Client.Send(GameServer.BuyResult(9, "Not enough Guild Fame!"));
                return;
            }
            player.Client.Send(GameServer.BuyResult(0, "Upgrade successful! Please leave the Guild Hall to have it upgraded."));
            player.SendGuild(player.Name + " has upgraded the guild hall to level " + UpgradeLevel + "!");
        }
    }

    //Purchasable extra vault chest, ported from ClosedVaultChest.cs. Buying
    //increments the account's persistent chest count and converts this vendor
    //into a linked storage chest, mirroring Vault.AddChest upstream.
    public class ClosedVaultChest : SellableObject
    {
        public ClosedVaultChest(ushort type) : base(type)
        {
            Price = Database.VaultChestPrice;
            Currency = CurrencyType.Fame;
            SyncMerchantStats();
        }

        public override void Buy(Player player)
        {
            string error = ValidateCustomer(player);
            if (error != null)
            {
                SendFailed(player, error);
                return;
            }
            if (!player.TryDeduct(Currency, Price))
            {
                SendFailed(player, "Purchase Error: Insufficient Funds.");
                return;
            }

            if (!Resources.Id2Object.TryGetValue("Vault Chest", out ObjectDesc chestDesc))
            {
                SendFailed(player, "Purchase Error: Transaction failed.");
                return;
            }

            int index = Database.GetVaultCount(player.Client.Account);
            Database.SetVaultCount(player.Client.Account, index + 1);

            Container chest = new Container(chestDesc.Type)
            {
                VaultOwnerId = player.Client.Account.Id,
                VaultIndex = index
            };
            Database.GetVaultItems(chest.VaultOwnerId, index, chest.Inventory, chest.ItemDatas);

            Position at = Position;
            player.Parent.RemoveStatic((int)at.X, (int)at.Y);
            if (player.Parent.AddEntity(chest, at) == -1)
            {
                SendFailed(player, "Purchase Error: Transaction failed.");
                return;
            }

            player.Fame = player.Client.Account.Stats.Fame;
            SendSuccess(player, "Vault chest purchased!");
        }
    }
}
