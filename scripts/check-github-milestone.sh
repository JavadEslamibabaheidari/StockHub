#!/usr/bin/env bash
set -euo pipefail

milestone="${1:-}"
[[ "$milestone" =~ ^[0-9]+$ ]] || { echo "Expected milestone number" >&2; exit 2; }
repository="${GITHUB_REPOSITORY:-JavadEslamibabaheidari/StockHub}"
response="$(curl --fail --silent --show-error --max-time 15 \
  -H 'Accept: application/vnd.github+json' \
  "https://api.github.com/repos/$repository/milestones?state=all&per_page=100")"
printf '%s' "$response" | jq -e --arg number "$milestone" '
  [.[] | select(.title | startswith($number + " "))] as $matches |
  ($matches | length) == 1 and
  $matches[0].state == "closed" and
  $matches[0].open_issues == 0
' >/dev/null || {
  echo "GitHub milestone $milestone must be closed with zero open issues" >&2
  exit 1
}
