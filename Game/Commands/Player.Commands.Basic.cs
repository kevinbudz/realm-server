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
        [Command("/allyshots", Rank.Player)]
        private void CmdAllyshots(CommandCall call)
        {
            Client.Account.AllyShots = !Client.Account.AllyShots;
            SendInfo($"Ally shots set to {Client.Account.AllyShots}");
        }

        [Command("/allydamage", Rank.Player)]
        private void CmdAllydamage(CommandCall call)
        {
            Client.Account.AllyDamage = !Client.Account.AllyDamage;
            SendInfo($"Ally damage set to {Client.Account.AllyDamage}");
        }

        [Command("/effects", Rank.Player)]
        private void CmdEffects(CommandCall call)
        {
            Client.Account.Effects = !Client.Account.Effects;
            SendInfo($"Effects set to {Client.Account.Effects}");
        }

        [Command("/sounds", Rank.Player)]
        private void CmdSounds(CommandCall call)
        {
            Client.Account.Sounds = !Client.Account.Sounds;
            SendInfo($"Sounds set to {Client.Account.Sounds}");
        }

        [Command("/notifications", Rank.Player)]
        private void CmdNotifications(CommandCall call)
        {
            Client.Account.Notifications = !Client.Account.Notifications;
            SendInfo($"Notifications set to {Client.Account.Notifications}");
        }

        [Command("/online", Rank.Player, "/who")]
        private void CmdOnline(CommandCall call)
        {
            SendInfo($"" +
                $"<{Manager.Clients.Values.Count(k => k.Player != null)} Player(s)> " +
                $"<{string.Join(", ", Manager.Clients.Values.Where(k => k.Player != null).Select(k => k.Player.Name))}>");
        }

        [Command("/server", Rank.Player, "/pos", "/loc")]
        private void CmdServer(CommandCall call)
        {
            SendInfo(this.ToString());
        }

        [Command("/where", Rank.Player, "/find")]
        private void CmdWhere(CommandCall call)
        {
            string input = call.Input;
            Player findTarget = Manager.GetPlayer(input);
            if (findTarget == null) SendError("Couldn't find player");
            else SendInfo(findTarget.ToString());
        }

        [Command("/g", Rank.Player, "/guild")]
        private void CmdG(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(GuildName))
                SendError("You are not in a guild.");
            else if (string.IsNullOrWhiteSpace(input))
                SendHelp("/g <message>");
            else
                SendGuild($"<{Name}> {input}");
        }

        [Command("/trade", Rank.Player)]
        private void CmdTrade(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
                SendHelp("/trade <player name>");
            else if (Parent == null)
                SendError("You are not in a world.");
            else
                RequestTrade(input);
        }

        [Command("/fame", Rank.Player, "/famestats", "/stats")]
        private void CmdFame(CommandCall call)
        {
            SaveToCharacter();
            FameStats fameStats = Database.CalculateStats(Client.Account, Client.Character, "");
            SendInfo($"Active: {FameStats.MinutesActive} minutes");
            SendInfo($"Shots: {FameStats.Shots}");
            SendInfo($"Accuracy: {(int)(((float)FameStats.ShotsThatDamage / FameStats.Shots) * 100f)}% ({FameStats.ShotsThatDamage}/{FameStats.Shots})");
            SendInfo($"Abilities Used: {FameStats.AbilitiesUsed}");
            SendInfo($"Tiles Seen: {FameStats.TilesUncovered}");
            SendInfo($"Monster Kills: {FameStats.MonsterKills} ({FameStats.MonsterAssists} Assists, {(int)(((float)FameStats.MonsterKills / (FameStats.MonsterKills + FameStats.MonsterAssists)) * 100f)}% Final Blows)");
            SendInfo($"God Kills: {FameStats.GodKills} ({(int)(((float)FameStats.GodKills / FameStats.MonsterKills) * 100f)}%) ({FameStats.GodKills}/{FameStats.MonsterKills})");
            SendInfo($"Oryx Kills: {FameStats.OryxKills} ({(int)(((float)FameStats.OryxKills / FameStats.MonsterKills) * 100f)}%) ({FameStats.OryxKills}/{FameStats.MonsterKills})");
            SendInfo($"Cube Kills: {FameStats.CubeKills} ({(int)(((float)FameStats.CubeKills / FameStats.MonsterKills) * 100f)}%) ({FameStats.CubeKills}/{FameStats.MonsterKills})");
            SendInfo($"Cyan Bags: {FameStats.CyanBags}");
            SendInfo($"Blue Bags: {FameStats.BlueBags}");
            SendInfo($"White Bags: {FameStats.WhiteBags}");
            SendInfo($"Damage Taken: {FameStats.DamageTaken}");
            SendInfo($"Damage Dealt: {FameStats.DamageDealt}");
            SendInfo($"Teleports: {FameStats.Teleports}");
            SendInfo($"Potions Drank: {FameStats.PotionsDrank}");
            SendInfo($"Quests Completed: {FameStats.QuestsCompleted}");
            SendInfo($"Pirate Caves Completed: {FameStats.PirateCavesCompleted}");
            SendInfo($"Spider Dens Completed: {FameStats.SpiderDensCompleted}");
            SendInfo($"Snake Pits Completed: {FameStats.SnakePitsCompleted}");
            SendInfo($"Sprite Worlds Completed: {FameStats.SpriteWorldsCompleted}");
            SendInfo($"Undead Lairs Completed: {FameStats.UndeadLairsCompleted}");
            SendInfo($"Abyss Of Demons Completed: {FameStats.AbyssOfDemonsCompleted}");
            SendInfo($"Tombs Completed: {FameStats.TombsCompleted}");
            SendInfo($"Escapes: {FameStats.Escapes}");
            SendInfo($"Near Death Escapes: {FameStats.NearDeathEscapes}");
            SendInfo($"Party Member Level Ups: {FameStats.LevelUpAssists}");
            foreach (FameBonus bonus in fameStats.Bonuses)
                SendHelp($"{bonus.Name}: +{bonus.Fame}");
            SendInfo($"Base Fame: {fameStats.BaseFame}");
            SendInfo($"Total Fame: {fameStats.TotalFame}");
        }

        [Command("/gland", Rank.Player, "/glands")]
        private void CmdGland(CommandCall call)
        {
            string command = call.Command;
            if (!(Parent is RealmWorld))
            {
                SendError("This command requires you to be in realm first.");
                return;
            }
            //Fixed glands point is never seen on entry (see
            //Teleport): bypass the seen check like /tq and
            //report a blocked tile instead of failing silent.
            if (!Teleport(new Position(1512 + 0.5f, 1048 + 0.5f), ignoreSeen: true))
                SendError("Cannot teleport to glands.");
        }

        [Command("/join", Rank.Player)]
        private void CmdJoin(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendHelp("/join <guild name>");
                return;
            }
            if (!string.IsNullOrWhiteSpace(GuildName))
            {
                SendError("You are already in a guild.");
                return;
            }
            if (string.IsNullOrWhiteSpace(GuildInvite))
            {
                SendError("No pending guild invite (ask an officer for /invite).");
                return;
            }
            if (!GuildInvite.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                SendError($"Your invite is for {GuildInvite}.");
                return;
            }
            //Membership and the reply wait for the commit (W03): a failed commit reverts the account and reports the same error.
            if (Database.AddGuildMember(GuildInvite, Client.Account, 0, committed =>
            {
                if (!committed)
                {
                    SendError("Could not join guild.");
                    return;
                }
                GuildName = Client.Account.GuildName;
                GuildRank = Client.Account.GuildRank;
                GuildInvite = null;
                SendInfo("Joined guild " + GuildName + ".");
            }) != Database.GuildResult.OK)
            {
                SendError("Could not join guild.");
                return;
            }
        }

        [Command("/tutorial", Rank.Player)]
        private void CmdTutorial(CommandCall call)
        {
            TransferToWorld(Manager.TutorialId);
        }

        [Command("/world", Rank.Player)]
        private void CmdWorld(CommandCall call)
        {
            SendInfo($"[{Parent.Id}] {Parent.DisplayName ?? Parent.Name} ({Parent.Players.Count} players)");
        }

        [Command("/pause", Rank.Player)]
        private void CmdPause(CommandCall call)
        {
            //No Paused effect in this build: Stasis freezes
            //actions and Invincible covers damage instead.
            if (HasConditionEffect(ConditionEffectIndex.Stasis))
            {
                ApplyConditionEffect(ConditionEffectIndex.Stasis, 0);
                ApplyConditionEffect(ConditionEffectIndex.Invincible, 0);
                SendInfo("Game resumed.");
                return;
            }
            bool unsafePause = false;
            foreach (Entity near in Parent.Entities.Values.ToArray())
            {
                if (near is Enemy foe && foe.Position.Distance(Position) < 8)
                {
                    unsafePause = true;
                    break;
                }
            }
            if (unsafePause)
            {
                SendError("Not safe to pause.");
                return;
            }
            ApplyConditionEffect(ConditionEffectIndex.Stasis, -1);
            ApplyConditionEffect(ConditionEffectIndex.Invincible, -1);
            SendInfo("Game paused.");
        }

        [Command("/tp", Rank.Player, "/teleport")]
        private void CmdTp(CommandCall call)
        {
            string input = call.Input;
            Player tpTarget = null;
            foreach (Player candidate in Parent.Players.Values.ToArray())
            {
                if (candidate.Name.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tpTarget = candidate;
                    break;
                }
            }
            if (tpTarget == null)
            {
                SendError($"Unable to find player: {input.Trim()}");
                return;
            }
            TryTeleportTo(tpTarget.Id);
        }

        [Command("/tell", Rank.Player, "/t")]
        private void CmdTell(CommandCall call)
        {
            string input = call.Input;
            if (!NameChosen)
            {
                SendError("Choose a name!");
                return;
            }
            if (Client.Account.Muted)
            {
                SendError("Muted. You can not tell at this time.");
                return;
            }
            int tellSpace = input.IndexOf(' ');
            if (tellSpace == -1)
            {
                SendError("Usage: /tell <player name> <text>");
                return;
            }
            string tellName = input.Substring(0, tellSpace);
            string tellMsg = input.Substring(tellSpace + 1);
            if (Name.Equals(tellName, StringComparison.InvariantCultureIgnoreCase))
            {
                SendInfo("Quit telling yourself!");
                return;
            }
            Player tellTarget = Manager.GetPlayer(tellName);
            if (tellTarget == null)
            {
                SendError($"{tellName} not found.");
                return;
            }
            if (tellTarget.Client.Account.IgnoredIds.Contains(AccountId))
                return;
            byte[] tell = GameServer.Text(Name, Id, NumStars, 0, tellTarget.Name, tellMsg);
            tellTarget.Client.Send(tell);
            Client.Send(tell);
        }

        [Command("/l", Rank.Player)]
        private void CmdL(CommandCall call)
        {
            string input = call.Input;
            if (!NameChosen)
            {
                SendError("Choose a name!");
                return;
            }
            if (Client.Account.Muted)
            {
                SendError("You are muted");
                return;
            }
            byte[] local = GameServer.Text(Name, Id, NumStars, 5, "", input);
            foreach (Player player in Parent.Players.Values.ToArray())
                if (!player.Client.Account.IgnoredIds.Contains(AccountId))
                    player.Client.Send(local);
        }

        [Command("/commands", Rank.Player)]
        private void CmdCommands(CommandCall call)
        {
            SendInfo("Available commands: " + string.Join(", ", CommandTable.NamesFor(CallerRank)));
        }

        [Command("/ignore", Rank.Player)]
        private void CmdIgnore(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendError("Usage: /ignore <player name>");
                return;
            }
            if (Name.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                SendInfo("Can't ignore yourself!");
                return;
            }
            int ignoreId = Database.IdFromUsername(input.Trim());
            if (ignoreId == -1)
            {
                SendError("Player not found.");
                return;
            }
            if (!Client.Account.IgnoredIds.Contains(ignoreId))
            {
                //Same cap as EditAccountList (GameServer.MaxAccountListEntries, the AccountList wire limit).
                if (Client.Account.IgnoredIds.Count >= GameServer.MaxAccountListEntries)
                {
                    SendError("Your list is full.");
                    return;
                }
                Client.Account.IgnoredIds.Add(ignoreId);
                Client.Account.Save();
            }
            Client.Send(GameServer.AccountList(1, Client.Account.IgnoredIds));
            SendInfo(input.Trim() + " has been added to your ignore list.");
        }

        [Command("/unignore", Rank.Player)]
        private void CmdUnignore(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendError("Usage: /unignore <player name>");
                return;
            }
            int unignoreId = Database.IdFromUsername(input.Trim());
            if (unignoreId == -1)
            {
                SendError("Player not found.");
                return;
            }
            Client.Account.IgnoredIds.Remove(unignoreId);
            Client.Account.Save();
            Client.Send(GameServer.AccountList(1, Client.Account.IgnoredIds));
            SendInfo(input.Trim() + " no longer ignored.");
        }

        [Command("/lock", Rank.Player)]
        private void CmdLock(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendError("Usage: /lock <player name>");
                return;
            }
            if (Name.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                SendInfo("Can't lock yourself!");
                return;
            }
            int lockId = Database.IdFromUsername(input.Trim());
            if (lockId == -1)
            {
                SendError("Player not found.");
                return;
            }
            if (!Client.Account.LockedIds.Contains(lockId))
            {
                if (Client.Account.LockedIds.Count >= GameServer.MaxAccountListEntries)
                {
                    SendError("Your list is full.");
                    return;
                }
                Client.Account.LockedIds.Add(lockId);
                Client.Account.Save();
            }
            Client.Send(GameServer.AccountList(0, Client.Account.LockedIds));
            SendInfo(input.Trim() + " has been locked.");
        }

        [Command("/unlock", Rank.Player)]
        private void CmdUnlock(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendError("Usage: /unlock <player name>");
                return;
            }
            int unlockId = Database.IdFromUsername(input.Trim());
            if (unlockId == -1)
            {
                SendError("Player not found.");
                return;
            }
            Client.Account.LockedIds.Remove(unlockId);
            Client.Account.Save();
            Client.Send(GameServer.AccountList(0, Client.Account.LockedIds));
            SendInfo(input.Trim() + " no longer locked.");
        }

        [Command("/uptime", Rank.Player)]
        private void CmdUptime(CommandCall call)
        {
            TimeSpan up = TimeSpan.FromMilliseconds(Manager.TotalTimeUnsynced);
            SendInfo(string.Format("The server has been up for {0:D2}h:{1:D2}m:{2:D2}s.", up.Hours, up.Minutes, up.Seconds));
        }

        [Command("/position", Rank.Player)]
        private void CmdPosition(CommandCall call)
        {
            SendInfo("Current Position: " + (int)Position.X + ", " + (int)Position.Y);
        }

        [Command("/time", Rank.Player)]
        private void CmdTime(CommandCall call)
        {
            SendInfo("Time for you to get a watch!");
        }

        [Command("/realm", Rank.Player)]
        private void CmdRealm(CommandCall call)
        {
            TransferToWorld(Manager.RealmId);
        }

        [Command("/nexus", Rank.Player)]
        private void CmdNexus(CommandCall call)
        {
            TransferToWorld(Manager.NexusId);
        }

        [Command("/vault", Rank.Player)]
        private void CmdVault(CommandCall call)
        {
            World vault = Manager.GetVaultWorld(Client);
            TransferToWorld(vault.Id);
        }

        [Command("/ghall", Rank.Player)]
        private void CmdGhall(CommandCall call)
        {
            if (string.IsNullOrWhiteSpace(GuildName))
            {
                SendError("You need to be in a guild.");
                return;
            }
            World hall = Manager.GetGuildHallWorld(GuildName);
            TransferToWorld(hall.Id);
        }

        [Command("/lefttomax", Rank.Player)]
        private void CmdLefttomax(CommandCall call)
        {
            PlayerDesc leftDesc = Desc as PlayerDesc;
            if (leftDesc == null)
            {
                SendError("Not available for this class.");
                return;
            }
            SendInfo($"HP: {leftDesc.Stats[0].MaxValue - Stats[0]}");
            SendInfo($"MP: {leftDesc.Stats[1].MaxValue - Stats[1]}");
            SendInfo($"Attack: {leftDesc.Stats[2].MaxValue - Stats[2]}");
            SendInfo($"Defense: {leftDesc.Stats[3].MaxValue - Stats[3]}");
            SendInfo($"Speed: {leftDesc.Stats[4].MaxValue - Stats[4]}");
            SendInfo($"Dexterity: {leftDesc.Stats[5].MaxValue - Stats[5]}");
            SendInfo($"Vitality: {leftDesc.Stats[6].MaxValue - Stats[6]}");
            SendInfo($"Wisdom: {leftDesc.Stats[7].MaxValue - Stats[7]}");
        }

        [Command("/gkick", Rank.Player)]
        private void CmdGkick(CommandCall call)
        {
            string input = call.Input;
            if (string.IsNullOrWhiteSpace(input))
            {
                SendHelp("/gkick <player name>");
                return;
            }
            if (Name.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                //Commit-gated (W03): the guild message and the cleared guild wait for the commit.
                if (Database.RemoveFromGuild(Client.Account, committed =>
                {
                    if (!committed)
                    {
                        SendError("Guild not found.");
                        return;
                    }
                    SendGuild($"{Name} has left the guild.");
                    GuildName = null;
                    GuildRank = 0;
                }) != Database.GuildResult.OK)
                {
                    SendError("Guild not found.");
                    return;
                }
                return;
            }
            int gkickId = Database.IdFromUsername(input.Trim());
            if (gkickId == -1)
            {
                SendError("Player not found");
                return;
            }
            Client gkickClient = Manager.GetClient(gkickId);
            AccountModel gkickAcc = Manager.GetAuthoritativeAccount(gkickId);
            bool gkickOnline = gkickClient != null;
            if (gkickAcc == null)
            {
                SendError("Player not found");
                return;
            }
            if (Client.Account.GuildRank < 20 || Client.Account.GuildName != gkickAcc.GuildName ||
                Client.Account.GuildRank <= gkickAcc.GuildRank)
            {
                SendError("Can't remove member. Insufficient privileges.");
                return;
            }
            //Commit-gated (W03): the kick and the guild message wait for the commit.
            if (Database.RemoveFromGuild(gkickAcc, committed =>
            {
                if (!committed)
                {
                    SendError("Guild not found.");
                    return;
                }
                if (gkickOnline && gkickClient.Player != null)
                {
                    gkickClient.Player.GuildName = null;
                    gkickClient.Player.GuildRank = 0;
                    gkickClient.Player.SendInfo("You have been kicked from the guild.");
                }
                SendGuild($"{gkickAcc.Name} has been kicked from the guild by {Name}");
            }) != Database.GuildResult.OK)
            {
                SendError("Guild not found.");
                return;
            }
        }

        [Command("/invite", Rank.Player, "/ginvite")]
        private void CmdInvite(CommandCall call)
        {
            string input = call.Input;
            if (Client.Account.GuildRank < 20)
            {
                SendError("Insufficient privileges.");
                return;
            }
            if (string.IsNullOrWhiteSpace(input))
            {
                SendHelp("/invite <player name>");
                return;
            }
            Player inviteTarget = Manager.GetPlayer(input.Trim());
            if (inviteTarget == null)
            {
                SendError("Could not find the player to invite.");
                return;
            }
            if (!inviteTarget.NameChosen)
            {
                SendError("Player needs to choose a name first.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(inviteTarget.Client.Account.GuildName))
            {
                SendError("Player is already in a guild.");
                return;
            }
            inviteTarget.GuildInvite = GuildName;
            inviteTarget.Client.Send(GameServer.InvitedToGuild(Name, GuildName));
            SendInfo($"Invited {inviteTarget.Name}.");
        }

        [Command("/gwho", Rank.Player, "/mates")]
        private void CmdGwho(CommandCall call)
        {
            if (string.IsNullOrWhiteSpace(Client.Account.GuildName))
            {
                SendError("You are not in a guild!");
                return;
            }
            List<string> mates = new List<string>();
            foreach (Client c in Manager.Clients.Values.ToArray())
            {
                if (c.Player != null && c.Account.GuildName == Client.Account.GuildName)
                    mates.Add(c.Player.Name);
            }
            SendInfo("Guild members online: " + (mates.Count == 0 ? "(none)" : string.Join(", ", mates)));
        }

        [Command("/servers", Rank.Player, "/svrs")]
        private void CmdServers(CommandCall call)
        {
            List<string> lines = new List<string>();
            foreach (World w in Manager.Worlds.Values.ToArray())
            {
                if (w.Players.Count == 0)
                    continue;
                lines.Add($"{w.DisplayName ?? w.Name} [{w.Id}] ({w.Players.Count})");
            }
            SendInfo("Worlds online (" + lines.Count + "): " + (lines.Count == 0 ? "(none)" : string.Join(", ", lines)));
        }
    }
}
