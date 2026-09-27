namespace StockHub.Api.Domain;

public sealed record Session(
    Guid Id,
    Guid UserId,
    Guid? ActiveWorkspaceId,
    DateTimeOffset ExpiresAt);
