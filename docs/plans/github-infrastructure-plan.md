# StockHub GitHub Infrastructure Plan

Status: local scaffolding added; blocked before GitHub provisioning

## Goal

Prepare StockHub for issue-by-issue delivery with a GitHub-centered production
lifecycle while keeping product and application architecture decisions in the
separate product/architecture track.

## Confirmed current state

- The workspace is `/mnt/data/StockHub`.
- The only project artifacts currently present are the product mockups:
  `docs/mockups/StockHub Design Review.pdf` and `docs/mockups/StockHub Final.html`.
- A `.git` directory exists but is empty, so the workspace is not currently a
  usable Git repository and has no local history, branch, or remote.
- No GitHub repository, labels, milestones, project board, workflows, or issue
  templates could be verified locally.
- The configured GitHub CLI identity is `JavadEslamibabaheidari`, but its saved
  token is invalid. GitHub-side state is therefore unverified.
- The approved target is the personal owner account, repository `StockHub`,
  visibility `public`.

## Local scaffolding added

The following product-neutral files are now present locally:

- `README.md`
- `.github/pull_request_template.md`
- `.github/ISSUE_TEMPLATE/feature.yml`
- `.github/ISSUE_TEMPLATE/bug.yml`
- `.github/ISSUE_TEMPLATE/infrastructure.yml`
- `.github/workflows/ci.yml`

The CI workflow currently performs repository sanity checks only. Runtime-
specific build, test, and lint commands remain deferred until the application
stack is selected.

## Scope

This track covers repository and delivery-process infrastructure only:

- GitHub repository setup and initial default-branch policy
- labels, milestones, issues, and project-board conventions
- pull-request and issue templates
- branch-protection policy
- a minimal GitHub Actions CI skeleton that can grow with the application
- environment conventions for local development, review/staging, and later
  production
- synchronization of the above with local planning/status documentation

Application source code, product behavior, data modeling, service boundaries,
deployment implementation, and decisions derived from the mockups are out of
scope.

## Required decision gate

Before creating or selecting a GitHub repository, confirm all three values:

1. GitHub owner: the user or organization that should own the repository.
2. Repository name: proposed default is `StockHub`; confirm or replace it.
3. Visibility: `private` or `public`.

GitHub authentication must also be repaired before remote inspection or
provisioning: `gh auth login -h github.com` (or an equivalent authenticated
GitHub connector) is required. No repository creation has been attempted.

## Proposed target structure

### Repository

- Default branch: `main`.
- Keep product mockups under `docs/mockups/` as reference artifacts.
- Add a concise `README.md` only when repository initialization is approved.
- Add contribution and security guidance when the repository's visibility and
  deployment model are confirmed.

### Labels

Use a small, stable vocabulary rather than labels that encode implementation
details:

- Type: `type:feature`, `type:bug`, `type:chore`, `type:docs`,
  `type:security`
- Area: `area:frontend`, `area:backend`, `area:infrastructure`,
  `area:product`
- State/attention: `priority:high`, `priority:normal`, `blocked`,
  `good first issue`
- Lifecycle: `needs-triage`, `ready`, `in-progress`, `needs-review`

The final color palette and any organization-standard labels should be
reconciled with the target owner's existing conventions before creation.

### Milestones

Create milestones only after the product track confirms the first delivery
boundaries. The infrastructure track should reserve an initial
`Foundation / GitHub lifecycle` milestone for repository setup, templates,
CI, and protection rules; later product milestones belong to the product track.

### Issues and project board

- Use one issue per independently deliverable task.
- Each issue should state objective, context, acceptance criteria, dependencies,
  verification, and out-of-scope items where useful.
- Use a single project board for the initial lifecycle with views for backlog,
  active work, review, blocked work, and completed work.
- Keep GitHub issues, milestones, and board state as the operational source of
  truth; local plans explain intent and decisions.

### Pull-request template

The PR template should require:

- linked issue
- summary and scope
- verification performed
- migration/configuration/deployment notes
- security or data-impact notes
- screenshots or logs where relevant
- reviewer checklist and explicit acknowledgment of follow-up work

### Issue templates

Provide templates for:

- feature or user story
- bug report
- infrastructure/maintenance task
- security concern, if the target owner's security process permits a public
  template

Templates must remain product-neutral and must not encode architecture that has
  not been decided by Track 1.

### Branch protection plan

Protect `main` after the first CI workflow exists:

- pull request required for changes
- required CI checks once their names are stable
- force-push and branch deletion disabled
- stale approvals dismissed when new commits invalidate the review
- conversation resolution required
- direct pushes limited to the minimum necessary maintainers

Whether one or two approvals are required depends on the target owner/team
size. This should be chosen when repository administration is available.

### GitHub Actions CI skeleton

Start with a deliberately small workflow that validates repository health and
can be extended after the application stack is known:

- trigger on pull requests and pushes to `main`
- checkout with a pinned major action version
- establish the supported runtime only after Track 1 identifies it
- run formatting/linting, tests, and build/package validation when available
- upload useful test/build artifacts on failure where practical
- keep secrets and deployment credentials out of CI until environments are
  explicitly approved

No application-specific commands will be invented in this track.

### Environments

- `local`: developer workstation; no GitHub environment or shared secrets.
- `review` or `staging`: later shared or per-PR validation target, protected
  by environment approvals/secrets when deployment exists.
- `production`: deferred until deployment architecture, ownership, rollback,
  and observability are decided by the product/architecture track.

## Execution order after approval

1. Repair authenticated GitHub access and verify `StockHub` availability under
   the personal owner account.
2. Make the workspace's Git metadata writable, initialize the local repository,
   and create the approved remote, preserving
   the existing mockup files.
3. Add lifecycle documentation, PR/issue templates, and the minimal CI
   skeleton without application code.
4. Create labels, the foundation milestone, and the project board.
5. Apply branch protection after CI check names are stable.
6. Create the initial infrastructure issues, link them to the milestone and
   board, and work them one issue per branch/PR.
7. Verify local docs, GitHub tracking, and the remote default branch agree.

## Acceptance criteria

The infrastructure track is complete when:

- the approved GitHub repository exists under the confirmed owner, name, and
  visibility;
- the local repository has a usable `main` branch and an authenticated remote;
- the repository contains the approved PR template, issue templates, and CI
  workflow, with no application implementation added by this track;
- labels, the foundation milestone, and the project board exist and have clear
  conventions;
- `main` protection requires the agreed review and CI gates;
- local/review-staging/production environment boundaries are documented, with
  production explicitly deferred until its decisions are made;
- at least one CI run is observed and its result is recorded;
- GitHub issues and board items for the remaining setup work are synchronized
  with this plan; and
- no unresolved owner, repository-name, visibility, authentication, or
  repository-permission blocker remains.

## Current blockers

- GitHub CLI authentication is invalid; run `gh auth login -h github.com`.
- The pre-existing `.git` directory is read-only, so local Git initialization
  and the first commit cannot be completed in the current workspace.
- The application runtime is not yet selected, so CI commands must wait for
  the product/architecture track.
