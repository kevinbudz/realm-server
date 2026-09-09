using RotMG.Common;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class TextureState
    {
        public int CurrentTexture;
        public int RemainingTime;
    }

    public class SetAltTexture : Behavior
    {
        public readonly int IndexMin;
        public readonly int IndexMax;
        public readonly int Cooldown;
        public readonly int CooldownVariance;
        public readonly bool Loop;

        public SetAltTexture(int minValue, int maxValue = -1, int cooldown = 0, int cooldownVariance = 0, bool loop = false)
        {
            IndexMin = minValue;
            IndexMax = maxValue;
            Cooldown = MathUtils.NormalizeCooldown(cooldown, 0);
            CooldownVariance = cooldownVariance;
            Loop = loop;
        }

        private int NextCooldown()
        {
            int cool = Cooldown;
            if (CooldownVariance != 0)
                cool += MathUtils.NextIntSnap(-CooldownVariance, CooldownVariance, Settings.MillisecondsPerTick);
            return cool;
        }

        public override void Enter(Entity host)
        {
            TextureState state = new TextureState
            {
                CurrentTexture = IndexMin,
                RemainingTime = NextCooldown()
            };
            host.StateObject[Id] = state;
            host.TrySetSV(StatType.AltTexture, IndexMin);
        }

        public override bool Tick(Entity host)
        {
            TextureState state = host.StateObject[Id] as TextureState;
            if (state == null)
                return false;

            if (IndexMax == -1 || (state.CurrentTexture == IndexMax && !Loop))
                return false;

            if (state.RemainingTime <= 0)
            {
                int next = state.CurrentTexture >= IndexMax ? IndexMin : state.CurrentTexture + 1;
                host.TrySetSV(StatType.AltTexture, next);
                state.CurrentTexture = next;
                state.RemainingTime = NextCooldown();
                return true;
            }

            state.RemainingTime -= Settings.MillisecondsPerTick;
            return false;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
