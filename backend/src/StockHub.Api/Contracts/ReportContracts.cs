namespace StockHub.Api.Contracts;

public sealed record ReportPeriod(DateOnly Start, DateOnly EndExclusive, string Label, string TimeZone, string Currency);
public sealed record ReportSummary(decimal Revenue, int OrderCount, int UnitCount, decimal AverageOrderValue);
public sealed record UnavailableMetric(bool Available, int? Count, decimal? EstimatedValue, string Explanation);
public sealed record PlatformSales(string Platform, int OrderCount, int UnitCount, decimal Revenue, decimal Share, decimal? ChangePercent = null);
public sealed record ProductSales(Guid ProductId, string Name, string Sku, int UnitCount, decimal Revenue);
public sealed record LostSale(string OrderNumber, string ProductName, string Platform, DateTimeOffset ExpiredAt, decimal Value);
public sealed record RestockSuggestion(Guid ProductId, string Name, string Sku, int Available, double AverageUnitsPerDay, double? DaysUntilStockout, int SuggestedUnits, bool OnReorderList);
public sealed record DailyReport(ReportPeriod Period, ReportSummary Summary, UnavailableMetric ExpiredReservations,
    IReadOnlyList<PlatformSales> SalesByPlatform, IReadOnlyList<LostSale> LostSales,
    IReadOnlyList<ProductSales> TopProducts, IReadOnlyList<ProductSales> BottomProducts,
    IReadOnlyList<RestockSuggestion> RestockSuggestions);
public sealed record RevenueByDay(DateOnly Date, decimal Revenue);
public sealed record WeeklyReport(ReportPeriod Period, ReportSummary Summary, decimal? RevenueChangePercent, UnavailableMetric ExpiredReservations,
    IReadOnlyList<RevenueByDay> RevenueByDay, IReadOnlyList<PlatformSales> SalesByPlatform,
    IReadOnlyList<ProductSales> TopProducts, IReadOnlyList<ProductSales> BottomProducts,
    IReadOnlyList<RestockSuggestion> RestockSuggestions);
public sealed record ReorderList(IReadOnlyList<Guid> ProductIds);
public sealed record ReorderListInput(bool Listed);
public sealed record ReorderListItem(Guid ProductId, bool Listed);
public sealed record AlertChannels(bool Email, bool InApp, bool Slack);
public sealed record AlertRule(string Key, bool Enabled, int? Threshold, AlertChannels Channels, string Frequency);
public sealed record AlertSettingsInput(IReadOnlyList<AlertRule> Alerts);
public sealed record AlertSettings(IReadOnlyList<AlertRule> Alerts, bool CanEdit, string DeliveryStatus);
public sealed record ReportOrderRecord(Guid Id, string OrderNumber, string Platform, DateTimeOffset PlacedAt, string Status, string? ReturnStage, decimal Revenue, string ProductName, int Units);
public sealed record ReportProductSalesRecord(Guid ProductId, int Units, decimal Revenue);
