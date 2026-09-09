using RotMG.Common;

namespace RotMG.Game.Logic.Behaviors
{
    //Destination tiles carry no elevation data, so this is kept for
    //behavior-database compatibility and always reports completion.
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
            return false;
        }
    }
}
