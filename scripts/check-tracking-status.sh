#!/usr/bin/env bash

# Compare committed closure verdicts with the live GitHub milestone state.
set -euo pipefail

repository="${GITHUB_REPOSITORY:-JavadEslamibabaheidari/StockHub}"
command -v curl >/dev/null || { echo 'curl is required for live tracking verification' >&2; exit 2; }
command -v jq >/dev/null || { echo 'jq is required for live tracking verification' >&2; exit 2; }

read_milestone() {
  local number="$1"
  local url="https://api.github.com/repos/$repository/milestones/$number"
  if [[ -n "${GH_TOKEN:-}" ]] && command -v gh >/dev/null; then
    gh api "repos/$repository/milestones/$number"
  else
    curl --fail --silent --show-error --location --max-time 15 \
      -H 'Accept: application/vnd.github+json' "$url"
  fi
}

failed=0
shopt -s nullglob
reports=(docs/reports/milestone-*-closure.md)
for report in "${reports[@]}"; do
  status="$(sed -n 's/^status: *//p' "$report" | head -1)"
  number="$(sed -n 's/^github_milestone_number: *//p' "$report" | head -1)"
  report_title="$(sed -n 's/^milestone: *//p' "$report" | head -1)"
  if [[ ! "$number" =~ ^[0-9]+$ ]]; then
    echo "FAIL $report: github_milestone_number is missing or invalid" >&2
    failed=1
    continue
  fi
  if [[ "$status" != PASS && "$status" != BLOCKED && "$status" != 'IN PROGRESS' ]]; then
    echo "FAIL $report: unsupported status '$status'" >&2
    failed=1
    continue
  fi
  if ! milestone="$(read_milestone "$number")"; then
    echo "FAIL $report: cannot read GitHub milestone $number" >&2
    failed=1
    continue
  fi
  live_state="$(jq -r '.state' <<< "$milestone")"
  open_issues="$(jq -r '.open_issues' <<< "$milestone")"
  live_title="$(jq -r '.title' <<< "$milestone")"
  if [[ -z "$report_title" || "$report_title" != "$live_title" ]]; then
    echo "FAIL $report: milestone title '$report_title' does not match GitHub milestone $number '$live_title'" >&2
    failed=1
    continue
  fi
  if [[ "$status" == PASS ]]; then
    if [[ "$live_state" != closed || "$open_issues" != 0 ]]; then
      echo "FAIL $report: PASS requires a closed GitHub milestone with zero open issues; live state=$live_state open=$open_issues" >&2
      failed=1
    else
      echo "PASS $report: closed; zero open issues"
    fi
  elif [[ "$live_state" != open ]]; then
    echo "FAIL $report: $status requires an open GitHub milestone; live state=$live_state" >&2
    failed=1
  else
    echo "PASS $report: $status; milestone open with $open_issues issue(s)"
  fi
done
exit "$failed"
