namespace StockHub.Api.Contracts;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    int OnHand,
    decimal BasePrice,
    string? Category);
