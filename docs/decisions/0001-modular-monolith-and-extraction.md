# ADR 0001: Extraction-ready modular monolith

Status: Accepted in PR #14 (remote verification pending in this workspace)

## Context

StockHub needs strong inventory correctness and is also intended to demonstrate
service-oriented design.

## Decision

Begin with a modular monolith. Every bounded context owns its domain,
persistence boundary, contracts, and tests. Modules communicate through
interfaces and contracts rather than direct entity or table access.

Platform Integration is the first planned extraction target. Inventory,
Reservations, and Orders remain together initially for transactional safety.

## Consequences

The product avoids premature distributed transactions while retaining a clear
path to separate repositories, deployables, and databases.
