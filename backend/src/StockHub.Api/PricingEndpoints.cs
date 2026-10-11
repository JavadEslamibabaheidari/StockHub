using System.Security.Claims;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;

namespace StockHub.Api;

public static class PricingEndpoints
{
    public static void MapPricing(this WebApplication app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}/pricing")
            .RequireAuthorization().WithTags("Pricing");
        group.AddEndpointFilter(async (context, next) =>
        {
            var principal = context.HttpContext.User;
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            var workspaceId = Guid.Parse(context.HttpContext.Request.RouteValues["workspaceId"]!.ToString()!);
            var memberships = await context.HttpContext.RequestServices.GetRequiredService<IAccessStore>()
                .ListWorkspacesAsync(userId, context.HttpContext.RequestAborted);
            var membership = memberships.FirstOrDefault(item => item.Workspace.Id == workspaceId);
            if (membership.Workspace is null) return Results.Forbid();

            var canEdit = Pricing.CanEdit(membership.Role);
            context.HttpContext.Items["pricingCanEdit"] = canEdit;
            var isPreview = context.HttpContext.Request.Path.Value!.EndsWith("/preview", StringComparison.Ordinal);
            if (context.HttpContext.Request.Method != "GET" && !isPreview && !canEdit)
                return Results.Forbid();
            try
            {
                return await next(context);
            }
            catch (PricingValidationException exception)
            {
                return Results.BadRequest(new Problem("Validation failed", exception.Message, "pricing_invalid"));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new Problem("Rule not found", "This rule is no longer available in this workspace.", "pricing_rule_not_found"));
            }
        });

        group.MapGet("/rules", async (
            Guid workspaceId, HttpContext context, PostgresPricingStore store, CancellationToken ct) =>
        {
            var rules = await store.ListAsync(workspaceId, ct);
            return Results.Ok(new PricingRuleList(rules.Select(item => item.Rule).ToArray(),
                (bool)context.Items["pricingCanEdit"]!));
        });
        group.MapPost("/rules", async (
            Guid workspaceId, RuleInput input, PostgresPricingStore store, CancellationToken ct) =>
        {
            var rule = await store.SaveAsync(workspaceId, null, input, ct);
            return Results.Created($"/api/workspaces/{workspaceId}/pricing/rules/{rule!.Id}", rule);
        });
        group.MapPut("/rules/{ruleId:guid}", async (
            Guid workspaceId, Guid ruleId, RuleInput input, PostgresPricingStore store, CancellationToken ct) =>
        {
            var rule = await store.SaveAsync(workspaceId, ruleId, input, ct) ?? throw new KeyNotFoundException();
            return Results.Ok(rule);
        });
        group.MapDelete("/rules/{ruleId:guid}", async (
            Guid workspaceId, Guid ruleId, PostgresPricingStore store, CancellationToken ct) =>
        {
            if (!await store.DeleteAsync(workspaceId, ruleId, ct)) throw new KeyNotFoundException();
            return Results.NoContent();
        });
        group.MapPost("/preview", async (
            Guid workspaceId, PreviewInput input, PostgresPricingStore store, CancellationToken ct) =>
            Results.Ok(await store.PreviewAsync(workspaceId, input, ct)));
    }
}
