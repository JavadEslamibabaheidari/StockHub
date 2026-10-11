using StockHub.Api.Domain;

namespace StockHub.Api.Contracts;

public sealed record TeamMemberResponse(
    Guid UserId,
    string FullName,
    string Email,
    WorkspaceRole Role,
    string LastActive);

public sealed record PendingInvitationResponse(
    Guid Id,
    string Email,
    WorkspaceRole Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public sealed record TeamResponse(
    IReadOnlyList<TeamMemberResponse> Members,
    IReadOnlyList<PendingInvitationResponse> Invitations,
    bool CanManage);
