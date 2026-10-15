---
name: chat-command-harness
description: Headless chat-command check (tools/hop/chat_check.py) and the facts behind it: seeding ranks in the kv table, per-IP registration, why the script runs in the Nexus, the /legendary hang, what to mask when diffing replies. Verified 2026-10-15 (W11).
metadata:
  type: reference
---

- `tools/hop/chat_check.py`: `seed` registers and enters four accounts, each from its own loopback address
  (`127.0.0.2`-`.5`; `hop_client.Conn` takes `source=`), because registration is capped per IP per start.
  `run --accounts F --who P,A --skip-destructive [--script FILE]` sends the script and writes one JSON line per message
  (Text parsed, Notification/Failure as hex, Reconnect as gameId).
- Rank in the DB: account XML is `kv.value` at `account.<id>`. `<id>` comes from `login.username.<name>`. A pristine
  build seeds `<Ranked>true</Ranked>`, and both builds read it (the W11 build maps it to Admin). Stored `<Rank>` is
  written on export by the W11 build. Setting `<Rank>80</Rank>` in the XML gives a Mod account.
- Run in the Nexus. Realm monsters volley EnemyShoot; the headless client never sends ShootAck, so the server
  disconnects every client with "Proj ack timed out" after about 25 s. Keep any realm part short and last.
- Pre-existing hang (not fixed): `/legendary <slot>` spins forever when the item in the slot can never roll T7 (the
  `while ((roll.Item2 & ItemData.T7) == 0)` loop). The server freezes at about 130% CPU for every client. The seeded
  Rogue has item 2594 in slot 4, so use an empty slot (2 or 3).
- Proof the server read every message: the Debug build logs `Packet received <5>` for each PlayerText. Count those
  against the messages sent. A shortfall means a stall, not a parser problem.
- Diff noise between two runs of the SAME build: player positions (spawn/nudge), entity counts, quest coordinates,
  NPC chatter (`#Sheep`), and floating Notification packets. Mask these and drop them, then check that two pristine runs
  match before you trust a new-vs-pristine diff.
- `srv.sh`-style launchers must not wipe the DB dir between seed and run. `tools/hop/server_ctl.sh` wipes it at every start.
- Destructive commands: `/terminate`, `/stop`, `/reboot` (exit the server); `/disconnect`, `/dc`, `/dcAll` (drop clients);
  `/closerealm`, `/quake` (realm and world changes). A ranked run skips them. A rank-0 run keeps them, because the denial path is the test.
- `/dcAll` can never match: the switch label is mixed case and the input is lowercased. Preserved as "Unknown command".
