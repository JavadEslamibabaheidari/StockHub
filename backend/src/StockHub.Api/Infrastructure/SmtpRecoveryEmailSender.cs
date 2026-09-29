using System.Net;
using System.Net.Mail;
using StockHub.Api.Abstractions;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class SmtpRecoveryEmailSender(IConfiguration configuration) : IRecoveryEmailSender
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"])
        && int.TryParse(configuration["Email:Smtp:Port"], out var port) && port is > 0 and <= 65535
        && MailAddress.TryCreate(configuration["Email:Smtp:From"], out _)
        && Uri.TryCreate(configuration["Application:PublicBaseUrl"], UriKind.Absolute, out var publicBaseUrl)
        && publicBaseUrl.Scheme is "http" or "https";

    public Task SendPasswordResetAsync(string recipient, Uri resetLink, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Recovery email is not configured.");
        }

        return SendAsync(
            recipient,
            "Reset your StockHub password",
            $"Use this link to reset your StockHub password. It expires in 30 minutes and can be used once.\n\n{resetLink}\n\nIf you did not request this, ignore this email.",
            cancellationToken);
    }

    public Task SendInvitationAsync(string recipient, Uri invitationLink, string workspaceName, WorkspaceRole role, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Recovery email is not configured.");
        }

        return SendAsync(
            recipient,
            $"You have been invited to {workspaceName} on StockHub",
            $"You have been invited to {workspaceName} as {role}. Use this link to accept the invitation. It expires in 72 hours and can be used once.\n\n{invitationLink}",
            cancellationToken);
    }

    private async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Recovery email is not configured.");
        }

        using var message = new MailMessage(configuration["Email:Smtp:From"]!, recipient, subject, body);

        using var client = new SmtpClient(
            configuration["Email:Smtp:Host"],
            int.Parse(configuration["Email:Smtp:Port"]!))
        {
            EnableSsl = configuration.GetValue("Email:Smtp:UseTls", true),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };

        var username = configuration["Email:Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, configuration["Email:Smtp:Password"]);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
