# Unparented milestone execution plan

Status: active; M12 is closed in GitHub with zero open issues and a PASS report. The final `Unparented milestone` is GitHub milestone #14. Issue #35 implementation PR #170 is merged to main at `39eb728ad958b2f01e9cc4894987614e16c4e873`; issue #65 PR #172, from source branch `codex/issue-65-signup-recovery` at `8208185d54df358181640504cf808c81774213ff`, is merged at main SHA `d27c1339d6ee993db13f79e590745bff2de13d45`; issue #123 PR #174, from source branch `codex/issue-123-responsive-main` at `bff2b87d5bd4f602a1e0dd2d6d0cea50bfcd2a59`, is merged at main SHA `892fdfb9ddf78ed1287599ebd527ad2eb6373a14`. Issue #132 PR #182 is merged from `codex/issue-132-business-alerts` at source `ff5ac2197e3b0d8774d2b22cc1f4cce4970bebe3` to main at `bdd439d8e33c50ada05341a028e9c537890dc3e5`; its issue is closed after PostgreSQL-backed non-production in-app delivery, recipient isolation, retry/deduplication, and Reports browser-control verification. Main delivery run [39500050827](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/39500050827) published `sha256:e73cef89718f4143085d13228816d9282e67403e2a78457bd94ce6fcc8363d97`. Deployment PR [#5](https://github.com/JavadEslamibabaheidari/StockHub-Deployment/pull/5) fixed stale image-pull-blocked migration Jobs and merged to Deployment `main` at `7c524bbf5891c90336639809b7cfb2ae22b2a5df`. Persistent local run [39503468842](https://github.com/JavadEslamibabaheidari/StockHub-Deployment/actions/runs/39503468842) succeeded from that commit, deployed StockHub source `bdd439d8e33c50ada05341a028e9c537890dc3e5` at the published digest, completed the migration Job and 1/1 rollout, and uploaded its deployment record. The log includes a transient `curl: (52) Empty reply from server` during startup; the readiness-gated workflow completed successfully and recorded the exact SHA/digest. A prior stale scheduled run selected `0d8ec2eebe0f0242503ddd2cc787cd8cce7deb3e` and failed to pull its migration image; it did not deploy or record a digest. Issues #35 and #65 remain open for real Google OIDC and email-provider verification; credentials and callback configuration are unavailable. Issue #123's phone-width browser evidence is in `docs/reports/issue-123-phone-pricing-check.md`.

## Goal

Complete all seven persisted follow-up issues assigned to milestone #14, close the milestone with passing evidence, and only then run the user's sign-up-to-end application audit. The static Cover page is explicitly excluded from that audit.

The audit will compare every implemented screen and interaction with the approved mockups and plans, use a real persisted account and data, verify cross-page consistency and synchronization, remove runtime mock/demo data, and resolve every confirmed defect or technical-debt item found within the repository. It will not claim that external providers or hosted deployment work unless a real end-to-end check proves them.

## Scope

- Resolve Access closure gaps tracked by #35 and #65; close Access through #27 only after live verification and its PASS report.
- Complete narrow viewport verification tracked by #123.
- Implement and verify configured, tenant-isolated alert delivery tracked by #132.
- Prove the local three-environment Compose promotion workflow tracked by #47.
- Activate shared hosted Kubernetes staging and production through #48 only when the real cluster, database, DNS/TLS, and runtime credentials exist.
- After milestone #14 is closed, perform the full product audit from sign-up through the last implemented application route, excluding Cover.

## Non-goals

- Redesigning approved mockups or changing numbered product-milestone scope without updating the roadmap and persisted GitHub tracking first.
- Using seeded/sample business values in runtime accounts or presenting an unconfigured integration as successful.
- Claiming shared staging or production readiness from local Compose, ephemeral CI, or local kind evidence.
- Deleting a historical tag or user data to make checks pass.

## Current baseline and start gate

