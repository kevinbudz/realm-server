# Implementation handoff

Implementation was stopped at the user's request and pushed as **`1d55cbe`** on `codex/enemy-behavior-audit-2026-10-15`. Treat this as work in progress, not a release-ready change. No further implementation or testing was performed after the stop request.

## Implemented

- Added the 52 enemy definitions supported by existing target XML: Highland (34), Tutorial (11), Forbidden Jungle (3), Oryx Chicken (3), and Zombie Trickster (1).
- Added Snakepit Dart Thrower firing through `SnakePitTowerShoot`.
- Retained root-level transitions, prevented repeated transitions from re-entering the active root phase, and made `NoPlayerWithinTransition` check players only.
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

## Resume here

1. Fetch this branch and review the WIP implementation, especially phase-transition precedence, prediction history sampling, and the deliberate cooldown compatibility change.
2. Run the complete regression project using a .NET 8 SDK:

   ```sh
   dotnet test tests/RotMG.BehaviorTests/RotMG.BehaviorTests.csproj -c Release
   dotnet build RotMG.sln -c Release
   ```

3. Resolve any failures without assuming they are fixture-only. Confirm whether the test project is included in the solution; the explicit test-project command above works independently of solution inclusion.
4. Review Spawn/Thessal behavior and exercise representative encounters with the real client. No live server/client playtest was completed.
5. Complete final integration review and update this verification record before calling the implementation finished.

The earlier audit documents describe the pre-implementation source snapshot. Their observations remain historical evidence, not a description of every behavior after this WIP change.
