# Milestone closure report — 1 Access

status: BLOCKED
milestone: 1 Access
reopened_reason: Live testing found unavailable Google sign-in, failed signup handling, missing password recovery, and incomplete visible controls. The PASS evidence below describes the earlier closure and is superseded by docs/plans/milestone-1-access-remediation.md and issue #65 until deployed verification is complete.
plan: docs/plans/milestone-1-access.md
mockup_evidence: docs/mockups/StockHub Final.html section 1 Access; docs/mockups/StockHub Design Review.pdf section 1 Access
adr_evidence: docs/decisions/0002-module-boundaries-and-contracts.md; docs/decisions/0006-identity-tenancy-and-roles.md; docs/decisions/0007-frontend-and-api-contracts.md; docs/decisions/0008-testing-ci-and-deployment.md
knowledge_evidence: docs/knowledge/architecture.md; docs/knowledge/data.md; docs/knowledge/frontend.md; docs/knowledge/messaging.md
roadmap_evidence: docs/plans/mockup-led-roadmap.md milestone 1 Access
code_evidence: backend/src/StockHub.Api; backend/database/migrations/001_access.sql; contracts/access.openapi.json; frontend/src/api; frontend/src/App.tsx; frontend/src/styles.css; Dockerfile; docker-compose.yml; scripts/check-compose-smoke.sh; .github/workflows/backend-ci.yml; .github/workflows/frontend-ci.yml; .github/workflows/container-ci.yml
tests_evidence: backend 6 tests passed locally and PostgreSQL integration tests passed in Backend CI; frontend 8 tests passed in Frontend CI; generated contract check and production build passed; Container smoke passed on PR #43 and PR #44
configuration_evidence: StockHub.sln; PostgreSQL migration; Docker Compose health checks and environment-only connection settings; same-origin static-file and SPA fallback; no production deployment introduced
github_evidence: milestone https://github.com/JavadEslamibabaheidari/StockHub/milestone/1; corrective issue #39 closed by PR #38; issue #40 closed by merged PR #43; issue #41 closed by merged PR #44; all required checks passed
previous_milestone_hook: PASS — Cover closure report is status PASS on merged PR #18 and milestone 0 is closed
baseline: merged Cover mainline before Access implementation

## Alignment matrix

| ID | Source / expected outcome | Implementation or configuration | Test / evidence | Status | Reason, owner, follow-up issue, target milestone |
|---|---|---|---|---|---|
| mockup | Sign-up, sign-in, workspace, and onboarding hierarchy and states | frontend/src/App.tsx and styles | 8 frontend tests; responsive CSS; keyboard-visible focus | PASS | Product owner; no follow-up; target complete |
| plan | Access acceptance criteria and dependency order implemented | docs/plans/milestone-1-access.md; issues #20–#27, #35, #39–#41 | PRs #28–#38, #43, and #44; corrective issues closed | PASS | Quality gate satisfied; no follow-up; target complete |
| adr | Tenant boundary, cookie session, generated contract, CI, persistence, and deployment decisions respected | backend/src; contracts; migrations; workflows; container files | Backend, frontend, container, and repository checks passed | PASS | No follow-up; target complete |
| knowledge | Architecture, data, frontend, and messaging knowledge matches implementation | docs/knowledge/*.md | Source and command verification | PASS | No follow-up; target complete |
| roadmap | Access section delivered without claiming future module completion | docs/plans/mockup-led-roadmap.md; onboarding handoffs | Route, API contract, same-origin, and closure checks | PASS | Inventory/platform/team actions remain owning-milestone handoffs |
| code | Durable Access runtime, migrations, frontend, container packaging, and one-origin serving are present | listed in code_evidence | PR #38 persistence; PR #43 packaging; PR #44 client-origin regression test | PASS | No follow-up; target complete |
| tests | Validation, retry, tenant denial, auth states, PostgreSQL persistence, contract, frontend routes, and builds pass | backend/tests; frontend tests; CI workflows | Local tests plus all required GitHub checks on PRs #43 and #44 | PASS | No follow-up; target complete |
| configuration | Local/review configuration and deployment evidence are reproducible | migration, solution, package locks, Docker Compose, workflows | docker compose config/build/smoke; backend/frontend CI | PASS | No production deployment introduced; target complete |
| github | Issues, branches, PRs, checks, milestone, and closure evidence synchronized | milestone 1 and issue/PR links above | GitHub review before closure | PASS | Repository owner; no follow-up; target complete |

## Deliberate deviations

Google provider credentials and production identity-provider operations remain configuration-owned deployment work, as permitted by the plan's non-goals. Inventory import execution, platform authorization, email delivery, and full team administration remain explicit owning-milestone handoffs; Access exposes honest entry states without claiming those capabilities complete.

## Missing coverage and follow-ups

Access remains blocked until deployed verification proves real Google sign-in and external email delivery for password recovery and invitations. Page 1.4 dashboard controls now have implementation coverage for product import, platform picker handoff, invitation routing, workspace switch, search, theme, notifications, account menu, sign-out, sidebar collapse, and later-milestone navigation. Start free trial remains the only explicitly deferred Access action.

## Evidence and verification

- PostgreSQL persistence replaced the default runtime in-memory store; migration, durable sessions, workspaces, memberships, invitations, onboarding actions, and idempotency are covered by implementation and integration tests.
- Dockerfile and Compose package the frontend, API, and PostgreSQL with health checks; the smoke script verifies health, root HTML, the /workspace deep link, and a same-origin unauthenticated API response.
- The frontend API client regression test proves its default request base is relative and uses cookie credentials.
- PR #43 passed backend, frontend, container, and repository checks and closed #40; PR #44 passed all four required checks and closed #41.
- The closure hook is run against this report before the GitHub milestone is closed.

## Closure verdict

BLOCKED. Milestone 1 Access is not ready to close until the runtime Google OAuth client and outbound email sender are configured, deployed through GitHub Actions, and verified against real provider flows.
