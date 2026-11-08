namespace RotMG.Game.Logic.Behaviors
{
    //Hand-written toolkit: not emitted by Tools/XmlBehaviorConverter (files/xml has no equivalent).
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
