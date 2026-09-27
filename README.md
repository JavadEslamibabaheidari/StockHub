# StockHub

StockHub is being prepared for issue-by-issue delivery through a public GitHub
repository. Product behavior and application architecture are being defined in
the separate product track.

## Repository conventions

- `main` is the protected integration branch.
- Work is delivered through focused issues and pull requests.
- Product mockups are reference material under `docs/mockups/`.
- The GitHub lifecycle plan is [docs/plans/github-infrastructure-plan.md](docs/plans/github-infrastructure-plan.md).

## Current status

The frontend Cover runtime has been restored through Milestone 0. Access
backend and frontend implementation remains gated by the Cover closure and the
normal branch/PR workflow.

The accepted architecture is documented in
[docs/plans/architecture-baseline.md](docs/plans/architecture-baseline.md),
with ADRs merged in PR #14 according to the current project context. The
implementation gate and closure process are documented in
[docs/ai-development-workflow.md](docs/ai-development-workflow.md).
