using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game
{
    //Multi-portal registry for the Nexus, adapted from realm-src-master
    //wServer/realm/PortalMonitor.cs. Portals are tracked by destination
    //world id and never destroyed/recreated here: Open/ClosePortal only
    //flip Portal.Usable (the use handler already rejects !Usable), and
    //Tick renames each tracked portal to "<DisplayName> (<playerCount>)"
    //via the Name stat (Entity has no Name field; TrySetSV is the local
    //equivalent of the reference Portal.Name assignment).
    public class PortalMonitor
    {
        private readonly NexusWorld _nexus;
        private readonly Dictionary<int, Portal> _portals = new Dictionary<int, Portal>();

        public PortalMonitor(NexusWorld nexus)
        {
            _nexus = nexus;
        }

        //Random free tile in the Realm_Portals regions, falling back to
        //the spiral-around-spawn search from Manager.PlacePortal.
        private bool TryGetPortalSpot(out int x, out int y)
        {
            x = 0;
            y = 0;
            if (_nexus.Map.Regions.TryGetValue(Region.Realm_Portals, out List<IntPoint> spots))
                for (int attempt = 0; attempt < spots.Count; attempt++)
                {
                    IntPoint spot = spots[MathUtils.Next(spots.Count)];
                    Tile? tile = _nexus.GetTile(spot.X, spot.Y);
                    if (tile == null || tile.Value.StaticObject != null)
                        continue;
                    x = spot.X;
                    y = spot.Y;
                    return true;
                }
            return false;
        }

        private Portal PlaceTrackedPortal(World world)
        {
            if (TryGetPortalSpot(out int x, out int y))
            {
                //Indexed directly for the link: Tile is a struct (see World).
                Portal portal = new Portal(Resources.Id2Object["Realm Portal"].Type) { WorldInstance = world };
                if (_nexus.GetTile(x, y) != null && _nexus.AddEntity(portal, new Position(x + 0.5f, y + 0.5f)) != -1)
                {
                    _nexus.Tiles[x, y].StaticObject = portal;
                    _nexus.Tiles[x, y].UpdateCount++;
                    _nexus.UpdateCount++;
                    return portal;
                }
            }
            return Manager.PlacePortal(_nexus, "Realm Portal", world);
        }

        //Registers a portal for a world. An already-placed portal (e.g.
        //the realm portal created in Manager.Init) is tracked as-is;
        //otherwise a new one is created and placed.
        public bool AddPortal(int worldId, Portal portal = null)
        {
            if (_portals.ContainsKey(worldId))
                return false;
            World world = Manager.GetWorld(worldId);
            if (world == null)
                return false;
            if (portal == null)
            {
                portal = PlaceTrackedPortal(world);
                if (portal == null)
                    return false;
            }
            _portals[worldId] = portal;
            return true;
        }

        public bool RemovePortal(int worldId)
        {
            if (!_portals.TryGetValue(worldId, out Portal portal))
                return false;
            DestroyPortal(portal);
            _portals.Remove(worldId);
            return true;
        }

        public bool RemovePortal(Portal portal)
        {
            foreach (KeyValuePair<int, Portal> kv in _portals.ToArray())
                if (kv.Value == portal)
                    return RemovePortal(kv.Key);
            return false;
        }

        public bool RemovePortal(World world)
        {
            foreach (KeyValuePair<int, Portal> kv in _portals.ToArray())
                if (kv.Value.WorldInstance == world)
                    return RemovePortal(kv.Key);
            return false;
        }

        private void DestroyPortal(Portal portal)
        {
            //Read the link before RemoveEntity clears it; write through the
            //indexer after (Tile is a struct, see World). Non-null GetTile
            //proves (x, y) in-bounds for the writes below.
            int x = (int)portal.Position.X;
            int y = (int)portal.Position.Y;
            Tile? tile = _nexus.GetTile(x, y);
            bool linked = tile != null && tile.Value.StaticObject == portal;
            _nexus.RemoveEntity(portal);
            if (linked)
            {
                _nexus.Tiles[x, y].StaticObject = null;
                _nexus.Tiles[x, y].BlocksSight = false;
                _nexus.Tiles[x, y].UpdateCount++;
                _nexus.UpdateCount++;
            }
        }

        public void OpenPortal(int worldId)
        {
            if (_portals.TryGetValue(worldId, out Portal portal) && !portal.Usable)
                portal.Usable = true;
        }

        public void ClosePortal(int worldId)
        {
            if (_portals.TryGetValue(worldId, out Portal portal) && portal.Usable)
                portal.Usable = false;
        }

        //The reference also requires !Locked; local Portal has no lock.
        public bool PortalIsOpen(int worldId)
        {
            return _portals.TryGetValue(worldId, out Portal portal) && portal.Usable;
        }

        public void UpdateWorldInstance(int worldId, World world)
        {
            if (_portals.TryGetValue(worldId, out Portal portal))
                portal.WorldInstance = world;
        }

        public void Tick()
        {
            RealmWorld realm = Manager.GetWorld(Manager.RealmId) as RealmWorld;
            if (realm != null)
            {
                if (realm.Closed)
                    ClosePortal(Manager.RealmId);
                else
                    OpenPortal(Manager.RealmId);
            }

            foreach (KeyValuePair<int, Portal> kv in _portals.ToArray())
            {
                World world = kv.Value.WorldInstance ?? Manager.GetWorld(kv.Key);
                if (world == null)
                    continue;
                kv.Value.TrySetSV(StatType.Name, world.GetDisplayName() + " (" + world.Players.Count + ")");
            }
        }
    }
}
