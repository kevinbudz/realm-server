#!/usr/bin/env python3
"""Summarize the start-to-start tick period from a server log written with REALM_TICKTRACE=1.

usage: python3 -I tools/hop/tick_period.py [--warmup N] [--expect-ms 100 --tol-ms 2] <server.log>

Each tick prints "[tick] start=<ms> period=<ms> work=<ms>" (Game/Manager.cs, TraceTick). The first
--warmup periods are dropped. Prints mean, p95, max and min of the period, and the work mean and max.
With --expect-ms, exit 1 unless mean, p95 and max are each within --tol-ms of the expected period.
Exit 2 on a usage error or when there are too few ticks to judge.
"""
import argparse
import math
import re
import statistics
import sys

TICK_RE = re.compile(r"\[tick\] start=([\d.]+) period=([\d.]+) work=(\d+)")


def pctl(sorted_vals, p):
    # nearest-rank percentile on an already sorted list
    k = max(0, math.ceil(p / 100.0 * len(sorted_vals)) - 1)
    return sorted_vals[k]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--warmup", type=int, default=10)
    ap.add_argument("--expect-ms", type=float, default=None)
    ap.add_argument("--tol-ms", type=float, default=2.0)
    ap.add_argument("log")
    args = ap.parse_args()

    periods, work = [], []
    try:
        with open(args.log, encoding="utf-8", errors="replace") as fh:
            for line in fh:
                m = TICK_RE.search(line)
                if m:
                    periods.append(float(m.group(2)))
                    work.append(int(m.group(3)))
    except OSError as exc:
        print(f"tick_period: cannot read {args.log}: {exc}", file=sys.stderr)
        return 2

    periods, work = periods[args.warmup:], work[args.warmup:]
    if len(periods) < 20:
        print(f"tick_period: only {len(periods)} periods after warmup; need >= 20", file=sys.stderr)
        return 2

    srt = sorted(periods)
    mean = statistics.fmean(periods)
    p95 = pctl(srt, 95)
    mx = srt[-1]
    mn = srt[0]
    print(f"ticks={len(periods)} (warmup {args.warmup} dropped)")
    print(f"period ms: mean={mean:.2f} p95={p95:.2f} max={mx:.2f} min={mn:.2f}")
    print(f"work ms:   mean={statistics.fmean(work):.2f} max={max(work)}")

    if args.expect_ms is None:
        return 0
    dev = {"mean": mean - args.expect_ms, "p95": p95 - args.expect_ms, "max": mx - args.expect_ms}
    ok = all(abs(v) <= args.tol_ms for v in dev.values())
    print(f"expect {args.expect_ms:g} +/- {args.tol_ms:g} ms: "
          + " ".join(f"{k}={v:+.2f}" for k, v in dev.items())
          + (" PASS" if ok else " FAIL"))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
