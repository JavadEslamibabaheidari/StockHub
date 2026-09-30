#!/usr/bin/env bash
set -euo pipefail

tag="${1:-}"
if [[ ! "$tag" =~ ^v0\.([0-9]+)\.0$ ]]; then
  echo "Expected milestone tag v0.<number>.0" >&2
  exit 2
fi
milestone="${BASH_REMATCH[1]}"

git fetch origin main --quiet
[[ "$(git cat-file -t "refs/tags/$tag")" == tag ]] || {
  echo "Milestone release must use an annotated tag" >&2
  exit 1
}
tagged_commit="$(git rev-list -n 1 "refs/tags/$tag")"
[[ "$tagged_commit" == "$(git rev-parse HEAD)" ]] || {
  echo "Tag does not match the checked-out commit" >&2
  exit 1
}
git merge-base --is-ancestor "$tagged_commit" FETCH_HEAD || {
  echo "Tagged commit is not on main" >&2
  exit 1
}

shopt -s nullglob
reports=(docs/reports/milestone-"$milestone"-*-closure.md)
[[ "${#reports[@]}" -eq 1 ]] || {
  echo "Expected exactly one closure report for milestone $milestone" >&2
  exit 1
}
bash scripts/check-milestone-closure.sh "${reports[0]}"
bash scripts/check-github-milestone.sh "$milestone"
