namespace StockHub.Api.Contracts;

public sealed record SignUpRequest(
    string FullName,
    string Email,
    string Password);
