#!/usr/bin/env bash
# Poll-based auto-deploy: fetches the tracked branch and, if it moved,
# rebuilds & restarts the stack. Invoked by boardgames-deploy.timer, but
# can also be run by hand. Safe to run repeatedly - it's a no-op when there
# are no new commits.
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/apps/boardgames}"
BRANCH="${BRANCH:-main}"
COMPOSE_FILE="deploy/docker-compose.prod.yml"

cd "$REPO_DIR"

git fetch --quiet origin "$BRANCH"
LOCAL="$(git rev-parse HEAD)"
REMOTE="$(git rev-parse "origin/$BRANCH")"

if [[ "$LOCAL" == "$REMOTE" && "${1:-}" != "--force" ]]; then
    exit 0
fi

echo "$(date -u +%FT%TZ) Deploying $REMOTE (was $LOCAL)"
git reset --hard "origin/$BRANCH"
docker compose -f "$COMPOSE_FILE" up -d --build --remove-orphans
docker image prune -f >/dev/null 2>&1 || true
echo "$(date -u +%FT%TZ) Deploy complete"
