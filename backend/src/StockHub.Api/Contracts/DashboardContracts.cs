namespace StockHub.Api.Contracts;

public sealed record DashboardSnapshotResponse(
    string State,
    string Title,
    string Subtitle,
    string StatusLabel,
    string StatusTone,
    IReadOnlyList<DashboardMetricResponse> Metrics,
    IReadOnlyList<DashboardReservationResponse> Reservations,
    IReadOnlyList<DashboardPlatformSaleResponse> SalesByPlatform,
    IReadOnlyList<DashboardAttentionResponse> Attention,
    IReadOnlyList<DashboardGetStartedResponse> GetStarted,
    DashboardSyncResponse? Sync,
    IReadOnlyList<DashboardSearchResultResponse> SearchIndex,
    IReadOnlyList<string> Notifications);

public sealed record DashboardMetricResponse(string Key, string Label, string Value, string Hint, string Tone);

public sealed record DashboardReservationResponse(
    string Id,
    string ProductName,
    string Platform,
    string OrderNumber,
    string Quantity,
    string TimeLeft,
    int ProgressPercent);

public sealed record DashboardPlatformSaleResponse(string Platform, decimal Percent, string Tone);

public sealed record DashboardAttentionResponse(
    string Id,
    string Kind,
    string Tone,
    string Title,
    string Detail,
    IReadOnlyList<DashboardActionResponse> Actions);

public sealed record DashboardActionResponse(string Key, string Label, string Style);

public sealed record DashboardGetStartedResponse(string Key, string Title, string Detail, string Status);

public sealed record DashboardSyncResponse(
    string Platform,
    int SyncedProducts,
    int TotalProducts,
    string RemainingLabel,
    IReadOnlyList<DashboardSyncStepResponse> Steps);

public sealed record DashboardSyncStepResponse(string Key, string Title, string Detail, string Status);

public sealed record DashboardSearchResultResponse(string Id, string Type, string Label, string Detail, string Action);

public sealed record DashboardActionRequest(string Action, string? TargetId, int? OnHand, string? Platform);

public sealed record DashboardActionResultResponse(string Status, string Message, DashboardSnapshotResponse Snapshot);
