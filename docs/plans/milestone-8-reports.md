# Milestone 8 Reports

Status: implementation in progress on isolated local branch `codex/milestone-8-reports`, based on main
`539c412345a6a78211557c8624ad0ea7c4df26ea`. GitHub's authenticated in-app
session confirmed milestone `8 Reports` (GitHub milestone number 9) is open;
planning issue #126 is closed after contract freeze, while implementation and
integration issues #127–#131 remain open. Maintenance issue #132
tracks real outbound delivery. Milestones 0–7 were confirmed closed in the same
GitHub session. Refreshed main is `539c412345a6a78211557c8624ad0ea7c4df26ea`
and dev remains `21549b36342cb031dc94bc79b28ee502be5dbf9c`. The CLI token is
invalid; the in-app GitHub browser is authenticated.

## Goal and source material

Implement the Reports screens in the mockup-led roadmap:

- `8.1 Reports · Daily digest`
- `8.2 Reports · Weekly summary`
- `8.3 Reports · Alert settings`

The visual reference is the Reports row in page 1 of
[`StockHub Design Review.pdf`](../mockups/StockHub%20Design%20Review.pdf).
The interaction and content source is the Reports section of
[`StockHub Final.html`](../mockups/StockHub%20Final.html), including the
`repTabs`, `repDaily`, `repWeekly`, `repAlerts`, and `alertRows` definitions.
No standalone Reports screenshot file was found in the project resources; the
design review PDF contains the three Reports screen captures.

The GitHub UI showed milestones 0–7 closed. Re-run the previous-milestone hook
against the current main checkout before closure. The user's existing checkout
is an older milestone-4 branch with uncommitted work; it is preserved and is
not the milestone-8 implementation base.

## User-visible behavior

- The Daily digest shows revenue, order and unit counts, expired
  reservation/lost-sale signals and estimated value, sales by platform, top 5
  products by units sold, bottom 5 products by 30-day unit sales, and restock suggestions.
- The Weekly summary shows revenue and week-over-week change, order count and
  average order value, expired reservation/lost-sale signals and estimated
  value, revenue per day, platform totals and changes, product rankings, and
  restock suggestions.
- The Alert settings screen shows the four alert cards in the mockup, with an
  enable switch, threshold controls (low stock ≤5 units, out-of-stock fixed
  at 0, sync failure at 5 minutes, overselling risk at 3 available units),
  delivery channels, and frequency (`Instantly`, `Hourly summary`, or `In the daily digest`).
- The Daily digest and Weekly summary `Download PDF` buttons generate and
  download a readable PDF for the active report, using the displayed period
  and figures. They must not be inert or merely show a success toast.
- Each of the five Daily digest `Add to reorder list` buttons adds that product
  to the workspace's reorder handoff, then shows the saved state; repeated
  clicks remove it if the mockup interaction is a toggle. Reload and workspace
  switching must show the correct workspace's selections.
- Daily reports cover the most recent complete UTC calendar day; weekly reports
  cover the most recent complete Monday–Sunday ISO week in UTC. Each view shows
  its inclusive start and exclusive end. Workspace currency is used; existing
  order values are not converted or combined across currencies.
- Revenue includes paid, picked, shipped, delivered, and non-refunded returns;
  it excludes awaiting-payment, cancelled, and refunded orders. A reservation
  still awaiting payment after ten minutes is shown as expired/lost-sale data.
  Do not count it as revenue.
- Restock suggestions use durable order-line unit sales over the trailing 14
  days and current available inventory (on-hand less reserved); the suggested
  quantity targets 14 days of coverage, clamped at zero available units. The
  top 5 ranks by units sold in the report period; the bottom 5 ranks by fewest
  units sold over the trailing 30 days. Label this as a planning estimate (no supplier lead-time or
  purchase-order data exists yet).
- Report tabs switch the active view and preserve a meaningful selected state.
  Saving alert changes persists them for the active workspace and reports
  validation or request errors. The button is disabled only while settings are
  clean or a save is in progress.
- Shared header, sidebar, theme, notifications, workspace/account, search, and
  sync controls retain the actual working shell behavior from the preceding
  milestones. Reports navigation must work from every implemented screen.
