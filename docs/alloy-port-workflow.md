# Alloy port workflow

Ordered backlog of fixes and ports for realm-server. It came out of the 2026-10-14
comparison with [Zolmex/alloy-server](https://github.com/Zolmex/alloy-server) (commit `c4bd116`)
and the measurement of the nexus/world-change delay.

Work **one row per chat**, top to bottom unless a row says otherwise:
1. Take the first `pending` row whose dependencies are `done`, and mark it `in progress`.
2. Read its section below, implement it, and run its gates plus the standing gates.
3. Write `docs/alloy-port/handoffs/WNN.md`: what changed (file:line), the gate commands with their
   results, the numbers, and any decisions made or deferred.
4. Mark the row `done` (or `blocked: <reason>`) and update **Measurements** if a number moved.

What we port from Alloy is the *technique*, not its code. Alloy's gameplay, persistence, IPC and
validation are less complete than ours (see **Not porting**).

## Ledger

| ID  | Task                                                      | Phase        | Depends  | Status  |
|-----|-----------------------------------------------------------|--------------|----------|---------|
| W00 | Make hop harness + `[hop]` tracing permanent and opt-in   | Nexus        | —        | done    |
| W01 | Confirm the 1 s SQLite busy-handler stall                 | Nexus        | W00      | done    |
| W02 | Send the Hello account save through the writer queue      | Nexus        | W00      | done    |
| W03 | Remove synchronous main-thread DB writes                  | Nexus        | W01, W02 | done    |
| W04 | Fix the tick gate; send the first frame on join           | Nexus        | W00      | done    |
| W05 | Correctness sweep (decoy NRE + suspected gaps)            | Fixes        | —        | done    |
| W06 | Span-based packet reader/writer                           | Alloy perf   | W00      | in progress (not applied) |
| W07 | Typed stat storage with dirty bitmask                     | Alloy perf   | W06      | pending |
| W08 | Shadowcasting sight + bitset tile state + shared caches   | Alloy perf   | W07      | pending |
| W09 | Closed-form projectile paths for hit validation           | Alloy perf   | —        | done    |
| W10 | Async sockets (SocketAsyncEventArgs)                      | Alloy perf   | W06      | pending |
| W11 | Attribute-based commands with rank levels                 | Alloy devex  | —        | done    |
| W13 | Leveled async file logging                                | Alloy devex  | —        | done    |

W05, W09, W11 and W13 are independent and can be done at any point.

## Where things stand (2026-10-15)

Done: W00-W05, W09, W11 and W13, each with a handoff in `docs/alloy-port/handoffs/`. Escape → first nexus Update went from
1113 ms to ~9.5 ms (median, as recorded for W04).

Left to do, in order:
1. **W05 is done** (2026-10-15): all 22 self-check flags pass, `dup_login` 16/16, and the soak's max tick work is 30 ms (Debug) and
   10 ms (Release), under the 50 ms goal. One gate is open: the hop median is 11.3 ms on this VM (Debug, trace on) against the 9.5 ms
   recorded for W04, and the gap is not explained. A W04 A/B on this VM would settle it. See `handoffs/W05.md`.
2. **Finish W06.** The partial, unverified patch is in `docs/alloy-port/wip/W06-partial.patch`. See `handoffs/W06.md`.
3. **W07** (typed stats, needs W06), then **W08** (sight + caches, needs W07), and **W10** (async sockets, needs W06). Each has a
   decision for the user in its section (W07: stat order; W08: shadowcasting vs rays).
4. Cross-cutting checks that no harness covers: in-game testing of vault, guild, trade and inventory (all became commit-gated in W03),
   plus a real shooting soak for W09.

Workflow notes for future rows:
- Build with `dotnet build RotMG.csproj -c <cfg> -o <scratch>/out-<cfg>`, **never into `bin/`**. The live server runs from `bin/Release/net8.0`.
- Run the harness via `tools/hop/run.sh` (it builds into its own state dir). Run verify flags from a throwaway cwd that has a generated
  `Settings.xml` (db= override) and a `Resources` symlink.
- Turn off file logs for timing runs with `REALM_LOG_FILES=0`.

## Standing gates (every row)

- `cd realm-server && dotnet build -c Debug` and `-c Release`: 0 errors, and no new warnings.
- The self-check flags in `Program.cs:40-207` (`--p2-verify` … `--p25-verify`, `--sizequake-verify`)
  pass. Run the ones whose subsystem you touched, and all of them before marking a Phase row done.
- Hop harness (from W00): Escape → first nexus Update, 12 hops. Report the median and max, and
  don't regress them.
- Never use the user's live server, ports or `bin/*/net8.0/Database/realm.db`. The harness runs
  on alternate ports with a throwaway DB, and never kills a process it didn't start.
- Preserve uncommitted work, and don't commit (the user commits). Don't touch `realm-cpp` unless a
  row says so.

## Measurements

| Metric                                              | Baseline (2026-10-14) | Current |
|-----------------------------------------------------|-----------------------|---------|
| Escape → first nexus Update, median / max (FULL)    | 1113 / 1116 ms        | 9.5 / 12.9 ms (W04, Debug trace on). Re-run 2026-10-15: 11.3–11.4 / 20.4–23.2 ms (Debug trace on), 9.2 / 14.4 ms (Release, trace off). The Debug gap is open; see `handoffs/W05.md` |
| Hello `acc.Save()` → `SetKey`, median               | 1005 ms               | 0 (enqueued, W02; 0 `SetKey` calls in the 2026-10-15 hops) |
| Same with `synchronous=NORMAL` / DB on tmpfs        | 52 ms / 53 ms         |         |
| Wait for first Update after the CreateSuccess packet | ~99 ms (one 10 TPS tick) | 0–1.1 ms median, max ≤ 3.6 ms (join frame, W04; 2026-10-15) |
| Max tick work under `soak.py` (goal < 50 ms)        | 45–140 ms under hop churn (pre-W05 sidekick note) | 30 ms Debug / 10 ms Release (W05, 2026-10-15) |

The raw baseline runs are in `tools/hop/baseline/*.jsonl`. The harness was a Python stand-in for
realm-cpp: it closes the old socket before reconnecting (like `Transport.cpp:199-201`) and sends
no Move packets.

---

## Phase 1 — Nexus / world-change latency

**Finding.** Escape itself is fast: `Reconnect` reaches the socket about 0.5 ms after Escape. The stall is
`client.Account.Save()` in `Hello` (`Networking/GameServer.Handlers.cs:632` → `Database.SetKey`,
`Common/Database.cs:902`). It is a synchronous fsync write on the **tick thread**. It collides with the
DatabaseWriter's commit of the old session's handoff save (queued in `FinishDisconnect`,
`Networking/Client.cs:~290`). It gets SQLITE_BUSY and then waits ~1000 ms, although the writer
holds the lock for only ~6 ms. Because this is on the tick thread, **the whole server freezes**,
not just the player who is hopping.

### W00 — Make the harness and tracing permanent and opt-in
- `tools/hop/` holds copies of the scratch harness (`hop_client.py`, `analyze.py`, `server_ctl.sh`,
  `sqlite_overlap_repro.py`). They still hard-code the old scratchpad path (`SCRATCH=` in
  `server_ctl.sh`). Make them repo-relative, putting temp DBs, logs and settings under
  `${TMPDIR:-/tmp}/realm-hop`. Add a `tools/hop/README.md` with one-command usage.
- The `[hop]` instrumentation is currently uncommitted in `Common/HopTrace.cs`, `Common/Database.cs`,
  `Networking/Client.cs`, `Networking/GameServer.cs` and `Networking/GameServer.Handlers.cs`. Keep it,
  but make it opt-in (env `REALM_HOPTRACE=1`), with near-zero cost when off (a static readonly bool
  checked before any formatting).
- Add an assertion mode, `analyze.py --max-median-ms N`, that exits nonzero, so later rows can use it
  as a gate.
- **Gate:** the harness reproduces the ~1.1 s baseline with tracing on. A build with tracing off
  shows no `[hop]` output.
- The sidekick's notes in `realm-server/.claude/` are untracked. Ask the user whether to delete
  them or add them to `.git/info/exclude`.

### W01 — Confirm the 1 s busy-handler stall
- Hypothesis: the bundled `e_sqlite3` (SQLitePCLRaw, via Microsoft.Data.Sqlite 8.0.0) was compiled
  without `HAVE_USLEEP`. If so, `sqliteDefaultBusyCallback` sleeps in whole seconds. A Python repro
  with the same pragmas stalled only 1.7 ms.
- Write a small C# repro with the **same package** (a throwaway console project in `/tmp`, not in
  the repo). Use two connections: one holds `BEGIN IMMEDIATE` for 6 ms, and the other runs an
  `INSERT … ON CONFLICT` with `busy_timeout=5000`. Measure 50 runs.
