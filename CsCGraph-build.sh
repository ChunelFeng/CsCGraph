#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

CONFIGURATION="${CONFIGURATION:-Release}"

PROJECTS=()
while IFS= read -r project; do
    PROJECTS+=("$project")
done < <(find tutorial -mindepth 2 -maxdepth 2 -name '*.csproj' | sort)

if [ "${#PROJECTS[@]}" -eq 0 ]; then
    echo "No tutorial projects found."
    exit 1
fi

echo "Build configuration: ${CONFIGURATION}"

for project in "${PROJECTS[@]}"; do
    echo
    echo "==> Building ${project}"
    dotnet build "$project" \
        -c "$CONFIGURATION" \
        --nologo \
        --verbosity quiet \
        --disable-build-servers \
        -m:1 \
        /p:UseSharedCompilation=false
done

echo
echo "All tutorial projects built successfully."
