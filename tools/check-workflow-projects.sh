#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

workflow_paths=()
if [[ $# -gt 0 ]]; then
  workflow_paths=("$1")
else
  while IFS= read -r workflow_path; do
    workflow_paths+=("$workflow_path")
  done < <(find .github/workflows -maxdepth 1 -type f \( -name '*.yml' -o -name '*.yaml' \) -print | sort)
fi

if [[ ${#workflow_paths[@]} -eq 0 ]]; then
  printf 'No workflow YAML files found.\n' >&2
  exit 2
fi

missing=0
for workflow_path in "${workflow_paths[@]}"; do
  if [[ ! -f "$workflow_path" ]]; then
    printf 'Workflow not found: %s\n' "$workflow_path" >&2
    missing=1
    continue
  fi

  while IFS= read -r project_path; do
    if [[ -f "$project_path" ]]; then
      printf 'OK %s (%s)\n' "$project_path" "$workflow_path"
    else
      printf 'MISSING %s (%s)\n' "$project_path" "$workflow_path"
      missing=1
    fi
  done < <(grep -Eo '(src|tests)/[^[:space:]]+\.csproj' "$workflow_path" | sort -u)
done

exit "$missing"
