# Production delivery gate before Dashboard

Status: implementation in progress; local self-hosted CD and GitHub gates pending
Tracking: [issue #47](https://github.com/JavadEslamibabaheidari/StockHub/issues/47), milestone `2 Dashboard`

## Goal and scope

Before Dashboard implementation advances, make the existing StockHub app image
the tested artifact that GitHub Actions can publish and deploy to persistent
Kubernetes on the owner's local PC.
This is a delivery prerequisite within the mockup-led roadmap, not another
product milestone. Cover is closed. Access was later reopened after live
review, so its earlier PASS report and tag are historical records; current
status is in [project status](../project-status.md).

The checkout has one deployable project: the root Dockerfile builds the React
frontend into the ASP.NET Core API image. PostgreSQL is a supporting service
with a Compose image and an ephemeral kind test fixture. No second app
repository or frontend-only production image is required.

## Deliverables and acceptance

| Work | Deliverable | Required evidence |
|---|---|---|
| Image | Root `Dockerfile` and `.dockerignore`, non-root runtime, `/health` and database `/ready` | Clean image build and Compose startup/route smoke |
| Database | `--migrate-only` entry point and Kubernetes Job before application rollout | Migration Job completes against fresh PostgreSQL |
| Local Kubernetes | App Deployment, Service, optional TLS Ingress, local PostgreSQL fixture, deployment and kind smoke scripts | Kind rollout 1/1 and HTTP/API smoke pass; cluster cleaned up |
| CI security | Image scan with SARIF upload and fixable HIGH/CRITICAL gate; weekly rescan and dependency update PRs | Security report visible on GitHub and no gating findings |
| Registry | Main-only GHCR publish tagged by source SHA; retain immutable digest | Published digest recorded by the workflow |
| Local CD | Private `StockHub-Deployment` Actions task with the dedicated self-hosted runner; persistent kind with PostgreSQL volume and localhost port; pull published digest, migrate, roll out, verify `/ready` | Successful private Actions run, deployed digest, and live `http://localhost:8080` |
| Future shared CD | Staging and production workflow definitions with environment-scoped secrets and protected digest promotion | Issue #48 remains open until hosted cluster deployment runs pass |
| Governance | Require `Image scan and local Kubernetes smoke` on main, synchronize issue #47 and this plan, tag closed milestones | Branch protection, issue and milestone state, and annotated tags verified |

## Dependencies and decisions

- The local PC must be on with Docker Desktop and the dedicated GitHub runner
  running. The runner belongs only to the private `StockHub-Deployment` repo.
  Public StockHub PR and main workflows use hosted runners; the private task
  polls successful protected-main publish runs and deploys their image. The
  kind cluster and PostgreSQL volume persist until explicitly deleted.
- A future shared target needs a pre-created Kubernetes namespace, restricted
  kubeconfig, durable PostgreSQL, an ingress controller, DNS, TLS Secret,
  GHCR pull access, and an owner for staging and production rollback.
  GitHub environment secrets and variables supply these at runtime.
- Future production must use the same digest that a successful staging run recorded.
  No arbitrary branch deployment or mutable-tag rebuild is allowed.
- The current app uses one replica and `Recreate`: Data Protection keys are
  pod-local, so scaling would invalidate cookie sessions. This causes a short
  deployment interruption. Shared durable key storage and zero-downtime
  rollout are a follow-up before horizontal scaling.
- Migration rollback is not automatic. Schema changes must remain compatible
  with the preceding image and have a reviewed recovery procedure.
- GitHub Actions cannot prove a shared deployment until environment values
  and cluster access are configured. Issue #48 tracks that activation. Issue
  #47 requires a real self-hosted local deployment run before closure.

## Verification sequence

1. Run backend and frontend CI-equivalent checks.
2. Build and smoke the image with `scripts/check-compose-smoke.sh`.
3. Scan the final image and resolve gating findings.
4. Run `scripts/check-kind-smoke.sh` through Docker and verify migration,
   rollout, readiness, routes, and cleanup.
5. Register the dedicated runner, create the persistent local kind cluster,
   and verify the same published image path on localhost.
6. Merge the focused PR after CI and branch protection pass. Main publishes
   the digest; the private deployment task selects a successful publish run
   and deploys that digest to local kind.
7. Record the run link, digest, and live URL in issue #47. Shared staging and
   production activation follows issue #48 when a hosted cluster exists.

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
