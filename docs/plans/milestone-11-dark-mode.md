# Milestone 11 Dark mode

Status: in progress. The previous Settings milestone is complete: GitHub
milestone #11 is closed with zero open issues, the PASS report is on `main`,
and the local closure hook passes. This plan covers roadmap milestone 11,
GitHub milestone #12 `11 Dark mode`.

## Goal and mockup source

Deliver the dark-mode variants for Dashboard, Inventory with its product quick
view open, and Product detail, as specified by section 11 of
`docs/plans/mockup-led-roadmap.md` and the Dark mode screens in
`docs/mockups/StockHub Final.html`.

Use the browser-local Light/Dark/System preference and theme initialization
implemented by M10. Dark must work on direct route entry, route navigation,
reload, and System resolving to a dark operating-system preference.

## Scope and non-goals

- Dashboard dark surface, navigation, metrics, charts, live-reservation rows,
  attention cards, menus, popovers, dialogs, and first-use/loading/error states.
- Inventory dark surface, navigation, filters, product rows, platform/status
  labels, bulk actions, and loading/empty/error states.
- An accessible Inventory quick view that opens from a product row and shows
  details for the selected persisted product. The mockup seeds Samsung; use
  Samsung when it is returned by the active workspace API, otherwise use the
  selected real product. Do not add fake production products or values.
- Product detail dark surface, stock math, platform rows, pricing when the
  current role permits it, activity, inventory controls, and loading/error
  states.
- Preserve existing routes and direct Product detail links. Quick view adds a
  list interaction without replacing direct detail navigation.
- Use the shared semantic dark palette. Fix component-specific contrast gaps
  without creating a second appearance state or preference store.

Out of scope: new backend APIs, database or generated-contract changes,
production fixture data, new appearance choices, changes to billing or
authorization policy, and unrelated screen redesign.

## User behavior and accessibility

- Quick view is available by pointer and keyboard, has an accessible title,
  moves focus into its dialog when opened, supports Escape and a visible close
  action, and restores focus to the invoking row control when closed.
- Existing row links continue to open Product detail directly.
- Normal text meets WCAG AA 4.5:1 contrast. Large text and meaningful
  non-text controls/graphics meet the applicable 3:1 threshold. Status meaning
  is paired with text or an icon and is never communicated by color alone.
- Controls keep visible focus, accessible names, and their existing behavior.
- Layouts remain usable at desktop and narrow phone widths without clipped
  primary content or unintended horizontal page overflow. A table may scroll
  horizontally inside its own labeled region when its data cannot reflow.
- Loading, empty, disabled, and error states remain distinguishable in Dark
  mode and keep their existing data/error semantics.

## Acceptance criteria

1. Dashboard, Inventory, quick view, and Product detail apply M10's preference
   consistently for Light, Dark, and System, including direct navigation and
   reload.
2. All in-scope surfaces and interactive states use legible dark backgrounds,
   text, borders, focus rings, and status treatments. Contrast is measured and
   all required thresholds pass.
3. Inventory row quick view opens, exposes the selected persisted product's
   available detail, closes with its controls and Escape, restores focus, and
   does not break existing direct detail navigation.
4. Earlier route behavior and business data remain unchanged; no dark-mode path
   substitutes mocked product, inventory, account, or platform values.
5. Focused unit/component tests and keyboard/pointer browser review cover the
   quick view and the three dark screen variants at desktop and narrow widths.
6. Frontend test/build, relevant contract/backend checks, Compose/container
   smoke, image security, required CI, migration, and rollout gates pass where
   affected. Hosted deployment is reported as unverified when skipped.
7. Plan, roadmap, frontend knowledge, project status, and a closure report
   match the implementation and live GitHub state. The report says PASS and
   the closure hook/tracking checks pass before issue/milestone closure.

## Issue breakdown

The live GitHub milestone is #12, `11 Dark mode`. All issues below were
created under that milestone after verifying it was open with zero issues.

