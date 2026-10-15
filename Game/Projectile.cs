using RotMG.Common;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game
{
    public class Projectile
    {
        public readonly Entity Owner;
        public readonly ProjectileDesc Desc;
        public readonly int Id;
        public readonly float Angle;
        public readonly Position StartPosition;
        public readonly int Damage;
        public readonly HashSet<int> Hit;

        public int Time;

        public Projectile(Entity owner, ProjectileDesc desc, int id, int time, float angle, Position startPos, int damage)
        {
            Owner = owner;
            Desc = desc;
            Id = id;
            Time = time;
            Angle = MathUtils.BoundToPI(angle);
            StartPosition = startPos;
            Damage = damage;
            Hit = new HashSet<int>();
        }

        public bool CanHit(Entity en)
        {
            if (en.HasConditionEffect(ConditionEffectIndex.Invincible) || en.HasConditionEffect(ConditionEffectIndex.Stasis))
                return false;

            if (!Hit.Contains(en.Id))
            {
                Hit.Add(en.Id);
                return true;
            }
            return false;
        }

        //Closed form (see ProjectilePath): one exact evaluation per call.
        public Position PositionAt(double elapsed)
        {
            return ProjectilePath.PositionAt(Desc, StartPosition, Angle, Id, elapsed);
        }
    }
}
