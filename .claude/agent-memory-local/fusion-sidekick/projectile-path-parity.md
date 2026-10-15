---
name: projectile-path-parity
description: W09 projectile math facts - client Speed truncation, negative-id parity, golden harness workflow, why bots cannot soak TryHitEnemy
metadata:
  type: project
---

Closed-form projectile paths (W09) live in `Game/ProjectilePath.cs` (client twin of realm-cpp `src/objects/ProjectileProperties.cpp`), with `--projpath-verify [golden.bin]` in `Game/ProjectilePathVerify.cs`. Status is in the W09 handoff, not here.

- **Client Speed is truncated.** realm-cpp `Catalog.cpp` parses Speed with `integer()`, so XML `Speed 0.02` (Oryx Knight projectile id 1) is speed 0 on the client, a bullet that never moves. The server's `ProjectileDesc.Speed` is a float, so `ProjectilePath` truncates it. Do not change the shared descriptor parse without updating the path math.
- **Negative bullet ids use AS3 truthiness.** Client-sided ids are negative. The client's `id % 2 ? 1 : -1` is truthy, so a negative odd id counts as odd. A C# `Id % 2 == 1` test flips the parametric x sign for those ids. The pre-W09 `Projectile.PositionAt` had this bug.
- **Bots cannot soak TryHitEnemy.** `Bot.TryBotShoot` (`Game/Entities/Bot.cs`) calls `HitByProjectile` directly, and no tool in `tools/hop` sends PlayerShoot or EnemyHit. A replay through the real `ResolveEnemyHit` is the available substitute (`Game/Entities/Player.ProjectileReplay.cs`).
- **Golden workflow.** `tools/projpath/extract_descs.py` writes the desc TSV with client parse rules. `golden_harness.cpp` is compiled with `-std=c++20 -O2 -ffp-contract=off` against realm-cpp's `include/` and `src/objects/ProjectileProperties.cpp`. Its binary output is then checked by the self-check. Run the self-check from a throwaway cwd.
- **Measured 2026-10-14.** 935 descs, 24 probes each, 3,680,424 samples. Linear, wavy, parametric and boomerang match realm-cpp bit-for-bit. Amplitude descs are within 5.4e-8 tiles, because Amplitude and Frequency are stored as floats.
- **Scratch shell gotcha.** The Bash tool runs zsh, which does not word-split unquoted variables. Put file-list loops in a `bash` script.

**Why:** these were found by the golden check, not by reading the code. The truncation case changed the server's answer for a real projectile.
**How to apply:** when touching projectile math, rerun the golden check, not only the anchors. Do not infer that all Speed values are integers from a partial scan.

Related: [[headless-hop-harness]] (alternate ports via `REALM_HOP_HTTP_PORT` and `REALM_HOP_TCP_PORT`, and `REALM_HOP_STATE` for logs).
