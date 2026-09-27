# StockHub PR, CI, and CD Policy

Status: hard pre-implementation gate

## Purpose

Before application implementation begins, StockHub must use a pull-request
delivery model with CI as the merge gate. Direct changes to `main` are not
allowed.

This policy applies to documentation, infrastructure, and future application
changes.

## Current GitHub protections

`main` is protected with:

- admin enforcement enabled;
- strict required status checks;
- required status check: `Repository sanity`;
- pull request required, with no approving review required;
- required conversation resolution;
- required linear history;
- disabled force pushes;
- disabled branch deletion;
- repository auto-merge enabled; and
- branch deletion after merge enabled.

## Required delivery flow

All changes must follow this path:

1. Create a new branch from current `main`.
2. Open a pull request for the branch.
3. Let CI run on the pull request.
4. Resolve review comments and conversations.
5. Keep the branch up to date with `main` so strict checks pass.
6. Merge only after required checks and review gates pass.
7. Use auto-merge where practical.

No app implementation should begin until this gate is in place and the
roadmap/spec is approved.

## CI policy

The current `Repository sanity` workflow is a temporary baseline for repository
health before application scaffolding exists.

Once frontend and backend scaffolding exist:

- frontend and backend must have separate CI pipelines;
- branch protection must require both frontend and backend checks;
- `Repository sanity` may remain as a lightweight repository-health check;
- each PR must trigger CI for changed code and any shared contracts it affects;
- app checks must include build, lint/format, tests, and any generated-artifact
  validation that the chosen stack requires.

## Required split-workflow PR

After application scaffolding exists, issue `#9` requires a dedicated
infrastructure PR to split CI into separate workflow files. The expected target
shape is:

- `.github/workflows/repository-sanity.yml`
- `.github/workflows/frontend-ci.yml`
- `.github/workflows/backend-ci.yml`

That PR must also update branch protection so the required checks include the
separate frontend and backend CI jobs. Do not leave branch protection requiring
only `Repository sanity` after real app code exists.

## CD policy

Production deployment must not run directly from arbitrary branches.

Recommended lifecycle:

- PR branches trigger CI and may trigger optional preview or review
  deployments.
- Approved PRs with passing checks may auto-merge to `main`.
- Merge or push to `main` triggers CD to `review-staging`.
- Production remains deferred until deployment architecture, ownership,
  rollback, approvals, and observability are approved.

Production CD must be gated and explicit. It should not be introduced as a
side-effect of early app scaffolding.

## Tracking

GitHub issues are the operational source of truth for active work. Keep the
policy issue and related infrastructure issues synchronized with this document.
