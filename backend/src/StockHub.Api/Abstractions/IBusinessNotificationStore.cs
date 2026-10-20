using StockHub.Api.Contracts;

namespace StockHub.Api.Abstractions;

public interface IBusinessNotificationStore
{
    Task<IReadOnlyList<BusinessNotification>> ListNotificationsAsync(Guid workspaceId, Guid recipientId, CancellationToken cancellationToken);
    Task<int> CountUnreadNotificationsAsync(Guid workspaceId, Guid recipientId, CancellationToken cancellationToken);
    Task<bool> MarkNotificationReadAsync(Guid workspaceId, Guid recipientId, Guid notificationId, CancellationToken cancellationToken);
}
