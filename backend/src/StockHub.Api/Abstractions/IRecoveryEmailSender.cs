namespace StockHub.Api.Abstractions;

using StockHub.Api.Domain;

public interface IRecoveryEmailSender
{
    bool IsConfigured { get; }

    Task SendPasswordResetAsync(string recipient, Uri resetLink, CancellationToken cancellationToken);

    Task SendInvitationAsync(string recipient, Uri invitationLink, string workspaceName, WorkspaceRole role, CancellationToken cancellationToken);
}
