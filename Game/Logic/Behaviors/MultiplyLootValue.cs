namespace RotMG.Game.Logic.Behaviors
{
    //Destination loot has no scalar hook, so this is kept for
    //behavior-database compatibility and applies no scaling.
    public class MultiplyLootValue : Behavior
    {
        public readonly int Multiplier;

        public MultiplyLootValue(int multiplier)
        {
            Multiplier = multiplier;
        }
    }
}
