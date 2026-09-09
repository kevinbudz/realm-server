//Dungeon-generator support written for this project. The generation engine
//it hosts is a port of realm-src-master's DungeonGen (see note on the
//ported files); this file itself is original.
using System;
using System.Collections.Generic;

namespace RotMG.Game.DungeonGen
{
    //Small runtime primitives for the dungeon generator, mirroring the
    //RotMG.Common types that realm-src-master's DungeonGen builds on
    //(Point/Rect/BitmapRasterizer, NormDist, Shuffle, Swap, Empty, dictionary
    //helpers). Semantics were recovered from that reference; only the
    //dungeon-generation behavior is reproduced here.
    public struct Point
    {
        public static readonly Point Zero = new Point();

        public readonly int X;
        public readonly int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point(double x, double y)
            : this((int)Math.Round(x), (int)Math.Round(y))
        {
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", X, Y);
        }
    }

    public struct Rect
    {
        public static readonly Rect Empty = new Rect();

        public readonly int MaxX;
        public readonly int MaxY;
        public readonly int X;
        public readonly int Y;

        public Rect(int x, int y, int maxX, int maxY)
        {
            MaxX = maxX < x ? x : maxX;
            MaxY = maxY < y ? y : maxY;
            X = x;
            Y = y;
        }

        public bool IsEmpty
        {
            get { return X == MaxX || Y == MaxY; }
        }

        public bool Contains(Point pt)
        {
            return Contains(pt.X, pt.Y);
        }

        public bool Contains(int x, int y)
        {
            return x >= X && x < MaxX && y >= Y && y < MaxY;
        }

