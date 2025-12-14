#!/bin/bash
# Build script with automatic logging
# Usage: ./scripts/build.sh [additional dotnet build args]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT" && dotnet build "$@" 2>&1 | tee BuildSolution.log
