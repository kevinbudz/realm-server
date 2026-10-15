# Behavior implementation comparison

This follow-up compares the target implementation at [`bb75bc1e438b465986a8ada354b6332f69e0e3a6`](https://github.com/kevinbudz/realm-server/tree/bb75bc1e438b465986a8ada354b6332f69e0e3a6) with the candidate reference at [`6fd20aad4a7905b13f25389c68368a942a2b68cb`](https://github.com/ossimc82/fabiano-swagger-of-doom/tree/6fd20aad4a7905b13f25389c68368a942a2b68cb), dated 2015-11-09. It answers whether underlying behavior implementations can make enemies pursue, wander, or fire differently even when definitions use similar settings. The reference postdates the requested RotMG 7.0.0 (2014) history and is not authority for intended 2014 behavior.

## Findings

Equivalent unslowed `Follow` and `Wander` settings use the same speed-to-distance-per-second conversion. At the checked target configuration of 10 ticks/second and the reference default of 20 ticks/second, target takes larger steps less often; ordinary movement does not have a general faster-speed multiplier. While Slowed, the formulas differ: target uses `(5.55 × speed + 0.74) / 2`, reference uses `2.22 × speed + 0.74`. At `Wander(0.4)`, target is about 9.1% slower; at `Follow(1)`, target is about 6.25% faster. The formulas cross at speed `2/3`. This comparison applies only while an enemy is affected by Slowed and is not immune.

Movement scheduling can change whether an enemy moves or pursues. Target `Prioritize` can keep its `Wander` fallback selected after it returns true, so it may fail to reconsider `Follow` when a player moves back into range. Target generic entity lookup can select the host itself, causing conditional idle circling or a no-op `StayBack`; its `NoPlayerWithinTransition` can also self-match when evaluated. In the shared Scarab definition, root-level target transitions are dropped, so the declared chase state is unreachable through those player-distance conditions. These are conditional movement/state-selection differences, not evidence that target movement speed is globally higher. The movement report details a target-only axis-aligned charge trigger and Orbit direction/radius differences.

There is a concrete repeat-fire timing difference. After a positive cooldown `C` is reset, target checks after subtracting one tick, yielding `ceil(C / dt) × dt`; reference checks before decrementing, yielding `(ceil(C / dt) + 1) × dt`. With target 10 TPS and reference default 20 TPS, the matched Brown Den Spider’s 500 ms attack repeats at 500 ms vs 550 ms (2 vs about 1.82 volleys/second); a matched 750 ms Beholder attack resolves to 800 ms on both. The target can therefore fire up to one reference tick sooner for these positive cooldowns, depending on `C` and tick settings, rather than firing universally faster. Target cooldown variance is also quantized to its tick interval. Both implementations perform the initial offset-zero shot on the first behavior tick; the cadence difference concerns repeats.

Other implementation differences can change attack results without changing repeat cadence: target `Shoot.predictive` treats the value as a probability of fixed four-tick linear lead, while reference uses continuous speed-scaled angular correction; target reduces projectile count while Dazed, while reference does not. Grenade argument surfaces and fixed-angle origins also differ; the combat report limits these to implementation/porting implications where no matched active enemy case supports a broader claim.

## Detailed reports

- [Scheduler, tick timing, state transitions, and target lookup](engine.md)
- [Follow, Wander, Charge, StayBack, and Orbit](movement.md)
- [Shoot cadence, aiming, status effects, Grenade, and Flash](combat.md)
- [Cooldown helpers, Spawn/Reproduce, transitions, and lifecycle](timing-and-lifecycle.md)

## Corrections to the definition audit

This follow-up caught and corrected two implementation interpretations in the earlier reports. In [`overworld.md`](../overworld.md), the Elf Swordsman’s reference `Buzz` constructor accepts a 2,000 ms value, but the active behavior does not use it; the earlier claim that the reference pauses between dashes was removed. In [`specialized.md`](../specialized.md), [`review-notes.md`](../review-notes.md), and [`bosses.md`](../bosses.md), the reference `Spawn` initial-spawn loop was described as incrementing its counter; it does not. Consequently, Big Creampuff’s initial count remains one and its cooldown can produce a second child, Spoiled Creampuff’s count remains four (above its cap), and Cube God’s initial counts remain 15/25/25 (above their caps). The corrected statements are in those linked reports.

This is a static source comparison, not a runtime replay or historical reconstruction. Tick rates can be overridden; lag, map collision, player visibility, effect state, enemy immunity, and behavior entry timing affect observed results. It is selective across implementation details and does not claim to cover every behavior primitive or every enemy callsite. No runtime tests or server simulations were run, and no gameplay code was changed.
