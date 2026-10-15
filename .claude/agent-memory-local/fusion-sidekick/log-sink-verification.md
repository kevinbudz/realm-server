---
name: log-sink-verification
description: How to prove a background log writer keeps file I/O off the tick thread, and how to test rotation, date rollover, drops and writer failure with env knobs (LogFiles, W13). Verified 2026-10-14/15.
metadata:
  type: reference
---

Facts about the W13 file sinks (Common/LogFiles.cs, Program.Print/Main/Terminate) and the test technique.

- **Proving no file I/O on the tick thread (no strace on this box).** Read per-task counters: `/proc/PID/task/TID/io`
  has per-thread `wchar` and `syscw`. Snapshot all tasks, wait a window, snapshot again. The main thread (TID == PID)
  wchar delta must equal console bytes, and the thread named `LogWriter` (`/proc/PID/task/TID/comm`) must equal the
  info-file growth. Observed exact equality (3327 and 4567 bytes). Debug builds also run `AssertWriterThread`
  (throws off the writer thread), so any violation shows up as a sink-disabled line.
- **Date rollover without waiting for midnight.** `REALM_LOG_CLOCK_OFFSET_S = (next local midnight - now) - 12`.
  The file clock jumps, so rollover happens about 12 s after start. It moves file stamps and dates only; the console clock is real.
- **Size rotation.** `REALM_LOG_MAX_BYTES=20000`. Rotated files are `DATE.N.log`, and the active file is `DATE.log`.
- **Retention (`REALM_LOG_RETENTION_DAYS`, default 30, 0 = keep all).** The writer thread prunes at startup and on each
  local-date change. A file goes only if it sits directly in a level dir, its name is exactly `DATE.log` or `DATE.N.log`,
  its date is before today minus N days, and it is a regular file that is not the open file. Symlinked root or level dirs
  are skipped. Test: plant old, boundary, non-matching, symlink and directory cases, then diff the expected deletions
  against the planted list (the planter and checker scripts live in the scratchpad, not the repo). Verified 9/9 deletions
  and 20/20 kept, with the rollover offset set to 23:59:40.
- **Drops.** `REALM_LOG_QUEUE_MAX=3` forces drops in the startup burst. Exact check:
  `console_lines - (file_lines - report_lines) == sum(dropped N)`, where the report lines are `<log> dropped N` in warn.
- **Crash loss.** `kill -9`, then compare the last persisted stamp with the kill time. Observed 0.34 and 0.45 s (bound about 1 s).
- **Startup logs need Start before Settings.Init** (Program.cs ~114). The `--*-verify` blocks sit both before and after
  the Settings/Resources/Database/Manager Init block, so a startup hook must skip `*-verify` args. Otherwise verify runs
  start writing `logs/` in their cwd.
- **Bash tool pitfalls (zsh and safety check).** A `for f in $LIST` loop does not word-split in zsh, so put loops in a
  bash script file. A `rm -rf "$(cmd)"` is blocked by the safety check, so use fresh variant names instead of removals.
- **Hop DB filesystem.** A script that exports `REALM_HOP_DB_ROOT` to a scratch dir puts the hop DB on tmpfs silently.
  Check the `db=... fs=` line in the run.sh output (must say `ext2/ext3`) before trusting hop numbers.

See also [[headless-hop-harness]] for server_ctl and run.sh usage.