- If confirmed: decide the defense in depth, which is to install a millisecond busy handler with
  `SQLitePCL.raw.sqlite3_busy_handler` (sleep 1 ms, up to the timeout) in `OpenConnection`
  (`Database.cs:167-181`). Check that it works on the version in use. That change lands in W03.
- **Gate:** the repro's numbers go in the handoff, with a yes or no on the hypothesis.

### W02 — Send the Hello account save through the writer
- Replace the synchronous `client.Account.Save()` at `GameServer.Handlers.cs:632` with an
  enqueue through `Database.WriteAtomically` (or a small `AccountModel.SaveAsync()` helper that
  exports and enqueues). The writer is FIFO, so this lands after the old session's handoff save.
  There's nothing to wait on, and no lock contention.
- Audit first: who reads `Account.Connected` **from the DB** rather than from memory?
  Check `Database.IsAccountInUse`, the AppServer `Account in use!` path
  (`Networking/AppServer.Handlers.cs:80`) and char/list. Make sure a not-yet-committed `Connected=true` can't
  open a duplicate-login window. If one exists, use `WriteAtomicallyAndWait` *off* the tick thread,
  or rely on the in-memory `Manager.Clients` check, and document the choice.
- **Gate:** the hop median falls below ~150 ms (an expected ~50 ms plus the tick wait). Run 12 hops,
  then log in from a second client while the first is connected, and confirm the duplicate is still
  rejected or kicked as before. Relog, and confirm the character state persisted.

