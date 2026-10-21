using System.Security.Claims;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;

namespace StockHub.Api;

public static class NotificationEndpoints
{
    public static void MapNotifications(this WebApplication app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}/notifications").RequireAuthorization().WithTags("Notifications");
        group.MapGet("", async (Guid workspaceId, ClaimsPrincipal principal, IAccessStore access, IBusinessNotificationStore store, CancellationToken ct) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
            var memberships = await access.ListWorkspacesAsync(userId, ct);
            if (!memberships.Any(item => item.Workspace.Id == workspaceId)) return Results.Forbid();
            var notifications = await store.ListNotificationsAsync(workspaceId, userId, ct);
            var unread = await store.CountUnreadNotificationsAsync(workspaceId, userId, ct);
            return Results.Ok(new BusinessNotificationList(notifications, unread));
        });
        group.MapPost("/{notificationId:guid}/read", async (Guid workspaceId, Guid notificationId, ClaimsPrincipal principal, IAccessStore access, IBusinessNotificationStore store, CancellationToken ct) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
            var memberships = await access.ListWorkspacesAsync(userId, ct);
            if (!memberships.Any(item => item.Workspace.Id == workspaceId)) return Results.Forbid();
            return await store.MarkNotificationReadAsync(workspaceId, userId, notificationId, ct)
                ? Results.NoContent()
                : Results.NotFound();
        });
    }
}
