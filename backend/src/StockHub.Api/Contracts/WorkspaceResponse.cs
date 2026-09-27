using StockHub.Api.Domain;

namespace StockHub.Api.Contracts;

public sealed record WorkspaceResponse(
    Guid Id,
    string BusinessName,
    string Country,
    string Currency,
    string? VatNumber,
    string Slug,
    WorkspaceRole Role);
