using System.Security.Claims;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;
using StockHub.Api.Validation;

namespace StockHub.Api;

public static class ReportsEndpoints
{
    public static void MapReports(this WebApplication app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}/reports").RequireAuthorization().WithTags("Reports");
        group.AddEndpointFilter(async (context, next) =>
        {
            var principal = context.HttpContext.User;
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
            var workspaceId = Guid.Parse(context.HttpContext.Request.RouteValues["workspaceId"]!.ToString()!);
            var memberships = await context.HttpContext.RequestServices.GetRequiredService<IAccessStore>()
                .ListWorkspacesAsync(userId, context.HttpContext.RequestAborted);
            var membership = memberships.FirstOrDefault(item => item.Workspace.Id == workspaceId);
            if (membership.Workspace is null) return Results.Forbid();
            context.HttpContext.Items["reportCurrency"] = membership.Workspace.Currency;
            context.HttpContext.Items["reportCanEdit"] = membership.Role is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Manager;
            if (context.HttpContext.Request.Method is "PUT" or "DELETE" && !(bool)context.HttpContext.Items["reportCanEdit"]!) return Results.Forbid();
            return await next(context);
        });

        group.MapGet("/daily", async (Guid workspaceId, HttpContext context, PostgresReportsStore store, IOrderStore orderStore, IProductStore productStore, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var end = today;
            var start = end.AddDays(-1);
            var data = await store.LoadAsync(workspaceId,(string)context.Items["reportCurrency"]!,start,end,orderStore,productStore,ct);
            return Results.Ok(new DailyReport(new(start,end,$"{start:dd MMM yyyy}","UTC",data.Currency),data.Summary,
                new(true,data.ExpiredCount,data.LostSales.Sum(sale => sale.Value),"Counts awaiting-payment orders whose ten-minute payment hold expired during this period; value estimates the persisted order lines and is not revenue."),
                data.Platforms,data.LostSales,data.Top,data.Bottom,data.Restock));
        });
        group.MapGet("/weekly", async (Guid workspaceId, HttpContext context, PostgresReportsStore store, IOrderStore orderStore, IProductStore productStore, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var end = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var start = end.AddDays(-7);
            var currency = (string)context.Items["reportCurrency"]!;
            var data = await store.LoadAsync(workspaceId,currency,start,end,orderStore,productStore,ct);
            var previous = await store.LoadAsync(workspaceId,currency,start.AddDays(-7),start,orderStore,productStore,ct);
            var days = Enumerable.Range(0,7).Select(i => new RevenueByDay(start.AddDays(i),data.RevenueByDay.GetValueOrDefault(start.AddDays(i)))).ToArray();
            var previousPlatforms = previous.Platforms.ToDictionary(x => x.Platform, x => x.Revenue, StringComparer.Ordinal);
            var platforms = data.Platforms.Select(x => x with
            {
                ChangePercent = previousPlatforms.TryGetValue(x.Platform, out var prior) && prior != 0
                    ? Math.Round((x.Revenue - prior) * 100m / prior, 2)
                    : null
            }).ToArray();
            var revenueChangePercent = ReportMath.RevenueChangePercent(data.Summary.Revenue,previous.Summary.Revenue);
            return Results.Ok(new WeeklyReport(new(start,end,$"{start:dd MMM} – {end.AddDays(-1):dd MMM yyyy}","UTC",data.Currency),data.Summary,revenueChangePercent,
                new(true,data.ExpiredCount,data.LostSales.Sum(sale => sale.Value),"Counts awaiting-payment orders whose ten-minute payment hold expired during this period; value estimates the persisted order lines and is not revenue."),
                days,platforms,data.Top,data.Bottom,data.Restock));
        });
        group.MapGet("/reorder-list", async (Guid workspaceId, PostgresReportsStore store, CancellationToken ct) => Results.Ok(new ReorderList(await store.GetReorderIdsAsync(workspaceId,ct))));
        group.MapPut("/reorder-list/{productId:guid}", async (Guid workspaceId, Guid productId, ReorderListInput input, PostgresReportsStore store, IProductStore products, CancellationToken ct) =>
        {
            if (!(await products.ListAsync(workspaceId,ct)).Any(product => product.Id == productId)) return Results.NotFound(new Problem("Product not found","This product is not available in this workspace.","product_not_found"));
            await store.SetReorderAsync(workspaceId,productId,input.Listed,ct);
            return Results.Ok(new ReorderListItem(productId,input.Listed));
        });
        group.MapGet("/alert-settings", async (Guid workspaceId, HttpContext context, PostgresReportsStore store, IRecoveryEmailSender email, CancellationToken ct) =>
            Results.Ok(new AlertSettings(await store.GetAlertsAsync(workspaceId,ct),(bool)context.Items["reportCanEdit"]!,
                new(email.IsConfigured?"available":"unavailable","available",await store.HasSlackWebhookAsync(workspaceId,ct)?"available":"unavailable"),["syncFailure"],await store.GetDeliveryStatusAsync(workspaceId,ct))));
        group.MapPut("/alert-settings", async (Guid workspaceId, AlertSettingsInput input, PostgresReportsStore store, IRecoveryEmailSender email, CancellationToken ct) =>
        {
            var error = ReportValidation.Alerts(input);
            if (error is not null) return Results.BadRequest(new Problem("Invalid alert settings",error,"alert_settings_invalid"));
            await store.SaveAlertsAsync(workspaceId,input.Alerts,ct);
            return Results.Ok(new AlertSettings(input.Alerts,true,new(email.IsConfigured?"available":"unavailable","available",await store.HasSlackWebhookAsync(workspaceId,ct)?"available":"unavailable"),["syncFailure"],await store.GetDeliveryStatusAsync(workspaceId,ct)));
        });
        group.MapPut("/alert-settings/slack", async (Guid workspaceId, SlackWebhookInput input, PostgresReportsStore store, CancellationToken ct) =>
        {
            if (!Uri.TryCreate(input.WebhookUrl,UriKind.Absolute,out var webhook)
                || webhook.Scheme!="https" || webhook.Host!="hooks.slack.com" || webhook.Port!=443
                || !webhook.AbsolutePath.StartsWith("/services/",StringComparison.Ordinal)
                || !string.IsNullOrEmpty(webhook.Query) || !string.IsNullOrEmpty(webhook.Fragment) || !string.IsNullOrEmpty(webhook.UserInfo))
                return Results.BadRequest(new Problem("Invalid Slack webhook","Enter a valid Slack incoming webhook URL.","slack_webhook_invalid"));
            await store.SaveSlackWebhookAsync(workspaceId,webhook,ct);
            return Results.Ok(new SlackWebhookStatus(true));
        });
        group.MapDelete("/alert-settings/slack", async (Guid workspaceId, PostgresReportsStore store, CancellationToken ct) =>
        {
            await store.RemoveSlackWebhookAsync(workspaceId,ct);
            return Results.NoContent();
        });
    }

}
