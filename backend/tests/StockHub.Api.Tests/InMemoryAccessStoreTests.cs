namespace StockHub.Api.Tests;

public sealed class InMemoryAccessStoreTests
{
    [Fact]
    public async Task Workspace_creation_is_idempotent_and_assigns_owner()
    {
        var store = new StockHub.Api.InMemoryAccessStore();
        var user = await store.CreateUserAsync("Ada Lovelace", "ADA@EXAMPLE.COM", "hash", CancellationToken.None);
        var request = new StockHub.Api.CreateWorkspaceRequest("Acme Goods", "IT", "EUR", null);

        var first = await store.CreateWorkspaceAsync(user.Id, request, "retry-1", CancellationToken.None);
        var second = await store.CreateWorkspaceAsync(user.Id, request, "retry-1", CancellationToken.None);

        Assert.Equal(first.Workspace.Id, second.Workspace.Id);
        var memberships = await store.ListWorkspacesAsync(user.Id, CancellationToken.None);
        var membership = Assert.Single(memberships);
        Assert.Equal(StockHub.Api.WorkspaceRole.Owner, membership.Role);
        Assert.Equal("acme-goods", first.Workspace.Slug);
    }

    [Fact]
    public async Task Active_workspace_can_only_be_selected_for_a_membership()
    {
        var store = new StockHub.Api.InMemoryAccessStore();
        var user = await store.CreateUserAsync("Ada Lovelace", "ADA@EXAMPLE.COM", "hash", CancellationToken.None);
        var other = await store.CreateUserAsync("Grace Hopper", "GRACE@EXAMPLE.COM", "hash", CancellationToken.None);
        var session = await store.CreateSessionAsync(user.Id, CancellationToken.None);
        var workspace = await store.CreateWorkspaceAsync(other.Id, new("Other", "IT", "EUR", null), "other", CancellationToken.None);

        Assert.False(await store.SetActiveWorkspaceAsync(user.Id, session.Id, workspace.Workspace.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Duplicate_business_names_receive_unique_slugs()
    {
        var store = new StockHub.Api.InMemoryAccessStore();
        var user = await store.CreateUserAsync("Ada", "ADA@EXAMPLE.COM", "hash", CancellationToken.None);
        var request = new StockHub.Api.CreateWorkspaceRequest("Acme Goods", "IT", "EUR", null);

        var first = await store.CreateWorkspaceAsync(user.Id, request, "one", CancellationToken.None);
        var second = await store.CreateWorkspaceAsync(user.Id, request, "two", CancellationToken.None);

        Assert.NotEqual(first.Workspace.Slug, second.Workspace.Slug);
    }
}
