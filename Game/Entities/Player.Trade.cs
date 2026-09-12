using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
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
        private const int TradeInactivityTimeoutMS = 60000;
        private const float TradeMaximumDistance = 4f;

        internal Dictionary<Player, int> PotentialTraders = new Dictionary<Player, int>();
        internal Player TradeTarget;
        internal bool[] Trade;
        internal bool TradeAccepted;
        private int _tradeActivityAt;

        private void TouchTradeActivity()
        {
            _tradeActivityAt = Manager.TotalTimeUnsynced;
            if (TradeTarget != null)
                TradeTarget._tradeActivityAt = _tradeActivityAt;
        }

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
                TouchTradeActivity();
                target._tradeActivityAt = _tradeActivityAt;
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
            TouchTradeActivity();
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

            //The client supplies these arrays, so validate them exactly like
            //ChangeTrade does: wrong length, equipped slots, empty slots and
            //soulbound items must never become an offer. (ChangeTrade
            //sanitizes the same way; AcceptTrade must not trust the client to
            //have sent one first.)
            if (myOffer == null || myOffer.Length != TradeSlots ||
                yourOffer == null || yourOffer.Length != TradeSlots)
            {
                SendError("Trade offer changed, please accept again");
                return;
            }

            bool[] clean = new bool[TradeSlots];
            for (int i = 0; i < TradeSlots; i++)
            {
                if (!myOffer[i] || i < 4 || i >= Inventory.Length || Inventory[i] == -1)
                    continue;
                ItemDesc desc = null;
                Resources.Type2Item.TryGetValue((ushort)Inventory[i], out desc);
                if (desc != null && desc.Soulbound)
                    continue;
                clean[i] = true;
            }

            Trade = clean;
            TouchTradeActivity();
            if (TradeTarget.Trade.SequenceEqual(yourOffer))
            {
                TradeAccepted = true;
                //Identity-check the peer's client: Client objects are pooled,
                //so a bare null check could deliver this to the peer's next
                //login session after a disconnect.
                if (TradeTarget.Client != null && TradeTarget.Client.Player == TradeTarget)
                    TradeTarget.Client.Send(GameServer.TradeAccepted(TradeTarget.Trade, Trade));

                if (TradeAccepted && TradeTarget.TradeAccepted)
                    DoTrade(this);
            }
            else
            {
                TradeAccepted = false;
                TradeTarget.TradeAccepted = false;
                if (Client != null && Client.Player == this)
                    Client.Send(GameServer.TradeChanged(TradeTarget.Trade));
                if (TradeTarget.Client != null && TradeTarget.Client.Player == TradeTarget)
                    TradeTarget.Client.Send(GameServer.TradeChanged(Trade));
                SendError("Trade offer changed, please accept again");
            }
        }

        private static bool TradePeerUsable(Player player, Player target)
        {
            if (target == null || target.Trade == null)
                return false;
            //Disposed (disconnected) players have Parent == null; the dead
            //cannot trade their grave goods away.
            if (player.Parent == null || target.Parent == null || player.Parent != target.Parent)
                return false;
            if (player.Dead || target.Dead)
                return false;
            if (player.Client == null || target.Client == null)
                return false;
            return true;
        }

        private static void DoTrade(Player player)
        {
            const string failedMsg = "Error while trading. Trade unsuccessful.";

            Player target = player.TradeTarget;
            if (!TradePeerUsable(player, target))
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

            //Validate soulbound again at execution time, per owning side
            //(slot indices overlap between the two sides, so each list is
            //checked against its own owner's inventory).
            foreach (int s in mySlots)
            {
                ItemDesc desc = null;
                Resources.Type2Item.TryGetValue((ushort)player.Inventory[s], out desc);
                if (desc != null && desc.Soulbound)
                {
                    FinishTrade(player, target, failedMsg);
                    return;
                }
            }
            foreach (int s in yourSlots)
            {
                ItemDesc desc = null;
                Resources.Type2Item.TryGetValue((ushort)target.Inventory[s], out desc);
                if (desc != null && desc.Soulbound)
                {
                    FinishTrade(player, target, failedMsg);
                    return;
                }
            }

            //Never destroy items: if either side cannot fit what it is about
            //to receive, abort with everything still in place. The vanilla
            //client already blocks the button in this case; a hacked client
            //used to be able to burn the counterparty's items.
            if (mySlots.Count > target.CountFreeInventorySlots() ||
                yourSlots.Count > player.CountFreeInventorySlots())
            {
                FinishTrade(player, target, failedMsg);
                return;
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

            for (int i = 0; i < myItems.Count; i++)
            {
                int slot = target.GetFreeInventorySlot();
                target.Inventory[slot] = myItems[i];
                target.ItemDatas[slot] = myDatas[i];
                target.UpdateInventorySlot(slot);
            }
            for (int i = 0; i < yourItems.Count; i++)
            {
                int slot = player.GetFreeInventorySlot();
                player.Inventory[slot] = yourItems[i];
                player.ItemDatas[slot] = yourDatas[i];
                player.UpdateInventorySlot(slot);
            }

            player.RecalculateEquipBonuses();
            target.RecalculateEquipBonuses();

            //Both sides commit in ONE transaction (see SaveTradePair): with
            //two separate saves, a crash after the first one duplicates every
            //traded item. If the commit itself throws, both in-memory states
            //are already swapped, so report success of the swap but keep the
            //trade window closed; the next autosave/disconnect will persist.
            try
            {
                player.SaveToCharacter();
                target.SaveToCharacter();
                Database.SaveTradePair(player.Client.Account, player.Client.Character,
                    target.Client.Account, target.Client.Character);
            }
            catch
            {
            }

            FinishTrade(player, target, "Trade Successful!");
        }

        private static void FinishTrade(Player player, Player target, string msg)
        {
            //Identity-check both clients: Client objects are pooled and
            //reused by later sessions, so never queue packets onto a client
            //that no longer belongs to this player.
            if (player.Client != null && player.Client.Player == player)
                player.Client.Send(GameServer.TradeDone(1, msg));
            if (target != null && target.Client != null && target.Client.Player == target)
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
            if (PotentialTraders.Count > 0)
            {
                int now = Manager.TotalTimeUnsynced;
                foreach (Player p in PotentialTraders.Keys.ToArray())
                    if (PotentialTraders[p] < now)
                    {
                        PotentialTraders.Remove(p);
                        p.SendInfo("Trade to " + Name + " has timed out!");
                    }
            }

            if (TradeTarget == null)
                return;
            if (TradeTarget.Parent != Parent || TradeTarget.Parent == null)
            {
                CancelTrade();
                return;
            }
            if (Position.Distance(TradeTarget) > TradeMaximumDistance)
            {
                CancelTrade();
                return;
            }
            if (Manager.TotalTimeUnsynced - _tradeActivityAt > TradeInactivityTimeoutMS)
                CancelTrade();
        }

        internal void CancelTradeIfTrading()
        {
            if (TradeTarget != null)
                CancelTrade();
            PotentialTraders.Clear();
        }
    }
}
