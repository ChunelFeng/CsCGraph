#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

CONFIGURATION="${CONFIGURATION:-Release}"
TUTORIAL_TIMEOUT_SECONDS="${TUTORIAL_TIMEOUT_SECONDS:-60}"

PROJECTS=()
while IFS= read -r project; do
    PROJECTS+=("$project")
done < <(find tutorial -mindepth 2 -maxdepth 2 -path 'tutorial/T[0-9][0-9]-*/*.csproj' | sort)

if [ "${#PROJECTS[@]}" -eq 0 ]; then
    echo "No executable tutorial projects found."
    exit 1
fi

echo "Run configuration: ${CONFIGURATION}"
echo "Executable tutorial count: ${#PROJECTS[@]}"

run_with_timeout() {
    local project="$1"
    local dll="$2"
    local timeout_seconds="$3"
    local elapsed=0
    local pid

    dotnet "$dll" &
    pid=$!

    while kill -0 "$pid" 2>/dev/null; do
        if [ "$elapsed" -ge "$timeout_seconds" ]; then
            echo "Tutorial timeout: ${project}"
            kill "$pid" 2>/dev/null || true
            wait "$pid" 2>/dev/null || true
            return 124
        fi

        sleep 1
        elapsed=$((elapsed + 1))
    done

    wait "$pid"
}

for project in "${PROJECTS[@]}"; do
    project_name="$(basename "$project" .csproj)"
    dll="tutorial/${project_name}/bin/${CONFIGURATION}/net10.0/${project_name}.dll"

    if [ ! -f "$dll" ]; then
        echo "Tutorial output not found, build first: ${project}"
        exit 1
    fi

    echo
    echo "==> Running ${project}"
    run_with_timeout "$project" "$dll" "$TUTORIAL_TIMEOUT_SECONDS"
done

echo
echo "All tutorials ran successfully."
