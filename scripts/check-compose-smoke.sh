#!/usr/bin/env bash
set -euo pipefail

project="stockhub-smoke-${BASHPID}"
compose=(docker compose -p "$project" -f docker-compose.yml)
cleanup() {
  "${compose[@]}" down --volumes --remove-orphans
}
trap cleanup EXIT

"${compose[@]}" up --build --detach
for attempt in {1..30}; do
  if curl --fail --silent "http://localhost:${STOCKHUB_PORT:-8080}/health" >/dev/null; then
    break
  fi
  if [[ "$attempt" == 30 ]]; then
    "${compose[@]}" ps
    "${compose[@]}" logs --no-color stockhub postgres
    exit 1
  fi
  sleep 2
done

curl --fail --silent "http://localhost:${STOCKHUB_PORT:-8080}/" > /tmp/stockhub-root.html
rg -q '<!doctype html|<html' /tmp/stockhub-root.html
curl --fail --silent "http://localhost:${STOCKHUB_PORT:-8080}/workspace" > /tmp/stockhub-workspace.html
rg -q '<!doctype html|<html' /tmp/stockhub-workspace.html
test "$(curl --silent --output /dev/null --write-out '%{http_code}' "http://localhost:${STOCKHUB_PORT:-8080}/api/auth/session")" = "401"
