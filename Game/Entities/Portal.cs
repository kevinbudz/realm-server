namespace RotMG.Game.Entities
{
    public class Portal : StaticObject
    {
        public bool Usable;
        public World WorldInstance;

        public Portal(ushort type) : base(type)
        {
            Usable = true;
        }

        public override bool HitByProjectile(Projectile projectile)
        {
            return false;
        }
    }
}