- Previous milestone hook: PASS. `docs/reports/milestone-12-design-system-closure.md` is merged, reports PASS, and GitHub milestone #13 is closed with zero open issues.
- Main baseline before M14 planning: `1652da6bc3426140ac017b8ae1ac593421155b17`. Current refreshed `main`: `bdd439d8e33c50ada05341a028e9c537890dc3e5`. Refreshed `dev`: `21549b36342cb031dc94bc79b28ee502be5dbf9c`.
- GitHub milestone #14 is open and has five open issues: #27, #35, #47, #48, and #65. Issues #123 and #132 are closed with their live acceptance evidence and merged code. The live open issue criteria are the execution contract; this plan records their dependency order and verification.
- **Branch transition constraint:** after the M14 plan merged, main advanced to f58266e23d8210361a8bc0933550d06361d92e80, while dev remained at 21549b36342cb031dc94bc79b28ee502be5dbf9c. They diverge from 8c5fe1f8f5d371f70a1a9a49bb99e4d60b754420: main is 60 commits ahead and dev has 2 unique commits; dev currently requires linear history. Therefore issues #35, #65, #123, and #132 use protected-main PRs from the already-current product baseline. Issue #47 must validate a protected synchronization path that preserves both branches' intended content and updates dev to main before any subsequent product PR targets dev. Do not merge main into dev through a linear-history squash that obscures ancestry or creates a misleading future promotion diff.
- Existing backend and frontend already contain parts of the Access follow-ups. Each issue starts with acceptance validation against current main and changes only verified gaps; existing code presence alone does not close an issue.
- `review-staging` and `production` GitHub environments exist. The repository secret listing currently shows only `STOCKHUB_DEPLOYMENT_DISPATCH_TOKEN`; the required hosted-cluster secret names were absent from both environment secret listings. Secret values are never read or copied into this plan.
- Issue #48 requires an external hosted Kubernetes cluster, durable databases and an encrypted durable ASP.NET Data Protection key ring, public DNS/TLS, restricted kubeconfigs, runtime credentials, and named rollback ownership. These prerequisites are not present in the repository. No cloud vendor or production endpoint is selected in the issue or current project docs. Therefore #48 is an explicit external blocker; independent issues proceed in order, and hosted deployment is not simulated.
- Issue #132 is implemented and merged by PR #182. The durable in-app route passed a real PostgreSQL end-to-end run, browser controls were exercised, email failure/retry and deduplication were verified, and its report is `docs/reports/issue-132-alert-delivery.md`. Real SMTP and Slack success remain unverified; these adapters stay unavailable until runtime credentials are configured. Main publication for merge SHA `bdd439d8e33c50ada05341a028e9c537890dc3e5` passed run #39500050827. Deployment PR #5 merged at `7c524bbf5891c90336639809b7cfb2ae22b2a5df`; persistent local run #39503468842 succeeded and recorded the same source SHA and published digest, with migration and rollout success. The local deployment requirement is reconciled; proceed to issue #47.
- GitHub dev branch and required status checks exist. Open Dependabot PRs #165–#168 and stale PR #156 are reviewed as maintenance inputs; they are not silently merged or counted as milestone evidence.

## Deliverables and acceptance criteria

