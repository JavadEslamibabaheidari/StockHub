# Milestone 12 Design system closure report

status: IN PROGRESS
milestone: 12 Design system
github_milestone_number: 13
plan: `docs/plans/milestone-12-design-system.md`; issues #159–#161
mockup_evidence: `docs/mockups/StockHub Design Review.pdf`, page 2
adr_evidence: `docs/decisions/0006-identity-tenancy-and-roles.md`, `0007-frontend-and-api-contracts.md`, and `0008-testing-ci-and-deployment.md`
knowledge_evidence: `docs/knowledge/frontend.md` records shared semantic tokens and formatter use across implemented routes
roadmap_evidence: `docs/plans/mockup-led-roadmap.md` records M12 scope/order and its dependency on M11
code_evidence: PR #162 from `codex/milestone-12-design-system`, source `315664b0a582e481bb31b39106543a2f3780dcd4`, merged to `main` at `8f9a656657e725c1ce4e9ade1777f89e321b557c`
tests_evidence: PR #162 and main run #38182610493 passed 67 frontend tests, lint, TypeScript/Vite build, Access/Reports contracts, backend tests with PostgreSQL, Compose health/route smoke, Trivy and image vulnerability gates, ephemeral Kubernetes migration/rollout, and repository sanity. Browser review covered `#design-system` in both themes at desktop and 390×844; signed-out route review and coverage limitations are recorded below.
configuration_evidence: Existing Dockerfile, `.dockerignore`, Compose health/readiness, and CI container gates were retained. Main run #38182610493 published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:ab1a11b751a32d09e74ea285287af6589ff6ba9a51cce8dd97520f65ca446776` for source `8f9a656657e725c1ce4e9ade1777f89e321b557c`. Persistent deployment run #38182882314 records the same source/digest; migration Job `stockhub-migrate-8f9a656657e7` completed, rollout reached 1/1 available, and `/ready` passed. Review-staging and production promotion were skipped and remain unverified.
github_evidence: PR #162 is merged. GitHub milestone #13 is open with three open issues #159–#161 while evidence is reviewed; final status synchronization is pending.
previous_milestone_hook: PASS — M11 report `docs/reports/milestone-11-dark-mode-closure.md` says PASS; GitHub milestone #12 is closed with zero open issues.

## Alignment matrix

| ID | Source / expected outcome | Implementation or configuration | Test / evidence | Status | Reason, owner, follow-up issue, target milestone |
|---|---|---|---|---|---|
| M12-1 | Shared semantic light/dark tokens, bundled typography, and consistent en-GB presentation | Shared theme-aware tokens, local Caprasimo/Figtree fonts, semantic statuses, and shared number/date formatters adopted across implemented routes | 67 frontend tests, token/contrast tests, formatter boundaries, preference compatibility, Access/Reports contracts, and production build pass; PR #162 | PASS | No API or stored-value change; #159, milestone 12 |
| M12-2 | Direct `#design-system` reference route matches page 2 of the Design Review mockup | Reference screen covers semantic tokens, typography, formatting, controls, status examples, and reversible Light/Dark preview | Desktop and 390×844 review in both themes; keyboard focus visible; no page-level horizontal overflow; focused route/theme/responsive tests pass | PASS | No runtime sample data or cover styling change; #160, milestone 12 |
| M12-3 | Apply shared system and formatters across non-Cover routes without changing business semantics | Shared tokens and formatters adopted by Dashboard, Inventory, Orders, Reservations, Platforms, Pricing, Reports, Team, and Settings | CI and route/component checks pass; desktop signed-out route review and 390×844 Light/Dark captures cover Sign up, Sign in, Inventory, Platforms, and Reservations | IN PROGRESS | Dashboard, Orders, Pricing, Reports, Team, and Settings redirect to Sign in. Their authenticated views and controls need a real signed-in review after M14; #161, milestone 12 |
| A12 | Accessible status meanings, keyboard focus, responsive layout, and theme contrast | Semantic foreground/background tokens, labels/icons, visible focus, and tabular right-aligned numeric columns | Contrast pairs tested in both themes; keyboard Tab showed visible focus on the Sign up home link; captured 390×844 screens showed vertical stacking without visible horizontal overflow | PASS | Protected screen review remains in M12-3; #161, milestone 12 |
| CI | Frontend/backend/container/security/repository gates pass | Existing CI and container configuration retained | PR #162 required checks and main run #38182610493 passed frontend, backend/PostgreSQL, contracts, Compose, Trivy, image scanning, ephemeral Kubernetes migration/rollout, tracking, and repository checks | PASS | Hosted review-staging and production promotion were skipped |
| DEPLOY | Merged source is published and healthy on persistent local Kubernetes | Main SHA `8f9a656657e725c1ce4e9ade1777f89e321b557c` published the immutable image and persistent deployment run #38182882314 rolled it out | Artifact records digest `sha256:ab1a11b751a32d09e74ea285287af6589ff6ba9a51cce8dd97520f65ca446776`; migration completed, rollout reached 1/1, and the configured `/ready` probe passed | PASS | Review-staging and production remain unverified |
| GITHUB | Plan, report, issue state, and milestone agree with live GitHub | PR #162 merged; closure report and issue state are being synchronized | GitHub milestone #13 remains open with three open issues #159–#161 | IN PROGRESS | Final GitHub status and closure hook remain before M12 can close |

