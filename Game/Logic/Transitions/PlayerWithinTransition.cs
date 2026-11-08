using RotMG.Common;
using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Transitions
{
    //Old playerWithin ("dist" or "dist|count") and noPlayerWithin. Holds while at least MinPlayers players are within
    //Distance of the host; Invert holds while fewer than MinPlayers are (noPlayerWithin is Invert with MinPlayers 1).
    //Players are counted from PlayerChunks, which also holds Decoy entries. Two rules, kept from the three old classes:
    //  - Invisible players do not count unless SeeInvis is set (old seeInvis / GetNearestPlayer rule).
    //  - The single-player form (MinPlayers 1, old PlayerWithin and NoPlayerWithin) counts any entry, Decoys included,
    //    as GetNearestPlayer did. The head-count form (MinPlayers above 1, old PlayersWithin) counts only Player
    //    instances.
    public class PlayerWithinTransition : Transition
    {
        public readonly float Distance;
        public readonly int MinPlayers;
        public readonly bool SeeInvis;
        public readonly bool Invert;

        public PlayerWithinTransition(double dist, string targetState, int minPlayers = 1, bool seeInvis = false, bool invert = false)
            : base(targetState)
        {
            Distance = (float)dist;
            MinPlayers = minPlayers;
            SeeInvis = seeInvis;
            Invert = invert;
        }

        public override bool Tick(Entity host)
        {
            bool within = Within(host);
            return Invert ? !within : within;
        }

        private bool Within(Entity host)
        {
            if (MinPlayers <= 0)
                return true;
            if (host.Parent == null)
                return false;

            int count = 0;
            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Distance))
            {
                if (!SeeInvis && en.HasConditionEffect(ConditionEffectIndex.Invisible))
                    continue;
                if (MinPlayers > 1 && !(en is Player))
                    continue;
                if (++count >= MinPlayers)
                    return true;
            }
            return false;
        }
    }
}
