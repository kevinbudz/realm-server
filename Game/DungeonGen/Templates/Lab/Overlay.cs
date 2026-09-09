//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates.Lab
{
    internal class Overlay : MapRender
    {
        public override void Rasterize()
        {
            DungeonTile wall = new DungeonTile
            {
                TileType = LabTemplate.Space,
                Object = new DungeonObject
                {
                    ObjectType = LabTemplate.LabWall
                }
            };

            int w = Rasterizer.Width, h = Rasterizer.Height;
            DungeonTile[,] buf = Rasterizer.Bitmap;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (buf[x, y].TileType != LabTemplate.Space || buf[x, y].Object != null)
                        continue;

                    bool isWall = false;
                    if (x == 0 || y == 0 || x + 1 == w || y + 1 == h)
                        isWall = false;
                    else
                    {
                        for (int dx = -1; dx <= 1 && !isWall; dx++)
                            for (int dy = -1; dy <= 1 && !isWall; dy++)
                            {
                                if (buf[x + dx, y + dy].TileType != LabTemplate.Space)
                                {
                                    isWall = true;
                                    break;
                                }
                            }
                    }
                    if (isWall)
                        buf[x, y] = wall;
                }
        }
    }
}
