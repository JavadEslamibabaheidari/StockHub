# Milestone 1 — Access

Status: start gate OPEN — previous milestone closure and Access issue persistence verified 2026-09-27

## Goal

Deliver the first usable StockHub access journey from the approved mockup:
create an account, sign in, create a workspace, and reach the onboarding
checklist. The milestone includes both backend capabilities and the React
frontend that consumes the generated API contract.

## Source material

- Product source of truth: `docs/mockups/StockHub Final.html`, section `1 Access`.
- Visual review reference: `docs/mockups/StockHub Design Review.pdf`, section
  `1 Access`.
- Roadmap: `docs/plans/mockup-led-roadmap.md`, milestone `1 Access`.
- Architecture: `docs/plans/architecture-baseline.md` and accepted ADRs
  `docs/decisions/0001-*.md` through `docs/decisions/0008-*.md`.
- Closure process: `docs/ai-development-workflow.md`.

## Scope

### 1.1 Sign up

Backend registration accepts full name, email, and password, validates the
email and minimum eight-character password, creates a global identity, starts a
secure authenticated session, and returns the next workspace step. Duplicate
emails and invalid input return stable problem-details errors without revealing
whether an account exists beyond the agreed authentication policy.

Frontend renders the mockup's Google and email entry points, full-name and
work-email fields, password show/hide control, minimum-password guidance,
validation, busy, success, and error states, and a route to sign in.

Google sign-in uses an OIDC provider adapter with environment-based
configuration and the same session and account-linking rules as email sign-up.
Missing provider configuration is a deployment/configuration failure, not a
silent success state.

Email verification, MFA, SSO, and Terms/Privacy content are deferred from this
milestone. The visible legal links remain non-submitting links to the approved
product/legal destination; they must not imply that legal acceptance has been
recorded when it has not.

### 1.2 Sign in

Backend authenticates email/password and the configured Google OIDC flow using
secure HttpOnly cookies, supports sign-out and current-session lookup, applies
generic invalid-credential responses, and records authentication failures in
the approved logging boundary without storing passwords or tokens in logs.
Locked, throttled, disabled, unverified, and no-membership states use explicit
safe responses and never disclose account existence. A signed-in user may
select among their workspaces or create another one; the active workspace is
stored in the session and every workspace-owned request carries tenant scope.

Frontend renders the mockup's Google and email entry points, work-email and
password fields, show/hide control, forgot-password route placeholder with an
explicitly stated deferred recovery workflow, sign-in busy/error/success
states, and a route back to account creation.

The visible forgot-password link opens an explicit deferred-recovery message
and does not claim that a reset email was sent.

### 1.3 Create workspace

Backend creates a workspace tenant for the authenticated user with business
name, country, currency, and optional VAT number; creates the owner membership;
enforces idempotency for retries; and returns the workspace identifier and
onboarding state. Currency is stored as an ISO code and country as an ISO
code. The owner role is assigned server-side. Duplicate business names are
allowed; the URL-safe workspace slug is generated uniquely. A client-provided
idempotency key makes retries return the original workspace instead of creating
a second one.

Frontend renders the mockup's two-step workspace flow with business name,
country, currency, optional VAT number, validation, retry-safe submission,
success navigation, and accessible error and loading states.

### 1.4 Onboarding checklist

Backend returns checklist state scoped to the current workspace and exposes
the three checklist actions: import products, connect the first platform, and
invite the team. Access milestone owns the checklist state and route guards.
The checklist records the user's selected next step, but it does not mark an
action complete until its owning capability reports completion.

Frontend renders the mockup's checklist and the three next steps. The import
entry point provides the CSV template/download, upload/drop-zone, preview, and
manual-add paths needed to express the design, while the actual product import
behavior is delivered by the Inventory milestone. Connect-platform presents
the approved platform catalog and an explicit handoff to the Platforms
milestone. Invite-team creates a durable invitation request with role policy
and an acceptance route; email delivery and full team administration remain
owned by the Team milestone. None of these handoffs may report completion
before the owning capability confirms it.

## Non-goals

- Inventory ledger, reservations, orders, platform adapters, pricing rules,
  reports, team administration, billing, or settings behavior.
- Password recovery delivery, MFA, SSO beyond the selected Google OIDC adapter,
  or production identity-provider operations.
- Production hosting, secrets, domain, rollback, and observability decisions.
- Sharing backend domain entities with TypeScript; only generated OpenAPI
  contracts cross the boundary.
- Full inventory import, live marketplace authorization/synchronization, and
  team administration beyond a secure invitation request and acceptance route.

## Deliverables

- Backend .NET solution and host with Access, Identity, workspace, and
  onboarding modules following the accepted module boundaries.
