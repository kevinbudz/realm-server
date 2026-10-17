using Microsoft.Data.Sqlite;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace RotMG.Common
{
    //SQLite (WAL mode) key/value storage system. Each legacy `.file` key is one row.
    public static partial class Database
    {
        private const int MaxLegends = 20;
        private const int MinFameRequiredToEnterLegends = 0;
        private static readonly Dictionary<string, TimeSpan> TimeSpans = new Dictionary<string, TimeSpan>()
        {
            {"week", TimeSpan.FromDays(7) },
            {"month", TimeSpan.FromDays(30) },
            {"all", TimeSpan.MaxValue }
        };

        private static readonly Dictionary<string, XElement> FameLists = new Dictionary<string, XElement>
        {
            { "week", null },
            { "month", null },
            { "all", null }
        };

        private static readonly HashSet<int> Legends = new HashSet<int>();

        private const int MaxInvalidLoginAttempts = 5;
        private static Dictionary<string, byte> InvalidLoginAttempts;

        private const int MaxRegisteredAccounts = 1;
        private static Dictionary<string, byte> RegisteredAccounts;

        private const int ResetCooldown = 60000 * 5; //5 minutes
        private static int ResetTime;

        private const int CharSlotPrice = 2000; //Fame
        private const int SkinPrice = 1000; //Credits

        //Bounds crash-loss and WAL growth. Autosave persists every connected
        //player (account + character, atomically per player); the checkpoint
        //sweep keeps the WAL file from growing without bound when the server
        //never shuts down cleanly (kill -9 never runs Shutdown).
        private const int AutosaveIntervalMS = 60000;
        private const int CheckpointIntervalMS = 60000;
        private static int _lastAutosave;
        private static int _lastCheckpoint;

        //Serializes all database access. This is what makes the single-writer
        //limit of SQLite a non-issue: writes queue here instead of hitting SQLITE_BUSY.
        private static readonly object _lock = new object();
        private static string _connectionString;

        //Dedicated writer thread (see WriterLoop): all multi-key bundles go
        //through _writerQueue instead of fsyncing on the tick thread. FIFO
        //order is preserved, so causality holds (a loot commit can never
        //persist ahead of the drop commit that created the bag). Plain key
        //bundles merge into shared group commits; read-modify-write funcs
        //keep their own transaction each, applied in arrival order.
        private static BlockingCollection<DbJob> _writerQueue;
        private static Thread _writerThread;
        private static int _writerStarted;

        public static void Init()
        {
            InvalidLoginAttempts = new Dictionary<string, byte>();
            RegisteredAccounts = new Dictionary<string, byte>();
            if (!string.IsNullOrWhiteSpace(Settings.DatabaseDirectory) && !Directory.Exists(Settings.DatabaseDirectory))
                Directory.CreateDirectory(Settings.DatabaseDirectory);

            //Private (default) cache, NOT Shared: with shared-cache mode every
            //connection in the process shares table locks, so a nested read
            //on a second connection while a write transaction is open fails
            //instantly with SQLITE_LOCKED instead of waiting. WAL mode plus
            //the _lock-serialized writers below already give us concurrent
            //readers with one writer and busy_timeout backoff.
            //Default Timeout caps a command blocked behind a long writer commit at 5 s (the Microsoft.Data.Sqlite
            //default is 30 s). The per-connection busy handler is set in OpenConnection (W01: 1 ms sleeps).
            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder
            {
                DataSource = Settings.DatabasePath,
                DefaultTimeout = 5
            };
            _connectionString = builder.ToString();

            lock (_lock)
            {
                using (SqliteConnection conn = OpenConnection())
                {
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE IF NOT EXISTS kv(path TEXT PRIMARY KEY, value TEXT NOT NULL);";
                        cmd.ExecuteNonQuery();
                    }
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA journal_mode=WAL;";
                        cmd.ExecuteScalar();
                    }
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        //FULL: every committed transaction survives a power
                        //loss, not just an app crash. NORMAL only guarantees
                        //the latter, which is exactly the outage this server
                        //must ride through. Write volume here is tiny (a few
                        //small rows per player action), so the extra fsync
                        //cost is negligible.
                        cmd.CommandText = "PRAGMA synchronous=FULL;";
                        cmd.ExecuteNonQuery();
                    }
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        //Checkpoint roughly every 1000 WAL pages even if the
                        //periodic PASSIVE sweep below never runs.
                        cmd.CommandText = "PRAGMA wal_autocheckpoint=1000;";
                        cmd.ExecuteNonQuery();
                    }
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        //Bound the WAL file size so a long-lived server that
                        //never shuts down cleanly cannot fill the disk.
                        cmd.CommandText = "PRAGMA journal_size_limit=33554432;";
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            MigrateLegacyFiles();

            CreateKey("nextAccId", "0", true);
            CreateKey("news", "", true);

            foreach (string span in TimeSpans.Keys)
                CreateKey($"legends.{span}", "", true);

            FlushLegends();
            StartWriter();
        }

        public static void Shutdown()
        {
            StopWriter();
            try
            {
                lock (_lock)
                {
                    using (SqliteConnection conn = OpenConnection())
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private static SqliteConnection OpenConnection()
        {
            SqliteConnection conn = new SqliteConnection(_connectionString);
            conn.Open();
            //Per-connection pragmas: every helper below opens a fresh
            //connection, so anything that is not persisted in the DB header
            //must be set here. (journal_mode and journal_size_limit persist;
            //synchronous persists too, but re-asserting it is free.)
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA synchronous = FULL; PRAGMA wal_autocheckpoint = 1000;";
                cmd.ExecuteNonQuery();
            }
            //W01: the bundled e_sqlite3 sleeps in whole seconds in its default busy handler, so a 6 ms writer
            //commit stalled a reader for 1 s. This 1 ms handler must come AFTER the PRAGMA batch (a busy_timeout
            //pragma would replace it). Default Timeout=5 (connection string) bounds the total wait.
            int rc = sqlite3_busy_handler(conn.Handle.DangerousGetHandle(), BusyStepCallback, IntPtr.Zero);
            if (rc != 0)
                throw new InvalidOperationException("sqlite3_busy_handler rc=" + rc);
            return conn;
        }

        //Native busy handler (SQLitePCLRaw 2.1.6 has no managed binding). BusyStepCallback is a static field so the
        //delegate stays rooted: a collected delegate aborts the process.
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private delegate int BusyCallback(IntPtr pArg, int count);

        [System.Runtime.InteropServices.DllImport("e_sqlite3", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern int sqlite3_busy_handler(IntPtr db, BusyCallback cb, IntPtr pArg);

        private static readonly BusyCallback BusyStepCallback = BusyStep;
        [ThreadStatic] private static long _busyStartTicks;
        private const double BusyLimitMs = 5000;

        private static int BusyStep(IntPtr pArg, int count)
        {
            if (count == 0)
                _busyStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            double elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - _busyStartTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            if (elapsedMs + 1 > BusyLimitMs)
                return 0;
            Thread.Sleep(1);
            return 1;
        }

        private static string GetKeyInTx(SqliteConnection conn, string combinedKey)
        {
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT value FROM kv WHERE path = @p;";
                cmd.Parameters.AddWithValue("@p", combinedKey);
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : (string)result;
            }
        }

        private static void UpsertKeyInTx(SqliteConnection conn, string combinedKey, string value)
        {
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO kv(path, value) VALUES(@p, @v) ON CONFLICT(path) DO UPDATE SET value = excluded.value;";
                cmd.Parameters.AddWithValue("@p", combinedKey);
                cmd.Parameters.AddWithValue("@v", value ?? "");
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteKeyInTx(SqliteConnection conn, string combinedKey)
        {
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM kv WHERE path = @p;";
                cmd.Parameters.AddWithValue("@p", combinedKey);
                cmd.ExecuteNonQuery();
            }
        }

        //One queued unit for the writer thread. PlainJobs carry pre-built
        //strings only (no game objects): the caller serializes first, so the
        //writer never touches game state. FuncJobs carry a read-modify-write
        //lambda that runs on the writer's connection inside its own
        //transaction. Completions run on the main thread (never the writer),
        //so they may touch game state; blocking waiters are signaled inline.
        private abstract class DbJob
        {
            public Action Completion;
            //Commit outcome for callers that gate an acknowledgement on the write: true after the commit, false when it
            //failed (nothing persisted). Runs on the main thread like Completion, but on failure too.
            public Action<bool> Outcome;
        }

        private sealed class PlainJob : DbJob
        {
            public Dictionary<string, string> Writes;
            public List<string> Deletes;
            public long Seq;
        }

        //Read-your-writes overlay (W03). A plain bundle is visible to GetKey from the moment it is queued until its
        //commit attempt ends, so a main-thread read-then-write sees the newest value without waiting for the writer.
        //Only the newest bundle per key is kept (Seq); an older bundle's release leaves a newer entry alone.
        private sealed class PendingWrite
        {
            public long Seq;
            public string Value;
            public bool Deleted;
        }
        private static readonly object _pendingLock = new object();
        private static readonly Dictionary<string, PendingWrite> _pending = new Dictionary<string, PendingWrite>();
        private static long _jobSeq;

        private sealed class FuncJob : DbJob
        {
            public Action<SqliteConnection> Work;
            //Keys this job will write with values already known on the main thread (W03 overlay). Released when the job ends.
            public Dictionary<string, string> Declared;
            public long Seq;
            public Exception Error;
        }

        private static void StartWriter()
        {
            if (Interlocked.Exchange(ref _writerStarted, 1) == 1)
                return;
            _writerQueue = new BlockingCollection<DbJob>(new ConcurrentQueue<DbJob>());
            _writerThread = new Thread(WriterLoop)
            {
                IsBackground = true,
                Name = "DatabaseWriter",
                Priority = ThreadPriority.AboveNormal
            };
            _writerThread.Start();
        }

        private static void StopWriter()
        {
            BlockingCollection<DbJob> queue = _writerQueue;
            Thread thread = _writerThread;
            if (queue == null || thread == null)
                return;
            try { queue.CompleteAdding(); } catch { }
            try
            {
                //Drain first: disconnect/death saves queued above must commit
                //before the TRUNCATE checkpoint in Shutdown runs.
                if (!thread.Join(15000))
                {
#if DEBUG
                    try { Console.WriteLine("<Database writer did not stop in time>"); } catch { }
#endif
                }
            }
            catch { }
        }

        //Owns one persistent connection and commits jobs FIFO. Consecutive
        //plain bundles share one IMMEDIATE transaction (group commit: N loot
        //saves cost one fsync); func jobs keep solo transactions, applied in
        //arrival order between the groups. A failed group rolls back and
        //retries member-by-member so one bad bundle cannot drop the rest.
        private static void WriterLoop()
        {
            try
            {
                using (SqliteConnection conn = OpenConnection())
                {
                    while (true)
                    {
                        //Timed take so the periodic WAL checkpoint runs here, on the writer, instead of on the tick thread.
                        if (!_writerQueue.TryTake(out DbJob first, 1000))
                        {
                            if (_writerQueue.IsCompleted)
                                break;
                            MaybeCheckpoint(conn);
                            continue;
                        }
                        List<DbJob> batch = new List<DbJob>(64);
                        batch.Add(first);
                        while (batch.Count < 512 && _writerQueue.TryTake(out DbJob extra))
                            batch.Add(extra);
                        long hopT0 = HopTrace.Enabled ? HopTrace.Now() : 0; //[hop]
                        try
                        {
                            RunBatch(conn, batch);
                        }
                        catch (Exception e)
                        {
                            //One bad batch must not stop the writer or leave its overlay entries behind.
                            FailJobs(batch, e);
                        }
                        if (HopTrace.Enabled) HopTrace.Batch(batch.Count, CountPlainJobs(batch), hopT0); //[hop]
                    }
                }
            }
#if DEBUG
            catch (Exception e)
            {
                try { Console.WriteLine($"<Database writer died> {e}"); } catch { }
            }
#else
            catch { }
#endif
            //The writer is gone. Stop accepting jobs (Enqueue then commits on the caller) and fail whatever is still
            //queued, so the overlay empties and no HTTP reply waits for a commit that will never happen.
            try { _writerQueue.CompleteAdding(); } catch { }
            List<DbJob> stranded = new List<DbJob>();
            while (_writerQueue != null && _writerQueue.TryTake(out DbJob rest))
                stranded.Add(rest);
            if (stranded.Count > 0)
                FailJobs(stranded, null);
        }

        //Runs on the writer thread between batches, outside any transaction. Keeps the WAL bounded on a server that
        //runs for weeks without a clean shutdown (Shutdown still does the TRUNCATE checkpoint).
        private static void MaybeCheckpoint(SqliteConnection conn)
        {
            if (Environment.TickCount - _lastCheckpoint < CheckpointIntervalMS)
                return;
            _lastCheckpoint = Environment.TickCount;
            try
            {
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA wal_checkpoint(PASSIVE);";
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static int CountPlainJobs(List<DbJob> batch) //[hop]
        {
            if (!HopTrace.Active) //[hop]
                return 0; //[hop]
            int plain = 0; //[hop]
            foreach (DbJob job in batch) //[hop]
                if (job is PlainJob) //[hop]
                    plain++; //[hop]
            return plain; //[hop]
        } //[hop]

        private static void RunBatch(SqliteConnection conn, List<DbJob> batch)
        {
            List<Action> completions = new List<Action>();
            int i = 0;
            while (i < batch.Count)
            {
                if (batch[i] is PlainJob)
                {
                    int j = i;
                    while (j < batch.Count && batch[j] is PlainJob)
                        j++;
                    if (!CommitPlains(conn, batch, i, j, completions))
                        for (int k = i; k < j; k++)
                            CommitOnePlain(conn, (PlainJob)batch[k], completions);
                    i = j;
                }
                else
                {
                    RunFunc(conn, (FuncJob)batch[i], completions);
                    i++;
                }
            }
            //Game-state completions hop to the main thread even when the
            //batch committed on the writer: AckedInvSwap /DelayedDrop code
            //must run where worlds are safe to touch.
            foreach (Action completion in completions)
            {
                try { Manager.RunOnMainThread(completion); }
                catch { }
            }
        }

        private static void BeginImmediate(SqliteConnection conn)
        {
            using (SqliteCommand begin = conn.CreateCommand())
            {
                begin.CommandText = "BEGIN IMMEDIATE;";
                begin.ExecuteNonQuery();
            }
        }

        private static void Commit(SqliteConnection conn)
        {
            using (SqliteCommand commit = conn.CreateCommand())
            {
                commit.CommandText = "COMMIT;";
                commit.ExecuteNonQuery();
            }
        }

        private static void Rollback(SqliteConnection conn)
        {
            try
            {
                using (SqliteCommand rollback = conn.CreateCommand())
                {
                    rollback.CommandText = "ROLLBACK;";
                    rollback.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static void ApplyPlain(SqliteConnection conn, PlainJob job)
        {
            if (job.Writes != null)
                foreach (KeyValuePair<string, string> kv in job.Writes)
                    UpsertKeyInTx(conn, kv.Key, kv.Value);
            if (job.Deletes != null)
                foreach (string key in job.Deletes)
                    DeleteKeyInTx(conn, key);
        }

        private static bool CommitPlains(SqliteConnection conn, List<DbJob> batch, int from, int to, List<Action> completions)
        {
            BeginImmediate(conn);
            try
            {
                for (int k = from; k < to; k++)
                    ApplyPlain(conn, (PlainJob)batch[k]);
                Commit(conn);
            }
            catch
            {
                Rollback(conn);
                return false;
            }
            for (int k = from; k < to; k++)
            {
                PlainJob job = (PlainJob)batch[k];
                ReleasePending(job);
                if (job.Completion != null)
                    completions.Add(job.Completion);
                if (job.Outcome != null)
                    completions.Add(() => job.Outcome(true));
            }
            return true;
        }

        private static void CommitOnePlain(SqliteConnection conn, PlainJob job, List<Action> completions)
        {
            bool committed = false;
            try
            {
                BeginImmediate(conn);
                try
                {
                    ApplyPlain(conn, job);
                    Commit(conn);
                }
                catch
                {
                    Rollback(conn);
                    throw;
                }
                committed = true;
            }
#if DEBUG
            catch (Exception e)
            {
                try { Console.WriteLine($"<Database write dropped> {e.Message}"); } catch { }
            }
#else
            catch { }
#endif
            ReleasePending(job);
            if (committed && job.Completion != null)
                completions.Add(job.Completion);
            if (job.Outcome != null)
                completions.Add(() => job.Outcome(committed));
        }

        private static void RunFunc(SqliteConnection conn, FuncJob job, List<Action> completions)
        {
            try
            {
                BeginImmediate(conn);
                try
                {
                    job.Work(conn);
                    Commit(conn);
                }
                catch (Exception e)
                {
                    Rollback(conn);
                    job.Error = e;
                    throw;
                }
                if (job.Completion != null)
                    completions.Add(job.Completion);
            }
            catch
            {
                if (job.Error == null)
                    job.Error = new Exception("Database func failed");
            }
            finally
            {
                ReleaseKeys(job.Declared?.Keys, null, job.Seq);
                if (job.Outcome != null)
                {
                    bool ok = job.Error == null;
                    completions.Add(() => job.Outcome(ok));
                }
            }
        }


        //Fire-and-forget read-modify-write on the writer thread. The optional
        //completion hops to the main thread after the commit.
        private static void TransactAsync(Action<SqliteConnection> work, Action onCommitted = null, Dictionary<string, string> declared = null)
        {
            if (Thread.CurrentThread == _writerThread)
            {
                RunFuncDirect(work);
                if (onCommitted != null)
                {
                    try { Manager.RunOnMainThread(onCommitted); } catch { }
                }
                return;
            }
            FuncJob job = new FuncJob { Work = work, Completion = onCommitted, Declared = declared, Seq = Interlocked.Increment(ref _jobSeq) };
            if (declared != null)
            {
                lock (_pendingLock)
                    foreach (KeyValuePair<string, string> kv in declared)
                        _pending[kv.Key] = new PendingWrite { Seq = job.Seq, Value = kv.Value ?? "" };
            }
            Enqueue(job);
        }

        private static void RunFuncDirect(Action<SqliteConnection> work)
        {
            using (SqliteConnection conn = OpenConnection())
            {
                BeginImmediate(conn);
                try
                {
                    work(conn);
                    Commit(conn);
                }
                catch
                {
                    Rollback(conn);
                    throw;
                }
            }
        }

        private static void Enqueue(DbJob job)
        {
            BlockingCollection<DbJob> queue = _writerQueue;
            if (queue == null || queue.IsAddingCompleted)
            {
                //Pre-Init (tests/tools) or post-Shutdown: fall back to a
                //direct commit so no save is silently lost.
                if (job is PlainJob plain)
                {
                    bool committed = true;
                    try { RunFuncDirect(conn => ApplyPlain(conn, plain)); }
                    catch { committed = false; }
                    ReleasePending(plain);
                    if (committed && job.Completion != null)
                    {
                        try { job.Completion(); } catch { }
                    }
                    if (job.Outcome != null)
                    {
                        try { job.Outcome(committed); } catch { }
                    }
                    return;
                }
                if (job is FuncJob func)
                {
                    try { RunFuncDirect(func.Work); }
                    catch (Exception e) { func.Error = e; }
                    ReleaseKeys(func.Declared?.Keys, null, func.Seq);
                    if (func.Outcome != null)
                    {
                        try { func.Outcome(func.Error == null); } catch { }
                    }
                }
                if (job.Completion != null)
                {
                    try { job.Completion(); } catch { }
                }
                return;
            }
            try
            {
                queue.Add(job);
            }
            catch (InvalidOperationException)
            {
                //The writer completed between the check above and this Add: the job was never queued, so run it the
                //same way as the fallback (and release what QueuePlain or TransactAsync registered).
                if (job is PlainJob p)
                    ReleasePending(p);
                else if (job is FuncJob f)
                    ReleaseKeys(f.Declared?.Keys, null, f.Seq);
                Enqueue(job);
            }
        }

        //Every queued plain bundle goes through here: its keys enter the read-your-writes overlay before the
        //writer can see the job, so a main-thread GetKey after this call returns the new value.
        private static void QueuePlain(PlainJob job)
        {
            job.Seq = Interlocked.Increment(ref _jobSeq);
            lock (_pendingLock)
            {
                if (job.Writes != null)
                    foreach (KeyValuePair<string, string> kv in job.Writes)
                        _pending[kv.Key] = new PendingWrite { Seq = job.Seq, Value = kv.Value ?? "" };
                if (job.Deletes != null)
                    foreach (string key in job.Deletes)
                        _pending[key] = new PendingWrite { Seq = job.Seq, Deleted = true };
            }
            Enqueue(job);
        }

        //Drops this bundle's overlay entries once its commit attempt is over (committed or rolled back). A key a newer
        //bundle has rewritten keeps the newer entry.
        private static void ReleasePending(PlainJob job)
        {
            ReleaseKeys(job.Writes?.Keys, job.Deletes, job.Seq);
        }

        //Removes this job's entries (only where no newer job has rewritten the key). Runs on every end of a job: commit,
        //rollback, a failed batch, or the fallback path, so the overlay cannot keep entries for a job that is gone.
        private static void ReleaseKeys(IEnumerable<string> writeKeys, IEnumerable<string> deleteKeys, long seq)
        {
            lock (_pendingLock)
            {
                if (writeKeys != null)
                    foreach (string key in writeKeys.ToList())
                    {
                        if (_pending.TryGetValue(key, out PendingWrite written) && written.Seq == seq)
                            _pending.Remove(key);
                    }
                if (deleteKeys != null)
                    foreach (string key in deleteKeys.ToList())
                    {
                        if (_pending.TryGetValue(key, out PendingWrite deleted) && deleted.Seq == seq)
                            _pending.Remove(key);
                    }
            }
        }

        //The writer could not run a batch (or is gone): release its overlay entries and answer each caller's outcome as
        //a failure, so no reader keeps a value that will never be stored and no HTTP reply waits for the timeout.
        private static void FailJobs(IEnumerable<DbJob> jobs, Exception reason)
        {
            foreach (DbJob job in jobs)
            {
                if (job is PlainJob plain)
                    ReleasePending(plain);
                else if (job is FuncJob func)
                    ReleaseKeys(func.Declared?.Keys, null, func.Seq);
                if (job.Outcome != null)
                {
                    Action<bool> outcome = job.Outcome;
                    try { Manager.RunOnMainThread(() => outcome(false)); } catch { }
                }
            }
#if DEBUG
            try { Console.WriteLine($"<Database writer failed a batch> {reason?.Message}"); } catch { }
#endif
        }

        //Overlay size (trace and tests): must return to 0 when the writer is idle.
        public static int PendingWriteCount()
        {
            lock (_pendingLock)
                return _pending.Count;
        }

        //True while a queued bundle has not finished its commit attempt for this key (main thread only uses this for
        //diagnostics; reads go through GetKey, which consults the overlay).
        private static bool HasPendingWrite(string combinedKey)
        {
            lock (_pendingLock)
                return _pending.ContainsKey(combinedKey);
        }

        //Commits a group of key writes (and optional deletes) as ONE SQLite
        //transaction. Keys must already be combined (see CombineKeyPath).
        //Callers must build the value strings first (pure in-memory work) and
        //pass only strings in. Fire-and-forget: the bundle commits FIFO on
        //the writer thread, so the tick thread never pays the fsync. Bundles
        //stay atomic (group commit only merges whole bundles), but the ack is
        //sent before the commit: a crash in between rolls the bundle back.
        //Paths that must not roll back after ack use WriteAtomicallyAsync
        //(commit-gated completion, WriteAtomicallyThen). No blocking write exists on the tick thread (W03).
        public static void WriteAtomically(Dictionary<string, string> writes, IEnumerable<string> deletes = null)
        {
            if ((writes == null || writes.Count == 0) && deletes == null)
                return;
            List<string> deleteList = deletes == null ? null : new List<string>(deletes);
            QueuePlain(new PlainJob { Writes = writes, Deletes = deleteList });
        }

        //Commit-gated bundle (W03): onDone(true) runs on the main thread after the commit, onDone(false) if it failed.
        //Use it when the caller acknowledges success to a client, and send the ack from onDone. The tick thread never
        //waits. A bundle with nothing to write still reports success, one main-thread pass later.
        public static void WriteAtomicallyThen(Dictionary<string, string> writes, IEnumerable<string> deletes, Action<bool> onDone)
        {
            if ((writes == null || writes.Count == 0) && deletes == null)
            {
                if (onDone != null)
                    Manager.RunOnMainThread(() => onDone(true));
                return;
            }
            List<string> deleteList = deletes == null ? null : new List<string>(deletes);
            QueuePlain(new PlainJob { Writes = writes, Deletes = deleteList, Outcome = onDone });
        }

        //Same bundle, but onCommitted runs on the main thread after the
        //commit lands. Use when client-visible effects (drop bags, vault
        //acks) must not outrun durability.
        public static void WriteAtomicallyAsync(Dictionary<string, string> writes, IEnumerable<string> deletes, Action onCommitted)
        {
            if ((writes == null || writes.Count == 0) && deletes == null)
            {
                if (onCommitted != null)
                {
                    try { Manager.RunOnMainThread(onCommitted); } catch { }
                }
                return;
            }
            List<string> deleteList = deletes == null ? null : new List<string>(deletes);
            QueuePlain(new PlainJob { Writes = writes, Deletes = deleteList, Completion = onCommitted });
        }


        //Imports pre-existing `*.file` keys once. Originals are left in place as backup.
        private static void MigrateLegacyFiles()
        {
            string dir = Settings.DatabaseDirectory;
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return;
            string[] files;
            try { files = Directory.GetFiles(dir, "*.file"); }
            catch { return; }
            if (files.Length == 0)
                return;
            int imported = 0;
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                if (name == null || !name.EndsWith(".file"))
                    continue;
                string key = name.Substring(0, name.Length - ".file".Length);
                string contents;
                try { contents = File.ReadAllText(file); }
                catch { continue; }
                try
                {
                    lock (_lock)
                    {
                        using (SqliteConnection conn = OpenConnection())
                        using (SqliteCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "INSERT OR IGNORE INTO kv(path, value) VALUES(@p, @v);";
                            cmd.Parameters.AddWithValue("@p", key);
                            cmd.Parameters.AddWithValue("@v", contents);
                            if (cmd.ExecuteNonQuery() > 0)
                                imported++;
                        }
                    }
                }
                catch { }
            }
#if DEBUG
            if (imported > 0)
                Program.Print(PrintType.Debug, $"Database migrated {imported} legacy keys to SQLite");
#endif
        }

        public static string AccountKey(int accountId) => $"account.{accountId}";
        public static string CharacterKey(int accountId, int charId) => $"char.{accountId}.{charId}";
        public static string VaultItemsKey(int accountId, int index) => $"vault.{accountId}.{index}";

        public static string VaultValue(int[] types, int[] datas)
        {
            string[] parts = new string[types.Length * 2];
            for (int i = 0; i < types.Length; i++)
            {
                parts[i * 2] = types[i].ToString();
                parts[i * 2 + 1] = datas[i].ToString();
            }
            return string.Join(",", parts);
        }

        //Persists one player's account + character rows atomically. Every
        //in-memory mutation of gold/fame/inventory must end here (or in one
        //of the wider helpers below), otherwise a crash between two separate
        //saves resurrects spent currency or duplicates moved items. Queued
        //FIFO on the writer thread: the bundle still commits as one
        //transaction, but the caller does not wait for the fsync.
        public static void SaveAccountAndCharacter(AccountModel acc, CharacterModel ch, Action<bool> onDone = null)
        {
            string accountXml = acc.Export(false).ToString();
            string charXml = ch.Export(false).ToString();
            Dictionary<string, string> writes = new Dictionary<string, string>
            {
                { AccountKey(acc.Id), accountXml },
                { CharacterKey(acc.Id, ch.Id), charXml }
            };
            //Queued like every other save. A later load for this account reads the queued
            //rows through the overlay (GetKey), so no caller waits for the commit. onDone gates an ack on it.
            if (onDone != null)
                WriteAtomicallyThen(writes, null, onDone);
            else
                WriteAtomically(writes);
            acc.Data = XElement.Parse(accountXml);
            ch.Data = XElement.Parse(charXml);
        }

        //Persists both sides of a completed trade in ONE transaction. Two
        //separate saves would let a crash duplicate every traded item (side
        //A saved without the item, side B never saved with it, or vice
        //versa); one transaction makes the swap all-or-nothing. Queued FIFO
        //on the writer thread; TradeDone is still sent immediately (a failed
        //commit only delays persistence to the next autosave, as before).
        public static void SaveTradePair(AccountModel acc1, CharacterModel ch1, AccountModel acc2, CharacterModel ch2, Action<bool> onDone = null)
        {
            string a1 = acc1.Export(false).ToString();
            string c1 = ch1.Export(false).ToString();
            string a2 = acc2.Export(false).ToString();
            string c2 = ch2.Export(false).ToString();
            //One bundle for both sides: a crash can never persist one side's items without the other's (W03: the caller's
            //TradeDone waits for onDone, so neither client is told the swap happened before it is durable).
            Dictionary<string, string> pair = new Dictionary<string, string>
            {
                { AccountKey(acc1.Id), a1 },
                { CharacterKey(acc1.Id, ch1.Id), c1 },
                { AccountKey(acc2.Id), a2 },
                { CharacterKey(acc2.Id, ch2.Id), c2 }
            };
            if (onDone != null)
                WriteAtomicallyThen(pair, null, onDone);
            else
                WriteAtomically(pair);
            acc1.Data = XElement.Parse(a1);
            ch1.Data = XElement.Parse(c1);
            acc2.Data = XElement.Parse(a2);
            ch2.Data = XElement.Parse(c2);
        }

        //Persists a player together with the vault chest it just swapped
        //with, atomically. The vault used to write through on every slot
        //mutation while the player inventory only saved on disconnect, so a
        //crash in between duplicated vaulted items; this closes that window.
        //Extra writes (e.g. a vault-count bump when buying a chest) join the
        //same transaction via extraWrites. onDone gates the client's ack on the commit (vault swaps are dup-sensitive).
        public static void SaveClientAndVault(AccountModel acc, CharacterModel ch,
            int vaultOwnerId, int vaultIndex, int[] vaultTypes, int[] vaultDatas,
            Dictionary<string, string> extraWrites = null, Action<bool> onDone = null)
        {
            string a = acc.Export(false).ToString();
            string c = ch.Export(false).ToString();
            Dictionary<string, string> writes = new Dictionary<string, string>
            {
                { AccountKey(acc.Id), a },
                { CharacterKey(acc.Id, ch.Id), c },
                { VaultItemsKey(vaultOwnerId, vaultIndex), VaultValue(vaultTypes, vaultDatas) }
            };
            if (extraWrites != null)
                foreach (KeyValuePair<string, string> kv in extraWrites)
                    writes[kv.Key] = kv.Value;
            if (onDone != null)
                WriteAtomicallyThen(writes, null, onDone);
            else
                WriteAtomically(writes);
            acc.Data = XElement.Parse(a);
            ch.Data = XElement.Parse(c);
        }

        public static void Tick()
        {
            if (Environment.TickCount - ResetTime >= ResetCooldown)
            {
#if DEBUG
                Program.Print(PrintType.Debug, "Database reset");
#endif
                RegisteredAccounts.Clear();
                InvalidLoginAttempts.Clear();
                FlushLegends();
                ResetTime = Environment.TickCount;
            }

            //The periodic PASSIVE checkpoint runs on the writer thread (MaybeCheckpoint), not here: it does disk I/O
            //and used to hold _lock while the tick thread waited for it.

            //Bound crash-loss to ~one interval: without this, everything a
            //player earns between login and disconnect lives only in memory
            //and a crash wipes hours of progress (and, worse, resurrects
            //already-traded away items while their new owners keep them).
            if (Environment.TickCount - _lastAutosave >= AutosaveIntervalMS)
            {
                _lastAutosave = Environment.TickCount;
                AutosaveConnectedPlayers();
            }
        }

        //Serializes every connected player first (pure in-memory work), then
        //queues all rows as ONE bundle. The writer usually folds it into a
        //shared group commit, so the old N-transaction fsync stall every 60s
        //is gone and the tick thread never blocks here at all. Crash
        //semantics are unchanged (everything is durable once the commit
        //returns), and a per-player failure only drops that player from the
        //batch.
        private static void AutosaveConnectedPlayers()
        {
            Client[] snapshot;
            try { snapshot = Manager.Clients.Values.ToArray(); }
            catch { return; }
            Dictionary<string, string> writes = new Dictionary<string, string>(snapshot.Length * 2);
            List<AccountModel> accs = new List<AccountModel>(snapshot.Length);
            List<CharacterModel> chs = new List<CharacterModel>(snapshot.Length);
            List<string> accXml = new List<string>(snapshot.Length);
            List<string> chXml = new List<string>(snapshot.Length);
            foreach (Client client in snapshot)
            {
                try
                {
                    if (client == null || client.Account == null || client.Character == null)
                        continue;
                    if (client.Player != null && client.Player.Parent != null)
                        client.Player.SaveToCharacter();
                    string a = client.Account.Export(false).ToString();
                    if (client.Character.Dead)
                    {
                        writes[AccountKey(client.Account.Id)] = a;
                        accs.Add(client.Account);
                        chs.Add(null);
                        accXml.Add(a);
                        chXml.Add(null);
                    }
                    else
                    {
                        string c = client.Character.Export(false).ToString();
                        writes[AccountKey(client.Account.Id)] = a;
                        writes[CharacterKey(client.Account.Id, client.Character.Id)] = c;
                        accs.Add(client.Account);
                        chs.Add(client.Character);
                        accXml.Add(a);
                        chXml.Add(c);
                    }
                }
                catch { }
            }
            if (writes.Count == 0)
                return;
            try { WriteAtomically(writes); }
            catch { return; }
            for (int i = 0; i < accs.Count; i++)
            {
                try
                {
                    accs[i].Data = XElement.Parse(accXml[i]);
                    if (chs[i] != null)
                        chs[i].Data = XElement.Parse(chXml[i]);
                }
                catch { }
            }
        }

        private static void AddRegisteredAccount(string ip)
        {
            if (RegisteredAccounts.ContainsKey(ip))
                RegisteredAccounts[ip]++;
            else RegisteredAccounts[ip] = 1;
        }

        private static void AddInvalidLoginAttempt(string ip)
        {
            if (InvalidLoginAttempts.ContainsKey(ip))
                InvalidLoginAttempts[ip]++;
            else InvalidLoginAttempts[ip] = 1;
        }

        private static void CreateKey(string path, string contents, bool global = false)
        {
            Program.AssertMainThread("Database.CreateKey");
            string key = CombineKeyPath(path, global);
            lock (_lock)
            {
                using (SqliteConnection conn = OpenConnection())
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT OR IGNORE INTO kv(path, value) VALUES(@p, @v);";
                    cmd.Parameters.AddWithValue("@p", key);
                    cmd.Parameters.AddWithValue("@v", contents ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteKey(string path, bool global = false)
        {
            Program.AssertMainThread("Database.DeleteKey");
            string key = CombineKeyPath(path, global);
            lock (_lock)
            {
                using (SqliteConnection conn = OpenConnection())
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM kv WHERE path = @p;";
                    cmd.Parameters.AddWithValue("@p", key);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void SetKey(string path, string contents, bool global = false)
        {
            Program.AssertMainThread("Database.SetKey");
            string key = CombineKeyPath(path, global);
            //A synchronous write under a queued bundle is overwritten when that bundle commits (FIFO). Say so loudly.
            if (HasPendingWrite(key))
                Program.Print(PrintType.Error, $"Database.SetKey({key}) while a queued bundle still writes it; the bundle will win");
            long hopT0 = HopTrace.Enabled ? HopTrace.Now() : 0; //[hop]
            long hopX0 = 0, hopX1 = 0; //[hop]
            lock (_lock)
            {
                using (SqliteConnection conn = OpenConnection())
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO kv(path, value) VALUES(@p, @v) ON CONFLICT(path) DO UPDATE SET value = excluded.value;";
                    cmd.Parameters.AddWithValue("@p", key);
                    cmd.Parameters.AddWithValue("@v", contents ?? "");
                    if (HopTrace.Enabled) hopX0 = HopTrace.Now(); //[hop]
                    cmd.ExecuteNonQuery();
                    if (HopTrace.Enabled) hopX1 = HopTrace.Now(); //[hop]
                }
            }
            if (HopTrace.Enabled) HopTrace.Db("SetKey", key, hopT0, hopX0, hopX1); //[hop]
        }

        //Queued write for tick-thread callers (W03): same key and value as SetKey, but nothing waits for the commit.
        private static void SetKeyQueued(string path, string contents, bool global = false)
        {
            WriteAtomically(new Dictionary<string, string> { { CombineKeyPath(path, global), contents ?? "" } });
        }


        public static string GetKey(string path, bool global = false)
        {
            string key = CombineKeyPath(path, global);
            //Read-your-writes: the newest queued value wins over the file, so a main-thread check made right after an
            //async write sees it (see QueuePlain). A queued delete reads as absent.
            lock (_pendingLock)
            {
                if (_pending.TryGetValue(key, out PendingWrite pending))
                    return pending.Deleted ? null : pending.Value;
            }
            long hopT0 = HopTrace.Enabled ? HopTrace.Now() : 0; //[hop]
            long hopX0 = 0, hopX1 = 0; //[hop]
            string value;
            lock (_lock)
            {
                using (SqliteConnection conn = OpenConnection())
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT value FROM kv WHERE path = @p;";
                    cmd.Parameters.AddWithValue("@p", key);
                    if (HopTrace.Enabled) hopX0 = HopTrace.Now(); //[hop]
                    object result = cmd.ExecuteScalar();
                    if (HopTrace.Enabled) hopX1 = HopTrace.Now(); //[hop]
                    value = result == null || result == DBNull.Value ? null : (string)result;
                }
            }
            if (HopTrace.Enabled) HopTrace.Db("GetKey", key, hopT0, hopX0, hopX1); //[hop]
            return value;
        }

        public static string[] GetKeyLines(string path, bool global = false)
        {
            string value = GetKey(path, global);
            if (value == null)
                return null;
            if (value.Length == 0)
                return new string[0];
            return value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        public static string CombineKeyPath(string path, bool global = false)
        {
            return (global ? "@" : "") + path;
        }

        public static bool CanRegisterAccount(string ip)
        {
            if (RegisteredAccounts.TryGetValue(ip, out byte attempts) && attempts >= MaxRegisteredAccounts)
                return false;
            return true;
        }

        public static bool CanAttemptLogin(string ip)
        {
            if (InvalidLoginAttempts.TryGetValue(ip, out byte attempts) && attempts >= MaxInvalidLoginAttempts)
                return false;
            return true;
        }

        public static AccountModel GuestAccount()
        {
            return new AccountModel() 
            {
                MaxNumChars = 1, 
                Stats = new StatsInfo() { ClassStats = new ClassStatsInfo[0] },
                AliveChars = new List<int>(),
                DeadChars = new List<int>(),
                OwnedSkins = new List<int>(),
                LockedIds = new List<int>(),
                IgnoredIds = new List<int>()
            };
        }

        public static int GetStars(AccountModel acc)
        {
            int stars = 0;
            foreach (ClassStatsInfo classStat in acc.Stats.ClassStats)
                for (int i = 0; i < Player.Stars.Length; i++)
                {
                    if (classStat.BestFame >= Player.Stars[i])
                        stars++;
                }
            return stars;
        }

        public static bool IsValidPassword(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (input.Length < 9) return false;
            return true;
        }

        public static bool IsValidUsername(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (input.Length < 1 || input.Length > 12) return false;
            return Regex.IsMatch(input, @"^[a-zA-Z0-9]+$");
        }

        public static int IdFromUsername(string username)
        {
            string value = GetKey($"login.username.{username}");
            return string.IsNullOrWhiteSpace(value) ? -1 : int.Parse(value);
        }

        public static string UsernameFromId(int id)
        {
            string value = GetKey($"login.id.{id}");
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        //Refusals return false with the reason and queue nothing. Otherwise the registration is queued and onDone(committed)
        //runs on the main thread after the commit. The username check, the account id (nextAccId) and the per-IP
        //reservation all run on the main thread against the overlay, so two registrations in a row never share a name,
        //an id or a slot under the per-IP cap.
        public static bool TryRegisterAccount(string username, string password, string ip, out RegisterStatus refusal, Action<bool> onDone)
        {
            refusal = RegisterStatus.Success;
            if (!CanRegisterAccount(ip))
            {
                refusal = RegisterStatus.TooManyRegisters;
                return false;
            }

            if (!IsValidUsername(username))
            {
                refusal = RegisterStatus.InvalidUsername;
                return false;
            }

            if (!IsValidPassword(password))
            {
                refusal = RegisterStatus.InvalidPassword;
                return false;
            }

            if (IdFromUsername(username) != -1)
            {
                refusal = RegisterStatus.UsernameTaken;
                return false;
            }

            int id = int.Parse(GetKey("nextAccId", true) ?? "0");
            string salt = MathUtils.GenerateSalt();
            //Detached: the account row is built here, not reloaded from the store.
            AccountModel acc = new AccountModel(id, skipReload: true)
            {
                Stats = new StatsInfo
                {
                    BestCharFame = 0,
                    TotalFame = 0,
                    Fame = 0,
                    TotalCredits = 0,
                    Credits = 0,
                    ClassStats = CreateClassStats()
                },

                MaxNumChars = 1,
                NextCharId = 0,
                AliveChars = new List<int>(),
                DeadChars = new List<int>(),
                OwnedSkins = new List<int>(),
                Ranked = false,
                Muted = false,
                Banned = false,
                GuildName = null,
                GuildRank = 0,
                Connected = false,
                LockedIds = new List<int>(),
                IgnoredIds = new List<int>(),
                AllyDamage = true,
                AllyShots = true,
                Effects = true,
                Sounds = true,
                Notifications = true,
                RegisterTime = UnixTime()
            };
            string accountXml = acc.Export(false).ToString();
            //Reserve the per-IP slot now, so two requests from one IP cannot both pass the cap while the first commits.
            AddRegisteredAccount(ip);
            //Id allocation, login keys and the account row commit as ONE bundle, which a later read sees whole.
            WriteAtomicallyThen(new Dictionary<string, string>
            {
                { CombineKeyPath("nextAccId", true), (id + 1).ToString() },
                { $"login.username.{username}", id.ToString() },
                { $"login.id.{id}", username },
                { $"login.hash.{id}", (password + salt).ToSHA1() },
                { $"login.salt.{id}", salt },
                { AccountKey(id), accountXml }
            }, null, committed =>
            {
                if (!committed)
                    ReleaseRegisteredAccount(ip);
                onDone?.Invoke(committed);
            });
            acc.Data = XElement.Parse(accountXml);
            return true;
        }

        private static void ReleaseRegisteredAccount(string ip)
        {
            if (RegisteredAccounts.TryGetValue(ip, out byte attempts) && attempts > 0)
                RegisteredAccounts[ip] = (byte)(attempts - 1);
        }

        public static int UnixTime()
        {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static bool IsAccountInUse(AccountModel acc, bool charListGrace = false)
        {
            //GetClient / in-flight handoff are the source of truth. Never
            //acc.Save() here: the model may be a stale SQLite snapshot from
            //Verify, and writing it would roll back gold/fame/locks.
            Client live = Manager.GetClient(acc.Id);
            if (live != null)
            {
                //Char list after death/transfer: the old socket is still
                //counted until Disconnect, but Active is already false.
                //Hello stays strict so a second session cannot skip the kick.
                if (charListGrace && (!live.Active || live.Reconnecting || live.State == ProtocolState.Disconnected))
                { }
                else
                    return true;
            }
            if (Manager.HasHandoff(acc.Id))
                return true;
            if (acc.Connected)
                acc.Connected = false;
            return false;
        }

        //Password check without constructing an AccountModel, so Hello can
        //kick the live session before any SQLite account Load.
        public static int Authenticate(string username, string password, string ip)
        {
            if (!CanAttemptLogin(ip))
                return -1;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return -1;

            int id = IdFromUsername(username);
            if (id == -1) return -1;

            string hash = GetKey($"login.hash.{id}");
            //A half-written registration (pre-atomic era) or a deleted key
            //could leave a login name with no hash; that must fail closed,
            //not throw NullReferenceException onto the HTTP thread.
            if (string.IsNullOrWhiteSpace(hash))
            {
                AddInvalidLoginAttempt(ip);
                return -1;
            }
            string match = (password + GetKey($"login.salt.{id}")).ToSHA1();
            if (!hash.Equals(match))
            {
                AddInvalidLoginAttempt(ip);
                return -1;
            }
            return id;
        }

        public static AccountModel Verify(string username, string password, string ip)
        {
            int id = Authenticate(username, password, ip);
            if (id == -1) return null;
            AccountModel acc = new AccountModel(id);
            acc.Load();
            return acc;
        }

        public static bool DeleteCharacter(AccountModel acc, int charId, Action<bool> onDone)
        {
            if (!acc.AliveChars.Contains(charId))
                return false;

            CharacterModel character = new CharacterModel(acc.Id, charId);
            character.Load();

            //One transaction: a crash between the two saves used to leave a
            //character flagged Deleted while still listed as alive (or the
            //reverse), stranding or resurrecting it on next login.
            character.Deleted = true;
            acc.AliveChars.Remove(charId);
            string charXml = character.Export(false).ToString();
            string accountXml = acc.Export(false).ToString();
            //The /char/delete reply waits for the commit (onDone), so the re-list that follows reads the new rows.
            WriteAtomicallyThen(new Dictionary<string, string>
            {
                { CharacterKey(acc.Id, charId), charXml },
                { AccountKey(acc.Id), accountXml }
            }, null, onDone);
            character.Data = XElement.Parse(charXml);
            acc.Data = XElement.Parse(accountXml);
            return true;
        }

        //Web rename (ChooseName): the old login key must disappear in the
        //same transaction the new keys and the account row appear, otherwise
        //a crash leaves two names for one account or none at all.
        public static void RenameAccountKeys(int accId, string oldName, string newName, AccountModel acc, Action<bool> onDone = null)
        {
            string accountXml = acc.Export(false).ToString();
            //One bundle: the new login keys, the account row and the removal of the old login key commit together.
            //Writes apply before deletes, so an unchanged name must not be deleted.
            string[] deletes = string.IsNullOrWhiteSpace(oldName) || oldName == newName
                ? null
                : new[] { $"login.username.{oldName}" };
            WriteAtomicallyThen(
                new Dictionary<string, string>
                {
                    { $"login.username.{newName}", accId.ToString() },
                    { $"login.id.{accId}", newName },
                    { AccountKey(accId), accountXml }
                },
                deletes, onDone);
            acc.Data = XElement.Parse(accountXml);
        }

        public static bool ChangePassword(AccountModel acc, string newPassword, Action<bool> onDone)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Undefined account");
#endif
            if (!IsValidPassword(newPassword))
                return false;

            string salt = MathUtils.GenerateSalt();
            //Both halves commit together: a crash between them would lock the account out (new hash, old salt).
            //The reply waits for the commit, so a relogin cannot read the old hash.
            WriteAtomicallyThen(new Dictionary<string, string>
            {
                { $"login.hash.{acc.Id}", (newPassword + salt).ToSHA1() },
                { $"login.salt.{acc.Id}", salt }
            }, null, onDone);
            return true;
        }

        //Check and debit run here on the main thread, against the row as the overlay sees it, so two purchases in a
        //row cannot both spend the same fame. The debit is queued; onDone(committed) runs after the commit.
        //Returns false (nothing queued) when the balance is short.
        public static bool BuyCharSlot(AccountModel acc, Action<bool> onDone)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Undefined account");
#endif
            if (acc.Stats.Fame < CharSlotPrice)
                return false;
            acc.Stats.Fame -= CharSlotPrice;
            acc.MaxNumChars++;
            acc.SaveThen(onDone);
            return true;
        }

        public static bool BuySkin(AccountModel acc, int skinType, Action<bool> onDone)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Undefined account");
#endif
            if (!Resources.Type2Skin.ContainsKey((ushort)skinType))
                return false;
            if (acc.OwnedSkins.Contains(skinType))
                return false;
            if (acc.Stats.Credits < SkinPrice)
                return false;
            acc.Stats.Credits -= SkinPrice;
            acc.OwnedSkins.Add(skinType);
            acc.SaveThen(onDone);
            return true;
        }

        public static XElement GetNews(AccountModel acc)
        {
            int maxNews = 7;
            int newsCount = 0;
            XElement news = new XElement("News");
            foreach (XElement item in Resources.News)
            {
                if (++newsCount > maxNews)  break;
                news.Add(item);
            }
            foreach (int d in acc.DeadChars)
            {
                if (++newsCount > maxNews) break;
                CharacterModel character = LoadCharacter(acc, d);
                character.Load();
                news.Add(new XElement("Item",
                    new XElement("Icon", "fame"),
                    new XElement("Title", $"Your {Resources.Type2Player[(ushort)character.ClassType].DisplayId} died at Level {character.Level}"),
                    new XElement("TagLine", $"Earning {character.Fame} Base Fame and {character.DeathFame} Total Fame"),
                    new XElement("Link", $"fame:{character.Id}"),
                    new XElement("Date", character.DeathTime)));
            }
            return news;
        }

        public static CharacterModel LoadCharacter(AccountModel acc, int charId)
        {
            CharacterModel character = new CharacterModel(acc.Id, charId);
            if (!character.IsNull)
                character.Load();
            return character;
        }

        public static void SaveCharacter(CharacterModel character)
        {
            character.Save();
        }

        public static void Death(string killer, AccountModel acc, CharacterModel character)
        {
#if DEBUG
            if (character == null)
                throw new Exception("Undefined character model");
#endif
            acc.AliveChars.Remove(character.Id);
            if (acc.DeadChars.Count == AccountModel.MaxDeadCharsStored)
                acc.DeadChars.RemoveAt(AccountModel.MaxDeadCharsStored - 1);
            acc.DeadChars.Insert(0, character.Id);

            int deathTime = UnixTime();
            int baseFame = character.Fame;
            int totalFame = character.Fame;

            XElement fame = new XElement("Fame");
            XElement ce = character.ExportFame();
            ce.Add(new XAttribute("id", character.Id));
            ce.Add(new XElement("Account", new XElement("Name", acc.Name)));
            fame.Add(ce);
            character.FameStats.ExportTo(fame);

            ClassStatsInfo classStats = acc.Stats.GetClassStats(character.ClassType);
            FameStats fameStats = CalculateStats(acc, character, killer);
            if (acc.Stats.BestCharFame < baseFame)
                acc.Stats.BestCharFame = baseFame;
            totalFame = fameStats.TotalFame;
            foreach (FameBonus bonus in fameStats.Bonuses)
                fame.Add(new XElement("Bonus", new XAttribute("id", bonus.Name), bonus.Fame));

            fame.Add(new XElement("CreatedOn", character.CreationTime));
            fame.Add(new XElement("KilledOn", deathTime));
            fame.Add(new XElement("KilledBy", killer));
            fame.Add(new XElement("BaseFame", baseFame));
            fame.Add(new XElement("TotalFame", totalFame));

            if (classStats.BestFame < baseFame)
                classStats.BestFame = baseFame;

            if (classStats.BestLevel < character.Level)
                classStats.BestLevel = character.Level;


            character.Dead = true;
            character.DeathTime = UnixTime();
            character.DeathFame = totalFame;

            acc.Stats.Fame += totalFame;
            acc.Stats.TotalCredits += totalFame;

            //Everything death persists (character + account + guild pool +
            //legends + death record) commits as ONE transaction. The old code
            //saved each key separately, so a crash could leave a dead
            //character listed as alive, double-credit fame on retry, or take
            //guild fame without recording the death. The bundle now commits
            //FIFO on the writer thread (still one transaction, still
            //all-or-nothing); the legends board refreshes on the main thread
            //right after, so it always reads the committed state.
            string charXml = character.Export(false).ToString();
            string accountXml = acc.Export(false).ToString();
            string fameXml = fame.ToString();
            string guildName = acc.GuildName;
            int accId = acc.Id;
            int charId = character.Id;
            int charFame = character.Fame;
            TransactAsync(conn =>
            {
                UpsertKeyInTx(conn, CharacterKey(accId, charId), charXml);
                UpsertKeyInTx(conn, AccountKey(accId), accountXml);
                UpsertKeyInTx(conn, $"death.{accId}.{charId}", fameXml);

                //Death fame accrues to the guild pool, as upstream.
                if (!string.IsNullOrWhiteSpace(guildName) &&
                    !string.IsNullOrWhiteSpace(GetKeyInTx(conn, GuildKey(guildName))))
                {
                    int famePool = 0;
                    int.TryParse(GetKeyInTx(conn, GuildKey(guildName) + ".fame"), out famePool);
                    UpsertKeyInTx(conn, GuildKey(guildName) + ".fame", (famePool + totalFame).ToString());
                    if (totalFame > 0)
                    {
                        int totalPool = 0;
                        int.TryParse(GetKeyInTx(conn, GuildKey(guildName) + ".totalFame"), out totalPool);
                        UpsertKeyInTx(conn, GuildKey(guildName) + ".totalFame", (totalPool + totalFame).ToString());
                    }
                }

                if (charFame >= MinFameRequiredToEnterLegends)
                    foreach (var span in TimeSpans)
                    {
                        string key = CombineKeyPath($"legends.{span.Key}", true);
                        string raw = GetKeyInTx(conn, key);
                        List<string> legends = new List<string>();
                        if (!string.IsNullOrEmpty(raw))
                            legends.AddRange(raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
                        legends.Add($"{accId}:{charId}:{totalFame}:{deathTime}");
                        legends = legends.OrderByDescending(k => int.Parse(k.Split(':')[2])).ToList();
                        if (span.Key == "all")
                            legends = legends.Take(MaxLegends).ToList();
                        UpsertKeyInTx(conn, key, string.Join("\n", legends.ToArray()));
                    }
            }, FlushLegends, new Dictionary<string, string>
            {
                { CharacterKey(accId, charId), charXml },
                { AccountKey(accId), accountXml },
                { $"death.{accId}.{charId}", fameXml }
            });
            character.Data = XElement.Parse(charXml);
            acc.Data = XElement.Parse(accountXml);
        }

        //Pure: evaluates every fame bonus in order without mutating the account. Death records BestCharFame itself.
        public static List<FameBonusLine> EvaluateBonuses(AccountModel acc, CharacterModel character, string killer = "")
        {
            int baseFame = character.Fame;
            FameStatsInfo fs = character.FameStats;
            List<FameBonusLine> lines = new List<FameBonusLine>();

            //Ratio conditions are not met when the denominator is 0.
            bool godRatio = fs.MonsterKills > 0;
            bool shotRatio = fs.Shots > 0;
            bool finalBlowRatio = (fs.MonsterKills + fs.MonsterAssists) > 0;

            lines.Add(BonusLine(baseFame, "Ancestor", "First death of this class.", 20, 1,
                acc.Stats.GetClassStats(character.ClassType).BestLevel == 0));
            lines.Add(BonusLine(baseFame, "First Born", "Best fame of any of your characters.", 20, 1,
                acc.Stats.BestCharFame < baseFame));
            lines.Add(BonusLine(baseFame, "Pacifist", "Never dealt any damage.", 25, 20,
                fs.DamageDealt == 0 && character.Level == 20));
            lines.Add(BonusLine(baseFame, "Thirsty", "Never drank a potion.", 25, 20,
                fs.PotionsDrank == 0 && character.Level == 20));
            lines.Add(BonusLine(baseFame, "Mundane", "Never used a special ability.", 25, 20,
                fs.AbilitiesUsed == 0 && character.Level == 20));
            lines.Add(BonusLine(baseFame, "Boots On The Ground", "Never teleported.", 25, 20,
                fs.Teleports == 0 && character.Level == 20));
            lines.Add(BonusLine(baseFame, "Tunnel Rat", "Completed every classic dungeon.", 10, 1,
                fs.PirateCavesCompleted >= 1 &&
                fs.AbyssOfDemonsCompleted >= 1 &&
                fs.SnakePitsCompleted >= 1 &&
                fs.SpiderDensCompleted >= 1 &&
                fs.SpriteWorldsCompleted >= 1 &&
                fs.TombsCompleted >= 1 &&
                fs.UndeadLairsCompleted >= 1));
            lines.Add(BonusLine(baseFame, "Dungeon Master", "Completed every classic dungeon 20 times.", 20, 1,
                fs.PirateCavesCompleted >= 20 &&
                fs.AbyssOfDemonsCompleted >= 20 &&
                fs.SnakePitsCompleted >= 20 &&
                fs.SpiderDensCompleted >= 20 &&
                fs.SpriteWorldsCompleted >= 20 &&
                fs.TombsCompleted >= 20 &&
                fs.UndeadLairsCompleted >= 20));
            lines.Add(BonusLine(baseFame, "Enemy Of The Gods", "At least 10% of kills are gods.", 10, 1,
                godRatio && (float)fs.GodKills / fs.MonsterKills >= 0.1f));
            lines.Add(BonusLine(baseFame, "Slayer Of The Gods", "At least 50% of kills are gods.", 10, 1,
                godRatio && (float)fs.GodKills / fs.MonsterKills >= 0.5f));
            lines.Add(BonusLine(baseFame, "Oryx Slayer", "Dealt the killing blow to Oryx.", 10, 1,
                fs.OryxKills >= 1));
            lines.Add(BonusLine(baseFame, "Dominator Of Realms", "Killed Oryx 1000 times.", 25, 1,
                fs.OryxKills >= 1000));
            lines.Add(BonusLine(baseFame, "Accurate", "Accuracy of at least 25%.", 10, 1,
                shotRatio && (float)fs.ShotsThatDamage / fs.Shots >= .25f));
            lines.Add(BonusLine(baseFame, "Sharpshooter", "Accuracy of at least 50%.", 10, 1,
                shotRatio && (float)fs.ShotsThatDamage / fs.Shots >= .5f));
            lines.Add(BonusLine(baseFame, "Sniper", "Accuracy of at least 75%.", 10, 1,
                shotRatio && (float)fs.ShotsThatDamage / fs.Shots >= .75f));
            lines.Add(BonusLine(baseFame, "Explorer", "Uncovered 1 million tiles.", 5, 1,
                fs.TilesUncovered >= 1000000));
            lines.Add(BonusLine(baseFame, "Cartographer", "Uncovered 4 million tiles.", 5, 1,
                fs.TilesUncovered >= 4000000));
            lines.Add(BonusLine(baseFame, "Pathfinder", "Uncovered 20 million tiles.", 5, 1,
                fs.TilesUncovered >= 20000000));
            lines.Add(BonusLine(baseFame, "Team Player", "Assisted 100 party member level ups.", 10, 1,
                fs.LevelUpAssists >= 100));
            lines.Add(BonusLine(baseFame, "Leader Of Men", "Assisted 1000 party member level ups.", 10, 1,
                fs.LevelUpAssists >= 1000));
            lines.Add(BonusLine(baseFame, "Friend Of The Cubes", "Never killed a cube.", 10, 20,
                fs.CubeKills == 0 && character.Level == 20));
            lines.Add(BonusLine(baseFame, "Careless", "Took 100,000 damage.", 5, 1,
                fs.DamageTaken >= 100000));
            lines.Add(BonusLine(baseFame, "Expert Manoeuvres", "Never took any damage.", 50, 20,
                fs.DamageTaken == 0 && character.Level == 20));
            lines.Add(BonusLine(baseFame, "Beginner's Luck", "Escaped near death.", 5, 1,
                fs.NearDeathEscapes >= 1));
            lines.Add(BonusLine(baseFame, "Living On The Edge", "Escaped near death 50 times.", 10, 1,
                fs.NearDeathEscapes >= 50));
            lines.Add(BonusLine(baseFame, "Living In The Nexus", "Escaped to the Nexus 1000 times.", 5, 1,
                fs.Escapes >= 1000));
            lines.Add(BonusLine(baseFame, "Seal The Deal", "Dealt the final blow on at least half of your kills.", 10, 20,
                finalBlowRatio && (float)fs.MonsterKills / (fs.MonsterKills + fs.MonsterAssists) >= 0.5f && character.Level == 20));
            lines.Add(BonusLine(baseFame, "Realm Riches", "Found 100 white bags.", 15, 1,
                fs.WhiteBags >= 100));
            lines.Add(BonusLine(baseFame, "Devil's Advocate", "A pact sealed in lava.", 100, 1,
                fs.AbyssOfDemonsCompleted >= 1000 &&
                fs.CubeKills >= 50000 &&
                fs.OryxKills >= 666 &&
                killer == "Lava"));

            //Percent carries the equipment sum; the fame is the sum of worn item bonuses.
            int wellEquipped = 0;
            for (int k = 0; k < 4; k++)
                if (character.Inventory[k] != -1)
                {
                    wellEquipped += Resources.Type2Item[(ushort)character.Inventory[k]].FameBonus;
                    wellEquipped += (int)ItemDesc.GetStat(character.ItemDatas[k], ItemData.FameBonus, 1);
                }
            lines.Add(BonusLine(baseFame, "Well Equipped", "Combined fame bonus of all worn equipment.", wellEquipped, 1,
                wellEquipped > 0));

            return lines;
        }

        private static FameBonusLine BonusLine(int baseFame, string name, string description, int percent, int minLevel, bool earned)
        {
            return new FameBonusLine
            {
                Name = name,
                Description = description,
                Percent = percent,
                MinLevel = minLevel,
                Earned = earned,
                Fame = earned ? (int)(baseFame * (percent / 100f)) : 0
            };
        }

        public static FameStats CalculateStats(AccountModel acc, CharacterModel character, string killer = "")
        {
            int baseFame = character.Fame;
            int totalFame = baseFame;

            FameStats stats = new FameStats
            {
                BaseFame = baseFame,
                Bonuses = new List<FameBonus>()
            };

            foreach (FameBonusLine line in EvaluateBonuses(acc, character, killer))
            {
                if (!line.Earned)
                    continue;
                totalFame += line.Fame;
                stats.Bonuses.Add(new FameBonus { Name = line.Name, Fame = line.Fame });
            }

            stats.TotalFame = totalFame;
            return stats;
        }

        public static void FlushLegends()
        {
            int time = UnixTime();
            Legends.Clear();
            foreach (KeyValuePair<string, TimeSpan> span in TimeSpans)
            {
                string[] legends = GetKeyLines($"legends.{span.Key}", true);
                if (span.Key != "all")
                {
                    legends = legends.Where(k => !((time - span.Value.TotalSeconds) > int.Parse(k.Split(':')[3]))).ToArray();
                    legends = legends.OrderByDescending(k => int.Parse(k.Split(':')[2])).ToArray();
                    SetKeyQueued($"legends.{span.Key}", string.Join("\n", legends), true);
                }

                //Update famelist
                XElement list = new XElement("FameList");
                list.Add(new XAttribute("timespan", span.Key));

                foreach (string i in legends.Take(20))
                {
                    string[] s = i.Split(':');
                    int accId = int.Parse(s[0]);
                    int charId = int.Parse(s[1]);
                    int totalFame = int.Parse(s[2]);
                    int deathTime = int.Parse(s[3]);

                    AccountModel acc = new AccountModel(accId);
                    //acc.Load(); Only name is accessed so no load needed

                    CharacterModel character = new CharacterModel(accId, charId);
                    character.Load();

                    list.Add(
                        new XElement("FameListElem",
                        new XAttribute("accountId", accId),
                        new XAttribute("charId", charId),
                        new XElement("Name", acc.Name),
                        new XElement("ObjectType", character.ClassType),
                        new XElement("Tex1", character.Tex1),
                        new XElement("Tex2", character.Tex2),
                        new XElement("Texture", character.SkinType),
                        new XElement("Equipment", string.Join(",", character.Inventory)),
                        new XElement("ItemDatas", string.Join(",", character.ItemDatas)),
                        new XElement("TotalFame", totalFame)));
                    Legends.Add(accId);
                }

                FameLists[span.Key] = list
;            }
        }

        public static void PushLegend(int accId, int charId, int totalFame, int deathTime)
        {
            foreach (var span in TimeSpans)
            {
                List<string> legends = GetKeyLines($"legends.{span.Key}", true).ToList();

                string entry = $"{accId}:{charId}:{totalFame}:{deathTime}";
                legends.Add(entry);
                legends = legends.OrderByDescending(k => int.Parse(k.Split(':')[2])).ToList();

                if (span.Key == "all")
                    legends = legends.Take(MaxLegends).ToList();

                SetKeyQueued($"legends.{span.Key}", string.Join("\n", legends), true);
            }
            FlushLegends();
        }

        public static XElement GetLegends(string timespan)
        {
            if (!FameLists.ContainsKey(timespan))
                return null;
            return FameLists[timespan];
        }

        public static string GetLegend(int accId, int charId)
        {
            return GetKey($"death.{accId}.{charId}");
        }

        public static bool IsLegend(int accountId)
        {
            return Legends.Contains(accountId);
        }

        public static ClassStatsInfo[] CreateClassStats()
        {
            List<ClassStatsInfo> classStats = new List<ClassStatsInfo>();
            foreach (PlayerDesc player in Resources.Type2Player.Values) 
            {
                classStats.Add(new ClassStatsInfo
                {
                    BestFame = 0,
                    BestLevel = 0,
                    ObjectType = (int)player.Type
                });
            }
            return classStats.ToArray();
        }

        public static CharacterModel PrepareCharacter(AccountModel acc, int classType, int skinType)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Account is null.");
#endif
            if (!HasEnoughCharacterSlots(acc))
                return null;

            if (!Resources.Type2Player.TryGetValue((ushort)classType, out PlayerDesc player))
                return null;

            if (skinType != 0)
            {
                if (!Resources.Type2Skin.TryGetValue((ushort)skinType, out SkinDesc skin))
                    return null;
                if (skin.PlayerClassType != classType)
                    return null;
            }

            //Step 1 of 2 (W03): the in-memory account row lists the character and bumps NextCharId now; nothing is
            //written until CommitCharacter. CommitCharacter undoes the bump if the commit fails.
            int newId = acc.NextCharId;
            acc.NextCharId++;
            acc.AliveChars.Add(newId);

            CharacterModel character = new CharacterModel(acc.Id, newId)
            {
                ClassType = classType,
                Level = 1,
                Experience = 0,
                Fame = 0,
                Inventory = player.Equipment.ToArray(),
                ItemDatas = player.ItemDatas.ToArray(),
                Stats = player.StartingValues.ToArray(),
                HP = player.StartingValues[0],
                MP = player.StartingValues[1],
                Tex1 = 0,
                Tex2 = 0,
                SkinType = skinType,
                HasBackpack = false,
                HealthPotions = Player.MaxPotions,
                MagicPotions = Player.MaxPotions,
                CreationTime = UnixTime(),
                Deleted = false,
                Dead = false,
                DeathFame = -1,
                DeathTime = -1,
                FameStats = new FameStatsInfo(),
                PetId = -1
            };

            return character;
        }

        //Step 2 of 2: queues the account row (which lists the character) and the character row as ONE bundle.
        //onCommitted(true) runs on the main thread after the commit. On failure the in-memory bump is undone, so a
        //later save of this account cannot persist a character row that was never written.
        public static void CommitCharacter(AccountModel acc, CharacterModel character, Action<bool> onCommitted)
        {
            int newId = character.Id;
            string accountXml = acc.Export(false).ToString();
            string charXml = character.Export(false).ToString();
            WriteAtomicallyThen(new Dictionary<string, string>
            {
                { AccountKey(acc.Id), accountXml },
                { CharacterKey(acc.Id, newId), charXml }
            }, null, committed =>
            {
                if (!committed)
                {
                    acc.AliveChars.Remove(newId);
                    acc.NextCharId = newId;
                }
                onCommitted?.Invoke(committed);
            });
            acc.Data = XElement.Parse(accountXml);
            character.Data = XElement.Parse(charXml);
        }

        public static bool HasEnoughCharacterSlots(AccountModel acc)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Account is null.");
#endif
            return acc.AliveChars.Count + 1 <= acc.MaxNumChars;
        }
    }
}
