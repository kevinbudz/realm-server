using RotMG.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RotMG.Game
{
    //Tick timing for stress validation. Recording is allocation-free
    //(timestamp deltas into plain fields); aggregation happens on the
    //main thread at log/query time. Query via Snapshot()/Summary() or
    //the in-game /perf command; periodic lines go to the server log.
    public static class ServerPerf
    {
        public struct WorldSample
        {
            public int Id;
            public string Name;
            public int Players;
            public double BroadcastMs;
            public double EntityMs;
            public double TotalMs;
        }

        private static double _tickAvgMs;
        private static double _tickMaxMs;
        private static int _ticksInWindow;
        private static int _lastLogTick;
        private const int LogEveryTicks = 300; //30s at 10 TPS

        private static double ToMs(long from, long to)
        {
            return (to - from) * 1000.0 / Stopwatch.Frequency;
        }

        public static long Stamp()
        {
            return Stopwatch.GetTimestamp();
        }

        public static double ElapsedMs(long from, long to)
        {
            return ToMs(from, to);
        }

        //Called once per Manager tick with the measured body duration.
        public static void EndTick(double tickMs)
        {
            _ticksInWindow++;
            if (_ticksInWindow == 1)
                _tickAvgMs = tickMs;
            else
                _tickAvgMs += (tickMs - _tickAvgMs) / Math.Min(_ticksInWindow, 300);
            if (tickMs > _tickMaxMs)
                _tickMaxMs = tickMs;

            if (Manager.TotalTicks - _lastLogTick >= LogEveryTicks)
            {
                _lastLogTick = Manager.TotalTicks;
                LogWindow();
            }
        }

        private static List<WorldSample> SampleWorlds()
        {
            List<WorldSample> samples = new List<WorldSample>(Manager.Worlds.Count);
            foreach (World world in Manager.Worlds.Values)
            {
                samples.Add(new WorldSample
                {
                    Id = world.Id,
                    Name = world.GetDisplayName(),
                    Players = world.Players.Count,
                    BroadcastMs = world.LastBroadcastMs,
                    EntityMs = world.LastEntityMs,
                    TotalMs = world.LastTickMs
                });
            }
            return samples;
        }

        private static void LogWindow()
        {
            List<WorldSample> samples = SampleWorlds();
            int players = 0;
            WorldSample worst = default;
            foreach (WorldSample s in samples)
            {
                players += s.Players;
                if (s.TotalMs > worst.TotalMs)
                    worst = s;
            }
            long memMb = GC.GetTotalMemory(false) / (1024 * 1024);
            Program.Print(PrintType.Info,
                $"[perf] tick avg {_tickAvgMs:F1}ms max {_tickMaxMs:F1}ms (budget {Settings.MillisecondsPerTick}ms)" +
                $" | worlds {samples.Count} players {players}" +
                (samples.Count == 0 ? "" : $" | worst <{worst.Name}:{worst.Id}> {worst.TotalMs:F1}ms (b {worst.BroadcastMs:F1}/e {worst.EntityMs:F1})") +
                $" | mem {memMb}MB gen2 {GC.CollectionCount(2)}");
            _tickMaxMs = 0;
            _ticksInWindow = 0;
        }

        //One-shot multi-line summary for the /perf chat command.
        public static List<string> Summary()
        {
            List<string> lines = new List<string>
            {
                $"tick avg {_tickAvgMs:F1}ms max {_tickMaxMs:F1}ms (budget {Settings.MillisecondsPerTick}ms)"
            };
            List<WorldSample> samples = SampleWorlds();
            int players = 0;
            foreach (WorldSample s in samples)
                players += s.Players;
            lines.Add($"worlds {samples.Count} players {players} mem {GC.GetTotalMemory(false) / (1024 * 1024)}MB");
            foreach (WorldSample s in samples.OrderByDescending(s => s.TotalMs).Take(5))
                lines.Add($"<{s.Name}:{s.Id}> p{s.Players} {s.TotalMs:F1}ms (b {s.BroadcastMs:F1}/e {s.EntityMs:F1})");
            return lines;
        }
    }
}
