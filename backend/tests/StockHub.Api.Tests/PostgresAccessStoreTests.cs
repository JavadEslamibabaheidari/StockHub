namespace StockHub.Api.Tests;

using StockHub.Api.Contracts;
using StockHub.Api.Infrastructure;

public sealed class PostgresAccessStoreTests
{
    [Fact]
    public async Task Access_records_survive_store_recreation_and_workspace_retry_is_idempotent()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var email = $"integration-{Guid.NewGuid():N}@example.com".ToUpperInvariant();
        var firstStore = new PostgresAccessStore(connectionString);
        var user = await firstStore.CreateUserAsync("Ada Lovelace", email, "hash", CancellationToken.None);
        var request = new CreateWorkspaceRequest("Integration Goods", "IT", "EUR", null);
        var first = await firstStore.CreateWorkspaceAsync(user.Id, request, $"retry-{Guid.NewGuid():N}", CancellationToken.None);

        var recreatedStore = new PostgresAccessStore(connectionString);
        var loadedUser = await recreatedStore.FindUserAsync(email, CancellationToken.None);
        var workspaces = await recreatedStore.ListWorkspacesAsync(user.Id, CancellationToken.None);

        Assert.NotNull(loadedUser);
        Assert.Contains(workspaces, workspace => workspace.Workspace.Id == first.Workspace.Id);

        var retryKey = $"same-{Guid.NewGuid():N}";
        var retryFirst = await recreatedStore.CreateWorkspaceAsync(user.Id, request, retryKey, CancellationToken.None);
        var retrySecond = await recreatedStore.CreateWorkspaceAsync(user.Id, request, retryKey, CancellationToken.None);
        Assert.Equal(retryFirst.Workspace.Id, retrySecond.Workspace.Id);
    }
}
