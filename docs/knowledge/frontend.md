# StockHub Frontend Knowledge

The frontend uses React, TypeScript, and Vite. Generated OpenAPI contracts will
be the only backend/frontend boundary once API work begins; domain models are
not shared between C# and TypeScript.

## Milestone 0 Cover

The current implementation entry point is `frontend/src/main.tsx`, rendering
`frontend/src/App.tsx`. The Cover is a static product surface with no backend
dependency. It establishes the warm neutral background, serif brand/hero
typography, accent/sage brand marks, stock-model cards, status pills, and
visual language used by later screens. The roadmap Contents panel was removed
after user review.

Verification commands:

```bash
cd frontend
npm ci
npm test
npm run build
```

The Cover is responsive from 320px through 1440px, uses visible focus states, and communicates status with text plus
icons/dots rather than colour alone.

Milestone 0 Cover closure is verified by merged PR #18 and report
`docs/reports/milestone-0-cover-closure.md`. Access frontend work consumes only
generated OpenAPI TypeScript clients and is tracked by issues #22, #23, #25,
and #26 under milestone 1.

The committed contract is `contracts/access.openapi.json`; the generated
TypeScript boundary is `frontend/src/api/generated.ts`. Run
`cd frontend && npm run contract:check` to verify required operations and
generated-client alignment before frontend changes are delivered.

Frontend CI runs the locked install, tests, generated-client contract check,
and production build in `.github/workflows/frontend-ci.yml`.

Access routes are hash-addressable in the current shell (`#signup`, `#signin`,
`#workspace`, `#onboarding`, `#invite`, and deferred handoffs). Forms use the
generated client, expose labelled controls and live alert errors, and keep
future Inventory, Platforms, and Team handoffs explicitly deferred. Password
recovery now has request and reset screens backed by the access API. Milestone 1
has been reopened; see `docs/plans/milestone-1-access-remediation.md` for its
current acceptance gate.


## Milestone 2 Dashboard

Dashboard now has a dedicated `#dashboard` route implemented in `frontend/src/DashboardScreens.tsx`. Sign-in and workspace creation route to Dashboard when a workspace exists, while `#onboarding` remains available for the Access checklist. The Dashboard screens cover first use, first sync, and live operations. Visible controls either work inside Dashboard or open honest future-milestone handoffs. The frontend consumes Dashboard methods added to `frontend/src/api/generated.ts`; the Access contract check still guards the Access operations.

Local verification for the Dashboard branch used the Docker Compose app on port 18082 and browser-tested sign-in, first-use controls, first sync, product import, live dashboard actions, later-milestone handoff navigation, and sign-out.

## Milestone 3 Inventory

Inventory routes are hash-addressable as `#inventory`, `#inventory-empty`,
`#inventory-staff`, `#inventory-detail`, and `#inventory-detail-failing`.
`frontend/src/InventoryScreens.tsx` renders the approved Inventory list, empty
state, Warehouse staff view, and product detail. The screens use the generated
API client and show backend loading or empty states when a session/workspace has
not provided persisted products; they should not fall back to seeded preview
products.

Inventory is integrated with existing Dashboard/product import work. The API
uses workspace products for real data, supports on-hand adjustment through the
product persistence path, and keeps CSV import hardening, marketplace repair,
and pricing-rule automation as explicit future handoffs.

## Milestone 4 Orders

Orders routes are hash-addressable as `#orders`, `#order-detail`,
`#orders-empty`, `#orders-no-results`, `#order-return`, `#order-cancel`, and
`#orders-staff`. `frontend/src/OrdersScreens.tsx` renders the Orders list,
order detail, empty state, no-results state, return workflow, cancel
confirmation, and Warehouse staff view from the backend Orders API.

Visible controls are wired to backend behavior where the closed milestone needs
it: filtering/search/CSV export are client-side over persisted rows, and return,
cancel, pick, ship, receive, and refund actions call the Orders API. Real
marketplace order capture, payment settlement, carrier label creation, and live
platform authorization remain future integration work.

## Milestone 5 Reservations

Reservations are hash-addressable as `#reservations`.
`frontend/src/ReservationsScreens.tsx` renders the single `5.1 Reservations`
owner screen from deterministic preview data, matching the mockup-led active
holds table, expiring timer bars, summary metrics, and expired-today lost-sale
signals from the 2026-10-10 screenshot.

Visible controls are wired for browser behavior: global search filters active
holds, CSV export downloads visible reservations, timer refresh advances the
preview countdowns, release returns held units to Available in local state,
restore hold moves an expired lost-sale signal back into active reservations,
and shell controls open status/account/workspace panels, toggle appearance, or
collapse the sidebar. Real marketplace reservation release, connector repair,
payment conversion, and durable Reservations persistence remain future backend
or integration work.

## Milestone 6 Platforms

Platforms are hash-addressable as `#platforms`.
`frontend/src/PlatformsScreens.tsx` renders the `6.1 Platforms` screen and
`6.2 Add platform picker` modal from deterministic preview connector state.
The screen covers Amazon, Unieuro, Euronics, eBay, oversell protection, an
Euronics expired-token state, and add-platform entry points for Zalando,
ePRICE, and MediaWorld.

Visible controls are wired for browser behavior: global search filters
platform cards, oversell protection toggles local messaging, pause/resume sync
changes connector state, disconnect marks a connector disconnected, reconnect
clears the Euronics error, settings/status/account/workspace/notification
controls open panels, theme toggles appearance, sidebar collapse changes the
shell, and the add-platform modal supports Connect, Done, close X, Escape, and
backdrop close. Real marketplace OAuth, credential persistence, sync workers,
stock/price publication, and product listing selection remain future durable
Platforms integration work.
