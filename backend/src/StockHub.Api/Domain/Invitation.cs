namespace StockHub.Api.Domain;

public sealed record Invitation(
    Guid Id,
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConsumedAt = null);
