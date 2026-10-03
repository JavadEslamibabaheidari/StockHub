using StockHub.Api.Contracts;

namespace StockHub.Api.Abstractions;

public interface IOrderStore
{
    Task<DashboardImportResponse> ImportAsync(
        Guid workspaceId,
        DashboardImportRequest request,
        CancellationToken cancellationToken);

    Task<OrderListResponse> ListAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<OrderDetailResponse?> GetAsync(
        Guid workspaceId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<OrderDetailResponse?> ApplyActionAsync(
        Guid workspaceId,
        Guid orderId,
        OrderActionRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> ReservedByProductAsync(
        Guid workspaceId,
        CancellationToken cancellationToken);
}
