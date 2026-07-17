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
            && echo "$ui" | rg -q '"AXLabel":"Enable page-turn testing mode"'; then
            return 0
        fi
        sleep 1
    done

    echo "The demo did not remain in the foreground after launch." >&2
    return 1
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

echo "Device: $DEVICE"
echo "Device record:"
xcrun simctl list devices | awk -v id="$DEVICE" 'index($0, id) && record == "" { record = $0 } END { print record }'
echo "App: $APP"
echo "Screenshots: $OUTPUT/light.png $OUTPUT/dark.png"
