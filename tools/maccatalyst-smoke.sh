#!/usr/bin/env bash
# Fast Mac Catalyst demo driver: launch, click, drag, capture, OCR, and sample.
# Usage: maccatalyst-smoke.sh <command> [args...]
#   launch              kill + open the debug app, wait for the window
#   click X Y           left click at screen point
#   drag X1 Y1 X2 Y2 MS left drag between screen points over MS milliseconds
#   shot NAME           capture the app window to .artifacts/maccatalyst-smoke/
#   ocr X Y W H         OCR a point-rect of the last window (output text)
#   turns N             perform N forward+backward turns, sampling CPU/RSS
# Requires: tools/clickevent and tools/ocr binaries (see tools/build-helpers.sh).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/.artifacts/maccatalyst-smoke"
APP="$ROOT/src/PageCurl.Demo/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/PageCurl.Demo.app"
CLICK="$ROOT/tools/clickevent"
OCR="$ROOT/tools/ocr"
mkdir -p "$OUT"

window_bounds() {
    local pid
    pid="$(pgrep -x PageCurl.Demo | head -1 || true)"
    [[ -n "$pid" ]] || { echo ""; return; }
    osascript -e "tell application \"System Events\" to tell (first process whose unix id is $pid) to get {position, size} of front window" 2>/dev/null || true
}

case "${1:-}" in
    launch)
        pkill -x PageCurl.Demo >/dev/null 2>&1 || true
        sleep 0.5
        open "$APP"
        for _ in $(seq 1 40); do
            sleep 0.5
            pgrep -x PageCurl.Demo >/dev/null && break
        done
        # wait for a window with content
        sleep 4
        window_bounds
        ;;
    bounds)
        window_bounds
        ;;
    click)
        "$CLICK" click "$2" "$3"
        ;;
    drag)
        "$CLICK" drag "$2" "$3" "$4" "$5" "$6"
        ;;
    shot)
        B="$(window_bounds)"
        [[ -n "$B" ]] || { echo "no window" >&2; exit 1; }
        X=$(echo "$B" | cut -d, -f1 | tr -d ' ')
        Y=$(echo "$B" | cut -d, -f2 | tr -d ' ')
        W=$(echo "$B" | cut -d, -f3 | tr -d ' ')
        H=$(echo "$B" | cut -d, -f4 | tr -d ' ')
        screencapture -x -R"$X,$Y,$W,$H" "$OUT/$2.png"
        echo "$OUT/$2.png"
        ;;
    ocr)
        # args: X Y W H in window points; uses the last shot if present
        SHOT="${SHOT:-$OUT/$2.png}"
        if [[ $# -ge 5 ]]; then
            SHOT="$OUT/ocr-temp.png"
            B="$(window_bounds)"
            X=$(echo "$B" | cut -d, -f1 | tr -d ' ')
            Y=$(echo "$B" | cut -d, -f2 | tr -d ' ')
            screencapture -x -R"$((X+$2)),$((Y+$3)),$4,$5" "$SHOT"
        fi
        "$OCR" "$SHOT"
        ;;
    turns)
        N="$2"
        NEXT_X="${3:-1400}"; NEXT_Y="${4:-972}"
        PREV_X="${5:-1330}"; PREV_Y="${6:-972}"
        echo "turn cpu% rss_mb"
        for i in $(seq 1 "$N"); do
            "$CLICK" click "$NEXT_X" "$NEXT_Y"
            sleep 2.2
            "$CLICK" click "$PREV_X" "$PREV_Y"
            sleep 2.2
            ps -Ao %cpu,rss,comm | rg "PageCurl.Demo$" | awk -v i="$i" '{printf "%d %s %.1f\n", i, $1, $2/1024}'
        done
        ;;
    *)
        echo "unknown command: ${1:-}" >&2
        exit 2
        ;;
esac
