# ADR 0004: Inventory ledger and reservation correctness

Status: Proposed for review

## Decision

Physical inventory is an append-only stock movement ledger. Reservations are
separate temporary commitments. Availability is physical on-hand minus active
reservations.

Reservation allocation, conversion, release, and expiry are idempotent and
protected by PostgreSQL transactions and appropriate locking. Platform stock
is a projection and never the authority.
