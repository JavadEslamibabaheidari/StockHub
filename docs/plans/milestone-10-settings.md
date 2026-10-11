# Milestone 10 Settings

Status: delivery merged and deployed; GitHub closure is pending. Milestone #11
`10 Settings` and its issues #144 (Billing), #145 (Appearance), and #146
(integration and closure) remain open. The implementation merged to `main` in
PR #147 and the dark-card contrast fix merged in PR #148; this plan records the
verified delivery evidence.

## Goal

Deliver the `10.1 Settings · Billing` and `10.2 Settings · Appearance` screens
from `docs/mockups/StockHub Final.html` and page 1 of
`docs/mockups/StockHub Design Review.pdf`, preserving the existing workspace
shell, account behavior, routes, and accessibility conventions.

## Mockup-backed scope

### Billing

- Show current plan, renewal cadence/date, product/platform/team usage, and
  plan comparison for Starter, Pro, and Business.
- Show payment method and expiry, billing email, company, and VAT number, with
  the mockup's update, edit billing details, cancel plan, and plan selection
  actions.
- Use only values returned by an established backend/provider integration.
  Existing local evidence shows workspace business name, country, currency, and
  VAT number in Access persistence, but no subscription, payment method, billing
  contact, or payment-provider contract. Never substitute mockup sample values
  such as a card suffix, renewal date, or plan usage for live account values.
- Do not add a payment processor or claim that a plan/payment action succeeded
  unless that integration and its acceptance criteria are confirmed in the
  authoritative milestone issues. If the existing integration has no value or
  action configured, present an explicit unavailable state and prevent false
  success.

### Appearance

- Offer Light, Dark, and System choices. System follows the device preference.
- The design specifies that the preference applies to this browser, so
  persistence is browser-local, not a server-side account setting.
- Apply the preference consistently across implemented routes, including when
  the OS preference changes while System is selected and when the app reloads.
- Keep status colors distinguishable in both themes and pair color with an icon
  or text label. Preserve keyboard operation, visible focus, and contrast.

## Acceptance criteria

