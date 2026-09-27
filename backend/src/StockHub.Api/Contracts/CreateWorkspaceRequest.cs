namespace StockHub.Api.Contracts;

public sealed record CreateWorkspaceRequest(
    string BusinessName,
    string Country,
    string Currency,
    string? VatNumber);
