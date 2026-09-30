namespace StockHub.Api.Tests;

using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;

public sealed class InMemoryDashboardStoreTests
{
    [Fact]
    public async Task Dashboard_state_is_workspace_scoped_and_persistent()
    {
        var store = new InMemoryDashboardStore();
        var firstWorkspace = Guid.NewGuid();
        var secondWorkspace = Guid.NewGuid();

        var initial = await store.GetAsync(firstWorkspace, CancellationToken.None);
        Assert.Equal("first-use", initial.Mode);
        Assert.False(initial.EuronicsRetried);

        await store.SaveAsync(initial with
        {
            Mode = "first-sync",
            EuronicsRetried = true,
            MismatchResolved = true,
            RestockListed = true,
            ShowMoreLowStock = true,
            DetailPanel = "Sync details opened"
        }, CancellationToken.None);

        var saved = await store.GetAsync(firstWorkspace, CancellationToken.None);
        Assert.Equal("first-sync", saved.Mode);
        Assert.True(saved.EuronicsRetried);
        Assert.True(saved.MismatchResolved);
        Assert.True(saved.RestockListed);
        Assert.True(saved.ShowMoreLowStock);
        Assert.Equal("Sync details opened", saved.DetailPanel);

        var other = await store.GetAsync(secondWorkspace, CancellationToken.None);
        Assert.Equal("first-use", other.Mode);
        Assert.False(other.EuronicsRetried);
    }
}
