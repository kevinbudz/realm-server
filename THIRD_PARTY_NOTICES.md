# Third-party source attribution

The following enemy behavior registrations and behavior adaptations are derived from or adapted from `ossimc82/fabiano-swagger-of-doom`, pinned at commit `6fd20aad4a7905b13f25389c68368a942a2b68cb` (2015-11-09). That repository is licensed under GNU Affero General Public License version 3.0; its license text is included at [licenses/fabiano-AGPL-3.0.txt](licenses/fabiano-AGPL-3.0.txt).

The directly ported database definitions are:

- `Game/Logic/Database/ForbiddenJungle.cs`
- `Game/Logic/Database/Highland.cs`
- `Game/Logic/Database/OryxChicken.cs`
- `Game/Logic/Database/Specialized.cs`
- `Game/Logic/Database/Tutorial.cs`

Reference behavior code was also adapted for the Snake Pit tower attack and for attack/projectile timing and prediction in `Game/Logic/Behaviors/SnakePitTowerShoot.cs`, `Game/Logic/Behaviors/Shoot.cs`, and `Game/Logic/Behaviors/Grenade.cs`, plus the Snake Pit database wiring in `Game/Logic/Database/SnakePit.cs`. The changes retain this server's existing APIs and XML identifiers.

The upstream README requests credit for the following contributors: ossimc82 / Fabian Fischer, C453, Trapped, Donran, creepylava, Krazyshank, Barm, Nilly, sebastianfra12, and Kieron, along with other contributors to the project. The upstream README also specifically excludes two named MPGH accounts from its permission statement; that restriction is preserved here as source attribution context.

This notice documents provenance for the files and adapted portions above. It does not state or infer a license for the rest of this repository.
