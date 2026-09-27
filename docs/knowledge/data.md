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

## Access planned data boundary

The first implementation milestone adds global Users, tenant Workspaces,
Workspace Memberships, single-use Invitations, workspace-scoped
OnboardingChecklistTasks, and active-workspace session state. User email
uniqueness is case-insensitive. Workspace creation and the initial Owner
membership are atomic and retry-safe. Invitation tokens are hashed, time
limited, single-use, and cannot grant a role above the inviter's authority.

Product imports, platform credentials, and product records remain owned by
their future modules; Access only owns the onboarding handoff and its durable
state. The detailed contract and acceptance criteria are in
[docs/plans/milestone-1-access.md](../plans/milestone-1-access.md).
