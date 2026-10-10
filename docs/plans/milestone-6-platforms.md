# Milestone 6 Platforms

## Goal

Deliver the mockup-led Platforms milestone as hash-addressable screens:

- `6.1 Platforms`
- `6.2 Add platform picker`

## Scope

- Render the Platforms owner screen in the existing React/Vite app shell.
- Preserve the StockHub sidebar, global search, sync alert, owner account
  controls, warm visual language, responsive behavior, and previous
  Access/Dashboard/Inventory/Orders/Reservations routes.
- Make every visible Platforms control functional in the browser: platform
  search, oversell protection, pause sync, disconnect, settings, reconnect,
  add platform entry points, workspace/account/status panels, appearance
  toggle, notifications, upgrade handoff, and sidebar collapse.
- Render the Add platform picker modal with Zalando, ePRICE, and MediaWorld
  connect actions plus Done, close X, Escape, and backdrop handling.
- Represent connector state with deterministic preview data until durable
  platform credential storage, OAuth, and marketplace sync workers exist.

## Non-goals

- Durable marketplace credential storage.
- Real marketplace OAuth or seller authorization.
- Real stock or price publication to Amazon, Unieuro, Euronics, eBay, Zalando,
  ePRICE, or MediaWorld.
- Real connector health checks, webhooks, sync jobs, or retry queues.
- Pricing-rule automation, Reports workflows, Team management, Settings
  billing, and full Dark mode coverage beyond the already-visible theme toggle.

## Start Gate

- Previous milestone hook: Milestone 5 Reservations is closed on GitHub with
  zero open issues. `docs/reports/milestone-5-reservations-closure.md` is
  `PASS`.
- Branch and SHA basis: this plan starts from `origin/main`
  `0acc08f6f377871e1edf184981b7304dd95616f8` on branch
  `codex/milestone-6-platforms`; `origin/dev` is
  `21549b36342cb031dc94bc79b28ee502be5dbf9c`.
- Roadmap source: `docs/plans/mockup-led-roadmap.md` defines Milestone 6 as
  `6 Platforms` with screens `6.1 Platforms` and `6.2 Add platform picker`.
- Visual source:
  `/home/javad/Pictures/Screenshots/Screenshot From 2026-10-10 23-58-49.png`
  and
  `/home/javad/Pictures/Screenshots/Screenshot From 2026-10-10 23-59-10.png`.
- Live GitHub milestone: milestone number 7, `6 Platforms`, is open.
- GitHub issue tracking: issues #111, #113, #112, and #110 cover screen
  delivery, visible interactions, modal delivery, and closure evidence.
- Implementation-blocking decisions: none. Durable connector/OAuth behavior is
  explicitly out of scope for this milestone and will be tracked as maintenance
  only if a visible screenshot control cannot receive a useful local behavior.

## Implementation Plan

| ID | Issue | Screen | Deliverable | Acceptance |
|---|---|---|---|---|
| P1 | [#111](https://github.com/JavadEslamibabaheidari/StockHub/issues/111) | 6.1 Platforms | Owner platforms screen with connected/error platform cards, oversell protection banner, add tile, and existing shell chrome | Route `#platforms` renders the screenshot-aligned screen without leaking screenshot placeholder identity in unauthenticated preview |
| P2 | [#113](https://github.com/JavadEslamibabaheidari/StockHub/issues/113) | 6.1 Platforms | Browser behavior for page controls: search, status panels, oversell toggle, pause/disconnect/settings/reconnect, navigation, theme, notifications, account, upgrade, and collapse | Every visible page control responds visibly, routes to its owner, or has a tracked maintenance issue |
| P3 | [#112](https://github.com/JavadEslamibabaheidari/StockHub/issues/112) | 6.2 Add platform picker | Modal with Zalando/ePRICE/MediaWorld connect actions, Done, close X, Escape, and backdrop handling | Both add entry points open the modal; modal controls update local state or close predictably |
| P4 | [#110](https://github.com/JavadEslamibabaheidari/StockHub/issues/110) | Previous milestones and closure | Regression protection, browser smoke, knowledge/status/report updates, tracking check, PR/CI evidence, and milestone closure | Previous milestones still pass, closure report is `PASS`, all Milestone 6 PRs/issues are closed, and deployment/image claims are separated by evidence type |

## Execution Order

1. **Critical prerequisite:** P1 establishes the route, screen structure, and
   reusable local state. P2/P3 depend on this file structure.
2. **High implementation:** P2 wires visible page controls. It should land
   before the modal because it owns both add entry points.
3. **High implementation:** P3 adds modal-specific behavior and accessibility.
4. **Critical verification:** P4 runs regression checks, browser smoke, docs,
   tracking synchronization, and closure.

P1-P3 all touch the same frontend files, so parallel implementation would
increase conflict risk. Parallel sessions may be used for bounded review,
browser verification, or closure evidence collection after the integrated UI
exists.

## Testing

- Frontend SSR tests for `#platforms`, visible platform data, and previous
  route preservation.
- Frontend interaction tests where practical for search, oversell toggle,
  platform actions, and modal close/connect behavior.
- `cd frontend && npm test`.
- `cd frontend && npm run build`.
- `dotnet test StockHub.sln` to preserve previous backend-backed milestones.
- Browser smoke for `#platforms`, visible control interactions, Add platform
  modal behavior, and previous milestone route handoffs when a local dev server
  is available.
- `scripts/check-milestone-closure.sh docs/reports/milestone-6-platforms-closure.md`
  and `scripts/check-tracking-status.sh` before closure.

## Documentation

- Update `docs/knowledge/frontend.md` with Platforms routes, behavior, and
  explicit connector/OAuth deferrals.
- Update `docs/project-status.md` after code merge, publication, deployment, and
  closure evidence are known.
- Create `docs/reports/milestone-6-platforms-closure.md` during P4 only, with
  live GitHub issue/PR/milestone state and exact branch/SHA evidence.

## Deployment And Configuration

Milestone 6 adds no new deployable project, service, database migration, secret,
or container. The existing `Dockerfile`, `.dockerignore`, `docker-compose.yml`,
readiness endpoint, CI workflows, and Kubernetes delivery path remain the
containerization and deployment configuration.
