namespace StockHub.Api.Domain;

public sealed record Workspace(
    Guid Id,
    string BusinessName,
    string Country,
    string Currency,
    string? VatNumber,
    string Slug);
