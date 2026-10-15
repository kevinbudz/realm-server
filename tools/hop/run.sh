#!/usr/bin/env bash
# run.sh - end-to-end hop harness: build, start a throwaway server on alternate ports, run N hops,
# stop the server, analyze. Exit 0 only if every hop completed and every requested check passed.
#
# usage: tools/hop/run.sh [--config Debug|Release] [--runs N] [--variant NAME]
#                         [--trace on|off | --no-trace] [--skip-build]
#                         [--max-median-ms N] [--max-ms N]
#
# Defaults: Debug, 12 hops, tracing on (REALM_HOPTRACE=1), variant trace-on.
# Examples:
#   tools/hop/run.sh                                   build Debug, trace on, 12 hops, print median/max
#   tools/hop/run.sh --no-trace                        trace unset: expect zero [hop] lines, hops still complete
#   tools/hop/run.sh --skip-build --max-median-ms 150  fail if the median Escape -> first nexus Update > 150 ms
#
# Output: $STATE/logs/<variant>-{build,server,client,analyze}.txt|log and $STATE/results/<variant>.jsonl,
#         where STATE = ${REALM_HOP_STATE:-${TMPDIR:-/tmp}/realm-hop}.
# The build step writes to $STATE/build/<config> (dotnet build -o), never to the repo's bin/: bin/ holds the
# user's live server. --skip-build reuses that output. server_ctl.sh starts the server from the same directory.
set -uo pipefail

HOP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
REPO="$(cd "$HOP_DIR/../.." && pwd -P)"
STATE_ROOT="${REALM_HOP_STATE:-${TMPDIR:-/tmp}/realm-hop}"
HTTP_PORT="${REALM_HOP_HTTP_PORT:-7877}"
TCP_PORT="${REALM_HOP_TCP_PORT:-2060}"

die() { echo "run.sh: $*" >&2; exit 1; }
usage() { echo "usage: $0 [--config Debug|Release] [--runs N] [--variant NAME] [--trace on|off | --no-trace] [--skip-build] [--max-median-ms N] [--max-ms N]" >&2; exit 2; }

CONFIG="${REALM_HOP_CONFIG:-Debug}"
RUNS=12
TRACE=on
VARIANT=""
SKIP_BUILD=0
MAX_MEDIAN=""
MAX_ONE=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        --config)        CONFIG="${2:-}"; shift 2 ;;
        --runs)          RUNS="${2:-}"; shift 2 ;;
        --variant)       VARIANT="${2:-}"; shift 2 ;;
        --trace)         TRACE="${2:-}"; shift 2 ;;
        --no-trace)      TRACE=off; shift ;;
        --skip-build)    SKIP_BUILD=1; shift ;;
        --max-median-ms) MAX_MEDIAN="${2:-}"; shift 2 ;;
        --max-ms)        MAX_ONE="${2:-}"; shift 2 ;;
        -h|--help)       echo "usage: $0 [--config Debug|Release] [--runs N] [--variant NAME] [--trace on|off | --no-trace] [--skip-build] [--max-median-ms N] [--max-ms N]"; exit 0 ;;
        *) echo "run.sh: unknown argument: $1" >&2; usage ;;
    esac
done

case "$CONFIG" in Debug|Release) ;; *) die "--config must be Debug or Release" ;; esac
[[ "$RUNS" =~ ^[1-9][0-9]*$ ]] || die "--runs must be a positive integer"
case "$TRACE" in on|off) ;; *) die "--trace must be on or off" ;; esac
[[ -n "$VARIANT" ]] || VARIANT="trace-$TRACE"
[[ "$VARIANT" =~ ^[A-Za-z0-9._-]+$ ]] || die "--variant must match [A-Za-z0-9._-]+"
for gate in "$MAX_MEDIAN" "$MAX_ONE"; do
    [[ -z "$gate" || "$gate" =~ ^[0-9]+([.][0-9]+)?$ ]] || die "gate values must be non-negative numbers"
done

BUILD_OUT="$STATE_ROOT/build/$CONFIG"
LOGS="$STATE_ROOT/logs"
RESULTS_DIR="$STATE_ROOT/results"
mkdir -p "$LOGS" "$RESULTS_DIR" || die "cannot create $STATE_ROOT"
BUILD_LOG="$LOGS/$VARIANT-build.txt"
SERVER_LOG="$LOGS/$VARIANT-server.log"
CLIENT_LOG="$LOGS/$VARIANT-client.txt"
ANALYZE_LOG="$LOGS/$VARIANT-analyze.txt"
RESULTS="$RESULTS_DIR/$VARIANT.jsonl"

