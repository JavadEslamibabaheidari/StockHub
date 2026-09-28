namespace StockHub.Api.Tests;

using StockHub.Api.Contracts;
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;

public sealed class InMemoryAccessStoreTests
{
    [Fact]
    public async Task Signup_creates_one_user_and_session_and_rejects_duplicate_email()
    {
        var store = new InMemoryAccessStore();
        var email = "PERSONAL@GMAIL.COM";

        var first = await store.CreateUserWithSessionAsync("Personal User", email, "hash", CancellationToken.None);
        Assert.Equal(first.User.Id, first.Session.UserId);
        Assert.NotNull(await store.FindSessionAsync(first.Session.Id, CancellationToken.None));
        Assert.Equal(first.User.Id, (await store.FindUserAsync(email, CancellationToken.None))?.Id);

        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.CreateUserWithSessionAsync("Other User", email, "other-hash", CancellationToken.None));
        Assert.Equal("duplicate_user", duplicate.Message);
        Assert.Equal(first.User.Id, (await store.FindUserAsync(email, CancellationToken.None))?.Id);
    }

    [Fact]
    public async Task Google_identity_reuses_verified_gmail_account_and_rejects_untrusted_link()
    {
        var store = new InMemoryAccessStore();
        var account = await store.CreateUserAsync("Personal User", "PERSONAL@GMAIL.COM", "hash", CancellationToken.None);

        var linked = await store.GetOrCreateGoogleUserAsync(
            "google-sub-1", account.NormalizedEmail, "Google Name", true, CancellationToken.None);
        var again = await store.GetOrCreateGoogleUserAsync(
            "google-sub-1", account.NormalizedEmail, "Changed Name", true, CancellationToken.None);
        Assert.Equal(account.Id, linked.Id);
        Assert.Equal(account.Id, again.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetOrCreateGoogleUserAsync(
            "google-sub-2", account.NormalizedEmail, "Another Person", true, CancellationToken.None));

        var other = await store.CreateUserAsync("Outside User", "OUTSIDE@EXAMPLE.COM", "hash", CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetOrCreateGoogleUserAsync(
            "google-sub-3", other.NormalizedEmail, "Outside User", false, CancellationToken.None));
    }

    [Fact]
    public async Task Password_reset_is_single_use_and_revokes_existing_sessions()
    {
        var store = new InMemoryAccessStore();
        var signup = await store.CreateUserWithSessionAsync("Ada", "ADA@EXAMPLE.COM", "old", CancellationToken.None);
        await store.CreatePasswordResetAsync(signup.User.Id, "token-hash", DateTimeOffset.UtcNow.AddMinutes(30), CancellationToken.None);

        Assert.True(await store.ResetPasswordAsync("token-hash", "new", CancellationToken.None));
        Assert.False(await store.ResetPasswordAsync("token-hash", "another", CancellationToken.None));
        Assert.Null(await store.FindSessionAsync(signup.Session.Id, CancellationToken.None));
        Assert.Equal("new", (await store.FindUserAsync(signup.User.NormalizedEmail, CancellationToken.None))?.PasswordHash);

        await store.CreatePasswordResetAsync(signup.User.Id, "expired", DateTimeOffset.UtcNow.AddSeconds(-1), CancellationToken.None);
        Assert.False(await store.ResetPasswordAsync("expired", "another", CancellationToken.None));
    }

    [Fact]
    public async Task Workspace_creation_is_idempotent_and_assigns_owner()
    {
        var store = new InMemoryAccessStore();
        var user = await store.CreateUserAsync("Ada Lovelace", "ADA@EXAMPLE.COM", "hash", CancellationToken.None);
        var request = new CreateWorkspaceRequest("Acme Goods", "IT", "EUR", null);

        var first = await store.CreateWorkspaceAsync(user.Id, request, "retry-1", CancellationToken.None);
        var second = await store.CreateWorkspaceAsync(user.Id, request, "retry-1", CancellationToken.None);

        Assert.Equal(first.Workspace.Id, second.Workspace.Id);
        var memberships = await store.ListWorkspacesAsync(user.Id, CancellationToken.None);
        var membership = Assert.Single(memberships);
        Assert.Equal(WorkspaceRole.Owner, membership.Role);
        Assert.Equal("acme-goods", first.Workspace.Slug);
    }

    [Fact]
    public async Task Active_workspace_can_only_be_selected_for_a_membership()
    {
        var store = new InMemoryAccessStore();
        var user = await store.CreateUserAsync("Ada Lovelace", "ADA@EXAMPLE.COM", "hash", CancellationToken.None);
        var other = await store.CreateUserAsync("Grace Hopper", "GRACE@EXAMPLE.COM", "hash", CancellationToken.None);
        var session = await store.CreateSessionAsync(user.Id, CancellationToken.None);
        var workspace = await store.CreateWorkspaceAsync(other.Id, new("Other", "IT", "EUR", null), "other", CancellationToken.None);

        Assert.False(await store.SetActiveWorkspaceAsync(user.Id, session.Id, workspace.Workspace.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Duplicate_business_names_receive_unique_slugs()
    {
        var store = new InMemoryAccessStore();
        var user = await store.CreateUserAsync("Ada", "ADA@EXAMPLE.COM", "hash", CancellationToken.None);
        var request = new CreateWorkspaceRequest("Acme Goods", "IT", "EUR", null);

        var first = await store.CreateWorkspaceAsync(user.Id, request, "one", CancellationToken.None);
        var second = await store.CreateWorkspaceAsync(user.Id, request, "two", CancellationToken.None);

        Assert.NotEqual(first.Workspace.Slug, second.Workspace.Slug);
    }
}
