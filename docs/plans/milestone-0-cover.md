# Milestone 0 — Cover

Status: implemented locally; closure pending branch, PR, CI, and GitHub verification

## Goal

Restore the first product surface from the approved mockup: a clear StockHub
cover that explains the product proposition, teaches the three stock numbers,
defines status semantics, and exposes the approved roadmap contents.

This is the first implementation milestone. The previous-milestone closure hook
is `NOT APPLICABLE` because `0 Cover` is the first implementation milestone;
there is no earlier implementation milestone to close.

## Source material

- Product source of truth: `docs/mockups/StockHub Final.html`, cover section.
- Visual reference: `docs/mockups/StockHub Design Review.pdf`, page 1 cover.
- Roadmap: `docs/plans/mockup-led-roadmap.md`, milestone `0 Cover`.
- Architecture: `docs/plans/architecture-baseline.md`, React/TypeScript/Vite
  frontend decision.
- Closure process: `docs/ai-development-workflow.md`.

## Scope

- Create the `frontend/` React/Vite application shell.
- Implement the Cover hero with StockHub branding and the approved proposition:
  “One stock count and one price list for every marketplace you sell on, kept
  in sync in real time, in the browser.”
- Implement the three-number model: On hand, Reserved, and Available, with the
  approved explanatory text and equation.
- Implement the status-colour legend with text and visible status dots: Synced,
  Low stock, Failed, and Paused.
- Implement the Contents panel for sections 0 through 12 and the current Cover
  state. Future sections are represented as roadmap content, not falsely
  implemented routes.
- Establish responsive behavior from 320px through 1440px and accessible
  headings, navigation, focus states, and non-colour status communication.

## Non-goals

- No backend, database, authentication, API, generated contract, or deployment
  behavior. Cover has no runtime data contract and backend is not applicable.
- No Access forms or navigation behavior beyond the roadmap representation.
- No live marketplace data, account creation, or interactive inventory state.
- No dark-mode implementation; that is roadmap milestone 11.

## Deliverables

- `frontend/` Vite/React/TypeScript application with reproducible build and
  test commands.
- Cover page implementation and responsive design tokens in `frontend/src/`.
- Smoke tests for proposition, stock model, contents navigation, and accessible
  landmarks.
- This milestone plan and an updated roadmap/knowledge record.
- Closure evidence prepared for the reusable milestone hook once branch, PR,
  CI, and GitHub state are available.

## Acceptance criteria

1. The page renders the StockHub brand, approved proposition, source-of-truth
   note, three stock numbers, and status legend.
2. The equation and explanatory text distinguish On hand, Reserved, and
   Available without relying on colour alone.
3. The Contents panel lists the approved roadmap sections and marks Cover as
   the current section; future sections are visibly planned rather than
   presented as live application routes.
4. The page has one clear level-one heading, a named contents navigation, and
   keyboard-visible focus states.
5. The layout reflows at 320px, 520px, 760px, 1024px, and 1440px without
   horizontal scrolling or clipped content.
6. Reduced-motion preferences are respected.
7. `npm run build` and `npm test` pass in `frontend/`.
8. The closure report records backend as `NOT_APPLICABLE`, explains why no
   previous milestone exists, and verifies mockup, plan, code, tests,
   configuration, knowledge, roadmap, and GitHub evidence before closure.

## Task breakdown

| ID | Work | Boundary | Status |
|---|---|---|---|
| C0.1 | Recover Cover requirements from HTML/PDF and record scope | product/design | complete |
| C0.2 | Scaffold React/Vite/TypeScript frontend | frontend foundation | complete |
| C0.3 | Implement Cover hero, stock model, legend, and contents | frontend | complete |
| C0.4 | Add responsive and accessibility-oriented styles | frontend quality | complete |
| C0.5 | Add smoke tests and run build/test checks | verification | complete — clean `npm ci`, 5 Vitest tests, and `npm run build` pass locally |
| C0.6 | Create branch/PR and synchronize milestone tracking | delivery | blocked by unusable local Git/GitHub auth |
| C0.7 | Run closure hook and record evidence | quality gate | pending C0.6 |

## Testing strategy

- Vitest server-rendering smoke tests verify the proposition, stock model, status
  legend, named navigation, current-section state, heading structure, roadmap
  inventory, and stylesheet safeguards.
- TypeScript build validation catches invalid component and configuration
  boundaries.
- Manual visual verification compares the rendered Cover against the PDF/HTML
  reference at desktop and narrow responsive widths.
