using RotMG.Common;
using RotMG.Game.Commands;
using RotMG.Game.Logic;
using RotMG.Game.Logic.Transitions;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        [Command("/legendary", Rank.Admin, Silent = true)]
        private void CmdLegendary(CommandCall call)
        {
            string[] j = call.Args;
            int slot = int.Parse(j[0]);
            if (Inventory[slot] != -1)
            {
                //Bounded (W05): an item that can never roll T7 (Roll() is always a no-modifier result for
                //non-modifiable slot types) used to spin this loop forever on the tick thread. Unchanged on failure.
                const int maxRolls = 10000;
                Tuple<bool, ItemData> roll = null;
                for (int attempt = 0; attempt < maxRolls && roll == null; attempt++)
                {
                    Tuple<bool, ItemData> candidate = Resources.Type2Item[(ushort)Inventory[slot]].Roll();
                    if ((candidate.Item2 & ItemData.T7) != 0)
                        roll = candidate;
                }
                if (roll == null)
                {
                    SendError("Could not roll a T7 for that item.");
                    return;
                }
                ItemDatas[slot] = !roll.Item1 ? -1 : (int)roll.Item2;
                UpdateInventorySlot(slot);
                RecalculateEquipBonuses();
            }
        }

        [Command("/roll", Rank.Admin, Silent = true)]
        private void CmdRoll(CommandCall call)
        {
            for (int k = 0; k < 20; k++)
            {
                if (Inventory[k] != -1)
                {
                    Tuple<bool, ItemData> roll = Resources.Type2Item[(ushort)Inventory[k]].Roll();
                    ItemDatas[k] = !roll.Item1 ? -1 : (int)roll.Item2;
                    UpdateInventorySlot(k);
                    RecalculateEquipBonuses();
                }
            }
        }

        [Command("/disconnect", Rank.Admin, "/dcAll", "/dc", Silent = true)]
        private void CmdDisconnect(CommandCall call)
        {
            foreach (Client c in Manager.Clients.Values.ToArray())
            {
                try { c.Disconnect(); }
                catch { }
            }
        }

        [Command("/terminate", Rank.Admin, "/stop", Silent = true)]
        private void CmdTerminate(CommandCall call)
        {
            Program.StartTerminating();
            return;
        }

        [Command("/gimme", Rank.Admin, "/give")]
        private void CmdGimme(CommandCall call)
        {
            string input = call.Input;
            if (Resources.IdLower2Item.TryGetValue(input.ToLower(), out ItemDesc item))
            {
                if (GiveItem(item.Type))
                    SendInfo("Success");
                else SendError("No inventory slots");
            }
            else SendError($"Item <{input}> not found in GameData");
        }

        [Command("/create", Rank.Admin, "/spawn")]
        private void CmdCreate(CommandCall call)
        {
            string[] j = call.Args;
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendHelp("/spawn <count> <entity>");
                return;
            }
            int spawnCount;
            if (!int.TryParse(j[0], out spawnCount))
                spawnCount = -1;
            if (Resources.IdLower2Object.TryGetValue((spawnCount == -1 ? input : string.Join(' ', j.Skip(1))).ToLower(), out ObjectDesc desc))
            {
                if (spawnCount == -1) spawnCount = 1;
                if (desc.Player || desc.Static)
                {
                    SendError("Can't spawn this entity");
                    return;
                }
                SendInfo($"Spawning <{spawnCount}> <{desc.DisplayId}> in 2 seconds");
                Position pos = Position;
                Manager.AddTimedAction(2000, () =>
                {
                    for (int i = 0; i < spawnCount; i++)
                    {
                        Entity entity = Resolve(desc.Type);
                        Parent?.AddEntity(entity, pos);
                    }
                });
            }
            else
            {
                SendError($"Entity <{input}> not found in Game Data");
            }
        }

        [Command("/max", Rank.Admin)]
        private void CmdMax(CommandCall call)
        {
            for (int i = 0; i < Stats.Length; i++)
                Stats[i] = (Desc as PlayerDesc).Stats[i].MaxValue;
            UpdateStats();
            SendInfo("Maxed");
        }

        [Command("/god", Rank.Admin)]
        private void CmdGod(CommandCall call)
        {
            ApplyConditionEffect(ConditionEffectIndex.Invincible, HasConditionEffect(ConditionEffectIndex.Invincible) ? 0 : -1);
            SendInfo($"Godmode set to {HasConditionEffect(ConditionEffectIndex.Invincible)}");
        }

        [Command("/closerealm", Rank.Admin)]
        private void CmdCloserealm(CommandCall call)
        {
            RealmWorld realm = Parent as RealmWorld;
            if (realm == null)
            {
                SendError("Not in a realm.");
                return;
            }
            if (realm.Closing || realm.Closed)
            {
                SendError("Realm already closing.");
                return;
            }
            realm.Overseer.InitCloseRealm();
        }

        [Command("/clearspawn", Rank.Admin, "/cs")]
        private void CmdClearspawn(CommandCall call)
        {
            int removed = 0;
            for (int pass = 0; pass < 5; pass++)
            {
                int passRemoved = 0;
                foreach (Entity spawned in Parent.Entities.Values.ToArray())
                {
                    if (!spawned.Spawned || spawned is Player)
                        continue;
                    if (spawned is Enemy spawnedEnemy)
                        spawnedEnemy.Death(this);
                    else
                        Parent.RemoveEntity(spawned);
                    passRemoved++;
                }
                removed += passRemoved;
                if (passRemoved == 0)
                    break;
            }
            SendInfo($"{removed} spawned entities removed!");
        }

        [Command("/cleargraves", Rank.Admin, "/cgraves")]
        private void CmdCleargraves(CommandCall call)
        {
            int removed = 0;
            foreach (StaticObject st in Parent.Statics.Values.ToArray())
            {
                if (st.Desc == null || st.Desc.DisplayId == null)
                    continue;
                if (st.Desc.DisplayId.StartsWith("Gravestone") && st.Position.Distance(Position) < 15)
                {
                    Parent.RemoveEntity(st);
                    removed++;
                }
            }
            SendInfo($"{removed} gravestones removed!");
        }

        [Command("/eff", Rank.Admin)]
        private void CmdEff(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input) ||
                !Enum.TryParse(input.Trim(), true, out ConditionEffectIndex effFx))
            {
                SendError("Invalid effect!");
                return;
            }
            if (HasConditionEffect(effFx))
                ApplyConditionEffect(effFx, 0);
            else
                ApplyConditionEffect(effFx, -1);
            SendInfo($"Effect {effFx} toggled to {HasConditionEffect(effFx)}");
        }

        [Command("/grank", Rank.Admin)]
        private void CmdGrank(CommandCall call)
        {
            string input = call.Input;
            int space = input.IndexOf(' ');
            if (string.IsNullOrWhiteSpace(input) || space == -1)
            {
                SendHelp("/grank <player name> <guild rank>");
                return;
            }
            string rankName = input.Substring(0, space);
            string rankValue = input.Substring(space + 1).Trim().ToLower();
            int rank;
            switch (rankValue)
            {
                case "initiate": rank = 0; break;
                case "member": rank = 10; break;
                case "officer": rank = 20; break;
                case "leader": rank = 30; break;
                case "founder": rank = 40; break;
                default:
                    if (!int.TryParse(rankValue, out rank) || rank % 10 != 0 || rank < 0 || rank > 40)
                    {
                        SendError("Unknown rank! Use initiate/member/officer/leader/founder or 0-40.");
                        return;
                    }
                    break;
            }
            int rankId = Database.IdFromUsername(rankName);
            if (rankId == -1)
            {
                SendError("Account not found!");
                return;
            }
            AccountModel rankAcc = Manager.GetAuthoritativeAccount(rankId);
            if (rankAcc == null)
            {
                SendError("Account not found!");
                return;
            }
            //Reply and the live rank update wait for the commit (W03); a failed commit reverts the account rank.
            if (Database.ChangeGuildRank(rankAcc, rank, committed =>
            {
                if (!committed)
                {
                    SendError("Could not change rank (not in a guild?).");
                    return;
                }
                SendInfo($"You changed the guildrank of player {rankAcc.Name} to {rank}.");
                Client liveRankClient = Manager.GetClient(rankId);
                if (liveRankClient?.Player != null)
                {
                    liveRankClient.Player.GuildRank = rank;
                    liveRankClient.Player.SendInfo("Your guild rank was changed");
                }
            }) != Database.GuildResult.OK)
            {
                SendError("Could not change rank (not in a guild?).");
                return;
            }
        }

        [Command("/tppos", Rank.Admin, "/goto")]
        private void CmdTppos(CommandCall call)
        {
            string input = call.Input;
            string[] coords = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (coords.Length != 2 || !int.TryParse(coords[0], out int gotoX) || !int.TryParse(coords[1], out int gotoY))
            {
                SendError("Invalid coordinates! Usage: /tppos <x> <y>");
                return;
            }
            if (!Teleport(new Position(gotoX + 0.5f, gotoY + 0.5f)))
                SendError("Cannot teleport there.");
        }

        [Command("/setpiece", Rank.Admin)]
        private void CmdSetpiece(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendInfo("Valid SetPieces: " + string.Join(", ", Setpieces.SetPieces.Events.Select(e => e.Item1)) + ".");
                return;
            }
            if (Parent is NexusWorld)
            {
                SendInfo("/setpiece not allowed in Nexus.");
                return;
            }
            var piece = Setpieces.SetPieces.Events
                .FirstOrDefault(e => e.Item1.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase));
            if (piece == null)
            {
                SendError("Invalid SetPiece.");
                return;
            }
            piece.Item2.RenderSetPiece(Parent, new IntPoint((int)Position.X + 1, (int)Position.Y + 1));
            SendInfo($"Rendered {piece.Item1}.");
        }

        [Command("/wall", Rank.Admin)]
        private void CmdWall(CommandCall call)
        {
            string input = call.Input;
            if (Parent == null)
                return;
            string wallName = string.IsNullOrWhiteSpace(input) ? "grey wall" : input.Trim().ToLower();
            if (!Resources.IdLower2Object.TryGetValue(wallName, out ObjectDesc wallDesc) || !wallDesc.Static)
            {
                SendError($"Static object <{wallName}> not found. Usage: /wall [object id]");
                return;
            }
            int wx = (int)Position.X;
            int wy = (int)Position.Y;
            Parent.UpdateStatic(wx, wy, wallDesc.Type);
            SendInfo($"Placed {wallDesc.DisplayId} at {wx},{wy}. Standing players are nudged.");
        }

        [Command("/killall", Rank.Admin, "/ka")]
        private void CmdKillall(CommandCall call)
        {
            string input = call.Input;
            int killed = 0;
            for (int pass = 0; pass < 5; pass++)
            {
                int passKilled = 0;
                foreach (Entity killable in Parent.Entities.Values.ToArray())
                {
                    if (!(killable is Enemy killEnemy) || killEnemy.Desc == null)
                        continue;
                    if (!string.IsNullOrWhiteSpace(input) &&
                        (killEnemy.Desc.DisplayId == null ||
                         killEnemy.Desc.DisplayId.IndexOf(input.Trim(), StringComparison.InvariantCultureIgnoreCase) < 0))
                        continue;
                    killEnemy.Spawned = true;
                    killEnemy.Death(this);
                    passKilled++;
                }
                killed += passKilled;
                if (passKilled == 0)
                    break;
            }
            SendInfo($"{killed} enemy killed!");
        }

        [Command("/kick", Rank.Admin)]
        private void CmdKick(CommandCall call)
        {
            string input = call.Input;
            Client kickTarget = null;
            foreach (Client c in Manager.Clients.Values.ToArray())
            {
                if (c.Account != null && c.Account.Name != null &&
                    c.Account.Name.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    kickTarget = c;
                    break;
                }
            }
            if (kickTarget == null)
            {
                SendError($"Player '{input.Trim()}' could not be found!");
                return;
            }
            try { kickTarget.Disconnect(); } catch { }
            SendInfo("Player disconnected!");
        }

        [Command("/getquest", Rank.Admin)]
        private void CmdGetquest(CommandCall call)
        {
            if (Quest == null)
            {
                SendError("Player does not have a quest!");
                return;
            }
            SendInfo("Quest location: (" + (int)Quest.Position.X + ", " + (int)Quest.Position.Y + ")");
        }

        [Command("/oryxsay", Rank.Admin, "/osay")]
        private void CmdOryxsay(CommandCall call)
        {
            string input = call.Input;
            foreach (Player witness in Parent.Players.Values.ToArray())
                witness.SendEnemy("Oryx the Mad God", input);
        }

        [Command("/announce", Rank.Admin)]
        private void CmdAnnounce(CommandCall call)
        {
            string input = call.Input;
            foreach (Client c in Manager.Clients.Values.ToArray())
                if (c.Player != null)
                    c.Player.SendInfo("[Announcement] " + input);
        }

        [Command("/summon", Rank.Admin)]
        private void CmdSummon(CommandCall call)
        {
            string input = call.Input;
            Player summoned = null;
            foreach (Player candidate in Parent.Players.Values.ToArray())
            {
                if (candidate.Name.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    summoned = candidate;
                    break;
                }
            }
            if (summoned == null)
            {
                SendError($"Player '{input.Trim()}' could not be found!");
                return;
            }
            summoned.Teleport(Position);
            summoned.SendInfo($"You've been summoned by {Name}.");
            SendInfo("Player summoned!");
        }

        [Command("/summonall", Rank.Admin)]
        private void CmdSummonall(CommandCall call)
        {
            foreach (Player guest in Parent.Players.Values.ToArray())
            {
                if (guest == this)
                    continue;
                guest.Teleport(Position);
                guest.SendInfo($"You've been summoned by {Name}.");
            }
            SendInfo("All players summoned!");
        }

        [Command("/killplayer", Rank.Admin)]
        private void CmdKillplayer(CommandCall call)
        {
            string input = call.Input;
            Player victim = Manager.GetPlayer(input.Trim());
            if (victim == null)
            {
                SendError($"Player '{input.Trim()}' could not be found!");
                return;
            }
            victim.HP = 0;
            victim.Death(Name);
            SendInfo("Player killed!");
        }

        [Command("/size", Rank.Admin)]
        private void CmdSize(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input.Trim(), out int newSize) ||
                (newSize != 0 && (newSize < 0 || newSize > 500)))
            {
                SendError("Usage: /size <0-500>. Using 0 restores the default size.");
                return;
            }
            Size = newSize == 0 ? 100 : newSize;
            Client.Character.Size = Size;
            SendInfo(newSize == 0 ? "Size restored." : $"Size set to {Size}.");
        }

        [Command("/reboot", Rank.Admin)]
        private void CmdReboot(CommandCall call)
        {
            //Single-process build: no inter-server reboot bus, so a
            //reboot is a clean shutdown for the external monitor.
            SendInfo("Rebooting server...");
            Program.StartTerminating();
            return;
        }

        [Command("/reskin", Rank.Admin)]
        private void CmdReskin(CommandCall call)
        {
            string input = call.Input;
            List<ushort> choices = Resources.Type2Skin.Values
                .Where(s => s.PlayerClassType == Type)
                .Select(s => s.Type)
                .ToList();
            if (string.IsNullOrWhiteSpace(input) ||
                !ushort.TryParse(input.Trim(), out ushort skinType) ||
                (skinType != 0 && !choices.Contains(skinType)))
            {
                SendError("Usage: /reskin <skin type>. Choices: 0, " + string.Join(", ", choices));
                return;
            }
            SkinType = skinType;
            UpdateStats();
            SendInfo(skinType == 0 ? "Skin cleared." : $"Skin set to {skinType}.");
        }

        [Command("/tq", Rank.Admin)]
        private void CmdTq(CommandCall call)
        {
            if (Quest == null)
            {
                SendError("Player does not have a quest!");
                return;
            }
            if (!Teleport(Quest.Position, ignoreSeen: true))
                SendError("Cannot teleport to quest.");
            else
                SendInfo("Teleported to Quest Location: (" + (int)Quest.Position.X + ", " + (int)Quest.Position.Y + ")");
        }

        [Command("/mute", Rank.Admin)]
        private void CmdMute(CommandCall call)
        {
            string input = call.Input;
            string[] muteArgs = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (muteArgs.Length < 1 || muteArgs.Length > 2 ||
                !Regex.IsMatch(muteArgs[0], @"^\w+$"))
            {
                SendError("Usage: /mute <player name> [minutes]");
                return;
            }
            int muteMinutes = -1;
            if (muteArgs.Length == 2 && (!int.TryParse(muteArgs[1], out muteMinutes) || muteMinutes <= 0))
            {
                SendError("Usage: /mute <player name> [minutes]");
                return;
            }
            int muteId = Database.IdFromUsername(muteArgs[0]);
            if (muteId == -1)
            {
                SendError("Account not found!");
                return;
            }
            AccountModel muteAcc = Manager.GetAuthoritativeAccount(muteId);
            if (muteAcc == null)
            {
                SendError("Account not found!");
                return;
            }
            if (muteAcc.Ranked)
            {
                SendError("Cannot mute other admins.");
                return;
            }
            muteAcc.Muted = true;
            muteAcc.Save();
            if (muteMinutes > 0)
            {
                int unmuteId = muteId;
                string unmuteName = muteArgs[0];
                Manager.AddTimedAction(muteMinutes * 60000, () =>
                {
                    AccountModel timed = Manager.GetAuthoritativeAccount(unmuteId);
                    if (timed == null)
                        return;
                    timed.Muted = false;
                    timed.Save();
                });
                SendInfo(unmuteName + " successfully muted for " + muteMinutes + " minutes.");
            }
            else
                SendInfo(muteArgs[0] + " successfully muted indefinitely.");
        }

        [Command("/unmute", Rank.Admin)]
        private void CmdUnmute(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input.Trim(), @"^\w+$"))
            {
                SendError("Usage: /unmute <player name>");
                return;
            }
            int unmuteId = Database.IdFromUsername(input.Trim());
            if (unmuteId == -1)
            {
                SendError("Account not found!");
                return;
            }
            AccountModel unmuteAcc = Manager.GetAuthoritativeAccount(unmuteId);
            if (unmuteAcc == null)
            {
                SendError("Account not found!");
                return;
            }
            if (!unmuteAcc.Muted)
                SendInfo(input.Trim() + " wasn't muted...");
            else
            {
                unmuteAcc.Muted = false;
                unmuteAcc.Save();
                SendInfo(input.Trim() + " successfully unmuted.");
            }
        }

        [Command("/ban", Rank.Admin)]
        private void CmdBan(CommandCall call)
        {
            string input = call.Input;
            int banSpace = input.IndexOf(' ');
            if (banSpace == -1)
            {
                SendError("Usage: /ban <player name> <reason>");
                return;
            }
            string banName = input.Substring(0, banSpace).Trim();
            string banReason = input.Substring(banSpace + 1).Trim();
            if (string.IsNullOrWhiteSpace(banReason))
            {
                SendError("A reason must be provided.");
                return;
            }
            int banId = Database.IdFromUsername(banName);
            if (banId == -1)
            {
                SendError("Account not found...");
                return;
            }
            AccountModel banAcc = Manager.GetAuthoritativeAccount(banId);
            if (banAcc == null)
            {
                SendError("Account not found...");
                return;
            }
            banAcc.Banned = true;
            banAcc.Save();
            Client liveBanned = Manager.GetClient(banId);
            try { liveBanned?.Disconnect(); } catch { }
            SendInfo($"{banAcc.Name} successfully banned. Reason: {banReason}");
        }

        [Command("/banip", Rank.Admin, "/ipban")]
        private void CmdBanip(CommandCall call)
        {
            string input = call.Input;
            int banIpSpace = input.IndexOf(' ');
            if (banIpSpace == -1)
            {
                SendError("Usage: /banip <player name> <reason>");
                return;
            }
            string banIpName = input.Substring(0, banIpSpace).Trim();
            string banIpReason = input.Substring(banIpSpace + 1).Trim();
            if (string.IsNullOrWhiteSpace(banIpReason))
            {
                SendError("A reason must be provided.");
                return;
            }
            int banIpId = Database.IdFromUsername(banIpName);
            if (banIpId == -1)
            {
                SendError("Account not found...");
                return;
            }
            AccountModel banIpAcc = Manager.GetAuthoritativeAccount(banIpId);
            if (banIpAcc == null)
            {
                SendError("Account not found...");
                return;
            }
            banIpAcc.Banned = true;
            banIpAcc.Save();
            //No persistent IP-ban list in this build: ban the
            //account and drop every session sharing its IP.
            Client seed = Manager.GetClient(banIpId);
            string banIp = seed?.IP;
            if (banIp != null)
            {
                foreach (Client c in Manager.Clients.Values.ToArray())
                {
                    if (banIp.Equals(c.IP))
                    {
                        if (c.Account != null)
                        {
                            c.Account.Banned = true;
                            try { c.Account.Save(); } catch { }
                        }
                        try { c.Disconnect(); } catch { }
                    }
                }
            }
            else if (seed != null)
            {
                try { seed.Disconnect(); } catch { }
            }
            SendInfo($"Banned {banIpAcc.Name} (account plus active sessions on its IP). Reason: {banIpReason}");
        }

        [Command("/unban", Rank.Admin)]
        private void CmdUnban(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input.Trim(), @"^\w+$"))
            {
                SendError("Usage: /unban <player name>");
                return;
            }
            int unbanId = Database.IdFromUsername(input.Trim());
            if (unbanId == -1)
            {
                SendError("Account doesn't exist...");
                return;
            }
            AccountModel unbanAcc = Manager.GetAuthoritativeAccount(unbanId);
            if (unbanAcc == null)
            {
                SendError("Account doesn't exist...");
                return;
            }
            if (!unbanAcc.Banned)
                SendInfo($"{unbanAcc.Name} wasn't banned...");
            else
            {
                unbanAcc.Banned = false;
                unbanAcc.Save();
                SendInfo($"Success! {unbanAcc.Name}'s account no longer banned.");
            }
        }

        [Command("/clearinv", Rank.Admin)]
        private void CmdClearinv(CommandCall call)
        {
            for (int i = 4; i < 12 && i < Inventory.Length; i++)
            {
                Inventory[i] = -1;
                UpdateInventorySlot(i);
            }
            RecalculateEquipBonuses();
            SendInfo("Inventory Cleared.");
        }

        [Command("/quake", Rank.Admin)]
        private void CmdQuake(CommandCall call)
        {
            string input = call.Input;
            //Any world can be quaked except the Nexus, mirroring
            //realm-src-master RankedCommands.QuakeCommand.
            if (Parent == null || Parent is NexusWorld)
            {
                SendError("Cannot use /quake in Nexus.");
                return;
            }
            if (string.IsNullOrWhiteSpace(input))
            {
                //No argument: a realm quakes to the castle siege.
                //Otherwise list the valid targets, like the reference.
                if (!(Parent is RealmWorld quakeRealm))
                {
                    SendInfo("Valid world names: " + string.Join(", ", ValidQuakeWorlds()) + ".");
                    return;
                }
                Manager.QuakeRealmToCastle(quakeRealm);
                SendInfo("Quaking realm to castle...");
                return;
            }
            string quakeKey = null;
            foreach (string key in Resources.Worlds.Keys)
            {
                if (key.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    quakeKey = key;
                    break;
                }
            }
            if (quakeKey == null || !IsQuakeTarget(quakeKey))
            {
                SendError("Invalid world.");
                return;
            }
            WorldDesc quakeDesc = Resources.Worlds[quakeKey];
            World quakeWorld;
            if (quakeDesc.Maps.Length > 0)
            {
                quakeWorld = Manager.CreateWorld(quakeDesc);
                Manager.AddWorld(quakeWorld);
            }
            else
            {
                //Generated dungeon (see Game/DungeonGen).
                quakeWorld = Manager.CreateDungeonWorld(quakeDesc);
            }
            Parent.QuakeToWorld(quakeWorld);
            SendInfo($"Quaking world to {quakeKey}...");
        }

        [Command("/visit", Rank.Admin)]
        private void CmdVisit(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendHelp("/visit <player name>");
                return;
            }
            Player visitTarget = Manager.GetPlayer(input.Trim());
            if (visitTarget?.Parent == null)
            {
                SendError("Player not found!");
                return;
            }
            TransferToWorld(visitTarget.Parent.Id);
        }

        [Command("/link", Rank.Admin, "/unlink")]
        private void CmdLink(CommandCall call)
        {
            string command = call.Command;
            //Single-process build has no portal-link monitor: realm
            //portals are placed directly, so there is nothing to link.
            SendError(command.Equals("/link") ? "Link not supported in this build." : "Link not found.");
        }

        [Command("/level20", Rank.Admin, "/l20")]
        private void CmdLevel20(CommandCall call)
        {
            if (Level >= MaxLevel)
                return;
            EXP = GetLevelEXP(MaxLevel);
            Level = MaxLevel;
            UpdateStats();
            SendInfo("Leveled to 20.");
        }

        [Command("/rename", Rank.Admin)]
        private void CmdRename(CommandCall call)
        {
            string input = call.Input;
            int renameSpace = input.IndexOf(' ');
            if (renameSpace == -1)
            {
                SendHelp("/rename <player name> <new player name>");
                return;
            }
            string oldName = input.Substring(0, renameSpace).Trim();
            string newName = input.Substring(renameSpace + 1).Trim();
            int renameId = Database.IdFromUsername(oldName);
            if (renameId == -1)
            {
                SendError("Player account not found!");
                return;
            }
            if (!Database.IsValidUsername(newName) || Database.IdFromUsername(newName) != -1)
            {
                SendError("New name is invalid or taken.");
                return;
            }
            AccountModel renameAcc = Manager.GetAuthoritativeAccount(renameId);
            if (renameAcc == null)
            {
                SendError("Player account not found!");
                return;
            }
            Database.RenameAccountKeys(renameId, oldName, newName, renameAcc);
            renameAcc.Name = newName;
            Client liveRenamed = Manager.GetClient(renameId);
            if (liveRenamed?.Player != null)
            {
                liveRenamed.Player.Name = newName;
                liveRenamed.Player.Credits = renameAcc.Stats.Credits;
            }
            SendInfo("Rename successful.");
        }

        [Command("/unname", Rank.Admin)]
        private void CmdUnname(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendHelp("/unname <player name>");
                return;
            }
            string unnameOld = input.Trim();
            int unnameId = Database.IdFromUsername(unnameOld);
            if (unnameId == -1)
            {
                SendError("Player account not found!");
                return;
            }
            string guestName = "Guest" + unnameId;
            if (Database.IdFromUsername(guestName) != -1)
            {
                SendError("Could not free the name (guest slot taken).");
                return;
            }
            AccountModel unnameAcc = Manager.GetAuthoritativeAccount(unnameId);
            if (unnameAcc == null)
            {
                SendError("Player account not found!");
                return;
            }
            Database.RenameAccountKeys(unnameId, unnameOld, guestName, unnameAcc);
            unnameAcc.Name = guestName;
            Client liveUnnamed = Manager.GetClient(unnameId);
            if (liveUnnamed?.Player != null)
            {
                liveUnnamed.Player.Name = guestName;
                liveUnnamed.Player.NameChosen = false;
            }
            SendInfo("Account successfully unnamed.");
        }

        [Command("/compactloh", Rank.Admin)]
        private void CmdCompactloh(CommandCall call)
        {
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            System.GC.Collect();
            SendInfo("LOH compacted.");
        }

        [Command("/bot", Rank.Admin)]
        private void CmdBot(CommandCall call)
        {
            string input = call.Input;
            string[] args = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0)
            {
                SendHelp("/bot spawn|clear|clearall|follow|stay|wander|come|send|shoot|list");
                return;
            }
            switch (args[0].ToLower())
            {
                case "spawn":
                    {
                        if (Parent == null)
                        {
                            SendError("You are not in a world.");
                            return;
                        }
                        if (args.Length < 2 || !int.TryParse(args[1], out int count))
                        {
                            SendHelp("/bot spawn <count> [class]");
                            return;
                        }
                        ushort? classType = null;
                        if (args.Length >= 3)
                        {
                            if (!Resources.IdLower2Object.TryGetValue(args[2].ToLower(), out ObjectDesc classDesc) ||
                                !classDesc.Player || !Resources.Type2Player.ContainsKey(classDesc.Type))
                            {
                                SendError($"Unknown player class <{args[2]}>");
                                return;
                            }
                            classType = classDesc.Type;
                        }
                        SendInfo(BotManager.Spawn(this, count, classType));
                        break;
                    }
                case "clear":
                    {
                        int count = int.MaxValue;
                        if (args.Length >= 2 && !int.TryParse(args[1], out count))
                        {
                            SendHelp("/bot clear [count]");
                            return;
                        }
                        SendInfo(BotManager.Remove(this, count));
                        break;
                    }
                case "clearall":
                    SendInfo(BotManager.ClearAll());
                    break;
                case "follow":
                    SendInfo(BotManager.SetMode(this, BotMode.Follow));
                    break;
                case "stay":
                    SendInfo(BotManager.SetMode(this, BotMode.Stay));
                    break;
                case "wander":
                    SendInfo(BotManager.SetMode(this, BotMode.Wander));
                    break;
                case "come":
                    SendInfo(BotManager.Recall(this));
                    break;
                case "send":
                    {
                        if (args.Length < 2)
                        {
                            SendHelp("/bot send <player|world|id>");
                            return;
                        }
                        SendInfo(BotManager.Send(this, BotManager.ResolveWorld(string.Join(' ', args.Skip(1)))));
                        break;
                    }
                case "shoot":
                    {
                        if (args.Length < 2 || (args[1].ToLower() != "on" && args[1].ToLower() != "off"))
                        {
                            SendHelp("/bot shoot on|off");
                            return;
                        }
                        SendInfo(BotManager.SetShoot(this, args[1].ToLower() == "on"));
                        break;
                    }
                case "list":
                    SendInfo(BotManager.List(this));
                    break;
                default:
                    SendHelp("/bot spawn|clear|clearall|follow|stay|wander|come|send|shoot|list");
                    break;
            }
        }

