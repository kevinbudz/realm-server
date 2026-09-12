using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        private const float ContainerMinimumDistance = 2f;

        private const int MaxSlotsWithoutBackpack = 12;
        private const int MaxSlotsWithBackpack = MaxSlotsWithoutBackpack + 8;

        private const byte HealthPotionSlotId = 254;
        private const byte MagicPotionSlotId = 255;

        private const int HealthPotionItemType = 2594;
        private const int MagicPotionItemType = 2595;

        private static byte[] InvalidInvSwap = GameServer.InvResult(1);
        private static byte[] ValidInvSwap = GameServer.InvResult(0);

        //From IContainer :)
        public int[] Inventory { get; set; }
        public int[] ItemDatas { get; set; }

        //InvResult has no correlation id, so every InvSwap/InvDrop/UseItem
        //must reply. On rejection, re-queue the live slot values (including
        //ITEMDATA_*) so the next NewTick is a safety net if the client's
        //FIFO restore races a bag that has already left sight.
        private void ReplyInv(bool success)
        {
            Client.Send(success ? ValidInvSwap : InvalidInvSwap);
        }

        private void RejectInventory(IContainer a, int slotA, IContainer b = null, int slotB = -1)
        {
            QueueAuthoritativeSlot(a, slotA);
            if (b != null && slotB >= 0)
                QueueAuthoritativeSlot(b, slotB);
            ReplyInv(false);
        }

        private void QueueAuthoritativeSlot(IContainer con, int slot)
        {
            if (con == null)
                return;
            if (con.ValidSlot(slot))
                con.UpdateInventorySlot(slot);
            ForceContainerStatus(con as Entity);
        }

        //NewTick for non-players sends NewSVs (deltas). If those were
        //cleared after the previous tick, bumping UpdateCount alone would
        //emit an empty ObjectStatus. Copy SVs so the bag goes out in full.
        internal static void ForceContainerStatus(Entity en)
        {
            if (en == null || en is Player)
                return;
            foreach (KeyValuePair<StatType, object> kv in en.SVs)
                en.NewSVs[kv.Key] = kv.Value;
            en.UpdateCount++;
        }

        public void InitInventory(CharacterModel character)
        {
            Inventory = character.Inventory.ToArray();
            ItemDatas = character.ItemDatas.ToArray();
            UpdateInventory();
        }

        public void RecalculateEquipBonuses()
        {
            for (int i = 0; i < 8; i++)
                Boosts[i] = 0;

            for (int i = 0; i < 4; i++)
            {
                if (Inventory[i] == -1)
                    continue;

                ItemDesc item = Resources.Type2Item[(ushort)Inventory[i]];
                //XML 'stat' ids are vanilla network ids (e.g. 26/27/28 for
                //Vit/Wis/Dex); translate to internal boost slots. Unknown ids
                //are ignored instead of indexing Boosts out of range.
                foreach (KeyValuePair<int, int> s in item.StatBoosts)
                {
                    int boost = ItemDesc.GetBoostIndex(s.Key);
                    if (boost != -1)
                        Boosts[boost] += s.Value;
                }

                int data = ItemDatas[i];
                if (data == -1)
                    continue;

                Boosts[0] += (int)(ItemDesc.GetStat(data, ItemData.MaxHP, 5));
                Boosts[1] += (int)(ItemDesc.GetStat(data, ItemData.MaxMP, 5));
                Boosts[2] += (int)(ItemDesc.GetStat(data, ItemData.Attack, 1));
                Boosts[3] += (int)(ItemDesc.GetStat(data, ItemData.Defense, 1));
                Boosts[4] += (int)(ItemDesc.GetStat(data, ItemData.Speed, 1));
                Boosts[5] += (int)(ItemDesc.GetStat(data, ItemData.Dexterity, 1));
                Boosts[6] += (int)(ItemDesc.GetStat(data, ItemData.Vitality, 1));
                Boosts[7] += (int)(ItemDesc.GetStat(data, ItemData.Wisdom, 1));
            }

            for (int i = 0; i < 8; i++)
                Boosts[i] += ActivateBoosts[i];

            UpdateStats();
        }

        public ItemDesc GetItem(int index)
        {
#if DEBUG
            if (index < 0 || index > (HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack))
                throw new Exception("GetItem index out of bounds");
#endif
            if (Inventory[index] == -1)
                return null;
            return Resources.Type2Item[(ushort)Inventory[index]];
        }

        public bool GiveItem(ushort type)
        {
            int slot = GetFreeInventorySlot();
            if (slot == -1)
                return false;
            Inventory[slot] = type;
            UpdateInventorySlot(slot);
            return true;
        }

        public int GetFreeInventorySlot()
        {
            int maxSlots = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            for (int i = 4; i < maxSlots; i++)
                if (Inventory[i] == -1)
                    return i;
            return -1;
        }

        public int CountFreeInventorySlots()
        {
            int maxSlots = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            int free = 0;
            for (int i = 4; i < maxSlots; i++)
                if (Inventory[i] == -1)
                    free++;
            return free;
        }

        public void DropItem(byte slot)
        {
            CancelTradeIfTrading();

            if (!ValidSlot(slot))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid slot");
#endif
                ReplyInv(false);
                return;
            }

            QueueAuthoritativeSlot(this, slot);

            int item = Inventory[slot];
            int data = ItemDatas[slot];
            if (item == -1)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Nothing to drop");
