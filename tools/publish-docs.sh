#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
bash tools/build-docs.sh
SCRATCH="$(mktemp -d "${TMPDIR:-/tmp}/pagecurl-docs.XXXXXX")"
trap 'python3 -c '\''import shutil,sys; shutil.rmtree(sys.argv[1])'\'' "$SCRATCH"' EXIT
git clone --quiet https://github.com/mariusmuntean/PageCurl-docs.git "$SCRATCH"
python3 - "$ROOT/.artifacts/site" "$SCRATCH" <<'PY'
from pathlib import Path
import shutil
import sys
source, target = map(Path, sys.argv[1:])
site = target / 'site'
if site.exists():
    shutil.rmtree(site)
shutil.copytree(source, site)
(site / '.nojekyll').touch()
PY
git -C "$SCRATCH" add --all
if git -C "$SCRATCH" diff --cached --quiet; then
  printf 'Published documentation already matches this build.\n'
  exit 0
fi
git -C "$SCRATCH" commit -m 'Update PageCurl documentation'
git -C "$SCRATCH" push origin HEAD:main
