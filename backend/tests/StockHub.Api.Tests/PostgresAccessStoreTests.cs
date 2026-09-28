namespace StockHub.Api.Tests;

using StockHub.Api.Contracts;
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
}