        public Rect Intersection(Rect rect)
        {
            return new Rect(Math.Max(X, rect.X), Math.Max(Y, rect.Y),
                Math.Min(MaxX, rect.MaxX), Math.Min(MaxY, rect.MaxY));
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3})", X, MaxX, Y, MaxY);
        }
    }

    //Clamped gaussian sampler, same distribution as the reference NormDist
    //(Box-Muller pair cache, stdev/mean scaling, clamp to [min, max]).
    public class NormDist
    {
        private readonly float _stdev;
        private readonly float _mean;
        private readonly float _max;
        private readonly float _min;
        private readonly Random _random;
        private double? _nextNum;

        public NormDist(float stdev, float mean, float min, float max, int? seed = null)
        {
            _stdev = stdev;
            _mean = mean;
            _min = min;
            _max = max;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        private double NextValueInternal()
        {
            if (_nextNum.HasValue)
            {
                double value = _nextNum.Value;
                _nextNum = null;
                return value;
            }
            double num;
            double num2;
            double num3;
            do
            {
                num = _random.NextDouble() * 2.0 - 1.0;
                num2 = _random.NextDouble() * 2.0 - 1.0;
                num3 = num * num + num2 * num2;
            } while (num3 >= 1.0);
            _nextNum = num * Math.Sqrt(-2.0 * Math.Log(num3) / num3);
            return num2 * Math.Sqrt(-2.0 * Math.Log(num3) / num3);
        }

        public double NextValue()
        {
            double num = NextValueInternal() * _stdev + _mean;
            if (num < _min)
                num = _min;
            if (num > _max)
                num = _max;
            return num;
        }
    }

    public static class RandomExtensions
    {
        public static void Shuffle<T>(this Random random, IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int index = random.Next(i + 1);
                T value = items[i];
                items[i] = items[index];
                items[index] = value;
            }
        }
    }

    public static class GenUtils
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }
    }

    public static class Empty<T>
    {
        public static readonly T[] Array = new T[0];
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> creator)
        {
            TValue value;
            if (!dict.TryGetValue(key, out value))
                dict[key] = value = creator(key);
            return value;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            TValue value;
            if (!dict.TryGetValue(key, out value))
                return defaultValue;
            return value;
        }
    }

    //Tile rasterizer backing the generator output, same operations as the
    //reference BitmapRasterizer (Clear/FillRect/Bresenham DrawLine with round
    //caps/FillTriangle/Bezier).
    public class BitmapRasterizer<TPixel> where TPixel : struct
    {
        private const int SegCount = 10;

        private readonly TPixel[,] _buffer;
        private readonly int _height;
        private readonly int _width;

        private readonly bool[][,] _caps = new bool[5][,]
        {
            new bool[1, 1] { { true } },
            new bool[2, 2]
            {
                { true, true },
                { true, true }
            },
            new bool[3, 3]
            {
                { false, true, false },
                { true, true, true },
                { false, true, false }
            },
            new bool[4, 4]
            {
                { false, true, true, false },
                { true, true, true, true },
                { true, true, true, true },
                { false, true, true, false }
            },
            new bool[5, 5]
            {
                { false, true, true, true, false },
                { true, true, true, true, true },
                { true, true, true, true, true },
                { true, true, true, true, true },
                { false, true, true, true, false }
            }
        };

        public TPixel[,] Bitmap { get { return _buffer; } }
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }

        public BitmapRasterizer(int width, int height)
        {
            _buffer = new TPixel[width, height];
            _width = width;
            _height = height;
        }

        public void Clear(TPixel bg)
        {
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    _buffer[x, y] = bg;
        }

        private void FillRectInternal(int minX, int minY, int maxX, int maxY, TPixel pix)
        {
            for (int y = minY; y < maxY; y++)
                for (int x = minX; x < maxX; x++)
                    _buffer[x, y] = pix;
        }

        private void FillRectInternal(int minX, int minY, int maxX, int maxY, Func<int, int, TPixel> texMapping)
        {
            for (int y = minY; y < maxY; y++)
                for (int x = minX; x < maxX; x++)
                    _buffer[x, y] = texMapping(x, y);
        }

        public void FillRect(int x, int y, int w, int h, TPixel pix)
        {
            FillRectInternal(x, y, x + w, y + h, pix);
        }

        public void FillRect(Rect rect, TPixel pix)
        {
            FillRectInternal(rect.X, rect.Y, rect.MaxX, rect.MaxY, pix);
        }

        private void ApplyCap(int x, int y, TPixel pix, int width)
        {
            if (width == 1)
                _buffer[x, y] = pix;
            if (width <= 5)
            {
                bool[,] cap = _caps[width - 1];
                x -= width >> 1;
                y -= width >> 1;
                for (int i = 0; i < width; i++)
                    for (int j = 0; j < width; j++)
                        if (cap[j, i])
                            _buffer[x + j, y + i] = pix;
            }
            else
            {
                int r = width >> 1;
                x -= r;
                y -= r;
                FillRectInternal(x, y, x + width, y + width, pix);
            }
        }

        private void ApplyCap(int x, int y, Func<int, int, TPixel> texMapping, int width)
        {
            if (width == 1)
                _buffer[x, y] = texMapping(x, y);
            if (width <= 5)
            {
                bool[,] cap = _caps[width - 1];
                x -= width >> 1;
                y -= width >> 1;
                for (int i = 0; i < width; i++)
                    for (int j = 0; j < width; j++)
                        if (cap[j, i])
                            _buffer[x + j, y + i] = texMapping(x + j, y + i);
            }
            else
            {
                int r = width >> 1;
                x -= r;
                y -= r;
                FillRectInternal(x, y, x + width, y + width, texMapping);
            }
        }

        public void DrawLine(Point a, Point b, TPixel pix, int width = 1)
        {
            DrawLine(a.X, a.Y, b.X, b.Y, pix, width);
        }

        public void DrawLine(int x0, int y0, int x1, int y1, TPixel pix, int width = 1)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (x0 != x1 || y0 != y1)
            {
                ApplyCap(x0, y0, pix, width);
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                else
                {
                    err += dx;
                    y0 += sy;
                }
            }
            ApplyCap(x0, y0, pix, width);
        }

        public void DrawLine(Point a, Point b, Func<int, int, TPixel> texMapping, int width = 1)
        {
            DrawLine(a.X, a.Y, b.X, b.Y, texMapping, width);
        }

        public void DrawLine(int x0, int y0, int x1, int y1, Func<int, int, TPixel> texMapping, int width = 1)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            ApplyCap(x0, y0, texMapping, width);
            while (x0 != x1 || y0 != y1)
            {
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                else
                {
                    err += dx;
                    y0 += sy;
                }
                ApplyCap(x0, y0, texMapping, width);
            }
        }

        private void ScanEdge(int x0, int y0, int x1, int y1, int?[] min, int?[] max)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            if (!min[y0].HasValue || min[y0] > x0)
                min[y0] = x0;
            if (!max[y0].HasValue || max[y0] < x0)
                max[y0] = x0;
            while (x0 != x1 || y0 != y1)
            {
                int e2 = 2 * err;
                if (e2 >= -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
                if (!min[y0].HasValue || min[y0] > x0)
                    min[y0] = x0;
                if (!max[y0].HasValue || max[y0] < x0)
                    max[y0] = x0;
            }
        }

        public void FillTriangle(Point a, Point b, Point c, TPixel color)
        {
            int top = Math.Min(a.Y, Math.Min(b.Y, c.Y));
            int bottom = Math.Max(a.Y, Math.Max(b.Y, c.Y)) + 1;
            int?[] min = new int?[bottom - top];
            int?[] max = new int?[bottom - top];
            ScanEdge(a.X, a.Y - top, b.X, b.Y - top, min, max);
            ScanEdge(b.X, b.Y - top, c.X, c.Y - top, min, max);
            ScanEdge(c.X, c.Y - top, a.X, a.Y - top, min, max);
            for (int y = top; y < bottom; y++)
                for (int x = min[y - top].Value; x <= max[y - top].Value; x++)
                    _buffer[x, y] = color;
        }

        public void DrawBezier(Point a, Point cp, Point b, TPixel pix, int width = 1)
        {
            DrawBezier(a.X, a.Y, cp.X, cp.Y, b.X, b.Y, pix, width);
        }

        public void DrawBezier(int x0, int y0, int x1, int y1, int x2, int y2, TPixel pix, int width = 1)
        {
            double px = x0;
            double py = y0;
            for (int i = 0; i < SegCount; i++)
            {
                double t = (i + 1) / 10.0;
                double u = 1.0 - t;
                double nx = u * u * x0 + 2.0 * u * t * x1 + t * t * x2;
                double ny = u * u * y0 + 2.0 * u * t * y1 + t * t * y2;
                DrawLine((int)px, (int)py, (int)nx, (int)ny, pix, width);
                px = nx;
                py = ny;
            }
        }
    }
}
