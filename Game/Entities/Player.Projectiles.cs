using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotMG.Game.Entities
{
    public class AoeAck
    {
        public int Damage;
        public ConditionEffectDesc[] Effects;
        public Position Position;
        public string Hitter;
        public float Radius;
        public int Time;
    }

    public struct ProjectileAck
    {
        public static ProjectileAck Undefined = new ProjectileAck
        {
            Projectile = null,
            Time = -1
        };

        public Projectile Projectile;
        public int Time;
        //Sweep cursor: last swept bullet age (client-clock ms since ack)
        //and the server tick it was taken at. Move sweeps anchor elapsed
        //to the client clock; tick sweeps advance it by server ms, so
        //standing players stay covered with no gaps or double-scans.
        public int Checked;
        public int LastSweep;

        public override bool Equals(object obj)
        {
#if DEBUG
            if (obj is null)
                throw new Exception("Undefined object");
#endif
            return (obj as ProjectileAck?).Value.Projectile.Id == Projectile.Id;
        }

        public override int GetHashCode()
        {
            return Projectile.Id;
        }
    }

    //One server-volley (or one player-nova volley) waiting for its
    //ShootAck. The wait runs on the server clock: Projectile.Time stays
    //on the client clock for hit math, and the two epochs differ
    //(client getTimer vs server uptime), so the timeout must not reuse
    //it. Forgiven tracks the one grace re-stamp a starved head gets
    //before it is resolved without its ack.
    public class AwaitingShots
    {
        public List<Projectile> Projectiles;
        public int EnqueueTime;
        public bool Forgiven;
    }

    public partial class Player
    {
        private const int TimeUntilAckTimeout = 2000;
        private const int TickProjectilesDelay = 2000;
        //Back-to-back starved volleys before the client is dropped for
        //stopped acking. Isolated losses (a lost packet, a slow frame)
        //resolve below without disconnecting; only sustained ack
        //suppression trips this. Reset by every received ShootAck.
        private const int MaxConsecutiveAckTimeouts = 5;
        private int _ackTimeoutStreak;
        private const float RateOfFireThreshold = 1.1f;
        private const float EnemyHitRangeAllowance = 1.7f;
        private const float EnemyHitTrackPrecision = 8;
        private const int EnemyHitHistoryBacktrack = 2;

        //Client-authoritative hits (skillys ForceHit model): the client
        //owns hit detection and the server applies what it reports. The
        //sweep below is observe-only: it records enemy bullets that pass
        //all but certainly through the player without the client ever
        //reporting a hit. Never deals damage, never consumes CanHit, so
        //it cannot kill or eat a legit hit. The box stays TIGHTER than
        //any real hitbox (server 0.4, client 0.5): a wider box flags
        //honest grazes the client correctly never reports. Each bullet
        //is recorded once and only counted if it lives out its full
        //lifetime unreported, so in-flight PlayerHit packets always win
        //the race and one slow bullet can never pile on many points.
        //Sustained definite misses mean a suppressing client.
        private const float VerifyHalfBox = 0.3f;
        private const int SuspicionThreshold = 15;
        private const int SuspicionWindowMs = 60000;

        private int _suspicion;
        private int _suspicionWindowStart;
        private readonly HashSet<int> _contactBullets = new HashSet<int>();
        //Reused VerifyProjectiles snapshots: iterating a scratch copy keeps
        //the exact remove-during-scan semantics of ToArray with no per-call
        //allocation (VerifyProjectiles runs on every Move packet).
        private readonly List<KeyValuePair<int, Projectile>> _shotVerifyScratch = new List<KeyValuePair<int, Projectile>>();
        private readonly List<KeyValuePair<int, ProjectileAck>> _ackVerifyScratch = new List<KeyValuePair<int, ProjectileAck>>();

        public Queue<AwaitingShots> AwaitingProjectiles;
        public Dictionary<int, ProjectileAck> AckedProjectiles;

        public Queue<AoeAck> AwaitingAoes; //Doesn't really belong here... But Player.Aoe.cs???

        public Dictionary<int, Projectile> ShotProjectiles;
        public int NextAEProjectileId = int.MinValue; //Goes up positively from bottom (Server sided projectiles)
        public int NextProjectileId; //Goes down negatively (Client sided projectiles)
        public int ShotTime;
        public int ShotDuration;

        public void TickProjectiles()
        {
            if (Manager.TotalTime % TickProjectilesDelay != 0)
                return;

            foreach (AoeAck aoe in AwaitingAoes)
            {
                if (Manager.TotalTime - aoe.Time > TimeUntilAckTimeout)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Aoe ack timed out");
#endif
                    Client.RequestDisconnect("Aoe ack timed out");
                    return;
                }
            }

            //Ack waits use the server clock (see AwaitingShots): the head
            //is the oldest waiter, so a fresh head means a fresh queue.
            //A starved head is forgiven once (a late ack still drains it
            //in order); a twice-starved head is resolved without its ack
            //and the streak only disconnects on sustained suppression.
            while (AwaitingProjectiles.Count > 0 &&
                Manager.TotalTime - AwaitingProjectiles.Peek().EnqueueTime > TimeUntilAckTimeout)
            {
                AwaitingShots head = AwaitingProjectiles.Peek();
                if (!head.Forgiven)
                {
#if DEBUG
                    Program.Print(PrintType.Warn, "Proj ack late, forgiven");
#endif
                    head.Forgiven = true;
                    head.EnqueueTime = Manager.TotalTime;
                    break;
                }

                AwaitingProjectiles.Dequeue();
                foreach (Projectile p in head.Projectiles)
                {
                    //Own nova volleys were definitely sent, so they stay
                    //hittable with their client fire time; enemy volleys
                    //the client never acked were never rendered, so they
                    //are dropped instead of made hittable.
                    if (p.Owner.Equals(this))
                        ShotProjectiles[p.Id] = p;
                }
#if DEBUG
                Program.Print(PrintType.Error, $"Proj ack wait expired ({head.Projectiles.Count} bullets, streak {_ackTimeoutStreak + 1})");
#endif
                if (++_ackTimeoutStreak >= MaxConsecutiveAckTimeouts)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Proj ack timed out");
#endif
                    Client.RequestDisconnect("Proj ack timed out");
                    return;
                }
            }

            //Acked-bullet expiry and suspicion recording for standing
            //players live in SweepAckedProjectilesTick (every tick,
            //unified clock cursor), not here: this only runs when
            //TotalTime % 2000 == 0.
        }

        public int GetNextDamageSeeded(int min, int max, int data)
        {
            float dmgMod = ItemDesc.GetStat(data, ItemData.Damage, ItemDesc.DamageMultiplier);
            int minDmg = min + (int)(min * dmgMod);
            int maxDmg = max + (int)(max * dmgMod);
            return (int)Client.Random.NextIntRange((uint)minDmg, (uint)maxDmg);
        }

        public int GetNextDamage(int min, int max, int data)
        {
            float dmgMod = ItemDesc.GetStat(data, ItemData.Damage, ItemDesc.DamageMultiplier);
            int minDmg = min + (int)(min * dmgMod);
            int maxDmg = max + (int)(max * dmgMod);
            return MathUtils.NextInt(minDmg, maxDmg);
        }

        public void TryHitEnemy(int time, int bulletId, int targetId)
        {
            if (Dead)
                return;

            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid time for enemy hit");
#endif
                Client.Disconnect();
                return;
            }

            if (ShotProjectiles.TryGetValue(bulletId, out Projectile p))
            {
                Entity target = Parent.GetEntity(targetId);
                if (target == null || !target.Desc.Enemy)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Invalid enemy target");
#endif
                    return;
                }
                int elapsed = time - p.Time;
                int steps = (int)Math.Ceiling((p.Desc.Speed / 100f) * ((elapsed * EnemyHitTrackPrecision) / 1000f));
                float timeStep = (float)elapsed / steps;

                for (int k = 0; k <= steps; k++)
                {
                    Position pos = p.PositionAt(k * timeStep);
                    if (k == steps) //Try hit enemy
                    {
                        if (target.Desc.Static)
                        {
                            if (pos.Distance(target.Position) <= EnemyHitRangeAllowance && p.CanHit(target))
                            {
                                target.HitByProjectile(p);
                                if (!p.Desc.MultiHit)
                                    ShotProjectiles.Remove(p.Id);
                                return;
                            }
                        }
                        else
                        {
                            for (int j = 0; j <= EnemyHitHistoryBacktrack; j++)
                            {
                                if (pos.Distance(target.TryGetHistory(j)) <= EnemyHitRangeAllowance && p.CanHit(target))
                                {
                                    target.HitByProjectile(p);
                                    if (!p.Desc.MultiHit)
                                        ShotProjectiles.Remove(p.Id);
                                    return;
                                }
                            }
                        }
#if DEBUG
                    Console.WriteLine(pos);
                    Console.WriteLine(target);
                    Program.Print(PrintType.Error, "Enemy hit aborted, too far away from projectile");
#endif
                    }
                    else //Check collisions to make sure player isn't shooting through walls etc
                    {
                        Tile? tile = Parent.GetTileF(pos.X, pos.Y);

                        if ((tile == null || tile.Value.Type == 255) ||
                            (tile.Value.StaticObject != null && !tile.Value.StaticObject.Desc.Enemy && (tile.Value.StaticObject.Desc.EnemyOccupySquare || !p.Desc.PassesCover && tile.Value.StaticObject.Desc.OccupySquare)))
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Shot projectile hit wall, removed");
#endif
                            ShotProjectiles.Remove(bulletId);
                            return;
                        }
                    }
                }
            }
