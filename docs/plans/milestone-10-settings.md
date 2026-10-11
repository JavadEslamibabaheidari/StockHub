# Milestone 10 Settings

Status: implementation in progress. On 2026-10-11, the GitHub API and
authenticated issue page confirmed milestone #11 `10 Settings` is open. Its
initial zero-issue state was not treated as acceptance criteria. Issues #144
(Billing), #145 (Appearance), and #146 (integration and closure) now define the
tracked scope and acceptance criteria; this plan is aligned with those records.

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
- The design specifies that the preference applies to the account in this
  browser, so the candidate persistence boundary is browser-local, not a new
  server-side account setting.
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
  capabilities that are absent from the inspected code. The exact billing
  provider and billing mutation boundary must be checked against the live issue
  bodies before implementation is approved.

## Candidate task breakdown

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

## Affected areas identified locally

- `frontend/src/App.tsx`: `#settings` currently falls through to the generic
  deferred route; add the approved Settings route here.
- `frontend/src/AccessScreens.tsx`: shared navigation and account/workspace
  context currently link to Settings.
- `frontend/src/{DashboardScreens,InventoryScreens,OrdersScreens,PlatformsScreens,ReservationsScreens,PricingScreens,ReportsScreens,TeamScreens}.tsx`:
  duplicated shell navigation and account/upgrade handoffs point to Settings;
  update these to the implemented screens without breaking existing behavior.
- `frontend/src/styles.css`: theme styling exists as scattered
  `[data-theme='dark']` rules. Centralize initialization and preference state
  without changing unrelated visual behavior.
- `frontend/src/api/generated.ts`, `contracts/`, and `backend/src/StockHub.Api/`
  only if authoritative issue scope requires a server-backed billing contract.
  Local search found workspace business identity fields but no billing,
  subscription, payment-method, or provider API/model.
- Existing Access persistence includes business name, country, currency, and VAT
  number; these are candidate billing identity fields, subject to the confirmed
  contract and role policy.

## Dependencies and risks

- GitHub milestone #11 is verified open and now has three open issues (#144,
  #145, and #146). Their bodies define the current implementation scope. Keep
  the plan aligned with these issues as implementation details change.
- The GitHub API check succeeded, but refreshing local `main`/`dev` remains
  unavailable because the shared Git metadata is read-only for fetches. The
  current checkout is detached at `5897a55`.
- Confirm whether a payment/subscription provider is already configured and
  what data/actions the milestone requires. The local source inspected here
  does not contain that integration.
- Confirm which workspace roles may view/edit billing identity and manage plan
  or payment actions; the mockup does not specify these permissions.
- Cross-route theme support currently uses several independently maintained
  shells and theme toggle implementations, increasing regression risk.

## Verification and deployment strategy

- Add focused unit/component tests for the approved UI states and persistence
  behavior, plus backend/PostgreSQL integration tests for any new API,
  authorization, tenant isolation, and durable writes.
- Run generated-contract checks, frontend tests/build, backend tests, Compose
  health and route smoke, repository/tracking checks, vulnerability checks, and
  CI-required Kubernetes migration/rollout smoke as applicable.
- Inspect individual delivery jobs. Record source branch/SHA, whether code is
  integrated into `dev` or `main`, image digest publication, local deployment
  health, and shared hosted deployment health as separate facts. A skipped
  hosted deployment job is unverified.
- For closure, update the Settings plan, `docs/project-status.md`, relevant
  knowledge files, and a PASS closure report; run
  `scripts/check-tracking-status.sh` and the milestone closure hook. GitHub
  milestone closure requires the live milestone to be closed with zero open
  issues and the report to pass.

## Prior milestone gate

The local M9 report at `docs/reports/milestone-9-team-closure.md` records PASS,
identifies GitHub milestone #10 as closed with zero open issues, and the local
closure hook passed during this discovery. These are repository-local evidence;
current GitHub state and refreshed `main`/`dev` remain unverified here.

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
- Installed the locked frontend dependencies with `npm ci`; npm is configured
  for `https://registry.npmjs.org/` and installation completed without an auth
  error. The production frontend build passes.
- Frontend tests pass (38 tests), including focused appearance preference and
  Settings unavailable-state coverage. Access and Reports contract checks pass.
- `git diff --check` passes. Branch creation is blocked because the shared Git
  metadata is read-only; the complete local worktree diff remains reviewable.
- GitHub milestone #11 remains open with three open issues (#144–#146). The
  implementation is tracked in those issues; none is closed yet. The checkout
  is detached at `5897a55`; fetch remains unavailable because shared Git
  metadata is read-only. Publication and deployment have not been verified.
