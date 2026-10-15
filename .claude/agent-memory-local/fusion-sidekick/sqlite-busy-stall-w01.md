---
name: sqlite-busy-stall-w01
description: Verified root cause of the ~1000 ms SetKey stall (bundled e_sqlite3 busy callback sleeps whole seconds), MS RetryWhileBusy 150 ms loop, 1 ms busy-handler fix, and P/Invoke gotchas. Measured 2026-10-14.
metadata:
  type: project
---

**Stall root cause (confirmed):** the bundled e_sqlite3 (SQLitePCLRaw.lib.e_sqlite3 2.1.6, SQLite 3.41.2, Microsoft.Data.Sqlite 8.0.0) uses SQLite's non-HAVE_USLEEP default busy callback, which sleeps in whole seconds. A 6 ms writer lock collision costs ~1000 ms. Evidence: busy_timeout=5000 gives 1000.2 ms median; busy_timeout=100 or 0 gives 150 ms (no SQLite retries, Microsoft's loop takes over); a managed callback mimicking the whole-second algorithm reproduces 1000.2 ms. The native lib imports nanosleep only, not usleep (inference, not proven).

**Fix (measured, n=50):** a 1 ms busy handler installed via DllImport("e_sqlite3") `sqlite3_busy_handler` gives 6.5 ms median (p95 6.8, max 6.9). Control with no contention is 0.09 ms, so no overhead.

**Gotchas:**
- SQLitePCLRaw.core 2.1.6 has NO managed `sqlite3_busy_handler`. Only `raw.sqlite3_busy_timeout` exists. Get the pointer with `conn.Handle.DangerousGetHandle()`.
- Install the handler AFTER `PRAGMA busy_timeout`. The pragma calls sqlite3_busy_timeout, which replaces the handler. Installing before it leaves the stall at 1000 ms (measured).
- The delegate must be a static/readonly root. An unrooted delegate aborts the process with "callback was made on a garbage collected delegate" (exit 134, measured).
- Microsoft.Data.Sqlite RetryWhileBusy: after SQLITE_BUSY/LOCKED (primary codes only, not 517), it retries every 150 ms until CommandTimeout (default 30 s, from connection-string Default Timeout). Timeout 0 means unbounded. So a lock held longer than the SQLite window still stalls up to 30 s (measured 30.75 s with a 5 s handler). Default Timeout=5 caps it at ~5.0 s (measured).
- Never set busy_timeout below 1000 on this build. The default callback then gives up immediately and MS's 150 ms steps apply.
- Read-then-write on a deferred transaction with a stale WAL snapshot (SQLITE_BUSY_SNAPSHOT, ext 517): the SQLite handler is not invoked, but MS retries for CommandTimeout (30 s) and then throws. Only BEGIN IMMEDIATE is used in Common/Database.cs (line ~354), so no current path hits this.

**Repro:** scratchpad `w01/repro` (throwaway, outside the repo). Results in `w01/results`.

**How to apply:** when writing W03's OpenConnection or any new connection setup, install the handler after the busy_timeout pragma (or drop that pragma), root the delegate statically, and remember the 30 s CommandTimeout ceiling. Decide separately whether to set Default Timeout.
