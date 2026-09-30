using System.Collections.Concurrent;
using StockHub.Api.Abstractions;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class InMemoryDashboardStore : IDashboardStore
{
    private readonly ConcurrentDictionary<Guid, DashboardWorkspaceState> states = new();

    public Task<DashboardWorkspaceState> GetAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        Task.FromResult(states.GetOrAdd(workspaceId, DashboardWorkspaceState.Create));

    public Task<DashboardWorkspaceState> SaveAsync(DashboardWorkspaceState state, CancellationToken cancellationToken)
    {
        var saved = state with { UpdatedAt = DateTimeOffset.UtcNow };
        states[saved.WorkspaceId] = saved;
        return Task.FromResult(saved);
    }
}
