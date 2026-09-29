namespace StockHub.Api.Domain;

public sealed record DashboardWorkspaceState(
    Guid WorkspaceId,
    string Mode,
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
        false,
        false,
        false,
        false,
        null,
        DateTimeOffset.UtcNow);
}
