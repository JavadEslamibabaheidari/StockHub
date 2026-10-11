# Pricing rules

Pricing rules are owned by the existing API host and use workspace-scoped
PostgreSQL persistence. The execution plan and OpenAPI contract are
[`milestone-7-pricing.md`](../plans/milestone-7-pricing.md) and
[`pricing.openapi.json`](../../contracts/pricing.openapi.json).

Each rule targets one marketplace and all products, a category, or a SKU. Rule
mutations require Owner, Admin, or Manager membership; every workspace member
can list rules and request a read-only preview. Enabled, in-date rules match
case-insensitively and apply by priority, creation sequence, then stable ID.
Each step rounds to cents away from zero. Saving validates existing products
against current and scheduled rule boundaries, so imports and base-price
changes cannot leave invalid marketplace prices behind.

The editor supports create, update, delete, enable/disable, dates, priority,
search, and a persisted-workspace price preview. Workspace switching and
sign-out use the existing Access APIs. Preview discloses estimated marketplace
fees (Amazon 15%, Unieuro 10%, Euronics 12%, eBay 13%) and assumed VAT of 22%.
Margin remains unavailable until cost data exists; marketplace publication is
not connected.

The current implementation is in `backend/src/StockHub.Api/Domain/Pricing.cs`,
`backend/src/StockHub.Api/PricingEndpoints.cs`,
`backend/src/StockHub.Api/Infrastructure/PostgresPricingStore.cs`, and
`frontend/src/PricingScreens.tsx`. Migration `006_pricing_rules.sql` creates
the durable rule table. Contract, domain, PostgreSQL, API, frontend, Compose,
and Kubernetes checks are recorded in the milestone 7 closure report.
