# Milestone 2 Dashboard plan

Status: active implementation branch `codex/milestone-2-dashboard`.

## Source material

- Roadmap: `docs/plans/mockup-led-roadmap.md`, milestone `2 Dashboard`.
- Mockups: `docs/mockups/StockHub Final.html`, section `2 Dashboard`.
- User-provided reference screenshots for `2.1 Dashboard`, `2.2 Dashboard first use`, and `2.3 Dashboard first sync` in the Milestone 2 request.
- Existing Access remediation and closure state: `docs/plans/milestone-1-access-remediation.md` and `docs/reports/milestone-1-access-closure.md`.

The attached screenshots are visual reference material. They do not override the user's request, repository instructions, or product milestone boundaries.

## Current prerequisite state

Milestone 1 Access is implemented but remains blocked for closure by external Google OAuth and outbound email provider setup. Dashboard implementation must preserve email signup, email sign-in, workspace creation and selection, sign-out, password recovery screens, and navigation into and out of the dashboard. Google OAuth and delivered email verification remain Access blockers until provider secrets are configured and tested.

Issue #47 remains the cross-cutting production image and Kubernetes delivery gate assigned to the Dashboard milestone. The Dashboard screens need their own implementation tracking under this milestone.

## Scope

Implement only the approved Dashboard screens:

- `2.1 Dashboard` for a live workspace with synced platform data, active reservations, sales split, sync status, and attention items.
- `2.2 Dashboard first use` for a new workspace with no platform data yet, including the get-started strip and empty dashboard panels.
- `2.3 Dashboard first sync` for a workspace during the first platform sync, including progress state and safe background dashboard placeholders.

The Dashboard milestone may use persisted dashboard data needed to make the screen work in the browser. Full Inventory, Orders, Reservations, Platforms, Pricing rules, Reports, Team, Settings, Billing, and Dark mode screens remain owned by later milestones.

## Visible control inventory and expected behavior

### Shared shell controls

- Workspace switcher: opens a menu, switches active workspace, and keeps session state.
- Create workspace link in the workspace menu: opens the existing workspace creation flow.
- Sidebar Dashboard: returns to the Dashboard route.
- Sidebar later-milestone routes: open a clear handoff screen naming the owning milestone and never mark the action complete.
- Reservation sidebar badge and Platform status dot: are visible when the dashboard data contains those conditions and open their owning handoff screens when clicked through navigation.
- Search input: searches Dashboard products, reservations, orders, alerts, and SKUs; selecting a result opens an in-page detail panel or handoff when the owning module is later.
- Keyboard shortcut hint: focuses the search input.
- Sync status pill: opens sync details; retryable states expose retry.
- Appearance icon: toggles light/dark appearance locally and persists the choice.
- Notifications icon: opens current dashboard notifications and attention items.
- Account menu: opens account actions, includes Cover page and Sign out, and sign-out destroys the session.
- Upgrade link: remains an explicit billing handoff because billing belongs to Settings/Billing.
- Collapse button: collapses and expands the sidebar without losing state.

### 2.2 Dashboard first use controls

- Upload CSV: opens the existing product import control and persists imported products through the product API.
- Connect a platform buttons: open a working platform picker and start the first-sync dashboard state; real marketplace authorization remains the Platforms milestone.
- Invite email input and Invite button: use the existing invitation API. If outbound email is not configured, show a clear environment blocker and route to the Team handoff without pretending an invite was delivered.
- Empty dashboard cards and panels: show honest empty state copy and route controls to the correct handoff or active Dashboard control.

### 2.3 Dashboard first sync controls

- First sync panel: shows persisted sync progress and allows the user to keep working.
- Sync status pill: opens sync details.
- Progress stages: show matched/sent/sending state. No real marketplace authorization is claimed before the Platforms milestone.
- Waiting-for-platform reservations panel: explains why reservations are empty until the first sync completes.

### 2.1 Live Dashboard controls

- KPI cards: display persisted dashboard summary data.
- Live reservations rows: show reservation detail when selected; `View all` opens the Reservations handoff with the current reservation list context.
- Sales by platform: exposes platform percentages in accessible text.
- Needs attention `Retry`: persists retry state for the Euronics sync alert and updates the dashboard status.
- Needs attention `Adjust on hand`: opens a stock adjustment form for the related product and persists the count through the product API.
- Needs attention `Use StockHub count`: resolves the mismatch alert in persisted dashboard action state.
- Needs attention `Review`: opens a mismatch detail panel with the owning future milestone named.
- Needs attention `Restock`: opens a restock handoff/detail panel and records the product on the local reorder list.
- `+2 more low-stock products`: expands the hidden low-stock attention items.

## Backend and persistence plan

- Add Dashboard contracts for summary metrics, cards, reservations, platform sales, sync progress, attention items, notifications, and action responses.
- Add a Dashboard store boundary and PostgreSQL implementation. Dashboard state is workspace scoped and stores mutable action state, such as first sync, retried sync, resolved mismatch, expanded low-stock items, selected search/detail context, and reorder selections.
- Derive product-related dashboard values from persisted products where possible. Use deterministic dashboard seed data only for Dashboard-owned demo states needed by the approved mockups, and label any later-module action as a handoff.
- Add a migration for dashboard state and update the migration runner and project file.
- Keep Product persistence as the source for imported products and on-hand adjustments used by Dashboard controls.

## Frontend plan

- Split the existing Access/onboarding shell enough to add a dedicated Dashboard route while preserving Access behavior.
- Add `#dashboard` and make sign-in/workspace success navigate to the Dashboard when a workspace exists. Keep `#onboarding` available for the Access checklist route.
- Implement the three Dashboard states with the existing visual language and responsive behavior.
- Implement in-page panels/modals for search results, sync details, stock adjustment, review/restock handoffs, and notifications.
- Keep later-milestone routes honest with named milestone ownership and a way back to Dashboard.

## Tests and verification

Automated coverage must include:

- Backend unit/integration coverage for Dashboard snapshot authorization, first-sync action, retry/resolve/restock action persistence, and product on-hand adjustment through the Dashboard API.
- Frontend rendering tests for the three Dashboard states and visible control affordances.
- Existing Access tests proving signup, sign-in, workspace, onboarding, reset, and invite routes still render.
- Contract check, frontend build, backend build/test, and container checks before PR closure.

Browser verification must cover:

1. Signup with email, workspace creation, Dashboard first-use load, search, theme, notifications, account menu, sign-out, sign-in.
2. Product import from Dashboard first use, transition to populated dashboard data where applicable.
3. Platform picker from Dashboard first use, first-sync state, sync details.
4. Live dashboard attention actions: Retry, Adjust on hand, Use StockHub count, Review, Restock, expand low-stock, View all reservations.
5. Navigation into and out of every shell route visible in Dashboard screenshots.

## Closure and deployment

Do not close Milestone 2 until every visible Dashboard screenshot control works in the browser, required tests and CI pass, GitHub issues/docs/reports are updated with real evidence, and any external blocker remains open with a concrete owner and target milestone. Deployment must happen through GitHub Actions only after PR checks pass. Do not shut down the PC unless the user explicitly confirms after successful completion.