#if DEBUG
        [Command("/badpacket", Rank.Admin, "/p5")]
        private void CmdBadpacket(CommandCall call)
        {
            //P5 probe: unmapped id 61 with an arbitrary body, then a
            //normal Text. The client must log the protocol error and
            //close rather than parse the body/Text as a new header.
            SendInfo("P5 probe: sending unknown packet id 61, then Text.");
            PacketWriter wtr = PacketWriter.Rent();
            wtr.Write((byte)61);
            wtr.Write(0x45524F52);
            wtr.Write("unknown-id-body");
            Client.Send(PacketWriter.RentedBytes());
            Client.Send(GameServer.Text("", 0, -1, 0, "", "P5 FAIL: parsed past unknown id 61"));
        }

        [Command("/p13", Rank.Admin)]
        private void CmdP13(CommandCall call)
        {
            //P13 probe: EnemyShoot from an owner the client was
            //never sent, plus MAX_MP on a non-Player. The client
            //must ack/keep moving (trace, not stall). Enqueue an
            //empty await so the resulting ShootAck is not a desync.
            const int unknownOwnerId = 0x7f0ead01;
            AwaitProjectiles(new List<Projectile>());
            Client.Send(GameServer.EnemyShoot(0, unknownOwnerId, 0, Position, 0f, 0, 1, 0f));
            SendInfo("P13 probe 1: EnemyShoot for unknown owner " + unknownOwnerId);

            Entity target = null;
            if (Parent != null)
            {
                foreach (Entity e in Parent.Entities.Values)
                {
                    if (!(e is Player))
                    {
                        target = e;
                        break;
                    }
                }
            }
            if (target != null)
            {
                target.TrySetSV(StatType.MaxMP, 100);
                SendInfo("P13 probe 2: MaxMP on object " + target.Id + " type=" + target.Type);
            }
            else
                SendInfo("P13 probe 2 skipped: no non-player entity in world");
        }
#endif
    }
}
