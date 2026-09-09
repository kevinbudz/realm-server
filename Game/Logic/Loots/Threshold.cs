using System.Text;

namespace RotMG.Game.Logic.Loots
{
    public class Threshold : Loot
    {
        public readonly float Value;
        public readonly Loot[] Children;

        public Threshold(float threshold, params Loot[] children)
        {
            Value = threshold;
            Children = children;
        }
    }
}
