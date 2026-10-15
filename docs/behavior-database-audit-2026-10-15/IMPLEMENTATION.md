# Implementation handoff

Implementation was first stopped at the user's request and pushed as **`1d55cbe`** on `codex/enemy-behavior-audit-2026-10-15`. A follow-up review resumed from that handoff, corrected root-transition semantics, completed the automated verification below, and is recorded under [Follow-up review](#follow-up-review). No live server/client playtest has been performed.

## Implemented

- Added the 52 enemy definitions supported by existing target XML: Highland (34), Tutorial (11), Forbidden Jungle (3), Oryx Chicken (3), and Zombie Trickster (1).
- Added Snakepit Dart Thrower firing through `SnakePitTowerShoot`.
- Retained root-level transitions (previously discarded silently), prevented repeated transitions from re-entering the active root phase, and made `NoPlayerWithinTransition` check players only. The follow-up review changed how root transitions are stored and ordered; see below.
- Added per-entity Wander completion signaling so Prioritize can reconsider pursuit without pausing or doubling movement. Removed unintended self-target fallbacks in movement behaviors while preserving named targets.
- Changed Shoot/Grenade cooldown ordering to reference-compatible semantics and predictive aiming from a probability to a strength. At the current 10 TPS configuration, a 500 ms repeat cooldown becomes 600 ms; the reference's 20 TPS default produces 550 ms. Tick rate itself was not changed.
- Fixed Spawn initial-child counting and capped initial spawns.
- Replaced the unresolved Thessal Dropper chain with direct creation of the existing Ocean Vent, preserving the wounded-form chance. Vent creation is immediate rather than delayed by a helper tick; overlapping Thessals now produce a vent per death.
- Added regression tests, upstream attribution, and the upstream AGPL license text.

Reference-only enemies absent from target XML, ancillary pet gaps, and broad shared-enemy loot/balance differences were not filled wholesale.

## Verification completed before the stop

- Baseline and intermediate combined solution builds passed with zero warnings and errors.
- Offline registration check resolved all 52 added IDs and 586 behavior models. The new definitions' 75 Shoot projectile-index references were checked against target descriptors.
- The dedicated registration test passed.
- The test-infrastructure agent reported 8 focused engine/lifecycle and movement tests passing after correcting the test fixtures.
- Combat and Spawn/transform tests were added, but a final complete-suite result was not verified by the orchestrator before the stop. Concurrent agent build/test processes had caused stalled runs; use one test runner when resuming.

## Follow-up review

The review covered phase-transition precedence, prediction sampling, the cooldown change, and Spawn/Thessal behavior against the reference implementation (`wServer/realm/Entity.cs`, `logic/State.cs`, `logic/Transition.cs`, and `logic/behaviors/{Shoot,Spawn,Prioritize,Wander}.cs` at the pinned reference commit).

### Root transitions corrected

`1d55cbe` copied each root transition into every root phase. That made the phase lifecycle exit and re-enter them on every phase change, and tested them before the phase's own transitions. Both differ from the reference, where root transitions belong to the unnamed root state: they are never exited, and `TickState` tests the deepest active state first and the root last, stopping at the first transition that fires. Enumerating the loaded database showed 27 models with root transitions, nearly all pre-existing definitions whose root transitions had been dead code before this branch. Concrete consequences of the `1d55cbe` approach:

- **Thessal the Mermaid Goddess Wounded**: the root `TimedTransition(12000, "Fail")` restarted on every 250 ms texture swap, so the invincible form never failed or despawned.
- **Tomb Defender/Support/Attacker Statue**: once both sarcophagi were gone, the two root `EntityNotExistsTransition`s preempted the phase-local transition to `ItsGoTime` and alternated `checkActive`/`checkInactive` indefinitely, so the statues never transformed.

