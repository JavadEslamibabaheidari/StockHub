namespace StockHub.Api.Contracts;

public sealed record PasswordResetConfirmRequest(string Token, string NewPassword);
