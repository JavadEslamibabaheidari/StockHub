namespace StockHub.Api.Domain;

public sealed record Invitation(
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt = null);
