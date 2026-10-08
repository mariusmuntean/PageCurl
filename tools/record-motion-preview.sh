#!/usr/bin/env bash
# macOS 15+: record the linked native demo through the assertion host.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="${PAGECURL_DOTNET:-dotnet}"
SCRATCH="$(mktemp -d "${TMPDIR:-/tmp}/pagecurl-preview.XXXXXX")"
APP_PID=""
cleanup() {
    if [[ -n "$APP_PID" ]] && kill -0 "$APP_PID" 2>/dev/null; then
        kill "$APP_PID" 2>/dev/null || true
        wait "$APP_PID" 2>/dev/null || true
    fi
    rm -rf "$SCRATCH"
}
trap cleanup EXIT
OUT="$ROOT/.artifacts/motion-preview"
mkdir -p "$OUT"
swiftc -parse-as-library "$ROOT/tools/record-preview-window.swift" -o "$SCRATCH/recorder"
"$DOTNET" build "$ROOT/tests/PageCurl.Apple.Tests" -c Release -f net10.0-maccatalyst -p:RuntimeIdentifier=maccatalyst-arm64 -m:1 -nr:false
PAGECURL_PREVIEW_DIR="$SCRATCH" "$ROOT/tests/PageCurl.Apple.Tests/bin/Release/net10.0-maccatalyst/maccatalyst-arm64/PageCurl.Apple.Tests.app/Contents/MacOS/PageCurl.Apple.Tests" >"$OUT/app.log" 2>&1 &
APP_PID=$!
for _ in {1..60}; do
    [[ -f "$SCRATCH/ready" ]] && break
    if [[ -f "$SCRATCH/error" ]]; then cat "$SCRATCH/error" >&2; exit 1; fi
    kill -0 "$APP_PID" 2>/dev/null || { cat "$OUT/app.log" >&2; exit 1; }
    sleep 0.5
done
[[ -f "$SCRATCH/ready" ]] || { echo "Preview app did not become ready" >&2; exit 1; }
"$SCRATCH/recorder" "$APP_PID" "$SCRATCH" "$SCRATCH/native.mp4"
wait "$APP_PID"
APP_PID=""
python3 "$ROOT/tools/check-motion-preview.py" "$SCRATCH/native.mp4"
mv "$SCRATCH/native.mp4" "$OUT/native.mp4"
mkdir -p "$ROOT/docs/site/images"
# Keep real-time timestamps; encode a web-sized, silent clip and its poster.
ffmpeg -hide_banner -loglevel error -y -i "$OUT/native.mp4" -an -vf 'scale=1280:-2:flags=lanczos,fps=30' -c:v libx264 -crf 18 -preset slow -pix_fmt yuv420p -movflags +faststart "$SCRATCH/pagecurl-preview.mp4"
ffmpeg -hide_banner -loglevel error -y -ss 2.4 -i "$SCRATCH/pagecurl-preview.mp4" -frames:v 1 -pix_fmt yuvj420p -q:v 2 "$SCRATCH/pagecurl-preview.jpg"
python3 "$ROOT/tools/check-motion-preview.py" "$SCRATCH/pagecurl-preview.mp4"
[[ -s "$SCRATCH/pagecurl-preview.jpg" ]] || { echo "Preview poster was not produced" >&2; exit 1; }
mv "$SCRATCH/pagecurl-preview.mp4" "$SCRATCH/pagecurl-preview.jpg" "$ROOT/docs/site/images/"
printf 'PASS native motion preview: docs/site/images/pagecurl-preview.mp4\n'
