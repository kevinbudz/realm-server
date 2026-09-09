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

        public int[,] TileUpdates;
        public Dictionary<int, int> EntityUpdates;
        public HashSet<Entity> Entities;
        public HashSet<IntPoint> CalculatedSightCircle;
        private readonly List<Entity> _hitTestScratch = new List<Entity>();
        private readonly List<Entity> _dropScratch = new List<Entity>();

        public void SendNewTick()
        {
            HandleQuest();
            List<ObjectStatus> statuses = new List<ObjectStatus>();
            foreach (Entity en in Entities)
                if (EntityUpdates[en.Id] != en.UpdateCount)
                {
                    statuses.Add(en.GetObjectStatus(true));
                    EntityUpdates[en.Id] = en.UpdateCount;
                }

            Client.Send(GameServer.NewTick(statuses, PrivateSVs));
            PrivateSVs.Clear();
            AwaitingMoves++;
        }

        public void SendUpdate()
        {
            bool nUpdate = ShouldCalculateSightCircle();
            HashSet<IntPoint> sight = Parent.BlockSight == 0 ? SightCircle :
                    nUpdate ? CalculateSightCircle() : CalculatedSightCircle;

            List<TileData> tiles = null;
            List<ObjectDefinition> adds = null;
            List<ObjectDrop> drops = null;
            HashSet<int> droppedIds = null;

            if (nUpdate)
            {
                //Get tiles
                foreach (IntPoint p in sight)
                {
                    int x = p.X + (int)Position.X;
                    int y = p.Y + (int)Position.Y;
                    Tile tile = Parent.GetTile(x, y);

                    if (tile == null || TileUpdates[x, y] == tile.UpdateCount)
                        continue;

                    if (tiles == null)
                        tiles = new List<TileData>();
                    tiles.Add(new TileData
                    {
                        TileType = tile.Type,
                        X = (short)x,
                        Y = (short)y
                    });

                    TileUpdates[x, y] = tile.UpdateCount;
                }

                //Add statics
                foreach (IntPoint p in sight)
                {
                    int x = p.X + (int)Position.X;
                    int y = p.Y + (int)Position.Y;

                    Tile tile = Parent.GetTile(x, y);
                    if (tile == null || tile.StaticObject == null)
                        continue;

                    if (TileUpdates[x, y] == tile.UpdateCount)
                    {
                        if (Entities.Add(tile.StaticObject))
                        {
                            if (adds == null)
                                adds = new List<ObjectDefinition>();
                            adds.Add(tile.StaticObject.GetObjectDefinition());
                            EntityUpdates.Add(tile.StaticObject.Id, tile.StaticObject.UpdateCount);
                        }
                    }
                }
            }

            //Add players (chunk-routed, sight-gated; self passes at 0,0 and must stay tracked for NewTick)
            Parent.PlayerChunks.HitTest(Position, SightRadius, _hitTestScratch);
            foreach (Entity en in _hitTestScratch)
            {
                int dx = (int)en.Position.X - (int)Position.X;
                int dy = (int)en.Position.Y - (int)Position.Y;
                if (!sight.Contains(new IntPoint(dx, dy)))
                    continue;

                if (Entities.Add(en))
                {
                    if (adds == null)
                        adds = new List<ObjectDefinition>();
                    adds.Add(en.GetObjectDefinition());
                    EntityUpdates.Add(en.Id, en.UpdateCount);
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
                    if (adds == null)
                        adds = new List<ObjectDefinition>();
                    adds.Add(en.GetObjectDefinition());
                    EntityUpdates.Add(en.Id, en.UpdateCount);
                }
            }

            //Remove entities and statics (as they end up in the same Entities dictionary
            _dropScratch.Clear();
            foreach (Entity en in Entities)
            {
                if (en == this)
                    continue;

                if (en.Parent != null)
                {
                    int dx = (int)en.Position.X - (int)Position.X;
                    int dy = (int)en.Position.Y - (int)Position.Y;
                    if (sight.Contains(new IntPoint(dx, dy)))
                        continue;
                }

                if (drops == null)
                    drops = new List<ObjectDrop>();
                if (droppedIds == null)
                    droppedIds = new HashSet<int>();
                drops.Add(en.GetObjectDrop());
                droppedIds.Add(en.Id);
                EntityUpdates.Remove(en.Id);
                _dropScratch.Add(en);
            }

            foreach (Entity en in _dropScratch)
                Entities.Remove(en);

            int tileCount = tiles == null ? 0 : tiles.Count;
            int addCount = adds == null ? 0 : adds.Count;
            int dropCount = drops == null ? 0 : drops.Count;
            if (tileCount > 0 || addCount > 0 || dropCount > 0)
            {
                Client.Send(GameServer.Update(
                    tiles == null ? new List<TileData>() : tiles,
                    adds == null ? new List<ObjectDefinition>() : adds,
                    drops == null ? new List<ObjectDrop>() : drops));
                FameStats.TilesUncovered += tileCount;
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