#endif
                ReplyInv(false);
                return;
            }

            Inventory[slot] = -1;
            ItemDatas[slot] = -1;
            UpdateInventorySlot(slot);

            RecalculateEquipBonuses();
            ReplyInv(true);
            //The dropped bag is ephemeral, but the removal from this player's
            //inventory is durable: the commit lands before the drop becomes
            //visible, or a crash resurrects the item while someone else
            //already looted it. The bag therefore spawns from the commit
            //completion (next tick) instead of inline; the tick thread never
            //waits for the fsync.
            World dropWorld = Parent;
            Position dropAt = Position + MathUtils.Position(.2f, .2f);
            try
            {
                SaveToCharacter();
                string accountXml = Client.Account.Export(false).ToString();
                string charXml = Client.Character.Export(false).ToString();
                Dictionary<string, string> writes = new Dictionary<string, string>
                {
                    { Database.AccountKey(Client.Account.Id), accountXml },
                    { Database.CharacterKey(Client.Account.Id, Client.Character.Id), charXml }
                };
                Client.Account.Data = System.Xml.Linq.XElement.Parse(accountXml);
                Client.Character.Data = System.Xml.Linq.XElement.Parse(charXml);
                Database.WriteAtomicallyAsync(writes, null, () =>
                    SpawnDroppedBag(dropWorld, dropAt, item, data, slot));
            }
            catch { }
        }

        //Commit-gated half of DropItem: runs on the main thread after the
        //inventory removal is durable. If the world went away (or the player
        //left/died) the bag has nowhere to land, so the item goes back into
        //the player's inventory and re-queues instead of vanishing.
        private void SpawnDroppedBag(World world, Position at, int item, int data, int slot)
        {
            try
            {
                if (world == null || Manager.GetWorld(world.Id) != world || Parent != world)
                {
                    RestoreDroppedItem(item, data, slot);
                    return;
                }
                Container container = new Container(Container.PurpleBag, Id, 120000);
                container.Inventory[0] = item;
                container.ItemDatas[0] = data;
                container.UpdateInventorySlot(0);
                if (world.AddEntity(container, at) == -1)
                    RestoreDroppedItem(item, data, slot);
            }
            catch
            {
                try { RestoreDroppedItem(item, data, slot); } catch { }
            }
        }

        private void RestoreDroppedItem(int item, int data, int slot)
        {
            if (Parent == null || Client == null || Client.Character == null)
                return;
            if (slot >= 0 && slot < Inventory.Length && Inventory[slot] == -1)
            {
                Inventory[slot] = item;
                ItemDatas[slot] = data;
            }
            else
            {
                int free = GetFreeInventorySlot();
                if (free == -1)
                    return;
                Inventory[free] = item;
                ItemDatas[free] = data;
                slot = free;
            }
            UpdateInventorySlot(slot);
            RecalculateEquipBonuses();
            try
            {
                SaveToCharacter();
                Database.SaveAccountAndCharacter(Client.Account, Client.Character);
            }
            catch { }
        }

        public void SwapItem(SlotData slot1, SlotData slot2)
        {
            CancelTradeIfTrading();
            Entity en1 = Parent.GetEntity(slot1.ObjectId);
            Entity en2 = Parent.GetEntity(slot2.ObjectId);

            (en1 as IContainer)?.UpdateInventorySlot(slot1.SlotId);
            (en2 as IContainer)?.UpdateInventorySlot(slot2.SlotId);
            
            //Undefined entities
            if (en1 == null || en2 == null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Undefined entities");
#endif
                RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                return;
            }
            
            //Entities which are not containers???
            if (!(en1 is IContainer) || !(en2 is IContainer))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Not containers");
#endif
                RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                return;
            }

            if (en1.Position.Distance(en2) > ContainerMinimumDistance)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Too far away from container");
