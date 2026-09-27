using StockHub.Api.Domain;

namespace StockHub.Api.Contracts;

public sealed record WorkspaceSummary(
    Guid Id,
    string BusinessName,
    string Country,
    string Currency,
    WorkspaceRole Role);
