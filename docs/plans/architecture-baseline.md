# StockHub Architecture Baseline

Status: agreed direction; formal ADR review and PR pending

## Purpose

This document records the architecture agreed for StockHub before application
implementation begins. It is intentionally extraction-ready without introducing
distributed-system complexity before the product has demonstrated the need for
separate services.

## Architecture shape

StockHub starts as a modular monolith. The application is one deployable
backend and initially uses one PostgreSQL database, but each bounded context
owns its domain rules, application use cases, persistence schema, contracts,
configuration, and tests.

The first future extraction target is Platform Integration because external
APIs, retries, rate limits, credentials, and eventual consistency make it a
naturally independent workload. Inventory, Reservations, and Orders remain
together initially because reservation conversion requires strong transactional
correctness.

## Repository shape

```text
backend/
  Host/
  Modules/
    Access/
    Workspaces/
    Catalog/
    Inventory/
    Reservations/
    Orders/
    Platforms/
    PricingRules/
    Reports/
    Team/
    Settings/
  BuildingBlocks/
  Contracts/
frontend/
shared/
  openapi/
docs/
  decisions/
  plans/
```

The backend host composes modules through explicit registration methods. A
module must not reference another module's domain entities, EF Core mappings, or
tables. Cross-module access uses application interfaces, versioned contracts,
or events.

## Backend technology

- .NET 10 LTS and ASP.NET Core Web API.
- PostgreSQL as the source of truth.
- Entity Framework Core migrations.
- REST APIs with generated OpenAPI contracts.
- Database-backed background jobs and an outbox.
- No Kafka, gRPC, event sourcing, or microservices in the initial runtime.

## Messaging

Modules depend only on `StockHub.Messaging.Abstractions`. Infrastructure
implementations live separately:

```text
Messaging/
  StockHub.Messaging.Abstractions/
  StockHub.Messaging.InProcess/
  StockHub.Messaging.Kafka/
  StockHub.Messaging.RabbitMq/
```

The initial implementation is in-process mediation behind asynchronous,
technology-agnostic interfaces. The abstraction distinguishes commands,
events, and integration messages. `AddProducerMessaging<TMessage>()` and
`AddConsumerMessaging<TMessage, THandler>()` are composition-root extensions;
modules do not reference broker-specific packages.

Integration messages are written to an outbox in the same transaction as the
business change. A future Kafka or RabbitMQ adapter dispatches those messages
with at-least-once delivery, retries, dead-letter handling, correlation IDs,
and idempotent consumers.

## Inventory correctness

Physical stock is represented by an append-only movement ledger. Reservations
are separate temporary commitments and do not change physical on-hand stock.

```text
available = physical on-hand - active reservation quantity
```

Reservation conversion and release are idempotent transactional operations.
Database locking or serializable transactions protect competing allocations.
Platform availability is a projection of this calculation and never an
independent stock authority.

## Caching

Caching is an optional infrastructure concern:

```text
BuildingBlocks/Caching.Abstractions/
Infrastructure/Caching.Redis/
```

Redis may cache dashboard summaries, reports, platform metadata, and other
short-lived read models. PostgreSQL remains authoritative for stock, orders,
reservations, permissions, and audit data. Inventory concurrency does not rely
on Redis locks. A no-op or in-memory implementation is sufficient until a
measured use case requires Redis.

## Frontend and contracts

- React, TypeScript, and Vite.
- React Router for navigation.
- TanStack Query for server state.
- URL parameters for filters, sorting, and pagination.
- Generated TypeScript API client from OpenAPI.
- Accessible shared components and design tokens.
- No shared domain model between C# and TypeScript.

## Identity and tenancy

ASP.NET Core Identity with PostgreSQL and secure HttpOnly cookie authentication
is the initial authentication model. Users are global identities; workspaces
are tenants; memberships provide roles and permissions. Every workspace-owned
query carries an explicit tenant scope.

Initial roles are Owner, Admin, Manager, Warehouse Staff, and Viewer.

## Testing and delivery

Backend testing includes domain, application, API, PostgreSQL integration, API
contract, authorization, idempotency, and concurrency tests. Frontend testing
uses Vitest, React Testing Library, Playwright, and targeted accessibility
checks.

CI remains split into repository sanity, frontend, and backend workflows once
runtime scaffolding exists. All changes continue through protected `main`, a
branch, a PR, review, and passing checks.

## Deferred decisions

The following are deliberately deferred until the relevant need appears:

- Kafka versus RabbitMQ for an extracted integration service.
- Redis deployment and eviction policy.
- Production hosting provider and managed service choices.
- Service extraction timing and independent database provisioning.
