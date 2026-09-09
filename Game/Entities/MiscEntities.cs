using RotMG.Networking;

using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;

namespace RotMG.Game.Entities
{
    //Informational sign; projectiles pass through, ported from
    //realm-src-master wServer/realm/entities/Sign.cs.
    public class Sign : StaticObject
    {
        public Sign(ushort type) : base(type) { }

        public override bool HitByProjectile(Projectile projectile)
        {
            return false;
        }
    }

    //Destructible wall with hit points from its descriptor, ported from
    //realm-src-master wServer/realm/entities/Wall.cs.
    public class Wall : StaticObject
    {
        public Wall(ushort type) : base(type)
        {
            HP = Desc.MaxHP;
            MaxHP = Desc.MaxHP;
        }

        public override bool HitByProjectile(Projectile projectile)
        {
            if (!(projectile.Owner is Player owner))
                return false;

            int damage = this.GetDefenseDamage(projectile.Damage, Desc.Defense, projectile.Desc.ArmorPiercing);
            HP -= damage;
            owner.FameStats.DamageDealt += damage;
            owner.FameStats.ShotsThatDamage++;

            byte[] packet = GameServer.Damage(Id, new ConditionEffectIndex[0], damage);
            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, Player.SightRadius))
                if (en is Player player && player.Client.Account.AllyDamage && !player.Equals(owner))
                    player.Client.Send(packet);

            if (HP <= 0)
            {
                Dead = true;
                Parent.RemoveStatic((int)Position.X, (int)Position.Y);
                return true;
            }
            return false;
        }
    }

    //Vanity pet bound to its owner, ported from
    //realm-src-master wServer/realm/entities/Pet.cs.
    public class Pet : Entity
    {
        public Player PlayerOwner;

        public Pet(ushort type, Player owner) : base(type)
        {
            PlayerOwner = owner;
        }
    }

    //Take-only container base, ported from
    //realm-src-master wServer/realm/entities/OneWayContainer.cs.
    public class OneWayContainer : Container
    {
        public OneWayContainer(ushort type) : base(type)
        {
        }

        public OneWayContainer(ushort type, int ownerId, int? lifetime) : base(type, ownerId, lifetime)
        {
        }
    }

    //Gift chest, ported from realm-src-master
    //wServer/realm/entities/GiftChest.cs.
    public class GiftChest : OneWayContainer
    {
        public GiftChest(ushort type) : base(type)
        {
        }

        public GiftChest(ushort type, int ownerId, int? lifetime) : base(type, ownerId, lifetime)
        {
        }
    }

    //Portal to a guild's hall instance, ported from realm-src-master
    //wServer/realm/entities/GuildHallPortal.cs.
    public class GuildHallPortal : Portal
    {
        public GuildHallPortal(ushort type) : base(type) { }
    }
}
