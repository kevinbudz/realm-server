#!/usr/bin/env bash
# server_ctl.sh - start/stop a throwaway RotMG server for the hop harness (alternate ports).
#
#   server_ctl.sh start  <variant> [--config Debug|Release] [--trace on|off]
#   server_ctl.sh stop   <variant>
#   server_ctl.sh status <variant>
#
# Safety rules (keep them if you edit this file):
#  - Never writes under the repo's bin/. The server runs with cwd = <state>/run-<variant>, which holds
#    a generated Settings.xml and a Resources symlink into $STATE/build/<config> (read-only use).
#    The server reads Settings.xml and its res= directory relative to its cwd (Common/Settings.cs:39-47).
#  - Generated Settings.xml = the repo's Settings.xml with <Ports>7877:2060</Ports> and db= pointed at a
#    per-variant DB dir. The live Settings.xml and bin/*/net8.0/Database/realm.db are never opened.
#  - Refuses to start if 7877 or 2060 is already listening. Never signals a process it did not start.
#  - stop signals only the PID it recorded, and only after /proc/PID/cwd == its run dir.
#
# Environment (all optional):
#  REALM_HOP_CONFIG     Debug or Release (default Debug; --config overrides)
#  REALM_HOP_STATE      state root for run dirs, logs, pid files (default ${TMPDIR:-/tmp}/realm-hop)
#  REALM_HOP_DB_ROOT    root for the throwaway DBs (default ${XDG_CACHE_HOME:-$HOME/.cache}/realm-hop-db).
#                       Keep it on disk. A tmpfs DB (e.g. under /tmp here) gives ~50 ms, not the
#                       ~1.1 s synchronous=FULL case this harness exists to measure.
#  REALM_HOP_HTTP_PORT  AppServer HTTP port (default 7877)
#  REALM_HOP_TCP_PORT   GameServer TCP port (default 2060)
#  REALM_HOP_LAUNCH     apphost (default, ./RotMG like the `debug` script) or dll (dotnet RotMG.dll)
set -u

HOP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
REPO="$(cd "$HOP_DIR/../.." && pwd -P)"
STATE_ROOT="${REALM_HOP_STATE:-${TMPDIR:-/tmp}/realm-hop}"
DB_ROOT="${REALM_HOP_DB_ROOT:-${XDG_CACHE_HOME:-$HOME/.cache}/realm-hop-db}"
HTTP_PORT="${REALM_HOP_HTTP_PORT:-7877}"
TCP_PORT="${REALM_HOP_TCP_PORT:-2060}"
LAUNCH="${REALM_HOP_LAUNCH:-apphost}"

die() { echo "server_ctl: $*" >&2; exit 1; }
usage() {
    echo "usage: $0 start <variant> [--config Debug|Release] [--trace on|off] | stop <variant> | status <variant>" >&2
    exit 2
}

cmd="${1:-}"; [[ -n "$cmd" ]] || usage; shift
variant="${1:-}"; [[ -n "$variant" ]] || usage; shift
[[ "$variant" =~ ^[A-Za-z0-9._-]+$ ]] || die "variant must match [A-Za-z0-9._-]+"

CONFIG="${REALM_HOP_CONFIG:-Debug}"
TRACE=on
while [[ $# -gt 0 ]]; do
    case "$1" in
        --config) CONFIG="${2:-}"; shift 2 ;;
        --trace)  TRACE="${2:-}"; shift 2 ;;
        *) usage ;;
    esac
done
case "$CONFIG" in Debug|Release) ;; *) die "config must be Debug or Release" ;; esac
case "$TRACE" in on|off) ;; *) die "--trace must be on or off" ;; esac
case "$LAUNCH" in apphost|dll) ;; *) die "REALM_HOP_LAUNCH must be apphost or dll" ;; esac

