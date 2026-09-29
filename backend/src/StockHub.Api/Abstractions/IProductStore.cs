using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Abstractions;

public interface IProductStore
{
    Task<IReadOnlyList<Product>> ListAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> UpsertAsync(
        Guid workspaceId,
        IReadOnlyList<ProductRequest> products,
        CancellationToken cancellationToken);

    Task<Product?> UpdateOnHandAsync(
        Guid workspaceId,
        Guid productId,
        int onHand,
        CancellationToken cancellationToken);
}