Root transitions now live only on `BehaviorModel.RootTransitions`. They are resolved against the root states, entered once in `InitStates`, never exited by phase changes, and tested after every active phase's own transitions only on ticks when no phase transition fired. This matches the reference's `transited` short-circuit, including its consequence that a root timer does not advance on ticks where a phase-local transition switches phases. A root transition that targets the active root phase is still a no-op. Root `PlayerTextTransition`s now receive chat as well. State switching was extracted into `Entity.TrySwitchState` without changing nested-state semantics.

### Septavius definition

The target's custom Septavius definition (unlike the reference's `Idle`/`Cycle` structure) declared `PlayerWithinTransition(8, "transition1")` at root. Once enabled, that condition stays true during the fight, so in either ordering it returns the boss to `transition1` whenever a phase has no pending transition, and the boss cannot progress. It is now an initial `Idle` phase. Because idle enemies without nearby players do not tick, the wake-up is effectively unchanged.

The other enabled root transitions were checked and judged intentional: terminal `Die`/`Suicide`/`Off` targets (Gold Planet, Beach Spectre Spawner, Red Gas Spawners, Limon Elements), idle/chase pairs (Tomb minions, Scarab), Sheep, TestChicken 2, and the Sarcophagi. Active Sarcophagus's root `HpLessTransition(60, "stun")` re-stuns every 1.5 s after it falls below the threshold; that is the same in the reference structure.

### Other WIP changes reviewed

- **Shoot/Grenade cooldown**: checking before decrementing matches the reference `Shoot.TickCore`. Kept.
- **Predictive aim**: matches the reference formula (angular velocity × projectile speed / 100 × strength), with the target sampled about 100 ms back and divided by the actual sample time. The reference's `TryGetHistory` ignores its argument and always returns the previous tick (50 ms at 20 TPS) but still divides by 100 ms, so its effective lead is about half of the nominal formula. The target implements the nominal formula; this is a documented difference, not a defect fix. The target additionally wraps the angular change across ±π.
- **Spawn**: counting initial children once and resetting the counter on entry matches the reference `Spawn.OnStateEntry`. Kept.
- **Prioritize/Wander**: the per-host cycle-complete signal reproduces the reference's `CycleStatus.Completed` reselection without double movement. Kept.
- **Thessal → Ocean Vent**: direct `TransformOnDeath("Ocean Vent")` is covered by an existing test. Kept.

### Test fixes

- The registration test now counts a freshly loaded `BehaviorDb`. Previously, other tests replaced models on the shared database, making its 586-model assertion order-dependent.
- Fake test players get an empty visible-entity set, so shots fired near them do not dereference a missing client.
- Added regressions: a root timer survives phase changes, active-phase transitions are tested before root transitions, and Septavius wakes once and reaches `spiral`. The first two fail against the `1d55cbe` engine.

## Verification

Run with .NET SDK 8.0.426 on Linux:

- `dotnet test tests/RotMG.BehaviorTests/RotMG.BehaviorTests.csproj -c Release`: **23 passed, 0 failed**, with no test-project warnings.
- `dotnet build RotMG.sln -c Release` and `-c Debug`: 0 errors. Release reports one warning, CS0169 for `Client._lastQueueDebugMs`, which also occurs on the unchanged starting revision.
- The test project is not part of `RotMG.sln`; run it explicitly as above.

## Remaining work

1. Exercise representative encounters with the real client, especially the Highland additions, Thessal (both death outcomes), Tomb of the Ancients statues, Septavius, and Snake Pit dart towers. No live playtest has been performed.
2. Decide whether nested-state transition order should also follow the reference (deepest first). The target tests shallower states first. This review deliberately left that pre-existing difference unchanged and matched the reference only for root transitions.
3. Reference-only enemies absent from target XML, ancillary pet gaps, and broad shared-enemy loot and balance differences remain out of scope, as above.

The earlier audit documents describe the pre-implementation source snapshot. Their observations remain historical evidence, not a description of every behavior after these changes.
