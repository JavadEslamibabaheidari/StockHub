namespace StockHub.Api.Domain;

public sealed record Product(
    Guid Id,
    Guid WorkspaceId,
    string Sku,
    string Name,
    int OnHand,
    decimal BasePrice,
    string? Category);
