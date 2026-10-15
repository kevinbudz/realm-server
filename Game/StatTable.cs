//[W07] Typed stat storage, replacing Dictionary<StatType, object> (boxed values, a hash lookup per write).
//Values live in an int array and a string array indexed by StatType. Two 256-bit masks say which slots hold a
//value (Present, the old SVs key set) and which changed since the last clear (Dirty, the old NewSVs key set).
//The stat order on the wire is StatType order (ascending bit index), not insertion order.
using System;
using System.Numerics;
using RotMG.Networking;

namespace RotMG.Game
{
    //A 256-bit set of StatType values. Passed by value: ObjectStatus takes a copy of the mask when it is built,
    //which is the per-tick snapshot of the diff.
    public struct StatMask
    {
        private ulong _w0, _w1, _w2, _w3;

        public int Count => BitOperations.PopCount(_w0) + BitOperations.PopCount(_w1) +
                            BitOperations.PopCount(_w2) + BitOperations.PopCount(_w3);

        public bool Get(int bit)
        {
            return (Word(bit >> 6) & (1UL << (bit & 63))) != 0;
        }

        public void Set(int bit)
        {
            ulong m = 1UL << (bit & 63);
            switch (bit >> 6)
            {
                case 0: _w0 |= m; break;
                case 1: _w1 |= m; break;
                case 2: _w2 |= m; break;
                default: _w3 |= m; break;
            }
        }

        public void Clear()
        {
            _w0 = _w1 = _w2 = _w3 = 0;
        }

        //Smallest set bit at or above from, or -1. Walks the words with TrailingZeroCount, so an empty mask costs four checks.
        public int NextSet(int from)
        {
            for (int w = from >> 6; w < 4; w++)
            {
                ulong bits = Word(w);
                if (w == (from >> 6))
                    bits &= ~0UL << (from & 63);
                if (bits != 0)
                    return (w << 6) + BitOperations.TrailingZeroCount(bits);
            }
            return -1;
        }

        private ulong Word(int w)
        {
            switch (w)
            {
                case 0: return _w0;
                case 1: return _w1;
                case 2: return _w2;
                default: return _w3;
            }
        }
    }

    public sealed class StatTable
    {
        //One slot per StatType value (the enum is checked once; it must fit the 256-bit masks, and StatType is
        //written as one byte on the wire).
        public static readonly int Length = ComputeLength();

        private readonly int[] _ints = new int[Length];
        private string[] _strings; //allocated by the first string write (only Name and GuildName use it)

        public StatMask Present;
        public StatMask Dirty;

        private static int ComputeLength()
        {
            int max = -1;
            foreach (StatType t in Enum.GetValues<StatType>())
                max = Math.Max(max, (int)t);
            if (max >= 256)
                throw new InvalidOperationException($"StatType value {max} does not fit the 256-bit stat masks");
            return max + 1;
        }

        //The stats that carry a string value (ObjectStatus writes them as strings, everything else as int32).
        public static bool IsString(StatType type)
        {
            return type == StatType.Name || type == StatType.GuildName;
        }

        public bool Has(StatType type) => Present.Get((int)type);

        public bool IsDirty(StatType type) => Dirty.Get((int)type);

        public int GetInt(StatType type) => _ints[(int)type];

        public string GetString(StatType type) => _strings?[(int)type];

        //Writes the value and marks it present and dirty. No duplicate check here (see Entity.TrySetSV).
        public void SetInt(StatType type, int value)
        {
#if DEBUG
            if (IsString(type))
                throw new InvalidOperationException($"{type} is a string stat");
#endif
            int i = (int)type;
            _ints[i] = value;
            Present.Set(i);
            Dirty.Set(i);
        }

        public void SetString(StatType type, string value)
        {
#if DEBUG
            if (!IsString(type))
                throw new InvalidOperationException($"{type} is not a string stat");
#endif
            int i = (int)type;
            _strings ??= new string[Length];
            _strings[i] = value;
            Present.Set(i);
            Dirty.Set(i);
        }

        //The per-tick diff is sent and then cleared (old NewSVs.Clear).
        public void ClearDirty()
        {
            Dirty.Clear();
        }

        //Forces a full resend of every present stat (old "copy SVs into NewSVs").
        public void MarkAllDirty()
        {
            Dirty = Present;
        }

        //Forgets every stat (old SVs.Clear / PrivateSVs.Clear).
        public void Clear()
        {
            Present.Clear();
            Dirty.Clear();
        }

        //One stat on the wire: u8 StatType, then i32, or for a string stat a string (a null value goes out as "",
        //which clears the client's copy; see W05 for GuildName).
        public void WriteStat(PacketWriter wtr, int index)
        {
            wtr.Write((byte)index);
            if (IsString((StatType)index))
                wtr.Write(_strings?[index] ?? "");
            else
                wtr.Write(_ints[index]);
        }
    }
}
