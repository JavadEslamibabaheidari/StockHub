# Local container packaging

The repository provides a local/reviewable Compose stack through `Dockerfile`,
`docker-compose.yml`, and `scripts/check-compose-smoke.sh`.

The multi-stage image builds the React bundle into the ASP.NET Core image,
starts PostgreSQL with a health check, applies the Access migration, and
exposes the application at one origin (`http://localhost:8080` by default).
Use `STOCKHUB_PORT=18080 bash scripts/check-compose-smoke.sh` when the default
port is occupied.

The Compose password is a development default only. Shared environments must
override database credentials, connection strings, and cookie settings through
environment or secret management. Production hosting, TLS, observability,
rollback, and deployment ownership remain outside this local packaging issue.
