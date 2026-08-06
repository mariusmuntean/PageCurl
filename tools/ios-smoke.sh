#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_ROOT="${DOTNET_ROOT:-/Users/marius/.dotnet}"
export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$PATH"

DEVICE="${1:-}"
if [[ -z "$DEVICE" ]]; then
    DEVICE="$(xcrun simctl list devices available | awk -F '[()]' '$1 ~ /iPhone 17 Pro $/ { print $2; exit }')"
fi
if [[ -z "$DEVICE" ]]; then
    DEVICE="$(xcrun simctl list devices available | awk -F '[()]' '$1 ~ /iPhone / { print $2; exit }')"
fi
if [[ -z "$DEVICE" ]]; then
    echo "No available iPhone simulator was found." >&2
    exit 1
fi

OUTPUT="${IOS_SMOKE_OUTPUT:-$ROOT/.artifacts/ios-smoke/$(date -u +%Y%m%dT%H%M%SZ)}"
APP="$ROOT/src/PageCurl.Demo/bin/Release/net10.0-ios/iossimulator-arm64/PageCurl.Demo.app"
BUNDLE_ID="com.example.pagecurldemo"
mkdir -p "$OUTPUT"

assert_demo_foreground() {
    if ! command -v idb >/dev/null 2>&1; then
        return 0
    fi

    for _ in 1 2 3 4 5; do
        local ui
        ui="$(idb ui describe-all --udid "$DEVICE" --json 2>/dev/null || true)"
        if echo "$ui" | rg -q '"AXLabel":"Page Curl Demo"' \
            && echo "$ui" | rg -q '"AXLabel":"Single"' \
            && echo "$ui" | rg -q '"AXLabel":"Spread"' \
            && echo "$ui" | rg -q '"AXLabel":"Content"' \
            && echo "$ui" | rg -q '"AXLabel":"Backside"' \
            && echo "$ui" | rg -q '"AXLabel":"Corner"' \
            && echo "$ui" | rg -q '"AXLabel":"Options"' \
            && ! echo "$ui" | rg -q '"AXLabel":"(↖|↗|↙|↘|‹|›|⌃  TOP EDGE  ⌃|⌄  BOTTOM EDGE  ⌄)"'; then
            return 0
        fi
        sleep 1
    done

    echo "The demo did not remain in the foreground after launch." >&2
    return 1
}

tap_label() {
    local label="$1"
    local ui frame values x y width height tap_x tap_y
    ui="$(idb ui describe-all --udid "$DEVICE" --json 2>/dev/null || true)"
    frame="$(echo "$ui" | jq -r --arg label "$label" '.. | objects | select(.AXLabel? == $label) | .AXFrame' | head -1)"
    if [[ -z "$frame" || "$frame" == "null" || "$frame" == "{{0, 0}, {0, 0}}" ]]; then
        echo "The '$label' control was not visible." >&2
        return 1
    fi

    values="$(echo "$frame" | tr -d '{}' | tr ',' ' ')"
    read -r x y width height <<< "$values"
    tap_x="$(awk -v x="$x" -v width="$width" 'BEGIN { printf "%d", x + width / 2 }')"
    tap_y="$(awk -v y="$y" -v height="$height" 'BEGIN { printf "%d", y + height / 2 }')"
    idb ui tap --udid "$DEVICE" --duration 0.2 "$tap_x" "$tap_y" >/dev/null
}

wait_for_label() {
    local label="$1"
    for _ in 1 2 3 4 5; do
        if idb ui describe-all --udid "$DEVICE" --json 2>/dev/null \
            | jq -e --arg label "$label" '.. | objects | select(.AXLabel? == $label)' >/dev/null; then
            return 0
        fi
        sleep 1
    done

    echo "The '$label' surface did not appear." >&2
    return 1
}

