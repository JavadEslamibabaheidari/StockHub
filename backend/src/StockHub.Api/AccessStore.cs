using System.Collections.Concurrent;
namespace StockHub.Api;

public interface IAccessStore
{
    Task<User?> FindUserAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User> CreateUserAsync(string fullName, string normalizedEmail, string passwordHash, CancellationToken cancellationToken);
    Task<Session> CreateSessionAsync(Guid userId, CancellationToken cancellationToken);
    Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task ClearSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<(Workspace Workspace, WorkspaceRole Role)>> ListWorkspacesAsync(Guid userId, CancellationToken cancellationToken);
    Task<(Workspace Workspace, WorkspaceRole Role)> CreateWorkspaceAsync(Guid userId, CreateWorkspaceRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<bool> SetActiveWorkspaceAsync(Guid userId, Guid sessionId, Guid workspaceId, CancellationToken cancellationToken);
}

public sealed class InMemoryAccessStore : IAccessStore
{
    private readonly ConcurrentDictionary<string, User> users = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, Session> sessions = new();
    private readonly ConcurrentDictionary<Guid, Workspace> workspaces = new();
    private readonly ConcurrentDictionary<(Guid UserId, Guid WorkspaceId), Membership> memberships = new();
    private readonly ConcurrentDictionary<(Guid UserId, string Key), Guid> idempotency = new();
    public Task<User?> FindUserAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(users.TryGetValue(normalizedEmail, out var user) ? user : null);

    public Task<User> CreateUserAsync(string fullName, string normalizedEmail, string passwordHash, CancellationToken cancellationToken)
    {
        var user = new User(Guid.NewGuid(), fullName.Trim(), normalizedEmail, passwordHash);
        if (!users.TryAdd(normalizedEmail, user)) throw new InvalidOperationException("duplicate_user");
        return Task.FromResult(user);
    }

    public Task<Session> CreateSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = new Session(Guid.NewGuid(), userId, null, DateTimeOffset.UtcNow.AddHours(8));
        sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(sessions.TryGetValue(sessionId, out var session) && session.ExpiresAt > DateTimeOffset.UtcNow ? session : null);

    public Task ClearSessionAsync(Guid sessionId, CancellationToken cancellationToken) { sessions.TryRemove(sessionId, out _); return Task.CompletedTask; }

    public Task<IReadOnlyList<(Workspace Workspace, WorkspaceRole Role)>> ListWorkspacesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var result = memberships.Values.Where(m => m.UserId == userId)
            .Select(m => (workspaces[m.WorkspaceId], m.Role)).ToArray();
        return Task.FromResult<IReadOnlyList<(Workspace, WorkspaceRole)>>(result);
    }

    public Task<(Workspace Workspace, WorkspaceRole Role)> CreateWorkspaceAsync(Guid userId, CreateWorkspaceRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && idempotency.TryGetValue((userId, idempotencyKey), out var existingId))
            return Task.FromResult((workspaces[existingId], WorkspaceRole.Owner));

        var slug = Slugify(request.BusinessName);
        var suffix = 1;
        while (workspaces.Values.Any(w => w.Slug == slug)) slug = $"{Slugify(request.BusinessName)}-{suffix++}";
        var workspace = new Workspace(Guid.NewGuid(), request.BusinessName.Trim(), request.Country.ToUpperInvariant(), request.Currency.ToUpperInvariant(), request.VatNumber?.Trim(), slug);
        workspaces[workspace.Id] = workspace;
        memberships[(userId, workspace.Id)] = new Membership(userId, workspace.Id, WorkspaceRole.Owner);
        if (!string.IsNullOrWhiteSpace(idempotencyKey)) idempotency[(userId, idempotencyKey)] = workspace.Id;
        return Task.FromResult((workspace, WorkspaceRole.Owner));
    }

    public Task<bool> SetActiveWorkspaceAsync(Guid userId, Guid sessionId, Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!memberships.ContainsKey((userId, workspaceId)) || !sessions.TryGetValue(sessionId, out var session)) return Task.FromResult(false);
        sessions[sessionId] = session with { ActiveWorkspaceId = workspaceId };
        return Task.FromResult(true);
    }

    private static string Slugify(string value) => string.Join('-', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Replace("'", "");
}
