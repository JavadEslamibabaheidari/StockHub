# ADR 0002: Module boundaries and shared contracts

Status: Proposed for review

## Decision

Modules own their domain entities, persistence mappings, tables, use cases,
configuration, and tests. Shared libraries contain only technical building
blocks and versioned integration contracts. Domain entities and repositories
are never shared between modules.

When a module is extracted, its contracts become a versioned package or
repository artifact rather than copied source code.
