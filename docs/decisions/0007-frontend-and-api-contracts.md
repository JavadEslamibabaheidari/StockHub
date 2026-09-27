# ADR 0007: React frontend and generated API contracts

Status: Proposed for review

## Decision

Use React, TypeScript, and Vite with React Router and TanStack Query. The
frontend consumes a generated TypeScript client from the backend OpenAPI
document. Shared code is limited to generated contracts and design-system
tokens; backend domain types are not shared with the frontend.
