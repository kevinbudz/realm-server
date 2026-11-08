using RotMG.Common;

namespace RotMG.Game.Logic.Behaviors
{
    //Port of the reference MetamorphoseBehavior: after Time ms, spawns Child at the host's position, then dies through
    //BehaviorDeath.Die (loot and XP with DropLoot). A missing or unknown child falls back to the host's own type, as in the
    //reference.
    public class Metamorphose : Behavior
    {
        public readonly string Child;
        public readonly int Time;
        public readonly bool DropLoot;
        public readonly bool GiveXP;

        public Metamorphose(string child, int time = 60000, bool dropLoot = false, bool giveXP = false)
        {
            Child = child;
            Time = time;
            DropLoot = dropLoot;
            GiveXP = giveXP;
        }

        public override void Enter(Entity host)
        {
            host.StateCooldown[Id] = Time;
        }

        public override bool Tick(Entity host)
        {
            host.StateCooldown[Id] -= Settings.MillisecondsPerTick;
            if (host.StateCooldown[Id] > 0)
                return false;

            ushort type = host.Type;
            if (!string.IsNullOrEmpty(Child) && BehaviorHelpers.TryGetObjType(Child, out ushort childType))
                type = childType;
            //The child is spawned before the host dies, as in the reference. SpawnChild returns null when the host has
            //no world, and BehaviorDeath.Die does nothing without one, so the host is removed exactly once.
            BehaviorHelpers.SpawnChild(host, type, host.Position);
            BehaviorDeath.Die(host, DropLoot);
            return true;
        }
    }
}
