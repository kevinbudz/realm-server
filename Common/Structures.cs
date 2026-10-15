using RotMG.Game;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace RotMG.Common
{
    public struct FameStats
    {
        public int BaseFame;
        public int TotalFame;
        public List<FameBonus> Bonuses;
    }

    public struct FameBonus
    {
        public string Name;
        public int Fame;
    }

    public struct TileData
    {
        public ushort TileType;
        public short X;
        public short Y;

        public void Write(PacketWriter wtr)
        {
            wtr.Write(X);
            wtr.Write(Y);
            wtr.Write(TileType);
        }
    }

    public struct ObjectDrop
    {
        public int Id;
        public bool Explode;

        public void Write(PacketWriter wtr)
        {
            wtr.Write(Id);
            wtr.Write(Explode);
        }
    }

    public struct ObjectDefinition
    {
        public ushort ObjectType;
        public ObjectStatus ObjectStatus;

        public void Write(PacketWriter wtr)
        {
            wtr.Write(ObjectType);
            ObjectStatus.Write(wtr);
        }
    }

    //[W08] One entity status in an Update add or a NewTick. Inside a broadcast phase it carries the bytes shared by every
    //receiver (Entity.SharedStatus). Outside one (the join frame, direct calls) it carries the status itself, serialized
    //into the packet as before, so that path builds no cache. Writes the same bytes either way.
    public struct StatusEntry
    {
        public bool Shared;
        public ArraySegment<byte> Bytes;
        public ObjectStatus Status;

        public static StatusEntry FromShared(ArraySegment<byte> bytes) => new StatusEntry { Shared = true, Bytes = bytes };
        public static StatusEntry Direct(ObjectStatus status) => new StatusEntry { Shared = false, Status = status };

        public void Write(PacketWriter wtr)
        {
            if (Shared)
                wtr.Write(new ReadOnlySpan<byte>(Bytes.Array, Bytes.Offset, Bytes.Count));
            else
                Status.Write(wtr);
        }
    }

    //[W08] An Update add: the type, then its status (StatusEntry). Writes the same bytes as ObjectDefinition.Write.
    public struct AddEntry
    {
        public ushort ObjectType;
        public StatusEntry Status;
    }

    //[W07] Stats are a StatTable (typed slots); Mask is the snapshot of which ones this status carries: every
    //present stat (Full, SVs) or the ones changed since the last clear (Delta, NewSVs). The wire order is StatType order.
    public struct ObjectStatus
    {
        public int Id;
        public Position Position;
        public StatTable Stats;
        public StatMask Mask;

        public static ObjectStatus Full(int id, Position position, StatTable stats)
        {
            return new ObjectStatus { Id = id, Position = position, Stats = stats, Mask = stats.Present };
        }

        public static ObjectStatus Delta(int id, Position position, StatTable stats)
        {
            return new ObjectStatus { Id = id, Position = position, Stats = stats, Mask = stats.Dirty };
        }

        public void Write(PacketWriter wtr)
        {
            wtr.Write(Id);
            Position.Write(wtr);

            StatMask mask = Mask;
            wtr.Write((byte)mask.Count);
            for (int s = mask.NextSet(0); s >= 0; s = mask.NextSet(s + 1))
                Stats.WriteStat(wtr, s);
        }

        public static bool IsStringStat(StatType stat)
        {
            return StatTable.IsString(stat);
        }
    }

    public struct IntPoint
    {
        public int X;
        public int Y;

        public IntPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(IntPoint a, IntPoint b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(IntPoint a, IntPoint b) => a.X != b.X || a.Y != b.Y;

        public bool Equals(IntPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj)
        {
            if (obj is IntPoint p)
            {
                return Equals(p);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (Y << 16) ^ X;
        }

        public override string ToString()
        {
            return $"X:{X}, Y:{Y}";
        }
    }

    public struct SlotData
    {
        public int ObjectId;
        public byte SlotId;

        public SlotData(ref SpanReader rdr)
        {
            ObjectId = rdr.ReadInt32();
            SlotId = rdr.ReadByte();
        }
    }

    public struct TradeItem
    {
        public int Item;
        public int SlotType;
        public bool Tradeable;
        public bool Included;

        public static TradeItem Read(ref SpanReader rdr)
        {
            return new TradeItem
            {
                Item = rdr.ReadInt32(),
                SlotType = rdr.ReadInt32(),
                Tradeable = rdr.ReadBoolean(),
                Included = rdr.ReadBoolean()
            };
        }

        public void Write(PacketWriter wtr)
        {
            wtr.Write(Item);
            wtr.Write(SlotType);
            wtr.Write(Tradeable);
            wtr.Write(Included);
        }
    }

    public struct Position
    {
        public float X;
        public float Y;

        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Position(ref SpanReader rdr)
        {
            X = rdr.ReadSingle();
            Y = rdr.ReadSingle();
        }

        public void Write(PacketWriter wtr)
        {
            wtr.Write(X);
            wtr.Write(Y);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }

        public static Position operator -(Position value)
        {
            value.X = -value.X;
            value.Y = -value.Y;
            return value;
        }


        public static bool operator ==(Position value1, Position value2)
        {
            return value1.X == value2.X && value1.Y == value2.Y;
        }


        public static bool operator !=(Position value1, Position value2)
        {
            return value1.X != value2.X || value1.Y != value2.Y;
        }


        public static Position operator +(Position value1, Position value2)
        {
            value1.X += value2.X;
            value1.Y += value2.Y;
            return value1;
        }


        public static Position operator -(Position value1, Position value2)
        {
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            return value1;
        }


        public static Position operator *(Position value1, Position value2)
        {
            value1.X *= value2.X;
            value1.Y *= value2.Y;
            return value1;
        }


        public static Position operator *(Position value, float scaleFactor)
        {
            value.X *= scaleFactor;
            value.Y *= scaleFactor;
            return value;
        }


        public static Position operator *(float scaleFactor, Position value)
        {
            value.X *= scaleFactor;
            value.Y *= scaleFactor;
            return value;
        }


        public static Position operator /(Position value1, Position value2)
        {
            value1.X /= value2.X;
            value1.Y /= value2.Y;
            return value1;
        }


        public static Position operator /(Position value1, float divider)
        {
            float factor = 1 / divider;
            value1.X *= factor;
            value1.Y *= factor;
            return value1;
        }

        public static Position Lerp(Position value1, Position value2, float amount)
        {
            return new Position(
                MathUtils.Lerp(value1.X, value2.X, amount),
                MathUtils.Lerp(value1.Y, value2.Y, amount));
        }

        public static void Lerp(ref Position value1, ref Position value2, float amount, out Position result)
        {
            result = new Position(
                MathUtils.Lerp(value1.X, value2.X, amount),
                MathUtils.Lerp(value1.Y, value2.Y, amount));
        }

        public void Normalize()
        {
            //A zero/non-finite vector has no direction: leave it unchanged
            //instead of producing NaN (0 * Infinity), which would poison
            //entity positions and hang ResolveNewLocation forever.
            float lenSq = (X * X) + (Y * Y);
            if (!float.IsFinite(lenSq) || lenSq < 1e-12f)
                return;
            float val = 1.0f / (float)Math.Sqrt(lenSq);
            X *= val;
            Y *= val;
        }

        public static Position Normalize(Position value)
        {
            float lenSq = (value.X * value.X) + (value.Y * value.Y);
            if (!float.IsFinite(lenSq) || lenSq < 1e-12f)
                return value;
            float val = 1.0f / (float)Math.Sqrt(lenSq);
            value.X *= val;
            value.Y *= val;
            return value;
        }

        public static void Normalize(ref Position value, out Position result)
        {
            float lenSq = (value.X * value.X) + (value.Y * value.Y);
            if (!float.IsFinite(lenSq) || lenSq < 1e-12f)
            {
                result = value;
                return;
            }
            float val = 1.0f / (float)Math.Sqrt(lenSq);
            result.X = value.X * val;
            result.Y = value.Y * val;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return $"X:{X}, Y:{Y}";
        }
    }
}
