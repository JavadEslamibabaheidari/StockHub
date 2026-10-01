# Milestone closure report - 3 Inventory

status: IN PROGRESS
github_milestone_number: 4
milestone: 3 Inventory
plan: docs/plans/milestone-3-inventory.md
mockup_evidence: user-provided screenshots 3.1 through 3.5 dated 2026-10-01 and docs/mockups/StockHub Final.html section 3 Inventory
adr_evidence: docs/decisions/0002-module-boundaries-and-contracts.md; docs/decisions/0004-inventory-ledger-and-reservations.md; docs/decisions/0006-identity-tenancy-and-roles.md; docs/decisions/0007-frontend-and-api-contracts.md; docs/decisions/0008-testing-ci-and-deployment.md
knowledge_evidence: docs/knowledge/architecture.md; docs/knowledge/data.md; docs/knowledge/frontend.md
roadmap_evidence: docs/plans/mockup-led-roadmap.md milestone 3 Inventory
code_evidence: backend/src/StockHub.Api/Contracts/InventoryContracts.cs; backend/src/StockHub.Api/Program.cs; frontend/src/InventoryScreens.tsx; frontend/src/App.tsx; frontend/src/api/generated.ts; frontend/src/App.test.tsx; frontend/src/api/generated.test.ts; frontend/src/styles.css
tests_evidence: cd frontend && npm test PASS 14 tests; cd frontend && npm run build PASS; cd frontend && npm run contract:check PASS; dotnet test StockHub.sln PASS 16 tests; browser smoke PASS for Inventory, Dashboard, and Sign-in routes
configuration_evidence: no new deployable project; existing Dockerfile, docker-compose.yml, readiness endpoint, and Kubernetes manifests remain the deployment path
github_evidence: issues #74, #75, #76, #77, #78 open under GitHub milestone 3 Inventory pending PR merge and deployment verification
previous_milestone_hook: PASS - GitHub milestone 2 Dashboard is closed with zero open issues

## Alignment matrix

| ID | Source / expected outcome | Implementation or configuration | Test / evidence | Status | Reason, owner, follow-up issue, target milestone |
|---|---|---|---|---|---|
| 3.1 | Inventory list with sync warning, filters, selected rows, stock equation, platform status | Inventory API list projection; `#inventory` route; Inventory table styles | Frontend route test; browser smoke; backend test suite compile | PASS | Owner: engineering; follow-up: #78 until merge/deploy; target: 3 Inventory |
| 3.2 | Empty state with import CSV, add product, template handoff | `#inventory-empty`; template download; explicit import/manual-add panels | Frontend route test; browser smoke | PASS | Full import review remains future Inventory hardening; owner: engineering; follow-up: #76; target: Inventory hardening |
| 3.3 | Warehouse staff view can adjust on hand but not prices | Staff route, capability model, row steppers, backend role capability response | Frontend route test; browser smoke | PASS | Owner: engineering; follow-up: #75 until merge; target: 3 Inventory |
| 3.4 | Product detail shows stock, listings, pricing, audit log | Inventory detail projection; `#inventory-detail`; product detail UI | Frontend route test; browser smoke | PASS | Pricing automation handed to milestone 7; owner: engineering; follow-up: #77; target: 3 Inventory |
| 3.5 | All-platforms-failing state with oversell warning, pause, retry | `allFailing` detail projection; `#inventory-detail-failing` | Frontend route test; browser smoke | PASS | Live connector repair belongs to milestone 6; owner: engineering; follow-up: #77; target: 3 Inventory |
| REG | Previous pages do not diverge | Current `origin/main` used; Dashboard and Sign-in routes preserved | `npm test`; browser smoke for `#dashboard` and `#signin` | PASS | Owner: engineering; follow-up: #78 until post-merge verification; target: 3 Inventory |
| BTN | Buttons/icons functional unless future-owned | Inventory controls update local/API state or open explicit panels naming future milestones | Browser smoke and code review | PASS | Owner: engineering; follow-up: future owning milestones for import hardening, Platforms, Pricing rules, Reports |
| API | Backend and frontend delivered together | Inventory contracts and endpoints plus generated TypeScript client methods | `dotnet test`, `npm test`, `contract:check` | PASS | Owner: engineering; follow-up: #78 until merged/deployed |
| DEPLOY | Latest deployed pod contains changes | Existing deployment path unchanged | Pending PR merge and deployment check | IN_PROGRESS | Owner: engineering; follow-up: #78; target: 3 Inventory |
| GITHUB | Issues/PRs closed and merged to main | Issues #74-#78 created | Pending PR and closure | IN_PROGRESS | Owner: engineering; follow-up: #78; target: 3 Inventory |

## Deliberate deviations

- Full CSV import review and manual product creation are represented through
  the existing import contract plus explicit handoff panels. Rationale: the
  existing Dashboard/onboarding import path already owns the first usable CSV
  import; richer Inventory import review should be a focused hardening task.
  Owner: engineering. Follow-up: #76. Target: Inventory hardening.
- Marketplace connector repair and real retry transport are not implemented in
  this milestone. Rationale: platform credentials and sync workers belong to
  Milestone 6 Platforms. Owner: engineering. Follow-up: future Platforms
  issues. Target: 6 Platforms.
- Pricing-rule automation is not implemented. Rationale: it belongs to
  Milestone 7 Pricing rules. Owner: engineering. Follow-up: future Pricing
  rules issue. Target: 7 Pricing rules.

## Missing coverage and follow-ups

- PR merge, issue closure, and deployed pod freshness remain pending. Owner:
  engineering. Follow-up: #78. Target: 3 Inventory.

## Evidence and verification

- `cd frontend && npm test` - PASS, 14 tests.
- `cd frontend && npm run build` - PASS.
- `cd frontend && npm run contract:check` - PASS.
- `dotnet test StockHub.sln` - PASS, 16 tests.
- Browser smoke on `http://127.0.0.1:5173` confirmed:
  - `#inventory`
  - `#inventory-empty`
  - `#inventory-staff`
  - `#inventory-detail`
  - `#inventory-detail-failing`
  - `#dashboard`
  - `#signin`
- GitHub previous milestone check: `2 Dashboard` is closed with zero open
  issues.

## Closure verdict

IN PROGRESS. The implementation and local verification pass. Final closure is
waiting for PR merge to `main`, issue closure, and deployed pod freshness
verification.
