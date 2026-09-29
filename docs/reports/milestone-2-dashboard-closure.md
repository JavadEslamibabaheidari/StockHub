# Milestone closure report — 2 Dashboard

status: IN PROGRESS
milestone: 2 Dashboard
plan: docs/plans/milestone-2-dashboard.md
mockup_evidence: docs/mockups/StockHub Final.html section 2 Dashboard; user-provided Milestone 2 screenshots for 2.1 Dashboard, 2.2 Dashboard first use, and 2.3 Dashboard first sync
adr_evidence: docs/decisions/0002-module-boundaries-and-contracts.md; docs/decisions/0004-inventory-ledger-and-reservations.md; docs/decisions/0006-identity-tenancy-and-roles.md; docs/decisions/0007-frontend-and-api-contracts.md; docs/decisions/0008-testing-ci-and-deployment.md
knowledge_evidence: docs/knowledge/architecture.md; docs/knowledge/data.md; docs/knowledge/frontend.md
roadmap_evidence: docs/plans/mockup-led-roadmap.md milestone 2 Dashboard
code_evidence: backend/src/StockHub.Api; backend/database/migrations/004_dashboard_state.sql; frontend/src/DashboardScreens.tsx; frontend/src/App.tsx; frontend/src/styles.css
configuration_evidence: Dockerfile; docker-compose.yml; backend readiness endpoint; GitHub Actions workflows

evidence_last_updated: 2026-09-29

## Alignment matrix

| ID | Source / expected outcome | Implementation or configuration | Test / evidence | Status | Reason, owner, follow-up issue, target milestone |
|---|---|---|---|---|---|
| mockup-2.1 | Live Dashboard metrics, reservations, sales, sync, attention controls | Dashboard API snapshot, workspace dashboard state, React live dashboard | Browser verified on local Compose at `http://127.0.0.1:18082/#dashboard`; API action flow persisted retry, adjust, resolve, restock, and expanded low stock | PASS locally | CI and PR review pending |
| mockup-2.2 | First-use Dashboard get-started, empty cards, upload, platform picker, invite | First-use dashboard state, product import reuse, platform first-sync action, invite API reuse | Browser verified first-use controls and API verified first-use snapshot | PASS locally | Invite delivery remains blocked by Access email provider setup when SMTP is not configured |
| mockup-2.3 | First-sync progress state and safe background placeholders | `start-first-sync` dashboard action and persisted mode | Browser verified platform button opens first-sync screen and finish preview returns safely | PASS locally | Real marketplace authorization belongs to Milestone 6 Platforms |
| access-regression | Signup, sign-in, workspace selection, logout, and navigation into/out of Dashboard still work | Dashboard route added; sign-in/workspace success now routes to Dashboard; sign-out remains cookie-backed | API signup/workspace setup verified; browser sign-in and sign-out verified | PASS | Milestone 1 closure still blocked by external Google OAuth and email provider setup |
| backend | UI data and mutable actions need real API/persistence | `IDashboardStore`, PostgreSQL `dashboard_states`, Dashboard endpoints, product on-hand update | Backend tests 16/16; API flow against Compose verified product on-hand persisted to 9 | PASS locally | CI pending |
| handoffs | Later-milestone actions must be clear and not pretend complete | Sidebar routes and Dashboard detail panels name owning future milestones | Browser verified Inventory route shows Coming next and no action marked complete | PASS locally | Future module implementation remains in owning milestones |
| tests | Important flows covered | Backend Dashboard store test; frontend Dashboard route render test; existing Access tests preserved | Local tests passed; PR #68 and PR #69 required checks passed | PASS | Main delivery run 36616827440 succeeded after PR #68 merge |
| deployment | Containerized app must build and run with readiness | Docker Compose build/start on port 18082 with PostgreSQL migration 004; GitHub Actions delivery | Compose image built locally; `/ready` returned `{"status":"ready"}`; main delivery run 36616827440 passed image/kind smoke and published the tested source image | PASS | Review-staging and production promotion skipped by configured gates |
| github | Issues, PR, docs, closure evidence synchronized | Dashboard implementation issue and PR to be linked before closure | Dashboard implementation issue #67 closed by merged PR #68 | PASS | PR #68 checks passed; main delivery run 36616827440 succeeded; review-staging and production promotion skipped by configured gates |

## Deliberate deviations

- Real marketplace OAuth, token refresh, and connector repair remain owned by Milestone 6 Platforms. Dashboard controls open first-sync, sync-detail, and retry states without claiming a real connector is authorized.
- Full Inventory, Orders, Reservations, Pricing, Reports, Team, Settings, Billing, and Dark mode screens remain owning milestones. Dashboard visible controls either work in Dashboard or open handoff pages/panels that name the owning milestone.
- Invitation delivery uses the Access invitation API. In environments without SMTP configuration, the UI reports the environment blocker rather than pretending an invite was delivered.

## Missing coverage and follow-ups

- Milestone 2 still has open cross-cutting delivery gate issue #47 assigned to the same GitHub milestone. Do not close the GitHub milestone until #47 is resolved or intentionally moved.
- Review-staging and production promotion remained skipped by repository gates in the main delivery workflow. Shared hosted deployment activation remains outside this Dashboard screen PR.
- Milestone 1 Access remains blocked by Google OAuth and email provider configuration; this Dashboard work does not close that milestone.

## Local and GitHub evidence collected on 2026-09-29

- `dotnet build StockHub.sln --no-restore` passed.
- `dotnet test StockHub.sln --no-restore` passed: 16 tests.
- `cd frontend && npm test -- --run` passed: 11 tests.
- `cd frontend && npm run contract:check` passed.
- `cd frontend && npm run build` passed.
- `docker compose up --build -d` built the production image. Port 8080 was occupied by another local deployment, so browser verification ran with `STOCKHUB_PORT=18082 docker compose up -d`.
- `curl http://127.0.0.1:18082/ready` returned ready.
- Local API flow verified disposable signup, workspace activation, first-use dashboard, first-sync action, product import, retry, adjust on hand, resolve mismatch, restock, low-stock expansion, and persisted product on-hand value.
- In-app browser verified sign-in, Dashboard first-use controls, first-sync screen, Upload CSV, Invite environment blocker, live dashboard, search, reservation handoff, retry, adjust on hand, use StockHub count, restock, low-stock expansion, later-milestone handoff, and sign-out.
- PR #68 passed required GitHub checks and was squash-merged as `444c1da9f2962e68f723357990fd9a920c1ffdaa`.
- Main GitHub Actions run `36616827440` completed successfully: `Image scan and local Kubernetes smoke` passed and `Publish tested source image` passed. `Deploy review-staging` and `Promote digest to production` were skipped by their configured workflow conditions.

## Closure verdict

IN PROGRESS. Dashboard screen implementation is merged and the main GitHub Actions delivery workflow succeeded. Keep Milestone 2 open until cross-cutting delivery gate issue #47 is resolved with the required self-hosted deployment evidence.
