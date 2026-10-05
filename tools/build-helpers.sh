#!/usr/bin/env bash
# Builds the helper binaries used by maccatalyst-smoke.sh from tools/src/.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"
swiftc -O -o clickevent src/clickevent.swift
swiftc -O -o ocr src/ocr.swift
echo "built tools/clickevent and tools/ocr"
