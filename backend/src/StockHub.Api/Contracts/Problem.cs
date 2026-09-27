namespace StockHub.Api.Contracts;

public sealed record Problem(
    string Title,
    string Detail,
    string? Code = null);
