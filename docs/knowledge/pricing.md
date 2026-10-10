# Pricing rules planned boundary

Milestone 7 plan: `docs/plans/milestone-7-pricing.md`; contract:
`contracts/pricing.openapi.json`; issues #116–#119. Implementation is pending.
Pricing owns PostgreSQL rules and authenticated workspace APIs inside the
existing host. Rules compound in priority/creation order with cent rounding and
inclusive UTC dates. Actual products supply scope and base prices. Preview
fees/VAT are disclosed estimates; profit margin is unavailable without costs.
The frontend must use actual workspace switching and sign-out APIs.

Create previews place new rules last among equal priorities; updates preserve creation order. Scope matching is ordinal case-insensitive. Saves validate all workspace products at today and every rule date boundary, with controlled overflow errors. Inventory pricing projections will consume the same evaluator; external marketplace publication remains out of scope.
