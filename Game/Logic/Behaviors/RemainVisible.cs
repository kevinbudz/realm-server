namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference RemainVisibleBehavior. The reference adds the host to the game's always-visible set (sent to
    //every player regardless of distance) until death. This engine has no always-visible set: sight is chunk-distance
    //based (Player.SightRadius), so this behavior is a no-op.
    public class RemainVisible : Behavior
    {
    }
}
