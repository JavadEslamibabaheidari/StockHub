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
