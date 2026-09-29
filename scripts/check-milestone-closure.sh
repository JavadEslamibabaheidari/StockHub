#!/usr/bin/env bash

set -euo pipefail

report="${1:-}"
if [[ -z "$report" || ! -f "$report" ]]; then
  echo "Usage: $0 path/to/milestone-closure-report.md" >&2
  exit 2
fi

fail=0
require_pattern() {
  local pattern="$1"
  local description="$2"
  if ! grep -Eq "$pattern" "$report"; then
    echo "FAIL: $description" >&2
    fail=1
  fi
}

require_pattern '^status:[[:space:]]*PASS[[:space:]]*$' 'report status must be PASS'
require_pattern '^milestone:[[:space:]]*[^[:space:]].*$' 'milestone is missing'
require_pattern '^github_milestone_number:[[:space:]]*[0-9]+[[:space:]]*$' 'GitHub milestone number is missing'
require_pattern '^plan:[[:space:]]*[^[:space:]].*$' 'plan evidence is missing'
require_pattern '^mockup_evidence:[[:space:]]*[^[:space:]].*$' 'mockup evidence is missing'
require_pattern '^adr_evidence:[[:space:]]*[^[:space:]].*$' 'ADR evidence is missing'
require_pattern '^knowledge_evidence:[[:space:]]*[^[:space:]].*$' 'knowledge evidence is missing'
require_pattern '^roadmap_evidence:[[:space:]]*[^[:space:]].*$' 'roadmap evidence is missing'
require_pattern '^code_evidence:[[:space:]]*[^[:space:]].*$' 'code evidence is missing'
require_pattern '^tests_evidence:[[:space:]]*[^[:space:]].*$' 'test evidence is missing'
require_pattern '^configuration_evidence:[[:space:]]*[^[:space:]].*$' 'configuration evidence is missing'
require_pattern '^github_evidence:[[:space:]]*[^[:space:]].*$' 'GitHub evidence is missing'
require_pattern '^previous_milestone_hook:[[:space:]]*[^[:space:]].*$' 'previous-milestone hook result is missing'

for heading in \
  '## Alignment matrix' \
  '## Deliberate deviations' \
  '## Missing coverage and follow-ups' \
  '## Evidence and verification' \
  '## Closure verdict'; do
  require_pattern "^${heading//\//\\/}[[:space:]]*$" "required heading is missing: $heading"
done

if grep -Eiq '(^|[^[:alnum:]])(TBD|TODO|unknown|undecided|open question)([^[:alnum:]]|$)' "$report"; then
  echo 'FAIL: unresolved placeholder or open decision found' >&2
  fail=1
fi

for source in \
  docs/ai-development-workflow.md \
  docs/plans/mockup-led-roadmap.md \
  docs/mockups/StockHub\ Final.html \
  docs/knowledge/architecture.md; do
  if [[ ! -e "$source" ]]; then
    echo "FAIL: required source is absent: $source" >&2
    fail=1
  fi
done

if (( fail != 0 )); then
  exit 1
fi

echo "PASS: milestone closure report has complete metadata and required evidence sections"
