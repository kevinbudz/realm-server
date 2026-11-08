---
name: build-lock-compiler-server
description: scratch build under /scratchpad/build.lock can stall for 10+ minutes because a leftover VBCSCompiler server inherits the lock fd; verify with lslocks and build an isolated copy instead
metadata:
  type: reference
---

The mandated build (`flock .../build.lock dotnet build ... -o .../buildA`) can queue for a long time. The `VBCSCompiler`
(Roslyn server) that `dotnet build` spawns inherits the flock fd and stays alive idle, so the lock is held after the
build finishes. Every later build then waits for that server to exit, and each new build spawns another one.

**Diagnose:** `lslocks | grep build.lock` shows the holder. A `VBCSCompiler` PID means the stall is the compiler server,
not a live build. Other builds appear as `flock` processes in `ps`.

**Do not** kill another agent's compiler server or queued flock without authorization.

**Workaround used (2026-11-08):** copy the sources (`rsync -a --exclude bin/ --exclude obj/`) into a scratch dir, then run
`dotnet build` there with output in scratch. The copy has its own obj/, so it does not touch the shared tree or
interfere with the lock holder. Report the copy build as an isolated verification, not as the locked build.

**Also:** a cancelled queued `flock` for my own build leaves its background task marked completed, with the log
truncated at queue time. Check the log size and `DOTNET_EXIT` before trusting it.

**Verified 2026-11-08:** `flock <scratch>/build.lock dotnet build RotMG.csproj -c Debug -o <scratch>/buildX -p:UseSharedCompilation=false`
finished in about 4 s with exit 0 and released the lock. `-p:UseSharedCompilation=false` starts no VBCSCompiler, so the
stall above does not happen. Use that flag for every build.
