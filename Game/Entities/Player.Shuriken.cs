using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Entities
{
    //Fabianos-style ninja star ability: hold to channel (Speedy + 10 MP/s
    //drain), release to throw. Ported from fabianos
    //wServer/realm/entities/player/Player.UseItem.cs (ShurikenAbility case)
    //and Player.Boosts.cs (HandleBoosts MP drain).
    public partial class Player
    {
        private bool _ninjaCharging;
        private float _ninjaDrainCounter;

        public bool IsNinjaCharging => _ninjaCharging;

        public void TickNinjaCharge()
        {
            if (!_ninjaCharging)
                return;

            if (Dead || Parent == null)
            {
                _ninjaCharging = false;
                _ninjaDrainCounter = 0;
                RemoveConditionEffect(ConditionEffectIndex.Speedy);
                return;
            }

            if (MP > 0)
            {
                _ninjaDrainCounter += 10f * Settings.SecondsPerTick;
                int drain = (int)_ninjaDrainCounter;
                if (drain > 0)
                {
                    MP = Math.Max(0, MP - drain);
                    _ninjaDrainCounter -= drain;
                    if (MP <= 0)
                        RemoveConditionEffect(ConditionEffectIndex.Speedy);
                }
            }
        }

        private void FireShuriken(int time, ItemDesc desc, SlotData slot, Position target)
        {
            if (desc.Projectile == null)
                return;

            float arcGap = desc.ArcGap * MathF.PI / 180;
            float startAngle = Position.Angle(target) - (desc.NumProjectiles - 1) / 2f * arcGap;

            List<byte[]> shots = new List<byte[]>();
            int startId = NextAEProjectileId;
            NextAEProjectileId += desc.NumProjectiles;

            for (int i = 0; i < desc.NumProjectiles; i++)
            {
                float a = startAngle + arcGap * i;
                int d = GetNextDamage(desc.Projectile.MinDamage, desc.Projectile.MaxDamage, ItemDatas[slot.SlotId]);
                List<Projectile> p = new List<Projectile>()
                {
                    new Projectile(this, desc.Projectile, startId + i, time, a, Position, d)
                };

                shots.Add(GameServer.ServerPlayerShoot(startId + i, Id, desc.Type, Position, a, 0, p));
                AwaitProjectiles(p);
            }

            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
            {
                if (j is Player k)
                {
                    if (k.Client.Account.AllyShots || k.Equals(this))
                        foreach (byte[] s in shots)
                            k.Client.Send(s);
                }
            }
        }
    }
}