- PostgreSQL migrations and integration tests for identities, workspaces,
  memberships, and idempotent workspace creation.
- OpenAPI endpoints and generated TypeScript client committed or reproducibly
  generated in CI.
- React/Vite frontend routes, accessible components, design tokens, and Access
  states matching the mockup at the supported responsive widths.
- Separate backend and frontend CI workflows with build, lint/format, unit,
  contract, integration, and end-to-end checks appropriate to each side.
- A milestone closure report that passes
  `scripts/check-milestone-closure.sh` and a reviewed alignment matrix.

## Acceptance criteria

1. A new user can complete the email sign-up flow, is authenticated with a
   secure cookie, and is directed to workspace creation.
2. Existing users can sign in and sign out; invalid credentials and validation
   errors are safe, stable, and represented in the frontend states.
3. A signed-in user can create exactly one requested workspace on retry, become
   its Owner, and reach the onboarding checklist.
4. A workspace-scoped session cannot read or mutate another workspace's access
   or onboarding state.
5. Google sign-in is either operational in the configured environment or fails
   visibly with an actionable configuration error; it never creates an
   unauthenticated success state.
6. Password recovery, email verification, legal acceptance, platform sync,
   catalog import, and team administration have visible, tested deferred or
   handoff states and are not represented as complete.
7. The four Access screens match the approved mockup's content hierarchy,
   states, responsive reflow, keyboard access, labels, focus handling, and
   error announcements.
8. OpenAPI is the frontend/backend contract; generated client changes are
   reproducible and both pipelines validate the contract.
9. Backend and frontend tests cover happy paths, validation, duplicate or retry
   behavior, authorization, cookie/session behavior, API errors, responsive
   interaction, and the Access end-to-end journey.
10. Configuration and migrations are documented for local and review-staging
   environments; no production deployment is introduced by this milestone.
11. The closure report proves alignment with the mockup, plan, ADRs, knowledge,
    roadmap, code, tests, configuration, and GitHub tracking, including every
    deliberate deviation and follow-up.

## Contract and data sketch

The initial API surface is versioned under `/api` and represented in OpenAPI:

- `POST /api/auth/sign-up`, `POST /api/auth/sign-in`,
  `POST /api/auth/sign-out`, and `GET /api/auth/session`;
- `POST /api/auth/google/start` and `GET /api/auth/google/callback` using
  validated OIDC state and the same cookie session;
- `POST /api/workspaces`, `GET /api/workspaces`, and
  `PUT /api/workspaces/{workspaceId}/active`;
- `GET /api/workspaces/{workspaceId}/onboarding` and
  `POST /api/workspaces/{workspaceId}/onboarding/actions`;
- `POST /api/workspaces/{workspaceId}/invitations` and
  `GET /api/invitations/{token}` for the secure invitation handoff; and
- `GET /api/platforms/catalog` and
  `GET /api/workspaces/{workspaceId}/products/import-template` for the
  explicit future-capability handoffs.

The Access persistence boundary includes User, Workspace, Membership,
Invitation, OnboardingChecklistTask, and ActiveWorkspaceSession records. User
email uniqueness is normalized case-insensitively. Workspace creation and the
owner membership are atomic. Invitation tokens are hashed at rest, single-use,
time-limited, and cannot grant a role above the inviter's authority. Product
import jobs, platform credentials, and product records remain owned by their
future modules even when Access exposes their onboarding entry points.

Mutating cookie-authenticated endpoints require anti-forgery protection and
return consistent `401`, `403`, `409`, `422`, and `429` Problem Details. All
security-sensitive operations are rate-limited and auditable without logging
passwords, cookies, raw invitation tokens, or provider credentials.

## Frontend quality contract

Access must be tested at 320, 375, 768, 1024, and 1440 CSS pixels. Auth and
workspace two-column layouts stack without horizontal scrolling; onboarding
controls wrap; manual-product entry remains usable; and primary actions become
full width where needed. Every form control has a real label, field-associated
error text, `aria-invalid`/`aria-describedby` when invalid, visible focus, and
keyboard operation. Busy and server-error states use an announced live region
and do not rely on color alone. Password toggles expose their pressed state,
progress exposes its step semantics, and file upload/drop-zone controls have a
keyboard-accessible browse path.

## Task breakdown

