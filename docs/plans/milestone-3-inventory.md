# Milestone 3 - Inventory

Status: implemented on branch `codex/milestone-3-inventory`; awaiting PR merge and deployment verification

## Goal

Deliver the Inventory milestone from the approved mockup screenshots:
Inventory list, Inventory empty state, Warehouse staff inventory view, Product
detail, and Product detail when all platforms are failing. The milestone covers
backend and frontend together.

## Source material

- User-provided screenshots dated 2026-10-01 for sections 3.1 through 3.5.
- `docs/mockups/StockHub Final.html`, section `3 Inventory`.
- `docs/plans/mockup-led-roadmap.md`, milestone `3 Inventory`.

## Scope

- Workspace-scoped Inventory API projections for product list and product
  detail using existing workspace products where present.
- Demo projection for the approved populated Inventory screenshots when a
  workspace has no products or the route is opened unauthenticated for visual
  review.
- Role-aware capabilities for owner/manager/admin versus Warehouse staff.
- On-hand adjustment endpoint reusing the existing product persistence path.
- Retry-sync endpoint that queues the Inventory-owned retry state while real
  marketplace connector repair remains the Platforms milestone.
- React routes for:
  - `#inventory`
  - `#inventory-empty`
  - `#inventory-staff`
  - `#inventory-detail`
  - `#inventory-detail-failing`

## Non-goals and handoffs

- Marketplace OAuth, token repair, and live sync workers are owned by
  Milestone 6 Platforms.
- Pricing-rule automation is owned by Milestone 7 Pricing rules.
- Full CSV import review and manual product creation forms are future
  Inventory hardening; existing Dashboard/onboarding import contracts remain
  available.
- Orders, Reservations, Reports, Team, Settings, and dark-mode quick view remain
  their owning roadmap milestones.

## GitHub tracking

| Issue | Scope |
|---|---|
| #74 | 3.1 Inventory list and sync states |
| #76 | 3.2 Inventory empty state and import handoff |
| #75 | 3.3 Warehouse staff inventory permissions |
| #77 | 3.4-3.5 Product detail, platform failures, and audit log |
| #78 | 3.x Inventory milestone quality gate and deployment verification |

## Verification

Local checks on 2026-10-01:

- `cd frontend && npm test` - PASS, 14 tests.
- `cd frontend && npm run build` - PASS.
- `cd frontend && npm run contract:check` - PASS.
- `dotnet test StockHub.sln` - PASS, 16 tests.
- Browser smoke verified `#inventory`, `#inventory-empty`,
  `#inventory-staff`, `#inventory-detail`, `#inventory-detail-failing`,
  `#dashboard`, and `#signin` on `http://127.0.0.1:5173`.

## Closure checklist

- [x] New development is based on current `origin/main`, not the stale local
  branch.
- [x] Previous Cover, Access, and Dashboard routes still render in tests or
  browser smoke.
- [x] Related Dashboard navigation continues to route to Inventory.
- [x] Buttons/icons are functional or open explicit owning-milestone handoffs.
- [ ] PR merged to `main`.
- [ ] Milestone issues closed.
- [ ] Deployed pod/image verified against latest merged commit.
