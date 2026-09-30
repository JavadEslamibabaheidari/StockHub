# Milestone closure report — 1 Access

status: BLOCKED
github_milestone_number: 1
milestone: 1 Access
reopened_reason: Live testing found unavailable Google sign-in, failed signup handling, missing password recovery, and incomplete visible controls. PR #66 added corrective code, but real provider and deployed browser verification remain open under #27, #35, and #65.
plan: docs/plans/milestone-1-access.md
mockup_evidence: docs/mockups/StockHub Final.html section 1 Access; docs/mockups/StockHub Design Review.pdf section 1 Access
adr_evidence: docs/decisions/0002-module-boundaries-and-contracts.md; docs/decisions/0006-identity-tenancy-and-roles.md; docs/decisions/0007-frontend-and-api-contracts.md; docs/decisions/0008-testing-ci-and-deployment.md
knowledge_evidence: docs/knowledge/architecture.md; docs/knowledge/data.md; docs/knowledge/frontend.md; docs/knowledge/messaging.md
roadmap_evidence: docs/plans/mockup-led-roadmap.md milestone 1 Access
code_evidence: backend/src/StockHub.Api; backend/database/migrations/001_access.sql; contracts/access.openapi.json; frontend/src/api; frontend/src/App.tsx; frontend/src/styles.css; Dockerfile; docker-compose.yml; corrective authentication PR #66
tests_evidence: PR #66 reports 13 PostgreSQL-backed backend tests, 9 frontend tests, contract check, production build, Docker/Compose smoke, and a local API/Mailpit flow; real Google callback and external inbox remain unverified
configuration_evidence: StockHub.sln; PostgreSQL migration; Docker Compose health checks and environment-only connection settings; real Google OAuth and external SMTP credentials are not yet verified in deployment
github_evidence: milestone https://github.com/JavadEslamibabaheidari/StockHub/milestone/1 is open with #27, #35, and #65 open; PR #66 merged corrective code but explicitly did not close Access
previous_milestone_hook: PASS — Cover closure report is status PASS on merged PR #18 and milestone 0 is closed
baseline: merged Cover mainline before Access implementation

## Alignment matrix

The PASS rows below describe the earlier code and CI audit. They do not
establish current closure. The live review reopened the Access milestone and
issues #27, #35, and #65; the missing provider, email, and deployed browser
evidence in the verdict below overrides the earlier row assessments.

| ID | Source / expected outcome | Implementation or configuration | Test / evidence | Status | Reason, owner, follow-up issue, target milestone |
|---|---|---|---|---|---|
| mockup | Sign-up, sign-in, workspace, and onboarding hierarchy and states | frontend/src/App.tsx and styles | PR #66 added recovery screens; live provider and visible-control verification pending | PARTIAL | #35 and #65; target Milestone 1 |
| plan | Access acceptance criteria and dependency order implemented | docs/plans/milestone-1-access.md and remediation plan; issues #27, #35, #65 | PR #66 corrective code merged; live provider evidence pending | BLOCKED | #27, #35, and #65; target Milestone 1 |
| adr | Tenant boundary, cookie session, generated contract, CI, persistence, and deployment decisions respected | backend/src; contracts; migrations; workflows; container files | Backend, frontend, container, and repository checks passed | PASS | No follow-up; target complete |
| knowledge | Architecture, data, frontend, and messaging knowledge matches implementation | docs/knowledge/*.md | Source and command verification | PASS | No follow-up; target complete |
| roadmap | Access section delivered without claiming future module completion | docs/plans/mockup-led-roadmap.md; onboarding handoffs | Route, API contract, same-origin, and closure checks | PASS | Inventory/platform/team actions remain owning-milestone handoffs |
| code | Durable Access runtime, migrations, frontend, container packaging, and one-origin serving are present | listed in code_evidence | PR #38 persistence; PR #43 packaging; PR #44 client-origin regression test | PASS | No follow-up; target complete |
| tests | Validation, retry, tenant denial, auth states, PostgreSQL persistence, contract, frontend routes, and builds pass | backend/tests; frontend tests; CI workflows | PR #66 automated and local smoke checks passed | PARTIAL | Real provider and external inbox journeys remain; #35 and #65 |
| configuration | Local/review configuration and deployment evidence are reproducible | migration, solution, package locks, Docker Compose, workflows | Local smoke checks passed; deployed Google and SMTP flows not verified | BLOCKED | Provider configuration and browser checks remain; #65 |
| github | Issues, branches, PRs, checks, milestone, and closure evidence synchronized | milestone 1 and issue/PR links above | Current GitHub milestone has three open issues | BLOCKED | #27, #35, and #65 remain open; target Milestone 1 |

## Deliberate deviations

Inventory import execution, platform authorization, and full team administration remain owning-milestone handoffs. Google sign-in and outbound email are Access closure requirements after live review; they are blockers, not accepted deviations.

## Missing coverage and follow-ups

Access remains blocked until deployed verification proves real Google sign-in and external email delivery for password recovery and invitations. Page 1.4 dashboard controls now have implementation coverage for product import, platform picker handoff, invitation routing, workspace switch, search, theme, notifications, account menu, sign-out, sidebar collapse, and later-milestone navigation. Start free trial remains the only explicitly deferred Access action.

## Evidence and verification

- PostgreSQL persistence replaced the default runtime in-memory store; migration, durable sessions, workspaces, memberships, invitations, onboarding actions, and idempotency are covered by implementation and integration tests.
- Dockerfile and Compose package the frontend, API, and PostgreSQL with health checks; the smoke script verifies health, root HTML, the /workspace deep link, and a same-origin unauthenticated API response.
- The frontend API client regression test proves its default request base is relative and uses cookie credentials.
- PR #43 passed backend, frontend, container, and repository checks and closed #40; PR #44 passed all four required checks and closed #41.
- The earlier report and tag were superseded by live review. PR #66 merged corrective code, but its description keeps the milestone open pending provider and deployed browser verification.

## Closure verdict

BLOCKED. Milestone 1 Access is not ready to close until the runtime Google OAuth client and outbound email sender are configured, deployed through GitHub Actions, and verified against real provider flows.
