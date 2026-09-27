namespace StockHub.Api.Domain;

public sealed record Membership(
    Guid UserId,
    Guid WorkspaceId,
    WorkspaceRole Role);
