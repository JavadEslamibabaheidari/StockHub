#!/usr/bin/env bash
set -euo pipefail

project="stockhub-smoke-${BASHPID}"
compose=(docker compose -p "$project" -f docker-compose.yml)
cookie_jar="$(mktemp)"
port="${STOCKHUB_PORT:-8080}"
cleanup() {
  "${compose[@]}" down --volumes --remove-orphans
  rm -f "$cookie_jar" /tmp/stockhub-root.html /tmp/stockhub-workspace.html
}
trap cleanup EXIT

"${compose[@]}" up --build --detach
for attempt in {1..30}; do
  if curl --fail --silent "http://localhost:$port/health" >/dev/null; then
    break
  fi
  if [[ "$attempt" == 30 ]]; then
    "${compose[@]}" ps
    "${compose[@]}" logs --no-color stockhub postgres
    exit 1
  fi
  sleep 2
done

curl --fail --silent "http://localhost:$port/" > /tmp/stockhub-root.html
grep -Eq '<!doctype html|<html' /tmp/stockhub-root.html
curl --fail --silent "http://localhost:$port/workspace" > /tmp/stockhub-workspace.html
grep -Eq '<!doctype html|<html' /tmp/stockhub-workspace.html
api_url="http://localhost:$port/api"
test "$(curl --silent --output /dev/null --write-out '%{http_code}' "$api_url/auth/session")" = "401"

smoke_email="smoke-${BASHPID}@example.test"
test "$(curl --silent --output /dev/null --write-out '%{http_code}' \
  -c "$cookie_jar" -b "$cookie_jar" -H 'Content-Type: application/json' \
  -d "{\"fullName\":\"Smoke User\",\"email\":\"$smoke_email\",\"password\":\"SmokePassword123!\"}" \
  "$api_url/auth/sign-up")" = "201"
test "$(curl --silent --output /dev/null --write-out '%{http_code}' \
  -c "$cookie_jar" -b "$cookie_jar" "$api_url/auth/session")" = "200"

# The database session and encrypted cookie must remain valid when the API
# container restarts. Data Protection keys live on a separate named volume.
"${compose[@]}" restart stockhub
for attempt in {1..30}; do
  if curl --fail --silent "http://localhost:$port/health" >/dev/null; then
    break
  fi
  [[ "$attempt" != 30 ]] || { "${compose[@]}" logs --no-color stockhub; exit 1; }
  sleep 2
done
test "$(curl --silent --output /dev/null --write-out '%{http_code}' \
  -c "$cookie_jar" -b "$cookie_jar" "$api_url/auth/session")" = "200"
test "$(curl --silent --output /dev/null --write-out '%{http_code}' \
  -c "$cookie_jar" -b "$cookie_jar" -X POST "$api_url/auth/sign-out")" = "204"
test "$(curl --silent --output /dev/null --write-out '%{http_code}' \
  -c "$cookie_jar" -b "$cookie_jar" "$api_url/auth/session")" = "401"
