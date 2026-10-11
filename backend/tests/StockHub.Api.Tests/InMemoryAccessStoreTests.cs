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
    public async Task Onboarding_selection_is_persisted_per_user_and_workspace_without_completing_the_task()
    {
        var store = new InMemoryAccessStore();
        var user = await store.CreateUserAsync("Ada", "ADA@EXAMPLE.COM", "hash", CancellationToken.None);
        var firstWorkspace = await store.CreateWorkspaceAsync(
            user.Id,
            new CreateWorkspaceRequest("North Shop", "IT", "EUR", null),
            "onboarding-1",
            CancellationToken.None);
        var secondWorkspace = await store.CreateWorkspaceAsync(
            user.Id,
            new CreateWorkspaceRequest("South Shop", "IT", "EUR", null),
            "onboarding-2",
            CancellationToken.None);

        Assert.Null(await store.GetSelectedOnboardingActionAsync(user.Id, firstWorkspace.Workspace.Id, CancellationToken.None));
        await store.RecordOnboardingActionAsync(user.Id, firstWorkspace.Workspace.Id, "connect-platform", CancellationToken.None);

        Assert.Equal("connect-platform", await store.GetSelectedOnboardingActionAsync(user.Id, firstWorkspace.Workspace.Id, CancellationToken.None));
        Assert.Null(await store.GetSelectedOnboardingActionAsync(user.Id, secondWorkspace.Workspace.Id, CancellationToken.None));
        Assert.Equal("selected", Assert.Single(OnboardingTask.CreateChecklist("connect-platform"), task => task.Key == "connect-platform").Status);
        Assert.All(OnboardingTask.CreateChecklist("connect-platform"), task => Assert.NotEqual("completed", task.Status));
        await Assert.ThrowsAsync<ArgumentException>(() => store.RecordOnboardingActionAsync(
            user.Id,
            firstWorkspace.Workspace.Id,
            "unknown",
            CancellationToken.None));
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

    [Fact]
    public async Task Invitation_acceptance_matches_email_and_is_single_use()
    {
        var store = new InMemoryAccessStore();
        var owner = await store.CreateUserAsync("Owner", "OWNER@EXAMPLE.COM", "hash", CancellationToken.None);
        var invited = await store.CreateUserAsync("Invited", "INVITED@EXAMPLE.COM", "hash", CancellationToken.None);
        var workspace = await store.CreateWorkspaceAsync(owner.Id, new("Acme Goods", "IT", "EUR", null), "workspace", CancellationToken.None);

        await store.CreateInvitationAsync(
            Guid.NewGuid(),
            "token-hash",
            workspace.Workspace.Id,
            "invited@example.com",
            WorkspaceRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1),
            CancellationToken.None);

        Assert.Null(await store.AcceptInvitationAsync("token-hash", owner.Id, owner.NormalizedEmail, CancellationToken.None));
        Assert.Equal(workspace.Workspace.Id, await store.AcceptInvitationAsync("token-hash", invited.Id, invited.NormalizedEmail, CancellationToken.None));
        Assert.Null(await store.AcceptInvitationAsync("token-hash", invited.Id, invited.NormalizedEmail, CancellationToken.None));

        var memberships = await store.ListWorkspacesAsync(invited.Id, CancellationToken.None);
        var membership = Assert.Single(memberships);
        Assert.Equal(WorkspaceRole.Viewer, membership.Role);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateInvitationAsync(
            Guid.NewGuid(), "duplicate-member-token", workspace.Workspace.Id, invited.NormalizedEmail, WorkspaceRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1), CancellationToken.None));

        await store.CreateInvitationAsync(Guid.NewGuid(), "expired-token", workspace.Workspace.Id, "expired@example.com",
            WorkspaceRole.Manager, DateTimeOffset.UtcNow.AddSeconds(-1), CancellationToken.None);
        Assert.Null(await store.FindInvitationAsync("expired-token", CancellationToken.None));
        Assert.Empty(await store.ListPendingInvitationsAsync(workspace.Workspace.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Team_invitations_rotate_cancel_and_removals_preserve_the_last_owner()
    {
        var store = new InMemoryAccessStore();
        var owner = await store.CreateUserAsync("Owner", "OWNER@EXAMPLE.COM", "hash", CancellationToken.None);
        var teammate = await store.CreateUserAsync("Teammate", "TEAMMATE@EXAMPLE.COM", "hash", CancellationToken.None);
        var otherOwner = await store.CreateUserAsync("Other Owner", "OTHER@EXAMPLE.COM", "hash", CancellationToken.None);
        var workspace = await store.CreateWorkspaceAsync(owner.Id, new("Acme", "IT", "EUR", null), "team", CancellationToken.None);
        var workspaceId = workspace.Workspace.Id;
        var otherWorkspace = await store.CreateWorkspaceAsync(otherOwner.Id, new("Other", "IT", "EUR", null), "other-team", CancellationToken.None);
        var invitationId = Guid.NewGuid();
        await store.CreateInvitationAsync(invitationId, "old-token-hash", workspaceId, teammate.NormalizedEmail,
            WorkspaceRole.Manager, DateTimeOffset.UtcNow.AddHours(72), CancellationToken.None);

        var team = await store.ListPendingInvitationsAsync(workspaceId, CancellationToken.None);
        Assert.Equal(invitationId, Assert.Single(team).Id);
        Assert.Empty(await store.ListPendingInvitationsAsync(otherWorkspace.Workspace.Id, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateInvitationAsync(
            Guid.NewGuid(), "duplicate-token", workspaceId, teammate.NormalizedEmail, WorkspaceRole.Manager,
            DateTimeOffset.UtcNow.AddHours(72), CancellationToken.None));
        Assert.True(await store.RotateInvitationAsync(workspaceId, invitationId, "new-token-hash", DateTimeOffset.UtcNow.AddHours(72), CancellationToken.None));
        Assert.Null(await store.FindInvitationAsync("old-token-hash", CancellationToken.None));
        Assert.NotNull(await store.FindInvitationAsync("new-token-hash", CancellationToken.None));
        Assert.True(await store.CancelInvitationAsync(workspaceId, invitationId, CancellationToken.None));
        Assert.Empty(await store.ListPendingInvitationsAsync(workspaceId, CancellationToken.None));

        // A fresh invite demonstrates membership removal behavior after acceptance.
        await store.CreateInvitationAsync(Guid.NewGuid(), "accepted-token", workspaceId, teammate.NormalizedEmail,
            WorkspaceRole.Manager, DateTimeOffset.UtcNow.AddHours(72), CancellationToken.None);
        Assert.Equal(workspaceId, await store.AcceptInvitationAsync("accepted-token", teammate.Id, teammate.NormalizedEmail, CancellationToken.None));
        Assert.Equal(2, (await store.ListTeamMembersAsync(workspaceId, CancellationToken.None)).Count);
        Assert.False(await store.RemoveMemberAsync(workspaceId, owner.Id, CancellationToken.None));
        Assert.True(await store.RemoveMemberAsync(workspaceId, teammate.Id, CancellationToken.None));
        Assert.Single(await store.ListTeamMembersAsync(workspaceId, CancellationToken.None));
        Assert.DoesNotContain(await store.ListTeamMembersAsync(workspaceId, CancellationToken.None), member => member.UserId == teammate.Id);
        Assert.Empty(await store.ListWorkspacesAsync(teammate.Id, CancellationToken.None));
    }

}
