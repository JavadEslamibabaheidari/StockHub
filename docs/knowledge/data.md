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

The Access migration baseline is `backend/database/migrations/001_access.sql`.
It defines users, workspaces, memberships, and cookie session records with
case-insensitive email uniqueness, owner-role constraints, ISO code columns,
and tenant foreign keys. The application store is behind `IAccessStore` so the
PostgreSQL adapter can be introduced without sharing domain entities across
the API/frontend boundary.

The encrypted authentication cookie also depends on ASP.NET Data Protection
keys. Local Compose projects keep a dedicated named key-ring volume, and the
local Kubernetes deployment keeps a dedicated PVC, so an API restart does not
invalidate persisted sessions. Hosted environments must provide durable,
encrypted key-ring storage shared by app replicas; do not use an ephemeral
container filesystem for this key material.


## Pricing rules

Migration `006_pricing_rules.sql` stores marketplace rules by workspace with
their scope, signed adjustment, unit, enabled state, priority, and optional
inclusive date range. The API checks workspace membership before reads and
requires Owner/Admin/Manager for mutations. Product imports and base-price
updates lock the workspace and validate resulting prices against persisted
rules in the same transaction; an invalid price rolls back the change. Rule
evaluation and preview use existing workspace products and keep unavailable
cost-based margin null.

## Dashboard read model state

Milestone 2 adds workspace-scoped `dashboard_states` in `004_dashboard_state.sql`. The table stores Dashboard UI/action state such as first-sync mode, Euronics retry, mismatch resolution, restock handoff, expanded low-stock items, and the current detail panel. Product counts still live in `products`; Dashboard on-hand adjustments update product persistence through the product store. The first-use Dashboard response uses the member's persisted workspace name. Real connector, reservation, order, and inventory ledgers remain owned by their future modules.

## Team memberships and invitations

The Access module owns workspace membership and invitation reads/actions through
`IAccessStore`; the API returns a workspace-scoped Team response and checks
current membership and role for each request. Migration
`008_team_management.sql` adds durable invitation UUIDs and an index for pending
workspace invitations. Tokens remain hashed at rest; resend rotates the token,
cancellation invalidates it, and removal takes effect on the next authorized
request. The last Owner cannot be removed, and Owner cannot be invited.

Owner/Admin manage Team invitations and members. Manager, WarehouseStaff, and
Viewer cannot. Inventory detail redacts price data for WarehouseStaff and
Viewer; the access matrix and generated endpoint contract are documented in
[`milestone-9-team.md`](../plans/milestone-9-team.md) and
[`access.openapi.json`](../../contracts/access.openapi.json).

## Onboarding selection state

The Access follow-up stores the user's latest selected next step per user and
workspace in onboarding_selections, introduced by
009_onboarding_selection.sql. It is presentation/workflow state, not evidence
that the owning capability completed. The older onboarding_actions table
continues to record requested actions. The API only accepts the three defined
handoffs: import-products, connect-platform, and invite-team; reads are
workspace-membership checked, and selection state is isolated per workspace.
