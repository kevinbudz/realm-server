# Hop harness

Measures the Escape -> first nexus Update delay (the `docs/alloy-port-workflow.md` standing gate) against a
throwaway server on alternate ports. The server prints `[hop]` timing lines only when it is started with
`REALM_HOPTRACE=1`. Tracing is opt-in, and with it unset the cost is one static readonly bool check per site.

## One command

From the repo root (`realm-server/`):

```sh
tools/hop/run.sh                                   # build Debug, trace on, 12 hops, print median/max
tools/hop/run.sh --no-trace --variant trace-off    # REALM_HOPTRACE unset: expect 0 [hop] lines, hops still complete
tools/hop/run.sh --config Release                  # same with the Release build
tools/hop/run.sh --skip-build --max-median-ms 150  # no build; exit 1 if the median Escape -> first Update > 150 ms
tools/hop/run.sh --runs 12 --max-ms 1200           # also gate on the max
```

`run.sh` does this in order: `dotnet build -c <config>` (skipped with `--skip-build`), start the server,
run the hops with `hop_client.py`, stop the server, then `analyze.py`. It prints the table and a
`SUMMARY` line with the median and max of Escape -> first nexus Update (client-side,
`escape_to_first_update_ms`, the metric in the baseline). It ends with `RESULT PASS` or `RESULT FAIL`
and exits nonzero on any failure. The server is stopped even when a step fails.

Baseline (Debug, ext4, synchronous=FULL, 2026-10-14): median 1113 ms, max 1116 ms (`baseline/results-disk1.jsonl`).

## Step by step

```sh
tools/hop/server_ctl.sh start my-variant --config Debug --trace on   # or --trace off
python3 -I tools/hop/hop_client.py --http 7877 --tcp 2060 --runs 12 --out "$STATE/results/my-variant.jsonl"
tools/hop/server_ctl.sh stop my-variant
python3 -I tools/hop/analyze.py [--max-median-ms N] [--max-ms N] "my-variant=$STATE/logs/my-variant-server.log:$STATE/results/my-variant.jsonl"
```

`STATE` is `${REALM_HOP_STATE:-${TMPDIR:-/tmp}/realm-hop}`. `server_ctl.sh status <variant>` reports whether it is running.

## What is written where

| Path | Contents |
|------|----------|
| `$STATE/build/<config>/` | build output (`dotnet build -o`), the server runs from here. Never the repo's `bin/` |
| `$STATE/run-<variant>/` | server cwd: generated `Settings.xml` and a `Resources` symlink into `$STATE/build/<config>` |
| `$STATE/logs/<variant>-server.log` | server stdout/stderr (truncated at each start) |
| `$STATE/logs/<variant>-build.txt`, `-client.txt`, `-analyze.txt` | build, hop client, and analysis output |
| `$STATE/results/<variant>.jsonl` | one JSON line per hop |
| `$STATE/pids/<variant>.pid` | PID of the server this harness started |
| `${REALM_HOP_DB_ROOT:-${XDG_CACHE_HOME:-$HOME/.cache}/realm-hop-db}/<variant>/` | throwaway DB, wiped at each start |

Environment overrides: `REALM_HOP_STATE`, `REALM_HOP_DB_ROOT`, `REALM_HOP_CONFIG`, `REALM_HOP_HTTP_PORT` (7877),
`REALM_HOP_TCP_PORT` (2060), `REALM_HOP_LAUNCH=dll` (run `dotnet RotMG.dll` instead of the `./RotMG` apphost).

## Safety

- The harness never writes under the repo's `bin/`. The build step (`run.sh` without `--skip-build`) writes to
  `$STATE/build/<config>` with `dotnet build RotMG.csproj -o` (the .sln would warn NETSDK1194 with -o). `bin/` holds the user's live server, so gate builds must not use it either.
- It never touches `Settings.xml` or any `realm.db`. The server's `Settings.xml` comes from the repo root's
  copy, with ports replaced by 7877:2060 and `db=` pointed at the throwaway DB.
- It refuses to start if 7877 or 2060 is already listening.
- `stop` signals only the PID in its own pid file, and only if `/proc/<pid>/cwd` is that variant's run dir.
  It never kills other processes.
- The throwaway DB root defaults to `$HOME/.cache` (ext4). Do not point `REALM_HOP_DB_ROOT` at tmpfs (for
  example `/tmp`): the stall disappears there, so the measurement is wrong. `start` prints the DB filesystem
  and warns on tmpfs.

## Notes

- The server allows one account registration per start (`Database.cs`, `MaxRegisteredAccounts = 1`), so
  each variant run starts a fresh server with a fresh DB. `hop_client.py` registers one throwaway account.
- `analyze.py` exits 0 when no gate is requested or the gate passes, 1 when a gate fails or there are no
  completed hops, and 2 on a usage error.
- `baseline/` holds the reference runs. Do not overwrite them.

## Wire captures and malformed frames (W06)

- `REALM_PKTCAP=<file>` makes the server write every framed game packet it reads or writes (Common/PacketCapture.cs).
  The harness scripts pass the environment through, so set it on `run.sh` or `server_ctl.sh start`.
- `pktdiff.py BASE.bin NEW.bin` compares two captures per packet, after the built-in masks (documented in its
  docstring). `pktdiff.py --jsonl BASE.jsonl NEW.jsonl` compares `chat_check.py` replies. Exit 0 means no diff.
- `malformed_check.py --http PORT --tcp PORT --out FILE` sends five malformed frames from one socket each (truncated
  frame, string length past the payload, negative string length, length < 5, length > 64 KiB). It passes only if each bad
  socket is closed by the server while a second, well-formed client stays in the Nexus.