| ID | Work | Owner boundary | Dependency | Done when |
|---|---|---|---|---|
| A1 | Restore usable checkout and verify branch/PR/CI access | delivery | GitHub credentials | branch can be created from current `main` and PR checks are observable |
| A2 | Create .NET host, Access module, Identity, workspace persistence, and migrations | backend | accepted ADRs; A1 | backend builds and migrations apply to PostgreSQL |
| A3 | Define Access OpenAPI surface and generated TypeScript client | contract | A2 | spec and client are reproducible and checked in or generated in CI |
| A4 | Create React/Vite shell, routes, tokens, and Access screens | frontend | A3 | four screens render with all specified states and responsive behavior |
| A5 | Add backend unit, API, PostgreSQL, authorization, and idempotency tests | backend quality | A2 | required backend checks pass in CI |
| A6 | Add frontend unit, accessibility, contract, and Playwright tests | frontend quality | A4 | required frontend checks pass in CI |
| A7 | Split CI and update required branch checks | delivery | A2–A6 | repository, backend, and frontend checks are all required and green |
| A8 | Run planned-state validation and closure hook | quality gate | A1–A7 | closure report passes script and review |

## Dependencies and risks

- The current workspace has no usable Git metadata, so A1 is blocking.
- GitHub CLI authentication is invalid and network access is unavailable in the
  current execution environment, so remote milestone, issue, project, branch
  protection, CI, and PR state cannot yet be independently verified.
- Google OIDC requires provider credentials and callback configuration. Local
  tests use a deterministic test adapter; review-staging must provide approved
  configuration before the Google acceptance criterion is marked complete.
- PostgreSQL and browser-test dependencies must be available in CI before
  implementation is considered complete.
- The mockup includes future-module actions in the onboarding checklist. Their
  Access behavior must remain honest and explicitly deferred until those
  milestones implement the actions.

## Testing strategy

- Backend unit tests for validation, role assignment, checklist transitions,
  and problem-details mapping.
- API tests for cookies, sign-up/sign-in/sign-out, current session, workspace
  creation, duplicate/retry behavior, and tenant authorization.
- PostgreSQL integration tests for Identity, workspace, membership, and
  idempotency constraints using an isolated database.
- Contract tests that compare the OpenAPI document and generated TypeScript
  client.
- Frontend component and accessibility tests for every form and state.
- Playwright tests for sign-up → workspace → checklist and sign-in → checklist,
  including validation and visible server errors.

## GitHub tracking

GitHub remains the source of truth. The persisted issue set is verified under
milestone `1 Access`: specification issues #5–#8 and implementation issues
#20–#27. Issue #9 remains the repository-wide CI prerequisite and is explicitly
linked by A7.

Verified milestone: https://github.com/JavadEslamibabaheidari/StockHub/milestone/1
Verified previous closure: https://github.com/JavadEslamibabaheidari/StockHub/issues/17
Verified Cover PR: https://github.com/JavadEslamibabaheidari/StockHub/pull/18

| Order | Issue | URL | Depends on |
|---|---|---|---|
| 1 | A1 Prepare Access implementation baseline | https://github.com/JavadEslamibabaheidari/StockHub/issues/20 | Cover closure #17 |
| 2 | A2 Implement Access backend identity, sessions, and workspace persistence | https://github.com/JavadEslamibabaheidari/StockHub/issues/21 | A1; specs #5–#7 |
| 3 | A3 Publish Access OpenAPI contract and generated TypeScript client | https://github.com/JavadEslamibabaheidari/StockHub/issues/22 | A2 |
| 4 | A4 Build Access React routes and responsive screens | https://github.com/JavadEslamibabaheidari/StockHub/issues/23 | A3 |
| 5 | A5 Add Access backend security and integration quality gates | https://github.com/JavadEslamibabaheidari/StockHub/issues/24 | A2–A3 |
| 6 | A6 Add Access frontend contract, accessibility, and end-to-end tests | https://github.com/JavadEslamibabaheidari/StockHub/issues/25 | A3–A4 |
| 7 | A7 Split and enforce Access backend/frontend CI checks | https://github.com/JavadEslamibabaheidari/StockHub/issues/26 | A2–A6; #9 |
| 8 | A8 Validate and close Milestone 1 Access | https://github.com/JavadEslamibabaheidari/StockHub/issues/27 | A1–A7; specs #5–#8 |

## Knowledge updates

After implementation, update `docs/knowledge/` with the Access module boundary,
identity/session model, workspace and membership tables, API contract location,
frontend route conventions, test commands, and any accepted deviations. Remove
stale planned-only statements rather than appending contradictory notes.

## Start-gate verdict

The gate is OPEN. The previous-milestone closure hook passed: Cover report
`docs/reports/milestone-0-cover-closure.md` is `status: PASS` on merged PR #18,
Cover milestone 0 is closed, and its three issues are closed. The Access plan
has no unresolved implementation-blocking decisions, the accepted ADRs and
knowledge agree with the plan, and the twelve Access issues are persisted under
milestone 1 with verified immutable URLs. Implementation begins with A1 in the
isolated Access worktree and proceeds one issue/branch/PR at a time.
