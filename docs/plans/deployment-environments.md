# Deployment environments

Status: local container packaging and isolated self-hosted Kubernetes CD configured;
shared cluster credentials, domains, and secrets pending (issue #48)

## Environments

StockHub uses three lifecycle environments:

- `local`: developer-only execution with no shared GitHub secrets.
- `review-staging`: the first shared deployment target after a hosting target
  is selected. Deployments must come from `main` or an approved pull request
  workflow, and must expose a rollback path.
- `production`: a protected GitHub environment. Production deployment must be
  explicitly approved, limited to `main`, and recorded with the deployed
  commit, migration status, health verification, and rollback owner.

## Required configuration before deployment

The hosting decision must identify the following values before a shared
deployment can run:

- hosting provider and region;
- frontend and backend runtime targets;
- PostgreSQL connection and migration ownership;
- domain, TLS, and cookie settings;
- observability and alert destinations;
- rollback mechanism and recovery-time target; and
- named owners for staging and production approval.

Secrets belong to GitHub Environments or the hosting provider's secret store,
never to the repository. Expected secret categories are database credentials,
application signing/encryption keys, external integration credentials, and
observability ingestion credentials. Exact names remain deferred until the
hosting target is approved.

## Current delivery rule

The repository now provides a local/reviewable Compose stack through
`Dockerfile`, `docker-compose.yml`, and `scripts/check-compose-smoke.sh`. It
builds the React bundle into the ASP.NET Core image, starts PostgreSQL with a
health check, applies the Access migration, and exposes the app at one origin
(`http://localhost:8080` by default). The local Compose password is a
development default only; shared environments must override all database and
cookie settings through environment or secret management.

CI covers frontend, backend, PostgreSQL integration, container smoke, image
vulnerability reporting, and ephemeral kind rollout on hosted runners. Main
publishes the immutable digest to GHCR. A separate private
`StockHub-Deployment` GitHub Actions task polls successful `main` publish
runs and uses its dedicated self-hosted runner to deploy to persistent kind
on this PC. The public repository has no self-hosted runner. Review-staging
is disabled until a hosted target exists. A future manual production dispatch
takes a successful staging run ID, retrieves its digest record, and requires
the protected `production` environment. Shared activation is issue #48.

## Persistent local Kubernetes target

`scripts/setup-local-kind.sh` creates `kind-stockhub-local`, namespace
`stockhub-local`, a generated local-only PostgreSQL secret, and a 2 Gi PVC.
`deploy/kind-local.yaml` maps NodePort 30080 to `localhost:8080`. The private
deployment task runs on a dedicated `stockhub-local` self-hosted runner: it
selects only a successful public `main` publish run, pulls that digest, loads
it into kind, runs the same migration and rollout script, and probes `/ready`.
The runner and Docker Desktop must be online; GitHub queues deployments while
the runner is offline. Public PR workflows cannot target this runner.

On the configured local PC, the official GitHub runner is registered in the
private deployment repo as `stockhub-local-pc` with the `stockhub-local` label
and runs as the user service `stockhub-runner.service`. The private repository
currently has only the owner as a collaborator, and its deployment job checks
for `main`. GitHub Free does not provide branch protection for this private
repository. To check or restart the local delivery target:

```bash
systemctl --user status stockhub-runner.service
systemctl --user restart stockhub-runner.service
kind get clusters
kubectl --context kind-stockhub-local -n stockhub-local get pods,service,pvc
curl --fail http://localhost:8080/ready
```

Runner registration tokens are short-lived and must be obtained through
`gh api` when reinstalling; no token or kubeconfig is stored in this repo.
The user service depends on this PC's logged-in Docker Desktop session. If the
PC is off or Docker is stopped, the GitHub deployment job waits or fails and
can be rerun after the local target is restored.

To inspect the app locally:

```bash
curl --fail http://localhost:8080/ready
```

The kind cluster and database PVC persist until the cluster is explicitly
deleted. Back up needed data before deleting it. This local target does not
expose a public production service.

## Kubernetes delivery contract

The root `Dockerfile` is the sole StockHub app image; it contains the API and
frontend. PostgreSQL is a supporting service. `deploy/k8s/local-postgres.yaml`
is an ephemeral fixture for kind only. Shared environments need a durable
PostgreSQL instance.

Future `review-staging` and `production` GitHub environments require secrets
`KUBE_CONFIG_B64` (base64 kubeconfig with namespace-scoped rights) and
`POSTGRES_CONNECTION`, plus variables `KUBE_NAMESPACE`, `STOCKHUB_HOST`, and
`INGRESS_CLASS`. The cluster also needs an ingress controller, a `stockhub-tls`
certificate Secret in the pre-created namespace, and GHCR image pull access.

The workflow creates or updates the runtime Secret, runs the `--migrate-only`
Job, applies the app Deployment and Service, waits for rollout, configures
Ingress, and probes the public `/ready` route. A failed rollout or probe
restores the previous app image. Database migrations are not automatically
rolled back, so schema changes need a backward-compatible rollout and a
reviewed recovery plan.

The app currently uses one replica and `Recreate` because ASP.NET Data
Protection keys are pod-local. Shared durable keys are required before
scaling or zero-downtime rollout; the current strategy briefly interrupts
availability. Record the deployed digest, health result, and rollback owner
in each environment deployment record.

Local verification from the repository root:

```bash
bash scripts/check-compose-smoke.sh
bash scripts/check-kind-smoke.sh
```

The kind script starts Kubernetes through Docker, runs an ephemeral database,
and exercises the same migration and rollout script used by CD. Compose
separately checks the local stack; Docker Compose does not itself run
Kubernetes. Both checks run in GitHub CI before publishing. The image check
also runs weekly to catch newly disclosed vulnerabilities;
`.github/dependabot.yml` proposes Docker, GitHub Actions, npm, and NuGet
updates through the same PR gates.

## Requirement for new projects

Every new deployable project must be containerized before it is considered
implementation-ready. Its initial delivery must include a production-oriented
`Dockerfile`, a `.dockerignore`, documented container startup, a health or
readiness verification, and a CI build/smoke check. Multi-service projects must
also provide a Compose or equivalent local orchestration definition. Runtime
secrets and environment-specific settings must be supplied by the deployment
environment, never embedded in the image.

The project plan and milestone closure report must link the container files and
verification evidence. Any exception requires an approved deviation and a
tracked follow-up before implementation starts.
