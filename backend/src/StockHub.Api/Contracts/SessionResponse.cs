namespace StockHub.Api.Contracts;

public sealed record SessionResponse(
    Guid UserId,
    string FullName,
    string Email,
    Guid? ActiveWorkspaceId,
    IReadOnlyList<WorkspaceSummary> Workspaces);
