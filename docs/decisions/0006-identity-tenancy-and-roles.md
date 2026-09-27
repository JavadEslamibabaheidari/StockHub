# ADR 0006: Identity, tenancy, and roles

Status: Proposed for review

## Decision

Use ASP.NET Core Identity with PostgreSQL and secure HttpOnly cookie
authentication. Users are global identities, workspaces are tenants, and
memberships grant roles. Every workspace-owned operation must enforce explicit
tenant scope.

Initial roles are Owner, Admin, Manager, Warehouse Staff, and Viewer.
