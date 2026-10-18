using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Entities
{
    //Two-phase ninja star ability, as betterskillys: hold to channel (Speedy + 10 MP/s drain),
    //release to throw and pay MpEndCost. The key-down and key-up arrive as UseItem useType
    //START_USE and END_USE.
    public partial class Player
    {
        //The key is held: START_USE has been accepted and END_USE has not.
        private bool _ninjaCharging;
        //The channel's Speedy is active. It ends when MP runs out, while the key stays held, so MP
        //can regenerate and END_USE can still throw (betterskillys removes NinjaSpeedy at MP 0).
        private bool _ninjaSpeedOn;
        private float _ninjaDrainCounter;

        public bool IsNinjaCharging => _ninjaCharging;

        //betterskillys blocks MP regeneration only while the channel's speed is active.
        public bool NinjaBlocksMPRegen => _ninjaSpeedOn;

        private void StartNinjaCharge()
        {
            ApplyConditionEffect(ConditionEffectIndex.Speedy, -1);
            _ninjaCharging = true;
            _ninjaSpeedOn = true;
            _ninjaDrainCounter = 0;
        }

        private void ReleaseNinjaCharge(int time, ItemDesc desc, SlotData slot, Position target)
        {
            RemoveConditionEffect(ConditionEffectIndex.Speedy);
            _ninjaCharging = false;
            _ninjaSpeedOn = false;
            _ninjaDrainCounter = 0;
            if (MP >= desc.MpEndCost)
            {
                FireShuriken(time, desc, slot, target);
                MP -= desc.MpEndCost;
            }
        }

        public void TickNinjaCharge()
        {
            if (!_ninjaCharging)
                return;

            if (Dead || Parent == null)
            {
                _ninjaCharging = false;
                _ninjaSpeedOn = false;
                _ninjaDrainCounter = 0;
                RemoveConditionEffect(ConditionEffectIndex.Speedy);
                return;
            }

            if (_ninjaSpeedOn && MP > 0)
            {
                _ninjaDrainCounter += 10f * Settings.SecondsPerTick;
                int drain = (int)_ninjaDrainCounter;
                if (drain > 0)
                {
                    MP = Math.Max(0, MP - drain);
                    _ninjaDrainCounter -= drain;
                    if (MP <= 0)
                    {
                        _ninjaSpeedOn = false;
                        RemoveConditionEffect(ConditionEffectIndex.Speedy);
                    }
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

                shots.Add(GameServer.ServerPlayerShoot(startId + i, Id, desc.Type, Position, a, 0, p).ToArray());
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