#if DEBUG
            else
            {
                Program.Print(PrintType.Error, "Tried to hit enemy with undefined projectile");
            }
#endif
        }

        public void TryShoot(int time, Position pos, float attackAngle, bool ability, int numShots)
        {
            if (Dead)
                return;

            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid time for player shoot");
#endif
                Client.Disconnect();
                return;
            }

            if (AwaitingGoto.Count > 0)
            {
                Client.Random.Drop(numShots);
                return;
            }

            if (!ValidMove(time, pos))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid move for player shoot");
#endif
                Client.Disconnect();
                return;
            }

            //A NaN angle would bake NaN into projectile paths.
            if (!float.IsFinite(attackAngle))
            {
                Client.Random.Drop(numShots);
                return;
            }

            int startId = NextProjectileId;
            NextProjectileId -= numShots;

            ItemDesc desc = ability ? GetItem(1) : GetItem(0);
            if (desc == null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Undefined item descriptor");
#endif
                Client.Random.Drop(numShots);
                return;
            }


            if (numShots != desc.NumProjectiles)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Manipulated num shots");
#endif
                Client.Random.Drop(numShots);
                return;
            }

            if (HasConditionEffect(ConditionEffectIndex.Stunned))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Stunned...");
#endif
                Client.Random.Drop(numShots);
                return;
            }

            if (ability)
            {
                if (ShootAEs.TryDequeue(out ushort aeItemType))
                {
                    if (aeItemType != desc.Type)
                    {
                        Client.Random.Drop(numShots);
                        return;
                    }

                    float arcGap = (desc.ArcGap * MathUtils.ToRadians);
                    float totalArc = arcGap * (numShots - 1);
                    float angle = attackAngle - (totalArc / 2f);
                    for (int i = 0; i < numShots; i++)
                    {
                        int damage = (int)(GetNextDamageSeeded(desc.Projectile.MinDamage, desc.Projectile.MaxDamage, ItemDatas[1]) * GetAttackMultiplier());
                        Projectile projectile = new Projectile(this, desc.Projectile, startId - i, time, angle + (arcGap * i), pos, damage);
                        ShotProjectiles.Add(projectile.Id, projectile);
                    }

                    byte[] packet = GameServer.AllyShoot(Id, desc.Type, attackAngle);
                    foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
                        if (en is Player player && player.Client.Account.AllyShots && !player.Equals(this))
                            player.Client.Send(packet);

                    FameStats.Shots += numShots;
                }
                else
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Invalid ShootAE");
#endif
                    Client.Random.Drop(numShots);
                }
            }
            else
            {
                if (time > ShotTime + ShotDuration)
                {
                    float arcGap = (desc.ArcGap * MathUtils.ToRadians);
                    float totalArc = arcGap * (numShots - 1);
                    float angle = attackAngle - (totalArc / 2f);
                    for (int i = 0; i < numShots; i++)
                    {
                        int damage = (int)(GetNextDamageSeeded(desc.Projectile.MinDamage, desc.Projectile.MaxDamage, ItemDatas[0]) * GetAttackMultiplier());
                        Projectile projectile = new Projectile(this, desc.Projectile, startId - i, time, angle + (arcGap * i), pos, damage);
                        ShotProjectiles.Add(projectile.Id, projectile);
                    }

                    byte[] packet = GameServer.AllyShoot(Id, desc.Type, attackAngle);
                    foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
                        if (en is Player player && player.Client.Account.AllyShots && !player.Equals(this))
                            player.Client.Send(packet);

                    FameStats.Shots += numShots;
                    float rateOfFireMod = ItemDesc.GetStat(ItemDatas[0], ItemData.RateOfFire, ItemDesc.RateOfFireMultiplier);
                    float rateOfFire = desc.RateOfFire;
                    rateOfFire *= 1 + rateOfFireMod;
                    ShotDuration = (int)((1f / GetAttackFrequency() * (1f / rateOfFire)) * (1f / RateOfFireThreshold));
                    ShotTime = time;
                }

                else
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Shot too early, ignored");
#endif
                    Client.Random.Drop(numShots);
                }
            }
        }

        public void AwaitAoe(AoeAck aoe)
        {
            AwaitingAoes.Enqueue(aoe);
        }

        //Observe-only sweep, move-driven half: records every live enemy
        //bullet passing through the tight box over the exact interval
        //since it was last swept (bullet segment vs. the player's
        //validated movement segment, so fast bullets can't tunnel and
        //lagged moves still cover the whole travelled path). Counting
        //happens only at expiry (see ExpireAckedBullet). prevPlayerPos
        //is the server position before this move was applied.
        public void VerifyProjectiles(int time, Position prevPlayerPos)
        {
            if (Parent == null)
                return;

            _shotVerifyScratch.Clear();
            _shotVerifyScratch.AddRange(ShotProjectiles);
            foreach (KeyValuePair<int, Projectile> p in _shotVerifyScratch)
            {
                int elapsed = time - p.Value.Time;
                if (elapsed > p.Value.Desc.LifetimeMS)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Shot projectile removed");
#endif
                    ShotProjectiles.Remove(p.Key);
                    continue;
                }
            }

            //Bullets the client is immune to locally are never reported; skip.
            if (HasConditionEffect(ConditionEffectIndex.Invincible) ||
                HasConditionEffect(ConditionEffectIndex.Stasis))
                return;

            _ackVerifyScratch.Clear();
            _ackVerifyScratch.AddRange(AckedProjectiles);
            foreach (KeyValuePair<int, ProjectileAck> p in _ackVerifyScratch)
            {
                Projectile projectile = p.Value.Projectile;
                if (projectile?.Desc == null)
                {
                    AckedProjectiles.Remove(p.Key);
                    _contactBullets.Remove(p.Key);
                    continue;
                }
                ProjectileAck ack = p.Value;
                int elapsed = Math.Max(0, time - ack.Time);
                if (elapsed > projectile.Desc.LifetimeMS)
                {
                    ExpireAckedBullet(p.Key, projectile, elapsed);
                    continue;
                }

                int start = Math.Min(Math.Max(0, ack.Checked), elapsed);
                SweepAckedInterval(p.Key, projectile, start, elapsed, prevPlayerPos);

                if (AckedProjectiles.ContainsKey(p.Key))
                {
                    ack.Checked = elapsed;
                    ack.LastSweep = Manager.TotalTime;
                    AckedProjectiles[p.Key] = ack;
                }
            }
        }

        //Tick-driven twin of VerifyProjectiles for players who stand
        //still (no Move packets): advances each bullet by server ms since
        //its last sweep, so recording and expiry have no gaps. Skips the
        //same windows the move path skips (teleport, immunity).
        public void SweepAckedProjectilesTick()
        {
            if (Parent == null || AckedProjectiles.Count == 0)
                return;
            if (AwaitingGoto.Count > 0)
                return;
            if (HasConditionEffect(ConditionEffectIndex.Invincible) ||
                HasConditionEffect(ConditionEffectIndex.Stasis))
                return;

            _ackVerifyScratch.Clear();
            _ackVerifyScratch.AddRange(AckedProjectiles);
            foreach (KeyValuePair<int, ProjectileAck> p in _ackVerifyScratch)
            {
                Projectile projectile = p.Value.Projectile;
                if (projectile?.Desc == null)
                {
                    AckedProjectiles.Remove(p.Key);
                    _contactBullets.Remove(p.Key);
                    continue;
                }
                ProjectileAck ack = p.Value;
                int elapsed = Math.Max(0, ack.Checked + (Manager.TotalTime - ack.LastSweep));
                if (elapsed > projectile.Desc.LifetimeMS)
                {
                    ExpireAckedBullet(p.Key, projectile, elapsed);
                    continue;
                }

                int start = Math.Min(Math.Max(0, ack.Checked), elapsed);
                SweepAckedInterval(p.Key, projectile, start, elapsed, Position);

                if (AckedProjectiles.ContainsKey(p.Key))
                {
                    ack.Checked = elapsed;
                    ack.LastSweep = Manager.TotalTime;
                    AckedProjectiles[p.Key] = ack;
                }
            }
        }

        //Observe-only contact test over [startElapsed, elapsed]: records
        //bullets passing through the tight box for counting at expiry
        //(see ExpireAckedBullet), or drops wall-killed bullets. Never
        //deals damage, never consumes CanHit. Removing the bullet (wall
        //death) is signalled by its absence from AckedProjectiles;
        //callers must re-check before advancing the cursor.
        private void SweepAckedInterval(int bulletId, Projectile projectile, int startElapsed, int elapsed, Position prevPlayerPos)
        {
            Position prev = projectile.PositionAt(startElapsed);
            Position pos = projectile.PositionAt(elapsed);
            Position mid = projectile.PositionAt((startElapsed + elapsed) / 2f);
            if (ProjectileBlockedAt(projectile, pos))
            {
                //Died in a wall: clients delete these too. No suspicion,
                //bullet is gone.
                AckedProjectiles.Remove(bulletId);
                _contactBullets.Remove(bulletId);
                return;
            }
            if (ProjectileBlockedAt(projectile, prev) || ProjectileBlockedAt(projectile, mid))
            {
                //Wall/cover-adjacent segment diverges client vs server by
                //design (the client deletes on walls); withdrawing here
                //favours false negatives over false positives. Bullet stays
                //live for the next interval.
                _contactBullets.Remove(bulletId);
                return;
            }

            //Record, don't punish: the bullet only counts if it lives out
            //its full lifetime without ever being reported (see
            //ExpireAckedBullet), so legit in-flight hits never register
            //and each bullet counts at most once.
            if (SegmentDistSquared(prev, pos, Position) <= VerifyHalfBox * VerifyHalfBox)
                _contactBullets.Add(bulletId);
        }

        //A bullet's reporting window closes when it expires: a PlayerHit for
        //it can no longer be in flight, so a recorded point-blank contact
        //that stayed unreported through the full lifetime is a genuine miss,
        //not a packet race.
        private void ExpireAckedBullet(int bulletId, Projectile projectile, int elapsed)
        {
#if DEBUG
            Program.Print(PrintType.Error, "Acked projectile expired");
#endif
            AckedProjectiles.Remove(bulletId);
            if (projectile != null && _contactBullets.Remove(bulletId) && !projectile.Hit.Contains(Id))
                FlagMissedBullet(bulletId, elapsed);
        }

        private bool ProjectileBlockedAt(Projectile projectile, Position pos)
        {
            //Defensive: never dereference a nulled world or descriptor here.
            if (Parent == null || projectile?.Desc == null)
                return true;
            Tile? tile = Parent.GetTileF(pos.X, pos.Y);
            return (tile == null || tile.Value.Type == 255) ||
                (tile.Value.StaticObject != null && !tile.Value.StaticObject.Desc.Enemy &&
                 (tile.Value.StaticObject.Desc.EnemyOccupySquare ||
                  (!projectile.Desc.PassesCover && tile.Value.StaticObject.Desc.OccupySquare)));
        }

        private static float SegmentDistSquared(Position a, Position b, Position p)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float lenSq = dx * dx + dy * dy;
            float t = lenSq <= 0 ? 0 : ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
            t = Math.Max(0, Math.Min(1, t));
            float cx = a.X + t * dx - p.X;
            float cy = a.Y + t * dy - p.Y;
            return cx * cx + cy * cy;
        }

        private void FlagMissedBullet(int bulletId, int elapsed)
        {
            int now = Manager.TotalTime;
            if (now - _suspicionWindowStart > SuspicionWindowMs)
            {
                _suspicion = 0;
                _suspicionWindowStart = now;
            }

            if (++_suspicion >= SuspicionThreshold)
            {
                Program.Print(PrintType.Error, $"Suppressed enemy hits suspected <{Name}> (bullet {bulletId}, elapsed {elapsed})");
                Client.RequestDisconnect("Suppressed enemy hits");
            }
#if DEBUG
            else
            {
                Program.Print(PrintType.Error, $"Unreported projectile contact <{Name}> (bullet {bulletId}, suspicion {_suspicion})");
            }
#endif
        }

        //Client-authoritative hit (skillys ForceHit model): the client
        //owns detection, so every report for a known, hittable bullet
        //lands. Deliberately no range/time denial — PlayerHit carries no
        //timestamp and clocks may be stale, so any plausibility check
        //only risks dropping legit hits, while forged self-hits merely
        //harm the forger. Suppression (never reporting) is policed by
        //suspicion counting at expiry, not by doubting reports.
        public void TryHit(int bulletId)
        {
            if (Dead || IsTransferring)
                return;

            if (AckedProjectiles.TryGetValue(bulletId, out ProjectileAck v))
            {
                if (v.Projectile?.Desc != null && v.Projectile.CanHit(this))
                {
                    bool died = HitByProjectile(v.Projectile);
#if DEBUG
                    Program.Print(PrintType.Error, $"Applied enemy hit <{Name}> bullet {bulletId} via PlayerHit killed={died}");
#endif
                    AckedProjectiles.Remove(bulletId);
                    _contactBullets.Remove(bulletId);
                }
                //Known bullet but not currently hittable (immune or
                //already hit): leave it live for expiry.
            }
#if DEBUG
            else
            {
                Program.Print(PrintType.Error, "Tried to hit with undefined projectile");
            }
#endif
        }

        public override bool HitByProjectile(Projectile projectile)
        {
            return Damage(Resources.Type2Object[projectile.Desc.ContainerType].DisplayId,
                   projectile.Damage, 
                   projectile.Desc.Effects, 
                   projectile.Desc.ArmorPiercing);
        }

        public void AwaitProjectiles(List<Projectile> projectiles)
        {
            AwaitingProjectiles.Enqueue(new AwaitingShots { Projectiles = projectiles, EnqueueTime = Manager.TotalTime });
        }

        public void TryHitSquare(int time, int bulletId)
        {
            if (Dead)
                return;

            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "HitSquare invalid time");
