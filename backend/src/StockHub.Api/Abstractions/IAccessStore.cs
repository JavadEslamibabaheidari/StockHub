using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Abstractions;

public interface IAccessStore
{
    Task<User?> FindUserAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User> CreateUserAsync(
        string fullName,
        string normalizedEmail,
        string passwordHash,
        CancellationToken cancellationToken);

    Task<Session> CreateSessionAsync(Guid userId, CancellationToken cancellationToken);

    Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    Task ClearSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<(Workspace Workspace, WorkspaceRole Role)>> ListWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<(Workspace Workspace, WorkspaceRole Role)> CreateWorkspaceAsync(
        Guid userId,
        CreateWorkspaceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<bool> SetActiveWorkspaceAsync(
        Guid userId,
        Guid sessionId,
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task CreateInvitationAsync(
        string tokenHash,
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task<bool> InvitationExistsAsync(string tokenHash, CancellationToken cancellationToken);

    Task RecordOnboardingActionAsync(
        Guid userId,
        Guid workspaceId,
        string actionKey,
        CancellationToken cancellationToken);
}
