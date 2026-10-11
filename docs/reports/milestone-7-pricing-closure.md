# Milestone 7 Pricing rules closure report

status: PASS
milestone: 7 Pricing rules
github_milestone_number: 8
plan: `docs/plans/milestone-7-pricing.md`; approved baseline and OpenAPI contract in PR #120
mockup_evidence: `/home/javad/Pictures/Screenshots/Screenshot From 2026-10-11 00-35-48.png` visually inspected; section 7.1 Pricing rules in `docs/mockups/StockHub Final.html`
adr_evidence: `docs/decisions/0006-identity-tenancy-and-roles.md`, `0007-frontend-and-api-contracts.md`, and `0008-testing-ci-and-deployment.md`; reviewed for the M7 start gate and implementation
knowledge_evidence: `docs/knowledge/pricing.md`, `docs/knowledge/data.md`, and `docs/knowledge/architecture.md` updated against merged implementation
roadmap_evidence: `docs/plans/mockup-led-roadmap.md` updated; milestone 8 Reports is next
code_evidence: PR #120 contract/planning; PR #121 backend merged at `06a00536bb7dca984ac01501b31521e9f6965dac`; PR #122 UI merged at main `2c43ca9927a0784838d6c512fafe129ed7439d3b`
tests_evidence: 29 backend tests passed with isolated PostgreSQL; 20 frontend tests, production build, generated-client contract check, repository sanity, Compose smoke, Trivy/image scan, and temporary Kubernetes rollout smoke passed; browser create/preview/update/reload/search/theme/collapse/sign-out and two-workspace isolation checks passed
configuration_evidence: migration `006_pricing_rules.sql`; main run 38118474661 published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:d0669a559e8a8dfe6176787146f3cbb067af8a97e9dfdf986af5d3d178010e0e`; `kind-stockhub-local` deployment record names source SHA `2c43ca9927a0784838d6c512fafe129ed7439d3b` and that digest; migration Job completed, deployment pod ready, `http://127.0.0.1:8080/ready` returned `{"status":"ready"}`
github_evidence: GitHub milestone 8 closed with zero open issues; issues #116–#119 closed; PRs #120–#122 merged; main delivery run 38118474661 succeeded. Hosted review-staging and production jobs were skipped. Mobile viewport automation limitation is tracked in Maintenance issue #123.
previous_milestone_hook: PASS — milestone 6 Platforms report passed structural validation and GitHub milestone 7 was closed with zero open issues

## Alignment matrix

| ID | Source / expected outcome | Implementation or configuration | Test / evidence | Status | Reason, owner, follow-up issue, target milestone |
|---|---|---|---|---|---|
| M7-1 | Screenshot 7.1, workspace pricing rules, frozen API | `docs/plans/milestone-7-pricing.md`, `contracts/pricing.openapi.json`, PR #120 | Contract check and approved design decisions | PASS | None |
| M7-2 | Durable tenant-scoped rule CRUD, evaluation, validation, preview | `Pricing.cs`, `PricingEndpoints.cs`, `PostgresPricingStore.cs`, migration `006_pricing_rules.sql`; PR #121 | Domain, role, PostgreSQL persistence/isolation/rollback tests; backend CI | PASS | None |
| M7-3 | Responsive pricing list/editor, scopes, dates, priority, preview, shell controls | `frontend/src/PricingScreens.tsx`, generated API client, `App.tsx`, styles; PR #122 | 20 frontend tests/build; browser create, preview, update, reload, search, theme, collapse, sign-out; two-workspace isolation/switch persistence | PASS | Phone-width viewport interaction could not be driven by the available browser control; tracked in Maintenance issue #123 |
| M7-4 | Integrated regression, container, CI, delivery, deployment and closure evidence | Main SHA `2c43ca9`; immutable image and persistent local Kind deployment | Backend/frontend/contract, Compose, image scan, migration/rollout checks, local `/ready`; closure and tracking hooks | PASS | Hosted staging/production remain unverified because deployment jobs were skipped; existing Maintenance issue #48 tracks activation |
| TENANCY | Workspace switching shows only that workspace’s rules and persists selection | Existing active-workspace endpoint; API workspace lock/isolation | Browser switched A→B→reload→A: B showed zero rules; A’s saved rule remained | PASS | None |
| FINANCE | Clear marketplace fee/VAT assumptions and no fabricated cost margin | Preview discloses marketplace estimates and VAT 22%; margin remains null until cost exists | Browser preview for €799 with +5% returned €838.95 and an unavailable-margin explanation | PASS | Real marketplace fee configuration and publication remain outside this milestone |

