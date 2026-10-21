# M14 issue #132 business alert delivery evidence

Status: implementation and isolated local verification complete; PR #182
merged to `main` at `bdd439d8e33c50ada05341a028e9c537890dc3e5` and issue #132
closed. Main run [39500050827](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/39500050827)
published digest `sha256:e73cef89718f4143085d13228816d9282e67403e2a78457bd94ce6fcc8363d97`
for that source SHA. Deployment PR [#5](https://github.com/JavadEslamibabaheidari/StockHub-Deployment/pull/5)
fixed deletion/recreation of image-pull-blocked migration Jobs and merged to
Deployment `main` at `7c524bbf5891c90336639809b7cfb2ae22b2a5df`. Persistent
local run [39503468842](https://github.com/JavadEslamibabaheidari/StockHub-Deployment/actions/runs/39503468842)
succeeded from that commit, recorded the exact StockHub SHA and published
digest, completed the migration Job and 1/1 rollout, and uploaded its
deployment record. The log showed one transient `curl: (52) Empty reply from
server` while the container started; the readiness-gated workflow completed
successfully. An earlier overlapping scheduled run selected stale source
`0d8ec2eebe0f0242503ddd2cc787cd8cce7deb3e` and failed to pull its migration
image; it did not deploy or record a digest.

## Implementation

- Alert evaluation reads product and reservation state through the owning
  `IProductStore` and `IOrderStore` interfaces. Workspace members are taken
  from the Access module; delivery records and per-workspace settings live in
  Reports-owned PostgreSQL tables from migration `010_business_notifications`.
- In-app notifications are durable and scoped to workspace and recipient.
  Instant, hourly, and daily delivery frequencies have incident/bucket
  deduplication. The worker claims jobs with a lease, supports parallel-safe
  `SKIP LOCKED` dispatch, and records pending, delivered, or dead status.
- SMTP alerts are enabled only when host, port, sender, and public base URL are
  configured. Slack incoming hooks are workspace scoped, HTTPS-only, and
  encrypted using ASP.NET Data Protection. The local Compose project persists
  its Data Protection key ring. Channel state remains unavailable until real
  runtime configuration is supplied.
- Failures use exponential retry (30 seconds initially, capped at one hour),
  and become dead after eight attempts. In-app insertion and job completion are
  atomic. External channels are at-least-once if the provider accepts a message
  while the database completion write fails.
- Sync-failure delivery stays explicitly unavailable until Platforms exposes
  persisted synchronization status.

## Verification

Run from the issue branch checkout on 2026-10-20:

- `cd frontend && npm test -- --run`: 68 tests passed.
- `cd frontend && npm run contract:check`: Access and Reports generated
  OpenAPI/client checks passed.
- `cd frontend && npm run build`: TypeScript and production Vite build passed.
- `cd backend && STOCKHUB_POSTGRES_TEST_CONNECTION='Host=127.0.0.1;Port=5542;Database=stockhub;Username=stockhub;Password=stockhub-ci'
  dotnet test tests/StockHub.Api.Tests/StockHub.Api.Tests.csproj --no-restore
  -m:1 /nodeReuse:false`: 53 tests passed with PostgreSQL-backed tests enabled.
- Disposable Compose project `stockhub-m14-132` rebuilt from the edited source
  and started healthy on `127.0.0.1:8184` with an isolated PostgreSQL volume.
- Signed up a disposable test account, created a real workspace and product,
  enabled instant low-stock in-app delivery, and observed the persisted
  notification naming `E2E Low Stock Product`. Mark-read returned HTTP 204 and
  the next list response reported zero unread notifications.
- A second account attempting to read the first workspace's notifications
  received HTTP 403.
- In the browser, the Reports Alert settings Save changes control persisted a
  changed low-stock threshold across page reload. Slack Connect, Replace, and
  Disconnect each worked with a fabricated test webhook URL; after
  Disconnect, the screen again showed Slack unavailable. The test URL was
  removed and no Slack request was sent.
- After another worker evaluation, PostgreSQL still contained one in-app job
  and one notification for the active low-stock incident, confirming repeat
  evaluation did not enqueue duplicates.
- With an intentionally unreachable SMTP host at `127.0.0.1:1`, the email job
  remained pending with attempt count 1 and a 30-second retry delay, then
  attempt count 2 with another retry scheduled. It was not counted as
  delivered. The independent in-app job was delivered.
- PostgreSQL inspection confirmed migration `010_business_notifications`, one
  delivered in-app job and notification for the low-stock incident, and the
  failed email retry state.

## Deployment limits and remaining evidence

This is non-production local Docker/PostgreSQL and local kind evidence. It does
not prove a real SMTP provider, Slack workspace, shared staging, or production
delivery. The implementation, publication, digest-specific local deployment,
and issue reconciliation requirements are complete. Issue #132 remains closed
for its non-production channel acceptance; issue #47 is the next item in the
M14 dependency sequence.
