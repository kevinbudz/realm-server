---
name: hop-latency-finding
description: Escape->nexus hop on the C# server. Was ~1.1 s on ext4 (synchronous Hello account save); W02 async save -> ~72 ms; W04 join frame + fixed tick schedule -> ~10 ms median (max ~52 ms, Nagle outliers). [hop] instrumentation still uncommitted.
metadata:
  type: project
---

Measured with 12 headless hops per variant on the Debug build (see [[headless-hop-harness]]).

Before W02 (2026-10-14, ext4 DB, synchronous=FULL): Escape -> first nexus Update median ~1110 ms / max ~1117 ms.
The Hello `client.Account.Save()` -> `Database.SetKey` (~1005 ms) collided with the writer's commit of the old session's disconnect save. synchronous=NORMAL or a tmpfs DB gave ~52 ms, but synchronous=FULL is a durability decision, so do not change it without a user call.

After W02 (Hello calls `AccountModel.SaveAsync()`, Common/Models.cs:468, which exports on the tick thread and enqueues via `Database.WriteAtomically`):
- median 72.5 ms / max 108.5 ms (`run.sh --variant w02 --max-median-ms 150`, PASS). `SetKey(account.*)` on the hop path: 0 calls. Hello rx -> MapInfo ~4 ms. The rest is the next-tick wait (CreateSuccess -> first Update median ~63 ms, W04's job).
- Duplicate-login and relog checks (`tools/hop/dup_login.py`): 13/13 before and after, 16/16 after (adds an immediate second Hello).

After W04 (join frame: `Player.SendJoinFrame()` from Load/Create, Player.Update.cs; fixed tick schedule in `Manager.Tick`, Game/Manager.cs):
- median 10.08 ms / max 51.87 ms (`run.sh --variant w04 --max-median-ms 150`, PASS). Server writes CreateSuccess + first Update + NewTick within ~1 ms of each other.
- Max outliers (3/12 hops, ~40 ms CreateSuccess -> first Update at the client): Linux delayed-ACK timeout. Game sockets are accepted in `Networking/GameServer.cs:222` with no `NoDelay` (the only NoDelay is in `RejectSocket`, :319), so the second small write waits on Nagle. In a scratch copy with `_socket.NoDelay = true` in `Networking/Client.cs:376` the max fell to 17.0 ms, median unchanged. Applied in Part A (Client.cs BeginHandling). Re-run: median 9.52 ms, max 12.89 ms.
- Tick period (Debug, `REALM_TICKTRACE=1`, `tools/hop/tick_period.py`): old gate with 30 ms load = 70.5 ms mean (ticks run fast); fixed schedule = 100.00 mean, p95 100.9, max 101.8 ms.

**Why:** the Hello save added no new state except the Connected flag, so it can be async. The DB `Account.Connected` is never a login gate: `Database.IsAccountInUse` (Database.cs:1133-1155) reads it only to clear it in memory. The gate is `Manager.GetClient` / `HasHandoff`, plus `Manager.LinkClient` on the same tick (GameServer.Handlers.cs:642).

**How to apply:** keep that invariant. Any new "in use" gate must use the in-memory session map, not the DB flag. Known pre-existing race, not introduced by W02: a non-handoff Hello reads the account with GetKey (GameServer.Handlers.cs:563) and can see a snapshot older than a queued async disconnect save for the same account (Client.cs:302, `Disconnect()` path). The window is the writer's pending time (hop batches: median ~1.2 ms, max ~6 ms). Fixing it is a design call for W03.

**Working tree state (uncommitted):** `[hop]` instrumentation in Common/HopTrace.cs (new), Common/Database.cs, Networking/Client.cs, Networking/GameServer.cs, Networking/GameServer.Handlers.cs. It is opt-in (`REALM_HOPTRACE=1`). `tools/hop/analyze.py:36` still matches the old label "after acc.Save", so the Hello row shows `-`. Rename it to "after acc save enqueued" to fix that. Recheck line numbers before relying on them.
