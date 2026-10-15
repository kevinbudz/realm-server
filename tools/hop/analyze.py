#!/usr/bin/env python3
"""Summarize [hop] server marks + client results per variant, and optionally gate on the hop time.

usage: python3 -I analyze.py [--max-median-ms N] [--max-ms N] <variant>=<server.log>:<results.jsonl> ...
Server runs are delimited by 'escape rx' ... 'HOP COMPLETE' (single account, sequential hops).
Server-side times are ms since the escape rx of the same run.

The gate metric is the client-side escape_to_first_update_ms (Escape -> first nexus Update), which is
the number the baseline (tools/hop/baseline, 1113/1116 ms) reports.
Exit status: 0 = no gate requested or passed; 1 = a gate failed or there were no completed hops;
2 = usage error.
"""
import argparse
import json
import re
import statistics
import sys

HOP_RE = re.compile(r"\[hop\] acc=(-?\d+) t=\+([\d.]+)ms (.*)$")
DB_RE = re.compile(r"db (SetKey|GetKey) (\S+) open=([\d.]+)ms exec=([\d.]+)ms total=([\d.]+)ms")
BATCH_RE = re.compile(r"writer batch jobs=(\d+) plain=(\d+) func=(\d+) ms=([\d.]+)")

GATE_KEY = "escape_to_first_update_ms"

STEPS = [
    ("escape rx", "escape rx (server handler)"),
    ("BeginTransfer returned", "BeginTransfer returned"),
    ("Reconnect written to socket", "Reconnect written to socket"),
    ("new connection accepted", "new connection accepted (accept thread)"),
    ("BeginHandling on main thread", "BeginHandling on main thread"),
    ("old socket closed", "old socket closed (server side)"),
    ("Hello rx", "Hello rx"),
    ("after Database.Authenticate", "after Database.Authenticate"),
    ("after old-session kick", "after old-session kick"),
    ("after handoff take", "after handoff take (no DB load)"),
    ("after acc.Save", "after acc.Save (synchronous SetKey)"),  # pre-W02 logs
    ("after acc save enqueued", "after acc save enqueued"),
    ("MapInfo written to socket", "MapInfo written to socket"),
    ("Load rx", "Load rx"),
    ("Load: character resolved", "Load: character resolved"),
    ("joined world", "joined world (AddEntity + CreateSuccess queued)"),
    ("CreateSuccess written to socket", "CreateSuccess written to socket"),
    ("first Update written to socket", "first Update written to socket"),
    ("first NewTick written to socket", "first NewTick written to socket"),
    ("HOP COMPLETE", "HOP COMPLETE"),
]


def parse_runs(path):
    runs = []
    cur = None
    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            m = HOP_RE.search(line)
            if not m:
                continue
            acc, t, label = int(m.group(1)), float(m.group(2)), m.group(3).strip()
            if label == "escape rx":
                cur = {"acc": acc, "marks": {}, "db": [], "batches": []}
                runs.append(cur)
            if cur is None:
                continue
            if label.startswith("db "):
                d = DB_RE.search(label)
                if d:
                    cur["db"].append((d.group(1), d.group(2), float(d.group(3)), float(d.group(4)), float(d.group(5)), t))
                continue
            if label.startswith("writer batch"):
                b = BATCH_RE.search(label)
                if b:
                    cur["batches"].append((int(b.group(1)), int(b.group(2)), float(b.group(4)), t))
                continue
            cur["marks"].setdefault(label, t)
    return runs


def find(marks, prefix):
    for label, t in marks.items():
        if label.startswith(prefix):
            return t
    return None


def stats(values):
    if not values:
        return None, None, 0
    return statistics.median(values), max(values), len(values)


def fmt(v):
    return "   -   " if v is None else f"{v:9.3f}"


def parse_spec(spec):
    if "=" not in spec:
        raise ValueError(f"expected <variant>=<server.log>:<results.jsonl>, got {spec!r}")
    name, rest = spec.split("=", 1)
    if ":" not in rest:
        raise ValueError(f"expected <server.log>:<results.jsonl> after '=', got {spec!r}")
    log, res = rest.split(":", 1)
    return name, log, res


