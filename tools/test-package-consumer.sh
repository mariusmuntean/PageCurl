#!/usr/bin/env bash
# Verify a fresh PackageReference consumer with an isolated package cache.
# Pass a prebuilt distribution directory to test the exact assembled packages.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="${PAGECURL_DOTNET:-dotnet}"
if [[ $# -gt 1 ]]; then
  printf 'Usage: %s [package-feed-directory]\n' "$0" >&2
  exit 2
fi
SCRATCH="$(mktemp -d "${TMPDIR:-/tmp}/pagecurl-consumer.XXXXXX")"
trap 'rm -rf "$SCRATCH"' EXIT
mkdir -p "$SCRATCH/feed" "$SCRATCH/consumer"
shopt -s nullglob
if [[ $# -eq 1 ]]; then
  if [[ ! -d "$1" ]]; then
    printf 'Package feed directory not found: %s\n' "$1" >&2
    exit 1
  fi
  packages=("$1"/PageCurl.*.nupkg)
  if [[ ${#packages[@]} -gt 0 ]]; then cp "${packages[@]}" "$SCRATCH/feed/"; fi
else
  "$DOTNET" pack "$ROOT/src/PageCurl.Core/PageCurl.Core.csproj" -c Release --output "$SCRATCH/feed"
  "$DOTNET" pack "$ROOT/src/PageCurl.Maui/PageCurl.Maui.csproj" -c Release -p:TargetFrameworks=net10.0 --output "$SCRATCH/feed"
fi
core_packages=("$SCRATCH/feed"/PageCurl.Core.*.nupkg)
maui_packages=("$SCRATCH/feed"/PageCurl.Maui.*.nupkg)
if [[ ${#core_packages[@]} -ne 1 || ${#maui_packages[@]} -ne 1 ]]; then
  printf 'Expected one Core and one MAUI package in the feed.\n' >&2
  exit 1
fi
cp "$ROOT/tests/PageCurl.Package.Tests/Consumer.csproj" "$ROOT/tests/PageCurl.Package.Tests/Program.cs" "$SCRATCH/consumer/"
cp "$ROOT/global.json" "$SCRATCH/global.json"
python3 - "$ROOT" "$SCRATCH" "${core_packages[0]}" "${maui_packages[0]}" <<'PY'
import importlib.util
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
import zipfile

root, scratch = map(Path, sys.argv[1:3])
spec = importlib.util.spec_from_file_location('check_packages', root / 'tools/check-packages.py')
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)
packages = []
for path in sys.argv[3:]:
    try:
        package = checker.Package(path)
    except (OSError, ValueError, ET.ParseError, zipfile.BadZipFile, KeyError) as error:
        sys.exit(f'Invalid package {Path(path).name}: {error}')
    errors = checker.check_package(package, ['net10.0'])
    if errors:
        sys.exit('\n'.join(errors))
    print(f'PASS package {package.path.name}: net10.0')
    packages.append(package)
if packages[0].identity[1:] != packages[1].identity[1:]:
    sys.exit('Core and MAUI release identities differ')
consumer = ET.parse(scratch / 'consumer/Consumer.csproj')
consumer.find('.//PackageReference[@Include="PageCurl.Maui"]').set('Version', packages[1].version)
consumer.write(scratch / 'consumer/Consumer.csproj', encoding='utf-8', xml_declaration=True)
config = ET.Element('configuration')
sources = ET.SubElement(config, 'packageSources')
ET.SubElement(sources, 'clear')
ET.SubElement(sources, 'add', key='local', value=str(scratch / 'feed'))
ET.SubElement(sources, 'add', key='nuget.org', value='https://api.nuget.org/v3/index.json')
mapping = ET.SubElement(config, 'packageSourceMapping')
ET.SubElement(ET.SubElement(mapping, 'packageSource', key='local'), 'package', pattern='PageCurl.*')
ET.SubElement(ET.SubElement(mapping, 'packageSource', key='nuget.org'), 'package', pattern='*')
ET.ElementTree(config).write(scratch / 'NuGet.Config', encoding='utf-8', xml_declaration=True)
PY
"$DOTNET" restore "$SCRATCH/consumer/Consumer.csproj" --configfile "$SCRATCH/NuGet.Config" --packages "$SCRATCH/packages" --no-cache
"$DOTNET" run --project "$SCRATCH/consumer/Consumer.csproj" -c Release --no-restore
