# Milestone 5 Reservations

## Goal

Deliver the mockup-led Reservations milestone as one hash-addressable screen:

- `5.1 Reservations`

## Scope

- Render the Reservations owner screen in the existing React/Vite app shell.
- Preserve the StockHub sidebar, sync alert, owner account controls, warm visual
  language, responsive behavior, and previous Access/Dashboard/Inventory/Orders
  routes.
- Make visible controls functional in the browser: search, timer refresh, CSV
  export, active hold release, expired hold restore, workspace/account/status
  panels, appearance toggle, sidebar collapse, and future-milestone navigation.
- Represent reservation state with deterministic preview data until durable
  Reservations persistence and marketplace connector events exist.

## Non-goals

- Durable reservation persistence.
- Real marketplace hold creation, release, retry, or payment conversion.
- Real Euronics connector repair.
- Marketplace connector credential repair, billing upgrade execution, and full
  durable Reservations persistence.

## Start Gate

- Previous milestone hook: Milestone 4 Orders is closed on GitHub with zero open
  issues as of 2026-10-10. `origin/main` is `6bccd9c`; `origin/dev` is
  `21549b3`; the current Milestone 5 branch starts from local SHA `624f7ae`.
- Roadmap source: `docs/plans/mockup-led-roadmap.md` defines Milestone 5 as
  `5 Reservations` with one screen, `5.1 Reservations`.
- Visual source: user-provided screenshot
  `/home/javad/Pictures/Screenshots/Screenshot From 2026-10-10 17-13-27.png`.
- Live GitHub milestone: milestone number 6, `5 Reservations`, is open.
- GitHub issue tracking: issues #104 through #107 cover screen delivery,
  visible interactions, regression protection, and closure evidence.

## Implementation Plan

| ID | Issue | Screen | Deliverable | Acceptance |
|---|---|---|---|
| R1 | #104 | 5.1 Reservations | Owner reservations list with active holds, summary metrics, expired-today lost-sale table, platform sync alert, and sidebar | Route `#reservations` renders the screenshot-aligned screen |
| R2 | #105 | 5.1 Reservations | Search, CSV export, timer refresh, release, restore, workspace/status/account/notification/appearance, upgrade, collapse, and navigation controls | Every visible screenshot control responds visibly or routes to the owning future milestone |
| R3 | #106 | Previous milestones | Preserve existing routes and backend dashboard-state persistence | Existing route SSR tests for Access, Dashboard, Inventory, and Orders continue to pass; dashboard state regression remains covered |
| R4 | #107 | Closure evidence | Plan, knowledge, project status, closure report, tracking check, PR/CI evidence | Evidence identifies branch/source SHA, tests, smoke results, GitHub state, and unverified deployment dimensions |

## Testing

- Frontend route SSR test for `#reservations`.
- Existing route regression test for Dashboard/Access/Inventory/Orders.
- Frontend typecheck/build and Vitest suite.
- Browser smoke for Reservations interactions and previous milestone routes
  when a local dev server is available.

## Documentation

- Update `docs/knowledge/frontend.md` with the Reservations route, behavior,
  and deferrals.
- Closure report remains future work until GitHub issue, PR, CI, and milestone
  closure evidence are synchronized.
