---
name: w03-writer-overlay
description: W03 DB write design - read-your-writes overlay in Database.GetKey, outcome-gated bundles (WriteAtomicallyThen), PendingJoin token for Create, soak.py usage and its gotchas. Verified 2026-10-14.
metadata:
  type: project
---

Design (Common/Database.cs): queued plain bundles register their keys in an overlay (`_pending`, `QueuePlain`), and GetKey returns the newest queued value first (`ReleasePending` drops entries after the commit attempt). So main-thread check-then-write sees earlier async writes, and purchases/renames/registration check and write on the tick thread with no blocking. `WriteAtomicallyThen(writes, deletes, Action<bool>)` runs the outcome on the main thread after commit (true) or failure (false), and the HTTP reply is sent from there.

**Why:** the writer is FIFO, so a debit queued before a check is committed before the next job runs. The overlay makes the check see it without waiting. No Hello deferral was needed: all account writes from the tick thread are plain bundles.

**How to apply:** new tick-thread writes use `Save()` (queued) or `WriteAtomicallyThen` (ack after commit). Do not add SetKey/WriteAtomicallyAndWait/Transact on the tick thread. Func jobs (TransactAsync/Transact) are not in the overlay. Keep RMW that needs in-transaction reads on the writer.

Create: `Database.PrepareCharacter` + `CommitCharacter`. `Client.PendingJoin` is a token per Create; FinishCreateJoin joins only if it is still current. Load is guarded on `PendingJoin == null`.

soak.py (`tools/hop/soak.py`): the server allows one registration per IP, so it registers one base account over HTTP and clones the rest into realm.db (same hash). Login names change on set-name, so rename accounts must be separate from buy accounts (a renamed account gets "Invalid account." on the next call). The rename double-spend test is weak for this reason: only one success, but the other seven fail on login. The slot double-spend test uses a stable name and is meaningful.

Gotcha (confirmed 2026-10-15): the tick spike under hop churn (max 45-140 ms) is the Poison Scorpion (0x0603) entity tick. Its Protect behavior calls BehaviorHelpers.NearbyEntities, which yields every entry of World.Statics (~267k in the nexus) per query. Skipping that scan (REALM_TOGGLE_NOSTATICS=1, measurement only) cuts the tick max from 101 ms to 12 ms. The join frame is not the cause (spike persists with REALM_TOGGLE_NOJOIN=1). Pre-existing; defer to W08 (spatial index for statics). Trace: REALM_TICKTRACE=1 prints [tickworld] with the top entity types per slow tick.

Overlay bound: pending entries peak at ~3 under soak and return to 0 (trace field pending=). Entries are released on commit, rollback, a failed batch, writer death (FailJobs) and fallback.

Rule (2026-10-15): never build into the repo bin/. The live server runs from bin/Release/net8.0 (PID 80730, ports 2050/7777). Harness builds go to $STATE/build/<config> via dotnet build RotMG.csproj -o.