#endif
                Client.Disconnect();
                return;
            }

            if (AckedProjectiles.TryGetValue(bulletId, out ProjectileAck ac))
            {
                Position pos = ac.Projectile.PositionAt(time - ac.Time);
                Tile? tile = Parent.GetTileF(pos.X, pos.Y);

                if ((tile == null || tile.Value.Type == 255 || GetSeenTileUpdate((int)pos.X, (int)pos.Y) != tile.Value.UpdateCount) ||
                    (tile.Value.StaticObject != null && (tile.Value.StaticObject.Desc.EnemyOccupySquare || !ac.Projectile.Desc.PassesCover && tile.Value.StaticObject.Desc.OccupySquare)))
                {
                    AckedProjectiles.Remove(bulletId);
                    _contactBullets.Remove(bulletId);
                }
#if DEBUG
                else
                {
                    Program.Print(PrintType.Error, "Manipualted SquareHit?");
                }
#endif
            }
#if DEBUG
            else
            {
                Program.Print(PrintType.Error, "Tried to hit square with undefined projectile");
            }
#endif
        }

        public void TryAckAoe(int time, Position pos)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "AoeAck invalid time");
#endif
                Client.Disconnect();
                return;
            }

            if (AwaitingAoes.TryDequeue(out AoeAck aoe))
            {
                if (!ValidMove(time, pos) && AwaitingGoto.Count == 0)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "INVALID MOVE FOR AOEACK!");
