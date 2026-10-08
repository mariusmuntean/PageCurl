#!/usr/bin/env bash
# Compile the documented XAML and code-behind without rewriting the examples.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="${PAGECURL_DOTNET:-dotnet}"
SCRATCH="$(mktemp -d "${TMPDIR:-/tmp}/pagecurl-quick-start.XXXXXX")"
trap 'rm -rf "$SCRATCH"' EXIT
SCRATCH="$(cd "$SCRATCH" && pwd -P)"
cp "$ROOT/global.json" "$ROOT/Directory.Build.props" "$SCRATCH/"
python3 - "$ROOT" "$SCRATCH" <<'PY'
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

root, scratch = map(Path, sys.argv[1:])
for directory, guide in (('quick-start', 'getting-started.md'), ('reader', 'spreads.md')):
    output = scratch / directory
    subprocess.run([sys.executable, str(root / 'tools/extract-doc-example.py'),
                    str(root / 'docs/site' / guide), str(output)], check=True)
project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
properties = ET.SubElement(project, 'PropertyGroup')
for name, value in {'TargetFramework': 'net10.0', 'UseMaui': 'true',
                    'SingleProject': 'true', 'IsPackable': 'false',
                    'TreatWarningsAsErrors': 'true'}.items():
    ET.SubElement(properties, name).text = value
items = ET.SubElement(project, 'ItemGroup')
ET.SubElement(items, 'ProjectReference', Include=str(root / 'src/PageCurl.Maui/PageCurl.Maui.csproj'))
ET.SubElement(items, 'PackageReference', Include='Microsoft.Maui.Controls', Version='$(MauiVersion)')
for directory in ('quick-start', 'reader'):
    ET.ElementTree(project).write(scratch / directory / 'Example.csproj', encoding='utf-8', xml_declaration=True)
PY
for example in quick-start reader; do
    "$DOTNET" build "$SCRATCH/$example/Example.csproj" -c Release -f net10.0 -p:TargetFrameworks=net10.0 -m:1 -nr:false
done
printf 'PASS documented quick start and reader: unchanged XAML and code-behind compile\n'
