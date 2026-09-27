# ADR 0005: Optional Redis caching

Status: Accepted in PR #14 (remote verification pending in this workspace)

## Decision

Caching is exposed through a small application-facing abstraction and supplied
by an optional Redis infrastructure adapter. A no-op or in-memory implementation
is sufficient initially.

Redis may cache read models, dashboard summaries, reports, platform metadata,
and rate-limit state. It must not be authoritative for inventory, reservations,
orders, permissions, or audit data. Inventory correctness does not depend on
Redis locks.
