#!/usr/bin/env python3
"""Two-connection WAL repro: does a main-thread autocommit write stall when a writer
thread's commit is in flight? Mirrors DatabaseWriter (own connection, BEGIN IMMEDIATE,
2 upserts, COMMIT) vs the main thread's SetKey (autocommit upsert, same pragmas).

usage: sqlite_overlap_repro.py <dbfile> <synchronous FULL|NORMAL> <iterations>
Prints main-thread write latency for two schedules:
  overlap    - main writes right after the writer starts its commit
  separate   - main writes after the writer has finished (writer idle)
"""
import queue
import sqlite3
import statistics
import sys
import threading
import time


def open_conn(path, sync):
    conn = sqlite3.connect(path, isolation_level=None, check_same_thread=False, timeout=5.0)
    conn.execute("PRAGMA busy_timeout = 5000")
    conn.execute(f"PRAGMA synchronous = {sync}")
    return conn


def upsert(conn, key, value):
    conn.execute("INSERT INTO kv(path, value) VALUES(?, ?) ON CONFLICT(path) DO UPDATE SET value = excluded.value", (key, value))


def main():
    path, sync, iters = sys.argv[1], sys.argv[2], int(sys.argv[3])
    main_conn = open_conn(path, sync)
    main_conn.execute("PRAGMA journal_mode=WAL")
    main_conn.execute("CREATE TABLE IF NOT EXISTS kv(path TEXT PRIMARY KEY, value TEXT NOT NULL)")
    jobs = queue.Queue()
    done = queue.Queue()
    writer_ms = []

    def writer():
        wc = open_conn(path, sync)
        while True:
            job = jobs.get()
            if job is None:
                return
            t0 = time.perf_counter()
            wc.execute("BEGIN IMMEDIATE")
            upsert(wc, "account.0", job)
            upsert(wc, "char.0.0", job)
            in_commit.set()          # main thread writes now, racing this COMMIT's fsync
            wc.execute("COMMIT")
            writer_ms.append((time.perf_counter() - t0) * 1000)
            done.put(True)

    in_commit = threading.Event()
    th = threading.Thread(target=writer, daemon=True)
    th.start()
    results = {"overlap": [], "separate": []}
    for i in range(iters):
        # overlap: writer commit is queued, main writes immediately (like Hello after FinishDisconnect)
        in_commit.clear()
        jobs.put(f"o{i}" * 50)
        in_commit.wait()
        t0 = time.perf_counter()
        upsert(main_conn, "account.0", f"m{i}" * 50)
        results["overlap"].append((time.perf_counter() - t0) * 1000)
        done.get()
        time.sleep(0.05)
        # separate: writer finishes first, then main writes
        jobs.put(f"s{i}" * 50)
        done.get()
        t0 = time.perf_counter()
        upsert(main_conn, "account.0", f"n{i}" * 50)
        results["separate"].append((time.perf_counter() - t0) * 1000)
        time.sleep(0.05)
    jobs.put(None)
    for mode, vals in results.items():
        vals_sorted = sorted(vals)
        print(f"{path} sync={sync} {mode:9s} n={len(vals)} median={statistics.median(vals):9.3f}ms "
              f"max={vals_sorted[-1]:9.3f}ms p90={vals_sorted[int(len(vals_sorted)*0.9)-1]:9.3f}ms")
    print(f"  writer batch median={statistics.median(writer_ms):.3f}ms max={max(writer_ms):.3f}ms")


if __name__ == "__main__":
    main()
