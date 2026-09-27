# ADR 0008: Testing, CI, and deployment boundaries

Status: Proposed for review

## Decision

Backend and frontend have independent test and CI pipelines, with shared API
contract changes triggering both. PostgreSQL integration and concurrency tests
are required for inventory and reservations. The application is containerized,
but staging and production deployment details remain deferred until runtime
scaffolding and hosting decisions are approved.
