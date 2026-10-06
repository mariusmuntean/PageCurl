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
import re
import sys
import xml.etree.ElementTree as ET

root, scratch = map(Path, sys.argv[1:])
guide = (root / 'docs/site/getting-started.md').read_text()
xml = re.findall(r'```xml\n(.*?)\n```', guide, re.S)
code = [block for block in re.findall(r'```csharp\n(.*?)\n```', guide, re.S)
        if 'public partial class MainPage' in block]
if len(xml) != 1 or len(code) != 1:
    sys.exit('Expected one documented MainPage XAML and code-behind example')
(scratch / 'MainPage.xaml').write_text(xml[0] + '\n')
(scratch / 'MainPage.xaml.cs').write_text(code[0] + '\n')
project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
properties = ET.SubElement(project, 'PropertyGroup')
for name, value in {'TargetFramework': 'net10.0', 'UseMaui': 'true',
                    'SingleProject': 'true', 'IsPackable': 'false',
                    'TreatWarningsAsErrors': 'true'}.items():
    ET.SubElement(properties, name).text = value
items = ET.SubElement(project, 'ItemGroup')
ET.SubElement(items, 'ProjectReference', Include=str(root / 'src/PageCurl.Maui/PageCurl.Maui.csproj'))
ET.SubElement(items, 'PackageReference', Include='Microsoft.Maui.Controls', Version='$(MauiVersion)')
ET.ElementTree(project).write(scratch / 'QuickStart.csproj', encoding='utf-8', xml_declaration=True)
PY
"$DOTNET" build "$SCRATCH/QuickStart.csproj" -c Release -f net10.0 -p:TargetFrameworks=net10.0 -m:1 -nr:false
printf 'PASS documented quick start: unchanged XAML and code-behind compile\n'
