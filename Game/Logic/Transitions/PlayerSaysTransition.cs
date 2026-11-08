using RotMG.Utils;
using System.Text;

namespace RotMG.Game.Logic.Transitions
{
    //Old playerSays: holds when a recent player chat within 30 tiles, once cleaned, equals the cleaned text.
    //Chat arrives through World.RecentChats (filled by Player.Chat). The transition reads that list each tick, so it
    //keeps a position and time window and needs no per-chat hook.
    public class PlayerSaysTransition : Transition
    {
        public const float ChatRange = 30f;

        public readonly string Text;

        public PlayerSaysTransition(string text, string targetState) : base(targetState)
        {
            Text = CleanText(text);
        }

        public override bool Tick(Entity host)
        {
            World world = host.Parent;
            if (world == null)
                return false;

            world.PruneRecentChats();
            float rangeSq = ChatRange * ChatRange;
            foreach (World.RecentChat chat in world.RecentChats)
            {
                if (host.Position.DistanceSquared(chat.Position) > rangeSq)
                    continue;
                if (CleanText(chat.Text) == Text)
                    return true;
            }
            return false;
        }

        //Old cleanString: lowercase, keep ASCII letters and spaces, then trim.
        public static string CleanText(string value)
        {
            if (value == null)
                return "";

            StringBuilder cleaned = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                char lower = c >= 'A' && c <= 'Z' ? (char)(c + 32) : c;
                if ((lower >= 'a' && lower <= 'z') || lower == ' ')
                    cleaned.Append(lower);
            }
            return cleaned.ToString().Trim();
        }
    }
}