capture_feature_scenes() {
    if ! command -v idb >/dev/null 2>&1 || ! command -v jq >/dev/null 2>&1; then
        echo "Skipping feature-scene capture because idb or jq is unavailable."
        return 0
    fi

    tap_label "Options"
    wait_for_label "Select the stationary edge or pinned corner"
    wait_for_label "Page change threshold"
    wait_for_label "Enable flick gestures"
    wait_for_label "Enable page-turn haptic feedback"
    xcrun simctl io "$DEVICE" screenshot "$OUTPUT/options.png" >/dev/null
    tap_label "Close"

    tap_label "Spread"
    wait_for_label "Open spread"
    sleep 1
    xcrun simctl io "$DEVICE" screenshot "$OUTPUT/spread.png" >/dev/null

    tap_label "Content"
    wait_for_label "Live content"
    tap_label "Options"
    wait_for_label "View"
    tap_label "View"
    tap_label "Close"
    wait_for_label "Increment the live content counter"
    tap_label "Increment the live content counter"
    wait_for_label "Live content updated"
    sleep 1
    xcrun simctl io "$DEVICE" screenshot "$OUTPUT/content-view.png" >/dev/null

    tap_label "Options"
    wait_for_label "Video"
    tap_label "Video"
    tap_label "Close"
    wait_for_label "Looping local video page with snapshot poster fallback"
    sleep 2
    xcrun simctl io "$DEVICE" screenshot "$OUTPUT/content-video.png" >/dev/null

    tap_label "Backside"
    wait_for_label "The other side"
    tap_label "Options"
    wait_for_label "View"
    tap_label "View"
    tap_label "Close"
    sleep 1
    xcrun simctl io "$DEVICE" screenshot "$OUTPUT/backside.png" >/dev/null

    tap_label "Corner"
    wait_for_label "Pinned corner"
    tap_label "Options"
    wait_for_label "Top left"
    xcrun simctl io "$DEVICE" screenshot "$OUTPUT/corner.png" >/dev/null
    tap_label "Close"

    tap_label "Single"
    wait_for_label "One page"
}

echo "Building the iOS simulator demo..."
rm -rf "$ROOT/src/PageCurl.Core/bin" "$ROOT/src/PageCurl.Core/obj" \
    "$ROOT/src/PageCurl.Maui/bin" "$ROOT/src/PageCurl.Maui/obj" \
    "$ROOT/src/PageCurl.Demo/bin" "$ROOT/src/PageCurl.Demo/obj"
dotnet restore "$ROOT/src/PageCurl.Demo/PageCurl.Demo.csproj" -p:TargetFrameworks=net10.0-ios
dotnet restore "$ROOT/src/PageCurl.Maui/PageCurl.Maui.csproj" -p:TargetFrameworks=net10.0-ios
dotnet restore "$ROOT/src/PageCurl.Core/PageCurl.Core.csproj" -p:TargetFrameworks=net10.0
dotnet build "$ROOT/src/PageCurl.Demo/PageCurl.Demo.csproj" -c Release -f net10.0-ios --no-restore

if [[ ! -d "$APP" ]]; then
    echo "Expected app bundle was not produced: $APP" >&2
    exit 1
fi

echo "Booting simulator $DEVICE..."
xcrun simctl boot "$DEVICE" >/dev/null 2>&1 || true
perl -e 'alarm 120; exec @ARGV or die $!' xcrun simctl bootstatus "$DEVICE" -b
xcrun simctl install "$DEVICE" "$APP"
xcrun simctl ui "$DEVICE" appearance light
sleep 2
xcrun simctl terminate "$DEVICE" "$BUNDLE_ID" >/dev/null 2>&1 || true
xcrun simctl launch "$DEVICE" "$BUNDLE_ID"
assert_demo_foreground
# Let the first Skia surface finish its simulator compositor warm-up before capture.
sleep 10
xcrun simctl io "$DEVICE" screenshot "$OUTPUT/light.png"
xcrun simctl ui "$DEVICE" appearance dark
sleep 2
xcrun simctl terminate "$DEVICE" "$BUNDLE_ID" >/dev/null 2>&1 || true
xcrun simctl launch "$DEVICE" "$BUNDLE_ID"
assert_demo_foreground
sleep 10
xcrun simctl io "$DEVICE" screenshot "$OUTPUT/dark.png"
capture_feature_scenes

echo "Device: $DEVICE"
echo "Device record:"
xcrun simctl list devices | awk -v id="$DEVICE" 'index($0, id) && record == "" { record = $0 } END { print record }'
echo "App: $APP"
echo "Screenshots: $OUTPUT/light.png $OUTPUT/dark.png $OUTPUT/options.png $OUTPUT/spread.png $OUTPUT/content-view.png $OUTPUT/content-video.png $OUTPUT/backside.png $OUTPUT/corner.png"
