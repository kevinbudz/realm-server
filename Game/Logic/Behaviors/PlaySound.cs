namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference PlaySoundBehavior (PlaySoundMessage(objectId, soundId), once per state entry). The PlaySound
    //packet here (GameServer.PlaySound) carries a sound name and no numeric sound id, so there is no packet to send and
    //this behavior is a no-op. SoundId is kept for the contract.
    public class PlaySound : Behavior
    {
        public readonly int SoundId;

        public PlaySound(int soundId = 0)
        {
            SoundId = soundId;
        }
    }
}
