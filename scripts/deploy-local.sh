#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: STOCKHUB_CONFIG_DIR=/private/path $0 {dev|staging|prod} <commit-sha>" >&2
  exit 2
}
[[ "$#" == 2 ]] || usage
target="$1"
commit="$2"
[[ "$target" =~ ^(dev|staging|prod)$ ]] || usage
[[ "$commit" =~ ^[a-f0-9]{40}$ ]] || usage

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ "$target" != dev ]]; then
  release_tag="${STOCKHUB_RELEASE_TAG:-}"
  [[ "$release_tag" =~ ^v0\.[0-9]+\.0$ ]] || {
    echo "Staging and prod require STOCKHUB_RELEASE_TAG" >&2
    exit 2
  }
  [[ "$(git -C "$repo" cat-file -t "refs/tags/$release_tag")" == tag ]] || exit 2
  [[ "$(git -C "$repo" rev-list -n 1 "refs/tags/$release_tag")" == "$commit" ]] || exit 2
  git -C "$repo" merge-base --is-ancestor "$commit" refs/remotes/origin/main || exit 2
fi
config_dir="${STOCKHUB_CONFIG_DIR:-$HOME/.config/stockhub}"
config="$config_dir/$target.env"
state_dir="${STOCKHUB_STATE_DIR:-$HOME/.local/state/stockhub}"
[[ -f "$config" ]] || { echo "Missing $config" >&2; exit 2; }
[[ "$(stat -c %a "$config")" == 600 ]] || { echo "$config must have mode 600" >&2; exit 2; }

mkdir -p "$state_dir"
export STOCKHUB_IMAGE="stockhub:$commit"
export STOCKHUB_PORT
STOCKHUB_PORT="$(sed -n 's/^STOCKHUB_PORT=//p' "$config" | tail -1)"
[[ "$STOCKHUB_PORT" =~ ^[0-9]+$ ]] &&
  (( STOCKHUB_PORT >= 1024 && STOCKHUB_PORT <= 65535 )) || {
  echo "Invalid port in $config" >&2
  exit 2
}
compose=(docker compose --project-name "$target-stockhub" --env-file "$config" -f "$repo/deploy/compose.local.yml")

if ! docker image inspect "$STOCKHUB_IMAGE" >/dev/null 2>&1; then
  build_dir="$(mktemp -d)"
  trap 'rm -rf "$build_dir"' EXIT
  git -C "$repo" archive "$commit" | tar -x -C "$build_dir"
  docker build -t "$STOCKHUB_IMAGE" "$build_dir"
fi

previous=""
[[ -f "$state_dir/$target.image" ]] && previous="$(cat "$state_dir/$target.image")"
"${compose[@]}" up --wait -d postgres
if [[ "$target" == prod ]]; then
  backup_dir="$state_dir/backups"
  mkdir -p "$backup_dir"
  chmod 700 "$backup_dir"
  "${compose[@]}" exec -T postgres pg_dump -U stockhub stockhub | gzip > "$backup_dir/stockhub-$(date -u +%Y%m%dT%H%M%SZ).sql.gz"
fi
"${compose[@]}" run --rm --no-deps stockhub dotnet StockHub.Api.dll --migrate-only
"${compose[@]}" up -d --no-build stockhub
healthy=0
for attempt in {1..24}; do
  if curl --fail --silent --show-error --max-time 3 "http://127.0.0.1:$STOCKHUB_PORT/ready" >/dev/null 2>&1; then
    healthy=1
    break
  fi
  sleep 5
done
if [[ "$healthy" != 1 ]]; then
  if [[ -n "$previous" ]]; then
    STOCKHUB_IMAGE="$previous" "${compose[@]}" up -d --no-build stockhub || true
  fi
  echo "$target health check failed; previous image restored when available" >&2
  exit 1
fi
printf '%s\n' "$STOCKHUB_IMAGE" > "$state_dir/$target.image"
echo "Deployed $target: $STOCKHUB_IMAGE at http://127.0.0.1:$STOCKHUB_PORT"
