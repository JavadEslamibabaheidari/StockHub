namespace StockHub.Api.Contracts;

public sealed record OnboardingTask(
    string Key,
    string Title,
    string Status,
    string? Handoff);
