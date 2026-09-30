using StockHub.Api.Domain;

namespace StockHub.Api.Abstractions;

public interface IDashboardStore
{
    Task<DashboardWorkspaceState> GetAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<DashboardWorkspaceState> SaveAsync(DashboardWorkspaceState state, CancellationToken cancellationToken);
}
