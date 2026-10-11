# StockHub Mockup-Led Roadmap

Status: milestones 0–10 are closed in GitHub with PASS reports. Dark mode (`11 Dark mode`) is merged to `main` and deployed to the persistent local target; GitHub milestone #12 remains in progress while its closure report and status gates are completed.

## Source of truth

GitHub is the source of truth for active milestones, issues, and board state.
This document explains the roadmap structure and keeps the mockup references
readable inside the repository.

The roadmap is led by `docs/mockups/StockHub Final.html`. Milestones must match
the mockup sections. Do not create a parallel product milestone structure.

No application implementation should start until the roadmap/spec is approved.

## Milestones

- `0 Cover`
  - `0 Cover`
- `1 Access`
  - `1.1 Sign up`
  - `1.2 Sign in`
  - `1.3 Create workspace`
  - `1.4 Onboarding checklist`
- `2 Dashboard`
  - `2.1 Dashboard`
  - `2.2 Dashboard first use`
  - `2.3 Dashboard first sync`
- `3 Inventory`
  - `3.1 Inventory`
  - `3.2 Inventory empty state`
  - `3.3 Inventory Warehouse staff view`
  - `3.4 Product detail`
  - `3.5 Product detail all platforms failing`
- `4 Orders`
  - `4.1 Orders`
  - `4.2 Order detail`
  - `4.3 Orders empty state`
  - `4.4 Orders filters with no results`
  - `4.5 Order detail return flow`
  - `4.6 Order detail cancel confirmation`
  - `4.7 Orders Warehouse staff view`
- `5 Reservations`
  - `5.1 Reservations`
- `6 Platforms`
  - `6.1 Platforms`
  - `6.2 Add platform picker`
- `7 Pricing rules`
  - `7.1 Pricing rules`
- `8 Reports`
  - `8.1 Reports Daily digest`
  - `8.2 Reports Weekly summary`
  - `8.3 Reports Alert settings`
- `9 Team`
  - `9.1 Team`
- `10 Settings`
  - `10.1 Settings Billing`
  - `10.2 Settings Appearance`
- `11 Dark mode`
  - `11.1 Dashboard dark`
  - `11.2 Inventory dark quick view open`
  - `11.3 Product detail dark`
- `12 Design system`
  - `12.1 Design system`

## Current milestone

Milestones 0–10 are closed in GitHub with zero open issues. M10 Settings
feature PR #147 merged at `3c4b655`; the dark-card contrast correction PR #148
merged at `e280c0a`; closure evidence PR #150 merged at `32a3d12`. Issues
#144–#146 and GitHub milestone #11 are closed. Settings execution and closure
evidence are in [milestone-10-settings.md](milestone-10-settings.md) and
[`milestone-10-settings-closure.md`](../reports/milestone-10-settings-closure.md).

M11 Dark mode PR #155 merged to `main` at
`dceb8cf9adba52bca7c5964c427c49effee26e34` from source
`75a8890539718d36afeab217edbfbee4efe786f4`; its published digest is deployed
to the persistent local target. Live `dev` remains at
`21549b36342cb031dc94bc79b28ee502be5dbf9c`.

The current milestone is `11 Dark mode`, GitHub milestone #12. It delivers
dark Dashboard, Inventory with quick view, and Product detail variants using
M10's shared Light/Dark/System preference. Issues #151–#154 track the screens,
integration, accessibility review, and closure. Its execution plan is
[milestone-11-dark-mode.md](milestone-11-dark-mode.md); implementation and
deployment are merged and verified; the closure report and GitHub status gates
remain in progress.
Team execution and closure evidence are in
[milestone-9-team.md](milestone-9-team.md) and
[`milestone-9-team-closure.md`](../reports/milestone-9-team-closure.md). Pricing execution is tracked in
[milestone-7-pricing.md](milestone-7-pricing.md), issues #116–#119; closure
evidence is in
[milestone-7-pricing-closure.md](../reports/milestone-7-pricing-closure.md).
The narrow-viewport browser automation follow-up is in Maintenance issue #123.

Reports execution is specified in [milestone-8-reports.md](milestone-8-reports.md), and its PASS closure evidence is in [milestone-8-reports-closure.md](../reports/milestone-8-reports-closure.md). PR #133 merged; closure PR #134 records the integration evidence and closes issue #131. Configured alert delivery is tracked separately in Maintenance issue #132. The source screens are on page 1 of [StockHub Design Review.pdf](../mockups/StockHub%20Design%20Review.pdf) and in the matching Reports section of [StockHub Final.html](../mockups/StockHub%20Final.html).

GitHub issues should exist for the current milestone with detailed acceptance
criteria and direct mockup references. Future milestone records may exist before
their start gate, but detailed issues should be created when that milestone is
ready to be specified.

Completed Access issues covered:

- `1.1 Sign up`
- `1.2 Sign in`
- `1.3 Create workspace`
- `1.4 Onboarding checklist`
- PostgreSQL persistence and integration tests (#39)
- Docker/Compose packaging (#40)
- One-origin frontend/API serving (#41)

Milestone 0 implementation is documented in
[docs/plans/milestone-0-cover.md](milestone-0-cover.md). It is frontend-only;
backend is not applicable because the Cover has no runtime data contract.

## Start gate for implementation

Implementation can begin only after:

- the roadmap/spec is approved;
- current milestone issues have clear acceptance criteria;
- the architecture baseline and ADRs in `docs/decisions/` are reviewed and
  merged;
- module boundaries, extraction rules, messaging abstractions, inventory
  correctness, authentication, caching, testing, and deployment decisions are
  resolved as described by the architecture baseline;
- implementation-blocking product and architecture decisions are resolved or
  explicitly deferred out of scope;
- GitHub milestone and issue state matches this roadmap; and
- the project board is available or its access gap is recorded.

The Access implementation plan is
[milestone-1-access.md](milestone-1-access.md), with remediation in
[milestone-1-access-remediation.md](milestone-1-access-remediation.md).
Dashboard implementation is documented in
[milestone-2-dashboard.md](milestone-2-dashboard.md).

## Milestone closure gate

A milestone must not be marked complete or closed in GitHub until its closure
report passes the reusable hook in
[docs/ai-development-workflow.md](../ai-development-workflow.md), including
the alignment matrix, deliberate deviations, missing coverage, follow-ups, and
evidence for code, tests, configuration, and GitHub tracking.

## Architecture checkpoint

The current agreed direction is an extraction-ready modular monolith. Modules
run in one application initially, but each module owns its domain, persistence
boundary, contracts, and tests so it can later move to a separate repository
and deployable service. Messaging is technology-agnostic: the initial runtime
uses in-process mediation behind shared abstractions, with an outbox boundary
for future Kafka or RabbitMQ transport. Redis is an optional infrastructure
adapter and is not a source of truth for inventory or reservations.

The architecture baseline and ADRs were accepted in PR #14. GitHub and Git access
were verified for the Pricing start gate on 2026-10-11.