#endif
                RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                return;
            }

            //Player manipulation attempt
            if ((en1 is Player && slot1.ObjectId != Id) ||
                (en2 is Player && slot2.ObjectId != Id))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Player manipulation attempt");
#endif
                RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                return;
            }

            //Container manipulation attempt
            if ((en1 is Container && 
                (en1 as Container).OwnerId != -1 && 
                Id != (en1 as Container).OwnerId) ||
             (en2 is Container && 
             (en2 as Container).OwnerId != -1 && 
             Id != (en2 as Container).OwnerId))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Container manipulation attempt");
#endif
                RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                return;
            }

            IContainer con1 = en1 as IContainer;
            IContainer con2 = en2 as IContainer;

            //Invalid slots
            if (!con1.ValidSlot(slot1.SlotId) || !con2.ValidSlot(slot2.SlotId))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid inv swap");
#endif
                RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                return;
            }

            //Invalid slot types
            int item1 = con1.Inventory[slot1.SlotId];
            int data1 = con1.ItemDatas[slot1.SlotId];
            int item2 = con2.Inventory[slot2.SlotId];
            int data2 = con2.ItemDatas[slot2.SlotId];

            //One-way containers (vault/gift chests) only dispense.
            if ((en1 is OneWayContainer && item2 != -1) ||
                (en2 is OneWayContainer && item1 != -1))
            {
#if DEBUG
                Program.Print(PrintType.Error, "One-way container deposit attempt");
#endif
                RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                return;
            }
            PlayerDesc d = Desc as PlayerDesc;
            ItemDesc d1;
            ItemDesc d2;
            Resources.Type2Item.TryGetValue((ushort)item1, out d1);
            Resources.Type2Item.TryGetValue((ushort)item2, out d2);

            if (con1 is Player)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (slot1.SlotId == i)
                    {
                        if ((d1 != null && d.SlotTypes[i] != d1.SlotType) ||
                            (d2 != null && d.SlotTypes[i] != d2.SlotType))
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Invalid slot type");
#endif
                            RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                            return;
                        }
                    }
                }
            }

            if (con2 is Player)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (slot2.SlotId == i)
                    {
                        if ((d1 != null && d.SlotTypes[i] != d1.SlotType) ||
                            (d2 != null && d.SlotTypes[i] != d2.SlotType))
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Invalid slot type");
#endif
                            RejectInventory(en1 as IContainer, slot1.SlotId, en2 as IContainer, slot2.SlotId);
                            return;
                        }
                    }
                }
            }

            con1.Inventory[slot1.SlotId] = item2;
            con1.ItemDatas[slot1.SlotId] = data2;
            con2.Inventory[slot2.SlotId] = item1;
            con2.ItemDatas[slot2.SlotId] = data1;
            con1.UpdateInventorySlot(slot1.SlotId);
            con2.UpdateInventorySlot(slot2.SlotId);
            RecalculateEquipBonuses();
            PersistInventoryMutation(en1 as Container, en2 as Container);
            ReplyInv(true);
        }

        //Persists this swap before acknowledging it. Ground bags are
        //ephemeral (a crash simply undoes the move), but vault chests and the
        //player row are durable: they must commit in ONE transaction or a
        //crash between them duplicates vaulted items. Vault swaps wait for
        //the commit before InvResult goes out, so the client never believes
        //a swap survived that did not; ground swaps queue FIFO behind them.
        private void PersistInventoryMutation(Container c1, Container c2)
        {
            try
            {
                SaveToCharacter();
                string accountXml = Client.Account.Export(false).ToString();
                string charXml = Client.Character.Export(false).ToString();
                Dictionary<string, string> writes = new Dictionary<string, string>
                {
                    { Database.AccountKey(Client.Account.Id), accountXml },
                    { Database.CharacterKey(Client.Account.Id, Client.Character.Id), charXml }
                };
                bool vaultInvolved = false;
                foreach (Container c in new[] { c1, c2 })
                {
                    if (c == null || c.VaultOwnerId == -1 || c.VaultIndex < 0)
                        continue;
                    vaultInvolved = true;
                    writes[Database.VaultItemsKey(c.VaultOwnerId, c.VaultIndex)] =
                        Database.VaultValue(c.Inventory, c.ItemDatas);
                }
                if (vaultInvolved)
                    Database.WriteAtomicallyAndWait(writes);
                else
                    Database.WriteAtomically(writes);
                Client.Account.Data = System.Xml.Linq.XElement.Parse(accountXml);
                Client.Character.Data = System.Xml.Linq.XElement.Parse(charXml);
            }
            catch { }
        }

        public bool ValidSlot(int slot)
        {
            int maxSlots = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            if (slot < 0 || slot >= maxSlots)
                return false;
            return true;
        }

        public void UpdateInventory()
        {
            int length = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            for (int k = 0; k < length; k++)
                UpdateInventorySlot(k);
        }

        public void UpdateInventorySlot(int slot)
        {
#if DEBUG
            if (!HasBackpack && slot >= MaxSlotsWithoutBackpack)
                throw new Exception("Should not be updating backpack stats when there is no backpack present.");
            if (slot < 0 || slot >= MaxSlotsWithBackpack)
                throw new Exception("Out of bounds slot update attempt.");
#endif
            switch (slot)
            {
                case 0: 
                    SetSV(StatType.Inventory_0, Inventory[0]);
                    SetPrivateSV(StatType.ItemData_0, ItemDatas[0]);
                    break;
                case 1: 
                    SetSV(StatType.Inventory_1, Inventory[1]);
                    SetPrivateSV(StatType.ItemData_1, ItemDatas[1]);
                    break;
                case 2: 
                    SetSV(StatType.Inventory_2, Inventory[2]);
                    SetPrivateSV(StatType.ItemData_2, ItemDatas[2]);
                    break;
                case 3: 
                    SetSV(StatType.Inventory_3, Inventory[3]);
                    SetPrivateSV(StatType.ItemData_3, ItemDatas[3]);
                    break;
                case 4: 
                    SetPrivateSV(StatType.Inventory_4, Inventory[4]);
                    SetPrivateSV(StatType.ItemData_4, ItemDatas[4]);
                    break;
                case 5: 
                    SetPrivateSV(StatType.Inventory_5, Inventory[5]);
                    SetPrivateSV(StatType.ItemData_5, ItemDatas[5]);
                    break;
                case 6: 
                    SetPrivateSV(StatType.Inventory_6, Inventory[6]);
                    SetPrivateSV(StatType.ItemData_6, ItemDatas[6]);
                    break;
                case 7: 
                    SetPrivateSV(StatType.Inventory_7, Inventory[7]);
                    SetPrivateSV(StatType.ItemData_7, ItemDatas[7]);
                    break;
                case 8: 
                    SetPrivateSV(StatType.Inventory_8, Inventory[8]);
                    SetPrivateSV(StatType.ItemData_8, ItemDatas[8]);
                    break;
                case 9: 
                    SetPrivateSV(StatType.Inventory_9, Inventory[9]);
                    SetPrivateSV(StatType.ItemData_9, ItemDatas[9]);
                    break;
                case 10: 
                    SetPrivateSV(StatType.Inventory_10, Inventory[10]);
                    SetPrivateSV(StatType.ItemData_10, ItemDatas[10]);
                    break;
                case 11: 
                    SetPrivateSV(StatType.Inventory_11, Inventory[11]);
                    SetPrivateSV(StatType.ItemData_11, ItemDatas[11]);
                    break;
                case 12: 
                    SetPrivateSV(StatType.Backpack_0, Inventory[12]);
                    SetPrivateSV(StatType.ItemData_12, ItemDatas[12]);
                    break;
                case 13: 
                    SetPrivateSV(StatType.Backpack_1, Inventory[13]);
                    SetPrivateSV(StatType.ItemData_13, ItemDatas[13]);
                    break;
                case 14: 
                    SetPrivateSV(StatType.Backpack_2, Inventory[14]);
                    SetPrivateSV(StatType.ItemData_14, ItemDatas[14]);
                    break;
                case 15: 
                    SetPrivateSV(StatType.Backpack_3, Inventory[15]);
                    SetPrivateSV(StatType.ItemData_15, ItemDatas[15]);
                    break;
                case 16: 
                    SetPrivateSV(StatType.Backpack_4, Inventory[16]);
                    SetPrivateSV(StatType.ItemData_16, ItemDatas[16]);
                    break;
                case 17: 
                    SetPrivateSV(StatType.Backpack_5, Inventory[17]);
                    SetPrivateSV(StatType.ItemData_17, ItemDatas[17]);
                    break;
                case 18: 
                    SetPrivateSV(StatType.Backpack_6, Inventory[18]);
                    SetPrivateSV(StatType.ItemData_18, ItemDatas[18]);
                    break;
                case 19: 
                    SetPrivateSV(StatType.Backpack_7, Inventory[19]);
                    SetPrivateSV(StatType.ItemData_19, ItemDatas[19]);
                    break;
            }
        }

        //Headless check that a rejected bag swap re-queues both ITEMDATA_*
        //and a full ObjectStatus (NewSVs copied from SVs) without changing
        //the InvResult wire layout (id + int32).
        public static bool VerifyInventoryResync()
        {
            Container bag = new Container(Container.PurpleBag, -1, 120000);
            bag.Inventory[0] = 0x15f;
            bag.ItemDatas[0] = 7;
            bag.Inventory[1] = 0x15f;
            bag.ItemDatas[1] = 3;
            bag.UpdateInventorySlot(0);
            bag.UpdateInventorySlot(1);
            int afterSet = bag.UpdateCount;
            bag.NewSVs.Clear();
            ForceContainerStatus(bag);
            if (bag.UpdateCount <= afterSet)
            {
                Program.Print(PrintType.Error, "P10 verify: UpdateCount not forced");
                return false;
            }
            if (!bag.NewSVs.ContainsKey(StatType.Inventory_0) || !bag.NewSVs.ContainsKey(StatType.ItemData_0) ||
                !bag.NewSVs.ContainsKey(StatType.Inventory_1) || !bag.NewSVs.ContainsKey(StatType.ItemData_1))
            {
                Program.Print(PrintType.Error, "P10 verify: bag NewSVs missing slot stats");
                return false;
            }
            if ((int)bag.NewSVs[StatType.Inventory_0] != 0x15f || (int)bag.NewSVs[StatType.ItemData_0] != 7 ||
                (int)bag.NewSVs[StatType.Inventory_1] != 0x15f || (int)bag.NewSVs[StatType.ItemData_1] != 3)
            {
                Program.Print(PrintType.Error, "P10 verify: bag slot values mismatch");
                return false;
            }
            byte[] fail = GameServer.InvResult(1);
            byte[] ok = GameServer.InvResult(0);
            if (fail == null || fail.Length != 5 || fail[0] != (byte)GameServer.PacketId.InvResult)
            {
                Program.Print(PrintType.Error, "P10 verify: InvResult(1) wire layout changed");
                return false;
            }
            if (ok == null || ok.Length != 5 || ok[0] != (byte)GameServer.PacketId.InvResult)
            {
                Program.Print(PrintType.Error, "P10 verify: InvResult(0) wire layout changed");
                return false;
            }
            int failResult = (fail[1] << 24) | (fail[2] << 16) | (fail[3] << 8) | fail[4];
            int okResult = (ok[1] << 24) | (ok[2] << 16) | (ok[3] << 8) | ok[4];
            if (failResult != 1 || okResult != 0)
            {
                Program.Print(PrintType.Error, "P10 verify: InvResult payload is not a big-endian int");
                return false;
            }
            Program.Print(PrintType.Info, "P10 verify: container status requeue and InvResult layout ok");
            return true;
        }
    }
}
