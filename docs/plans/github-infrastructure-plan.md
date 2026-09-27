# StockHub GitHub Infrastructure Plan

Status: GitHub repository provisioned; mockup roadmap milestones synchronized;
Project board authorization pending

## Goal

Prepare StockHub for issue-by-issue delivery with a GitHub-centered production
lifecycle while keeping product and application architecture decisions in the
separate product/architecture track.

## Confirmed current state

- The workspace is `/mnt/data/StockHub`.
- The only project artifacts currently present are the product mockups:
  `docs/mockups/StockHub Design Review.pdf` and `docs/mockups/StockHub Final.html`.
- A `.git` directory exists but is empty and read-only in the workspace, so the
  local folder itself is still not a normal writable Git checkout.
- A temporary Git metadata directory was used to create and push the initial
  `main` commit without modifying the read-only `.git` directory.
- The configured GitHub CLI identity is authenticated as `JavadEslamibabaheidari`.
- The approved target has been created: personal owner account, repository
  `StockHub`, visibility `public`.
- Remote repository: <https://github.com/JavadEslamibabaheidari/StockHub>
- Initial commit: `9650787322d30056cc08d93fcc3c32836992ef59`
- Initial CI run completed successfully on September 27, 2026.
- Track 1 decision update: the roadmap is mockup-led from
  `docs/mockups/StockHub Final.html`; GitHub milestones must match the mockup
  sections and must not invent a parallel product milestone structure.

## Local scaffolding added

The following product-neutral files are now present locally and on `main`:

- `README.md`
- `.github/pull_request_template.md`
- `.github/ISSUE_TEMPLATE/feature.yml`
- `.github/ISSUE_TEMPLATE/bug.yml`
- `.github/ISSUE_TEMPLATE/infrastructure.yml`
- `.github/workflows/ci.yml`
- `docs/plans/mockup-led-roadmap.md`

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

Completed on September 27, 2026:

1. GitHub owner: `JavadEslamibabaheidari`.
2. Repository name: `StockHub`.
3. Visibility: `public`.
4. GitHub CLI authentication repaired for repository operations.

## Proposed target structure

### Repository

- Default branch: `main`.
- Keep product mockups under `docs/mockups/` as reference artifacts.
- Add a concise `README.md` only when repository initialization is approved.
- Add contribution and security guidance when the repository's visibility and
  deployment model are confirmed.

### Labels

The following lifecycle labels have been created:

- `track:github-infra`
- `type:infrastructure`
- `type:documentation`
- `type:ci`
- `status:ready`
- `status:blocked`
- `priority:p0`
- `priority:p1`
- `priority:p2`

### Milestones

GitHub milestones now match the mockup roadmap:

- `0 Cover`
- `1 Access`
- `2 Dashboard`
- `3 Inventory`
- `4 Orders`
- `5 Reservations`
- `6 Platforms`
- `7 Pricing rules`
- `8 Reports`
- `9 Team`
- `10 Settings`
- `11 Dark mode`
- `12 Design system`

The current implementation/spec milestone is `1 Access`. Future milestone
records may exist before their start gate, but detailed issues should be
created when the milestone becomes active.

### Issues and project board

Current milestone issues were created for `1 Access` with mockup references and
acceptance criteria:

- `Spec Access 1.1: Sign up`
- `Spec Access 1.2: Sign in`
- `Spec Access 1.3: Create workspace`
- `Spec Access 1.4: Onboarding checklist`

Existing infrastructure housekeeping issues remain open but are not assigned to
the mockup roadmap milestone:

- Replace CI placeholders after the app runtime is selected.
- Define review-staging and production deployment policies.
- Keep branch protection aligned with CI checks.
- Maintain GitHub lifecycle board and issue hygiene.

Project board creation is pending an additional GitHub OAuth scope refresh:
`project` and `read:project`.

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

`main` is protected:

- required status check: `repository-sanity`
- strict status checks enabled
- one approving pull-request review required
- stale approvals dismissed when new commits invalidate the review
- force-push and branch deletion disabled
- admin enforcement disabled for the owner account

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

No application implementation should begin until the roadmap/spec is approved.

### Environments

- `local`: developer workstation; no GitHub environment or shared secrets.
- `review-staging`: GitHub environment created, with deployment policy to be
  tightened when deployment targets and secrets are known.
- `production`: GitHub environment created, with production approvals, branch
  policy, rollback, and observability deferred until deployment architecture is
  decided by the product/architecture track.

## Execution order after approval

1. Completed: repaired authenticated GitHub access and verified `StockHub`
   availability under the personal owner account.
2. Completed with workaround: used temporary Git metadata because the
   workspace `.git` directory is read-only.
3. Completed: added lifecycle documentation, PR/issue templates, and the
   minimal CI skeleton without application code.
4. Completed: created labels and synchronized GitHub milestones to the mockup
   roadmap.
5. Completed: applied branch protection after the first CI run succeeded.
6. Completed: created the initial infrastructure issues; after the Track 1
   update, moved them out of the mockup milestone.
7. Pending: create the GitHub Project board after Project OAuth scopes are
   authorized.
8. Recommended next workspace step: replace the empty read-only `.git`
   directory with a normal writable clone or checkout of the remote.
9. Completed: created the current `1 Access` spec issues with acceptance
   criteria and mockup references.

## Acceptance criteria

The infrastructure track is complete when:

- the approved GitHub repository exists under the confirmed owner, name, and
  visibility;
- the local repository has a usable `main` branch and an authenticated remote;
- the repository contains the approved PR template, issue templates, and CI
  workflow, with no application implementation added by this track;
- labels, mockup-roadmap milestones, current-milestone issues, and the project
  board exist and have clear conventions;
- `main` protection requires the agreed review and CI gates;
- local/review-staging/production environment boundaries are documented, with
  production explicitly deferred until its decisions are made;
- at least one CI run is observed and its result is recorded;
- GitHub issues and board items for the remaining setup work are synchronized
  with this plan; and
- no unresolved owner, repository-name, visibility, authentication, or
  repository-permission blocker remains.

## Current blockers

- GitHub Project board creation requires approving the additional
  `project` and `read:project` scopes in the browser authorization flow.
- The pre-existing `.git` directory is read-only, so this workspace is not a
  normal writable checkout even though the remote repository has been created.
- The application runtime is not yet selected, so CI build/lint/test commands
  must wait for the product/architecture track.
- Roadmap/spec approval is required before app implementation begins.