#endif
                    Client.Disconnect();
                    return;
                }

                if (pos.Distance(aoe.Position) < aoe.Radius && !HasConditionEffect(ConditionEffectIndex.Invincible))
                {
                    Damage(aoe.Hitter, aoe.Damage, aoe.Effects, false);
                }
            }
            else
            {
#if DEBUG
                Program.Print(PrintType.Error, "AoeAck desync");
#endif
                Client.Disconnect();
            }
        }

        public void TryShootAck(int time)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "ShootAck invalid time");
#endif
                Client.Disconnect();
                return;
            }

            if (AwaitingProjectiles.TryDequeue(out AwaitingShots awaiting))
            {
                _ackTimeoutStreak = 0;
                foreach (Projectile p in awaiting.Projectiles)
                {
                    if (p.Owner.Equals(this))
                    {
                        p.Time = time;
                        ShotProjectiles[p.Id] = p;
                    }
                    else
                    {
#if DEBUG
                        if (AckedProjectiles.ContainsKey(p.Id))
                        {
                            Program.Print(PrintType.Warn, "Duplicate ack key");
                        }
#endif
                        ProjectileAck ack = new ProjectileAck { Projectile = p, Time = time, Checked = 0, LastSweep = Manager.TotalTime };
                        AckedProjectiles[p.Id] = ack;
                    }
                }
            }
            else
            {
#if DEBUG
                Program.Print(PrintType.Error, "ShootAck desync");
#endif
                Client.Disconnect();
            }
        }
    }
}
