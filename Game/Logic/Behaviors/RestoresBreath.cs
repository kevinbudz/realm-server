using System;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference RestoresBreathBehavior. Player breath is the Oxygen stat (StatType.Breath), capped at 100.
    //Each tick, every player within Radius gains Speed * SecondsPerTick breath (10 per tick at the default speed of 50),
    //rounded to whole points because Oxygen is an int.
    public class RestoresBreath : Behavior
    {
        private const int MaxBreath = 100;

        public readonly float Radius;
        public readonly float Speed;

        public RestoresBreath(double radius = 1, double speed = 50)
        {
            Radius = (float)radius;
            Speed = (float)speed;
        }

        public override bool Tick(Entity host)
        {
            if (host.Parent == null)
                return false;

            int gain = (int)Math.Round(Speed * Settings.SecondsPerTick);
            float radiusSq = Radius * Radius;
            foreach (Entity en in host.Parent.PlayerChunks.HitTest(host.Position, Radius))
            {
                if (en is Player player && host.Position.DistanceSquared(player.Position) < radiusSq)
                    player.Oxygen = Math.Min(MaxBreath, player.Oxygen + gain);
            }
            return true;
        }
    }
}
