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
            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder
            {
                DataSource = Settings.DatabasePath
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
                cmd.CommandText = "PRAGMA busy_timeout = 5000; PRAGMA synchronous = FULL; PRAGMA wal_autocheckpoint = 1000;";
                cmd.ExecuteNonQuery();
            }
            return conn;
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
        }

        private sealed class PlainJob : DbJob
        {
            public Dictionary<string, string> Writes;
            public List<string> Deletes;
        }

        private sealed class FuncJob : DbJob
        {
            public Action<SqliteConnection> Work;
            public ManualResetEventSlim Done;
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
                    foreach (DbJob first in _writerQueue.GetConsumingEnumerable())
                    {
                        List<DbJob> batch = new List<DbJob>(64);
                        batch.Add(first);
                        while (batch.Count < 512 && _writerQueue.TryTake(out DbJob extra))
                            batch.Add(extra);
                        RunBatch(conn, batch);
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
        }

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
                if (batch[k].Completion != null)
                    completions.Add(batch[k].Completion);
            return true;
        }

        private static void CommitOnePlain(SqliteConnection conn, PlainJob job, List<Action> completions)
        {
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
                if (job.Completion != null)
                    completions.Add(job.Completion);
            }
#if DEBUG
            catch (Exception e)
            {
                try { Console.WriteLine($"<Database write dropped> {e.Message}"); } catch { }
            }
#else
            catch { }
