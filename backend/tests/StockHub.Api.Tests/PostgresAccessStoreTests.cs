namespace StockHub.Api.Tests;

using StockHub.Api.Contracts;
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;

public sealed class PostgresAccessStoreTests
{
    [Fact]
    public async Task Signup_persists_a_single_user_with_a_valid_session()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var email = $"signup-{Guid.NewGuid():N}@gmail.com".ToUpperInvariant();
        var store = new PostgresAccessStore(connectionString);
        var first = await store.CreateUserWithSessionAsync("Personal User", email, "hash", CancellationToken.None);

        Assert.Equal(first.User.Id, (await store.FindUserAsync(email, CancellationToken.None))?.Id);
        Assert.Equal(first.User.Id, (await store.FindSessionAsync(first.Session.Id, CancellationToken.None))?.UserId);
        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.CreateUserWithSessionAsync("Other User", email, "other-hash", CancellationToken.None));
        Assert.Equal("duplicate_user", duplicate.Message);
        Assert.Equal(first.User.Id, (await store.FindUserAsync(email, CancellationToken.None))?.Id);
    }

    [Fact]
    public async Task Password_reset_and_google_link_survive_store_recreation()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var email = $"access-{Guid.NewGuid():N}@gmail.com".ToUpperInvariant();
        var firstStore = new PostgresAccessStore(connectionString);
        var signup = await firstStore.CreateUserWithSessionAsync("Personal User", email, "old", CancellationToken.None);
        var linked = await firstStore.GetOrCreateGoogleUserAsync(
            $"google-{Guid.NewGuid():N}", email, "Google User", true, CancellationToken.None);
        Assert.Equal(signup.User.Id, linked.Id);

        var tokenHash = $"test-{Guid.NewGuid():N}";
        await firstStore.CreatePasswordResetAsync(signup.User.Id, tokenHash, DateTimeOffset.UtcNow.AddMinutes(30), CancellationToken.None);
        var recreated = new PostgresAccessStore(connectionString);
        Assert.True(await recreated.ResetPasswordAsync(tokenHash, "new", CancellationToken.None));
        Assert.False(await recreated.ResetPasswordAsync(tokenHash, "another", CancellationToken.None));
        Assert.Null(await recreated.FindSessionAsync(signup.Session.Id, CancellationToken.None));
        Assert.Equal("new", (await recreated.FindUserAsync(email, CancellationToken.None))?.PasswordHash);
    }

    [Fact]
    public async Task Google_identity_creates_once_and_rejects_unsafe_email_link()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var store = new PostgresAccessStore(connectionString);
        var subject = $"google-{Guid.NewGuid():N}";
        var email = $"google-{Guid.NewGuid():N}@gmail.com".ToUpperInvariant();
        var created = await store.GetOrCreateGoogleUserAsync(subject, email, "Google User", true, CancellationToken.None);
        var returned = await new PostgresAccessStore(connectionString).GetOrCreateGoogleUserAsync(
            subject, email, "Changed Name", true, CancellationToken.None);
        Assert.Equal(created.Id, returned.Id);
        Assert.Equal(created.Id, (await store.FindUserAsync(email, CancellationToken.None))?.Id);

        var existingEmail = $"existing-{Guid.NewGuid():N}@example.com".ToUpperInvariant();
        var existing = await store.CreateUserWithSessionAsync("Existing User", existingEmail, "hash", CancellationToken.None);
        var conflict = await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetOrCreateGoogleUserAsync(
            $"google-{Guid.NewGuid():N}", existingEmail, "Imposter", false, CancellationToken.None));
        Assert.Equal("external_account_conflict", conflict.Message);
        Assert.Equal(existing.User.Id, (await store.FindUserAsync(existingEmail, CancellationToken.None))?.Id);
    }

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

    [Fact]
    public async Task Product_import_upserts_workspace_products()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var accessStore = new PostgresAccessStore(connectionString);
        var productStore = new PostgresProductStore(connectionString);
        var user = await accessStore.CreateUserAsync($"Product User {Guid.NewGuid():N}", $"PRODUCT-{Guid.NewGuid():N}@EXAMPLE.COM", "hash", CancellationToken.None);
        var workspace = await accessStore.CreateWorkspaceAsync(user.Id, new CreateWorkspaceRequest("Product Goods", "IT", "EUR", null), $"retry-{Guid.NewGuid():N}", CancellationToken.None);

        var first = await productStore.UpsertAsync(
            workspace.Workspace.Id,
            new[] { new ProductRequest("SKU-1", "First Product", 5, 19.99m, "Phones") },
            CancellationToken.None);
        var second = await productStore.UpsertAsync(
            workspace.Workspace.Id,
            new[] { new ProductRequest("SKU-1", "Updated Product", 7, 21.50m, null) },
            CancellationToken.None);
        var products = await new PostgresProductStore(connectionString).ListAsync(workspace.Workspace.Id, CancellationToken.None);

        Assert.Equal(first.Single().Id, second.Single().Id);
        var product = Assert.Single(products);
        Assert.Equal("Updated Product", product.Name);
        Assert.Equal(7, product.OnHand);
        Assert.Equal(21.50m, product.BasePrice);
        Assert.Null(product.Category);
    }

    [Fact]
    public async Task Dashboard_import_persists_orders_and_order_actions_update_stock()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var accessStore = new PostgresAccessStore(connectionString);
        var productStore = new PostgresProductStore(connectionString);
        var orderStore = new PostgresOrderStore(connectionString);
        var user = await accessStore.CreateUserAsync($"Order User {Guid.NewGuid():N}", $"ORDER-{Guid.NewGuid():N}@EXAMPLE.COM", "hash", CancellationToken.None);
        var workspace = await accessStore.CreateWorkspaceAsync(user.Id, new CreateWorkspaceRequest("Order Goods", "IT", "EUR", null), $"retry-{Guid.NewGuid():N}", CancellationToken.None);

        var response = await orderStore.ImportAsync(
            workspace.Workspace.Id,
            new DashboardImportRequest(
                [new ProductRequest("ORD-SKU-1", "Order Product", 5, 100m, "Test")],
                [
                    new OrderImportRequest(
                        $"ORD-{Guid.NewGuid():N}",
                        "Amazon",
                        "A. Customer",
                        "Via Test 1",
                        "BRT",
                        new DateTimeOffset(2026, 10, 2, 9, 48, 0, TimeSpan.FromHours(2)),
                        "PaidToPick",
                        [new OrderImportItemRequest("ORD-SKU-1", "Order Product", 1, 120m, 70m)],
                        null,
                        null)
                ]),
            CancellationToken.None);

        var products = await productStore.ListAsync(workspace.Workspace.Id, CancellationToken.None);
        var product = Assert.Single(products);
        var orders = await orderStore.ListAsync(workspace.Workspace.Id, CancellationToken.None);
        var order = Assert.Single(orders.Orders);
        var reserved = await orderStore.ReservedByProductAsync(workspace.Workspace.Id, CancellationToken.None);

        Assert.Equal(1, response.ProductsImported);
        Assert.Equal(1, response.OrdersImported);
        Assert.Equal("Paid - to pick", order.Status);
        Assert.Equal(1, reserved[product.Id]);

        var picked = await orderStore.ApplyActionAsync(
            workspace.Workspace.Id,
            order.Id,
            new OrderActionRequest("mark-picked", null),
            CancellationToken.None);
        var updatedProducts = await productStore.ListAsync(workspace.Workspace.Id, CancellationToken.None);
        var updatedReserved = await orderStore.ReservedByProductAsync(workspace.Workspace.Id, CancellationToken.None);

        Assert.NotNull(picked);
        Assert.Equal("Picked - to ship", picked.Summary.Status);
        Assert.Equal(4, Assert.Single(updatedProducts).OnHand);
        Assert.DoesNotContain(product.Id, updatedReserved.Keys);
    }

    [Fact]
    public async Task Dashboard_state_save_preserves_sync_platform_and_flags()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var accessStore = new PostgresAccessStore(connectionString);
        var store = new PostgresDashboardStore(connectionString);
        var user = await accessStore.CreateUserAsync($"Dashboard User {Guid.NewGuid():N}", $"DASHBOARD-{Guid.NewGuid():N}@EXAMPLE.COM", "hash", CancellationToken.None);
        var workspace = await accessStore.CreateWorkspaceAsync(user.Id, new CreateWorkspaceRequest("Dashboard Goods", "IT", "EUR", null), $"retry-{Guid.NewGuid():N}", CancellationToken.None);
        var workspaceId = workspace.Workspace.Id;

        var saved = await store.SaveAsync(
            DashboardWorkspaceState.Create(workspaceId) with
            {
                Mode = "first-sync",
                SyncPlatform = "Amazon",
                EuronicsRetried = true,
                MismatchResolved = true,
                RestockListed = true,
                ShowMoreLowStock = true,
                DetailPanel = "Dashboard action saved."
            },
            CancellationToken.None);
        var loaded = await new PostgresDashboardStore(connectionString).GetAsync(workspaceId, CancellationToken.None);

        Assert.Equal("Amazon", saved.SyncPlatform);
        Assert.True(saved.EuronicsRetried);
        Assert.Equal(saved.SyncPlatform, loaded.SyncPlatform);
        Assert.True(loaded.EuronicsRetried);
        Assert.True(loaded.MismatchResolved);
        Assert.True(loaded.RestockListed);
        Assert.True(loaded.ShowMoreLowStock);
        Assert.Equal("Dashboard action saved.", loaded.DetailPanel);
    }

}
