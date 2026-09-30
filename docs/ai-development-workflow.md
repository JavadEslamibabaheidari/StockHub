# StockHub AI Development Workflow

Status: active governance; current milestone state is recorded in
`docs/project-status.md`

## Purpose

This workflow keeps every milestone aligned with the approved product mockup,
milestone plan, accepted ADRs, curated knowledge, roadmap, implemented code,
tests, configuration, and GitHub tracking. It applies to work planned or
implemented with Codex or another engineering agent.

## Required order of work for every milestone

Every roadmap milestone, including Cover, Access, and all future sections, must
follow this sequence. No milestone is considered started merely because local
work exists.

1. Read `docs/knowledge/` and verify its important claims against the
   repository.
2. Run the previous-milestone closure hook before writing the next milestone
   plan or creating its implementation issues. For the first implementation
   milestone only, record that no previous implementation milestone exists and
   mark this check not applicable.
3. Write or update the milestone plan in `docs/plans/`.
4. Break the plan into dependency-ordered GitHub issues with explicit
   objectives, acceptance criteria, dependencies, owners, tests, and closure
   evidence.
5. Persist and verify those issues under the correct GitHub milestone before
   implementation begins. Record their URLs in the local milestone plan.
6. Confirm the start gate: product scope, API/data/auth decisions, frontend
   behavior, backend behavior, testing, delivery, and GitHub tracking are
   implementation-ready.
7. Implement one persisted issue at a time through a branch and pull request
   into `dev`. A merged PR deploys to the dev environment after CI and image
   checks.
   Keep backend and frontend work separately testable, with generated API
   contracts as the boundary.
8. After each issue, verify its acceptance criteria, tests, documentation, and
   GitHub status before moving to the next issue.
9. Collect and verify closure evidence while the report remains `IN PROGRESS`
   or `BLOCKED`. Resolve every issue and record deliberate deviations, missing
   coverage, and follow-ups.
10. Promote implementation from `dev` to `main` by milestone PR once the
    pre-promotion evidence is accepted. Verify the merged code and checks,
    close every issue and the GitHub milestone, then mark the final report
    `PASS` and run the closure validator and live milestone guard. Merge that
    final report to `main` before tagging its commit for local staging and
    prod as described in `docs/plans/deployment-environments.md`.

Implementation must not bypass the persisted issue sequence. If a missing
requirement appears, create or update a GitHub issue under the milestone,
update the local plan, and only then implement it.
Use `docs/project-status.md` to report merged code, published image, and live
dev, staging, and prod deployments as separate states.

## Previous milestone and start gate order

The previous milestone must be closed before a new milestone's issue breakdown
is created. The only exception is the first implementation milestone, whose
previous-milestone result is explicitly `NOT APPLICABLE`.

The implementation start gate remains the same after issue persistence:

- plan and issue acceptance criteria agree;
- GitHub issues are under the correct milestone and their URLs are recorded;
- accepted ADRs and knowledge agree with the plan; and
- no implementation-blocking decision or tracking gap remains.

## Issue execution rule

Each issue is completed independently and reports:

- changed files or external artifacts;
- acceptance criteria verified;
- tests and CI results;
- documentation/knowledge updates;
- deliberate deviations and follow-ups; and
- GitHub issue, branch, pull request, and board state.

Any issue that creates a new deployable project must also report the
containerization evidence required by the repository `AGENTS.md`: Dockerfile,
`.dockerignore`, local container startup documentation, health/readiness check,
and an automated CI build/smoke check. The issue is not implementation-ready
without this delivery path unless an approved, tracked deviation is recorded.

Only the main integration task may advance the milestone after reviewing that
report and resolving conflicts or gaps.

## Closure rule

The closure hook is mandatory for every milestone, not only milestone 0. The
report must compare the final implementation to the approved mockup/spec,
milestone plan, persisted issues, accepted ADRs, knowledge, roadmap, code,
tests, configuration, deployment evidence, and GitHub tracking.

The GitHub milestone is closed after its issues and closure evidence are
accepted. Its final report can then pass the live GitHub milestone guard. Do
not mark the milestone fully closed in local project status or create its
release tag until that `PASS` report is merged and both checks succeed.

