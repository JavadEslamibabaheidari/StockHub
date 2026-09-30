using StockHub.Api.Domain;

namespace StockHub.Api.Contracts;

public sealed record InvitationResponse(string Email, WorkspaceRole Role, string Status);
