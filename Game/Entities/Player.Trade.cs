using RotMG.Common;
using RotMG.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Entities
{
    //Player-to-player trading, ported from realm-src-master
    //wServer/realm/entities/player/Player.Trade.cs. Wire IDs (51-59) match
    //realm-client, which carries the trade messages appended after
    //NAMERESULT=50.
    public partial class Player
    {
        public const int TradeSlots = 12;
        private const int TradeRequestTimeoutMS = 20000;

        internal Dictionary<Player, int> PotentialTraders = new Dictionary<Player, int>();
        internal Player TradeTarget;
        internal bool[] Trade;
        internal bool TradeAccepted;

        private TradeItem BuildTradeItem(int slot)
        {
            int item = slot < Inventory.Length ? Inventory[slot] : -1;
            ItemDesc desc = null;
            if (item != -1)
                Resources.Type2Item.TryGetValue((ushort)item, out desc);
            PlayerDesc pd = Desc as PlayerDesc;
            return new TradeItem
            {
                Item = item,
                SlotType = pd != null && slot < pd.SlotTypes.Length ? pd.SlotTypes[slot] : 0,
                Included = false,
                Tradeable = item != -1 && slot >= 4 && (desc == null || !desc.Soulbound)
            };
        }

        private TradeItem[] BuildTradeList()
        {
            TradeItem[] items = new TradeItem[TradeSlots];
            for (int i = 0; i < TradeSlots; i++)
                items[i] = BuildTradeItem(i);
            return items;
        }

        public void RequestTrade(string name)
        {
            if (TradeTarget != null)
            {
                SendError("Already trading!");
                return;
            }

            if (!NameChosen)
            {
                SendError("A unique name is required before trading with others!");
                return;
            }

            Player target = Manager.GetPlayer(name);
            if (target == null || target.Parent != Parent)
            {
                SendError(name + " not found!");
                return;
            }

            if (target == this)
            {
                SendError("You can't trade with yourself!");
                return;
            }

            if (target.Client.Account.IgnoredIds.Contains(AccountId))
                return;

            if (target.TradeTarget != null)
            {
                SendError(target.Name + " is already trading!");
                return;
            }

            if (PotentialTraders.ContainsKey(target))
            {
                TradeTarget = target;
                Trade = new bool[TradeSlots];
                TradeAccepted = false;
                target.TradeTarget = this;
                target.Trade = new bool[TradeSlots];
                target.TradeAccepted = false;
                PotentialTraders.Clear();
                target.PotentialTraders.Clear();

                TradeItem[] my = BuildTradeList();
                TradeItem[] your = target.BuildTradeList();
                Client.Send(GameServer.TradeStart(my, target.Name, your));
                target.Client.Send(GameServer.TradeStart(your, Name, my));
            }
            else
            {
                target.PotentialTraders[this] = Manager.TotalTimeUnsynced + TradeRequestTimeoutMS;
                target.Client.Send(GameServer.TradeRequested(Name));
                SendInfo("You have sent a trade request to " + target.Name + "!");
            }
        }

        public void ChangeTrade(bool[] offer)
        {
            if (TradeTarget == null || TradeTarget.Client == null)
                return;

            if (offer == null || offer.Length != TradeSlots)
                return;

            bool blockedSoulbound = false;
            for (int i = 0; i < TradeSlots; i++)
            {
                if (!offer[i])
                    continue;
                int item = i < Inventory.Length ? Inventory[i] : -1;
                if (item == -1)
                {
                    offer[i] = false;
                    continue;
                }
                ItemDesc desc = null;
                Resources.Type2Item.TryGetValue((ushort)item, out desc);
                if (i < 4 || (desc != null && desc.Soulbound))
                {
                    blockedSoulbound = true;
                    offer[i] = false;
                }
            }

            TradeAccepted = false;
            TradeTarget.TradeAccepted = false;
            Trade = offer;
            TradeTarget.Client.Send(GameServer.TradeChanged(Trade));

            if (blockedSoulbound)
                SendError("You can't trade Soulbound items.");
        }

        public void AcceptTrade(bool[] myOffer, bool[] yourOffer)
        {
            if (TradeTarget == null || TradeTarget.Trade == null)
                return;

            if (TradeAccepted)
                return;

            Trade = myOffer;
            if (TradeTarget.Trade.SequenceEqual(yourOffer))
            {
                TradeAccepted = true;
                TradeTarget.Client.Send(GameServer.TradeAccepted(TradeTarget.Trade, Trade));

                if (TradeAccepted && TradeTarget.TradeAccepted)
                    DoTrade(this);
            }
        }

        private static void DoTrade(Player player)
        {
            const string failedMsg = "Error while trading. Trade unsuccessful.";
            string msg = "Trade Successful!";

            Player target = player.TradeTarget;
            if (target == null || player.Parent == null || target.Parent == null || player.Parent != target.Parent)
            {
                FinishTrade(player, target, failedMsg);
                return;
            }

            if (!player.TradeAccepted || !target.TradeAccepted)
                return;

            List<int> mySlots = new List<int>();
            List<int> yourSlots = new List<int>();
            for (int i = 4; i < TradeSlots && i < player.Inventory.Length; i++)
                if (player.Trade[i] && player.Inventory[i] != -1)
                    mySlots.Add(i);
            for (int i = 4; i < TradeSlots && i < target.Inventory.Length; i++)
                if (target.Trade[i] && target.Inventory[i] != -1)
                    yourSlots.Add(i);

            //Validate soulbound again at execution time.
            foreach (int s in mySlots.Concat(yourSlots).ToArray())
            {
                Player owner = mySlots.Contains(s) ? player : target;
                ItemDesc desc = null;
                Resources.Type2Item.TryGetValue((ushort)owner.Inventory[s], out desc);
                if (desc != null && desc.Soulbound)
                {
                    FinishTrade(player, target, failedMsg);
                    return;
                }
            }

            List<int> myItems = mySlots.Select(s => player.Inventory[s]).ToList();
            List<int> myDatas = mySlots.Select(s => player.ItemDatas[s]).ToList();
            List<int> yourItems = yourSlots.Select(s => target.Inventory[s]).ToList();
            List<int> yourDatas = yourSlots.Select(s => target.ItemDatas[s]).ToList();

            foreach (int s in mySlots)
            {
                player.Inventory[s] = -1;
                player.ItemDatas[s] = -1;
                player.UpdateInventorySlot(s);
            }
            foreach (int s in yourSlots)
            {
                target.Inventory[s] = -1;
                target.ItemDatas[s] = -1;
                target.UpdateInventorySlot(s);
            }

            bool lost = false;
            for (int i = 0; i < myItems.Count; i++)
            {
                int slot = target.GetFreeInventorySlot();
                if (slot == -1)
                {
                    lost = true;
                    continue;
                }
                target.Inventory[slot] = myItems[i];
                target.ItemDatas[slot] = myDatas[i];
                target.UpdateInventorySlot(slot);
            }
            for (int i = 0; i < yourItems.Count; i++)
            {
                int slot = player.GetFreeInventorySlot();
                if (slot == -1)
                {
                    lost = true;
                    continue;
                }
                player.Inventory[slot] = yourItems[i];
                player.ItemDatas[slot] = yourDatas[i];
                player.UpdateInventorySlot(slot);
            }

            player.RecalculateEquipBonuses();
            target.RecalculateEquipBonuses();

            if (lost)
                msg = "An error occured while trading! Some items were lost!";
            FinishTrade(player, target, msg);
        }

        private static void FinishTrade(Player player, Player target, string msg)
        {
            player.Client.Send(GameServer.TradeDone(1, msg));
            if (target != null && target.Client != null)
                target.Client.Send(GameServer.TradeDone(1, msg));
            player.ResetTrade();
        }

        public void CancelTrade()
        {
            Client.Send(GameServer.TradeDone(1, "Trade canceled!"));
            if (TradeTarget != null && TradeTarget.Client != null)
                TradeTarget.Client.Send(GameServer.TradeDone(1, "Trade canceled!"));
            ResetTrade();
        }

        public void ResetTrade()
        {
            if (TradeTarget != null)
            {
                TradeTarget.TradeTarget = null;
                TradeTarget.Trade = null;
                TradeTarget.TradeAccepted = false;
            }

            TradeTarget = null;
            Trade = null;
            TradeAccepted = false;
        }

        internal void CheckTradeTimeout()
        {
            if (PotentialTraders.Count == 0)
                return;
            int now = Manager.TotalTimeUnsynced;
            foreach (Player p in PotentialTraders.Keys.ToArray())
                if (PotentialTraders[p] < now)
                {
                    PotentialTraders.Remove(p);
                    p.SendInfo("Trade to " + Name + " has timed out!");
                }
        }

        internal void CancelTradeIfTrading()
        {
            if (TradeTarget != null)
                CancelTrade();
            PotentialTraders.Clear();
        }
    }
}