- Any external delivery destination not integrated by this milestone must be
  described honestly in the UI. If the mockup requires a delivery action that
  cannot work in the existing environment, create a Maintenance follow-up with
  an owner, acceptance criteria, and reason; do not leave an untracked dead
  control.

## Proposed issue decomposition

GitHub issues created and assigned to the existing `8 Reports` milestone
(#9):

1. [#126 Plan and freeze milestone 8 Reports data contract](https://github.com/JavadEslamibabaheidari/StockHub/issues/126)
   - Define report periods and boundaries. No workspace timezone exists in the
     current contract, so use UTC day boundaries and Monday–Sunday ISO weeks
     until timezone becomes a workspace setting; label displayed periods.
   - Define currency aggregation (use the currency represented by stored
     orders; never silently combine unlike currencies), revenue and canceled/
     returned order treatment, expired-reservation treatment, and
     empty/loading/error behavior.
   - Define workspace-scoped Daily and Weekly read contracts and alert-settings
     read/update contracts, role permissions, validation, and error responses.
   - Reuse Orders and Inventory data through their owning module interfaces.
     Do not query another module's tables directly. Reservation rows and
     expiry events are only partially represented today; define what can be
     derived honestly from persisted order statuses and show an empty or
     unavailable state where no durable source exists. Never fabricate the
     screenshot's sample values as customer data.
   - Link the PDF and HTML mockup sources above and record the approved contract
     in the milestone plan before backend/UI implementation starts.

2. [#127 Build workspace-scoped Daily and Weekly Reports API](https://github.com/JavadEslamibabaheidari/StockHub/issues/127)
   - Implement Daily and Weekly report reads using the frozen contract and
     existing Orders/Inventory module boundaries.
   - Implement the five product-specific reorder actions with workspace scope,
     durable state, and an honest saved/removed result.
   - Cover calculation boundaries, UTC periods, empty data, tenant isolation,
     persistence/reload, authorization, and request failures.

3. [#128 Build Daily digest and Weekly summary screens with working PDF export](https://github.com/JavadEslamibabaheidari/StockHub/issues/128)
   - Match both mockup screens at desktop and narrow widths and use real report
     API values with loading, empty, and error states.
   - Make both report tabs and every shared shell control functional.
   - Download a valid PDF containing the active period, summary values, and
     displayed report tables; verify the file content and browser behavior.
   - Make every restock action update its product's reorder state, including
     after reload, and give users a clear success or request error.

4. [#129 Persist workspace-scoped Reports alert settings](https://github.com/JavadEslamibabaheidari/StockHub/issues/129)
   - Persist alert enabled state, trigger thresholds, channels, and frequency
     per workspace; validate supported values and enforce existing membership
     and role rules on every request.
   - Define unavailable email/Slack dispatch honestly; settings persistence
     must not imply a message was delivered.
   - Cover persistence/reload, workspace isolation, authorization, validation,
     and request failures.

5. [#130 Build the Reports Alert settings screen](https://github.com/JavadEslamibabaheidari/StockHub/issues/130)
   - Match all four alert cards, enable switches, thresholds, channels, and
     frequency controls in the mockup.
   - Load/save workspace settings; show dirty/saved/saving/error states and
     retain values across reload and workspace switching.
   - Ensure toggling an alert controls whether its configuration fields are
     active and that invalid thresholds are rejected clearly.

6. [#131 Integrate Reports, verify milestone regressions, and record closure evidence](https://github.com/JavadEslamibabaheidari/StockHub/issues/131)
   - Review and integrate all work; resolve shared contract or UI conflicts.
   - Verify all visible Reports and shared-shell controls, responsive behavior,
     supported roles/workspaces, and routes from milestones 0–7.
   - Run frontend/backend/contract/container/CI checks required by the project,
     inspect every PR and delivery job, refresh main/dev, and record source SHA,
     publication digest, and deployment status separately.
   - Close this issue only after all implementation issues are closed by merged
     PRs, required checks pass, no blocker is untracked, and the closure report
     passes `scripts/check-milestone-closure.sh`. Close the GitHub milestone
     only when it has zero open issues and the report says `PASS`.

## Dependencies and parallel execution

- Issue 1 is a hard start gate for implementation and freezes the contracts.
- Once issue 1 is merged, issue 2 (report/reorder backend), issue 3
  (Daily/Weekly UI), and issue 4 (alert-settings backend) can proceed in
  parallel if the contract and generated clients are stable.
- Issue 5 (alert settings UI) can proceed with issue 4 after the contract is
  frozen. Integrate via focused PRs and keep file ownership disjoint.
- Issue 6 depends on all implementation PRs and is coordinator-owned.

## Approved API contract

`contracts/reports.openapi.json` freezes the routes and response types. Read
Daily and Weekly values from the Orders/Inventory module interfaces; keep
cross-module access behind those interfaces. Order lines must be aggregated at
the owning Orders boundary rather than flattening an order's items in the UI.

- `GET /api/workspaces/{workspaceId}/reports/daily` returns the most recent
  complete UTC day.
- `GET /api/workspaces/{workspaceId}/reports/weekly` returns the prior
  complete Monday–Sunday ISO week in UTC.
- `GET /api/workspaces/{workspaceId}/reports/reorder-list` reads the current
  workspace's product IDs; `PUT .../reorder-list/{productId}` sets the
  idempotent `{ listed: boolean }` state.
- `GET/PUT /api/workspaces/{workspaceId}/reports/alert-settings` reads or
  writes four keyed alert rules, each with enabled state and mockup defaults (low stock 5 units, out-of-stock
  fixed at zero, sync failure 5 minutes, overselling risk 3 available units),
  Email/In-app/Slack channel booleans, and instant/hourly/daily frequency.
  Reads require membership; writes follow Owner/Admin/Manager edit permissions.
- Stored alert preferences are configuration only. Delivery status remains
  `notConfigured` until a dispatcher and channel adapters are implemented.

## Verification and closure gate

- Previous milestone hook: run
  `scripts/check-milestone-closure.sh` against each previous milestone report;
  verify the latest GitHub milestones and open PR state before claiming they
  still work. At minimum exercise Access, Dashboard, Inventory, Orders,
  Reservations, Platforms, and Pricing routes and their project-required
  frontend/backend regression checks.
- Reports: contract/client checks; backend domain, persistence, tenant, role,
  and endpoint tests; frontend route and interaction tests; production build;
  browser smoke for all three screens at desktop and narrow widths; inspect
  each downloaded PDF; reload and workspace-switch persistence checks.
- Run the project-required Compose/container health and delivery checks. This
  adds functionality to the existing deployable application, so it does not
  create a new containerized project; preserve existing Docker/Compose health
  and CI smoke coverage.
- Before closure: refresh `main` and `dev` when accessible; inspect all PRs and
  required workflow jobs; run `scripts/check-tracking-status.sh`; update
  `docs/project-status.md`, roadmap, knowledge docs, and the closure report in
  the same delivery; require GitHub milestone number 9 to be closed with zero
  open issues and a PASS report. Hosted environments are verified only if
  their deployment records name the source SHA/digest and healthy endpoint.

## Current external blockers

- `git fetch origin main dev` could not refresh refs because `.git/FETCH_HEAD`
  is read-only in this workspace. Existing local refs show `origin/main` at
  `7fb9350` and `origin/dev` at `21549b3`.
- `gh auth status` reports the saved GitHub token is invalid and API calls fail
  with a connectivity error. The GitHub in-app browser session is authenticated
  and currently usable for issue/milestone review and issue creation. Repository
  refs cannot be refreshed because `.git/FETCH_HEAD` is read-only.
- Email and Slack delivery integrations are not present in the current
  application. Persisting alert settings is in scope; actual outbound delivery
  must be explicitly scoped as a Maintenance follow-up if it cannot be added
  safely with existing infrastructure.

## Maintenance follow-up

[Issue #132](https://github.com/JavadEslamibabaheidari/StockHub/issues/132), in
the existing `unparentted milestone` (#14), tracks actual scheduled Email,
In-app, and Slack alert delivery, retries/deduplication, and operational
evidence. Reports implements truthful preference persistence and makes the
delivery limitation visible; it does not claim to send messages.
