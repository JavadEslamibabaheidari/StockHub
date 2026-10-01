namespace StockHub.Api.Contracts;

public sealed record InventoryCapabilities(
    bool CanAdjustOnHand,
    bool CanChangePrice,
    bool CanManageListings);

public sealed record InventoryPlatformStatus(
    string Platform,
    string Code,
    string Status,
    string Detail,
    int AvailableShown,
    bool Listed);

public sealed record InventoryProductSummary(
    Guid Id,
    string Sku,
    string Name,
    string Category,
    int OnHand,
    int Reserved,
    int Available,
    decimal BasePrice,
    string StockStatus,
    IReadOnlyList<InventoryPlatformStatus> Platforms);

public sealed record InventoryListResponse(
    int TotalProducts,
    string SyncSummary,
    IReadOnlyList<InventoryProductSummary> Products,
    InventoryCapabilities Capabilities);

public sealed record InventoryPricingRow(
    string Platform,
    string Rule,
    string Adjustment,
    decimal FinalPrice,
    decimal Fees,
    decimal Margin,
    string Status);

public sealed record InventoryAuditEntry(
    string Kind,
    string Title,
    string Detail,
    string When);

public sealed record InventoryProductDetail(
    InventoryProductSummary Product,
    bool SafetyBufferEnabled,
    int LowStockAlertAt,
    string SyncAlert,
    IReadOnlyList<InventoryPricingRow> Pricing,
    IReadOnlyList<InventoryAuditEntry> AuditLog,
    InventoryCapabilities Capabilities);

public sealed record AdjustOnHandRequest(int OnHand);

public sealed record InventoryActionResponse(string Status, InventoryProductSummary Product);