## Deliberate deviations

- The visual review browser had no authenticated workspace. Inventory displayed
  its explicit sign-in-required state; Dashboard, Orders, Pricing, Reports,
  Team, and Settings redirected to Sign in. Their authenticated screens and
  controls were not visually reviewed in this M12 pass. The user requested a
  full Sign-up-to-end audit after all milestones close; this coverage is queued
  for that audit after the unparented milestone.
- Signed-out Reservations and Platforms displayed sample-looking workspace
  values. They are not verified account data. No runtime account, product,
  order, or reservation data was created or modified. Their real-data behavior
  must be verified and any seeded content removed during the full audit after
  M14.
- The first browser appearance read showed System. The appearance toggle used
  for theme review changed the browser-local preference to Dark. Settings
  redirects to Sign in, so this browser could not restore System; the saved
  preference remains Dark and may need to be restored by the user. No workspace
  data or business actions were changed.
- Review-staging and production promotion were skipped by workflow
  configuration. Those targets remain unverified; local deployment evidence
  does not imply hosted deployment.

## Missing coverage and follow-ups

- Review the authenticated Dashboard, Orders, Pricing, Reports, Team, and
  Settings screens at desktop and 390×844 in Light and Dark after M14, using
  persisted application data created through the normal Sign-up flow.
- Verify all visible controls, cross-page data synchronization, real-data
  behavior, and remaining technical debt in the user-requested whole-app audit
  after M14. Existing sample-looking signed-out Reservations and Platforms
  content is an explicit audit target.
- Restore the browser-local appearance preference to System when an
  authenticated Settings session is available.
- Hosted review-staging and production are unverified.

## Evidence and verification

- PR #162 merged source `315664b0a582e481bb31b39106543a2f3780dcd4` to `main`
  at `8f9a656657e725c1ce4e9ade1777f89e321b557c`.
- PR #162 required frontend tests/build/contracts, backend/PostgreSQL, Compose
  health and route smoke, image vulnerability scanning, Trivy, ephemeral
  Kubernetes migration/rollout smoke, and repository sanity all passed.
- Main run #38182610493 passed, published immutable digest
  `sha256:ab1a11b751a32d09e74ea285287af6589ff6ba9a51cce8dd97520f65ca446776`,
  and dispatched persistent local deployment run #38182882314. The local run
  records the same source/digest, successful migration, 1/1 rollout, and
  `/ready` probe. Review-staging and production promotion were skipped.
- Live GitHub shows milestone #13 open with issues #159–#161 open. Final issue
  closure, milestone closure, report PASS, and tracking hooks remain.

## Closure verdict

IN PROGRESS. M12 implementation, required CI, image publication, and persistent
local deployment pass. GitHub issue and milestone closure remains outstanding.
