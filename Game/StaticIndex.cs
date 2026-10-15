using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Generic;

namespace RotMG.Game
{
    //Spatial index over World.Statics. Statics are not in EntityChunks or PlayerChunks, so every range query over
    //them used to walk the whole Statics dictionary (~261k entries in the realm) on each call, including calls from
    //behavior ticks. Cells are CellSize tiles square. A static whose position is off the grid (non-finite, or outside
    //the world) is kept in a loose list that every query checks, so results are exact for any position.
    //
    //Order: results come back in the order the old scan visited them, which is Statics dictionary enumeration order
    //(slot order). Dictionary.Remove puts a freed slot at the head of a LIFO free list and Add reuses that slot first,
    //so StaticOrder mirrors those slots: Add takes the most recently freed key or the next new one, and Remove pushes
    //the key back.
    //
    //World.AddEntity, RemoveEntity and MoveEntity are the only writers of World.Statics and of a static's Position,
    //and they keep this index in step (Add and Remove only where Statics.Add and Statics.Remove run).
    public sealed class StaticIndex
    {
        public const int CellSize = ChunkController.Size;

        private const int LooseSlot = -1;

        //Per-thread buffer for callers that read a query at once and do not call another query while reading.
        //Each world ticks on one worker thread, so the buffer is never shared between concurrent queries.
        [ThreadStatic] private static List<StaticObject> _scratch;

        private static readonly IComparer<StaticObject> ByOrder =
            Comparer<StaticObject>.Create((a, b) => a.StaticOrder.CompareTo(b.StaticOrder));

        private readonly int _cellsX;
        private readonly int _cellsY;
        private readonly List<StaticObject>[] _cells;
        private readonly List<StaticObject> _loose = new List<StaticObject>();
        //Static id -> cell slot (or LooseSlot), so Remove and Move need no position.
        private readonly Dictionary<int, int> _slotOf = new Dictionary<int, int>();
        //Order keys freed by Remove, reused LIFO by Add (the same policy as Dictionary's free list).
        private readonly Stack<int> _freeOrder = new Stack<int>();
        private int _nextOrder;

        public StaticIndex(int width, int height)
        {
            _cellsX = (int)Math.Floor(width / (double)CellSize) + 1;
            _cellsY = (int)Math.Floor(height / (double)CellSize) + 1;
            _cells = new List<StaticObject>[_cellsX * _cellsY];
        }

        public int Count => _slotOf.Count;

        //Empties and returns this thread's buffer. Do not hold it across another Scratch call.
        public static List<StaticObject> Scratch
        {
            get
            {
                if (_scratch == null)
                    _scratch = new List<StaticObject>(64);
                _scratch.Clear();
                return _scratch;
            }
        }

        //Call exactly where Statics.Add runs, after it, with the static's position already set.
        public void Add(StaticObject st)
        {
            if (_slotOf.ContainsKey(st.Id))
                return;
            st.StaticOrder = _freeOrder.Count > 0 ? _freeOrder.Pop() : _nextOrder++;
            int slot = SlotFor(st.Position);
            _slotOf[st.Id] = slot;
            BucketFor(slot).Add(st);
        }

        //Call exactly where Statics.Remove runs (a no-op for statics that are not indexed).
        public void Remove(StaticObject st)
        {
            if (!_slotOf.TryGetValue(st.Id, out int slot))
                return;
            _slotOf.Remove(st.Id);
            List<StaticObject> bucket = slot == LooseSlot ? _loose : _cells[slot];
            bucket?.Remove(st);
            _freeOrder.Push(st.StaticOrder);
        }

        //Re-files a static whose position changed. Its order key stays. A static that is not indexed (MoveEntity
        //during AddEntity, before Add) is left alone: Add reads its position.
        public void Move(StaticObject st)
        {
            if (!_slotOf.TryGetValue(st.Id, out int old))
                return;
            int slot = SlotFor(st.Position);
            if (slot == old)
                return;
            List<StaticObject> bucket = old == LooseSlot ? _loose : _cells[old];
            bucket?.Remove(st);
            _slotOf[st.Id] = slot;
            BucketFor(slot).Add(st);
        }

        //Appends to `into` every static s with target.DistanceSquared(s.Position) < range * range, the predicate
        //BehaviorHelpers.NearbyEntities used before the index. Only cells that can hold such a static are visited:
        //the window is |range| plus one tile on each side. Floor is monotone, so an entity inside that window
        //always lands in a visited cell, and the predicate itself is unchanged. Float rounding is far below one
        //tile at world coordinates. With inDictionaryOrder, results are sorted by StaticOrder (the old scan order).
        public void Collect(Position target, float range, List<StaticObject> into, bool inDictionaryOrder = false)
        {
            float r2 = range * range;
            if (float.IsNaN(r2) || !float.IsFinite(target.X) || !float.IsFinite(target.Y))
                return; //The predicate is false for every static in these cases.

            int first = into.Count;
            if (!float.IsFinite(r2))
            {
                //Infinite range: every static with a finite distance qualifies, so scan all cells.
                for (int slot = 0; slot < _cells.Length; slot++)
                    CollectBucket(_cells[slot], target, r2, into);
            }
            else
            {
                double reach = Math.Abs((double)range) + 1;
                int x0 = ClampCell(Math.Floor((target.X - reach) / CellSize), _cellsX);
                int x1 = ClampCell(Math.Floor((target.X + reach) / CellSize), _cellsX);
                int y0 = ClampCell(Math.Floor((target.Y - reach) / CellSize), _cellsY);
                int y1 = ClampCell(Math.Floor((target.Y + reach) / CellSize), _cellsY);
                for (int x = x0; x <= x1; x++)
                    for (int y = y0; y <= y1; y++)
                        CollectBucket(_cells[x * _cellsY + y], target, r2, into);
            }
            CollectBucket(_loose, target, r2, into);

            if (inDictionaryOrder && into.Count - first > 1)
                into.Sort(first, into.Count - first, ByOrder);
        }

        private static void CollectBucket(List<StaticObject> bucket, Position target, float r2, List<StaticObject> into)
        {
            if (bucket == null)
                return;
            foreach (StaticObject st in bucket)
                if (target.DistanceSquared(st.Position) < r2)
                    into.Add(st);
        }

        private static int ClampCell(double cell, int cells)
        {
            if (cell < 0)
                return 0;
            if (cell > cells - 1)
                return cells - 1;
            return (int)cell;
        }

        private int SlotFor(Position p)
        {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y))
                return LooseSlot;
            double cx = Math.Floor(p.X / (double)CellSize);
            double cy = Math.Floor(p.Y / (double)CellSize);
            if (cx < 0 || cy < 0 || cx >= _cellsX || cy >= _cellsY)
                return LooseSlot;
            return (int)cx * _cellsY + (int)cy;
        }

        private List<StaticObject> BucketFor(int slot)
        {
            if (slot == LooseSlot)
                return _loose;
            return _cells[slot] ??= new List<StaticObject>(8);
        }
    }
}
