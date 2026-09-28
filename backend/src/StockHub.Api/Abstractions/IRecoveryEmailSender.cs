namespace StockHub.Api.Abstractions;

public interface IRecoveryEmailSender
{
    bool IsConfigured { get; }

    Task SendPasswordResetAsync(string recipient, Uri resetLink, CancellationToken cancellationToken);
}
