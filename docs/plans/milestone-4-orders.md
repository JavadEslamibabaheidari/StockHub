# Milestone 4 Orders

## Goal

Deliver the mockup-led Orders milestone as seven hash-addressable screens:

- `4.1 Orders`
- `4.2 Order detail`
- `4.3 Orders empty state`
- `4.4 Orders filters with no results`
- `4.5 Order detail return flow`
- `4.6 Order detail cancel confirmation`
- `4.7 Orders Warehouse staff view`

## Scope

- Render Orders screens in the existing React/Vite app shell.
- Preserve the warm StockHub visual language, sidebar navigation, sync status,
  owner/staff role distinction, and responsive behavior from Dashboard and
  Inventory.
- Make visible controls functional in the browser. Owner controls can filter,
  search, export CSV, inspect orders, print invoices, start/advance returns,
  refund, and cancel eligible orders. Staff controls can pick, mark shipped,
  open tracking, receive returns, and print a picking list.
- Keep real marketplace capture, payment, refund settlement, carrier label
  generation, and connector repair out of scope until their owning milestones
  or integrations exist; surface those actions as honest local state changes or
  explicit panels.

## Non-goals

- Full durable Orders persistence.
- Real payment/refund provider execution.
- Real carrier label creation or marketplace connector side effects.
- Closing the GitHub milestone from this implementation branch.

## Start Gate

- Previous milestone hook: PASS locally. `docs/project-status.md` and
  `docs/reports/milestone-3-inventory-closure.md` record milestone 3 closed on
  GitHub with zero open issues.
- Live GitHub milestone: milestone number 5, `4 Orders`, is open.
- GitHub issue tracking: issues #81 through #87 cover the seven Orders screens
  and workflows in milestone number 5.
- Repository refresh: `git fetch origin main dev` succeeded on 2026-10-02 after
  sandbox elevation.

## Implementation Plan

| ID | Issue | Screen | Deliverable | Acceptance |
|---|---|---|---|
| O1 | #82 | 4.1 Orders | Owner order list with status tabs, platform/date filters, search, CSV export, sync alert, and financial columns | Filters and search recompute rows; CSV downloads visible rows; rows open details |
| O2 | #83 | 4.2 Order detail | Order timeline, items, customer/shipping, stock movement, invoice and return/refund controls | Invoice opens print flow; return/refund controls update local state or route to return flow |
| O3 | #81 | 4.3 Empty state | No-orders state with platform connection CTA and “how an order moves” explainer | CTA routes to Platforms milestone handoff |
| O4 | #86 | 4.4 No results | Filtered empty state | Clear filters restores rows |
| O5 | #84 | 4.5 Return flow | Return detail with approval, receive, restock/write off, refund progression | Each step mutates local state and updates visible status |
| O6 | #87 | 4.6 Cancel confirmation | Cancel dialog for paid-to-pick order | Keep closes modal; cancel marks order cancelled and records stock return |
| O7 | #85 | 4.7 Staff view | Warehouse staff order list with allowed next-step actions only | Staff actions mutate local statuses; financial columns are hidden |

## Testing

- Frontend route SSR tests for all seven Orders screens.
- Existing route regression test for Dashboard/Access/Inventory.
- Frontend typecheck/build and Vitest suite.
- Browser smoke across Orders owner, detail, empty, no-results, return, cancel,
  and staff routes when a local dev server is available.

## Documentation

- Update `docs/knowledge/frontend.md` after implementation with routes, behavior,
  and deferrals.
- Closure report remains future work until GitHub issues, PR, CI, and milestone
  closure evidence are synchronized.
