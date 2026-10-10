# Milestone 7 Pricing rules

Status: start gate ready; GitHub milestone 8 is authoritative.

## Goal and evidence

Implement screenshot 7.1 from `/home/javad/Pictures/Screenshots/Screenshot From 2026-10-11 00-35-48.png` (visually inspected), with durable workspace rules and functional controls. Previous milestone hook PASS: Platforms report passes structural validation; GitHub milestone 7 is closed with zero open issues. Refreshed main `9b79701ada8b959529deb05aaad57f049c8934c0`, dev `21549b36342cb031dc94bc79b28ee502be5dbf9c`; no open PRs. Main delivery 38091518130 published and passed local Kubernetes smoke; shared staging/production skipped.

## Decisions and acceptance

Pricing owns its table and API inside the existing host. PostgreSQL persists rules; no seeded rules are inserted into real workspaces. Existing products provide SKU/category/base price. Members can read; Owner/Admin/Manager can mutate; WarehouseStaff/Viewer read only. Explicit workspace membership is required for every request.

A rule has UUID id, name (1–100 trimmed characters), platform (Amazon/Unieuro/Euronics/eBay), scope (All/Category/Sku), optional scopeValue, adjustment decimal (signed; -100 through 100 for Percent, -1000000 through 1000000 for Euro), unit (Percent/Euro), enabled, priority integer 1–999, optional startDate/endDate (both required together, inclusive UTC calendar dates). Category/SKU values must exist in the workspace. All clears scopeValue. Zero adjustment is valid. Matching enabled rules apply sequentially in ascending priority then UUID order, rounding each result to two decimals away from zero. Negative final prices are validation failures, not silently clamped. Equal priorities are allowed with explicit stable ordering.

The editor previews the candidate replacing its saved rule, or adding a new rule, alongside other rules. Preview returns affected-product count and a representative matching product, applied rule IDs, base/final price and adjustment. Optional product selector chooses a matching SKU. Empty scope has an honest no-products state. Scheduled/expired/disabled rules explain inactivity. Dates are evaluated server-side using UTC today.

Fee assumptions are explicitly estimates: Amazon 15%, Unieuro 10%, Euronics 12%, eBay 13%; VAT assumption 22%. Net excluding VAT = finalPrice / 1.22 - finalPrice * feeRate. Product cost is unavailable in existing schema, so margin is null with explanation rather than a fabricated profit. Marketplace publication, real fee configuration, cost accounting and billing are outside this milestone.

Shell: actual session identity, workspace list and existing active-workspace API, real sign-out, rule search, persisted theme, sidebar collapse, navigation to implemented routes or existing named future-module handoffs. Sync/notifications identify existing connector preview limitations and link Platforms; no fabricated live status. Upgrade reaches existing Settings/Billing handoff. Unsaved edits require confirmation before selection/workspace change/delete.

## Frozen API contract

Base `/api/workspaces/{workspaceId}/pricing`:
- GET `/rules`: `{ rules: PricingRule[], canEdit: boolean }`.
- POST `/rules`: RuleInput; returns PricingRule (201).
- PUT `/rules/{ruleId}`: RuleInput; returns PricingRule.
- DELETE `/rules/{ruleId}`: 204; unknown ID 404.
- POST `/preview`: `{ rule: RuleInput, ruleId?: UUID, productId?: UUID }`; returns PricingPreview.

RuleInput fields: `name, platform, scope, scopeValue: string|null, adjustment: number, unit, enabled: boolean, priority: number, startDate: string|null, endDate: string|null`. PricingRule adds `id`. Preview: `{ affectedProducts: number, productId: string|null, productName: string|null, basePrice: number|null, finalPrice: number|null, adjustment: number|null, feeRate: number, vatRate: number, netExVat: number|null, margin: number|null, explanation: string, appliedRuleIds: string[] }`. Errors use existing Problem shape; 400 validation, 401 no session, 403 unauthorized, 404 missing rule. Preview is read-only and available to members. Contract and client live beside existing Access contract/client; no cross-module domain sharing.

## Issue execution and dependencies

- [#116](https://github.com/JavadEslamibabaheidari/StockHub/issues/116): plan/contract baseline, coordinator; first.
- [#117](https://github.com/JavadEslamibabaheidari/StockHub/issues/117): backend persistence/evaluation, isolated branch after baseline.
- [#118](https://github.com/JavadEslamibabaheidari/StockHub/issues/118): frontend editor/shell, isolated branch after baseline, parallel to backend against frozen contract.
- [#119](https://github.com/JavadEslamibabaheidari/StockHub/issues/119): integrated verification, regression repair, delivery/closure after both.

Each issue gets a focused PR. Coordinator reviews contracts, resolves conflicts and merges only passing required checks. User authorized parallel issue work and autonomous merging. Project board synchronization is blocked: authenticated token lacks read:project scope. GitHub issues, milestones, PRs and checks are accessible; board access is not required to implement the frozen issue contracts.

## Validation and delivery

Domain tests: scope matching, ordering, compounding/rounding, boundaries, disabled/scheduled/expired rules, negative prices. PostgreSQL tests: CRUD/reload, tenant isolation, missing IDs. Endpoint tests: authentication and roles. Frontend: model/contract checks, editor state and errors; browser desktop/mobile checks cover all visible controls, reload, workspace separation, sign-out, and previous Access/Dashboard/Inventory/Orders/Reservations/Platforms routes.

No new deployable project. Existing root Dockerfile, .dockerignore, docker-compose.yml, health/ready endpoints and CI Compose/Kubernetes smoke remain required. Add pricing migration to existing migration discovery. Run frontend tests/build/contract, backend tests, container startup/migration checks and all protected CI checks. Record source SHA and image digest separately from local target and shared target deployment. Shared targets lacking configuration remain unverified, with existing #48 as evidence.

Update docs/knowledge for pricing and correct stale historical summaries, roadmap and project status. Final report must pass `scripts/check-milestone-closure.sh`; run `scripts/check-tracking-status.sh` after tracking updates. Milestone closure requires merged PRs, zero open issues and PASS report, then verified main tag/release.
