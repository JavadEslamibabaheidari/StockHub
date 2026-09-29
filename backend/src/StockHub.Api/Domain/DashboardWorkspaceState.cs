namespace StockHub.Api.Domain;

public sealed record DashboardWorkspaceState(
    Guid WorkspaceId,
    string Mode,
    string SyncPlatform,
    bool EuronicsRetried,
    bool MismatchResolved,
    bool RestockListed,
    bool ShowMoreLowStock,
    string? DetailPanel,
    DateTimeOffset UpdatedAt)
{
    public static DashboardWorkspaceState Create(Guid workspaceId) => new(
        workspaceId,
        "first-use",
        "Amazon",
        false,
        false,
        false,
        false,
        null,
        DateTimeOffset.UtcNow);
}