1. **Access remediation (#35, then #65):** validate and correct onboarding, invitations, Google OIDC, signup, sign-in/out, password recovery, and visible controls against `docs/plans/milestone-1-access-remediation.md`. The verified M12 baseline has a specific onboarding gap: `GET /onboarding` always returns `available`, `IAccessStore` has no read operation for recorded actions, the in-memory writer discards the selection, and the frontend discards its fetch result. Persist and return the selected action without marking it complete, validate supported action keys, and consume the real API response in the frontend. Add API-level tests for authorization, tenant isolation, invitation delivery/acceptance failure paths, Google callback state/claims/errors/session creation, and the onboarding round trip. Google OIDC and email/invitation checks require real configured credentials; no stubbed success is accepted. PostgreSQL integration commands must assert that persistence tests ran, since the existing suite exits early when `STOCKHUB_POSTGRES_TEST_CONNECTION` is absent.
2. **Phone-width browser verification (#123):** PASS in the local 390×844 browser run; evidence and reproduction steps are in `docs/reports/issue-123-phone-pricing-check.md`. PR #174 fixed document-level overflow, and its merge SHA `892fdfb9ddf78ed1287599ebd527ad2eb6373a14` is published and verified on the persistent local target. The issue is reconciled and closed by this evidence update.
3. **Configured business alerts (#132):** PASS for code and non-production acceptance. PR #182 merged source `ff5ac2197e3b0d8774d2b22cc1f4cce4970bebe3` to main at `bdd439d8e33c50ada05341a028e9c537890dc3e5`. Frontend (68 tests), backend with PostgreSQL (53 tests), contracts/build, Compose, Trivy, and Kubernetes rollout checks passed; local Compose verified in-app delivery, retry/deduplication, tenant isolation, and browser controls. Evidence: `docs/reports/issue-132-alert-delivery.md`. Main run #39500050827 published digest `sha256:e73cef89718f4143085d13228816d9282e67403e2a78457bd94ce6fcc8363d97`. Deployment workflow repair PR #5 merged into StockHub-Deployment `main` at `7c524bbf5891c90336639809b7cfb2ae22b2a5df`; persistent local run #39503468842 successfully deployed the exact SHA/digest, completed migration and 1/1 rollout, and uploaded a deployment record. Real SMTP/Slack provider success and hosted targets are not claimed. The deployment/tracking reconciliation is complete; proceed to #47.
4. **Local promotion gate (#47):** prove exact-SHA deployment to dev after a protected merge and same-image staging/prod promotion for an eligible annotated milestone tag. Verify distinct Compose projects, ports, persistent PostgreSQL and Data Protection key volumes, migration/readiness, session cookie validity after container/pod restart, production backup gate, and stop-on-staging-failure behavior. Preserve mode-600 local config and keep secrets out of Git. Do not promote production until all issue-defined automatic gates pass.
5. **Hosted targets (#48):** verify required runtime configuration without exposing values; establish durable encrypted Data Protection key storage shared with every replica; deploy a published immutable digest to real staging; verify migration, public readiness, rollback; promote that same digest through protected production; verify endpoint, backups, observability, and named rollback ownership. If prerequisites remain absent, report the issue as blocked and do not close milestone #14 or issue #48.
6. **Access closure (#27):** verify all Access acceptance criteria against live code, generated contracts, browser/API tests, provider configuration, deployment, knowledge, and docs. Produce `docs/reports/milestone-1-access-closure.md`, run its closure hook, and close Access issues/milestone only after all required real checks pass.
7. **Milestone #14 closure:** update knowledge, status, plans, and a `PASS` closure report with issue/PR/run URLs, exact source SHAs, test output, local/hosted deployment distinctions, deviations, and remaining gaps. Close only with zero open issues and passing tracking/closure hooks.
8. **Post-milestone product audit:** start only after milestone #14 is closed. Create or select an isolated test account/workspace and real persisted records through the sign-up flow. Walk every implemented route and state except Cover, compare screenshots/interactions to `docs/mockups/StockHub Final.html` and relevant PDF pages, exercise every visible button/link/form, verify related pages update from the same persisted records, check Light/Dark and 390×844 plus desktop, and inspect APIs/database responses for seed/sample runtime data. Fix each confirmed defect through tracked PRs and rerun relevant regression checks. Inventory all detected technical debt and resolve it or provide explicit user-relevant evidence for any external blocker; do not report a clean audit while known unresolved debt remains.

## Dependency-ordered issue sequence

1. #35 — fix onboarding action persistence/readback and frontend consumption; add endpoint-level authorization, invitation, Google OIDC and tenant-isolation tests; compare OpenAPI/generated client; verify real provider behavior when credentials are configured.
2. #65 — validate/fix signup, password recovery, visible Access controls, and real browser/API journeys.
3. #123 — establish real 390×844 browser override and execute its phone-width Pricing workflow.
4. #132 — COMPLETE. PR #182 is merged and issue #132 is closed after non-production PostgreSQL in-app delivery, browser controls, tenant isolation, retry, and deduplication evidence. Reconcile its main publish/local deployment run before starting the next issue.
5. #47 — NEXT: verify local Compose automation and exact-source promotion paths; fix script/config gaps and collect real run evidence.
6. #48 — activate hosted staging and production when infrastructure and credentials are available; this is the known external blocker.
7. #27 — final Access acceptance/evidence and Access milestone closure after #35 and #65, plus all live/provider gates.
8. Close milestone #14 only when #27 and every other issue, including #48, is complete with evidence; then start the full app audit.

Each issue is handled alone on a dedicated branch and PR. Because the verified main/dev histories currently diverge, issues #35, #65, #123, and #132 target protected main. Issue #47 owns restoring the documented dev-first workflow; after it verifies a safe sync and promotion route, later work targets protected dev and uses promotion PRs to protected main. Do not rewrite protected refs, bypass PR checks, or start the next issue until the current issue is merged, deployed where required, and its GitHub state and evidence are reconciled.

## Risks and controls

- **External provisioning:** #48 has no selected hosted provider or required environment secrets. Continue independent work; keep #48 open and the whole final milestone open until a real hosted target can be verified.
- **Identity/email configuration:** Google OIDC and password recovery require real credentials and callback/delivery configuration. Preserve safe, explicit unavailable/error states until secrets are installed.
- **Notification delivery:** never equate enqueue, preference, or provider acceptance with user-visible delivery; persist status and test retries and duplicate suppression.
- **Local promotion side effects:** production Compose promotion changes the local production environment. Before running it, verify the issue's exact tag eligibility, backup behavior, and owner authorization; record database backup and rollback evidence.
- **Browser test data:** use a dedicated isolated test workspace, label test records, and remove only those records after evidence collection.
- **Mockup ambiguity:** resolve differences against approved mockup pages and issue acceptance before modifying screens; record any approved deviation in the plan and report.

## Verification strategy

- Backend: `dotnet test backend/StockHub.sln` and PostgreSQL-backed integration runs with the required connection configured; verify tests did not silently skip persistence coverage.
- Frontend: `cd frontend && npm ci && npm test && npm run contract:check && npm run build`.
- Packaging: `bash scripts/check-compose-smoke.sh` and `bash scripts/check-kind-smoke.sh` where Docker/kind are available.
- Browser: actual sign-up and signed-in journeys using the installed browser automation at desktop and 390×844, keyboard interaction, both themes, persisted API data, and per-route screenshot comparison.
- Delivery: required PR checks, main image scan/publication digest, local Compose/local kind evidence, and real shared staging/production run records independently.
- Tracking: `scripts/check-tracking-status.sh` after issue/milestone changes, plus each affected milestone closure script.
- Final audit evidence: route/control matrix, mockup comparison, data-provenance matrix, cross-route synchronization matrix, defect/debt ledger with resolved PRs, and rerun results.

## GitHub tracking

- Milestone: [#14 Unparented milestone](https://github.com/JavadEslamibabaheidari/StockHub/milestone/14).
- Issues in execution order: [#35](https://github.com/JavadEslamibabaheidari/StockHub/issues/35), [#65](https://github.com/JavadEslamibabaheidari/StockHub/issues/65), [#123](https://github.com/JavadEslamibabaheidari/StockHub/issues/123), [#132](https://github.com/JavadEslamibabaheidari/StockHub/issues/132), [#47](https://github.com/JavadEslamibabaheidari/StockHub/issues/47), [#48](https://github.com/JavadEslamibabaheidari/StockHub/issues/48), [#27](https://github.com/JavadEslamibabaheidari/StockHub/issues/27).
- Previous milestone gate: M12 closed with zero open issues and its PASS report merged before this plan branch was created.
- After meaningful implementation changes, update relevant `docs/knowledge/` files and validate planned state before issue closure.

## Start gate result

**Ready for independent work in issues #35, #65, #123, #132, and #47.** Their product/API boundaries, owners, existing plans, and acceptance checks are persisted. **Issue #48 is blocked by external hosted infrastructure and runtime credentials.** Work on it may proceed only to repo-side readiness and safe preflight; deployment acceptance and final milestone closure remain blocked until real resources exist. The dependency ordering prevents this external blocker from stopping work that does not depend on it.
