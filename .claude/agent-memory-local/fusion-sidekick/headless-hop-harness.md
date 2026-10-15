---
name: headless-hop-harness
description: How to run the hop harness (tools/hop/run.sh, server_ctl.sh), the duplicate-login checker (dup_login.py), and the verify self-checks from a throwaway cwd. Facts: run dir instead of bin/, disk-backed DB root, pid/cwd safety, bool XML, Settings cwd. Verified 2026-10-14.
metadata:
  type: reference
---

Harness: `tools/hop/run.sh` (one command: build, start, 12 hops, stop, analyze), `tools/hop/server_ctl.sh`
(start/stop/status), `tools/hop/analyze.py --max-median-ms N --max-ms N`. README is `tools/hop/README.md`.
Duplicate-login and relog checker: `tools/hop/dup_login.py --db <dbroot>/<variant> --out <json>`. Run it against a
fresh server started by `server_ctl.sh start <variant>`, because registration is capped at one per IP per start.
It takes about 40 s, and all of its checks print PASS/FAIL (exit 0 only if all pass).
Tick period: start the server with `REALM_TICKTRACE=1` (optionally `REALM_TICK_LOAD_MS=30` for a synthetic per-tick
load, both read once in Game/Manager.cs), let it run ~50 s with no clients, then
`python3 -I tools/hop/tick_period.py --expect-ms 100 --tol-ms 2 <server.log>`. Pass env vars to the server through
`server_ctl.sh start` (they are inherited). Run long servers in the background, not as a foreground sleep.

Verified facts (sources in the realm-server repo):

- AppServer reads form parameters from the POST body (Networking/AppServer.cs:50-52), for every route. `/account/register` must be a form-encoded POST, or you get `<Error>Invalid username.</Error>`.
- Registration is capped at one account per IP per server start (Common/Database.cs:43, `MaxRegisteredAccounts = 1`). So every hop run starts a fresh server with a fresh DB dir. Five bad logins per IP also lock out (Database.cs:40).
- The server reads `Settings.xml` and its `res=` directory relative to its cwd (Common/Settings.cs:39-47; Common/Resources.cs:44). The repo's `Settings.xml` has `db="Database"`, which is relative to cwd, so any `--*-verify` run must use a throwaway cwd with a generated Settings.xml (db pointed at a throwaway dir) and a `Resources` symlink into `bin/<cfg>/net8.0`, the same as `server_ctl.sh`. Never run them from the repo root, or they open `bin/*/net8.0/Database/realm.db`. All 20 verify flags passed this way on 2026-10-14.
- `server_ctl.sh` therefore runs the server from `$STATE/run-<variant>`, with a generated Settings.xml and a `Resources` symlink into `bin/<cfg>/net8.0`. Nothing under bin/ is written.
- The apphost `./RotMG` and `dotnet RotMG.dll` both run correctly from a foreign cwd (verified). `setsid` is not used: in a backgrounded subshell it forks when the shell is a process-group leader, so `$!` would not be the server.
- **The temp dir is tmpfs.** `/tmp` is tmpfs on this machine and `TMPDIR` is unset. A DB there gives ~50 ms hops, not ~1.1 s. The DB root stays at `$HOME/.cache/realm-hop-db` (ext4). To keep logs out of /tmp, set `REALM_HOP_STATE` to a scratch dir. Only the DB root has to be on ext4.
- `run.sh` builds into `$STATE/build/<config>` with `dotnet build -o` (2026-10-15), never `bin/`. `--skip-build` only reuses that directory, so a first run must build. `server_ctl.sh` starts from the same directory.
- Safety: stop signals only the recorded PID, and only if `/proc/PID/cwd` is that variant's run dir and the cmdline contains RotMG. Start refuses when 7877 or 2060 is listening. Verified with a foreign listener: start refused, stop refused to signal it.
- Wire format: frame = u32 BE total length (includes its own 4 bytes) + 1 id byte + body. Strings = u16 BE length + UTF-8. Hello (15): str buildVersion "1.0.0", i32 gameId, str user, str pass, i32 0. Create (2): i16 class, i16 skin (Rogue 0x0300 is free). Load (21): i32 charId. Escape (42): empty body. Ping (61): answer Pong (62) with two i32s.
- Ids: realm -2, nexus -1 (Game/Manager.cs:20-25). Reconnect (19) carries i32 gameId. CreateSuccess (1): i32 objectId, i32 charId.
- The realm-cpp client on Reconnect calls `transport.close()` before `connect()` (realm-cpp/src/net/Transport.cpp:199-201). hop_client.py mimics that ordering.
- Debug main loop sleeps 2 ms per iteration (Program.cs:255), Release 1 ms (Program.cs:258).
- ptrace is restricted (Yama scope 1), so gdb cannot attach. Per-thread `wchan` in /proc is readable.
- Stored DB bools are lowercase XML (`<Connected>true</Connected>`), because XElement writes them that way. Compare case-insensitively.
- `stat -f -c %T` reports `ext2/ext3` for this ext4 DB. server_ctl only warns on a true `tmpfs`.
- `dotnet build` with no `--no-incremental` can print 0 warnings even when code changed, so use `--no-incremental` for warning counts. Release has one pre-existing CS0169 (Networking/Client.cs:141).
- Game sockets are accepted at Networking/GameServer.cs:222 without `NoDelay`, so small back-to-back sends can wait ~40 ms (Linux delayed ACK). Seen as 3/12 hop outliers after W04. Check `NoDelay` before blaming the tick.
- zsh (the Bash tool's shell here): quote glob arguments (`--include='*.cs'`), or grep fails with "no matches found". `${PIPESTATUS}` is not set in zsh. Run harness scripts with `bash` explicitly, or via their shebang.
