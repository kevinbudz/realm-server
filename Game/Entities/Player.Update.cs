using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        public const int SightRadius = 15;
        //realm-src-master parity (Player.Update StaticBoundingBox):
        //revealed tile statics persist to twice the sight radius instead
        //of dropping the moment the sight circle breaks.
        private const int StaticRetentionRadius = SightRadius * 2;
        private const float StartAngle = 0;
        private const float EndAngle = (float)(2 * Math.PI);
        private const float RayStepSize = .05f;
        private const float AngleStepSize = 2.30f / (SightRadius * 2);

        private static readonly IntPoint[] SurroundingPoints = new IntPoint[]
        {
            new IntPoint(1, 0),
            new IntPoint(1, 1),
            new IntPoint(0, 1),
            new IntPoint(-1, 1),
            new IntPoint(-1, 0),
            new IntPoint(-1, -1),
            new IntPoint(0, -1),
            new IntPoint(1, -1)
        };

        private static HashSet<IntPoint> SightCircle;
        public static void InitSightCircle()
        {
            SightCircle = new HashSet<IntPoint>();
            for (int x = -SightRadius; x <= SightRadius; x++)
                for (int y = -SightRadius; y <= SightRadius; y++)
                    if (x * x + y * y <= SightRadius * SightRadius)
                        SightCircle.Add(new IntPoint(x, y));
        }

        private static HashSet<IntPoint>[] SightRays;
        public static void InitSightRays()
        {
            List<HashSet<IntPoint>> sightRays = new List<HashSet<IntPoint>>();

            float currentAngle = StartAngle;
            while (currentAngle < EndAngle)
            {
                HashSet<IntPoint> ray = new HashSet<IntPoint>();
                float dist = RayStepSize;
                while (dist < (SightRadius + 25))
                {
                    IntPoint point = new IntPoint(
                        (int)(dist * Math.Cos(currentAngle)),
                        (int)(dist * Math.Sin(currentAngle)));

                    if (SightCircle.Contains(point))
                        ray.Add(point);
                    dist += RayStepSize;
                }
                sightRays.Add(ray);
                currentAngle += AngleStepSize;
            }

            SightRays = sightRays.ToArray();
        }

        //[W08] Tiles this client was sent at their current UpdateCount: one bit per world tile (TileSeenBits), sized to
        //the world at Init and never grown. It replaces the Dictionary of last-sent versions, which grew with every tile
        //revealed and was never pruned. Null before Init, after Dispose, and for bots (they never send, so they hold none).
        private TileSeenBits _tileSent;
        //True when this client was sent the tile at its current version. Same question as the old per-tile version compare.
        internal bool TileSeen(int x, int y) => _tileSent != null && _tileSent.Get(x, y);
        //Called by World.BumpTile for every player in the world when a tile's version changes.
        internal void ForgetTile(int x, int y) => _tileSent?.Clear(x, y);
        //The W05 teleport check marks a tile as seen at a given version. The bit is set only when v is the tile's current
        //version, which is what the old dictionary held right after the sight loop sent it.
        private void SetSeenTileUpdate(int x, int y, int v)
        {
            Tile? tile = Parent.GetTile(x, y);
            if (tile != null && tile.Value.UpdateCount == v)
                _tileSent.Set(x, y);
            else
                _tileSent.Clear(x, y);
        }
        public Dictionary<int, int> EntityUpdates;
        public HashSet<Entity> Entities;
        public HashSet<IntPoint> CalculatedSightCircle;
        //Presence tier for far players (minimap/teleport): tracked world-wide
        //like realm-src, refreshed at a reduced rate. 5 tps / 4 ~= 1.25 Hz
        //dot movement, enough for the minimap while bounding NewTick fan-out.
        private const int FarPlayerTickDivisor = 2;
        private int _newTickCount;
        //Ids of tracked players inside sight on the last SendUpdate. Rebuilt
        //there (pre-move) and read by SendNewTick (post-move); sub-tick
        //staleness is irrelevant for throttle bucketing.
        private readonly HashSet<int> _nearPlayerIds = new HashSet<int>();
        //Far-presence watermark: which world's player roster this client
        //already knows. Matches World.PlayerListVersion; see SendUpdate.
        private World _knownPlayerWorld;
        private int _knownPlayerVersion = -1;
        private readonly List<Entity> _hitTestScratch = new List<Entity>();
        private readonly List<Entity> _dropScratch = new List<Entity>();
        //Reused per-tick packet buffers: Update/NewTick serialize
        //synchronously, so these never escape the call.
        //[W08] Inside a broadcast phase these hold the entity bytes shared with the other receivers (Entity.SharedStatus).
        private readonly List<StatusEntry> _statusScratch = new List<StatusEntry>();
        private readonly List<TileData> _tilesScratch = new List<TileData>();
        private readonly List<AddEntry> _addsScratch = new List<AddEntry>();
        private readonly List<ObjectDrop> _dropsScratch = new List<ObjectDrop>();

        //Join frame (W04): the joiner's first Update and NewTick, sent when Load/Create adds the player
        //instead of at the next world tick (up to 100 ms later). It runs through the same diff state a tick
        //uses, so the next tick sends only what changed and nothing twice. SendNewTick credits AwaitingMoves
        //as on every tick. That keeps the client pairing: realm-cpp counts one pending Move per NewTick it
        //receives (Session.cpp NEWTICK, finish_frame). ClientTimeGate only sees Move times, so the extra
        //NewTick is not a drift event.
        public void SendJoinFrame()
        {
            if (Parent == null || Client == null) //AddEntity returns -1 without placing the player
                return;
            SendUpdate();
            SendNewTick();
        }

        //Phase gen for the shared serialized statuses: the world's broadcast phase when called from Tick, zero otherwise.
        //Zero (a direct call, such as the join frame) serializes each status into the packet, with no cache.
        private long WireGen()
        {
            return Parent?.BroadcastGen ?? 0;
        }

        private static StatusEntry StatusOf(Entity en, bool form, long gen)
        {
            return gen != 0 ? StatusEntry.FromShared(en.SharedStatus(form, gen)) : StatusEntry.Direct(en.GetObjectStatus(form));
        }

        public virtual void SendNewTick()
        {
            HandleQuest();
            _newTickCount++;
            long gen = WireGen();
            _statusScratch.Clear();
            foreach (Entity en in Entities)
            {
                if (EntityUpdates[en.Id] != en.UpdateCount)
                {
                    //[W08] Same forms as before: players send Full, other entities Delta (GetObjectStatus(true)).
                    //The bytes are built once per phase and shared by every receiver.
                    if (en is Player && en != this && !_nearPlayerIds.Contains(en.Id))
                    {
                        //Far presence: round-robin slice only. Full stats keep
                        //every send self-contained, so no delta is ever lost
                        //across skipped ticks (SVs.Dirty is cleared globally).
                        if ((_newTickCount + en.Id) % FarPlayerTickDivisor != 0)
                            continue;
                        _statusScratch.Add(StatusOf(en, false, gen));
                    }
                    else if (en is Player)
                        _statusScratch.Add(StatusOf(en, false, gen));
                    else
                        _statusScratch.Add(StatusOf(en, true, gen));
                    EntityUpdates[en.Id] = en.UpdateCount;
                }
            }

            Client.Send(GameServer.NewTickShared(_statusScratch, PrivateSVs));
            PrivateSVs.Clear();
            AwaitingMoves = CreditNewTick(AwaitingMoves);
            ReleaseDirectWire();
        }

        //Full status for an add (shared bytes inside a phase). GetObjectDefinition runs first, as before: it can change the
        //entity (ConnectedObject sets its Connect stat there), and the status must be taken after that.
        private void AddShared(Entity en, long gen)
        {
            ObjectDefinition def = en.GetObjectDefinition();
            _addsScratch.Add(new AddEntry
            {
                ObjectType = def.ObjectType,
                Status = gen != 0 ? StatusEntry.FromShared(en.SharedStatus(false, gen)) : StatusEntry.Direct(def.ObjectStatus)
            });
        }

        public virtual void SendUpdate()
        {
            long gen = WireGen();
            long trSight = Manager.TickTraceEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            bool nUpdate = ShouldCalculateSightCircle();
            HashSet<IntPoint> sight = Parent.BlockSight == 0 ? SightCircle :
                    nUpdate ? CalculateSightCircle() : CalculatedSightCircle;
            if (Manager.TickTraceEnabled)
            {
                System.Threading.Interlocked.Add(ref Parent.TraceSightTicks, System.Diagnostics.Stopwatch.GetTimestamp() - trSight);
                if (nUpdate)
                    System.Threading.Interlocked.Increment(ref Parent.TraceSightCalcs);
            }

            _tilesScratch.Clear();
            _addsScratch.Clear();
            _dropsScratch.Clear();

            if (nUpdate)
            {
                //Tiles and statics in one pass (one GetTile per sight
                //point instead of two). The old second loop re-checked
                //seen == UpdateCount after the first loop had synced every
                //dirty tile, so its condition was always true: every
                //in-sight static is covered below.
                foreach (IntPoint p in sight)
                {
                    int x = p.X + (int)Position.X;
                    int y = p.Y + (int)Position.Y;
                    Tile? tile = Parent.GetTile(x, y);

                    if (tile == null)
                        continue;

                    if (!TileSeen(x, y))
                    {
                        _tilesScratch.Add(new TileData
                        {
                            TileType = tile.Value.Type,
                            X = (short)x,
                            Y = (short)y
                        });

                        _tileSent.Set(x, y);
                    }

                    if (tile.Value.StaticObject != null && Entities.Add(tile.Value.StaticObject))
                    {
                        AddShared(tile.Value.StaticObject, gen);
                        EntityUpdates.Add(tile.Value.StaticObject.Id, tile.Value.StaticObject.UpdateCount);
                    }
                }
            }

            //Near players (chunk-routed, sight-gated; self passes at 0,0 and
            //must stay tracked for NewTick). Records the near set for the
            //SendNewTick throttle below.
            _nearPlayerIds.Clear();
            _nearPlayerIds.Add(Id);
            Parent.PlayerChunks.HitTest(Position, SightRadius, _hitTestScratch);
            foreach (Entity en in _hitTestScratch)
            {
                int dx = (int)en.Position.X - (int)Position.X;
                int dy = (int)en.Position.Y - (int)Position.Y;
                if (!sight.Contains(new IntPoint(dx, dy)))
                    continue;

                _nearPlayerIds.Add(en.Id);
                if (Entities.Add(en))
                {
                    AddShared(en, gen);
                    EntityUpdates.Add(en.Id, en.UpdateCount);
                }
            }

            //Far presence: every world player stays known (realm-src parity)
            //so minimap dots and teleport clicks work at any distance. Gated
            //on World.PlayerListVersion, which bumps on join/leave: the full
            //roster scan runs once per membership change, not per-tick
            //per-player. Afterwards Entities.Add is a no-op hit and drops are
            //handled by the removal pass below, so the known set cannot leak.
            //Near ids come from the chunk query above every tick; its radius
            //math covers sight exactly, so no full-scan top-up is needed.
            if (_knownPlayerWorld != Parent || _knownPlayerVersion != Parent.PlayerListVersion)
            {
                foreach (Player p in Parent.Players.Values)
                {
                    if (Entities.Add(p))
                    {
                        AddShared(p, gen);
                        EntityUpdates.Add(p.Id, p.UpdateCount);
                    }
                }
                _knownPlayerWorld = Parent;
                _knownPlayerVersion = Parent.PlayerListVersion;
            }

            //Add entities
            Parent.EntityChunks.HitTest(Position, SightRadius, _hitTestScratch);
            foreach (Entity en in _hitTestScratch)
            {
                Container container = en as Container;
                if (container != null && container.OwnerId != -1 && container.OwnerId != Id)
                    continue;

                int dx = (int)en.Position.X - (int)Position.X;
                int dy = (int)en.Position.Y - (int)Position.Y;
                if (sight.Contains(new IntPoint(dx, dy)) && Entities.Add(en))
                {
                    AddShared(en, gen);
                    EntityUpdates.Add(en.Id, en.UpdateCount);
                }
            }

            //The quest target is always known to the client, even far out
            //of sight: the QuestArrow needs its GameObject to point at it.
            //Mirrors realm-src-master Player.Update GetNewEntities, which
            //yields questEntity unconditionally.
            if (Quest != null && Quest.Parent == Parent && Entities.Add(Quest))
            {
                AddShared(Quest, gen);
                EntityUpdates.Add(Quest.Id, Quest.UpdateCount);
            }

            //Remove entities and statics (as they end up in the same Entities dictionary
            _dropScratch.Clear();
            foreach (Entity en in Entities)
            {
                if (en == this)
                    continue;

                //Players persist world-wide for minimap/teleport (mirrors
                //realm-src-master GetRemovedEntities skipping players); only
                //a gone player (Parent == null) falls through to the drop.
                //Far ones are throttled in SendNewTick instead of dropped.
                if (en is Player)
                {
                    if (en.Parent != null)
                        continue;
                }
                else if (en is StaticObject staticObj)
                {
                    //Sticky retention like realm-src-master
                    //GetRemovedStatics: a revealed tile static (wall,
                    //portal, deco) stays known while its tile still hosts
                    //it and it remains within the retention box, even when
                    //occluded or outside the sight circle. Without this,
                    //rooms revealed by breaking destructible walls vanish
                    //as soon as the sight line breaks.
                    if (en.Parent != null && IsRetainedStatic(staticObj))
                        continue;
                }
                else
                {
                    //The live quest target is never dropped (mirrors
                    //realm-src-master GetRemovedEntities keeping questEntity).
                    //The liveness gate matters: a dead quest (Parent == null)
                    //must fall through to the drop below, exactly like the
                    //reference drops Owner == null before its quest check.
                    if (en == Quest && Quest.Parent != null)
                        continue;

                    if (en.Parent != null)
                    {
                        int dx = (int)en.Position.X - (int)Position.X;
                        int dy = (int)en.Position.Y - (int)Position.Y;
                        if (sight.Contains(new IntPoint(dx, dy)))
                            continue;
                    }
                }

                _dropsScratch.Add(en.GetObjectDrop());
                EntityUpdates.Remove(en.Id);
                _nearPlayerIds.Remove(en.Id);
                _dropScratch.Add(en);
            }

            foreach (Entity en in _dropScratch)
                Entities.Remove(en);

            if (_tilesScratch.Count > 0 || _addsScratch.Count > 0 || _dropsScratch.Count > 0)
            {
                Client.Send(GameServer.UpdateShared(_tilesScratch, _addsScratch, _dropsScratch));
                FameStats.TilesUncovered += _tilesScratch.Count;
            }
            ReleaseDirectWire();
        }

        //[W08] A direct SendUpdate/SendNewTick (the join frame) owns the serialized statuses it filled: they are
        //released once its packet is sent. Inside a broadcast phase the phase releases them instead.
        private void ReleaseDirectWire()
        {
            World world = Parent;
            if (world != null && world.BroadcastGen == 0)
                world.ReleaseWireBlobs();
        }

        //Retention box is symmetric on both axes. realm-src-master's
        //equivalent check is accidentally one-sided (no Abs on one path);
        //squaring it here so statics drop at the same distance in every
        //direction instead of lingering east/south.
        private bool IsRetainedStatic(StaticObject staticObj)
        {
            if (Math.Abs(staticObj.Position.X - Position.X) > StaticRetentionRadius ||
                Math.Abs(staticObj.Position.Y - Position.Y) > StaticRetentionRadius)
                return false;
            Tile? tile = Parent.GetTileF(staticObj.Position.X, staticObj.Position.Y);
            return tile != null && ReferenceEquals(tile.Value.StaticObject, staticObj);
        }

        IntPoint _p; int _w;
        private bool ShouldCalculateSightCircle()
        {
            IntPoint pos = Position.ToIntPoint();
            if (_p != pos || Parent.UpdateCount != _w)
            {
                _p = pos;
                _w = Parent.UpdateCount;
                return true;
            }
            return false;
        }

        private HashSet<IntPoint> CalculateSightCircle()
        {
            CalculatedSightCircle.Clear();

            if (Parent.BlockSight == 1) //Line casting
            {
                foreach (HashSet<IntPoint> ray in SightRays)
                    foreach (IntPoint p in ray)
                    {
                        if (Parent.BlocksSight(p.X + (int)Position.X, p.Y + (int)Position.Y))
                            break;

                        CalculatedSightCircle.Add(p);
                        foreach (IntPoint s in SurroundingPoints)
                        {
                            IntPoint sp = new IntPoint(p.X + s.X, p.Y + s.Y);
                            if (SightCircle.Contains(sp))
                                CalculatedSightCircle.Add(sp);
                        }
                    }
            }

            if (Parent.BlockSight == 2) //Path
            {
                Stack<IntPoint> scan = new Stack<IntPoint>();
                HashSet<int> scanned = new HashSet<int>();

                scan.Push(Position.ToIntPoint());
                while (scan.Count != 0)
                {
                    IntPoint current = scan.Pop();
                    if (CalculatedSightCircle.Add(new IntPoint(current.X - (int)Position.X, current.Y - (int)Position.Y)))
                        foreach (IntPoint s in SurroundingPoints)
                        {
                            IntPoint p = new IntPoint(current.X + s.X, current.Y + s.Y);
                            IntPoint c = new IntPoint(p.X - (int)Position.X, p.Y - (int)Position.Y);
                            if (scanned.Contains(c.GetHashCode()) || !SightCircle.Contains(c))
                                continue;

                            scanned.Add(c.GetHashCode());
                            if (Parent.BlocksSight(p.X, p.Y))
                            {
                                CalculatedSightCircle.Add(c);
                                continue;
                            }

                            scan.Push(p);
                        }
                }
            }

            return CalculatedSightCircle;
        }
    }
}
