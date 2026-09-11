using RotMG.Common;
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
        private const int ChatCooldownMS = 200;

        public int LastChatTime;

        public void SendInfo(string text) => Client.Send(GameServer.Text("", 0, -1, 0, "", text));
        public void SendError(string text) => Client.Send(GameServer.Text("*Error*", 0, -1, 0, "", text));
        public void SendHelp(string text) => Client.Send(GameServer.Text("*Help*", 0, -1, 0, "", text));
        public void SendClientText(string text) => Client.Send(GameServer.Text("*Client*", 0, -1, 0, "", text));
        public void SendEnemy(string name, string text) => Client.Send(GameServer.Text(name, 0, -1, 0, "", text));

        public void Chat(string text)
        {
            if (text.Length <= 0 || text.Length > 128)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Text too short or too long");
#endif
                Client.Disconnect();
                return;
            }

            string validText = Regex.Replace(text, @"[^a-zA-Z0-9`!@#$%^&* ()_+|\-=\\{}\[\]:"";'<>?,./]", "");
            if (validText.Length <= 0)
            {
                SendError("Invalid text.");
                return;
            }

            if (LastChatTime + ChatCooldownMS > Manager.TotalTimeUnsynced)
            {
                SendError("Message sent too soon after previous one.");
                return;
            }

            LastChatTime = Manager.TotalTimeUnsynced;

            if (Parent != null)
                foreach (Entity en in Parent.Entities.Values.ToArray())
                {
                    if (en.CurrentStates == null)
                        continue;
                    foreach (State state in en.CurrentStates.ToArray())
                        foreach (Transition transition in state.Transitions)
                            if (transition is PlayerTextTransition textTransition)
                                textTransition.OnChatReceived(this, validText);
                }

            if (validText[0] == '/')
            {
                string[] s = validText.Split(' ');
                string[] j = new string[s.Length - 1];
                for (int i = 1; i < s.Length; i++)
                    j[i - 1] = s[i];
                string command = s[0];
                string input = string.Join(' ', j);
                switch (command.ToLower())
                {
                    case "/legendary":
                        if (Client.Account.Ranked)
                        {
                            int slot = int.Parse(j[0]);
                            if (Inventory[slot] != -1)
                            {
                                Tuple<bool, ItemData> roll = Resources.Type2Item[(ushort)Inventory[slot]].Roll();
                                while ((roll.Item2 & ItemData.T7) == 0)
                                    roll = Resources.Type2Item[(ushort)Inventory[slot]].Roll();
                                ItemDatas[slot] = !roll.Item1 ? -1 : (int)roll.Item2;
                                UpdateInventorySlot(slot);
                                RecalculateEquipBonuses();
                            }
                        }
                        break;
                    case "/roll":
                        if (Client.Account.Ranked)
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
                        break;
                    case "/disconnect":
                    case "/dcAll":
                    case "/dc":
                        if (Client.Account.Ranked)
                        {
                            foreach (Client c in Manager.Clients.Values.ToArray())
                            {
                                try { c.Disconnect(); }
                                catch { }
                            }
                        }
                        break;
                    case "/terminate":
                    case "/stop":
                        if (Client.Account.Ranked)
                        {
                            Program.StartTerminating();
                            return;
                        }
                        break;
                    case "/gimme":
                    case "/give":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
                        if (Resources.IdLower2Item.TryGetValue(input.ToLower(), out ItemDesc item))
                        {
                            if (GiveItem(item.Type))
                                SendInfo("Success");
                            else SendError("No inventory slots");
                        }
                        else SendError($"Item <{input}> not found in GameData");
                        break;
                    case "/create":
                    case "/spawn":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
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
                        break;
                    case "/max":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
                        for (int i = 0; i < Stats.Length; i++)
                            Stats[i] = (Desc as PlayerDesc).Stats[i].MaxValue;
                        UpdateStats();
                        SendInfo("Maxed");
                        break;
                    case "/god":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
                        ApplyConditionEffect(ConditionEffectIndex.Invincible, HasConditionEffect(ConditionEffectIndex.Invincible) ? 0 : -1);
                        SendInfo($"Godmode set to {HasConditionEffect(ConditionEffectIndex.Invincible)}");
                        break;
                    case "/allyshots":
                        Client.Account.AllyShots = !Client.Account.AllyShots;
                        SendInfo($"Ally shots set to {Client.Account.AllyShots}");
                        break;
                    case "/allydamage":
                        Client.Account.AllyDamage = !Client.Account.AllyDamage;
                        SendInfo($"Ally damage set to {Client.Account.AllyDamage}");
                        break;
                    case "/effects":
                        Client.Account.Effects = !Client.Account.Effects;
                        SendInfo($"Effects set to {Client.Account.Effects}");
                        break;
                    case "/sounds":
                        Client.Account.Sounds = !Client.Account.Sounds;
                        SendInfo($"Sounds set to {Client.Account.Sounds}");
                        break;
                    case "/notifications":
                        Client.Account.Notifications = !Client.Account.Notifications;
                        SendInfo($"Notifications set to {Client.Account.Notifications}");
                        break;
                    case "/online":
                    case "/who":
                        SendInfo($"" +
                            $"<{Manager.Clients.Values.Count(k => k.Player != null)} Player(s)> " +
                            $"<{string.Join(", ", Manager.Clients.Values.Where(k => k.Player != null).Select(k => k.Player.Name))}>");
                        break;
                    case "/server":
                    case "/pos":
                    case "/loc":
                        SendInfo(this.ToString());
                        break;
                    case "/where":
                    case "/find":
                        Player findTarget = Manager.GetPlayer(input);
                        if (findTarget == null) SendError("Couldn't find player");
                        else SendInfo(findTarget.ToString());
                        break;
                    case "/g":
                    case "/guild":
                        if (string.IsNullOrWhiteSpace(GuildName))
                            SendError("You are not in a guild.");
                        else if (string.IsNullOrWhiteSpace(input))
                            SendHelp("/g <message>");
                        else
                            SendGuild($"<{Name}> {input}");
                        break;
                    case "/trade":
                        if (string.IsNullOrWhiteSpace(input))
                            SendHelp("/trade <player name>");
                        else if (Parent == null)
                            SendError("You are not in a world.");
                        else
                            RequestTrade(input);
                        break;
                    case "/fame":
                    case "/famestats":
                    case "/stats":
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
                        break;
                    case "/closerealm":
                        if (!Client.Account.Ranked)
                        {
                            SendError("Not ranked");
                            return;
                        }
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
                        break;
                    case "/clearspawn":
                    case "/cs":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            break;
                        }
                    case "/cleargraves":
                    case "/cgraves":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            break;
                        }
                    case "/eff":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            break;
                        }
                    case "/grank":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            AccountModel rankAcc = new AccountModel(rankId);
                            rankAcc.Load();
                            if (Database.ChangeGuildRank(rankAcc, rank) != Database.GuildResult.OK)
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
                            break;
                        }
                    case "/tppos":
                    case "/goto":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            string[] coords = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (coords.Length != 2 || !int.TryParse(coords[0], out int gotoX) || !int.TryParse(coords[1], out int gotoY))
                            {
                                SendError("Invalid coordinates! Usage: /tppos <x> <y>");
                                return;
                            }
                            if (!Teleport(Manager.TotalTimeUnsynced, new Position(gotoX + 0.5f, gotoY + 0.5f)))
                                SendError("Cannot teleport there.");
                            break;
                        }
                    case "/setpiece":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            break;
                        }
                    case "/killall":
                    case "/ka":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            break;
                        }
                    case "/kick":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            break;
                        }
                    case "/getquest":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            if (Quest == null)
                            {
                                SendError("Player does not have a quest!");
                                return;
                            }
                            SendInfo("Quest location: (" + (int)Quest.Position.X + ", " + (int)Quest.Position.Y + ")");
                            break;
                        }
                    case "/oryxsay":
                    case "/osay":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            foreach (Player witness in Parent.Players.Values.ToArray())
                                witness.SendEnemy("Oryx the Mad God", input);
                            break;
                        }
                    case "/announce":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            foreach (Client c in Manager.Clients.Values.ToArray())
                                if (c.Player != null)
                                    c.Player.SendInfo("[Announcement] " + input);
                            break;
                        }
                    case "/summon":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            summoned.Teleport(Manager.TotalTimeUnsynced, Position);
                            summoned.SendInfo($"You've been summoned by {Name}.");
                            SendInfo("Player summoned!");
                            break;
                        }
                    case "/summonall":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            foreach (Player guest in Parent.Players.Values.ToArray())
                            {
                                if (guest == this)
                                    continue;
                                guest.Teleport(Manager.TotalTimeUnsynced, Position);
                                guest.SendInfo($"You've been summoned by {Name}.");
                            }
                            SendInfo("All players summoned!");
                            break;
                        }
                    case "/killplayer":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            Player victim = Manager.GetPlayer(input.Trim());
                            if (victim == null)
                            {
                                SendError($"Player '{input.Trim()}' could not be found!");
                                return;
                            }
                            victim.HP = 0;
                            victim.Death(Name);
                            SendInfo("Player killed!");
                            break;
                        }
                    case "/size":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input.Trim(), out int newSize) ||
                                (newSize != 0 && (newSize < 0 || newSize > 500)))
                            {
                                SendError("Usage: /size <0-500>. Using 0 restores the default size.");
                                return;
                            }
                            Size = newSize == 0 ? 100 : newSize;
                            SendInfo(newSize == 0 ? "Size restored." : $"Size set to {Size}.");
                            break;
                        }
                    case "/reboot":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            //Single-process build: no inter-server reboot bus, so a
                            //reboot is a clean shutdown for the external monitor.
                            SendInfo("Rebooting server...");
                            Program.StartTerminating();
                            return;
                        }
                    case "/reskin":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            break;
                        }
                    case "/tq":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            if (Quest == null)
                            {
                                SendError("Player does not have a quest!");
                                return;
                            }
                            if (!Teleport(Manager.TotalTimeUnsynced, Quest.Position, ignoreSeen: true))
                                SendError("Cannot teleport to quest.");
                            else
                                SendInfo("Teleported to Quest Location: (" + (int)Quest.Position.X + ", " + (int)Quest.Position.Y + ")");
                            break;
                        }
                    case "/mute":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            AccountModel muteAcc = new AccountModel(muteId);
                            muteAcc.Load();
                            if (muteAcc.Ranked)
                            {
                                SendError("Cannot mute other admins.");
                                return;
                            }
                            muteAcc.Muted = true;
                            muteAcc.Save();
                            foreach (Client c in Manager.Clients.Values.ToArray())
                                if (c.Account != null && c.Account.Id == muteId)
                                    c.Account.Muted = true;
                            if (muteMinutes > 0)
                            {
                                int unmuteId = muteId;
                                string unmuteName = muteArgs[0];
                                Manager.AddTimedAction(muteMinutes * 60000, () =>
                                {
                                    AccountModel timed = new AccountModel(unmuteId);
                                    timed.Load();
                                    timed.Muted = false;
                                    timed.Save();
                                    Client live = Manager.GetClient(unmuteId);
                                    if (live != null)
                                        live.Account.Muted = false;
                                });
                                SendInfo(unmuteName + " successfully muted for " + muteMinutes + " minutes.");
                            }
                            else
                                SendInfo(muteArgs[0] + " successfully muted indefinitely.");
                            break;
                        }
                    case "/unmute":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            AccountModel unmuteAcc = new AccountModel(unmuteId);
                            unmuteAcc.Load();
                            if (!unmuteAcc.Muted)
                                SendInfo(input.Trim() + " wasn't muted...");
                            else
                            {
                                unmuteAcc.Muted = false;
                                unmuteAcc.Save();
                                SendInfo(input.Trim() + " successfully unmuted.");
                            }
                            Client liveUnmuted = Manager.GetClient(unmuteId);
                            if (liveUnmuted != null)
                                liveUnmuted.Account.Muted = false;
                            break;
                        }
                    case "/ban":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            AccountModel banAcc = new AccountModel(banId);
                            banAcc.Load();
                            banAcc.Banned = true;
                            banAcc.Save();
                            Client liveBanned = Manager.GetClient(banId);
                            try { liveBanned?.Disconnect(); } catch { }
                            SendInfo($"{banAcc.Name} successfully banned. Reason: {banReason}");
                            break;
                        }
                    case "/banip":
                    case "/ipban":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            AccountModel banIpAcc = new AccountModel(banIpId);
                            banIpAcc.Load();
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
                            break;
                        }
                    case "/unban":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            AccountModel unbanAcc = new AccountModel(unbanId);
                            unbanAcc.Load();
                            if (!unbanAcc.Banned)
                                SendInfo($"{unbanAcc.Name} wasn't banned...");
                            else
                            {
                                unbanAcc.Banned = false;
                                unbanAcc.Save();
                                SendInfo($"Success! {unbanAcc.Name}'s account no longer banned.");
                            }
                            break;
                        }
                    case "/clearinv":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            for (int i = 4; i < 12 && i < Inventory.Length; i++)
                            {
                                Inventory[i] = -1;
                                UpdateInventorySlot(i);
                            }
                            RecalculateEquipBonuses();
                            SendInfo("Inventory Cleared.");
                            break;
                        }
                    case "/quake":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            break;
                        }
                    case "/visit":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            Client.Active = false;
                            CancelTradeIfTrading();
                            Client.Send(GameServer.Reconnect(visitTarget.Parent.Id));
                            Manager.AddTimedAction(2000, Client.Disconnect);
                            break;
                        }
                    case "/link":
                    case "/unlink":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            //Single-process build has no portal-link monitor: realm
                            //portals are placed directly, so there is nothing to link.
                            SendError(command.Equals("/link") ? "Link not supported in this build." : "Link not found.");
                            break;
                        }
                    case "/level20":
                    case "/l20":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            if (Level >= MaxLevel)
                                return;
                            EXP = GetLevelEXP(MaxLevel);
                            Level = MaxLevel;
                            UpdateStats();
                            SendInfo("Leveled to 20.");
                            break;
                        }
                    case "/rename":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            AccountModel renameAcc = new AccountModel(renameId);
                            renameAcc.Load();
                            Database.RenameAccountKeys(renameId, oldName, newName, renameAcc);
                            Client liveRenamed = Manager.GetClient(renameId);
                            if (liveRenamed != null)
                            {
                                AccountModel fresh = new AccountModel(renameId);
                                fresh.Load();
                                liveRenamed.Account = fresh;
                                if (liveRenamed.Player != null)
                                {
                                    liveRenamed.Player.Name = newName;
                                    liveRenamed.Player.Credits = fresh.Stats.Credits;
                                }
                            }
                            SendInfo("Rename successful.");
                            break;
                        }
                    case "/unname":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
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
                            AccountModel unnameAcc = new AccountModel(unnameId);
                            unnameAcc.Load();
                            Database.RenameAccountKeys(unnameId, unnameOld, guestName, unnameAcc);
                            Client liveUnnamed = Manager.GetClient(unnameId);
                            if (liveUnnamed != null)
                            {
                                AccountModel fresh = new AccountModel(unnameId);
                                fresh.Load();
                                liveUnnamed.Account = fresh;
                                if (liveUnnamed.Player != null)
                                {
                                    liveUnnamed.Player.Name = guestName;
                                    liveUnnamed.Player.NameChosen = false;
                                }
                            }
                            SendInfo("Account successfully unnamed.");
                            break;
                        }
                    case "/compactloh":
                        {
                            if (!Client.Account.Ranked)
                            {
                                SendError("Not ranked");
                                return;
                            }
                            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                            System.GC.Collect();
                            SendInfo("LOH compacted.");
                            break;
                        }
                    case "/gland":
                    case "/glands":
                        {
                            if (!(Parent is RealmWorld))
                            {
                                SendError("This command requires you to be in realm first.");
                                return;
                            }
                            Teleport(Manager.TotalTimeUnsynced, new Position(1512 + 0.5f, 1048 + 0.5f));
                            break;
                        }
                    case "/join":
                        {
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
                            if (Database.AddGuildMember(GuildInvite, Client.Account) != Database.GuildResult.OK)
                            {
                                SendError("Could not join guild.");
                                return;
                            }
                            GuildName = Client.Account.GuildName;
                            GuildRank = Client.Account.GuildRank;
                            GuildInvite = null;
                            SendInfo("Joined guild " + GuildName + ".");
                            break;
                        }
                    case "/tutorial":
                        {
                            Client.Active = false;
                            CancelTradeIfTrading();
                            Client.Send(GameServer.Reconnect(Manager.TutorialId));
                            Manager.AddTimedAction(2000, Client.Disconnect);
                            break;
                        }
                    case "/world":
                        {
                            SendInfo($"[{Parent.Id}] {Parent.DisplayName ?? Parent.Name} ({Parent.Players.Count} players)");
                            break;
                        }
                    case "/pause":
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
                            break;
                        }
                    case "/tp":
                    case "/teleport":
                        {
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
                            break;
                        }
                    case "/tell":
                    case "/t":
                        {
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
                            break;
                        }
                    case "/l":
                        {
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
                            break;
                        }
                    case "/commands":
                        {
                            SendInfo("Available commands: /tell, /l, /g, /trade, /who, /online, /where, /server, /pos, /position, " +
                                "/world, /uptime, /time, /realm, /nexus, /vault, /ghall, /tutorial, /gland, /join, /pause, " +
                                "/tp, /lock, /unlock, /ignore, /unignore, /lefttomax, /gkick, /invite, /gwho, /mates, " +
                                "/servers, /svrs, /fame, /spawn, /give, /max, /size, /level20, /eff, /tq, /tppos, /goto, " +
                                "/getquest, /setpiece, /killall, /ka, /clearspawn, /cs, /cleargraves, /cgraves, /clearinv, " +
                                "/summon, /summonall, /visit, /kick, /mute, /unmute, /ban, /banip, /unban, /grank, " +
                                "/rename, /unname, /quake, /closerealm, /announce, /oryxsay, /osay, /reskin, /reboot, " +
                                "/compactloh, /god, /roll, /legendary, /allyshots, /allydamage, /effects, /sounds, /notifications");
                            break;
                        }
                    case "/ignore":
                        {
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
                                Client.Account.IgnoredIds.Add(ignoreId);
                                Client.Account.Save();
                            }
                            Client.Send(GameServer.AccountList(1, Client.Account.IgnoredIds));
                            SendInfo(input.Trim() + " has been added to your ignore list.");
                            break;
                        }
                    case "/unignore":
                        {
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
                            break;
                        }
                    case "/lock":
                        {
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
                                Client.Account.LockedIds.Add(lockId);
                                Client.Account.Save();
                            }
                            Client.Send(GameServer.AccountList(0, Client.Account.LockedIds));
                            SendInfo(input.Trim() + " has been locked.");
                            break;
                        }
                    case "/unlock":
                        {
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
                            break;
                        }
                    case "/uptime":
                        {
                            TimeSpan up = TimeSpan.FromMilliseconds(Manager.TotalTimeUnsynced);
                            SendInfo(string.Format("The server has been up for {0:D2}h:{1:D2}m:{2:D2}s.", up.Hours, up.Minutes, up.Seconds));
                            break;
                        }
                    case "/position":
                        {
                            SendInfo("Current Position: " + (int)Position.X + ", " + (int)Position.Y);
                            break;
                        }
                    case "/time":
                        {
                            SendInfo("Time for you to get a watch!");
                            break;
                        }
                    case "/realm":
                        {
                            Client.Active = false;
                            CancelTradeIfTrading();
                            Client.Send(GameServer.Reconnect(Manager.RealmId));
                            Manager.AddTimedAction(2000, Client.Disconnect);
                            break;
                        }
                    case "/nexus":
                        {
                            Client.Active = false;
                            CancelTradeIfTrading();
                            Client.Send(GameServer.Reconnect(Manager.NexusId));
                            Manager.AddTimedAction(2000, Client.Disconnect);
                            break;
                        }
                    case "/vault":
                        {
                            World vault = Manager.GetVaultWorld(Client);
                            Client.Active = false;
                            CancelTradeIfTrading();
                            Client.Send(GameServer.Reconnect(vault.Id));
                            Manager.AddTimedAction(2000, Client.Disconnect);
                            break;
                        }
                    case "/ghall":
                        {
                            if (string.IsNullOrWhiteSpace(GuildName))
                            {
                                SendError("You need to be in a guild.");
                                return;
                            }
                            World hall = Manager.GetGuildHallWorld(GuildName);
                            Client.Active = false;
                            CancelTradeIfTrading();
                            Client.Send(GameServer.Reconnect(hall.Id));
                            Manager.AddTimedAction(2000, Client.Disconnect);
                            break;
                        }
                    case "/lefttomax":
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
                            break;
                        }
                    case "/gkick":
                        {
                            if (string.IsNullOrWhiteSpace(input))
                            {
                                SendHelp("/gkick <player name>");
                                return;
                            }
                            if (Name.Equals(input.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (Database.RemoveFromGuild(Client.Account) != Database.GuildResult.OK)
                                {
                                    SendError("Guild not found.");
                                    return;
                                }
                                SendGuild($"{Name} has left the guild.");
                                GuildName = null;
                                GuildRank = 0;
                                return;
                            }
                            int gkickId = Database.IdFromUsername(input.Trim());
                            if (gkickId == -1)
                            {
                                SendError("Player not found");
                                return;
                            }
                            Client gkickClient = Manager.GetClient(gkickId);
                            AccountModel gkickAcc = gkickClient?.Account;
                            bool gkickOnline = true;
                            if (gkickAcc == null)
                            {
                                gkickAcc = new AccountModel(gkickId);
                                gkickAcc.Load();
                                gkickOnline = false;
                            }
                            if (Client.Account.GuildRank < 20 || Client.Account.GuildName != gkickAcc.GuildName ||
                                Client.Account.GuildRank <= gkickAcc.GuildRank)
                            {
                                SendError("Can't remove member. Insufficient privileges.");
                                return;
                            }
                            if (Database.RemoveFromGuild(gkickAcc) != Database.GuildResult.OK)
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
                            break;
                        }
                    case "/invite":
                    case "/ginvite":
                        {
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
                            break;
                        }
                    case "/gwho":
                    case "/mates":
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
                            break;
                        }
                    case "/servers":
                    case "/svrs":
                        {
                            List<string> lines = new List<string>();
                            foreach (World w in Manager.Worlds.Values.ToArray())
                            {
                                if (w.Players.Count == 0)
                                    continue;
                                lines.Add($"{w.DisplayName ?? w.Name} [{w.Id}] ({w.Players.Count})");
                            }
                            SendInfo("Worlds online (" + lines.Count + "): " + (lines.Count == 0 ? "(none)" : string.Join(", ", lines)));
                            break;
                        }
                    default:
                        SendError("Unknown command");
                        break;
                }
                return;
            }

            if (Client.Account.Muted)
            {
                SendError("You are muted");
                return;
            }

            byte[] packet = GameServer.Text(Name, Id, NumStars, 5, "", validText);

            foreach (Player player in Parent.Players.Values)
                if (!player.Client.Account.IgnoredIds.Contains(AccountId))
                    player.Client.Send(packet);
        }

        //Quake targets must be enterable worlds: a static <Maps> file or a
        //DungeonGen template (see Dungeons.DungeonWorld.IsSupported), which
        //covers generated dungeons like Abyss of Demons. Nexus, Realm, Vault,
        //Guild Hall and Tutorial are excluded as targets as well: the first
        //is where /quake is denied, and the others are per-account or
        //per-guild instances that a fresh copy would not represent.
        private static bool IsQuakeTarget(string key)
        {
            switch (key)
            {
                case "Nexus":
                case "Realm":
                case "Vault":
                case "GuildHall":
                case "Tutorial":
                    return false;
                default:
                    return Dungeons.DungeonWorld.IsSupported(Resources.Worlds[key]);
            }
        }

        private static List<string> ValidQuakeWorlds()
        {
            List<string> names = new List<string>();
            foreach (string key in Resources.Worlds.Keys)
                if (IsQuakeTarget(key))
                    names.Add(key);
            return names;
        }
    }
}