### W03 — Remove synchronous main-thread DB writes
- Inventory every caller of `SetKey`, `SetKeyLines`, `CreateKey`, `DeleteKey`,
  `WriteAtomicallyAndWait`, `Transact` and `DatabaseModel.Save()` that runs on the tick thread. Known
  ones: `BuyCharSlot`/`BuySkin` (`Database.cs:1248, 1266`), `AccountSetName` with its 4 non-atomic writes
  (`Networking/AppServer.Social.cs:44-51`), `ChooseName` (`GameServer.Handlers.cs:332`), `/ban`
  (`Player.Chat.cs:833-871`), `FlushLegends` (`Database.cs:1689-1738`), register and char create.
- Fire-and-forget where nothing is acknowledged → `WriteAtomically`. Where the client is told
  "success" → a commit-gated completion that sends the ack from a `PushWork` callback, never blocking
  the tick thread. Make `AccountSetName` a single atomic bundle.
- Apply the W01 busy handler if it was confirmed.
- **Gate:** grep shows no blocking write reachable from the tick thread, except ones you list and
  justify. Run a soak: 4 harness clients hopping in a loop while a script hits the buy-slot and
  set-name routes. The max tick duration (log it) stays under 50 ms, and nothing stalls for 1 s.

### W04 — Fix the tick gate; send the first frame on join
- `Game/Manager.cs:1077` fires when `elapsed - LastTickTime >= MillisecondsPerTick - TickDelta`.
  `LastTickTime` is the tick's *start*, so the period becomes `100 − d`, and ticks speed up under load.
  `TotalTime` still advances a fixed 100 ms per tick (`:1203`). Fix it to keep a fixed cadence:
  schedule `nextTick += MsPerTick`, and catch up (or drop with a warning) if more than one tick
  behind.
- First frame: when a player joins a world (Load/Create), send the initial `Update` + `NewTick`
  right away, or on the next main-loop pass, instead of waiting up to a full tick. Check that
  realm-cpp tolerates a NewTick arriving off-cadence, and that projectile/time acks (`ClientTimeGate`,
  `Game/Entities/Player.Timing.cs`) don't count it as a drift event.
- **Gate:** under a synthetic 30 ms tick load the measured period stays 100 ± 2 ms. The hop median
  drops by roughly the tick wait. `--p*-verify` passes for the timing and movement checks.

### W05 — Correctness sweep
- **Confirmed:** `Game/Entities/Enemy.cs:66-68`, `(p as Player).FameStats` over `PlayerChunks`, which
  contains decoys → NRE on level-up assist. This can crash the process from the world tick, which
  has no catch (`Manager.cs:1184-1186`). Fix: `if (p is Player other && other != player)`.