OUT="$STATE_ROOT/build/$CONFIG"  #built by run.sh with dotnet build -o; never the repo's bin/ (the live server lives there)
RUN="$STATE_ROOT/run-$variant"
PIDFILE="$STATE_ROOT/pids/$variant.pid"
LOG="$STATE_ROOT/logs/$variant-server.log"
DBDIR="$DB_ROOT/$variant"
RUN_REAL=""
[[ -d "$RUN" ]] && RUN_REAL="$(cd "$RUN" && pwd -P)"

port_busy() {
    if command -v ss >/dev/null 2>&1; then
        ss -ltn 2>/dev/null | grep -q ":$1[[:space:]]"
    else
        (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null
    fi
}

# True while the PID exists and is not a zombie.
alive() { [[ -d "/proc/$1" ]] && ! grep -q '^State:[[:space:]]*Z' "/proc/$1/status" 2>/dev/null; }

# True only for a RotMG process whose cwd is this variant's run dir.
owned() {
    local cwd
    [[ -n "$RUN_REAL" ]] || return 1
    cwd="$(readlink "/proc/$1/cwd" 2>/dev/null)" || return 1
    [[ "$cwd" == "$RUN_REAL" ]] && tr '\0' ' ' < "/proc/$1/cmdline" 2>/dev/null | grep -q 'RotMG'
}

cmd_stop() {
    if [[ ! -f "$PIDFILE" ]]; then
        echo "server_ctl: no pid file for $variant; nothing to stop"
        return 0
    fi
    local pid
    pid="$(cat "$PIDFILE")"
    if [[ ! "$pid" =~ ^[0-9]+$ ]]; then
        echo "server_ctl: bad pid file $PIDFILE; left in place" >&2
        return 1
    fi
    if ! alive "$pid"; then
        echo "server_ctl: pid $pid already exited"
        rm -f "$PIDFILE"
        return 0
    fi
    if ! owned "$pid"; then
        echo "server_ctl: pid $pid is not our server (cwd=$(readlink "/proc/$pid/cwd" 2>/dev/null)); NOT signalled; pid file left in place" >&2
        return 1
    fi
    kill -TERM "$pid" 2>/dev/null || true
    local i
    for i in $(seq 1 40); do
        alive "$pid" || break
        sleep 0.5
    done
    if alive "$pid" && owned "$pid"; then
        echo "server_ctl: pid $pid ignored SIGTERM for 20 s; SIGKILL (still ours)"
        kill -KILL "$pid" 2>/dev/null || true
        sleep 1
    fi
    if alive "$pid"; then
        echo "server_ctl: pid $pid still alive after SIGKILL" >&2
        return 1
    fi
    rm -f "$PIDFILE"
    echo "server_ctl: stopped pid $pid"
}

cmd_status() {
    local pid=""
    [[ -f "$PIDFILE" ]] && pid="$(cat "$PIDFILE")"
    if [[ "$pid" =~ ^[0-9]+$ ]] && alive "$pid" && owned "$pid"; then
        echo "server_ctl: $variant running (pid $pid, run dir $RUN_REAL)"
    else
        echo "server_ctl: $variant not running"
    fi
}

cmd_start() {
    [[ -f "$OUT/RotMG.dll" ]] || die "no $OUT/RotMG.dll; build first: (cd $REPO && dotnet build RotMG.csproj -c $CONFIG -o $OUT)"
    [[ -d "$OUT/Resources" ]] || die "no $OUT/Resources"
    [[ -f "$REPO/Settings.xml" ]] || die "no $REPO/Settings.xml to generate from"
    local p
    for p in "$HTTP_PORT" "$TCP_PORT"; do
        port_busy "$p" && die "port $p is already in use; not starting (nothing was signalled)"
    done
    if [[ -f "$PIDFILE" ]]; then
        local old
        old="$(cat "$PIDFILE")"
        if [[ "$old" =~ ^[0-9]+$ ]] && alive "$old" && owned "$old"; then
            die "variant $variant already has our server running (pid $old); stop it first"
        fi
        rm -f "$PIDFILE"
    fi
    case "$DBDIR$RUN$REPO" in *\"*|*\&*|*\<*|*\>*|*\#*) die "paths must not contain \" & < > #" ;; esac

    mkdir -p "$STATE_ROOT/pids" "$STATE_ROOT/logs" "$RUN" "$DB_ROOT" || die "cannot create state dirs under $STATE_ROOT"
    RUN_REAL="$(cd "$RUN" && pwd -P)"

    # Run dir: Resources is a symlink into the build output (read-only use); Settings.xml is generated.
    if [[ -e "$RUN/Resources" && ! -L "$RUN/Resources" ]]; then
        die "$RUN/Resources is a real directory; remove it by hand"
    fi
    ln -sfn "$OUT/Resources" "$RUN/Resources"

    # Throwaway DB dir: wiped at each start, but only if we created it (marker file).
    if [[ -e "$DBDIR" ]]; then
        [[ "$DBDIR" == "$DB_ROOT"/* && -f "$DBDIR/.realm-hop-owned" ]] || die "$DBDIR exists and is not ours; not wiping it"
        rm -rf -- "$DBDIR"
    fi
    mkdir -p "$DBDIR" && : > "$DBDIR/.realm-hop-owned" || die "cannot create $DBDIR"

    sed -e "s#<Ports>[^<]*</Ports>#<Ports>${HTTP_PORT}:${TCP_PORT}</Ports>#" \
        -e "s#db=\"[^\"]*\"#db=\"$DBDIR\"#" "$REPO/Settings.xml" > "$RUN/Settings.xml.tmp" \
        && mv -f "$RUN/Settings.xml.tmp" "$RUN/Settings.xml" \
        || die "cannot write generated $RUN/Settings.xml"

    local -a server envcmd
    if [[ "$LAUNCH" == apphost && -x "$OUT/RotMG" ]]; then
        server=("$OUT/RotMG")
    else
        server=(dotnet "$OUT/RotMG.dll")
    fi
    if [[ "$TRACE" == on ]]; then
        envcmd=(env REALM_HOPTRACE=1)
    else
        envcmd=(env -u REALM_HOPTRACE)
    fi

    : > "$LOG"
    # No setsid: it would fork when the shell is a process-group leader, and then $! would not be the server.
    (
        cd "$RUN" || exit 1
        exec nohup "${envcmd[@]}" "${server[@]}" >>"$LOG" 2>&1 </dev/null
    ) &
    local pid=$!
    echo "$pid" > "$PIDFILE"

    local waited=0
    until port_busy "$TCP_PORT" && port_busy "$HTTP_PORT"; do
        if ! alive "$pid"; then
            rm -f "$PIDFILE"
            die "server exited during startup; see $LOG"
        fi
        waited=$((waited + 1))
        if (( waited > 60 )); then
            echo "server_ctl: ports not open after 30 s; stopping our pid $pid" >&2
            cmd_stop >&2
            die "startup timed out; see $LOG"
        fi
        sleep 0.5
    done
    if ! owned "$pid"; then
        die "pid $pid is not running out of $RUN_REAL; not signalling it; see $LOG"
    fi

    echo "server_ctl: up pid=$pid variant=$variant config=$CONFIG trace=$TRACE http=$HTTP_PORT tcp=$TCP_PORT"
    echo "server_ctl: cwd=$RUN_REAL  log=$LOG"
    grep -E '<Settings|<Ports>' "$RUN/Settings.xml" | sed 's/^/server_ctl:   /'
    local fs
    fs="$(stat -f -c %T "$DBDIR" 2>/dev/null || echo unknown)"
    echo "server_ctl: db=$DBDIR fs=$fs"
    if [[ "$fs" == tmpfs ]]; then
        echo "server_ctl: WARNING: DB is on tmpfs; expect ~50 ms hops, not the ext4 synchronous=FULL ~1.1 s case" >&2
    fi
}

case "$cmd" in
    start)  cmd_start ;;
    stop)   cmd_stop ;;
    status) cmd_status ;;
    *)      usage ;;
esac
