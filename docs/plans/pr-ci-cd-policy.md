# StockHub PR, CI, and CD Policy

Status: hard pre-implementation gate

## Purpose

StockHub uses a pull-request delivery model with CI as the merge gate. Direct
changes to `dev` and `main` are not allowed.

This policy applies to documentation, infrastructure, and future application
changes.

## Current GitHub protections

`main` has the protections below. Apply the same required checks and push
restrictions to `dev` before enabling automatic dev deployment:

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
- branch deletion after merge enforced by `.github/workflows/delete-merged-branch.yml`
  for same-repository pull requests merged into `main`.

Pull requests from forks are intentionally excluded because their source branch
belongs to another repository. The default branch and unmerged branches are
never targeted.

## Required delivery flow

Issue changes follow this path:

1. Create a new branch from current `dev`.
2. Open a pull request into `dev`.
3. Let CI run on the pull request.
4. Resolve review comments and conversations when present.
5. Keep the branch up to date with `dev` so strict checks pass.
6. Merge only after required checks pass.
7. Use auto-merge where practical.

At milestone closure, merge `dev` into `main` by pull request after the closure
report passes. Tag the resulting `main` commit with an annotated
`v0.<milestone-number>.0` tag. See
[deployment-environments.md](deployment-environments.md) for promotion gates.

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

- PR branches trigger CI.
- Merged issue PRs push to `dev`; the local PC sync timer builds that protected
  commit and deploys it to the dev Compose stack.
- A passing milestone closure is promoted to `main`; its annotated version tag
  deploys the same locally built image to staging and then production after
  staging health verification.
- The three current targets bind to localhost on the developer PC. External
  production hosting and access require a separate deployment decision.

Production CD must be gated and explicit. It should not be introduced as a
side-effect of early app scaffolding.

## Tracking

GitHub issues are the operational source of truth for active work. Keep the
policy issue and related infrastructure issues synchronized with this document.