1. [#151 Dashboard dark mode](https://github.com/JavadEslamibabaheidari/StockHub/issues/151)
   — Dashboard dark palette, responsive states, keyboard behavior, and focused
   coverage.
2. [#152 Inventory dark mode and quick view](https://github.com/JavadEslamibabaheidari/StockHub/issues/152)
   — dark Inventory and accessible real-product quick view.
3. [#153 Product detail dark mode](https://github.com/JavadEslamibabaheidari/StockHub/issues/153)
   — Product detail dark palette and regressions.
4. [#154 Integrate Dark mode and close milestone](https://github.com/JavadEslamibabaheidari/StockHub/issues/154)
   — cross-route integration, accessibility/responsive review, CI/deployment
   evidence, knowledge/status/report updates, and closure.

Execution order is #151, #152, #153, then #154. Each feature issue must meet
its acceptance criteria before integration/closure begins.

## Architecture and dependencies

- This is frontend-only. `frontend/src/appearance.ts` is the source of truth
  for browser preference, and the root `data-theme` attribute selects dark
  styles. Do not add APIs, state stores, migrations, or a second theme
  preference.
- Existing data remains owned by the current Access/Inventory APIs. Quick view
  may use the selected row summary and existing product-detail API; it must not
  request another module's tables or invent data.
- Accepted frontend/API, identity/tenant, and testing/deployment ADRs remain
  applicable. M10 verified the shared appearance behavior across all nine
  implemented routes; M11 adds the missing screen variants and quick view.
- Existing Dockerfile, `.dockerignore`, Compose readiness, and CI image smoke
  remain unchanged because no deployable project or backend service is added.

## Risks and mitigations

- Dark overrides are partial and include hard-coded light status colors. Audit
  computed text/background pairs and status components; correct each failing
  pair and record measurements/evidence.
- Inventory currently links rows directly to detail and has no quick-view
  component. Add an explicit row action and preserve its existing link.
- The mockup's Samsung seed may not exist in a user's workspace. Target it only
  when real API data contains it; otherwise the invoking persisted product is
  the source.
- A local screenshot or passing source build does not prove main integration,
  image publication, or deployment. Record those gates separately.

## Verification and deployment strategy

- Add focused tests for shared theme compatibility and screen states, quick
  view content/open/close/Escape/focus restoration, and preservation of detail
  links. Run the complete frontend test suite, production build, and contract
  checks. Run backend/Compose/security/Kubernetes gates required by repository
  CI even if backend source is untouched.
- Use a browser at a desktop width and 390px narrow width; inspect Dashboard,
  Inventory list and quick view, and Product detail in Dark, Light, and System
  where relevant. Exercise controls with keyboard and pointer. Measure normal
  and large text and meaningful control contrast.
- Refreshed `main` and `dev` in the isolated M11 clone to
  `32a3d12a55f2be923cd11f4e90f3b9600a010f37` and
  `21549b36342cb031dc94bc79b28ee502be5dbf9c`, respectively. `main` is the M10
  PASS closure source and is the implementation base; `dev` does not contain
  M10. The registered task worktree cannot update its shared `FETCH_HEAD`, so
  status, implementation, and Git operations use the isolated clone.
- At completion, record the feature branch/source SHA, merged main SHA, dev
  integration state, published digest, persistent local deployment/readiness,
  and hosted target status separately.
- Progress evidence (11 October 2026): the isolated frontend has 50 passing
  tests, a successful TypeScript/Vite production build, and passing Access and
  Reports contract checks. Local browser review confirmed the Dark Inventory
  empty state at desktop and 390px with no horizontal page overflow; a
  low-contrast secondary action was fixed. Sample text/background contrast
  calculations range from 5.84:1 (retry badge) to 16.29:1 (primary text on
  page), and the warning, danger, success, accent, button, and focus pairs all
  exceed 4.5:1.
  The original Vite API proxy targeted HTTPS port 7047 with no listening
  service. The documented Compose API was healthy at HTTP port 8080, so Vite
  now defaults to that target and supports `STOCKHUB_API_TARGET` overrides;
  README documents the local review setup. The API now responds, but its
  session endpoint returns 401 in the review browser, which has no authenticated
  workspace, and the local Inventory contains no accessible persisted
  products. Therefore live Dashboard data, Product detail, and persisted
  quick-view browser review remain unverified; mocked component fixtures are
  test-only and are not evidence of persisted data. Do not close the milestone
  until those browser/data gates and CI/deployment gates are verified.
- Previous milestone hook: PASS. M10 report
  `docs/reports/milestone-10-settings-closure.md` says PASS, GitHub milestone
  #11 is closed with zero open issues, and
  `scripts/check-milestone-closure.sh docs/reports/milestone-10-settings-closure.md`
  passed against this base.
