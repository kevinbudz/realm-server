using RotMG.Common;
using RotMG.Networking;
using System.Collections.Generic;

namespace RotMG.Game
{
    //Per-world-type behavior, mirroring realm-src-master
    //wServer/realm/worlds/logic/{Nexus,Realm,Vault,GuildHall}.cs.
    public class NexusWorld : World
    {
        public PortalMonitor Monitor;

        public NexusWorld(JSMap map, WorldDesc desc) : base(map, desc)
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

        public RealmWorld(JSMap map, WorldDesc desc) : base(map, desc)
        {
            Overseer = new Oryx(this);
        }

        protected override void OnTick()
        {
            Overseer.Tick();
        }
    }

    public class VaultWorld : World
    {
        public int AccountId = -1;
        public bool VaultPopulated;

        public VaultWorld(JSMap map, WorldDesc desc) : base(map, desc) { }

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
                    AddEntity(new Entities.Vendors.ClosedVaultChest(vendorDesc.Type), at);
            }
        }
    }

    public class GuildHallWorld : World
    {
        public string GuildName;
        public int Level;

        public GuildHallWorld(JSMap map, WorldDesc desc) : base(map, desc) { }
    }
}
