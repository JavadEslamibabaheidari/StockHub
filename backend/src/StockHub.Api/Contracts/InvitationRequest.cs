using StockHub.Api.Domain;

namespace StockHub.Api.Contracts;

public sealed record InvitationRequest(string Email, WorkspaceRole Role);
