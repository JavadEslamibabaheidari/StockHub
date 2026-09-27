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
- required status check: `Frontend test and build`;
- required status check: `Backend build and test`;
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
4. Resolve review comments and conversations when present.
5. Keep the branch up to date with `main` so strict checks pass.
6. Merge only after required checks pass.
7. Use auto-merge where practical.

No app implementation should begin until this gate is in place and the
roadmap/spec is approved.

## CI policy

The `Repository sanity` workflow checks repository health while the separate
frontend and backend workflows validate application code:

- frontend and backend must have separate CI pipelines;
- branch protection must require both frontend and backend checks;
- `Repository sanity` may remain as a lightweight repository-health check;
- each PR must trigger CI for changed code and any shared contracts it affects;
- app checks must include build, lint/format, tests, and any generated-artifact
  validation that the chosen stack requires.
- Pull-request approval is not a required merge gate. Review may still be used
  voluntarily for collaboration and risk management.

Milestone closure PRs must also include the closure report required by
`docs/ai-development-workflow.md` and pass
`scripts/check-milestone-closure.sh`.

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
- Merge or push to `main` publishes the digest. The private
  `StockHub-Deployment` Actions task selects a successful publish run and
  deploys its image to persistent local kind through the dedicated runner.
- Shared review-staging CD activates only after a hosted target is configured.
- Production promotes the digest recorded by a successful review-staging run
  through a manual dispatch and protected GitHub environment.

The `Image scan and local Kubernetes smoke` check must be required on `main`.
It reports SARIF findings, blocks fixable high/critical vulnerabilities, and
tests Compose and kind before publishing. Configure production approvers and
main-only deployment rules before enabling promotion. The workflow fails on
missing cluster or database configuration.

After each milestone closes, synchronize its report and GitHub milestone,
then create one annotated `v0.<milestone-number>.0` tag on verified `main`.
See [the delivery gate](production-delivery-gate.md) for the current rollout.

## Tracking

GitHub issues are the operational source of truth for active work. Keep the
policy issue and related infrastructure issues synchronized with this document.
