namespace StockHub.Api.Domain;

public sealed record User(
    Guid Id,
    string FullName,
    string NormalizedEmail,
    string PasswordHash);
