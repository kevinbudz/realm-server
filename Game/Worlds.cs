using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Networking;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game
{
    //Per-world-type behavior, mirroring realm-src-master
    //wServer/realm/worlds/logic/{Nexus,Realm,Vault,GuildHall}.cs.
    public class NexusWorld : World
    {
        public PortalMonitor Monitor;

        public NexusWorld(IGameMap map, WorldDesc desc) : base(map, desc)
        {
            Monitor = new PortalMonitor(this);
        }

        protected override void OnTick()
        {
            Monitor.Tick();
        }
    }

    public class RealmWorld : World
    {
        public Oryx Overseer;
        public bool Closed;
        public bool Closing;

        public RealmWorld(IGameMap map, WorldDesc desc) : base(map, desc)
        {
            SBName = Oryx.GetRandomRealmName();
            Setpieces.SetPieces.ApplySetPieces(this);
            Overseer = new Oryx(this);
        }

        //The closing realm resets its map, so even admins cannot stay
        //(mirrors reference Realm.AllowedAccess). Client.Account has no
        //Admin flag in this codebase, so there is no bypass.
        public override bool AllowedAccess(Client client)
        {
            return !Closed;
        }

        protected override void OnTick()
        {
            Overseer.Tick();
        }

        protected override void OnPlayerEntered(Player player)
        {
            Overseer.OnPlayerEntered(player);
        }
    }

    public class VaultWorld : World
    {
        public int AccountId = -1;
        public bool VaultPopulated;

        public VaultWorld(IGameMap map, WorldDesc desc) : base(map, desc) { }

        //Spawns the account's persistent chests on Vault-region tiles (sorted
        //by distance to spawn) and ClosedVaultChest vendors on the leftovers,
        //mirroring realm-src-master wServer/realm/worlds/logic/Vault.cs.
        public void PopulateVault(Client client)
        {
            if (VaultPopulated)
                return;
            VaultPopulated = true;
            AccountId = client.Account.Id;

            if (!Map.Regions.TryGetValue(Region.Vault, out List<IntPoint> spots) || spots.Count == 0)
                return;

            IntPoint spawn = GetRegion(Region.Spawn);
            List<IntPoint> ordered = new List<IntPoint>(spots);
            ordered.Sort((a, b) =>
                ((a.X - spawn.X) * (a.X - spawn.X) + (a.Y - spawn.Y) * (a.Y - spawn.Y))
                .CompareTo((b.X - spawn.X) * (b.X - spawn.X) + (b.Y - spawn.Y) * (b.Y - spawn.Y)));

            if (!Resources.Id2Object.TryGetValue("Vault Chest", out ObjectDesc chestDesc) ||
                !Resources.Id2Object.TryGetValue("Closed Vault Chest", out ObjectDesc vendorDesc))
                return;

            int count = Database.GetVaultCount(client.Account);
            for (int i = 0; i < ordered.Count; i++)
            {
                IntPoint spot = ordered[i];
                Position at = new Position(spot.X + 0.5f, spot.Y + 0.5f);
                if (i < count)
                {
                    Entities.Container chest = new Entities.Container(chestDesc.Type)
                    {
                        VaultOwnerId = AccountId,
                        VaultIndex = i
                    };
                    Database.GetVaultItems(AccountId, i, chest.Inventory, chest.ItemDatas);
                    AddEntity(chest, at);
                }
                else
                {
                    //Static vendors only reach the client through their tile
                    //link (see Player.SendUpdate), so claim the tile exactly
                    //like portal placement does. Without this the vendor is
                    //invisible and Buy's RemoveStatic cannot find it.
                    Entities.Vendors.ClosedVaultChest vendor =
                        new Entities.Vendors.ClosedVaultChest(vendorDesc.Type);
                    if (AddEntity(vendor, at) != -1)
                    {
                        //Indexed directly: Tile is a struct (see World).
                        if (GetTile(spot.X, spot.Y) != null)
                        {
                            Tiles[spot.X, spot.Y].StaticObject = vendor;
                            Tiles[spot.X, spot.Y].UpdateCount++;
                            UpdateCount++;
                        }
                    }
                }
            }
        }
    }

    public class GuildHallWorld : World
    {
        public string GuildName;
        public int Level;

        public GuildHallWorld(IGameMap map, WorldDesc desc) : base(map, desc) { }
    }

    //Oryx's Castle siege instance, mirroring realm-src-master
    //wServer/realm/worlds/logic/Castle.cs. The reference ships two
    //same-signature constructors (one defaulting playersEntering to 100,
    //one forcing 0); this is a single constructor with an optional count
    //instead. Small raids share one spawn tile, large raids fan out.
    public class CastleWorld : World
    {
        public int PlayersEntering;

        public CastleWorld(IGameMap map, WorldDesc desc, int playersEntering = 0) : base(map, desc)
        {
            PlayersEntering = playersEntering;
        }

        public override List<IntPoint> GetSpawnPoints()
        {
            List<IntPoint> all = base.GetSpawnPoints();
            if (PlayersEntering < 20)
                return all.Take(1).ToList();
            else if (PlayersEntering < 40)
                return all.Take(2).ToList();
            else if (PlayersEntering < 60)
                return all.Take(3).ToList();
            return all;
        }
    }
}
