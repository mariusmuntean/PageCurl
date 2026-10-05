#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
DOTNET="${PAGECURL_DOTNET:-dotnet}"
export MSBuildEnableWorkloadResolver=false
"$DOTNET" tool restore
"$DOTNET" build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0 -p:CopyLocalLockFileAssemblies=true -m:1 -nr:false
# Regenerate both directories so removed guides and API types cannot survive.
python3 - <<'PYTHON'
from pathlib import Path
import shutil
for folder in (Path('docs/site/api'), Path('.artifacts/site'), Path('.artifacts/docs-input')):
    if folder.exists():
        shutil.rmtree(folder)
# DocFX reads Source Link data from adjacent PDBs, including cross-assembly
# references. Publish signatures and XML docs without private source URLs.
inputs = Path('.artifacts/docs-input')
inputs.mkdir(parents=True)
for source in Path('src/PageCurl.Maui/bin/Release/net10.0').iterdir():
    if source.suffix in {'.dll', '.xml'}:
        shutil.copy2(source, inputs)
PYTHON
"$DOTNET" tool run docfx docs/site/docfx.json --warningsAsErrors
# DocFX's build manifest records an absolute workspace path; browsers do not need it.
python3 -c 'from pathlib import Path; Path(".artifacts/site/manifest.json").unlink(missing_ok=True)'
python3 tools/check-docs.py .artifacts/site
