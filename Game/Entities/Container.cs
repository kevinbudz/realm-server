using RotMG.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Entities
{
    public interface IContainer
    {
        public int[] Inventory { get; set; }
        public int[] ItemDatas { get; set; }
        public void UpdateInventory();
        public void UpdateInventorySlot(int slot);
        public bool ValidSlot(int slot);
    }

    public class Container : Entity, IContainer
    {
        public const int MaxSlots = 8;

        public const ushort BrownBag = 0x0500;
        public const ushort PinkBag = 0x0506;
        public const ushort PurpleBag = 0x0507;
        public const ushort BasketBag = 0x0508;
        public const ushort CyanBag = 0x0509;
        public const ushort BlueBag = 0x050B;
        public const ushort WhiteBag = 0x050C;
        public const ushort BoostBag = 0x0510;

        public static ushort FromBagType(int bagType) 
        {
            switch (bagType) 
            {
                case 0: return BrownBag;
                case 1: return PinkBag;
                case 2: return PurpleBag;
                case 3: return BasketBag;
                case 4: return CyanBag;
                case 5: return BlueBag;
                case 6: return WhiteBag;
                case 7: return BoostBag;
                case 8: return WhiteBag;
                default: return WhiteBag;
            }
        }

        public int OwnerId = -1;
        public bool Persistent;
        //Write-through link to a persistent vault chest (see Database.Vault).
        public int VaultOwnerId = -1;
        public int VaultIndex = -1;
        public int[] Inventory { get; set; }
        public int[] ItemDatas { get; set; }

        public Container(ushort type) : this(type, -1, null)
        {
            Persistent = true;
        }

        public Container(ushort type, int ownerId, int? lifetime) : base(type, lifetime)
        {
            OwnerId = ownerId;
            Inventory = new int[MaxSlots];
            ItemDatas = new int[MaxSlots];
            for (int i = 0; i < MaxSlots; i++)
            {
                Inventory[i] = -1;
                ItemDatas[i] = -1;
            }
        }

        public override void Tick()
        {
            if (Persistent)
            {
                base.Tick();
                return;
            }

            bool disappear = true;
            for (int i = 0; i < MaxSlots; i++)
                if (Inventory[i] != -1)
                {
                    disappear = false;
                    break;
                }

            if (disappear)
            {
                Parent.RemoveEntity(this);
                return;
            }

            base.Tick();
        }

        public bool ValidSlot(int slot)
        {
            if (slot < 0 || slot >= MaxSlots)
                return false;
            return true;
        }

        public void UpdateInventory()
        {
            for (int k = 0; k < MaxSlots; k++)
                UpdateInventorySlot(k);
            UpdateVaultCountName();
        }

        //Vault chests show their fill level ("0/8".."8/8") as an overhead
        //name, mirroring realm-portal occupancy counts. Non-vault
        //containers are untouched.
        public void UpdateVaultCountName()
        {
            if (Desc == null || Desc.DisplayId != "Vault Chest")
                return;
            int filled = 0;
            for (int i = 0; i < MaxSlots; i++)
                if (Inventory[i] != -1)
                    filled++;
            TrySetSV(StatType.Name, filled + "/" + MaxSlots);
        }

        public void UpdateInventorySlot(int slot)
        {
#if DEBUG
            if (slot < 0 || slot >= MaxSlots)
                throw new Exception("Out of bounds slot update attempt.");
#endif
            switch (slot)
            {
                case 0: 
                    SetSV(StatType.Inventory_0, Inventory[0]);
                    SetSV(StatType.ItemData_0, ItemDatas[0]);
                    break;
                case 1: 
                    SetSV(StatType.Inventory_1, Inventory[1]);
                    SetSV(StatType.ItemData_1, ItemDatas[1]);
                    break;
                case 2: 
                    SetSV(StatType.Inventory_2, Inventory[2]);
                    SetSV(StatType.ItemData_2, ItemDatas[2]);
                    break;
                case 3: 
                    SetSV(StatType.Inventory_3, Inventory[3]);
                    SetSV(StatType.ItemData_3, ItemDatas[3]);
                    break;
                case 4: 
                    SetSV(StatType.Inventory_4, Inventory[4]);
                    SetSV(StatType.ItemData_4, ItemDatas[4]);
                    break;
                case 5: 
                    SetSV(StatType.Inventory_5, Inventory[5]);
                    SetSV(StatType.ItemData_5, ItemDatas[5]);
                    break;
                case 6: 
                    SetSV(StatType.Inventory_6, Inventory[6]);
                    SetSV(StatType.ItemData_6, ItemDatas[6]);
                    break;
                case 7:
                    SetSV(StatType.Inventory_7, Inventory[7]);
                    SetSV(StatType.ItemData_7, ItemDatas[7]);
                    break;
            }

            UpdateVaultCountName();

            //No database write here by design. Vault chests used to write
            //through on every slot mutation while the swapping player's
            //inventory only saved on disconnect, so a crash in between
            //duplicated vaulted items. Vault rows now commit atomically with
            //the player row at the mutation sites (see
            //Player.PersistInventoryMutation and Player.UseItem).
        }
    }
}
