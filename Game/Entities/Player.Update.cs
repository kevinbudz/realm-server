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

        //Last tile UpdateCount sent to this client, keyed by packed tile
        //coords. Only ~700 sight tiles are ever stored, instead of a
        //W*H array (16 MiB per player in a 2048x2048 Realm).
        public Dictionary<long, int> TileUpdates;
        private static long TileKey(int x, int y) => ((long)x << 32) | (uint)y;
        private int GetSeenTileUpdate(int x, int y) => TileUpdates.TryGetValue(TileKey(x, y), out int v) ? v : 0;
        private void SetSeenTileUpdate(int x, int y, int v) => TileUpdates[TileKey(x, y)] = v;
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
        private readonly List<Entity> _hitTestScratch = new List<Entity>();
        private readonly List<Entity> _dropScratch = new List<Entity>();
        //Reused per-tick packet buffers: Update/NewTick serialize
        //synchronously, so these never escape the call.
        private readonly List<ObjectStatus> _statusScratch = new List<ObjectStatus>();
        private readonly List<TileData> _tilesScratch = new List<TileData>();
        private readonly List<ObjectDefinition> _addsScratch = new List<ObjectDefinition>();
        private readonly List<ObjectDrop> _dropsScratch = new List<ObjectDrop>();

        public void SendNewTick()
        {
            HandleQuest();
            _newTickCount++;
            _statusScratch.Clear();
            foreach (Entity en in Entities)
            {
                if (EntityUpdates[en.Id] != en.UpdateCount)
                {
                    if (en is Player && en != this && !_nearPlayerIds.Contains(en.Id))
                    {
                        //Far presence: round-robin slice only. Full stats keep
                        //every send self-contained, so no delta is ever lost
                        //across skipped ticks (NewSVs are cleared globally).
                        if ((_newTickCount + en.Id) % FarPlayerTickDivisor != 0)
                            continue;
                        _statusScratch.Add(en.GetObjectStatus(false));
                    }
                    else if (en is Player)
                        _statusScratch.Add(en.GetObjectStatus(false));
                    else
                        _statusScratch.Add(en.GetObjectStatus(true));
                    EntityUpdates[en.Id] = en.UpdateCount;
                }
            }

            Client.Send(GameServer.NewTick(_statusScratch, PrivateSVs));
            PrivateSVs.Clear();
            AwaitingMoves++;
        }

        public void SendUpdate()
        {
            bool nUpdate = ShouldCalculateSightCircle();
            HashSet<IntPoint> sight = Parent.BlockSight == 0 ? SightCircle :
                    nUpdate ? CalculateSightCircle() : CalculatedSightCircle;

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

                    if (GetSeenTileUpdate(x, y) != tile.Value.UpdateCount)
                    {
                        _tilesScratch.Add(new TileData
                        {
                            TileType = tile.Value.Type,
                            X = (short)x,
                            Y = (short)y
                        });

                        SetSeenTileUpdate(x, y, tile.Value.UpdateCount);
                    }

                    if (tile.Value.StaticObject != null && Entities.Add(tile.Value.StaticObject))
                    {
                        _addsScratch.Add(tile.Value.StaticObject.GetObjectDefinition());
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
                    _addsScratch.Add(en.GetObjectDefinition());
                    EntityUpdates.Add(en.Id, en.UpdateCount);
                }
            }

            //Far presence: every world player stays known (realm-src parity)
            //so minimap dots and teleport clicks work at any distance. This
            //fires once per join, not per tick: afterwards Entities.Add hits.
            //Near ids are topped up for players the chunk query missed.
            foreach (Player p in Parent.Players.Values)
            {
                int dx = (int)p.Position.X - (int)Position.X;
                int dy = (int)p.Position.Y - (int)Position.Y;
                if (dx * dx + dy * dy <= SightRadius * SightRadius && sight.Contains(new IntPoint(dx, dy)))
                    _nearPlayerIds.Add(p.Id);

                if (Entities.Add(p))
                {
                    _addsScratch.Add(p.GetObjectDefinition());
                    EntityUpdates.Add(p.Id, p.UpdateCount);
                }
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
                    _addsScratch.Add(en.GetObjectDefinition());
                    EntityUpdates.Add(en.Id, en.UpdateCount);
                }
            }

            //The quest target is always known to the client, even far out
            //of sight: the QuestArrow needs its GameObject to point at it.
            //Mirrors realm-src-master Player.Update GetNewEntities, which
            //yields questEntity unconditionally.
            if (Quest != null && Quest.Parent == Parent && Entities.Add(Quest))
            {
                _addsScratch.Add(Quest.GetObjectDefinition());
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
                Client.Send(GameServer.Update(_tilesScratch, _addsScratch, _dropsScratch));
                FameStats.TilesUncovered += _tilesScratch.Count;
            }
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
