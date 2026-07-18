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
            && echo "$ui" | rg -q '"AXLabel":"Interactive page\. Drag from any free edge or corner to turn the page\."' \
            && echo "$ui" | rg -q '"AXLabel":"Select the stationary page edge"' \
            && echo "$ui" | rg -q '"AXLabel":"Enable page-turn testing mode"' \
            && ! echo "$ui" | rg -q '"AXLabel":"(↖|↗|↙|↘|‹|›|⌃  TOP EDGE  ⌃|⌄  BOTTOM EDGE  ⌄)"'; then
            return 0
        fi
        sleep 1
    done

    echo "The demo did not remain in the foreground after launch." >&2
    return 1
}

capture_testing_mode() {
    if ! command -v idb >/dev/null 2>&1 || ! command -v jq >/dev/null 2>&1; then
        echo "Skipping Testing mode capture because idb or jq is unavailable."
        return 0
    fi

    local ui
    ui="$(idb ui describe-all --udid "$DEVICE" --json 2>/dev/null || true)"
    local switch_frame
    switch_frame="$(echo "$ui" | jq -r '.. | objects | select(.AXLabel? == "Enable page-turn testing mode") | .AXFrame' | head -1)"
    if [[ -z "$switch_frame" || "$switch_frame" == "null" ]]; then
        echo "Testing mode frame was not exposed by idb." >&2
        return 1
    fi

    local switch_values
    switch_values="$(echo "$switch_frame" | tr -d '{}' | tr ',' ' ')"
    local switch_x switch_y switch_width switch_height tap_x tap_y
    read -r switch_x switch_y switch_width switch_height <<< "$switch_values"
    tap_x="$(awk -v x="$switch_x" -v width="$switch_width" 'BEGIN { printf "%d", x + width / 2 }')"
    tap_y="$(awk -v y="$switch_y" -v height="$switch_height" 'BEGIN { printf "%d", y + height / 2 }')"
    idb ui tap --udid "$DEVICE" --duration 1 "$tap_x" "$tap_y" >/dev/null
    local testing_ui=""
    local next_frame=""
    local testing_ready=0
    for _ in 1 2 3 4 5; do
        sleep 1
        testing_ui="$(idb ui describe-all --udid "$DEVICE" --json 2>/dev/null || true)"
        next_frame="$(echo "$testing_ui" | jq -r '.. | objects | select(.AXLabel? == "Turn to next page") | .AXFrame' | head -1)"
        if echo "$testing_ui" | rg -q '"AXLabel":"RUNTIME PROBE"' \
            && echo "$testing_ui" | rg -q '"AXLabel":"Turn to next page"' \
            && [[ "$next_frame" != "{{0, 0}, {0, 0}}" ]]; then
            testing_ready=1
            break
        fi
    done
    if [[ "$testing_ready" != "1" ]]; then
        echo "Testing mode did not expose a complete, visible diagnostics surface." >&2
        return 1
    fi

    xcrun simctl io "$DEVICE" screenshot "$OUTPUT/testing.png" >/dev/null

    # Testing mode changes the vertical layout, so resolve the switch frame
    # again before turning it back off.
    ui="$(idb ui describe-all --udid "$DEVICE" --json 2>/dev/null || true)"
    switch_frame="$(echo "$ui" | jq -r '.. | objects | select(.AXLabel? == "Enable page-turn testing mode") | .AXFrame' | head -1)"
    switch_values="$(echo "$switch_frame" | tr -d '{}' | tr ',' ' ')"
    read -r switch_x switch_y switch_width switch_height <<< "$switch_values"
    tap_x="$(awk -v x="$switch_x" -v width="$switch_width" 'BEGIN { printf "%d", x + width / 2 }')"
    tap_y="$(awk -v y="$switch_y" -v height="$switch_height" 'BEGIN { printf "%d", y + height / 2 }')"
    idb ui tap --udid "$DEVICE" --duration 1 "$tap_x" "$tap_y" >/dev/null
    local final_value
    final_value="$(idb ui describe-all --udid "$DEVICE" --json 2>/dev/null | jq -r '.. | objects | select(.AXLabel? == "Enable page-turn testing mode") | .AXValue' | head -1)"
    if [[ "$final_value" != "0" ]]; then
        echo "Testing mode did not return to its default off state." >&2
        return 1
    fi
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
capture_testing_mode

echo "Device: $DEVICE"
echo "Device record:"
xcrun simctl list devices | awk -v id="$DEVICE" 'index($0, id) && record == "" { record = $0 } END { print record }'
echo "App: $APP"
echo "Screenshots: $OUTPUT/light.png $OUTPUT/dark.png"