- **Verify, then fix if real:**
  - Teleport ignores `World.AllowTeleport` (`Networking/GameServer.Social.cs:15-21`, `Player.Guild.cs:15-26`).
  - Vendor Buy has no proximity check (`Game/Entities/Vendors/Vendors.cs:35-42`).
  - `EditAccountList` doesn't deduplicate or cap the list (`GameServer.Handlers.cs:446-458`).
  - DungeonGen has unbounded retry loops (`Game/DungeonGen/Generator.cs:390, 403-415, 426-458`). Add an
    attempt cap with a fallback or failure.
  - `Hello` does `ReadBytes(ReadInt32())` before auth (`GameServer.Handlers.cs:518`). Bound the length.
  - The unhandled sequential world tick (`Manager.cs:1184-1186`): wrap it the same way the parallel
    path is wrapped (`:1169-1180`).
- **Gate:** a regression check for each fix (a self-check flag or a harness scenario), noted in the handoff.

---

## Phase 2 — Alloy performance techniques

Each row must keep **wire behavior identical** unless the row says otherwise. Proof: a byte capture
from the hop harness (and any scenario added) before and after, diffed per packet.

### W06 — Span-based packet reader/writer
- Today every packet costs a body copy (`Networking/GameServer.cs:33-38`), a `MemoryStream`, a `PacketReader`
  (`GameServer.Handlers.cs:130`), and a `ToArray()` on send (`Networking/PacketWriter.cs:42-67`).
- Port the technique from Alloy (`Common/Network/SpanWriter.cs`, `SocketSendState.cs`, `Outgoing/*.cs`).
  Write outgoing packets straight into the client's pooled send buffer, using a big-endian
  `BinaryPrimitives` writer. Read incoming packets with a `ref struct` reader over the receive buffer
  slice. Keep our framing: 4-byte BE length + id. Keep the existing 2/4 MiB backpressure policy
  (`Client.cs:118-124`).
- Don't copy Alloy's sizing bugs: grow the buffer to the *required* size, not to `_writeLength`, and
  don't detect overflow through exceptions. Bounds-check string lengths on write (`PacketWriter.cs:69-74`).
- Migrate incrementally: an adapter keeps the old `PacketReader` API working while the handlers move over.
- **Gate:** byte-identical captures. Allocation count per hop/tick (`dotnet-counters` or
  `GC.GetAllocatedBytesForCurrentThread` deltas logged under `REALM_HOPTRACE`) drops substantially.

### W07 — Typed stat storage with a dirty bitmask
- Today `SVs`/`NewSVs`/`PrivateSVs` are `Dictionary<StatType, object>`, boxing every write
  (`Game/Entity.cs:146-170, 711-726, 742-750`; `Entities/Player.Stats.cs:21-28, 171-189`).
- Technique from Alloy (`GameServer/Game/Entities/Components/EntityStats.cs`): an int array plus a string array
  indexed by `StatType`, a 256-bit dirty mask, and public/private masks; snapshot the diff per tick.
- **Decision for the user:** stat *order* inside ObjectData/ObjectStatusData will change, from
  dictionary insertion order to `StatType` order. Confirm that realm-cpp parses stats order-independently
  (check its ObjectStatusData decoder) before relying on that. If it does, wire bytes may differ only
  in order. Use a decode-and-compare diff instead of a byte diff.
- **Gate:** the decoded stat sets match for a scripted scenario (spawn, damage, level, equip, condition
  effects, death). No boxing allocations on the stat path.

### W08 — Shadowcasting sight + bitset tile state + shared caches
- Today: ~82 precomputed rays at radius 15 (`Game/Entities/Player.Update.cs:11-68`). The per-player
  `TileUpdates` `Dictionary<long,int>` is never pruned and grows with each tile revealed (`:70-77`). Any
  static change bumps the world's `UpdateCount`, which invalidates every player's sight (`World.cs:421-500`).
- Technique from Alloy (`PlayerSightManager.cs`, `Components/PlayerSight.cs`): 8-octant
  shadowcasting with a `stackalloc` stack; `BitArray2D` discovered/visible sets per player; and
  per-tick shared ObjectData/status caches, so an entity seen by N players serializes once. Keep our
  radius 15 and our BlockSight modes 0/1/2. Don't copy Alloy's 3×3-chunk query, which misses entities
  between 16 and 20 tiles away when chunks are 16 tiles.
- **Decision for the user:** shadowcasting gives a slightly different visible set than rays, mostly at the
  corners of walls. Option A: accept it. Option B: keep the rays and port only the bitset state and
  shared caches.
- Also look at the far-player half-rate and the world-wide known-player set, which is O(P²) bytes
  (`Player.Update.cs:83`).
