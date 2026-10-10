# StockHub Architecture Knowledge

Status: accepted direction; architecture ADRs merged and Cover closure verified in PR #18

Milestones 0–6 are closed in GitHub. See [project status](../project-status.md)
for delivery evidence; inspect live GitHub before claiming deployment freshness.
Pricing rules planning is in [milestone 7](../plans/milestone-7-pricing.md).

StockHub starts as an extraction-ready modular monolith. Modules own their
domain, persistence boundaries, contracts, configuration, and tests. The first
planned extraction is Platform Integration; Inventory, Reservations, and Orders
remain together initially for transactional correctness.

The architecture baseline is [docs/plans/architecture-baseline.md](../plans/architecture-baseline.md).
Decision records are in [docs/decisions/](../decisions/).

Key rules:

- PostgreSQL is authoritative for physical inventory, reservations, orders,
  permissions, and audit data.
- Modules do not reference other modules' domain entities, mappings, or tables.
- Shared libraries contain only technical building blocks and versioned
  integration contracts.
- Messaging is technology-agnostic. In-process mediation is initial; Kafka and
  RabbitMQ are infrastructure adapters for future extraction.
- Integration messages use an outbox and at-least-once delivery semantics.
- Redis is optional caching infrastructure, never the source of truth.
- Frontend API types are generated from OpenAPI; domain models are not shared
  between C# and TypeScript.

Access backend work now lives in `backend/src/StockHub.Api` with the HTTP host
and Access store boundary isolated from the frontend. Cookie authentication,
identity/password hashing, workspace membership checks, and the `/api/auth` and
`/api/workspaces` routes. Remaining Access remediation is tracked outside closed product milestones.


## Milestone 2 Dashboard architecture note

Dashboard is implemented as a workspace-scoped read/action surface in the modular monolith. `IDashboardStore` owns Dashboard UI state, while product data continues through `IProductStore`. Later-module controls do not mutate future module state; they open named handoffs until the owning milestone implements the real workflow.