#endif
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
                try { job.Done?.Set(); } catch { }
            }
        }

        //Runs work inside one IMMEDIATE transaction on the writer thread and
        //blocks the caller until it commits. Rare paths only (register, char
        //delete, vault-chest buy, renames): the tick thread must never wait
        //per-action, so hot paths use WriteAtomically / TransactAsync instead.
        //Crash rule is unchanged: once the commit returns, everything is
        //durable; if the process dies first, none of it is.
        private static void Transact(Action<SqliteConnection> work)
        {
            //Re-entrant guard: func jobs calling back in run inline instead
            //of queueing behind themselves.
            if (Thread.CurrentThread == _writerThread)
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
                return;
            }
            FuncJob job = new FuncJob { Work = work, Done = new ManualResetEventSlim(false) };
            Enqueue(job);
            job.Done.Wait();
            if (job.Error != null)
                throw job.Error;
        }

        //Fire-and-forget read-modify-write on the writer thread. The optional
        //completion hops to the main thread after the commit.
        private static void TransactAsync(Action<SqliteConnection> work, Action onCommitted = null)
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
            Enqueue(new FuncJob { Work = work, Completion = onCommitted });
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
                    RunFuncDirect(conn => ApplyPlain(conn, plain));
                else if (job is FuncJob func)
                {
                    try { RunFuncDirect(func.Work); }
                    catch (Exception e) { func.Error = e; }
                    finally { try { func.Done?.Set(); } catch { } }
                }
                if (job.Completion != null)
                {
                    try { job.Completion(); } catch { }
                }
                return;
            }
            queue.Add(job);
        }

        //Commits a group of key writes (and optional deletes) as ONE SQLite
        //transaction. Keys must already be combined (see CombineKeyPath).
        //Callers must build the value strings first (pure in-memory work) and
        //pass only strings in. Fire-and-forget: the bundle commits FIFO on
        //the writer thread, so the tick thread never pays the fsync. Bundles
        //stay atomic (group commit only merges whole bundles), but the ack is
        //sent before the commit: a crash in between rolls the bundle back.
        //Paths that must not roll back after ack use WriteAtomicallyAsync
        //(commit-gated completion) or WriteAtomicallyAndWait instead.
        public static void WriteAtomically(Dictionary<string, string> writes, IEnumerable<string> deletes = null)
        {
            if ((writes == null || writes.Count == 0) && deletes == null)
                return;
            List<string> deleteList = deletes == null ? null : new List<string>(deletes);
            Enqueue(new PlainJob { Writes = writes, Deletes = deleteList });
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
            Enqueue(new PlainJob { Writes = writes, Deletes = deleteList, Completion = onCommitted });
        }

        //Blocking variant for rare paths with same-call read-after-write
        //(name changes) or in-transaction reads (vault-chest buy). Never use
        //on a per-action hot path: it stalls the caller for the fsync.
        public static void WriteAtomicallyAndWait(Dictionary<string, string> writes, IEnumerable<string> deletes = null)
        {
            if ((writes == null || writes.Count == 0) && deletes == null)
                return;
            List<string> deleteList = deletes == null ? null : new List<string>(deletes);
            PlainJob plain = new PlainJob { Writes = writes, Deletes = deleteList };
            //One job, one transaction: the func applies the plain bundle and
            //signals, so ordering and atomicity match WriteAtomically. The
            //Done handle is always signaled, so this cannot hang while the
            //writer lives; pre-Init/post-Shutdown Enqueue runs inline above.
            FuncJob job = new FuncJob
            {
                Work = conn => ApplyPlain(conn, plain),
                Done = new ManualResetEventSlim(false)
            };
            Enqueue(job);
            job.Done.Wait();
            if (job.Error != null)
                throw job.Error;
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
        public static void SaveAccountAndCharacter(AccountModel acc, CharacterModel ch, bool andWait = false)
        {
            string accountXml = acc.Export(false).ToString();
            string charXml = ch.Export(false).ToString();
            Dictionary<string, string> writes = new Dictionary<string, string>
            {
                { AccountKey(acc.Id), accountXml },
                { CharacterKey(acc.Id, ch.Id), charXml }
            };
            //andWait is the Hello fallback: a non-reconnect kick must land
            //before the new connection LoadCharacter can read SQLite.
            if (andWait)
                WriteAtomicallyAndWait(writes);
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
        public static void SaveTradePair(AccountModel acc1, CharacterModel ch1, AccountModel acc2, CharacterModel ch2)
        {
            string a1 = acc1.Export(false).ToString();
            string c1 = ch1.Export(false).ToString();
            string a2 = acc2.Export(false).ToString();
            string c2 = ch2.Export(false).ToString();
            WriteAtomically(new Dictionary<string, string>
            {
                { AccountKey(acc1.Id), a1 },
                { CharacterKey(acc1.Id, ch1.Id), c1 },
                { AccountKey(acc2.Id), a2 },
                { CharacterKey(acc2.Id, ch2.Id), c2 }
            });
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
        //same transaction via extraWrites. Queued FIFO like the rest; vault
        //callers that ack to the client use WriteAtomicallyAndWait instead.
        public static void SaveClientAndVault(AccountModel acc, CharacterModel ch,
            int vaultOwnerId, int vaultIndex, int[] vaultTypes, int[] vaultDatas,
            Dictionary<string, string> extraWrites = null, bool andWait = false)
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
            //Vault rows back dup-sensitive swaps: callers that ack to the
            //client pass andWait so the ack cannot outrun the commit.
            if (andWait)
                WriteAtomicallyAndWait(writes);
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

            //Non-blocking checkpoint so the WAL cannot grow without bound on
            //a server that is never shut down cleanly.
            if (Environment.TickCount - _lastCheckpoint >= CheckpointIntervalMS)
            {
                _lastCheckpoint = Environment.TickCount;
                try
                {
                    lock (_lock)
                    {
                        using (SqliteConnection conn = OpenConnection())
                        using (SqliteCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "PRAGMA wal_checkpoint(PASSIVE);";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch { }
            }

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
            lock (_lock)
            {
                using (SqliteConnection conn = OpenConnection())
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO kv(path, value) VALUES(@p, @v) ON CONFLICT(path) DO UPDATE SET value = excluded.value;";
                    cmd.Parameters.AddWithValue("@p", key);
                    cmd.Parameters.AddWithValue("@v", contents ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void SetKeyLines(string path, string[] contents, bool global = false)
        {
            SetKey(path, string.Join("\n", contents ?? new string[0]), global);
        }

        public static string GetKey(string path, bool global = false)
        {
            string key = CombineKeyPath(path, global);
            lock (_lock)
            {
                using (SqliteConnection conn = OpenConnection())
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT value FROM kv WHERE path = @p;";
                    cmd.Parameters.AddWithValue("@p", key);
                    object result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? null : (string)result;
                }
            }
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

        public static RegisterStatus RegisterAccount(string username, string password, string ip)
        {
            if (!CanRegisterAccount(ip))
                return RegisterStatus.TooManyRegisters;

            if (!IsValidUsername(username))
                return RegisterStatus.InvalidUsername;

            if (!IsValidPassword(password))
                return RegisterStatus.InvalidPassword;

            //Fast-path pre-check; re-verified inside the transaction below.
            if (IdFromUsername(username) != -1)
                return RegisterStatus.UsernameTaken;

            string salt = MathUtils.GenerateSalt();
            //Account id allocation, login keys and the account row commit as
            //ONE transaction: a crash in between used to leave login keys
            //pointing at a nonexistent account (or burn an id), and two
            //concurrent registers could claim the same username.
            RegisterStatus usernameTaken = RegisterStatus.Success;
            Transact(conn =>
            {
                if (!string.IsNullOrWhiteSpace(GetKeyInTx(conn, $"login.username.{username}")))
                {
                    usernameTaken = RegisterStatus.UsernameTaken;
                    return;
                }

                int id = int.Parse(GetKeyInTx(conn, CombineKeyPath("nextAccId", true)) ?? "0");
                UpsertKeyInTx(conn, CombineKeyPath("nextAccId", true), (id + 1).ToString());
                UpsertKeyInTx(conn, $"login.username.{username}", id.ToString());
                UpsertKeyInTx(conn, $"login.id.{id}", username);
                UpsertKeyInTx(conn, $"login.hash.{id}", (password + salt).ToSHA1());
                UpsertKeyInTx(conn, $"login.salt.{id}", salt);

                //Detached: the account row is being created by this very
                //transaction, so there is nothing to Reload (and the ctor
                //Reload would open a nested connection inside the tx).
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
                UpsertKeyInTx(conn, AccountKey(id), accountXml);
                acc.Data = XElement.Parse(accountXml);
            });
            if (usernameTaken != RegisterStatus.Success)
                return usernameTaken;

            AddRegisteredAccount(ip);
            return RegisterStatus.Success;
        }

        public static int UnixTime()
        {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static bool IsAccountInUse(AccountModel acc)
        {
            //GetClient / in-flight handoff are the source of truth. Never
            //acc.Save() here: the model may be a stale SQLite snapshot from
            //Verify, and writing it would roll back gold/fame/locks.
            if (Manager.GetClient(acc.Id) != null)
                return true;
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

        public static bool DeleteCharacter(AccountModel acc, int charId)
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
            //Blocking: the /char/delete response precedes a re-list that
            //reads the row back. Rare admin path.
            WriteAtomicallyAndWait(new Dictionary<string, string>
            {
                { CharacterKey(acc.Id, charId), charXml },
                { AccountKey(acc.Id), accountXml }
            });
            character.Data = XElement.Parse(charXml);
            acc.Data = XElement.Parse(accountXml);
            return true;
        }

        //Web rename (ChooseName): the old login key must disappear in the
        //same transaction the new keys and the account row appear, otherwise
        //a crash leaves two names for one account or none at all.
        public static void RenameAccountKeys(int accId, string oldName, string newName, AccountModel acc)
        {
            string accountXml = acc.Export(false).ToString();
            //Blocking: ChooseName reloads the row in the same call, so the
            //rename must be committed before returning. Rare path.
            WriteAtomicallyAndWait(
                new Dictionary<string, string>
                {
                    { $"login.username.{newName}", accId.ToString() },
                    { $"login.id.{accId}", newName },
                    { AccountKey(accId), accountXml }
                },
                new[] { $"login.username.{oldName}" });
            acc.Data = XElement.Parse(accountXml);
        }

        public static bool ChangePassword(AccountModel acc, string newPassword) 
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Undefined account");
#endif
            if (!IsValidPassword(newPassword))
                return false;

            string salt = MathUtils.GenerateSalt();
            //Both halves commit together: a crash between them would lock
            //the account out (new hash, old salt). Blocking: a relogin may
            //read the hash back immediately. Rare path.
            WriteAtomicallyAndWait(new Dictionary<string, string>
            {
                { $"login.hash.{acc.Id}", (newPassword + salt).ToSHA1() },
                { $"login.salt.{acc.Id}", salt }
            });
            return true;
        }

        public static bool BuyCharSlot(AccountModel acc)
        {
#if DEBUG
            if (acc == null)
                throw new Exception("Undefined account");
#endif
            if (acc.Stats.Fame < CharSlotPrice)
                return false;
            acc.Stats.Fame -= CharSlotPrice;
            acc.MaxNumChars++;
            acc.Save();
            return true;
        }

        public static bool BuySkin(AccountModel acc, int skinType)
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
            acc.Save();
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
            }, FlushLegends);
            character.Data = XElement.Parse(charXml);
            acc.Data = XElement.Parse(accountXml);
        }

        public static FameStats CalculateStats(AccountModel acc, CharacterModel character, string killer = "")
        {
            int baseFame = character.Fame;
            int totalFame = baseFame;

            FameStats stats = new FameStats
            {
                BaseFame = baseFame,
                Bonuses = new List<FameBonus>()
            }; ClassStatsInfo classStats = acc.Stats.GetClassStats(character.ClassType);

            //Ancestor
            if (acc.Stats.GetClassStats(character.ClassType).BestLevel == 0)
            {
                int bonus = (int)(baseFame * .2f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Ancestor", Fame = bonus });
            }

            //First Born
            if (acc.Stats.BestCharFame < baseFame)
            {
                int bonus = (int)(baseFame * .2f);
                totalFame += bonus;
                acc.Stats.BestCharFame = baseFame;
                stats.Bonuses.Add(new FameBonus { Name = "First Born", Fame = bonus });
            }

            //Pacifist
            if (character.FameStats.DamageDealt == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Pacifist", Fame = bonus });
            }

            //Thirsty
            if (character.FameStats.PotionsDrank == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Thirsty", Fame = bonus });
            }

            //Mundane
            if (character.FameStats.AbilitiesUsed == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Mundane", Fame = bonus });
            }

            //Boots On The Ground
            if (character.FameStats.Teleports == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Boots On The Ground", Fame = bonus });
            }

            //Tunnel Rat
            if (character.FameStats.PirateCavesCompleted >= 1 &&
                character.FameStats.AbyssOfDemonsCompleted >= 1 &&
                character.FameStats.SnakePitsCompleted >= 1 &&
                character.FameStats.SpiderDensCompleted >= 1 &&
                character.FameStats.SpriteWorldsCompleted >= 1 &&
                character.FameStats.TombsCompleted >= 1 &&
                character.FameStats.UndeadLairsCompleted >= 1)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Tunnel Rat", Fame = bonus });
            }

            //Dungeon Master
            if (character.FameStats.PirateCavesCompleted >= 20 &&
                character.FameStats.AbyssOfDemonsCompleted >= 20 &&
                character.FameStats.SnakePitsCompleted >= 20 &&
                character.FameStats.SpiderDensCompleted >= 20 &&
                character.FameStats.SpriteWorldsCompleted >= 20 &&
                character.FameStats.TombsCompleted >= 20 &&
                character.FameStats.UndeadLairsCompleted >= 20)
            {
                int bonus = (int)(baseFame * .2f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Dungeon Master", Fame = bonus });
            }

            //Enemy Of The Gods
            if (((float)character.FameStats.GodKills / character.FameStats.MonsterKills) >= 0.1f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Enemy Of The Gods", Fame = bonus });
            }

            //Slayer Of The Gods
            if (((float)character.FameStats.GodKills / character.FameStats.MonsterKills) >= 0.5f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Slayer Of The Gods", Fame = bonus });
            }

            //Oryx Slayer
            if (character.FameStats.OryxKills >= 1)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Oryx Slayer", Fame = bonus });
            }

            //Dominator Of Realms
            if (character.FameStats.OryxKills >= 1000)
            {
                int bonus = (int)(baseFame * .25f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Dominator Of Realms", Fame = bonus });
            }

            //Accurate
            if (((float)character.FameStats.ShotsThatDamage / character.FameStats.Shots) >= .25f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Accurate", Fame = bonus });
            }

            //Sharpshooter
            if (((float)character.FameStats.ShotsThatDamage / character.FameStats.Shots) >= .5f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Sharpshooter", Fame = bonus });
            }

            //Sniper
            if (((float)character.FameStats.ShotsThatDamage / character.FameStats.Shots) >= .75f)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Sniper", Fame = bonus });
            }

            //Explorer
            if (character.FameStats.TilesUncovered >= 1000000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Explorer", Fame = bonus });
            }

            //Cartographer
            if (character.FameStats.TilesUncovered >= 4000000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Cartographer", Fame = bonus });
            }

            //Pathfinder
            if (character.FameStats.TilesUncovered >= 20000000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Pathfinder", Fame = bonus });
            }

            //Team Player
            if (character.FameStats.LevelUpAssists >= 100)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Team Player", Fame = bonus });
            }

            //Leader Of Men
            if (character.FameStats.LevelUpAssists >= 1000)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Leader Of Men", Fame = bonus });
            }

            //Friend Of The Cubes
            if (character.FameStats.CubeKills == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Friend Of The Cubes", Fame = bonus });
            }

            //Careless
            if (character.FameStats.DamageTaken >= 100000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Careless", Fame = bonus });
            }

            //Expert Manoeuvres
            if (character.FameStats.DamageTaken == 0 && character.Level == 20)
            {
                int bonus = (int)(baseFame * .5f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Expert Manoeuvres", Fame = bonus });
            }

            //Beginner's Luck
            if (character.FameStats.NearDeathEscapes >= 1)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Beginner's Luck", Fame = bonus });
            }

            //Living On The Edge
            if (character.FameStats.NearDeathEscapes >= 50)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Living On The Edge", Fame = bonus });
            }

            //Living In The Nexus
            if (character.FameStats.Escapes >= 1000)
            {
                int bonus = (int)(baseFame * .05f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Living In The Nexus", Fame = bonus });
            }

            //Seal The Deal
            if (((float)character.FameStats.MonsterKills / (character.FameStats.MonsterKills + character.FameStats.MonsterAssists) >= 0.5f) && character.Level == 20)
            {
                int bonus = (int)(baseFame * .1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Seal The Deal", Fame = bonus });
            }

            //Realm Riches
            if (character.FameStats.WhiteBags >= 100)
            {
                int bonus = (int)(baseFame * .15f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Realm Riches", Fame = bonus });
            }

            //Devil's Advocate
            if (character.FameStats.AbyssOfDemonsCompleted >= 1000 &&
                character.FameStats.CubeKills >= 50000 &&
                character.FameStats.OryxKills >= 666 &&
                killer == "Lava")
            {
                int bonus = (int)(baseFame * 1f);
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Devil's Advocate", Fame = bonus });
            }

            //Well Equipped
            int wellEquipped = 0;
            for (int k = 0; k < 4; k++)
                if (character.Inventory[k] != -1)
                {
                    wellEquipped += Resources.Type2Item[(ushort)character.Inventory[k]].FameBonus;
                    wellEquipped += (int)ItemDesc.GetStat(character.ItemDatas[k], ItemData.FameBonus, 1);
                }
            if (wellEquipped > 0)
            {
                int bonus = (int)(baseFame * (wellEquipped / 100.0f));
                totalFame += bonus;
                stats.Bonuses.Add(new FameBonus { Name = "Well Equipped", Fame = bonus });
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
                    SetKeyLines($"legends.{span.Key}", legends.ToArray(), true);
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

                SetKeyLines($"legends.{span.Key}", legends.ToArray(), true);
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

        public static CharacterModel CreateCharacter(AccountModel acc, int classType, int skinType)
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

            //The account row (which lists this character as alive and bumps
            //NextCharId) and the character row commit together: a crash
            //between them used to leave the account pointing at a character
            //that was never written.
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

            string accountXml = acc.Export(false).ToString();
            string charXml = character.Export(false).ToString();
            WriteAtomically(new Dictionary<string, string>
            {
                { AccountKey(acc.Id), accountXml },
                { CharacterKey(acc.Id, newId), charXml }
            });
            acc.Data = XElement.Parse(accountXml);
            character.Data = XElement.Parse(charXml);
            return character;
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
