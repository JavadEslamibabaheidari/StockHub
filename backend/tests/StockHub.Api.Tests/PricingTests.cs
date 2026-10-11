using StockHub.Api.Contracts;
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;

namespace StockHub.Api.Tests;

public sealed class PricingTests
{
    [Fact]
    public void Rules_match_scope_case_insensitively_and_round_after_each_priority()
    {
        var product = Product(basePrice: 100m, sku: "A-1", category: "Audio");
        var first = Stored(1, Rule("Percent up", "All", null, 5m, "Percent", 1));
        var second = Stored(2, Rule("Euro off", "Category", "audio", -10m, "Euro", 2));

        var result = Pricing.Evaluate(product, "Amazon", [second, first], new DateOnly(2026, 10, 11));

        Assert.Equal(95m, result.Price);
        Assert.Equal(new[] { first.Rule.Id, second.Rule.Id }, result.Applied);
        Assert.True(Pricing.Matches(new RuleInput("SKU", "Amazon", "Sku", "a-1", 1m,
            "Euro", true, 1, null, null), product));
    }

    [Fact]
    public void Dates_are_inclusive_and_disabled_rules_never_apply()
    {
        var product = Product();
        var start = new DateOnly(2026, 11, 27);
        var scheduled = Stored(1, Rule("Sale", "All", null, -10m, "Euro", 1, start,
            new DateOnly(2026, 11, 28)));
        var disabled = Stored(2, Rule("Disabled", "All", null, -50m, "Euro", 2, enabled: false));

        Assert.Equal(90m, Pricing.Evaluate(product, "Amazon", [scheduled, disabled], start).Price);
        Assert.Empty(Pricing.Evaluate(product, "Amazon", [scheduled, disabled], start.AddDays(2)).Applied);
    }

    [Fact]
    public void Negative_price_on_any_product_or_future_date_rejects_the_ruleset()
    {
        var products = new[] { Product(basePrice: 100m), Product(basePrice: 5m, sku: "LOW") };
        var rule = Stored(1, Rule("Discount", "All", null, -10m, "Euro", 1));
        Assert.Throws<PricingValidationException>(() => Pricing.ValidatePrices(products, [rule], new DateOnly(2026, 10, 11)));

        var future = Stored(2, Rule("Future discount", "All", null, -10m, "Euro", 2,
            new DateOnly(2026, 11, 27), new DateOnly(2026, 11, 30)));
        Assert.Throws<PricingValidationException>(() => Pricing.ValidatePrices(
            [Product(basePrice: 5m)], [future], new DateOnly(2026, 10, 11)));
    }

    [Fact]
    public void Decimal_overflow_becomes_a_validation_error()
    {
        var product = Product(basePrice: decimal.MaxValue);
        var rule = Stored(1, Rule("Overflow", "All", null, 1m, "Euro", 1));
        var error = Assert.Throws<PricingValidationException>(() =>
            Pricing.Evaluate(product, "Amazon", [rule], new DateOnly(2026, 10, 11)));
        Assert.Contains("numeric range", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Preview_replaces_the_saved_rule_and_discloses_missing_cost()
    {
        var product = Product(basePrice: 799m);
        var prior = Stored(1, Rule("Old", "All", null, 10m, "Percent", 1));
        var input = new RuleInput("New", "Amazon", "All", null, 5m, "Percent", true, 1, null, null);

        var preview = Pricing.Preview(new PreviewInput(input, prior.Rule.Id, product.Id), [product],
            [prior], new DateOnly(2026, 10, 11));

        Assert.Equal(1, preview.AffectedProducts);
        Assert.Equal(838.95m, preview.FinalPrice);
        Assert.Null(preview.Margin);
        Assert.Contains("cost is not recorded", preview.Explanation);
        Assert.Equal(.15m, preview.FeeRate);
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, true)]
    [InlineData(WorkspaceRole.Admin, true)]
    [InlineData(WorkspaceRole.Manager, true)]
    [InlineData(WorkspaceRole.WarehouseStaff, false)]
    [InlineData(WorkspaceRole.Viewer, false)]
    public void Only_management_roles_can_edit_rules(WorkspaceRole role, bool canEdit) =>
        Assert.Equal(canEdit, Pricing.CanEdit(role));

    private static Product Product(decimal basePrice = 100m, string sku = "SKU-1", string? category = "Audio") =>
        new(Guid.NewGuid(), Guid.NewGuid(), sku, "Test product", 5, basePrice, category);

    private static RuleInput Rule(
        string name,
        string scope,
        string? scopeValue,
        decimal adjustment,
        string unit,
        int priority,
        DateOnly? start = null,
        DateOnly? end = null,
        bool enabled = true) =>
        new(name, "Amazon", scope, scopeValue, adjustment, unit, enabled, priority, start, end);

    private static StoredPricingRule Stored(long sequence, RuleInput input) =>
        new(Pricing.ToRule(Guid.NewGuid(), input), sequence);
}

public sealed class PostgresPricingTests
{
    [Fact]
    public async Task Rules_persist_per_workspace_and_product_import_cannot_break_saved_prices()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var access = new PostgresAccessStore(connectionString);
        var products = new PostgresProductStore(connectionString);
        var pricing = new PostgresPricingStore(connectionString);
        var user = await access.CreateUserAsync("Pricing QA", $"PRICING-{Guid.NewGuid():N}@EXAMPLE.COM", "hash", CancellationToken.None);
        var first = await access.CreateWorkspaceAsync(user.Id,
            new CreateWorkspaceRequest("Pricing One", "IT", "EUR", null), $"pricing-{Guid.NewGuid():N}", CancellationToken.None);
        var second = await access.CreateWorkspaceAsync(user.Id,
            new CreateWorkspaceRequest("Pricing Two", "IT", "EUR", null), $"pricing-{Guid.NewGuid():N}", CancellationToken.None);
        var workspaceId = first.Workspace.Id;
        await products.UpsertAsync(workspaceId,
            [new ProductRequest("PRICE-1", "Price product", 3, 100m, "Audio")], CancellationToken.None);
        var rule = await pricing.SaveAsync(workspaceId, null,
            new RuleInput("Ten euro discount", "Amazon", "All", null, -10m, "Euro", true, 1, null, null),
            CancellationToken.None);
        Assert.NotNull(rule);

        var reloaded = await new PostgresPricingStore(connectionString).ListAsync(workspaceId, CancellationToken.None);
        Assert.Equal(rule.Id, Assert.Single(reloaded).Rule.Id);
        Assert.Empty(await pricing.ListAsync(second.Workspace.Id, CancellationToken.None));
        Assert.False(await pricing.DeleteAsync(second.Workspace.Id, rule.Id, CancellationToken.None));

        var invalidImport = await Assert.ThrowsAsync<PricingValidationException>(() => products.UpsertAsync(workspaceId,
            [new ProductRequest("PRICE-1", "Price product", 3, 5m, "Audio")], CancellationToken.None));
        Assert.Contains("negative price", invalidImport.Message, StringComparison.OrdinalIgnoreCase);
        var persistedProduct = Assert.Single(await products.ListAsync(workspaceId, CancellationToken.None));
        Assert.Equal(100m, persistedProduct.BasePrice);
    }
}
