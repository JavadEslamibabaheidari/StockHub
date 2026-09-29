# StockHub

StockHub is delivered through a public GitHub repository. See
[docs/project-status.md](docs/project-status.md) for the current milestone,
merged code, deployment verification, and closure state.

## Repository conventions

- `dev` is the intended protected integration branch for issue pull requests.
  The local sync timer deploys merged commits to the dev stack once that branch
  and its protections are configured.
- `main` receives milestone promotion pull requests. Annotated version tags on
  `main` promote the same local image through staging and prod after the release
  checks pass.
- Product mockups are reference material under `docs/mockups/`.
- The GitHub lifecycle plan is [docs/plans/github-infrastructure-plan.md](docs/plans/github-infrastructure-plan.md).
- Deployment policy is documented in [docs/plans/deployment-environments.md](docs/plans/deployment-environments.md).
- Issue metadata rules are documented in [docs/plans/github-issue-hygiene.md](docs/plans/github-issue-hygiene.md).

## Current status

The latest merged Dashboard commit is running on the local dev stack. Staging
and prod await a validated milestone tag. The implementation gate and closure
process are documented in
[docs/ai-development-workflow.md](docs/ai-development-workflow.md).

## Backend checks

```bash
dotnet restore StockHub.sln
dotnet build StockHub.sln --no-restore
dotnet test StockHub.sln --no-restore
```

## Frontend checks

```bash
cd frontend
npm ci
npm run lint
npm test -- --run
npm run contract:check
npm run build
```
