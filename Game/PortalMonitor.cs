using RotMG.Game.Entities;
using System.Linq;

namespace RotMG.Game
{
    //Keeps the Nexus realm portal in sync with the realm lifecycle,
    //adapted from realm-src-master wServer/realm/PortalMonitor.cs.
    public class PortalMonitor
    {
        private readonly NexusWorld _nexus;

        public PortalMonitor(NexusWorld nexus)
        {
            _nexus = nexus;
        }

        public void Tick()
        {
            RealmWorld realm = Manager.GetWorld(Manager.RealmId) as RealmWorld;
            if (realm == null)
                return;

            Portal portal = _nexus.Statics.Values
                .OfType<Portal>()
                .FirstOrDefault(p => p.WorldInstance == realm);

            if (realm.Closed || realm.Closing)
            {
                if (portal != null)
                    RemovePortal(portal);
                return;
            }

            if (portal == null)
                Manager.PlacePortal(_nexus, "Realm Portal", realm);
        }

        private void RemovePortal(Portal portal)
        {
            Tile tile = _nexus.GetTile((int)portal.Position.X, (int)portal.Position.Y);
            _nexus.RemoveEntity(portal);
            if (tile != null && tile.StaticObject == portal)
            {
                tile.StaticObject = null;
                tile.BlocksSight = false;
                tile.UpdateCount++;
                _nexus.UpdateCount++;
            }
        }
    }
}