## Legacy wording

The older per-milestone implementation guidance remains below for compatibility,
but all work is governed by the universal sequence above. The branch and pull
request rule is part of step 7, not an optional legacy step.

## Milestone start gate

The gate is open only when all of the following have evidence in the plan or
linked tracking:

- the roadmap section and approved mockup screens are identified;
- scope, non-goals, deliverables, acceptance criteria, and task dependencies
  are complete;
- backend APIs, data ownership, authentication, authorization, and error
  behavior are decided;
- frontend screens, states, responsive behavior, accessibility, and loading or
  failure behavior are decided;
- accepted ADRs and `docs/knowledge/` agree with the plan;
- backend and frontend build, lint, unit, integration, contract, and end-to-end
  checks are named;
- configuration, migration, local-development, review, and deployment impact
  are understood, including the required Dockerfile, container startup path,
  health/readiness verification, and CI container smoke check for every new
  deployable project;
- the GitHub milestone, issues, labels, board state, branch protection, and CI
  checks are verified or an explicit synchronization blocker is recorded; and
- no implementation-blocking decision remains open.

If any item lacks evidence, implementation stays paused and the missing
planning or infrastructure work is recorded as a blocker.

## Reusable milestone-closure hook

Every milestone closes with a report at
`docs/reports/milestone-<number>-<slug>-closure.md`. The report must contain
the metadata keys and headings below:

Start from `docs/templates/milestone-closure-report.md`, then replace every
placeholder before running the validator.

```text
status: PASS
milestone: <roadmap milestone>
plan: <plan path>
mockup_evidence: <screens or source path>
adr_evidence: <accepted ADR paths and references>
knowledge_evidence: <knowledge paths>
roadmap_evidence: <roadmap path and section>
code_evidence: <implemented paths or explicit none>
tests_evidence: <commands and results>
configuration_evidence: <configuration/deployment evidence>
github_evidence: <milestone/issues/board/PR evidence>
previous_milestone_hook: <PASS or NOT APPLICABLE with reason>
```

The report must also include these headings:

- `## Alignment matrix` — one row for mockup/spec, plan/acceptance criteria,
  ADRs, knowledge, roadmap, code, tests, configuration, and GitHub tracking;
- `## Deliberate deviations` — state `None` explicitly or list each deviation,
  its rationale, approval, and follow-up;
- `## Missing coverage and follow-ups` — state `None` explicitly or list each
  gap, owner, tracking issue, and due milestone;
- `## Evidence and verification` — commands, observed results, and links; and
- `## Closure verdict` — explain why the report is a complete PASS.

Each alignment-matrix row must use an explicit status: `PASS`, `PARTIAL`,
`DEVIATION`, `MISSING`, `BLOCKED`, or `NOT_APPLICABLE`. `PARTIAL`, `DEVIATION`,
`MISSING`, `BLOCKED`, and `NOT_APPLICABLE` rows must include the reason,
evidence, owner, follow-up issue, and target milestone. A closure can pass with
an approved deliberate deviation, but it cannot pass with an unexplained gap,
missing evidence, or an untracked follow-up.

The first implementation milestone must include
`previous_milestone: NOT APPLICABLE` and
`baseline: repository/planning state before implementation`. Milestone `0
Cover` is a product cover section, not a previous implementation milestone.

Run the structural gate with:

```bash
scripts/check-milestone-closure.sh docs/reports/milestone-<number>-<slug>-closure.md
```

The script is necessary but not sufficient. A human reviewer must inspect the
alignment matrix and verify that evidence matches the current repository and
GitHub state. The hook fails closure when the report is missing, any evidence
field is empty, a required source is absent, unresolved placeholders remain,
or the report does not explicitly account for deviations, missing coverage,
follow-ups, and the previous-milestone check.

## First implementation milestone

Milestone `0 Cover` is the first implementation milestone, so its
previous-milestone hook is `NOT APPLICABLE — no previous implementation
milestone exists`. Its own closure report is still mandatory. Milestone `1
Access` cannot start until the Cover closure report passes and its evidence is
synchronized.