## Deliberate deviations

- Marketplace publication, configurable real marketplace fees, and cost-based
  margin are outside the milestone boundary. The preview labels fee/VAT inputs
  as estimates and does not invent a margin.
- Hosted shared staging and production were not deployed: both workflow jobs
  were skipped. Existing Maintenance issue #48 records the required hosted
  infrastructure and secrets. The persistent local Kubernetes target was
  deployed and verified separately.
- The browser automation surface could not set a narrow 390×844 viewport; its
  exposed zoom keys did not resize the viewport. The Pricing screen was
  visually reviewed at 908×760, where the 1050px stacked layout applied, and
  the 680px responsive navigation rules were reviewed in the built stylesheet.
  The missing phone-width interaction check is tracked in Maintenance issue
  #123, with concrete acceptance criteria.

## Missing coverage and follow-ups

- Maintenance issue #123: run the Pricing workflows at 390×844 after browser
  viewport emulation is available. The app includes the mobile breakpoint;
  this follow-up concerns independent phone-width interaction evidence.
- Maintenance issue #48: activate and verify shared hosted review-staging and
  production targets. These were unavailable in this run and remain
  unverified.

## Evidence and verification

- Plan and contract: PR #120 merged to `main`; backend PR #121 and UI PR #122
  merged. Current `main` is `2c43ca9927a0784838d6c512fafe129ed7439d3b`.
- Backend suite: `dotnet test StockHub.sln --no-restore --verbosity minimal`
  passed 29 tests using an isolated PostgreSQL test database.
- Frontend: `npm --prefix frontend test` passed 20 tests;
  `npm --prefix frontend run build` passed;
  `npm --prefix frontend run contract:check` passed;
  `git diff --check` passed.
- PR #122 required CI passed, including backend tests, frontend build, Compose
  health/route smoke, Trivy, image scan, and temporary Kubernetes
  migration/rollout smoke. Main delivery run 38118474661 passed the same
  delivery gates, published the immutable image, and dispatched the persistent
  local deployer.
- Local browser/API verification used a disposable account and isolated
  PostgreSQL container. It created and previewed a +5% Amazon rule for a €799
  sample item (€838.95), saved/edited/reloaded the rule, filtered it by search,
  retained the theme through reload, collapsed/expanded the sidebar, and signed
  out. Two workspaces verified active-workspace persistence and rule
  separation. The test container and synthetic data were removed afterward.
- Local Kubernetes verification used context `kind-stockhub-local`, namespace
  `stockhub-local`: ConfigMap `stockhub-deployment` records SHA
  `2c43ca9927a0784838d6c512fafe129ed7439d3b` and the published digest; Job
  `stockhub-migrate-2c43ca9927a0` completed; deployment `stockhub` is
  Available; the pod is Ready; `http://127.0.0.1:8080/ready` returned ready.
- Hosted staging and production jobs in run 38118474661 were skipped. The
  separate dev Compose target remains at `21549b36342cb031dc94bc79b28ee502be5dbf9c`.
- `scripts/check-milestone-closure.sh docs/reports/milestone-7-pricing-closure.md`
  and `scripts/check-tracking-status.sh` passed after GitHub closure.

## Closure verdict

PASS. Pricing rules is merged to `main` at `2c43ca9`, all four milestone
issues are closed, GitHub milestone 8 is closed with zero open issues, CI and
the published image pass their delivery gates, and the persistent local
Kubernetes target runs the verified SHA and digest with migration and readiness
evidence. The narrow-viewport automation and hosted-target gaps are concrete
Maintenance follow-ups (#123 and #48), not untracked product deferrals.
