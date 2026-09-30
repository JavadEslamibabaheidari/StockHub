# Repository agent guidance

Before reporting project status, refresh `main` and `dev` when it exists, then read
`docs/project-status.md`. Verify current milestones and issues on GitHub and
inspect the relevant delivery run. Distinguish code merged, image published,
local deployment verified, and shared staging/production deployment verified.
A PR merged into `dev` is integrated there; it is on `main` only after the
promotion PR merges. Name the branch and source SHA in status claims.
A successful workflow with skipped deployment jobs proves publication only.
If GitHub or the deployment target is inaccessible, mark that part unverified.

For milestone work, treat GitHub issue and milestone state as authoritative.
`docs/reports/` records evidence, not a substitute for live state. A milestone
is closed only when its GitHub milestone is closed with zero open issues and
its report is `PASS`. Run `scripts/check-tracking-status.sh` after status
changes. The status definitions and evidence links are in
`docs/project-status.md`.

## Containerization is required for new projects

Every new application, service, or deployable project added to this repository
must be containerized as part of its initial implementation. Do not consider a
project implementation-ready until it includes:

- a production-oriented `Dockerfile` in the project root, or in the project's
  own directory when the repository contains multiple deployable projects;
- a `.dockerignore` that excludes local and generated files that are not needed
  in the image;
- documented local container startup instructions, using `docker compose` when
  the project has supporting services such as a database;
- a container health check or an equivalent readiness verification; and
- an automated CI smoke/build check that builds the image and verifies the
  container can start successfully.

The Dockerfile must build from a clean checkout, install pinned or lockfile-
controlled dependencies, expose the required runtime port, and run the
production entrypoint. Deployment-specific secrets and environment values must
be injected at runtime through the hosting platform or CI/CD secret store; do
not bake them into the image or commit them to the repository.

For milestone work, record the Dockerfile, container configuration, smoke test,
and CI evidence in the plan's deployment/configuration section and in the
milestone closure report. If containerization is intentionally deferred, record
an explicit approved deviation and a tracked follow-up before implementation
starts.