Detailed acceptance criteria and task ownership are tracked in GitHub issues
[#144](https://github.com/JavadEslamibabaheidari/StockHub/issues/144),
[#145](https://github.com/JavadEslamibabaheidari/StockHub/issues/145), and
[#146](https://github.com/JavadEslamibabaheidari/StockHub/issues/146).

1. The `#settings` route opens Settings with Billing and Appearance navigation;
   all existing shell links to Settings reach the correct page, and existing
   routes remain functional.
2. Billing displays only backend/provider-backed current-plan, renewal, usage,
   payment, and billing-identity values. Missing values have clear unavailable
   states; request failures have a recoverable error state.
3. Billing edits validate user input and persist through an authorized API.
   Plan change, payment update, and cancellation actions require confirmation
   when they cause a material account change and show success only after the
   responsible backend/provider confirms it. Unsupported operations are clearly
   unavailable and cannot appear successful.
4. Billing authorization and tenant boundaries are enforced server-side.
   Protected payment data is not exposed to unauthorized workspace roles.
5. Appearance offers Light, Dark, and System. Selection is saved in
   browser-local storage; System resolves from `prefers-color-scheme` and reacts
   to changes. Stored invalid values fall back safely. The chosen theme is
   applied before the main UI paints where the app entrypoint permits it.
6. Appearance works on every implemented route without losing contrast,
   visible keyboard focus, status icons/labels, or readable disabled/error
   states. The settings layout and actions remain usable at desktop and narrow
   viewport widths.
7. Automated coverage exercises billing loading, unavailable, error, validation,
   authorization, and action outcomes; theme selection, persistence, System
   changes, reload initialization, keyboard access, and route coverage.
8. Required frontend, backend/PostgreSQL, generated contract, container/Compose,
   security, CI, and deployment gates pass where affected. Any unavailable
   hosted target is reported as unverified.

## Scope decisions

- Keep the existing modular-monolith and module ownership boundaries. Billing
  data belongs to an existing owning domain or an explicitly approved provider
  integration; Settings must not query another module's tables directly.
- Do not introduce a new deployable application or service. Preserve the
  existing production Dockerfile, `.dockerignore`, Compose health/readiness,
  and CI image smoke gates. A new persistent API contract or migration must use
  existing contract and migration conventions.
- Appearance is a browser-local preference per the mockup wording. Do not add
  account-wide synchronization or a persistence migration.
- Do not claim payment-provider, subscription-management, or hosted billing
  capabilities that are absent from the inspected code. Billing provider and
  mutation behavior are out of scope in issue #144.

## Issue breakdown

1. **Deliver Billing settings** ([#144](https://github.com/JavadEslamibabaheidari/StockHub/issues/144)).
   Use real session-backed identity values and honest unavailable states for
   unsupported subscription/payment features; do not add a provider or billing
   API.
2. **Deliver Appearance settings** ([#145](https://github.com/JavadEslamibabaheidari/StockHub/issues/145)).
   Consolidate theme state and initialization; add browser-local
   Light/Dark/System preference controls and cross-route behavior.
3. **Integrate and close Settings** ([#146](https://github.com/JavadEslamibabaheidari/StockHub/issues/146)).
   Review responsive and accessible behavior, regress earlier routes, run
   required CI and deployment gates, update knowledge/status/report evidence,
   and close only when live GitHub tracking satisfies repository policy.

## Implemented areas

- `frontend/src/App.tsx`: `#settings` routes to the Settings screen.
- `frontend/src/AccessScreens.tsx`: shared navigation and account/workspace
  context currently link to Settings.
- `frontend/src/{DashboardScreens,InventoryScreens,OrdersScreens,PlatformsScreens,ReservationsScreens,PricingScreens,ReportsScreens,TeamScreens}.tsx`:
  duplicated shell navigation and account/upgrade handoffs point to Settings;
  update these to the implemented screens without breaking existing behavior.
- `frontend/src/styles.css`: Settings layout, appearance controls, and
  responsive styles use existing shell and theme selectors.
- `frontend/src/api/generated.ts`, `contracts/`, and `backend/src/StockHub.Api/`
  only if authoritative issue scope requires a server-backed billing contract.
  Local search found workspace business identity fields but no billing,
  subscription, payment-method, or provider API/model.
- Active-session data supplies workspace business name, country, currency, and
  signed-in account email. The session lacks VAT detail, so Billing marks it
  unavailable and labels account email separately from billing contact.

## Dependencies and risks

- PRs #147 and #148 merged; the corrected image is deployed and verified.
  Issues #144–#146 and milestone #11 remain open pending GitHub closure.
- PR #147 merged feature source `68af3ffc43a17780b8cdf0651fd53a312e95c648`
  to `main` at `3c4b655c4017f78b686bd6cdbf2594e1104e7c0f`. PR #148 merged
  contrast-fix source `2a961a415e0f3e3b763a1499ce0f06fb12530418` to `main` at
  `e280c0ada8464a82ca25ef8f4a77fd3df2896988`. Live `dev` is
  `21549b36342cb031dc94bc79b28ee502be5dbf9c`; M10 is not integrated there.
- No billing provider, subscription API, payment-method model, or billing
  mutation is present in the code. Those features remain unavailable by design.

## Verification and deployment strategy

- Add focused unit/component tests for the approved UI states and persistence
  behavior, plus backend/PostgreSQL integration tests for any new API,
  authorization, tenant isolation, and durable writes.
- Run generated-contract checks, frontend tests/build, backend tests, Compose
  health and route smoke, repository/tracking checks, vulnerability checks, and
  CI-required Kubernetes migration/rollout smoke as applicable.
- Main delivery run #38175305112 passed for merge SHA
  `e280c0ada8464a82ca25ef8f4a77fd3df2896988` and published immutable image
  digest `sha256:51d2f10d05b3da9919e280c6c8b88db65aedc81b0cb319086edc57bb8e99e640`.
  Persistent deployment run #38175592124 succeeded; the local target records
  this source/digest, its migration completed, rollout reached 1/1 available,
  and `/ready` returned HTTP 200. Review-staging and production are unverified.
  Record source branch/SHA, whether code is integrated into `dev` or `main`,
  image digest publication, local deployment health, and shared hosted
  deployment health as separate facts. A skipped hosted deployment job is
  unverified.
- For closure, update the Settings plan, `docs/project-status.md`, relevant
  knowledge files, and a PASS closure report; run
  `scripts/check-tracking-status.sh` and the milestone closure hook. GitHub
  milestone closure requires the live milestone to be closed with zero open
  issues and the report to pass.

## Prior milestone gate

The M9 report at `docs/reports/milestone-9-team-closure.md` records PASS and
identifies GitHub milestone #10 as closed with zero open issues. M10 closure
evidence is based on merged `main` source
`e280c0ada8464a82ca25ef8f4a77fd3df2896988`; live GitHub reports `dev` at
`21549b36342cb031dc94bc79b28ee502be5dbf9c`. Earlier milestone reports remain
historical evidence.

## Local implementation progress

- Added the Settings route with Billing and Appearance sections and the existing
  workspace navigation/switcher.
- Billing uses only active-session workspace identity and account email. Plan,
  usage, VAT detail, payment method, plan comparison, and billing mutations are
  explicitly unavailable because no corresponding API is present in this
  checkout; no mockup sample values are presented as account data.
- Appearance supports browser-local Light, Dark, and System preferences,
  including OS preference changes, migration from the old light/dark key, and
  the existing shell appearance toggles across implemented routes.
- PR #148 and main run #38175305112 passed frontend tests/build,
  backend/PostgreSQL tests, Compose smoke, image scan/security gate, and
  ephemeral Kubernetes migration/rollout smoke. PR closure evidence,
  milestone comparison, repository sanity, and Trivy also passed.
- Main run #38175305112 published digest
  `ghcr.io/javadeslamibabaheidari/stockhub@sha256:51d2f10d05b3da9919e280c6c8b88db65aedc81b0cb319086edc57bb8e99e640`
  for source `e280c0ada8464a82ca25ef8f4a77fd3df2896988`. Persistent
  deployment run #38175592124 and the local target record confirm this source
  and digest, completed migration, 1/1 available rollout, a 1/1 Ready pod, and
  `/ready` HTTP 200.
- Browser review at 1280×900 and 390×844 checked Billing, Appearance,
  keyboard focus/selection, no horizontal overflow, and dark preference across
  nine implemented routes. PR #148 corrected the discovered dark-card contrast
  issue; the corrected dark Appearance and Billing screens were reviewed.
- GitHub milestone #11 remains open with three open issues (#144–#146).
  Review-staging and production remain unverified; their jobs were skipped.
