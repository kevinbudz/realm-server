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
        private const int ChatCooldownMS = 200;

        public int LastChatTime;

        public void SendInfo(string text) => Client.Send(GameServer.Text("", 0, -1, 0, "", text));
        public void SendError(string text) => Client.Send(GameServer.Text("*Error*", 0, -1, 0, "", text));
        public void SendHelp(string text) => Client.Send(GameServer.Text("*Help*", 0, -1, 0, "", text));
        public void SendClientText(string text) => Client.Send(GameServer.Text("*Client*", 0, -1, 0, "", text));
        public void SendEnemy(string name, string text) => Client.Send(GameServer.Text(name, 0, -1, 0, "", text));

        private void TransferToWorld(int worldId)
        {
            World world = Manager.GetWorld(worldId);
            if (world == null)
            {
                SendError("World not found.");
                return;
            }
            BeginTransfer(world);
        }

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
                    foreach (Transition transition in en.Behavior.RootTransitions)
                        if (transition is PlayerTextTransition rootTextTransition)
                            rootTextTransition.OnChatReceived(this, validText);
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
                RunCommand(command, j, input);
                return;
            }

            if (Client.Account.Muted)
            {
                SendError("You are muted");
                return;
            }

            ReadOnlySpan<byte> packet = GameServer.Text(Name, Id, NumStars, 5, "", validText);

            foreach (Player player in Parent.Players.Values)
                if (!player.Client.Account.IgnoredIds.Contains(AccountId))
                    player.Client.Send(packet);
        }

        //[W11] Chat commands are methods marked [Command] (Game/Commands). Every command goes through here, so the
        //unknown-command reply and the rank check are the same for all of them.
        private Rank CallerRank => Client.Account == null ? Rank.Player : Client.Account.Rank;

        private void RunCommand(string command, string[] args, string input)
        {
            if (!CommandTable.TryFind(command.ToLower(), out CommandInfo info))
            {
                SendError("Unknown command");
                return;
            }
            if (CallerRank < info.MinRank)
            {
                if (!info.Silent)
                    SendError("Not ranked");
                return;
            }
            info.Handler(this, new CommandCall(command, args, input));
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
