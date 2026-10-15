# Enemy behavior database audit — 2026-10-15

This audit compares enemy behavior database definitions in `kevinbudz/realm-server` at [`bb75bc1e438b465986a8ada354b6332f69e0e3a6`](https://github.com/kevinbudz/realm-server/tree/bb75bc1e438b465986a8ada354b6332f69e0e3a6) with the candidate `ossimc82/fabiano-swagger-of-doom` snapshot at [`6fd20aad4a7905b13f25389c68368a942a2b68cb`](https://github.com/ossimc82/fabiano-swagger-of-doom/tree/6fd20aad4a7905b13f25389c68368a942a2b68cb), dated 2015-11-09. The requested historical version is RotMG 7.0.0 (2014). The reference postdates it and is a comparison corpus, not historical authority; a difference alone does not establish a defect or a 2014 omission.

The scope is enemy behavior database definitions: behavior states, phases, movement, attacks, transitions, spawns, and loot. Primitive implementations were inspected only where needed to interpret a reported enemy-specific effect (for example, spawn counters or an unresolved transform); this is not a standalone primitive catalog, a general non-enemy systems audit, or a runtime balance test. Source links in the detailed reports are pinned to the commits above.

## How to read coverage

The machine inventory is built from target XML object IDs, target behavior registrations, reference XML IDs, and reference behavior definitions that are active in the reference build. “Active” excludes behavior source that is not part of the reference project's compile list and behavior fields the loader does not discover at runtime. `InitMany` registration expansion and the reference's dynamic pet loader are included. See the coverage and registration reports for extraction rules and per-ID evidence.

A target XML enemy ID with no target behavior registration is reported as a definition-coverage gap. A candidate reference behavior whose ID is absent from target XML is reported separately as candidate-only catalog coverage. That second category is not a confirmed historical omission: the 2015 candidate cannot establish that an enemy existed in 2014. Name matching is exact; aliases and versioned names can affect apparent gaps and are recorded where identified. Static catalog coverage is exhaustive within the extraction rules, while semantic comparison is selective and does not claim exhaustive parity.

## Findings

The coverage inventory identifies **52 reference enemy definitions** whose IDs exist in the target's own XML but have no target behavior registration. A separate **49 reference-only enemy IDs** are present in the 2015 candidate's XML and active behavior database but absent from target XML; they are candidates for historical investigation, not established 2014 omissions. Ancillary non-enemy/helper/pet rows are separated in the coverage report.

Among target XML-supported gaps, 34 Highland enemy definitions have no target database module, and the target realm population still schedules several of them. This is the clearest player-facing coverage issue in the audit. A separate target-specific mismatch registers `Thessal Dropper` without a target XML object; the death transform that references it silently cannot resolve its destination. The semantic reports also identify concrete encounter and loot differences across the shared definitions; these remain source-to-source comparisons, not claims about which revision is historically correct.

## Detailed reports

- [Named event bosses and Oryx](bosses.md)
- [Specialized, custom, and later definitions](specialized.md)
- [Overworld behaviors](overworld.md)
- [Dungeon behaviors](dungeons.md)
- [Dungeon minions and support enemies](dungeon-minions.md)
- [Exhaustive registration coverage](coverage.md)
- [Machine-readable coverage rows (TSV)](coverage.tsv)
- [Machine-readable full inventories (JSON)](coverage.json)
- [Review and validation notes](review-notes.md)
- [Registration and loader validation](registration-validation.md)

No runtime tests or server simulations were run; this is a static source, catalog, and project-configuration audit. No gameplay code was changed.
