using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RotMG.Common
{
    //[W13] Leveled file logging behind Program.Print. The console path is unchanged.
    //
    //Layout (relative to the process cwd unless REALM_LOG_DIR is set):
    //  logs/debug/YYYY-MM-DD.log   Debug (Debug builds only; Release drops Debug before any sink)
    //  logs/info/YYYY-MM-DD.log
    //  logs/warn/YYYY-MM-DD.log
    //  logs/error/YYYY-MM-DD.log
    //Decision: each message goes to exactly one file, its own level's. Warn and Error are not copied
    //into info, so info stays a plain info log and error stays only errors. Debug has its own file
    //because it is high volume (per packet, per request) and would drown info.
    //
    //Line format: "2026-10-14T13:05:09.123 [WARN] <message>" in local time. A multi-line message keeps its
    //newlines; only the first line carries the prefix.
    //
    //Rotation: the active file is always YYYY-MM-DD.log. A record from a new local date opens that day's
    //file. When appending would take the active file past the size limit (default 64 MiB), the active file
    //is renamed to YYYY-MM-DD.N.log (N = highest existing + 1, so higher N is newer) and a new active file
    //starts. A restart appends to the active file.
    //
    //Retention (REALM_LOG_RETENTION_DAYS, default 30, 0 = keep everything; values above 36500 count as 36500).
    //The writer thread prunes once at startup and again whenever the local date changes. It deletes a file only
    //when every one of these holds:
    //  - it is directly inside one of this sink's own level directories (debug, info, warn, error). No recursion;
    //    subdirectories, including ones whose names look like log files, are never touched;
    //  - its name is exactly YYYY-MM-DD.log or YYYY-MM-DD.N.log (N digits only);
    //  - the date in its name is before today minus N days. With N=30 on 2026-10-15 the cutoff is 2026-09-15:
    //    2026-09-15 is kept, 2026-09-14 is deleted;
    //  - it is a regular file, not a symbolic link, and not the file a sink has open.
    //The logs root or a level directory that is itself a symbolic link is skipped, and nothing is followed.
    //Each prune that deletes files adds one info line with the count. A retention problem (for example a
    //file that cannot be deleted) prints one console line for the process and never stops logging.
    //
    //Threading: Program.Print calls Enqueue on the calling thread. It does no file I/O; it allocates one
    //record, enqueues it and signals the writer. A single LogWriter thread owns every FileStream: it opens, rotates, writes,
    //flushes and closes. The writer flushes at least once a second and on Shutdown, so a process crash
    //loses about one second at most.
    //
    //Overflow: the queue is bounded by records and by characters. When it is full, the record is dropped
    //and counted, and the writer adds one warn line with the count since the last report.
    //
    //Failure: the first file I/O exception (disk full, permission denied, ...) disables all file sinks
    //and prints one console line. Nothing else changes; the server keeps running.
    //
    //Environment (read once in Start; all optional):
    //  REALM_LOG_FILES=0            turn the file sinks off
    //  REALM_LOG_DIR=<path>         log root (default <cwd>/logs)
    //  REALM_LOG_MAX_BYTES=<n>      rotation size (default 67108864)
    //  REALM_LOG_QUEUE_MAX=<n>      queue limit in records (default 65536; also capped at 4M chars)
    //  REALM_LOG_RETENTION_DAYS=<n> keep this many days of dated files, 0 = keep all (default 30); invalid = 30
    //  REALM_LOG_CLOCK_OFFSET_S=<s> test hook: seconds added to the clock used for file stamps and file
    //                               dates, so a date rollover can happen during a short run. The console
    //                               clock is not affected.
    public static class LogFiles
    {
        public const long DefaultMaxFileBytes = 64L * 1024 * 1024;
        public const int DefaultQueueRecords = 65536;
        public const int MaxQueueChars = 4 * 1024 * 1024;
        public const int DefaultRetentionDays = 30;
        private const int MaxRetentionDays = 36500;
        private const int FlushIntervalMs = 1000;
        private const int ShutdownJoinMs = 3000;

        //YYYY-MM-DD.log or YYYY-MM-DD.N.log. Anchored with \z so a trailing newline cannot match.
        private static readonly Regex DatedLogName = new Regex(@"^([0-9]{4}-[0-9]{2}-[0-9]{2})(\.[0-9]+)?\.log\z", RegexOptions.CultureInvariant);

        private struct Record
        {
            public DateTime Stamp;
            public PrintType Level;
            public string Text;
        }

        private sealed class Sink
        {
            public string Dir;
            public string Tag;
            //Writer thread only.
            public FileStream Stream;
            public DateTime Day;
            public long Size;
        }

        private static readonly ConcurrentQueue<Record> Queue = new ConcurrentQueue<Record>();
        private static readonly AutoResetEvent Wake = new AutoResetEvent(false);
        //Indexed by (int)PrintType: Debug, Info, Warn, Error.
        private static readonly Sink[] Sinks = new Sink[4];

        private static Thread _writer;
        private static string _root;
        private static long _maxBytes = DefaultMaxFileBytes;
        private static int _queueRecords = DefaultQueueRecords;
        private static double _clockOffsetSeconds;
        private static int _retentionDays = DefaultRetentionDays;
        //Writer thread only: set once a retention problem has been printed to the console.
        private static int _retentionNoted;

        private static volatile bool _enabled;
        private static volatile bool _stopping;
        private static int _disabled;
        private static int _queued;
        private static int _queuedChars;
        private static long _dropped;
        //Writer thread only.
        private static long _reportedDropped;

        //Called once from Program.Main, before the server threads start. Starts the writer thread.
        public static void Start()
        {
            if (Environment.GetEnvironmentVariable("REALM_LOG_FILES") == "0")
                return;
            try
            {
                string dir = Environment.GetEnvironmentVariable("REALM_LOG_DIR");
                _root = Path.GetFullPath(string.IsNullOrWhiteSpace(dir) ? "logs" : dir);
                _maxBytes = Math.Max(1, ReadLong("REALM_LOG_MAX_BYTES", DefaultMaxFileBytes));
                _queueRecords = (int)Math.Max(1, Math.Min(int.MaxValue, ReadLong("REALM_LOG_QUEUE_MAX", DefaultQueueRecords)));
                _clockOffsetSeconds = ReadDouble("REALM_LOG_CLOCK_OFFSET_S");
                _retentionDays = ReadRetentionDays();

                Sinks[(int)PrintType.Debug] = new Sink { Dir = Path.Combine(_root, "debug"), Tag = "DEBUG" };
                Sinks[(int)PrintType.Info] = new Sink { Dir = Path.Combine(_root, "info"), Tag = "INFO" };
                Sinks[(int)PrintType.Warn] = new Sink { Dir = Path.Combine(_root, "warn"), Tag = "WARN" };
                Sinks[(int)PrintType.Error] = new Sink { Dir = Path.Combine(_root, "error"), Tag = "ERROR" };

                _writer = new Thread(WriterMain)
                {
                    Name = "LogWriter",
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal
                };
                _writer.Start();
                _enabled = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"<log files disabled at startup: {e.GetType().Name}: {e.Message}>");
            }
        }

        //Any thread. Never blocks, never does file I/O. Called by Program.Print with the same DateTime.Now
        //that the console line uses, and the message text.
        public static void Enqueue(PrintType level, DateTime now, string text)
        {
            if (!_enabled || _stopping || Volatile.Read(ref _disabled) != 0)
                return;
            DateTime stamp = Shift(now);
            int chars = text.Length;
            int count = Interlocked.Increment(ref _queued);
            int totalChars = Interlocked.Add(ref _queuedChars, chars);
            if (count > _queueRecords || totalChars > MaxQueueChars)
            {
                Interlocked.Decrement(ref _queued);
                Interlocked.Add(ref _queuedChars, -chars);
                Interlocked.Increment(ref _dropped);
                return;
            }
            Queue.Enqueue(new Record { Stamp = stamp, Level = level, Text = text });
            Wake.Set();
        }

        //Main thread, at shutdown (Program.Terminate). Drains what is queued, flushes and closes the files.
        public static void Shutdown()
        {
            if (!_enabled || _stopping)
                return;
            _stopping = true;
            Wake.Set();
            Thread w = _writer;
            if (w != null && w != Thread.CurrentThread && !w.Join(ShutdownJoinMs))
                Console.WriteLine("<log writer did not finish in time; the end of the log may be missing>");
        }

        private static DateTime Shift(DateTime now) =>
            _clockOffsetSeconds == 0 ? now : now.AddSeconds(_clockOffsetSeconds);

        private static void WriterMain()
        {
            try
            {
                WriterLoop();
            }
            catch (Exception e)
            {
                Disable(e);
            }
            CloseAll();
        }

        private static void WriterLoop()
        {
            long lastFlush = Environment.TickCount64;
            //Writer thread only: the local date of the last prune. The first pass prunes at startup.
            DateTime pruneDay = DateTime.MinValue;
            while (true)
            {
                Wake.WaitOne(FlushIntervalMs);
                bool stopping = _stopping;
                if (!stopping && Volatile.Read(ref _disabled) == 0)
                {
                    DateTime today = Shift(DateTime.Now).Date;
                    if (today != pruneDay)
                    {
                        pruneDay = today;
                        Prune(today);
                    }
                }
                while (Queue.TryDequeue(out Record r))
                {
                    Interlocked.Decrement(ref _queued);
                    Interlocked.Add(ref _queuedChars, -r.Text.Length);
                    if (Volatile.Read(ref _disabled) == 0)
                        Write(r);
                    if (Environment.TickCount64 - lastFlush >= FlushIntervalMs)
                    {
                        FlushAll();
                        lastFlush = Environment.TickCount64;
                    }
                }
                ReportDrops();
                if (stopping || Environment.TickCount64 - lastFlush >= FlushIntervalMs)
                {
                    FlushAll();
                    lastFlush = Environment.TickCount64;
                }
                if (stopping)
                    break;
            }
        }

        //Writer thread only.
        private static void Write(Record r)
        {
            try
            {
                Sink sink = Sinks[(int)r.Level];
                string line = r.Stamp.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture)
                    + " [" + sink.Tag + "] " + r.Text + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                DateTime day = r.Stamp.Date;
                if (sink.Stream == null || sink.Day != day)
                {
                    CloseSink(sink);
                    OpenSink(sink, day);
                }
                if (sink.Size > 0 && sink.Size + bytes.Length > _maxBytes)
                    Rotate(sink);
                AssertWriterThread();
                sink.Stream.Write(bytes, 0, bytes.Length);
                sink.Size += bytes.Length;
            }
            catch (Exception e)
            {
                Disable(e);
            }
        }

        //Writer thread only.
        private static void OpenSink(Sink sink, DateTime day)
        {
            AssertWriterThread();
            Directory.CreateDirectory(sink.Dir);
            string path = Path.Combine(sink.Dir, DayName(day) + ".log");
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096);
            sink.Stream = fs;
            sink.Day = day;
            sink.Size = fs.Length;
        }

        //Writer thread only. Renames the active file to the next free YYYY-MM-DD.N.log, then reopens.
        private static void Rotate(Sink sink)
        {
            AssertWriterThread();
            string day = DayName(sink.Day);
            CloseSink(sink);
            int next = 1;
            foreach (string path in Directory.EnumerateFiles(sink.Dir, day + ".*.log"))
            {
                string name = Path.GetFileName(path);
                string middle = name.Substring(day.Length + 1, name.Length - day.Length - 1 - 4);
                if (int.TryParse(middle, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n >= next)
                    next = n + 1;
            }
            File.Move(Path.Combine(sink.Dir, day + ".log"), Path.Combine(sink.Dir, day + "." + next + ".log"));
            OpenSink(sink, sink.Day);
        }

        //Writer thread only. Applies REALM_LOG_RETENTION_DAYS; the rules are in the header. Never throws.
        private static void Prune(DateTime today)
        {
            AssertWriterThread();
            if (_retentionDays <= 0)
                return;
            DateTime cutoff = today.AddDays(-_retentionDays);
            int removed = 0;
            try
            {
                if (IsLink(_root))
                {
                    NoteOnce($"log retention skipped: {_root} is a symbolic link, nothing under it is pruned");
                    return;
                }
                foreach (Sink sink in Sinks)
                {
                    if (IsLink(sink.Dir))
                    {
                        NoteOnce($"log retention skipped: {sink.Dir} is a symbolic link");
                        continue;
                    }
                    if (!Directory.Exists(sink.Dir))
                        continue;
                    try
                    {
                        removed += PruneDir(sink.Dir, cutoff, sink.Stream?.Name);
                    }
                    catch (Exception e)
                    {
                        NoteOnce($"log retention could not scan {sink.Dir}: {e.GetType().Name}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                NoteOnce($"log retention stopped: {e.GetType().Name}: {e.Message}");
            }
            if (removed > 0)
            {
                Write(new Record
                {
                    Stamp = Shift(DateTime.Now),
                    Level = PrintType.Info,
                    Text = $"<log> retention removed {removed} file(s) dated before {DayName(cutoff)} (REALM_LOG_RETENTION_DAYS={_retentionDays})"
                });
            }
        }

        //Writer thread only. Lists the top level of one level directory only (no recursion) and deletes the
        //matching files that are older than the cutoff. activePath is the file the sink has open, if any.
        private static int PruneDir(string dir, DateTime cutoff, string activePath)
        {
            int removed = 0;
            //Collected first, so nothing is deleted while the directory is being enumerated.
            foreach (string path in Directory.GetFiles(dir))
            {
                Match m = DatedLogName.Match(Path.GetFileName(path));
                if (!m.Success)
                    continue;
                if (!DateTime.TryParseExact(m.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime day))
                    continue;
                if (day >= cutoff)
                    continue;
                FileInfo fi = new FileInfo(path);
                if (fi.LinkTarget != null || (fi.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
                if (string.Equals(fi.FullName, activePath, StringComparison.Ordinal))
                    continue;
                try
                {
                    fi.Delete();
                    removed++;
                }
                catch (Exception e)
                {
                    NoteOnce($"log retention could not delete {path}: {e.GetType().Name}: {e.Message}");
                }
            }
            return removed;
        }

        //True for a symbolic link, file or directory, including a dangling one. Such paths are never followed.
        private static bool IsLink(string path)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            if (info.LinkTarget != null)
                return true;
            return info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0;
        }

        //Writer thread only. One console line for the first retention problem in the process.
        private static void NoteOnce(string message)
        {
            if (Interlocked.Exchange(ref _retentionNoted, 1) != 0)
                return;
            try
            {
                Console.WriteLine($"<{message}>");
            }
            catch { }
        }

        //Writer thread only.
        private static void FlushAll()
        {
            AssertWriterThread();
            foreach (Sink sink in Sinks)
            {
                if (sink == null || sink.Stream == null)
                    continue;
                try
                {
                    sink.Stream.Flush();
                }
                catch (Exception e)
                {
                    Disable(e);
                    return;
                }
            }
        }

        //Writer thread only.
        private static void CloseSink(Sink sink)
        {
            FileStream fs = sink.Stream;
            sink.Stream = null;
            if (fs == null)
                return;
            AssertWriterThread();
            try
            {
                fs.Dispose();
            }
            catch (Exception e)
            {
                Disable(e);
            }
        }

        //Writer thread only.
        private static void CloseAll()
        {
            foreach (Sink sink in Sinks)
            {
                if (sink != null)
                    CloseSink(sink);
            }
        }

        //Writer thread only. Reports the drop count since the last report to the warn file.
        private static void ReportDrops()
        {
            if (Volatile.Read(ref _disabled) != 0)
                return;
            long total = Interlocked.Read(ref _dropped);
            if (total == _reportedDropped)
                return;
            long since = total - _reportedDropped;
            _reportedDropped = total;
            Write(new Record
            {
                Stamp = Shift(DateTime.Now),
                Level = PrintType.Warn,
                Text = $"<log> dropped {since} line(s) because the writer fell behind (total dropped {total}; queue limit {_queueRecords} records / {MaxQueueChars} chars)"
            });
        }

        //Writer thread only. First failure wins: the file sinks stop, the console keeps working.
        private static void Disable(Exception e)
        {
            if (Interlocked.Exchange(ref _disabled, 1) != 0)
                return;
            try
            {
                Console.WriteLine($"<log files disabled: {e.GetType().Name}: {e.Message}; console output continues; logs under {_root} stop here>");
            }
            catch { }
            CloseAll();
        }

        //DEBUG-only inverse of Program.AssertMainThread: file I/O must run on the LogWriter thread,
        //never on the main (tick) thread or any other thread.
        [Conditional("DEBUG")]
        private static void AssertWriterThread()
        {
            if (Program.IsMainThread || Thread.CurrentThread != _writer)
                throw new InvalidOperationException($"log file I/O on tid {Thread.CurrentThread.ManagedThreadId} (main {(Program.IsMainThread ? "yes" : "no")}, writer {_writer?.ManagedThreadId})");
        }

        private static string DayName(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static long ReadLong(string name, long fallback)
        {
            string raw = Environment.GetEnvironmentVariable(name);
            return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) && value > 0 ? value : fallback;
        }

        //0 means keep everything. Invalid or negative values use the default; huge values are clamped so the cutoff stays representable.
        private static int ReadRetentionDays()
        {
            string raw = Environment.GetEnvironmentVariable("REALM_LOG_RETENTION_DAYS");
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value < 0)
                return DefaultRetentionDays;
            return Math.Min(value, MaxRetentionDays);
        }

        private static double ReadDouble(string name)
        {
            string raw = Environment.GetEnvironmentVariable(name);
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0;
        }
    }
}
