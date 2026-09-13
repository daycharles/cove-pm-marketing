#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PARENT_DIR="$(dirname "$SCRIPT_DIR")"
STATE_DIR="${PROPFLOW_STATE:-}"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker Desktop is required. Install it from https://www.docker.com/products/docker-desktop/ and run it, then launch this installer again."
  exit 1
fi
if ! docker info >/dev/null 2>&1; then
  echo "Docker Desktop is installed but is not running. Start Docker Desktop, wait until it is ready, then launch this installer again."
  exit 1
fi
if ! command -v node >/dev/null 2>&1; then
  echo "Node.js 22 or newer is required. Install it from https://nodejs.org/, then launch this installer again."
  exit 1
fi

# Prefer an explicitly configured state directory, then an existing state directory in the
# standard macOS application-data location, then a sibling release bundle during an upgrade.
if [[ -z "$STATE_DIR" && -d "$HOME/Library/Application Support/PropFlow" ]]; then
  STATE_DIR="$HOME/Library/Application Support/PropFlow"
fi
if [[ -z "$STATE_DIR" ]]; then
  for candidate in "$PARENT_DIR"/*/state; do
    if [[ "$candidate" != "$SCRIPT_DIR/state" && -f "$candidate/SIGN-IN.txt" ]]; then
      STATE_DIR="$candidate"
      break
    fi
  done
fi

if [[ -n "$STATE_DIR" && -x "$STATE_DIR/../propflow-deploy" ]]; then
  echo "Stopping the previous Cove PM installation while preserving its data..."
  "$STATE_DIR/../propflow-deploy" --state "$STATE_DIR" down || true
fi

cd "$SCRIPT_DIR"
xattr -dr com.apple.quarantine . 2>/dev/null || true
chmod +x propflow-deploy api/PropFlow.Api admin/PropFlow.Admin

if [[ -n "$STATE_DIR" ]]; then
  echo "Starting Cove PM with existing data in: $STATE_DIR"
  exec ./propflow-deploy --state "$STATE_DIR" up
else
  echo "Starting a new Cove PM installation..."
  exec ./propflow-deploy up
fi
