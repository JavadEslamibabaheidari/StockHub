using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class ReportsAlertWorker(
    IAccessStore accessStore,
    IProductStore productStore,
    IOrderStore orderStore,
    PostgresReportsStore reportsStore,
    IRecoveryEmailSender emailSender,
    IHttpClientFactory httpClientFactory,
    ILogger<ReportsAlertWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAsync(stoppingToken);
                var delivered = 0; var failed = 0;
                var slackClient=httpClientFactory.CreateClient(); slackClient.Timeout=TimeSpan.FromSeconds(15);
                for(var attempt=0;attempt<100;attempt++)
                {
                    var outcome=await reportsStore.DispatchNextAsync(emailSender,slackClient,stoppingToken);
                    if(outcome==NotificationDispatchOutcome.None)break;
                    if(outcome==NotificationDispatchOutcome.Delivered)delivered++; else {failed++;break;}
                }
                if (delivered > 0) logger.LogInformation("Delivered {NotificationCount} business alerts", delivered);
                if (failed > 0) logger.LogWarning("A business alert delivery failed and was scheduled for retry");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Business alert evaluation failed; the next scheduled pass will retry it"); }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private async Task EvaluateAsync(CancellationToken ct)
    {
        foreach (var workspaceId in await accessStore.ListWorkspaceIdsAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            try { await EvaluateWorkspaceAsync(workspaceId, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception exception) { logger.LogError(exception, "Business alerts failed for workspace {WorkspaceId}", workspaceId); }
        }
    }

    private async Task EvaluateWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        var members = await accessStore.ListTeamMembersAsync(workspaceId, ct);
        if (members.Count == 0) return;
        var rules = await reportsStore.GetAlertsAsync(workspaceId, ct);
        var products = await productStore.ListAsync(workspaceId, ct);
        var reserved = await orderStore.ReservedByProductAsync(workspaceId, ct);
        foreach (var product in products)
        {
            var reservedUnits = reserved.GetValueOrDefault(product.Id);
            var available = ReportMath.AvailableUnits(product.OnHand, reservedUnits);
            await ConsiderAsync(workspaceId, members, rules, "lowStock", product.Id.ToString("N"),
                available > 0 && available <= Threshold(rules, "lowStock"),
                $"Low stock: {product.Name}", $"{available} units are available for {product.Name} (SKU {product.Sku}).", ct);
            await ConsiderAsync(workspaceId, members, rules, "outOfStock", product.Id.ToString("N"),
                available == 0,
                $"Out of stock: {product.Name}", $"No units are available for {product.Name} (SKU {product.Sku}).", ct);
            await ConsiderAsync(workspaceId, members, rules, "oversellingRisk", product.Id.ToString("N"),
                reservedUnits > 0 && available <= Threshold(rules, "oversellingRisk"),
                $"Overselling risk: {product.Name}", $"{reservedUnits} units are reserved and only {available} are available for {product.Name} (SKU {product.Sku}).", ct);
        }
        // The Platforms module has no persisted sync status contract yet; never infer failure from UI copy.
    }

    private async Task ConsiderAsync(Guid workspaceId, IReadOnlyList<TeamMemberResponse> members,
        IReadOnlyList<AlertRule> rules, string ruleKey, string subjectKey, bool active,
        string title, string body, CancellationToken ct)
    {
        var rule = rules.FirstOrDefault(item => item.Key == ruleKey);
        active &= rule is { Enabled: true };
        var activeSince = await reportsStore.ObserveAlertAsync(workspaceId, ruleKey, subjectKey, active, ct);
        if (!active || activeSince is null || rule is null) return;

        var now = DateTimeOffset.UtcNow;
        var (period, deliverAt) = rule.Frequency switch
        {
            "hourly" => (now.ToString("yyyy-MM-dd-HH"), new DateTimeOffset(now.UtcDateTime.Date.AddHours(now.UtcDateTime.Hour + 1), TimeSpan.Zero)),
            "daily" => (now.ToString("yyyy-MM-dd"), new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero)),
            _ => ($"incident-{activeSince.Value.UtcDateTime.Ticks}", now)
        };
        if (rule.Channels.InApp)
        {
            foreach (var member in members)
                await reportsStore.EnqueueAsync(workspaceId, member.UserId, ruleKey, subjectKey, "inApp", $"{ruleKey}:{subjectKey}:{period}:inApp", title, body, deliverAt, ct);
        }
        if (rule.Channels.Email && emailSender.IsConfigured)
        {
            foreach (var member in members.Where(member => IsSafeEmail(member.Email)))
                await reportsStore.EnqueueAsync(workspaceId, member.UserId, ruleKey, subjectKey, "email", $"{ruleKey}:{subjectKey}:{period}:email:{member.UserId:N}", title, body, deliverAt, ct);
        }
        if (rule.Channels.Slack && await reportsStore.HasSlackWebhookAsync(workspaceId, ct))
        {
            var recipient = members.FirstOrDefault(member => member.Role == WorkspaceRole.Owner) ?? members[0];
            await reportsStore.EnqueueAsync(workspaceId, recipient.UserId, ruleKey, subjectKey, "slack", $"{ruleKey}:{subjectKey}:{period}:slack", title, body, deliverAt, ct);
        }
    }

    private static int Threshold(IReadOnlyList<AlertRule> rules, string key) =>
        rules.FirstOrDefault(item => item.Key == key)?.Threshold ?? 0;

    private static bool IsSafeEmail(string value) =>
        System.Net.Mail.MailAddress.TryCreate(value, out var address)
        && string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);
}
