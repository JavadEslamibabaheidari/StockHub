# Local delivery gate for Dashboard

Status: local Compose dev deployment verified; automatic branch and tag promotion pending
Tracking: [issue #47](https://github.com/JavadEslamibabaheidari/StockHub/issues/47), milestone `2 Dashboard`

## Goal and scope

Run StockHub on the owner's PC as three isolated local environments. GitHub
Actions builds, tests, scans, and publishes the image; the PC fetches protected
`dev` commits and milestone tags and deploys without a self-hosted runner.
This is a delivery requirement within the Dashboard milestone, not another
product milestone. Cover is closed. Access was later reopened after live
review, so its earlier PASS report and tag are historical records; current
status is in [project status](../project-status.md).

The checkout has one deployable project: the root Dockerfile builds the React
frontend into the ASP.NET Core API image. PostgreSQL is a supporting service
with a Compose image. The older kind test fixture remains for future work; no second app
repository or frontend-only production image is required.

## Deliverables and acceptance

| Work | Deliverable | Required evidence |
|---|---|---|
| Image | Root `Dockerfile` and `.dockerignore`, non-root runtime, `/health` and database `/ready` | Clean image build and Compose startup/route smoke |
| Database | `--migrate-only` entry point before app startup; separate PostgreSQL volume per environment | Migration completes against fresh PostgreSQL; production backup taken before migration |
| Local environments | `dev-stockhub`, `staging-stockhub`, and `prod-stockhub` Compose projects, localhost ports, independent private config files, and persistent session keys | Each project validates, has its own database and Data Protection volume, and exposes only its localhost port |
| CI security | Image scan with SARIF upload and fixable HIGH/CRITICAL gate; weekly rescan and dependency update PRs | Security report visible on GitHub and no gating findings |
| Registry | Protected `dev` and milestone-tag GHCR publish tagged by source SHA; retain immutable digest | Published digest recorded by the workflow; local deployment independently builds the exact source commit |
| Local CD | Poll protected `dev` and new annotated milestone tags from the PC; build the exact commit once, migrate, start, and verify `/ready` | Latest merged Dashboard commit live on localhost:8081; automated branch/tag path demonstrated |
| Staging and prod | Promote a passing milestone tag to staging then prod using the same local image | Both stacks healthy on localhost:8082 and :8083; failed staging blocks prod |
| Future shared CD | External hosting, TLS, secrets, backups, and protected promotion | Issue #48 remains open until a hosted deployment is selected and verified |
| Governance | Require `Image scan and Compose smoke` on `dev` and `main`, synchronize issue #47 and this plan, tag closed milestones | Branch protection, issue and milestone state, and annotated tags verified |

## Dependencies and decisions

- The local PC must be on with Docker Desktop and the user sync timer running.
  Public StockHub PR and publishing workflows use hosted runners only. No
  GitHub job executes on the owner's PC. The PC fetches the public repository
  using read-only Git access and builds the exact protected commit locally.
- The three private config files live under `~/.config/stockhub/` as `dev.env`,
  `staging.env`, and `prod.env`, each with a distinct password and port. They
  must remain outside Git and mode 600. Compose project names prefix every
  resource with `dev-`, `staging-`, or `prod-`.
- A future shared target needs a pre-created Kubernetes namespace, restricted
  kubeconfig, durable PostgreSQL, an ingress controller, DNS, TLS Secret,
  GHCR pull access, and an owner for staging and production rollback.
  GitHub environment secrets and variables supply these at runtime.
- Local prod must use the same commit image built for staging. No arbitrary
  branch deployment or mutable tag rebuild is allowed.
- Local Compose runs one app instance per project and persists its Data
  Protection keys in a project-specific volume. Deployment may briefly
  interrupt requests. Shared key storage and zero-downtime rollout remain a
  follow-up for future hosted multi-replica deployment.
- Migration rollback is not automatic. Schema changes must remain compatible
  with the preceding image and have a reviewed recovery procedure.
- Issue #48 tracks future shared hosting. Issue #47 requires a real local
  Compose deployment and the automated protected branch/tag sync before closure.

## Verification sequence

1. Run backend and frontend CI-equivalent checks.
2. Build and smoke the image with `scripts/check-compose-smoke.sh`.
3. Scan the final image and resolve gating findings.
4. Run `scripts/setup-local-environments.sh`, confirm private config modes,
   and validate the three distinct Compose project names and localhost ports.
5. Build the latest protected commit and verify local dev migration, readiness,
   and frontend routes. On 2026-09-29, commit `146191eeed063c5165688f9f002a6f27c7d1fab5`
   passed this check on localhost:8081.
6. Create and protect `dev`, publish the local sync scripts, initialize the
   timer, and demonstrate a merged PR automatically updating dev.
7. Close the GitHub milestone with zero open issues, then tag its passing
   `main` commit; prove staging health and prod promotion
   of the same local image. Record commit, image ID, and live URLs in issue #47.
   Shared hosting activation follows issue #48.

## Milestone tagging

After each milestone closes with all issues and PRs resolved, checks green,
closure report PASS, and `main` synchronized, create one annotated
`v0.<milestone-number>.0` tag. Backfill closed Cover and Access tags only
after checking their historical closure and commit targets. Dashboard gets
`v0.2.0` only after its own closure; issue #47 must be resolved first.

## Previous milestone hook

Access was once closed after PRs #45 and #46, then reopened after live review.
Issues #27, #35, and #65 currently block its closure. Dashboard issue #47
remains open independently; neither milestone should inherit the old Access
PASS verdict.
