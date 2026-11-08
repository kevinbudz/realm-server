---
name: behavior-engine-port-facts
description: Verified C# behavior-engine constraints for porting old C++ behaviors (kill without a player, pet-owned hitters, empty chests, PlaceMap paths, packet span lifetime)
metadata:
  type: project
---

Verified 2026-11-08 while porting 26 C++ behaviors (MakeMinions..VictoryAtDeath) into Game/Logic/Behaviors.

- Enemy.Death(Player killer) is the only path that grants loot, XP and kill stats. Behaviors reach it through Game/Logic/Behaviors/BehaviorDeath.cs (Die(host, dropLoot)): killer = highest non-zero DamageStorage entry that is a live non-Bot Player; with no killer it runs the Death hooks, sets Dead and removes the host (Suicide.cs pattern).
- Invulnerable hits still write zero-value DamageStorage entries, so zero-damage records must not count as a killer.
- Enemy.Damage and Enemy.ApplyPoison need a Player hitter. Only Pet (Game/Entities/MiscEntities.cs, PlayerOwner) has an owner. Zap, TrapTrigger and Shadow therefore act only on Pet hosts.
- Container.Inventory is int[8] with -1 for empty, and a fresh container is all -1. Setpiece Treasure Chests are Containers spawned empty, so an "empty means remove" rule deletes them on the first tick.
- BehaviorDb.Init(string[] ids, ...) shares one behavior instance across several types. Host-dependent defaults (host type, host group) must be resolved per entity in Enter, not cached on the instance.
- GameServer.* returns a span over a shared PacketWriter buffer. It is valid only until the next Rent, so send each packet before building the next.
- GameServer.PlaySound takes a sound name, not a numeric id. No packet exists for PlaceMap-style sound ids.
- SetPieces.RenderSubMap and GetSubMap read only Resources/Worlds/Setpieces. None of the XML PlaceMap paths (abyss/, lairOfDraconis/, ...) exist there.
- No always-visible set exists (RemainVisible is a no-op). No stats counter exists (VictoryAtDeath skips oryx_kills).
- XML behaviors are written as <Behavior attrs>Name</Behavior>, not as tags named after the behavior.
- Builds must go through flock on /tmp/claude-1000/.../scratchpad/build.lock. Another agent holds it often, so a build can queue for minutes.

**How to apply:** when a new behavior needs a kill with loot, a non-pet owner, or a sound id, check these facts first. They are engine limits, not bugs in the port.
