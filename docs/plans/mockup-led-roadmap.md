# StockHub Mockup-Led Roadmap

Status: roadmap structure approved; architecture baseline drafted; Access spec issues created

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

The current milestone is `1 Access`.

GitHub issues should exist for the current milestone with detailed acceptance
criteria and direct mockup references. Future milestone records may exist before
their start gate, but detailed issues should be created when that milestone is
ready to be specified.

Current Access issues cover:

- `1.1 Sign up`
- `1.2 Sign in`
- `1.3 Create workspace`
- `1.4 Onboarding checklist`

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

## Architecture checkpoint

The current agreed direction is an extraction-ready modular monolith. Modules
run in one application initially, but each module owns its domain, persistence
boundary, contracts, and tests so it can later move to a separate repository
and deployable service. Messaging is technology-agnostic: the initial runtime
uses in-process mediation behind shared abstractions, with an outbox boundary
for future Kafka or RabbitMQ transport. Redis is an optional infrastructure
adapter and is not a source of truth for inventory or reservations.

The architecture baseline and ADRs are drafted locally. GitHub synchronization
and the required PR are pending because the current workspace does not contain
a usable Git checkout and GitHub API access is unavailable.
