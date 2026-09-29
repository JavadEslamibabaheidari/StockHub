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
    private readonly ConcurrentDictionary<string, (Guid UserId, DateTimeOffset ExpiresAt)> _passwordResets = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Guid> _googleIdentities = new(StringComparer.Ordinal);
    private readonly object _userCreationLock = new();

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

    public Task<(User User, Session Session)> CreateUserWithSessionAsync(
        string fullName,
        string normalizedEmail,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        lock (_userCreationLock)
        {
            if (_users.ContainsKey(normalizedEmail))
            {
                throw new InvalidOperationException("duplicate_user");
            }

            var user = new User(Guid.NewGuid(), fullName.Trim(), normalizedEmail, passwordHash);
            var session = new Session(Guid.NewGuid(), user.Id, null, DateTimeOffset.UtcNow.AddHours(8));
            _users[normalizedEmail] = user;
            _sessions[session.Id] = session;
            return Task.FromResult((user, session));
        }
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

    public Task CreatePasswordResetAsync(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        lock (_userCreationLock)
        {
            foreach (var existing in _passwordResets.Where(item => item.Value.UserId == userId))
            {
                _passwordResets.TryRemove(existing.Key, out _);
            }

            _passwordResets[tokenHash] = (userId, expiresAt);
            return Task.CompletedTask;
        }
    }

    public Task<bool> ResetPasswordAsync(string tokenHash, string passwordHash, CancellationToken cancellationToken)
    {
        lock (_userCreationLock)
        {
            if (!_passwordResets.TryGetValue(tokenHash, out var reset)
                || reset.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Task.FromResult(false);
            }

            var user = _users.Values.FirstOrDefault(item => item.Id == reset.UserId);
            if (user is null)
            {
                return Task.FromResult(false);
            }

            _users[user.NormalizedEmail] = user with { PasswordHash = passwordHash };
            foreach (var session in _sessions.Where(item => item.Value.UserId == user.Id))
            {
                _sessions.TryRemove(session.Key, out _);
            }

            foreach (var existing in _passwordResets.Where(item => item.Value.UserId == user.Id))
            {
                _passwordResets.TryRemove(existing.Key, out _);
            }

            return Task.FromResult(true);
        }
    }

    public Task<User> GetOrCreateGoogleUserAsync(
        string subject,
        string normalizedEmail,
        string fullName,
        bool allowExistingAccountLink,
        CancellationToken cancellationToken)
    {
        lock (_userCreationLock)
        {
            if (_googleIdentities.TryGetValue(subject, out var existingUserId))
            {
                return Task.FromResult(_users.Values.Single(user => user.Id == existingUserId));
            }

            if (_users.TryGetValue(normalizedEmail, out var existing))
            {
                if (!allowExistingAccountLink
                    || _googleIdentities.Values.Contains(existing.Id))
                {
                    throw new InvalidOperationException("external_account_conflict");
                }

                _googleIdentities[subject] = existing.Id;
                return Task.FromResult(existing);
            }

            var user = new User(Guid.NewGuid(), fullName.Trim(), normalizedEmail, string.Empty);
            _users[normalizedEmail] = user;
            _googleIdentities[subject] = user.Id;
            return Task.FromResult(user);
        }
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

    public Task<Invitation?> FindInvitationAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var exists = _invitations.TryGetValue(tokenHash, out var invitation)
            && invitation.ExpiresAt > DateTimeOffset.UtcNow
            && invitation.ConsumedAt is null;

        return Task.FromResult(exists ? invitation : null);
    }

    public Task<Guid?> AcceptInvitationAsync(string tokenHash, Guid userId, string normalizedEmail, CancellationToken cancellationToken)
    {
        if (!_invitations.TryGetValue(tokenHash, out var invitation)
            || invitation.ExpiresAt <= DateTimeOffset.UtcNow
            || invitation.ConsumedAt is not null
            || !string.Equals(invitation.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<Guid?>(null);
        }

        _memberships.TryAdd((userId, invitation.WorkspaceId), new Membership(userId, invitation.WorkspaceId, invitation.Role));
        _invitations[tokenHash] = invitation with { ConsumedAt = DateTimeOffset.UtcNow };

        return Task.FromResult<Guid?>(invitation.WorkspaceId);
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
