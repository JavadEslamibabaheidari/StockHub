namespace StockHub.Api.Contracts;

public sealed record DashboardImportRequest(
    IReadOnlyList<ProductRequest> Products,
    IReadOnlyList<OrderImportRequest> Orders);

public sealed record OrderImportRequest(
    string OrderNumber,
    string Platform,
    string CustomerName,
    string ShipTo,
    string Carrier,
    DateTimeOffset PlacedAt,
    string Status,
    IReadOnlyList<OrderImportItemRequest> Items,
    decimal? Fee,
    string? ReturnReason);

public sealed record OrderImportItemRequest(
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal? CostOfGoods);

public sealed record DashboardImportResponse(int ProductsImported, int OrdersImported);

public sealed record OrderListResponse(
    int TotalToday,
    string SyncSummary,
    IReadOnlyList<OrderSummaryResponse> Orders);

public sealed record OrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    string Product,
    string Sku,
    string Platform,
    string PlatformCode,
    int Quantity,
    string Status,
    decimal Total,
    decimal Fee,
    decimal Net,
    decimal? Margin,
    string Customer,
    DateTimeOffset PlacedAt,
    string Age,
    string NextStep);

public sealed record OrderDetailResponse(
    OrderSummaryResponse Summary,
    IReadOnlyList<OrderItemResponse> Items,
    OrderCustomerShippingResponse CustomerAndShipping,
    IReadOnlyList<OrderTimelineStepResponse> Timeline,
    IReadOnlyList<OrderStockMovementResponse> StockMovements,
    OrderFinancialsResponse Financials,
    OrderReturnResponse? Return);

public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed record OrderCustomerShippingResponse(string Customer, string ShipTo, string Carrier);

public sealed record OrderTimelineStepResponse(string Key, string Label, string Status, string Detail);

public sealed record OrderStockMovementResponse(string Kind, string Title, string Detail, DateTimeOffset OccurredAt);

public sealed record OrderFinancialsResponse(
    decimal TotalPaidByCustomer,
    decimal Vat,
    decimal PlatformFee,
    decimal NetPayout,
    decimal CostOfGoods,
    decimal? MarginExVat);

public sealed record OrderReturnResponse(string Stage, string Reason, DateTimeOffset RequestedAt);

public sealed record OrderActionRequest(string Action, string? Reason);

public sealed record OrderActionResponse(string Status, string Message, OrderDetailResponse Order);
