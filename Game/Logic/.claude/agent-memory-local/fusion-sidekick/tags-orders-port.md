---
name: tags-orders-port
description: where the tag, global-tag, orders (FollowOrders/OrderDb) and new C# transitions live, and the decisions behind them; read before extending or debugging them
metadata:
  type: project
---

Ported from the old C++ engine (/home/kevin/Desktop/RotMG/files): tags, global tags, census/exist transitions, chat,
onTile, FollowOrders and the forward shoot. Built and verified 2026-11-08 by compiling a copy (see
[[build-lock-compiler-server]]).

**Files:** Transitions/{AllTransition,RandomTransition,TagTransition,GlobalTagTransition,WorldEntitiesTransition,
PlayersWithinTransition,PlayerSaysTransition,OnTileTransition}.cs; Behaviors/{SetTag,ClearTag,SetGlobalTag,
ClearGlobalTag,ShootForward,FollowOrders}.cs; Game/Logic/OrderDb.cs.

**Shared-file additions (additive only):** Entity.cs has the tag set and HasTag/AddTag/RemoveTag. World.cs has global tags
(HasGlobalTag/AddGlobalTag/RemoveGlobalTag), live type counts (CountLiveOfType, updated in AddEntity/RemoveEntity only when
the dictionary actually changes), and RecentChats (AddRecentChat, PruneRecentChats; 3 s window, `RecentChatWindowMs`).
Player.Chat.cs has one line calling `Parent?.AddRecentChat(Position, validText)`, which is the only chat feed.

**Decisions:**
- PlayerSays does not reuse PlayerTextTransition.OnChatReceived. That path is regex-based, non-virtual, latches one match
  per instance, and carries no position or time. Chat goes through World.RecentChats instead.
- ShootForward keeps its per-entity track in StateObject[-Id]. Shoot uses StateObject[Id] only with RotateAngle, and
  behavior ids are positive. The direction is refreshed in Tick (every tick), not only at fire time.
- FollowOrders exits the current block when the leader is lost, unlike the reference, which kept the stale state until
  the next orders change. The search timer runs as in the reference.
- OrderDb keys are leader type plus '/'-joined lowercase state path, with "" as leader-wide. Lookup goes deepest state
  first, then "", and the first block whose targets match wins. Same (leader, path, target set) replaces in place.
- The C++ recentChats lifetime is not in /files, so the 3 s window is a guess. Verify it against the old game if
  PlayerSays timing matters.

**Not ported:** SetGlobalTag's AddInitialNotification list for late joiners.
