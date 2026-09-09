using RotMG.Game.Entities;
using RotMG.Utils;
using System.Linq;
using System.Text.RegularExpressions;

namespace RotMG.Game.Logic.Transitions
{
    public class PlayerTextTransition : Transition
    {
        public readonly float? DistanceSquared;
        public readonly string Pattern;
        public readonly bool SetAttackTarget;
        public readonly bool IgnoreCase;

        private bool _transition;
        private Player _player;

        public PlayerTextTransition(string targetState, string regex, double? dist = null,
            bool setAttackTarget = false, bool ignoreCase = true) : base(targetState)
        {
            Pattern = regex;
            SetAttackTarget = setAttackTarget;
            IgnoreCase = ignoreCase;
            if (dist != null)
                DistanceSquared = (float)(dist.Value * dist.Value);
        }

        public void OnChatReceived(Player player, string text)
        {
            Regex regex = IgnoreCase
                ? new Regex(Pattern, RegexOptions.IgnoreCase)
                : new Regex(Pattern);
            if (!regex.Match(text).Success)
            {
                _transition = false;
                _player = null;
                return;
            }
            _transition = true;
            _player = player;
        }

        public override bool Tick(Entity host)
        {
            if (!_transition || host.Parent == null || _player == null ||
                !host.Parent.Players.Values.Contains(_player))
                return false;

            if (SetAttackTarget)
                host.StateObject[Id] = _player;
            if (DistanceSquared != null)
                return host.Position.DistanceSquared(_player.Position) <= DistanceSquared;
            return true;
        }
    }
}
