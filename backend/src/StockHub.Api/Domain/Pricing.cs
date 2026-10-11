using StockHub.Api.Contracts;

namespace StockHub.Api.Domain;

public sealed class PricingValidationException(string message) : Exception(message);
public sealed record StoredPricingRule(PricingRule Rule, long Sequence);
public sealed record PricingSnapshot(IReadOnlyList<Product> Products, IReadOnlyList<StoredPricingRule> Rules);

public static class Pricing
{
    public static readonly string[] Platforms = ["Amazon", "Unieuro", "Euronics", "eBay"];
    public const decimal VatRate = .22m;

    public static bool CanEdit(WorkspaceRole role) => role is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Manager;

    public static decimal FeeRate(string platform) => platform switch
    {
        "Amazon" => .15m,
        "Unieuro" => .10m,
        "Euronics" => .12m,
        "eBay" => .13m,
        _ => 0m
    };

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static bool Matches(RuleInput rule, Product product) => rule.Scope switch
    {
        "All" => true,
        "Category" => string.Equals(rule.ScopeValue, product.Category, StringComparison.OrdinalIgnoreCase),
        "Sku" => string.Equals(rule.ScopeValue, product.Sku, StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    public static bool Matches(PricingRule rule, Product product) => Matches(
        new RuleInput(rule.Name, rule.Platform, rule.Scope, rule.ScopeValue, rule.Adjustment,
            rule.Unit, rule.Enabled, rule.Priority, rule.StartDate, rule.EndDate), product);

    public static bool Active(PricingRule rule, DateOnly day) => Active(
        new RuleInput(rule.Name, rule.Platform, rule.Scope, rule.ScopeValue, rule.Adjustment,
            rule.Unit, rule.Enabled, rule.Priority, rule.StartDate, rule.EndDate), day);

    public static bool Active(RuleInput rule, DateOnly day) => rule.Enabled
        && (rule.StartDate is null || day >= rule.StartDate && day <= rule.EndDate);

    public static RuleInput Validate(RuleInput rule, IReadOnlyList<Product> products)
    {
        if (rule is null || string.IsNullOrWhiteSpace(rule.Name) || rule.Name.Trim().Length > 100)
            throw new PricingValidationException("Rule name must contain 1–100 characters.");
        if (!Platforms.Contains(rule.Platform, StringComparer.Ordinal)
            || rule.Scope is not ("All" or "Category" or "Sku")
            || rule.Unit is not ("Percent" or "Euro"))
            throw new PricingValidationException("Choose a valid platform, scope, and adjustment unit.");
        if (rule.Priority is < 1 or > 999)
            throw new PricingValidationException("Priority must be between 1 and 999.");
        var limit = rule.Unit == "Percent" ? 100m : 1_000_000m;
        if (rule.Adjustment < -limit || rule.Adjustment > limit)
            throw new PricingValidationException("Adjustment is outside the supported range.");
        if ((rule.StartDate is null) != (rule.EndDate is null) || rule.StartDate > rule.EndDate)
            throw new PricingValidationException("Provide both dates in chronological order.");

        rule = rule with
        {
            Name = rule.Name.Trim(),
            ScopeValue = rule.Scope == "All" ? null : rule.ScopeValue?.Trim()
        };
        if (rule.Scope != "All"
            && (string.IsNullOrWhiteSpace(rule.ScopeValue) || !products.Any(product => Matches(rule, product))))
            throw new PricingValidationException("The category or SKU must exist in this workspace.");
        return rule;
    }

    public static PricingRule ToRule(Guid id, RuleInput rule) => new(
        id, rule.Name, rule.Platform, rule.Scope, rule.ScopeValue, rule.Adjustment,
        rule.Unit, rule.Enabled, rule.Priority, rule.StartDate, rule.EndDate);

    public static (decimal Price, Guid[] Applied) Evaluate(
        Product product,
        string platform,
        IEnumerable<StoredPricingRule> rules,
        DateOnly day)
    {
        var price = product.BasePrice;
        var applied = new List<Guid>();
        try
        {
            foreach (var stored in rules.OrderBy(item => item.Rule.Priority)
                         .ThenBy(item => item.Sequence).ThenBy(item => item.Rule.Id))
            {
                var rule = stored.Rule;
                if (rule.Platform != platform || !Matches(rule, product) || !Active(rule, day)) continue;
                price = Round(rule.Unit == "Percent"
                    ? price * (1m + rule.Adjustment / 100m)
                    : price + rule.Adjustment);
                applied.Add(rule.Id);
            }
            if (price < 0)
                throw new PricingValidationException($"Rules produce a negative price for SKU {product.Sku} on {day:yyyy-MM-dd}.");
            return (price, applied.ToArray());
        }
        catch (OverflowException)
        {
            throw new PricingValidationException("Rules produce a price beyond the supported numeric range.");
        }
    }

    public static void ValidatePrices(
        IReadOnlyList<Product> products,
        IReadOnlyList<StoredPricingRule> rules,
        DateOnly today)
    {
        var dates = new HashSet<DateOnly> { today };
        foreach (var stored in rules)
        {
            if (stored.Rule.StartDate is { } start) dates.Add(start);
            if (stored.Rule.EndDate is { } end && end < DateOnly.MaxValue) dates.Add(end.AddDays(1));
        }
        foreach (var date in dates)
        foreach (var product in products)
        foreach (var platform in Platforms)
            _ = Evaluate(product, platform, rules, date);
    }

    public static void ValidateWorkspaceRules(
        IReadOnlyList<Product> products,
        IReadOnlyList<StoredPricingRule> rules)
    {
        foreach (var stored in rules)
        {
            var rule = stored.Rule;
            _ = Validate(new RuleInput(rule.Name, rule.Platform, rule.Scope, rule.ScopeValue,
                rule.Adjustment, rule.Unit, rule.Enabled, rule.Priority, rule.StartDate, rule.EndDate), products);
        }
    }

    public static PricingPreview Preview(
        PreviewInput input,
        IReadOnlyList<Product> products,
        IReadOnlyList<StoredPricingRule> rules,
        DateOnly today)
    {
        var candidate = Validate(input.Rule, products);
        var existing = input.RuleId is { } id ? rules.FirstOrDefault(rule => rule.Rule.Id == id) : null;
        if (input.RuleId is not null && existing is null) throw new KeyNotFoundException();
        var candidateId = existing?.Rule.Id ?? Guid.Empty;
        var combined = rules.Where(rule => rule.Rule.Id != candidateId)
            .Append(new StoredPricingRule(ToRule(candidateId, candidate), existing?.Sequence ?? long.MaxValue))
            .ToArray();
        ValidatePrices(products, combined, today);
        var matching = products.Where(product => Matches(candidate, product))
            .OrderBy(product => product.Sku, StringComparer.OrdinalIgnoreCase).ThenBy(product => product.Id).ToArray();
        var product = input.ProductId is { } productId
            ? matching.FirstOrDefault(item => item.Id == productId)
            : matching.FirstOrDefault();
        if (input.ProductId is not null && product is null)
            throw new PricingValidationException("Select a product matching this rule in this workspace.");

        var note = !candidate.Enabled ? "Rule is disabled. "
            : candidate.StartDate > today ? "Rule is scheduled and not active today. "
            : candidate.EndDate < today ? "Rule has expired. " : string.Empty;
        note += "Estimated fees and 22% VAT. Margin unavailable because product cost is not recorded. External marketplace publication is not connected.";
        if (product is null)
            return new(0, null, null, null, null, null, FeeRate(candidate.Platform), VatRate,
                null, null, "No products match this rule. " + note, []);

        var evaluated = Evaluate(product, candidate.Platform, combined, today);
        return new(matching.Length, product.Id, product.Name, product.BasePrice, evaluated.Price,
            Round(evaluated.Price - product.BasePrice), FeeRate(candidate.Platform), VatRate,
            Round(evaluated.Price / (1m + VatRate) - evaluated.Price * FeeRate(candidate.Platform)),
            null, note, evaluated.Applied);
    }
}
