//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using System.Linq;
using RotMG.Game.DungeonGen.Dungeon;
using RotMG.Game.DungeonGen.Templates;

namespace RotMG.Game.DungeonGen
{
    public enum RasterizationStep
    {
        Initialize = 5,

        Background = 6,
        Corridor = 7,
        Room = 8,
        Overlay = 9,

        Finish = 10
    }

    public class Rasterizer
    {
        private readonly Random _rand;
        private readonly DungeonGraph _graph;
        private readonly BitmapRasterizer<DungeonTile> _rasterizer;

        private static readonly TileType Space = new TileType(0x00fe, "Space");

        public RasterizationStep Step { get; set; }

        public Rasterizer(int seed, DungeonGraph graph)
        {
            _rand = new Random(seed);
            _graph = graph;
            _rasterizer = new BitmapRasterizer<DungeonTile>(graph.Width, graph.Height);
            Step = RasterizationStep.Initialize;
        }

        public void Rasterize(RasterizationStep? targetStep = null)
        {
            while (Step != targetStep && Step != RasterizationStep.Finish)
            {
                RunStep();
            }
        }

        private void RunStep()
        {
            switch (Step)
            {
                case RasterizationStep.Initialize:
                    _rasterizer.Clear(new DungeonTile
                    {
                        TileType = Space
                    });
                    _graph.Template.InitializeRasterization(_graph);
                    break;

                case RasterizationStep.Background:
                    MapRender bg = _graph.Template.CreateBackground();
                    bg.Init(_rasterizer, _graph, _rand);
                    bg.Rasterize();
                    break;

                case RasterizationStep.Corridor:
                    RasterizeCorridors();
                    break;

                case RasterizationStep.Room:
                    RasterizeRooms();
                    break;

                case RasterizationStep.Overlay:
                    MapRender overlay = _graph.Template.CreateOverlay();
                    overlay.Init(_rasterizer, _graph, _rand);
                    overlay.Rasterize();
                    break;
            }
            Step++;
        }

        private void RasterizeCorridors()
        {
            MapCorridor corridor = _graph.Template.CreateCorridor();
            corridor.Init(_rasterizer, _graph, _rand);

            foreach (Room room in _graph.Rooms)
                foreach (Edge edge in room.Edges)
                {
                    if (edge.RoomA != room)
                        continue;
                    RasterizeCorridor(corridor, edge);
                }
        }

        private void CreateCorridor(Room src, Room dst, out Point srcPos, out Point dstPos)
        {
            Edge edge = src.Edges.Single(ed => ed.RoomB == dst);
            Link link = edge.Linkage;

            if (link.Direction == Direction.South || link.Direction == Direction.North)
            {
                srcPos = new Point(link.Offset, src.Pos.Y + src.Height / 2);
                dstPos = new Point(link.Offset, dst.Pos.Y + dst.Height / 2);
            }
            else if (link.Direction == Direction.East || link.Direction == Direction.West)
            {
                srcPos = new Point(src.Pos.X + src.Width / 2, link.Offset);
                dstPos = new Point(dst.Pos.X + dst.Width / 2, link.Offset);
            }
            else
                throw new ArgumentException();
        }

        private void RasterizeCorridor(MapCorridor corridor, Edge edge)
        {
            Point srcPos, dstPos;
            CreateCorridor(edge.RoomA, edge.RoomB, out srcPos, out dstPos);
            corridor.Rasterize(edge.RoomA, edge.RoomB, srcPos, dstPos);
        }

        private void RasterizeRooms()
        {
            foreach (Room room in _graph.Rooms)
                room.Rasterize(_rasterizer, _rand);
        }

        public DungeonTile[,] ExportMap()
        {
            return _rasterizer.Bitmap;
        }
    }
}