- Browser-level checks remain a follow-up when Playwright is introduced in the
  application CI foundation.

## Dependencies and risks

- Node and npm are available locally and the frontend dependency lockfile is
  present. Docker Desktop is not required for this static Cover milestone;
  PostgreSQL and other service containers belong to later backend milestones.
- The workspace still lacks usable Git metadata, so no branch or PR evidence can
  be produced locally.
- GitHub CLI authentication and remote network access are unavailable in this
  task, so branch protection, board state, and CI execution cannot be
  independently verified from the local shell. The issue and milestone
  records were persisted through the authenticated GitHub browser session.
- The cover Contents list is intentionally informational until future roadmap
  sections become implemented routes.

## Verification record

- `cd frontend && npm ci` — PASS; lockfile install completed.
- `cd frontend && npm test -- --run` — PASS; 5 tests passed.
- `cd frontend && npm run build` — PASS; TypeScript validation and Vite
  production bundle completed.
- The frontend CI workflow is present at
  `.github/workflows/frontend-ci.yml`; its GitHub run is still unverified
  because the local checkout cannot push a branch or open a PR.
- Workspace commit evidence: `0be25f1ddc063823e67a0488133470e54106ece1`
  for M0-1 and `7b180655636d3fbfeb0753b599f1a85f42c1e466` for M0-2. The
  M0-3 delivery commit will record the final closure-blocker evidence after
  remote delivery is attempted.

## GitHub tracking

The repository roadmap names `0 Cover` as the first product section. Before
closure, verify the exact GitHub milestone/issue/board state, attach the PR,
observe required CI checks, and record immutable URLs in the closure report.
Until that verification is possible, this milestone is implemented locally but
not closed.

## GitHub issue breakdown

These issues are the persisted execution units for this milestone. They are
ordered by dependency and must be completed, committed, and verified in order.
The issue URLs are populated after GitHub creation and verification.

| Order | Issue | Depends on | Acceptance and evidence |
|---|---|---|---|
| M0-1 | [Implement the Cover frontend foundation](https://github.com/JavadEslamibabaheidari/StockHub/issues/15) | None | React/Vite/TypeScript shell exists; hero, stock model, status legend, and roadmap Contents match the approved source; responsive and accessible styles are included; evidence: frontend source, mockup comparison, and issue commit. |
| M0-2 | [Verify Cover parity and quality gates](https://github.com/JavadEslamibabaheidari/StockHub/issues/16) | M0-1 | Proposition, stock equation, exact roadmap names, one H1, named navigation, current state, reduced motion, responsive widths, tests, and production build pass; evidence: test/build output and visual/browser checks. |
| M0-3 | [Restore delivery evidence and close Milestone 0](https://github.com/JavadEslamibabaheidari/StockHub/issues/17) | M0-2 and existing CI tracking issue [#9](https://github.com/JavadEslamibabaheidari/StockHub/issues/9) | Issue commits are present on a branch, PR and CI are green, GitHub issue state and milestone are synchronized, closure report passes `scripts/check-milestone-closure.sh`, and all deviations/follow-ups are tracked; only then close the milestone. |

GitHub issue URLs:

- M0-1: https://github.com/JavadEslamibabaheidari/StockHub/issues/15
- M0-2: https://github.com/JavadEslamibabaheidari/StockHub/issues/16
- M0-3: https://github.com/JavadEslamibabaheidari/StockHub/issues/17

Execution rule: each issue receives its own commit after its acceptance
criteria are verified. If verification exposes missing work, add a new issue
under `0 Cover`, update this table and the relevant dependency, then implement
and commit that issue before proceeding.

## Knowledge updates

`docs/knowledge/architecture.md` records the active Cover milestone and the
React/Vite frontend boundary. A frontend knowledge record documents the Cover
entry point, design tokens, and verification commands. After closure, update
those records with the verified branch/PR/CI evidence.

## Readiness and closure verdict

The implementation scope is complete locally and backend is explicitly not
applicable. M0-1 and M0-2 are locally verified; M0-3 remains blocked because
the workspace cannot create a real branch/commit in its repository, push the
commits, open a PR, observe CI, or verify branch protection/board state. The
milestone must remain open. After restoring GitHub access, publish the two
commits (or recreate them on a real branch), complete M0-3, create the closure
report from
`docs/templates/milestone-closure-report.md`, and pass
`scripts/check-milestone-closure.sh` before reopening Milestone 1.
