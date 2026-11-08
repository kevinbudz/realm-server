---
name: hand-written-toolkit-behaviors
description: 15 hand-written, unused C# behavior/transition classes restored 2026-11-08 (HealPlayerMP..NotMovingTransition); they are not converter output and must survive regeneration
metadata:
  type: project
---

Restored into Game/Logic/Behaviors (HealPlayerMP, KillPlayer, MutePlayer, BringEnemy, TeleporttoTarget, ScaleHP,
MultiplyLootValue, RemoveTileObject, ShootForward, RemoveOnEmpty) and Game/Logic/Transitions (EntityWithinTransition,
AnyEntityWithinTransition, NoEntityWithinTransition, EntityHpLessTransition, NotMovingTransition). Each has a line
`//Hand-written toolkit: not emitted by Tools/XmlBehaviorConverter (files/xml has no equivalent).` above the class.

**Why:** the converter has no XML source for these, and the user wants them kept as an unused toolkit. Regenerating
Game/Logic/Database must not delete them. Source of the old code: a scratch snapshot of Game/Logic taken before stage 2.

**Facts verified 2026-11-08:**
- Ported text is the snapshot text plus the one comment line. It compiles against the current API with no other edits.
  The 13 tracked files differ from HEAD by that line only. ShootForward and RemoveOnEmpty are untracked, not in HEAD.
- EntityHpLessTransition already used strict `<` and derives from Transition directly. It never used HealthTransition,
  which is the `<=` class and is not restored. No edit was needed.
- ShootForward depends on Shoot's protected virtual GetFixedAngle hook. Shoot.cs is unchanged from the snapshot.
- RemoveOnEmpty keeps a public helper class RemoveOnEmptyState in the same file.
- The converter still drops XML Shoot type="forward" and RemoveOnEmpty with a report line. That is intentional.

**How to apply:** when touching these files, keep the toolkit comment. Do not add them to the generated Database
without a request. Before reusing them, recheck that Shoot, BehaviorHelpers and ChunkController.AnyInRadius still match.
