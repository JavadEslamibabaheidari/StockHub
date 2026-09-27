# StockHub Data Knowledge

PostgreSQL is the source of truth. Physical inventory uses an append-only stock
movement ledger. Reservations are separate temporary commitments.

```text
available = physical on-hand - active reservation quantity
```

Reservation allocation, conversion, release, and expiry are idempotent and
transactional. Competing allocations use database locking or serializable
transactions. Platform availability is a projection of warehouse availability.

Redis may later cache read models, dashboard summaries, reports, platform
metadata, or rate-limit state, but must not own inventory, reservations, orders,
permissions, or audit data.