- **Gate:** memory per player stays flat after exploring a full realm. Update bytes and CPU per tick
  at 50 bot players (`Bot.cs`) are measured before and after. If option A is chosen, the
  visible-tile diffs are documented.

### W09 — Closed-form projectile paths for hit validation
- Today `TryHitEnemy` re-simulates the path at 8 samples/s (`Entities/Player.Projectiles.cs:227-316`).
- Technique from Alloy (`Common/Projectiles/ProjectilePaths/`): position is a pure function of
  (elapsed time, start, angle, projectile desc). **It must match the realm-cpp client's projectile
  math**, not Alloy's segment schema. Port realm-cpp's formulas (Wavy, Parametric, Boomerang,
  Amplitude/Frequency, Accelerate/Decelerate as supported) into one server `ProjectilePath.PositionAt`.
- Use it for the path check at hit time (one exact evaluation plus a tile/cover sweep), instead of the
  coarse resim.
- **Gate:** a table-driven test of server vs client positions for every projectile kind in the
  GameData, at t = 0..lifetime in 10 ms steps, with max error ≤ 0.01 tiles. Self-checks pass. No new
  false rejections over a 10-minute bot soak.

### W10 — Async sockets
- Today a single IO thread busy-polls every socket at about 1 kHz (`Networking/Client.cs:368, 609-644`;
  `Networking/GameServer.cs:190-210`). The accept thread does `Sleep(10)` per accept (`GameServer.cs:212-289`), and
  every client snapshot takes `Manager.SyncRoot`.
- Technique from Alloy (`GameServer/Game/Network/SocketServer.cs`, `NetworkHandler.cs`): a SAEA accept loop,
  a receive SAEA per client feeding the existing `ConcurrentQueue` handoff, and a send-completion chain
  with one send in flight per client.
- Don't copy Alloy's bugs:
  - Validate the framing *inside* the try block.
  - Make disconnect idempotent with an `Interlocked` flag.
  - Keep per-IP counting correct, and close rejected sockets.
  - Keep `MaxClients` = 256 preallocation, not Alloy's ~1.1 GiB.
- **Gate:** the hop harness. A 200-client Python soak with no leaks (fd count, pooled buffer count).
  Idle server CPU is measured before and after. Malformed-frame fuzzing (length < 5, length > 64 KiB,
  truncated) disconnects only the bad client.

---

## Phase 3 — Alloy developer-experience techniques

### W11 — Attribute-based commands with rank levels
- Today the commands are one big switch (`Game/Entities/Player.Chat.cs:82+`), and authority is a single bool,
  `Account.Ranked` (`Common/Models.cs:350`).
- Technique from Alloy (`Chat/Commands/CommandManager.cs`, `Command.cs`): `[Command("name", Rank.X)]`
  classes discovered by reflection. Rank levels: Player 0, Donor 10-40, Mod 80, Dev 90, Admin 100.
  **Enforce rank for every level.** Alloy only checks Admin, which is a bug.
- Migration: add `Rank` to `AccountModel`, and on load map `Ranked=true` → Admin. Keep `Ranked` readable
  for one release. `/commands` lists only the commands available at the caller's rank.
- **Gate:** every existing command behaves the same for a ranked account. A rank-0 account is denied
  everything above 0, tested by a scripted chat run through the harness.

### W13 — Leveled async file logging
- Today `Program.Print` → console only. The `release`/`debug` scripts redirect stdout to `server.log`.
- Add file sinks per level (`logs/{info,warn,error}/YYYY-MM-DD.log`) written by a background thread
  from the existing queue, with daily or size rotation. Alloy writes synchronously on the game thread;
  don't.
- **Gate:** no file I/O on the tick thread (verify with tracing). Logs survive a restart, and
  rotation works.

---

## Not porting (and why)

- **LiteDB + AccountServer/GameServer process split + named-pipe IPC.** In Alloy this is half-wired:
  the account lock is never persisted, the `DbWriter<Character>` is never started, and nothing is saved
  on logout. Our SQLite group-commit writer is stronger.
- **Alloy's validation model.** It trusts the client's Move position and has a literal `TODO: Validate hit`.
  Our movement, shot, hit and clock checks are far ahead.
- **Sparse-set ECS rewrite.** The win comes from W07/W08's data layout, not from a full entity-model rewrite.
- **Busy-spin game loop; 1000-slot preallocation; .NET 10 upgrade.** None is needed for the techniques above.
  Revisit .NET 10 only if a row needs a C# 14 feature.
