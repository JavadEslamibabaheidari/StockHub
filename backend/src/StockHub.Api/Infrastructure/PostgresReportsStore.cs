using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Npgsql;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class PostgresReportsStore : IBusinessNotificationStore
{
    private readonly string connectionString;
    private readonly IDataProtector slackProtector;

    public PostgresReportsStore(string connectionString, IDataProtectionProvider dataProtectionProvider)
    {
        this.connectionString = connectionString;
        slackProtector = dataProtectionProvider.CreateProtector("StockHub.Reports.SlackWebhook.v1");
    }
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReportData> LoadAsync(Guid workspaceId, string currency, DateOnly start, DateOnly endExclusive, IOrderStore orderStore, IProductStore productStore, CancellationToken ct)
    {
        var startUtc = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var endUtc = new DateTimeOffset(endExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var rows = await orderStore.LoadReportOrdersAsync(workspaceId,startUtc.AddDays(-1),endUtc,ct);
        var reportSales = (await orderStore.LoadReportProductSalesAsync(workspaceId,startUtc,endUtc,ct)).ToDictionary(x => x.ProductId);
        var restockSales = (await orderStore.LoadReportProductSalesAsync(workspaceId,endUtc.AddDays(-14),endUtc,ct)).ToDictionary(x => x.ProductId);
        var trailingSales = (await orderStore.LoadReportProductSalesAsync(workspaceId,endUtc.AddDays(-30),endUtc,ct)).ToDictionary(x => x.ProductId);
        var reservedByProduct = await orderStore.ReservedByProductAsync(workspaceId,ct);
        var productRows = (await productStore.ListAsync(workspaceId,ct)).Select(product =>
        {
            reportSales.TryGetValue(product.Id,out var report); restockSales.TryGetValue(product.Id,out var restock); trailingSales.TryGetValue(product.Id,out var trailing);
            var available = ReportMath.AvailableUnits(product.OnHand,reservedByProduct.GetValueOrDefault(product.Id));
            return new ReportProduct(product.Id,product.Name,product.Sku,available,restock?.Units ?? 0,report?.Units ?? 0,report?.Revenue ?? 0m,trailing?.Units ?? 0,trailing?.Revenue ?? 0m);
        }).ToArray();
        var reorder = (await GetReorderIdsAsync(workspaceId,ct)).ToHashSet();
        var rangeStart = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rangeEnd = endExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var paid = rows.Where(r => IsRevenue(r) && r.PlacedAt >= rangeStart && r.PlacedAt < rangeEnd).ToArray();
        var platforms = paid.GroupBy(row => row.Platform).Select(group => new PlatformSales(group.Key, group.Count(), group.Sum(x => x.Units), group.Sum(x => x.Revenue), 0m)).ToArray();
        var totalRevenue = paid.Sum(x => x.Revenue);
        platforms = platforms.Select(x => x with { Share = totalRevenue == 0 ? 0 : Math.Round(x.Revenue * 100m / totalRevenue, 2) }).ToArray();
        var products = productRows.Select(p => new ProductSales(p.Id, p.Name, p.Sku, p.PeriodUnits, p.Revenue)).ToArray();
        var sold = ReportMath.TopProducts(products);
        IReadOnlyList<ProductSales> leastSold = productRows.Select(p => new ProductSales(p.Id,p.Name,p.Sku,p.Units30Days,p.Revenue30Days))
            .ToArray();
        leastSold = ReportMath.BottomProducts(leastSold);
        var restock = productRows.Select(p => {
            var metrics = ReportMath.RestockMetrics(p.Available,p.Units);
            return new RestockSuggestion(p.Id,p.Name,p.Sku,p.Available,metrics.AverageUnitsPerDay,metrics.DaysUntilStockout,metrics.SuggestedUnits,reorder.Contains(p.Id));
        }).Where(x => x.Available <= 10 || x.SuggestedUnits > 0).OrderBy(x => x.Available).ToArray();
        var lost = rows.Where(r => r.Status == "AwaitingPayment" && r.PlacedAt.AddMinutes(10) >= start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) && r.PlacedAt.AddMinutes(10) < endExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .Select(r => new LostSale(r.OrderNumber, r.ProductName, r.Platform, r.PlacedAt.AddMinutes(10), r.Revenue)).ToArray();
        var daily = paid.GroupBy(r => DateOnly.FromDateTime(r.PlacedAt.UtcDateTime)).ToDictionary(g => g.Key, g => g.Sum(r => r.Revenue));
        return new ReportData(currency, new ReportSummary(totalRevenue, paid.Length, paid.Sum(x => x.Units), paid.Length == 0 ? 0 : Math.Round(totalRevenue / paid.Length,2)), platforms,
            lost, sold, leastSold, restock,
            lost.Length, daily);
    }

    public async Task<IReadOnlyList<Guid>> GetReorderIdsAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("SELECT product_id FROM report_reorder_items WHERE workspace_id=@workspace ORDER BY created_at,product_id", connection);
        command.Parameters.AddWithValue("workspace",workspaceId); await using var reader = await command.ExecuteReaderAsync(ct);
        var values = new List<Guid>(); while (await reader.ReadAsync(ct)) values.Add(reader.GetGuid(0)); return values;
    }

    public async Task<bool> SetReorderAsync(Guid workspaceId, Guid productId, bool listed, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(listed
            ? "INSERT INTO report_reorder_items(workspace_id,product_id) VALUES(@workspace,@product) ON CONFLICT DO NOTHING"
            : "DELETE FROM report_reorder_items WHERE workspace_id=@workspace AND product_id=@product", connection);
        command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("product",productId); await command.ExecuteNonQueryAsync(ct); return true;
    }

    public async Task<IReadOnlyList<AlertRule>> GetAlertsAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("SELECT settings FROM report_alert_settings WHERE workspace_id=@workspace", connection);
        command.Parameters.AddWithValue("workspace",workspaceId); var raw = await command.ExecuteScalarAsync(ct);
        return raw is string json ? JsonSerializer.Deserialize<AlertRule[]>(json, JsonOptions)! : DefaultAlerts;
    }

    public async Task SaveAlertsAsync(Guid workspaceId, IReadOnlyList<AlertRule> alerts, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("INSERT INTO report_alert_settings(workspace_id,settings,updated_at) VALUES(@workspace,@settings::jsonb,now()) ON CONFLICT(workspace_id) DO UPDATE SET settings=EXCLUDED.settings,updated_at=now()", connection);
        command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("settings",JsonSerializer.Serialize(alerts,JsonOptions)); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<DateTimeOffset?> ObserveAlertAsync(Guid workspaceId, string ruleKey, string subjectKey, bool active, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO alert_rule_states(workspace_id,rule_key,subject_key,is_active,active_since)
            VALUES(@workspace,@rule,@subject,@active,CASE WHEN @active THEN now() ELSE NULL END)
            ON CONFLICT(workspace_id,rule_key,subject_key) DO UPDATE SET
              active_since=CASE WHEN EXCLUDED.is_active AND NOT alert_rule_states.is_active THEN now()
                                WHEN EXCLUDED.is_active THEN alert_rule_states.active_since ELSE NULL END,
              is_active=EXCLUDED.is_active,updated_at=now()
            RETURNING active_since
            """, connection);
        command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("rule",ruleKey);
        command.Parameters.AddWithValue("subject",subjectKey); command.Parameters.AddWithValue("active",active);
        var value = await command.ExecuteScalarAsync(ct);
        return value switch
        {
            DateTimeOffset offset => offset,
            DateTime timestamp => new DateTimeOffset(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)),
            _ => null
        };
    }

    public async Task EnqueueAsync(Guid workspaceId, Guid recipientId, string ruleKey, string subjectKey, string channel,
        string dedupeKey, string title, string body, DateTimeOffset deliverAt, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO alert_notification_jobs(id,workspace_id,recipient_id,rule_key,subject_key,dedupe_key,title,body,channel,next_attempt_at)
            VALUES(@id,@workspace,@recipient,@rule,@subject,@dedupe,@title,@body,@channel,@deliverAt)
            ON CONFLICT(workspace_id,recipient_id,dedupe_key) DO NOTHING
            """, connection);
        command.Parameters.AddWithValue("id",Guid.NewGuid()); command.Parameters.AddWithValue("workspace",workspaceId);
        command.Parameters.AddWithValue("recipient",recipientId); command.Parameters.AddWithValue("rule",ruleKey);
        command.Parameters.AddWithValue("subject",subjectKey); command.Parameters.AddWithValue("dedupe",dedupeKey);
        command.Parameters.AddWithValue("title",title); command.Parameters.AddWithValue("body",body);
        command.Parameters.AddWithValue("channel",channel);
        command.Parameters.AddWithValue("deliverAt",deliverAt); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> HasSlackWebhookAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection=new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM workspace_alert_integrations WHERE workspace_id=@workspace)",connection);
        command.Parameters.AddWithValue("workspace",workspaceId);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task SaveSlackWebhookAsync(Guid workspaceId, Uri webhook, CancellationToken ct)
    {
        var protectedWebhook=slackProtector.Protect(webhook.AbsoluteUri);
        await using var connection=new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand("INSERT INTO workspace_alert_integrations(workspace_id,slack_webhook_protected) VALUES(@workspace,@webhook) ON CONFLICT(workspace_id) DO UPDATE SET slack_webhook_protected=EXCLUDED.slack_webhook_protected,updated_at=now()",connection);
        command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("webhook",protectedWebhook);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveSlackWebhookAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection=new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand("DELETE FROM workspace_alert_integrations WHERE workspace_id=@workspace",connection);
        command.Parameters.AddWithValue("workspace",workspaceId); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AlertChannelDeliveryStatus>> GetDeliveryStatusAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand("""
            SELECT channel,count(*) FILTER(WHERE status IN ('pending','processing'))::int,
                   count(*) FILTER(WHERE status='delivered')::int,count(*) FILTER(WHERE status='dead')::int
            FROM alert_notification_jobs WHERE workspace_id=@workspace GROUP BY channel ORDER BY channel
            """,connection);
        command.Parameters.AddWithValue("workspace",workspaceId);
        await using var reader=await command.ExecuteReaderAsync(ct);var result=new List<AlertChannelDeliveryStatus>();
        while(await reader.ReadAsync(ct))result.Add(new AlertChannelDeliveryStatus(reader.GetString(0),reader.GetInt32(1),reader.GetInt32(2),reader.GetInt32(3)));
        return result;
    }

    public async Task<NotificationDispatchOutcome> DispatchNextAsync(IRecoveryEmailSender emailSender, HttpClient slackClient, CancellationToken ct)
    {
        Guid? failedJobId = null;
        try
        {
            await using var connection=new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
            Guid jobId,workspaceId,recipientId; string channel,title,body;
            await using (var transaction=await connection.BeginTransactionAsync(ct))
            {
                await using(var select=new NpgsqlCommand("SELECT id,workspace_id,recipient_id,channel,title,body FROM alert_notification_jobs WHERE (status='pending' AND next_attempt_at<=now()) OR (status='processing' AND lease_expires_at<=now()) ORDER BY next_attempt_at,created_at FOR UPDATE SKIP LOCKED LIMIT 1",connection,transaction))
                await using(var reader=await select.ExecuteReaderAsync(ct))
                {
                    if(!await reader.ReadAsync(ct)){await transaction.CommitAsync(ct);return NotificationDispatchOutcome.None;}
                    jobId=reader.GetGuid(0); workspaceId=reader.GetGuid(1); recipientId=reader.GetGuid(2);
                    channel=reader.GetString(3); title=reader.GetString(4); body=reader.GetString(5);
                }
                failedJobId=jobId;
                await using var lease=new NpgsqlCommand("UPDATE alert_notification_jobs SET status='processing',lease_expires_at=now()+interval '2 minutes' WHERE id=@id",connection,transaction);
                lease.Parameters.AddWithValue("id",jobId); await lease.ExecuteNonQueryAsync(ct);
                await transaction.CommitAsync(ct);
            }
            switch(channel)
            {
                case "inApp":
                    await using(var deliveryConnection=new NpgsqlConnection(connectionString))
                    {
                        await deliveryConnection.OpenAsync(ct); await using var deliveryTransaction=await deliveryConnection.BeginTransactionAsync(ct);
                        await using(var insert=new NpgsqlCommand("INSERT INTO business_notifications(id,job_id,workspace_id,recipient_id,title,body) VALUES(@id,@job,@workspace,@recipient,@title,@body) ON CONFLICT(job_id) DO NOTHING",deliveryConnection,deliveryTransaction))
                        {insert.Parameters.AddWithValue("id",Guid.NewGuid());insert.Parameters.AddWithValue("job",jobId);insert.Parameters.AddWithValue("workspace",workspaceId);insert.Parameters.AddWithValue("recipient",recipientId);insert.Parameters.AddWithValue("title",title);insert.Parameters.AddWithValue("body",body);await insert.ExecuteNonQueryAsync(ct);}
                        await MarkDeliveredAsync(deliveryConnection,deliveryTransaction,jobId,ct);
                        await deliveryTransaction.CommitAsync(ct);
                    }
                    break;
                case "email":
                    await using(var recipientQuery=new NpgsqlCommand("SELECT email::text FROM users WHERE id=@recipient",connection))
                    {
                        recipientQuery.Parameters.AddWithValue("recipient",recipientId);
                        var recipient=await recipientQuery.ExecuteScalarAsync(ct) as string ?? throw new InvalidOperationException("Notification recipient no longer exists.");
                        await emailSender.SendBusinessAlertAsync(recipient,title,body,ct);
                    }
                    await MarkDeliveredAsync(connection,null,jobId,ct);
                    break;
                case "slack":
                    await using(var webhookQuery=new NpgsqlCommand("SELECT slack_webhook_protected FROM workspace_alert_integrations WHERE workspace_id=@workspace",connection))
                    {
                        webhookQuery.Parameters.AddWithValue("workspace",workspaceId);
                        var protectedValue=await webhookQuery.ExecuteScalarAsync(ct) as string ?? throw new InvalidOperationException("Workspace Slack integration is unavailable.");
                        var webhook=new Uri(slackProtector.Unprotect(protectedValue),UriKind.Absolute);
                        using var response=await slackClient.PostAsJsonAsync(webhook,new { text=$"*{title}*\n{body}" },ct);
                        response.EnsureSuccessStatusCode();
                    }
                    await MarkDeliveredAsync(connection,null,jobId,ct);
                    break;
                default: throw new InvalidOperationException("Notification channel is unsupported.");
            }
            return NotificationDispatchOutcome.Delivered;
        }
        catch(Exception exception) when(exception is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            if(failedJobId is Guid id)
            {
                try { await RecordDeliveryFailureAsync(id,exception.GetType().Name,ct); }
                catch { return NotificationDispatchOutcome.Failed; }
            }
            return NotificationDispatchOutcome.Failed;
        }
    }

    private static async Task MarkDeliveredAsync(NpgsqlConnection connection,NpgsqlTransaction? transaction,Guid jobId,CancellationToken ct)
    {
        await using var command=new NpgsqlCommand("UPDATE alert_notification_jobs SET status='delivered',delivered_at=now(),lease_expires_at=NULL,last_error=NULL WHERE id=@id AND status='processing'",connection,transaction);
        command.Parameters.AddWithValue("id",jobId);
        if(await command.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("Notification delivery lease expired before completion.");
    }

    private async Task RecordDeliveryFailureAsync(Guid jobId,string safeError,CancellationToken ct)
    {
        await using var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand("""
            UPDATE alert_notification_jobs SET attempts=attempts+1,
              status=CASE WHEN attempts+1>=8 THEN 'dead' ELSE 'pending' END,
              next_attempt_at=now()+make_interval(secs=>LEAST(3600,30*power(2,LEAST(attempts,7)))),
              lease_expires_at=NULL,last_error=@error
            WHERE id=@id AND status='processing'
            """,connection);
        command.Parameters.AddWithValue("id",jobId);command.Parameters.AddWithValue("error",$"Delivery failed ({safeError})");await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<BusinessNotification>> ListNotificationsAsync(Guid workspaceId, Guid recipientId, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT id,title,body,created_at,read_at FROM business_notifications
            WHERE workspace_id=@workspace AND recipient_id=@recipient ORDER BY created_at DESC LIMIT 50
            """,connection);
        command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("recipient",recipientId);
        await using var reader=await command.ExecuteReaderAsync(ct); var result=new List<BusinessNotification>();
        while(await reader.ReadAsync(ct)) result.Add(new BusinessNotification(reader.GetGuid(0),reader.GetString(1),reader.GetString(2),reader.GetFieldValue<DateTimeOffset>(3),reader.IsDBNull(4)?null:reader.GetFieldValue<DateTimeOffset>(4)));
        return result;
    }

    public async Task<int> CountUnreadNotificationsAsync(Guid workspaceId, Guid recipientId, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand("SELECT count(*) FROM business_notifications WHERE workspace_id=@workspace AND recipient_id=@recipient AND read_at IS NULL",connection);
        command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("recipient",recipientId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    public async Task<bool> MarkNotificationReadAsync(Guid workspaceId, Guid recipientId, Guid notificationId, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand("UPDATE business_notifications SET read_at=COALESCE(read_at,now()) WHERE id=@id AND workspace_id=@workspace AND recipient_id=@recipient",connection);
        command.Parameters.AddWithValue("id",notificationId); command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("recipient",recipientId);
        return await command.ExecuteNonQueryAsync(ct)>0;
    }

    public static readonly AlertRule[] DefaultAlerts = [
        new("lowStock",true,5,new(true,true,false),"daily"), new("outOfStock",true,0,new(true,true,true),"instant"),
        new("syncFailure",true,5,new(true,true,true),"instant"), new("oversellingRisk",true,3,new(false,true,true),"instant") ];
    private static bool IsRevenue(ReportOrderRecord row) => (row.Status is "PaidToPick" or "PickedToShip" or "Shipped" or "Delivered" or "ReturnRequested") && row.ReturnStage != "Refunded";
}

public enum NotificationDispatchOutcome { None, Delivered, Failed }

public sealed record ReportData(string Currency, ReportSummary Summary, IReadOnlyList<PlatformSales> Platforms, IReadOnlyList<LostSale> LostSales,
    IReadOnlyList<ProductSales> Top, IReadOnlyList<ProductSales> Bottom, IReadOnlyList<RestockSuggestion> Restock, int ExpiredCount, IReadOnlyDictionary<DateOnly,decimal> RevenueByDay);
internal sealed record ReportProduct(Guid Id,string Name,string Sku,int Available,int Units,int PeriodUnits,decimal Revenue,int Units30Days,decimal Revenue30Days);
public sealed record RestockMetrics(double AverageUnitsPerDay,double? DaysUntilStockout,int SuggestedUnits);
public static class ReportMath
{
    public static IReadOnlyList<ProductSales> TopProducts(IEnumerable<ProductSales> products) =>
        products.OrderByDescending(p => p.UnitCount).ThenBy(p => p.Name).Take(5).ToArray();

    public static IReadOnlyList<ProductSales> BottomProducts(IEnumerable<ProductSales> products) =>
        products.OrderBy(p => p.UnitCount).ThenBy(p => p.Name).Take(5).ToArray();

    public static decimal? RevenueChangePercent(decimal currentRevenue,decimal previousRevenue) => previousRevenue > 0
        ? Math.Round((currentRevenue-previousRevenue)*100m/previousRevenue,1)
        : null;

    public static int AvailableUnits(int onHand,int reserved) => Math.Max(0,onHand-reserved);

    public static RestockMetrics RestockMetrics(int available,int unitsSoldLast14Days)
    {
        var average = unitsSoldLast14Days / 14d;
        var days = average > 0 ? Math.Round(available / average,1) : (double?)null;
        return new RestockMetrics(Math.Round(average,2),days,Math.Max(0,(int)Math.Ceiling(average*14)-available));
    }
}
