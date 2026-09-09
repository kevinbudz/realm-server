using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    // Keeps flyers above low ground, mirroring realm-src-master
    // wServer/logic/behaviors/StayAbove.cs: below its altitude the host
    // drifts toward the map center until the tile elevation allows rest.
    public class StayAbove : Behavior
    {
        public readonly float Speed;
        public readonly int Altitude;

        public StayAbove(double speed, int altitude)
        {
            Speed = (float)speed;
            Altitude = altitude;
        }

        public override bool Tick(Entity host)
        {
            if (host.HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return false;

            World world = host.Parent;
            Tile tile = world.GetTile((int)host.Position.X, (int)host.Position.Y);
            if (tile != null && tile.Elevation != 0 && tile.Elevation < Altitude)
            {
                Position vect = new Position(
                    world.Width / 2 - host.Position.X,
                    world.Height / 2 - host.Position.Y);
                vect.Normalize();
                float dist = host.GetSpeed(Speed) * Settings.SecondsPerTick;
                host.ValidateAndMove(host.Position + vect * dist);
                return true;
            }
            return false;
        }
    }
}
