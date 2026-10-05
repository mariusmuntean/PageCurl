#!/usr/bin/env bash
# Verify a fresh PackageReference consumer with an isolated package cache.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="${PAGECURL_DOTNET:-dotnet}"
SCRATCH="$(mktemp -d "${TMPDIR:-/tmp}/pagecurl-consumer.XXXXXX")"
trap 'rm -rf "$SCRATCH"' EXIT
mkdir -p "$SCRATCH/feed" "$SCRATCH/consumer"

"$DOTNET" pack "$ROOT/src/PageCurl.Core/PageCurl.Core.csproj" -c Release --output "$SCRATCH/feed"
"$DOTNET" pack "$ROOT/src/PageCurl.Maui/PageCurl.Maui.csproj" -c Release -p:TargetFrameworks=net10.0 --output "$SCRATCH/feed"
python3 "$ROOT/tools/check-packages.py" "$SCRATCH/feed/"PageCurl.Core.*.nupkg net10.0
python3 "$ROOT/tools/check-packages.py" "$SCRATCH/feed/"PageCurl.Maui.*.nupkg net10.0
cp "$ROOT/tests/PageCurl.Package.Tests/Consumer.csproj" "$ROOT/tests/PageCurl.Package.Tests/Program.cs" "$SCRATCH/consumer/"
cp "$ROOT/global.json" "$SCRATCH/global.json"
cat > "$SCRATCH/NuGet.Config" <<EOF
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$SCRATCH/feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local"><package pattern="PageCurl.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
EOF
"$DOTNET" restore "$SCRATCH/consumer/Consumer.csproj" --configfile "$SCRATCH/NuGet.Config" --packages "$SCRATCH/packages" --no-cache
"$DOTNET" run --project "$SCRATCH/consumer/Consumer.csproj" -c Release --no-restore
