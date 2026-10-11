# StockHub Mockup-Led Roadmap

Status: milestones 0–11 are closed in GitHub with PASS reports. M12 Design system is active on `main` after M11 closure; the unparented milestone remains last.

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

Milestones 0–11 are closed in GitHub with zero open issues. M10 Settings feature
PR #147 merged at `3c4b655`; contrast correction PR #148 merged at `e280c0a`;
closure PR #150 merged at `32a3d12`. M11 Dark mode PR #155 merged at
`dceb8cf9adba52bca7c5964c427c49effee26e34`; PASS closure PR #158 merged to
`main` at `17ae97645cf898bb68a73323e42f75da8e2eef1c`. Live `dev` remains at
`21549b36342cb031dc94bc79b28ee502be5dbf9c` and does not contain M10–M12.

M11 Dark mode PR #155 merged to `main` at
`dceb8cf9adba52bca7c5964c427c49effee26e34` from source
`75a8890539718d36afeab217edbfbee4efe786f4`; its published digest is deployed
to the persistent local target. Live `dev` remains at
`21549b36342cb031dc94bc79b28ee502be5dbf9c`.

M12 Design system, GitHub milestone #13, is active. Live GitHub UI confirmed the
milestone was open with zero issues; issues #159–#161 have since been created
under it and assigned to the repository owner. The implementation plan is
[milestone-12-design-system.md](milestone-12-design-system.md). Its scope is
page 2 of the Design Review PDF: shared light/dark semantic tokens, typography,
strict en-GB formats, an interactive design-system reference screen, and
adoption across implemented application routes. The previous M11 closure hook
passes against `main` at `17ae97645cf898bb68a73323e42f75da8e2eef1c`.
The authenticated repository Projects page reports no linked projects; this
board configuration gap is recorded in the M12 plan. The CLI tracking hook
cannot verify live state with the configured GitHub credentials.

Completed milestone `11 Dark mode`, GitHub milestone #12, delivered dark
Dashboard, Inventory with quick view, and Product detail variants using M10's
shared Light/Dark/System preference. Issues #151–#154 are closed. Its execution
plan is [milestone-11-dark-mode.md](milestone-11-dark-mode.md), and the PASS
closure report records verified implementation, deployment, and status gates.
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
