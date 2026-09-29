namespace StockHub.Api.Contracts;

public sealed record ProductRequest(
    string Sku,
    string Name,
    int OnHand,
    decimal BasePrice,
    string? Category);
