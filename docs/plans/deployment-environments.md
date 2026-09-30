# Local deployment environments and milestone promotion

Status: protected `dev` and the localhost dev stack verified on 2026-09-30.
PR #71 passed required checks and merged as `1788e945c1902f4fdf229805f3371bb5453e404a`.
The timer deployed that exact commit to dev; PostgreSQL and app containers are
healthy, `/ready` returned ready, and the frontend returned HTTP 200. The dev
push image scan, Compose smoke, and image publication passed. Tagged staging
and prod promotion awaits a completed milestone.

## Release flow

1. Feature and fix branches open pull requests into protected `dev`. Required
   checks pass before merge. A closed but unmerged PR does not deploy.
2. The local sync timer fetches `origin/dev` every two minutes. When its commit
   changes, it builds that exact commit and deploys it to the dev Compose stack.
3. When the milestone implementation and pre-promotion evidence are ready on
   `dev`, merge `dev` into protected `main` through a milestone PR. Verify the
   merged code and checks on `main`. The closure report may remain `IN PROGRESS`
   while this evidence is collected.
4. Resolve every milestone issue, close the GitHub milestone, and merge the
   final `PASS` closure report after its live milestone check succeeds. Create
   an annotated `v0.<milestone-number>.0` tag on that final `main` commit.
5. The timer accepts only new remote milestone tags that are annotated, point
   to `main` history, contain exactly one passing closure report, and have a
   closed GitHub milestone with zero open issues. It builds
   the tagged commit, deploys it to staging, waits for `/ready`, then deploys
   the same local image to production. A staging failure stops promotion.

The Compose project names are `dev-stockhub`, `staging-stockhub`, and
`prod-stockhub`. Each is the local equivalent of a namespace and has its own
PostgreSQL and Data Protection key volumes, random database password,
configuration file, and port. All ports bind to `127.0.0.1`: dev uses 8081,
staging uses 8082, and prod uses 8083. These are local lifecycle environments
on one PC. They are unavailable while the PC or Docker Desktop is off. Public access,
TLS, high availability, off-machine backups, and production operations remain
future hosting work.

## One-time local setup

```bash
bash scripts/setup-local-environments.sh
systemctl --user enable --now docker-desktop.service
bash scripts/sync-local-deployments.sh init
bash scripts/install-local-sync-timer.sh
```

The setup script writes separate mode-600 `dev.env`, `staging.env`, and
`prod.env` files in `~/.config/stockhub/`. Edit each file independently for
its port, password, public base URL, ASP.NET environment, cookie setting,
Google OAuth credentials, and SMTP settings. The optional provider settings
are blank until configured, and each environment can use different credentials.
The sync script writes deployment state under `~/.local/state/stockhub/`. `init`
records historical release tags without deploying them, then deploys current
`dev`. `init` also unlocks the timer only after the protected branch is ready.
The timer handles new commits and tags. To trigger a check immediately,
run `bash scripts/sync-local-deployments.sh sync` or start the user service with
`systemctl --user start stockhub-local-sync.service`. Keep Docker Desktop running
and the user systemd manager active. The deployment script selects the
`desktop-linux` Docker context by default, and both containers restart after
Docker Desktop restarts. The timer can be disabled with
`systemctl --user disable --now stockhub-local-sync.timer`.

To inspect the three stacks:

```bash
docker ps --filter label=com.docker.compose.project=dev-stockhub
docker ps --filter label=com.docker.compose.project=staging-stockhub
docker ps --filter label=com.docker.compose.project=prod-stockhub
curl --fail http://127.0.0.1:8081/ready
curl --fail http://127.0.0.1:8082/ready
curl --fail http://127.0.0.1:8083/ready
```

Production deployment takes a `pg_dump` backup before migrations in
`~/.local/state/stockhub/backups/`. A failed health check attempts to restore
the previous application image. Migrations are forward-only, so a database
restore may still be needed after a failed release. Do not move or reuse a published tag; fix on
`dev`, promote through `main`, and create a new version.

## GitHub and CI requirements

- Create `dev` from current `main`. Protect both branches with PRs, required
  checks, and no direct or force pushes. Normal issue PRs target `dev`;
  milestone promotion PRs target `main`.
- Hosted GitHub Actions run repository, frontend, backend, container smoke, and
  image security checks. No GitHub runner executes on this PC. Branch
  protection is the gate before a merged `dev` commit can be fetched locally.
- The current `main` branch rule still names the former Kubernetes smoke check.
  Update it to `Image scan and Compose smoke` when preparing the first
  `dev`-to-`main` milestone PR. Existing open dependency PRs target `main`
  and still run the old workflow.
- The local timer fetches this public remote without a GitHub token; private
  repositories need read-only credentials.

`Dockerfile`, `docker-compose.yml`, and `scripts/check-compose-smoke.sh` remain
the clean-checkout packaging and CI smoke path. `deploy/compose.local.yml`,
`scripts/deploy-local.sh`, and `scripts/sync-local-deployments.sh` implement the
three local stacks. Future Kubernetes files remain in the repository but are
not part of this local deployment path.
