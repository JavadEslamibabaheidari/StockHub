using System.Collections.Concurrent;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class InMemoryAccessStore : IAccessStore
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();
    private readonly ConcurrentDictionary<Guid, Workspace> _workspaces = new();
    private readonly ConcurrentDictionary<(Guid UserId, Guid WorkspaceId), Membership> _memberships = new();
    private readonly ConcurrentDictionary<(Guid UserId, string Key), Guid> _idempotency = new();
    private readonly ConcurrentDictionary<string, Invitation> _invitations = new(StringComparer.Ordinal);

    public Task<User?> FindUserAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        _users.TryGetValue(normalizedEmail, out var user);

        return Task.FromResult(user);
    }

    public Task<User> CreateUserAsync(
        string fullName,
        string normalizedEmail,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        var user = new User(Guid.NewGuid(), fullName.Trim(), normalizedEmail, passwordHash);

        if (!_users.TryAdd(normalizedEmail, user))
        {
            throw new InvalidOperationException("duplicate_user");
        }

        return Task.FromResult(user);
    }

    public Task<Session> CreateSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = new Session(Guid.NewGuid(), userId, null, DateTimeOffset.UtcNow.AddHours(8));

        _sessions[session.Id] = session;

        return Task.FromResult(session);
    }

    public Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var sessionIsActive = _sessions.TryGetValue(sessionId, out var session)
            && session.ExpiresAt > DateTimeOffset.UtcNow;

        return Task.FromResult(sessionIsActive ? session : null);
    }

    public Task ClearSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        _sessions.TryRemove(sessionId, out _);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<(Workspace Workspace, WorkspaceRole Role)>> ListWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = _memberships.Values
            .Where(membership => membership.UserId == userId)
            .Select(membership => (_workspaces[membership.WorkspaceId], membership.Role))
            .ToArray();

        return Task.FromResult<IReadOnlyList<(Workspace, WorkspaceRole)>>(result);
    }

    public Task<(Workspace Workspace, WorkspaceRole Role)> CreateWorkspaceAsync(
        Guid userId,
        CreateWorkspaceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey)
            && _idempotency.TryGetValue((userId, idempotencyKey), out var existingId))
        {
            return Task.FromResult((_workspaces[existingId], WorkspaceRole.Owner));
        }

        var slug = Slugify(request.BusinessName);
        var suffix = 1;

        while (_workspaces.Values.Any(workspace => workspace.Slug == slug))
        {
            slug = $"{Slugify(request.BusinessName)}-{suffix++}";
        }

        var workspace = new Workspace(
            Guid.NewGuid(),
            request.BusinessName.Trim(),
            request.Country.ToUpperInvariant(),
            request.Currency.ToUpperInvariant(),
            request.VatNumber?.Trim(),
            slug);

        _workspaces[workspace.Id] = workspace;
        _memberships[(userId, workspace.Id)] = new Membership(userId, workspace.Id, WorkspaceRole.Owner);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _idempotency[(userId, idempotencyKey)] = workspace.Id;
        }

        return Task.FromResult((workspace, WorkspaceRole.Owner));
    }

    public Task<bool> SetActiveWorkspaceAsync(
        Guid userId,
        Guid sessionId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (!_memberships.ContainsKey((userId, workspaceId))
            || !_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(false);
        }

        _sessions[sessionId] = session with { ActiveWorkspaceId = workspaceId };

        return Task.FromResult(true);
    }

    public Task CreateInvitationAsync(
        string tokenHash,
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        _invitations[tokenHash] = new Invitation(workspaceId, email.Trim(), role, expiresAt);

        return Task.CompletedTask;
    }

    public Task<bool> InvitationExistsAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var exists = _invitations.TryGetValue(tokenHash, out var invitation)
            && invitation.ExpiresAt > DateTimeOffset.UtcNow;

        return Task.FromResult(exists);
    }

    public Task RecordOnboardingActionAsync(
        Guid userId,
        Guid workspaceId,
        string actionKey,
        CancellationToken cancellationToken) => Task.CompletedTask;

    private static string Slugify(string value)
    {
        var slugParts = value
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join('-', slugParts).Replace("'", string.Empty);
    }
}
