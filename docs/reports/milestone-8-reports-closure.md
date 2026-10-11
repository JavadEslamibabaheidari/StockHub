# Milestone 8 Reports closure report

status: PASS
milestone: 8 Reports
github_milestone_number: 9
plan: `docs/plans/milestone-8-reports.md`; contract freeze issue #126
mockup_evidence: `docs/mockups/StockHub Design Review.pdf` page 1 Reports row and the Reports section of `docs/mockups/StockHub Final.html`; the three screen captures were inspected in the PDF
adr_evidence: `docs/decisions/0006-identity-tenancy-and-roles.md`, `0007-frontend-and-api-contracts.md`, and `0008-testing-ci-and-deployment.md`
knowledge_evidence: `docs/knowledge/reports.md` created from the merged API, persistence, and UI behavior
roadmap_evidence: `docs/plans/mockup-led-roadmap.md` records Reports closure and identifies Team as the next mockup section
code_evidence: PR #133 merged to `main` at `e2fa0bdc2cc3ef6ab1db670b363d3d7e1bba0ffb`; issue #126 froze the contract; issues #127–#130 delivered the API, screens, persisted reorder actions, and alert preferences; issue #131 owns integration and closure
tests_evidence: `dotnet test backend/tests/StockHub.Api.Tests/StockHub.Api.Tests.csproj --no-restore --verbosity minimal` passed 37 tests; `npm test` passed 21 tests; `npm run build` and `npm run contract:check` passed; CI run 38122448542 passed image scan, security gate, Compose smoke, and Kubernetes migration/rollout smoke; previous milestone closure report checks for milestones 0–7 passed
configuration_evidence: main run 38122722365 published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:a412e8a0a2435589e5a1fe31f0313592bcbadda4d60627734cd7c28444a4fa0f`; migration `007_reports.sql`; local kind deployment records source SHA `e2fa0bdc2cc3ef6ab1db670b363d3d7e1bba0ffb` and this digest, migration Job completed, deployment pod Ready, `/ready` returned `{"status":"ready"}`
github_evidence: GitHub milestone 9 closed with zero open issues; issues #126–#131 closed; PR #133 merged and closed; main delivery run 38122722365 passed. Review-staging and production jobs were skipped. Maintenance issue #132 records real alert delivery.
previous_milestone_hook: PASS — reports for milestones 0–7 passed structural closure checks and all corresponding GitHub milestones were confirmed closed with zero open issues

## Alignment matrix

| ID | Source / expected outcome | Implementation or configuration | Test / evidence | Status | Reason, owner, follow-up issue, target milestone |
|---|---|---|---|---|---|
| M8-1 | Frozen workspace-scoped contract and UTC period rules | Issue #126; `contracts/reports.openapi.json`; API routes | Contract checker and API tests | PASS | None |
| M8-2 | Daily/Weekly metrics, platform totals, lost-sale separation, rankings, reorder persistence | Reports API and migration `007_reports.sql`; PR #133 | Backend suite, persistence/reload and workspace checks; local browser smoke | PASS | None |
| M8-3 | Daily, Weekly, and Alert settings screens match project references | `frontend/src/ReportsScreens.tsx`; PR #133 | Frontend route/interactions; browser smoke for tabs, workspace, reorder, alert save/reload, shell controls, and PDF download | PASS | None |
| M8-4 | PR integration, regressions, image publication, local deployment, and closure | PR #133; main source SHA `e2fa0bdc2cc3ef6ab1db670b363d3d7e1bba0ffb`; main run 38122722365 | Backend/frontend/contract/Compose/security/Kubernetes checks and local `/ready` | PASS | Hosted review-staging/production remain unverified under Maintenance issue #48 |
| DELIVERY | Configured alert channels must represent actual delivery truthfully | UI states that alert preferences persist without dispatch; Maintenance issue #132 tracks email/in-app/Slack delivery | Browser verified the honest preference-only state and notification panel | PASS | #132, Maintenance |
| TENANCY | Reports and persisted preferences/reorder state remain workspace scoped | API verifies membership and workspace; settings/reorder persistence scoped by workspace | Backend tenant/role tests and browser workspace switch/reload checks | PASS | None |

## Deliberate deviations

- Email, in-app, and Slack alert dispatch is not configured. The Reports page
  describes this accurately and Maintenance issue #132 tracks the dispatcher,
  adapters, retries, and delivery evidence.
- Hosted review-staging and production deployment jobs were skipped. These
  targets remain unverified; Maintenance issue #48 tracks their activation.
- Automated browser smoke exercised the Reports screens and controls on the
  desktop layout. A phone-width interaction run was not captured during this
  milestone; responsive styling is present, and viewport interaction follow-up
  is tracked in Maintenance issue #123.

## Missing coverage and follow-ups

- Maintenance issue #132: implement and verify actual Email, in-app, and Slack
  delivery, retries, deduplication, and delivery state.
- Maintenance issue #48: activate and verify shared hosted staging and
  production targets.
- Maintenance issue #123: capture narrow phone-width interaction evidence.

## Evidence and verification

- GitHub PR #133 merged and closed. It contains migration `007_reports.sql`,
  workspace-scoped backend reports and alert preferences, Daily/Weekly/Alert
  screens, client-side PDF export, durable reorder controls, and updated
  contract and roadmap documentation.
- On the merged PR, backend tests passed (37), frontend tests passed (21),
  production frontend build passed, and generated Access/Reports OpenAPI checks
  passed. Compose build and same-origin smoke passed. Delivery PR workflow
  38122448542 passed image scanning, the critical/high vulnerability gate,
  Compose health/route smoke, and temporary Kubernetes migration/rollout smoke.
- Previous-milestone closure report checks passed for milestones 0–7.
  GitHub milestones 0–7 were confirmed closed. M8 changes are additive: Orders
  and Inventory data are consumed through owning module interfaces.
- Local browser verification used a disposable account and isolated database.
  Daily/Weekly report route and API values, reorder persistence, alert values
  and save/reload, report tabs, PDF download click, appearance, sync and
  notifications panels, account actions, workspace selector, and sidebar
  collapse/expand were exercised. The temporary database and container were
  removed afterward.
- Main delivery run 38122722365 succeeded for source SHA
  `e2fa0bdc2cc3ef6ab1db670b363d3d7e1bba0ffb` and published immutable image
  digest `sha256:a412e8a0a2435589e5a1fe31f0313592bcbadda4d60627734cd7c28444a4fa0f`.
  The local Kubernetes configmap names that source and digest; migration Job
  completed, deployment pod is Ready, rollout succeeded, and
  `http://127.0.0.1:8080/ready` returned `{"status":"ready"}`.
- Review-staging and production deployment jobs were skipped. GitHub `dev`
  remains at `21549b36342cb031dc94bc79b28ee502be5dbf9c`; M8 is merged to main
  and was not back-merged into dev.
- GitHub milestone 9 is closed with zero open issues; issues #126–#131 are
  closed; PR #133 is merged and closed. `scripts/check-milestone-closure.sh`
  and `scripts/check-tracking-status.sh` passed against the final GitHub state.

## Closure verdict

PASS. Reports is merged to `main` at `e2fa0bdc2cc3ef6ab1db670b363d3d7e1bba0ffb`.
  All Reports issues are closed, PR #133 is merged and closed, GitHub milestone
  9 is closed with zero open issues, CI and image publication passed, and the
  persistent local Kubernetes target runs the identified digest with migration
  and readiness evidence. Hosted target and real notification delivery gaps
  have tracked Maintenance follow-ups (#48 and #132); phone-width interaction
  evidence is tracked in #123.
