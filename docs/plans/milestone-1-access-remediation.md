# Milestone 1 Access remediation

Status: open. Milestone 1 and issues #27 and #35 were reopened after live testing
showed incomplete controls. The prior closure report is historical and does not
establish current acceptance.

## Goal

Make the approved signup, sign-in, workspace, and onboarding screens usable as
real product flows. Remove the roadmap Contents panel from Cover. Keep the
free-trial billing action as the only permitted deferred access action.

## Required behavior

- Email signup accepts personal and work addresses, creates exactly one user
  and session, and reports duplicate-account and recoverable failures clearly.
- Email sign-in, sign-out, workspace creation, and page navigation complete in
  the browser and survive reloads.
- Google sign-in uses a configured OAuth/OIDC web client, validates the provider
  identity, and establishes the same StockHub session. A verified existing
  account links safely; an unverified or conflicting identity cannot take it
  over. OAuth credentials are runtime secrets.
- Forgot password sends a short-lived, one-use reset link to the account's
  email without revealing whether an address exists. Reset changes the
  password and revokes existing sessions. The delivery credential is a runtime
  secret.
- Every visible control on pages 0 through 1.3 has a real destination and a
  tested outcome. For page 1.4, the onboarding dashboard controls must work;
  sidebar routes may open honest future-milestone handoff pages.
- Cover no longer shows the roadmap Contents panel.

## Implementation and checks

1. Add transactional signup and explicit conflict guidance; test first signup,
   duplicate signup, sign-in, and rollback after session creation failure.
2. Add provider identity persistence and Google OIDC callback/session handling;
   test state, verified email, linking, duplicate identities, and failure paths.
3. Add password-reset token storage, email delivery adapter, request/reset API,
   and frontend screens; test expiry, single use, enumeration resistance, and
   session revocation.
4. Remove the Cover Contents panel and verify desktop and mobile layout.
5. Run frontend/backend tests, contract checks, container/kind smoke, and
   browser journeys. Configure Google and email secrets in the deployment
   environment and verify both against real providers before closure.
6. Update the Access plan, knowledge, and closure report with actual evidence;
   merge through required GitHub Actions checks. Close #27 and the milestone
   only after every agreed visible control passes in the deployed app.

## Deployment dependencies

Google sign-in needs a Google Cloud web OAuth client with the deployed callback
URL. Password recovery and invitation delivery need an outbound email sender
and verified sender address. No such credentials are currently present in the
local deployment.
They must be supplied through runtime secrets, never committed to this repo.

For the local Actions deployment served at `http://127.0.0.1:8080`, register
`http://127.0.0.1:8080/api/auth/google/callback` as an authorized redirect URI
on a Google OAuth web client. Store its client ID and secret in the deployment
secret keys `google-client-id` and `google-client-secret`. Configure an SMTP
service that can deliver to external addresses and supply `smtp-host`,
`smtp-port`, `smtp-from`, `smtp-username`, `smtp-password`, and
`public-base-url` (`http://127.0.0.1:8080`) in `stockhub-runtime`. The
deployment workflow must preserve these keys when it refreshes the PostgreSQL
connection secret. After deployment, verify a real Google callback, a real
inbox reset message, one-use reset, and sign-in with the new password.

The root Dockerfile, `.dockerignore`, Compose file, Kubernetes manifests,
health/readiness endpoints, and CI image/kind smoke checks already exist.
Auth changes must build in that image and pass the same smoke gates; any new
mail test service belongs only in test/Compose configuration.

## Verification in progress (2026-09-29)

- Backend build passed, and 15 backend tests passed, including atomic signup,
  Google identity linking/conflict, single-use password reset with session
  revocation, invitation email acceptance semantics, and product import/upsert
  persistence when PostgreSQL integration is enabled.
- Frontend tests passed: 10 tests, including same-origin API behavior and empty
  401 error parsing. Production Vite build passed.
- Page 1.4 now has working onboarding dashboard controls for CSV/manual product
  import, product search, workspace switch, theme toggle, notifications, account
  sign-out, sidebar collapse, invite routing, and platform picker handoffs.
- Invitation creation now requires configured email delivery, sends a one-use
  accept link, and lets the invited user accept only with the matching account
  email.
- Real Google callback, external inbox delivery, deployed app verification, and
  provider secret setup remain open. Milestone stays open until those live checks
  pass.

## Verification update (2026-10-12, issues #35 and #65)

- PR #170 merged issue #35's onboarding persistence/readback, validation, API
  tests, and frontend consumption to `main` at SHA
  `39eb728ad958b2f01e9cc4894987614e16c4e873`. Migration
  `009_onboarding_selection.sql` preserves prior action history. Its persistent
  local deployment run #38185661756 recorded image digest
  `sha256:0c54de86147fc256a7fdf89637f2b2bef10a136029677f3dbea7fa0288428632`.
  The issue remains open for live Google OIDC and real email delivery.
- API tests now cover personal-address signup, duplicate retry, session
  creation, sign-out revocation, password-reset enumeration-safe responses,
  one-use reset/session revocation, unavailable email configuration, and the
  onboarding/invitation/OIDC scenarios above. The targeted Access endpoint
  suite passes 10/10.
- A browser journey ran at `http://127.0.0.1:5179` against a disposable
  Compose/PostgreSQL stack on port 8180. Signup created a test-only account,
  workspace creation reached Dashboard, sign-in succeeded, account-menu
  sign-out returned to sign-in, and recovery showed the actionable
  unconfigured-email error. The Dashboard first-use subtitle initially showed
  the fixed sample business `Rossi Elettronica` instead of the created
  workspace. The API now derives this label from the authorized workspace and
  has an endpoint regression test.
- Recreating the API container also invalidated a persisted database session
  because its ASP.NET Data Protection keys were ephemeral. Compose now stores
  the key ring on a dedicated named volume and Kubernetes uses a dedicated PVC.
  Compose and Kubernetes smoke checks verify the cookie survives an API
  restart; a focused test verifies cookie validation across hosts using the
  configured key ring.
- The disposable account/database volume were removed after browser review.
  Real Google and SMTP verification and hosted staging/production remain
  unverified pending external credentials/infrastructure. The local
  PostgreSQL-backed PR gate remains authoritative for migration coverage.
