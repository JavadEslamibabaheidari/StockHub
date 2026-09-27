# StockHub Frontend Knowledge

The frontend uses React, TypeScript, and Vite. Generated OpenAPI contracts will
be the only backend/frontend boundary once API work begins; domain models are
not shared between C# and TypeScript.

## Milestone 0 Cover

The current implementation entry point is `frontend/src/main.tsx`, rendering
`frontend/src/App.tsx`. The Cover is a static product surface with no backend
dependency. It establishes the warm neutral background, serif brand/hero
typography, accent/sage brand marks, stock-model cards, status pills, and
roadmap Contents panel used by later screens.

Verification commands:

```bash
cd frontend
npm ci
npm test
npm run build
```

The Cover is responsive from 320px through 1440px, exposes a named Contents
navigation, uses visible focus states, and communicates status with text plus
icons/dots rather than colour alone.

Milestone 0 Cover closure is verified by merged PR #18 and report
`docs/reports/milestone-0-cover-closure.md`. Access frontend work consumes only
generated OpenAPI TypeScript clients and is tracked by issues #22, #23, #25,
and #26 under milestone 1.

The committed contract is `contracts/access.openapi.json`; the generated
TypeScript boundary is `frontend/src/api/generated.ts`. Run
`cd frontend && npm run contract:check` to verify required operations and
generated-client alignment before frontend changes are delivered.

Frontend CI runs the locked install, tests, generated-client contract check,
and production build in `.github/workflows/frontend-ci.yml`.

Access routes are hash-addressable in the current shell (`#signup`, `#signin`,
`#workspace`, `#onboarding`, `#invite`, and deferred handoffs). Forms use the
generated client, expose labelled controls and live alert errors, and keep
future Inventory, Platforms, Team, and password-recovery behavior explicitly
deferred.
