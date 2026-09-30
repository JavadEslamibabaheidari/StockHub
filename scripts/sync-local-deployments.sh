#!/usr/bin/env bash
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
state_dir="${STOCKHUB_STATE_DIR:-$HOME/.local/state/stockhub}"
mkdir -p "$state_dir"
exec 9>"$state_dir/sync.lock"
flock -n 9 || { echo "Another StockHub sync is running" >&2; exit 0; }

mode="${1:-sync}"
[[ "$mode" == sync || "$mode" == init ]] || { echo "Usage: $0 [init|sync]" >&2; exit 2; }
if [[ "$mode" == sync && ! -f "$state_dir/initialized" ]]; then
  echo "Waiting for branch protection and local init"
  exit 0
fi
git -C "$repo" fetch origin main --tags --quiet
remote_dev="$(git -C "$repo" ls-remote --heads origin dev)"
if [[ -z "$remote_dev" ]]; then
  echo "Waiting for protected origin/dev branch"
  [[ "$mode" == init ]] && exit 1
  exit 0
fi
git -C "$repo" fetch origin dev --quiet
dev_commit="$(git -C "$repo" rev-parse refs/remotes/origin/dev)"
[[ "$dev_commit" =~ ^[a-f0-9]{40}$ ]] || exit 2

mapfile -t remote_tags < <(git -C "$repo" ls-remote --tags --refs origin 'v0.*.0' | sed 's|.*refs/tags/||' | sort -V)
if [[ "$mode" == init ]]; then
  printf '%s\n' "${remote_tags[@]}" > "$state_dir/release-tags.seen"
  echo "Recorded existing release tags; future tags will be deployed"
fi

if [[ "$mode" == init ]]; then
  if [[ ! -f "$state_dir/dev.sha" || "$(cat "$state_dir/dev.sha")" != "$dev_commit" ]]; then
    "$repo/scripts/deploy-local.sh" dev "$dev_commit"
    printf '%s\n' "$dev_commit" > "$state_dir/dev.sha"
  fi
  touch "$state_dir/initialized"
  exit 0
fi

touch "$state_dir/release-tags.seen"
for tag in "${remote_tags[@]}"; do
  grep -Fxq "$tag" "$state_dir/release-tags.seen" && continue
  [[ "$tag" =~ ^v0\.([0-9]+)\.0$ ]] || continue
  milestone="${BASH_REMATCH[1]}"
  [[ "$(git -C "$repo" cat-file -t "refs/tags/$tag")" == tag ]] || {
    echo "Rejecting lightweight release tag $tag" >&2
    exit 1
  }
  commit="$(git -C "$repo" rev-list -n 1 "refs/tags/$tag")"
  git -C "$repo" merge-base --is-ancestor "$commit" refs/remotes/origin/main || {
    echo "Release tag $tag does not point to main history" >&2
    exit 1
  }
  release_dir="$(mktemp -d)"
  trap 'rm -rf "$release_dir"' EXIT
  git -C "$repo" archive "$commit" | tar -x -C "$release_dir"
  shopt -s nullglob
  reports=("$release_dir"/docs/reports/milestone-"$milestone"-*-closure.md)
  [[ "${#reports[@]}" == 1 ]] || { echo "Missing closure report for $tag" >&2; exit 1; }
  (cd "$release_dir" && bash scripts/check-milestone-closure.sh "${reports[0]}")
  "$repo/scripts/check-github-milestone.sh" "$milestone"
  STOCKHUB_RELEASE_TAG="$tag" "$repo/scripts/deploy-local.sh" staging "$commit"
  STOCKHUB_RELEASE_TAG="$tag" "$repo/scripts/deploy-local.sh" prod "$commit"
  printf '%s\n' "$tag" >> "$state_dir/release-tags.seen"
  rm -rf "$release_dir"
  trap - EXIT
done

# A failed dev build must not hold back a valid milestone release.
if [[ ! -f "$state_dir/dev.sha" || "$(cat "$state_dir/dev.sha")" != "$dev_commit" ]]; then
  "$repo/scripts/deploy-local.sh" dev "$dev_commit"
  printf '%s\n' "$dev_commit" > "$state_dir/dev.sha"
fi
