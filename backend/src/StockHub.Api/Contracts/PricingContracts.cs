namespace StockHub.Api.Contracts;

public sealed record RuleInput(
    string Name,
    string Platform,
    string Scope,
    string? ScopeValue,
    decimal Adjustment,
    string Unit,
    bool Enabled,
    int Priority,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record PricingRule(
    Guid Id,
    string Name,
    string Platform,
    string Scope,
    string? ScopeValue,
    decimal Adjustment,
    string Unit,
    bool Enabled,
    int Priority,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record PricingRuleList(IReadOnlyList<PricingRule> Rules, bool CanEdit);

public sealed record PreviewInput(RuleInput Rule, Guid? RuleId, Guid? ProductId);

public sealed record PricingPreview(
    int AffectedProducts,
    Guid? ProductId,
    string? ProductName,
    decimal? BasePrice,
    decimal? FinalPrice,
    decimal? Adjustment,
    decimal FeeRate,
    decimal VatRate,
    decimal? NetExVat,
    decimal? Margin,
    string Explanation,
    IReadOnlyList<Guid> AppliedRuleIds);

