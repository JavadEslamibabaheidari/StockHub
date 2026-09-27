# StockHub Architecture Knowledge

Status: accepted direction; architecture ADRs merged and Cover closure verified in PR #18

Current milestone: `1 Access`. Cover is closed with passing closure evidence in
`docs/reports/milestone-0-cover-closure.md` and merged PR #18. The milestone plan is
[docs/plans/milestone-0-cover.md](../plans/milestone-0-cover.md). It is a
frontend-only foundation milestone; backend is not applicable. Milestone `1
Access` is active and follows the persisted issue sequence in its plan and
GitHub milestone.

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
