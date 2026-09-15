#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PARENT_DIR="$(dirname "$SCRIPT_DIR")"
STATE_DIR="${PROPFLOW_STATE:-}"

say() { echo "[PropFlow] $*"; }
ask() {
  [[ "${PROPFLOW_NONINTERACTIVE:-}" == "1" ]] && return 0
  read -r -p "$1 [Y/n] " answer
  [[ -z "$answer" || "$answer" =~ ^[Yy]([Ee][Ss])?$ ]]
}

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
NODE_VERSION="$(node --version)"
if [[ ! "$NODE_VERSION" =~ ^v(2[2-9]|[3-9][0-9])\. ]]; then
  say "Node.js 22 or newer is required; found $NODE_VERSION."
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

STATE_EXISTS=0
[[ -f "$STATE_DIR/deployment.json" ]] && STATE_EXISTS=1
if [[ "$STATE_EXISTS" == "1" ]]; then MODE="upgrade"; else MODE="new install"; fi
say "Detected $MODE."
say "Bundle: $SCRIPT_DIR"
say "State:  ${STATE_DIR:-$SCRIPT_DIR/state}"
say "The guided installer delegates stack changes to the trusted propflow-deploy executable."
ask "Continue with this $MODE?" || { say "Cancelled. No changes were made."; exit 0; }

if [[ "$STATE_EXISTS" == "1" ]]; then
  BACKUP="$STATE_DIR/backups/pre-upgrade-$(date +%Y%m%d-%H%M%S)"
  mkdir -p "$BACKUP"
  say "Creating a pre-upgrade snapshot in $BACKUP..."
  cp "$STATE_DIR/deployment.json" "$BACKUP/"
  for name in dataprotection-keys attachments; do
    [[ -d "$STATE_DIR/$name" ]] && cp -R "$STATE_DIR/$name" "$BACKUP/"
  done
  POSTGRES_PASSWORD="$(node -e 'const fs=require("fs"); const p=JSON.parse(fs.readFileSync(process.argv[1], "utf8")); process.stdout.write(p.postgresPassword)' "$STATE_DIR/deployment.json")"
  export POSTGRES_PASSWORD
  say "Creating a database backup..."
  docker compose --project-name propflow --file "$SCRIPT_DIR/compose.yaml" exec -T database pg_dump -U propflow -d propflow -Fc > "$BACKUP/propflow.dump" || {
    unset POSTGRES_PASSWORD; say "The pre-upgrade backup failed. Nothing was upgraded."; exit 1;
  }
  unset POSTGRES_PASSWORD
  say "Backup complete. Stopping the previous stack..."
  "$SCRIPT_DIR/propflow-deploy" --state "$STATE_DIR" down
fi

cd "$SCRIPT_DIR"
xattr -dr com.apple.quarantine . 2>/dev/null || true
chmod +x propflow-deploy api/PropFlow.Api admin/PropFlow.Admin

say "Starting PropFlow and applying migrations, permissions, and health checks..."
if [[ -n "$STATE_DIR" ]]; then ./propflow-deploy --state "$STATE_DIR" up; else ./propflow-deploy up; fi
say "Verifying the running workspace..."
if [[ -n "$STATE_DIR" ]]; then ./propflow-deploy --state "$STATE_DIR" status; else ./propflow-deploy status; fi
curl --fail --silent --show-error --insecure --max-time 10 https://localhost:5001/health/ready >/dev/null || {
  say "The API readiness check failed. Inspect logs under ${STATE_DIR:-$SCRIPT_DIR/state}/logs."; exit 1;
}
say "Installation complete. Open http://127.0.0.1:3000 to finish setup."