if (( SKIP_BUILD )); then
    echo "== build: skipped (using $BUILD_OUT as is)"
else
    echo "== build: dotnet build -c $CONFIG -o $BUILD_OUT (log: $BUILD_LOG)"
    if ! (cd "$REPO" && dotnet build RotMG.csproj -c "$CONFIG" -o "$BUILD_OUT" >"$BUILD_LOG" 2>&1); then
        grep -E 'error' "$BUILD_LOG" | head -20
        die "build failed; full log: $BUILD_LOG"
    fi
    grep -E 'Warning\(s\)|Error\(s\)' "$BUILD_LOG" | sed 's/^/  /'
fi

STARTED=0
cleanup() {
    if (( STARTED )); then
        "$HOP_DIR/server_ctl.sh" stop "$VARIANT" >&2 || echo "run.sh: WARNING: stop reported a problem for $VARIANT" >&2
        STARTED=0
    fi
}
trap cleanup EXIT
trap 'exit 130' INT TERM

echo "== start server: variant=$VARIANT config=$CONFIG trace=$TRACE http=$HTTP_PORT tcp=$TCP_PORT"
"$HOP_DIR/server_ctl.sh" start "$VARIANT" --config "$CONFIG" --trace "$TRACE" || die "server did not start; see $SERVER_LOG"
STARTED=1

echo "== hops: $RUNS (client log: $CLIENT_LOG)"
rm -f "$RESULTS"
python3 -I "$HOP_DIR/hop_client.py" --http "$HTTP_PORT" --tcp "$TCP_PORT" --runs "$RUNS" --out "$RESULTS" >"$CLIENT_LOG" 2>&1
CLIENT_RC=$?

echo "== stop server"
"$HOP_DIR/server_ctl.sh" stop "$VARIANT"
STOP_RC=$?
STARTED=0

OK_RUNS=$(grep -c -v '"error"' "$RESULTS" 2>/dev/null)
OK_RUNS=${OK_RUNS:-0}
HOP_LINES=$(grep -c '\[hop\]' "$SERVER_LOG" 2>/dev/null)
HOP_LINES=${HOP_LINES:-0}

ANALYZE_RC=1
if [[ -f "$RESULTS" && -f "$SERVER_LOG" ]]; then
    ANALYZE_ARGS=("$VARIANT=$SERVER_LOG:$RESULTS")
    [[ -n "$MAX_MEDIAN" ]] && ANALYZE_ARGS=(--max-median-ms "$MAX_MEDIAN" "${ANALYZE_ARGS[@]}")
    [[ -n "$MAX_ONE" ]] && ANALYZE_ARGS=(--max-ms "$MAX_ONE" "${ANALYZE_ARGS[@]}")
    echo "== analyze"
    python3 -I "$HOP_DIR/analyze.py" "${ANALYZE_ARGS[@]}" 2>&1 | tee "$ANALYZE_LOG"
    ANALYZE_RC=${PIPESTATUS[0]}
fi

FAIL=()
(( CLIENT_RC == 0 )) || FAIL+=("hop client exited $CLIENT_RC (see $CLIENT_LOG)")
(( OK_RUNS >= RUNS )) || FAIL+=("only $OK_RUNS of $RUNS hops completed (see $CLIENT_LOG)")
if [[ "$TRACE" == on && "$HOP_LINES" -eq 0 ]]; then
    FAIL+=("trace on but the server log has no [hop] lines ($SERVER_LOG)")
fi
if [[ "$TRACE" == off && "$HOP_LINES" -gt 0 ]]; then
    FAIL+=("trace off but the server log has $HOP_LINES [hop] lines ($SERVER_LOG)")
fi
(( STOP_RC == 0 )) || FAIL+=("server_ctl stop exited $STOP_RC")
(( ANALYZE_RC == 0 )) || FAIL+=("analyze.py exited $ANALYZE_RC (gate or missing data)")

if (( ${#FAIL[@]} )); then
    for msg in "${FAIL[@]}"; do echo "RESULT FAIL: $msg"; done
    exit 1
fi
echo "RESULT PASS variant=$VARIANT config=$CONFIG trace=$TRACE hops=$OK_RUNS/$RUNS [hop] lines=$HOP_LINES"
exit 0