def report(name, log, res):
    """Print the per-variant tables. Returns (client gate values, complete server hop count)."""
    runs = parse_runs(log)
    complete = [r for r in runs if find(r["marks"], "HOP COMPLETE") is not None]
    with open(res, encoding="utf-8") as fh:
        client = [json.loads(line) for line in fh if line.strip()]
    client = [c for c in client if "error" not in c]
    print(f"\n=== {name}: server hop runs={len(runs)} complete={len(complete)}; client runs={len(client)}")
    print(f"{'step (server, ms since escape rx)':42s} {'median':>9s} {'max':>9s} {'n':>3s}")
    for key, title in STEPS:
        vals = [find(r["marks"], key) for r in complete if find(r["marks"], key) is not None]
        med, mx, n = stats(vals)
        print(f"{title:42s} {fmt(med)} {fmt(mx)} {n:3d}")
    set_acc = [d[3] for r in complete for d in r["db"] if d[0] == "SetKey" and d[1].startswith("account.")]
    set_med, set_max, set_n = stats(set_acc)
    get_exec = [sum(d[3] for d in r["db"] if d[0] == "GetKey") for r in complete]
    get_n = [sum(1 for d in r["db"] if d[0] == "GetKey") for r in complete]
    g_med, g_max, _ = stats(get_exec)
    n_get_med, _, _ = stats(get_n)
    wb = [b[2] for r in complete for b in r["batches"]]
    wb_med, wb_max, wb_n = stats(wb)
    wbj = [b[0] for r in complete for b in r["batches"]]
    print(f"DB SetKey(account.*) exec in hop: median={fmt(set_med).strip()} max={fmt(set_max).strip()} n={set_n}")
    print(f"DB GetKey exec sum per hop:       median={fmt(g_med).strip()} max={fmt(g_max).strip()} (GetKey calls/hop median={n_get_med})")
    print(f"Writer batches in hop:            n={wb_n} (batches/hop max={max(wbj) if wbj else 0}) duration median={fmt(wb_med).strip()} max={fmt(wb_max).strip()} ms")
    keys = ["escape_to_reconnect_rx_ms", "connect_ms", "hello_to_mapinfo_ms", "load_to_createsuccess_ms",
            "createsuccess_to_first_update_ms", GATE_KEY, "escape_to_first_newtick_ms"]
    print(f"{'client-side (ms)':42s} {'median':>9s} {'max':>9s} {'n':>3s}")
    for k in keys:
        vals = [c[k] for c in client if k in c]
        med, mx, n = stats(vals)
        print(f"{k:42s} {fmt(med)} {fmt(mx)} {n:3d}")
    return [c[GATE_KEY] for c in client if GATE_KEY in c], len(complete)


def main():
    ap = argparse.ArgumentParser(description="Summarize hop runs and optionally gate on Escape -> first nexus Update.")
    ap.add_argument("specs", nargs="+", metavar="variant=server.log:results.jsonl")
    ap.add_argument("--max-median-ms", type=float, default=None,
                    help="fail (exit 1) if the median Escape -> first nexus Update exceeds N ms")
    ap.add_argument("--max-ms", type=float, default=None,
                    help="fail (exit 1) if the max Escape -> first nexus Update exceeds N ms")
    args = ap.parse_args()

    try:
        parsed = [parse_spec(s) for s in args.specs]
    except ValueError as exc:
        ap.error(str(exc))

    gated = args.max_median_ms is not None or args.max_ms is not None
    failures = []
    for name, log, res in parsed:
        values, n_complete = report(name, log, res)
        med, mx, n = stats(values)
        if n == 0:
            failures.append(f"{name}: no completed hops with {GATE_KEY}")
            print(f"SUMMARY {name}: Escape -> first nexus Update: no data")
            continue
        print(f"SUMMARY {name}: Escape -> first nexus Update (client) median={med:.3f} ms max={mx:.3f} ms n={n}")
        if args.max_median_ms is not None and med > args.max_median_ms:
            failures.append(f"{name}: median {med:.3f} ms > --max-median-ms {args.max_median_ms:g}")
        if args.max_ms is not None and mx > args.max_ms:
            failures.append(f"{name}: max {mx:.3f} ms > --max-ms {args.max_ms:g}")

    if failures:
        for msg in failures:
            print(f"GATE FAIL: {msg}")
        return 1
    if gated:
        print("GATE PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
