#!/usr/bin/env bash
set -euo pipefail
player=$(realpath "${1:?Pass the development Linux executable}")
if [[ ${2:-} != --offline-only ]]; then
    server=$(realpath "${2:?Pass the dedicated server executable}")
    shift
fi
mode=${2:-}
case "$mode" in
    "") ;;
    --offline-only) ;;
    --windows-peer) windows_peer=$(realpath "${3:?Pass the Windows development executable}") ;;
    *) echo "Unknown option: $mode" >&2; exit 2 ;;
esac
work=$(mktemp -d)
pids=()
cleanup() {
    for pid in "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done
    rm -rf "$work"
}
trap cleanup EXIT

start_player() {
    local role=$1 seconds=$2 player_work=$work logfile="$work/$1/player.log"
    local -a command=("$player")
    if [[ "$mode" == "--windows-peer" && "$role" == "second" ]]; then
        command=(wine "$windows_peer")
        player_work=$(winepath -w "$work")
        logfile=$(winepath -w "$logfile")
    fi
    mkdir -p "$work/$role"/{config,cache,data}
    XDG_CONFIG_HOME="$work/$role/config" XDG_CACHE_HOME="$work/$role/cache" \
    XDG_DATA_HOME="$work/$role/data" DIVINEWAR_UGS_PROFILE="$role" \
    DIVINEWAR_SMOKE_DIR="$player_work" DIVINEWAR_SMOKE_ROLE="$role" \
        timeout --kill-after=5s "${seconds}s" "${command[@]}" -batchmode -nographics -screen-width 640 \
            -screen-height 480 -logFile "$logfile" &
    pids+=("$!")
}

check_player() {
    local pid=$1 role=$2
    if ! wait "$pid" || [[ ! -f "$work/$role.passed" ]]; then
        echo "Smoke failed for $role" >&2
        for logfile in "$work"/*/player.log; do
            if [[ -f "$logfile" ]]; then
                # Report our failure without publishing raw SDK logs or session identifiers.
                grep -F 'Relay smoke failed (' "$logfile" | tail -1 || true
            fi
        done
        exit 1
    fi
}

start_player offline 75
check_player "${pids[0]}" offline
echo 'Solo simulation ran without initializing Unity Services and returned to Title.'
if [[ "$mode" == "--offline-only" ]]; then exit 0; fi

pids=()
mkdir -p "$work/server"/{config,cache,data}
XDG_CONFIG_HOME="$work/server/config" XDG_CACHE_HOME="$work/server/cache" \
XDG_DATA_HOME="$work/server/data" DIVINEWAR_UGS_PROFILE=server \
    timeout --kill-after=5s 250s "$server" -batchmode -nographics -logFile "$work/server.log" &
pids+=("$!")
start_player first 240
start_player second 240
check_player "${pids[1]}" first
check_player "${pids[2]}" second
echo 'Two clients exchanged commands and state with the dedicated server across two games.'
