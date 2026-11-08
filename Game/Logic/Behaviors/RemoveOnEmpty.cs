using RotMG.Common;
using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Behaviors
{
    public class RemoveOnEmptyState
    {
        public bool HadItems;
    }

    //Hand-written toolkit: not emitted by Tools/XmlBehaviorConverter (files/xml has no equivalent).
    //Port of the reference RemoveOnEmptyBehavior: removes a container once its inventory is empty.
    //Deviation: a container that never held items is kept. Setpiece Treasure Chests are created empty in this port
    //(no static-chest loot roller), so a plain empty check would delete them on the first tick.
    //Non-container hosts are never removed.
    public class RemoveOnEmpty : Behavior
    {
        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new RemoveOnEmptyState();
        }

        public override bool Tick(Entity host)
        {
            if (!(host is Container container) || container.Inventory == null)
                return false;
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is RemoveOnEmptyState state))
                return false;

            foreach (int slot in container.Inventory)
            {
                if (slot != -1)
                {
                    state.HadItems = true;
                    return false;
                }
            }
            if (!state.HadItems || host.Parent == null)
                return false;

            host.Dead = true;
            host.Parent.RemoveEntity(host);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }
    }
}
