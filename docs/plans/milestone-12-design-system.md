# Milestone 12 Design system

Status: implementation and focused local verification complete; PR, delivery,
tracking, and closure gates remain. GitHub milestone #13 `12 Design system` is open
with three issues (#159–#161), created after confirming the milestone had zero
issues. M11 Dark mode is complete: its PASS report is on `main` at
`17ae97645cf898bb68a73323e42f75da8e2eef1c`, milestone #12 is closed, and the
previous-milestone closure hook passes.

## Goal and mockup source

Deliver `12.1 Design system` from page 2 of
[`docs/mockups/StockHub Design Review.pdf`](../mockups/StockHub%20Design%20Review.pdf).
Establish reusable design tokens and shared number formats for the implemented
application screens, and add a Design system reference screen that displays the
tokens and components in Light and Dark. Keep the existing Cover screen intact.

The mockup specifies Organic light/dark surface tokens, StockHub status colors,
Caprasimo display type, Figtree UI type, tabular right-aligned numbers, and
strict en-GB money, quantity, percentage, signed-change, countdown, and date
formats. Status text on fills must reach WCAG AA 4.5:1 in both themes; amber
must read as yellow rather than terracotta.

## Scope and non-goals

- Define shared semantic tokens for page, surface, neutral panel/inset, border,
  primary and secondary text, accent and accent text, available/sage, healthy,
  warning, and error. Each status has a foreground/text treatment and a
  background or dot treatment, with a dark-theme mapping.
- Consolidate the M10 browser-local Light/Dark/System preference and M11
  `data-theme` behavior; do not add another preference or alter its persistence.
- Apply the tokens to all implemented application routes and shared controls,
  preserving each route's behavior, API data, loading/error/empty semantics,
  and responsive layouts. The static Cover is excluded from migration.
- Establish display, title, heading, KPI, panel-title, body, secondary,
  caption/badge, and overline typography with the specified weights and sizes.
  Bundle the specified Caprasimo and Figtree font assets with lockfile-controlled
  dependencies; do not rely on remote runtime font delivery.
- Establish shared en-GB formatting helpers for currency (2 decimals),
  abbreviated KPI currency where appropriate, grouped integer quantities,
  one-decimal percentages, explicit signed change using a true minus sign,
  `mm:ss` countdowns with amber `m:ss` below one minute, relative recent dates,
  and absolute older dates. Numeric table cells use tabular figures and
  right-alignment.
- Add a hash route `#design-system` with accessible, responsive examples of
  light/dark tokens, typography, number formats, buttons, status pills,
  platform/sync states, banners, KPI cards, theme controls, and search.
- Keep status meaning available as text or an icon in addition to color.

Out of scope: new backend APIs, database changes, application workflows,
account-wide theme synchronization, redesign of feature layouts, live-data
fixtures, and unrelated Cover changes.

## User behavior and accessibility

- The reference screen can be reached directly, participates in the existing
  hash navigation behavior, and provides a clear return to the application.
- Light/Dark preview controls on the reference screen use the existing browser
  preference behavior and can restore the user's previous preference.
- Controls have accessible names and visible keyboard focus. Status is never
  communicated by color alone. Numeric data remains understandable to
  assistive technology.
- Normal text contrast is at least 4.5:1; large text and meaningful
  non-text UI indicators meet 3:1. Token pairs are measured in both themes.
- The screen and token examples remain usable at desktop and 390px widths with
  no page-level horizontal overflow.

## Acceptance criteria

1. A documented semantic token set covers light and dark surfaces, text,
   accent, available, and healthy/warning/error status treatments. Implemented
   screens consume shared tokens instead of maintaining divergent copies of
   those semantic values. Existing behavior and M10/M11 theme preference
   persistence remain intact.
2. Typography roles match the mockup and use locally bundled Caprasimo and
   Figtree fonts with safe fallbacks.
3. Shared formatters produce the mockup's en-GB examples and edge cases:
   currency precision, grouped quantity, percentage precision, explicit
   positive/negative changes, countdown boundary at 60 seconds, and recent vs
   older dates. Existing currency codes and business values remain data-driven.
4. The `#design-system` reference screen shows tokens, both theme treatments,
   typography, formatting samples, and representative shared controls/status
   components. It supports direct navigation and restores the previous theme
   preference after preview.
5. Semantic token foreground/background pairs meet measured WCAG contrast
   thresholds. Status has text/icon support; focus remains visible.
6. Focused tests cover token/theme behavior, formatting outputs and boundaries,
   preview/restore behavior, and direct route access. Existing frontend
   regression tests, contract checks, and production build pass.
7. Browser review covers desktop and 390px widths, both themes, and keyboard
   focus. Report contrast calculations and any unavailable authenticated-data
   states honestly; do not seed runtime fixtures.
8. Required CI, repository, tracking, container, image-security, and delivery
   checks pass where affected. Record `main` and `dev` integration separately,
   published image digest, persistent local readiness, and hosted deployment
   status. Skipped hosted jobs remain unverified.
9. Plan, roadmap, frontend knowledge, project status, and a closure report
   align. The report is `PASS`, the closure hook and tracking checks pass, and
   milestone #13 is closed with zero open issues before calling M12 complete.

## Issue breakdown

The authenticated GitHub UI verified all three issues are open and assigned to
the repository owner under milestone #13. Sequence is token/format foundation,
reference screen, then integration and closure.

1. [#159 Create shared design tokens and formatting utilities](https://github.com/JavadEslamibabaheidari/StockHub/issues/159)
   — semantic light and dark tokens, bundled fonts, shared typography/number
   format utilities, and focused tests; do not redesign route layouts.
2. [#160 Build the Design system reference screen](https://github.com/JavadEslamibabaheidari/StockHub/issues/160)
   — `#design-system`, theme preview/restore, representative tokens and
   controls, responsive/accessibility behavior, and direct-route tests.
3. [#161 Adopt design system and close M12](https://github.com/JavadEslamibabaheidari/StockHub/issues/161)
   — migrate implemented application routes to tokens/formatters, regress all
   existing routes and business data, complete contrast/responsive review,
   update docs/status/report, gather CI and delivery evidence, and close the
   milestone only after the PASS hooks.

## Architecture and dependencies

- Previous milestone gate: PASS. `scripts/check-milestone-closure.sh
  docs/reports/milestone-11-dark-mode-closure.md` passes against the M12 base.
- M10's `frontend/src/appearance.ts` remains the single browser preference
  source; the root `data-theme` attribute selects dark tokens. No API,
  persistence migration, or second preference store is needed.
- Frontend API/data boundaries and existing module ownership remain unchanged.
  Shared formatting transforms presentation only; it does not alter stored
  values, calculations, API contracts, or locale-sensitive business rules.
- Accepted architecture, module-boundary, messaging, inventory, cache,
  identity/tenancy, frontend-contract, and CI/deployment ADRs remain applicable.
- This adds no deployable project. Existing Dockerfile, `.dockerignore`,
  Compose readiness, and image smoke configuration remain unchanged; CI still
  runs the existing frontend and delivery gates.

## Risks and mitigations

- Current `styles.css` has duplicated route-specific colors and dark overrides.
  Migrate by semantic role and check route-specific contrast before deleting
  old declarations.
- The reference uses specific display and UI fonts. Bundle them to avoid
  runtime network dependency and keep fallback stacks.
- Locale behavior currently varies across screens (`en-US`, `en-IE`, and
  environment-default formatting). Centralize presentation and preserve
  business-provided currency codes.
- A broad CSS migration may shift feature layouts. Preserve component geometry
  and compare existing route test/browser states as each group is migrated.

## Verification and deployment strategy

- Read frontend and architecture knowledge and all accepted ADRs before work;
  verify claims against current code. The previous M11 closure report hook
  passes.
- Exercise formatters with exact mockup examples and boundary cases; run the
  full frontend suite, contract checks, and production build.
- Review all implemented non-Cover routes in Light and Dark; inspect desktop
  and 390px layouts, keyboard focus, and status text/icon labels. Measure the
  defined token contrast pairs in both themes.
- Run repository-required CI, container smoke, image security, and delivery
  gates. This is a frontend-only change; no database migration is expected.
- Before status updates, refresh `main` and `dev`, read
  `docs/project-status.md`, verify live GitHub milestone/issues and the M12
  delivery run, then report branch/source SHA, main/dev integration, publication,
  local readiness, and hosted target state separately.
- M12 implementation base: `main` SHA
  `17ae97645cf898bb68a73323e42f75da8e2eef1c` (M11 PASS closure PR #158).
  Refreshed GitHub `main` and `dev` resolve to that SHA and
  `21549b36342cb031dc94bc79b28ee502be5dbf9c`, respectively. The `origin`
  remote is a stale local mirror and must not be used as the GitHub source.
- Live GitHub UI confirms milestone #13 `12 Design system` is open and issues
  #159–#161 are attached to it and assigned to the repository owner. The
  configured `gh` token is invalid; direct API requests fail DNS in the normal
  sandbox and return HTTP 403 when network escalation is granted. Browser
  verification is authoritative for issue/milestone state from this checkout.
  The coordinator environment has authenticated tracking access and owns the
  PR, delivery checks, and live closure transitions.
- The authenticated repository Projects page reports “There are no projects
  linked to this repository yet.” Milestone and issue tracking are available;
  the absent project board is recorded as an access/configuration gap, not a
  product implementation blocker.
- Implementation branch `codex/milestone-12-design-system` is pushed to GitHub.
  Latest local verification passes
  67 frontend tests, TypeScript build, Access/Reports contract checks, and
  `git diff --check`. Numeric alignment has focused CSS regression coverage.
  Contrast coverage includes normal text/status pairs,
  neutral labels, the distinct Admin role badge, focus outlines, and accent
  text on accent tint in both themes.
- The authenticated coordinator browser reviewed `#design-system` at 1280×720
  and 390×844 in Light and Dark. The narrow view stacked without page-level
  horizontal overflow and retained visible keyboard focus. Full browser review
  of all application routes, CI/delivery, tracking hook, and milestone closure
  remain outstanding. No signed-in workspace data was seeded during M12.
- Remaining CSS hex values after the Design system reference styles are
  opacity-based shadows, generic contrasting control/icon whites, brand glyphs,
  and Cover/decorative or role-specific values. Repeated semantic surface,
  foreground, status, and focus values use shared tokens.
