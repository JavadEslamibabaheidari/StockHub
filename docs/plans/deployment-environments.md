# Deployment environments

Status: local container packaging implemented; shared hosting targets and
secrets remain intentionally unselected

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

The hosting decision must identify the following values before deployment
automation is added:

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

CI is implemented for frontend, backend, PostgreSQL integration, and container
smoke validation. CD remains out of scope until the target and ownership
decisions above are approved. This prevents a workflow from accidentally
deploying an arbitrary branch or an unreviewed configuration.
