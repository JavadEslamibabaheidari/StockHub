namespace StockHub.Api;

public enum WorkspaceRole { Owner, Admin, Manager, WarehouseStaff, Viewer }

public sealed record User(Guid Id, string FullName, string NormalizedEmail, string PasswordHash);
public sealed record Workspace(Guid Id, string BusinessName, string Country, string Currency, string? VatNumber, string Slug);
public sealed record Membership(Guid UserId, Guid WorkspaceId, WorkspaceRole Role);
public sealed record Session(Guid Id, Guid UserId, Guid? ActiveWorkspaceId, DateTimeOffset ExpiresAt);

public sealed record SignUpRequest(string FullName, string Email, string Password);
public sealed record SignInRequest(string Email, string Password);
public sealed record CreateWorkspaceRequest(string BusinessName, string Country, string Currency, string? VatNumber);
public sealed record SetActiveWorkspaceRequest(Guid WorkspaceId);
public sealed record SessionResponse(Guid UserId, string FullName, string Email, Guid? ActiveWorkspaceId, IReadOnlyList<WorkspaceSummary> Workspaces);
public sealed record WorkspaceSummary(Guid Id, string BusinessName, string Country, string Currency, WorkspaceRole Role);
public sealed record WorkspaceResponse(Guid Id, string BusinessName, string Country, string Currency, string? VatNumber, string Slug, WorkspaceRole Role);
public sealed record Problem(string Title, string Detail, string? Code = null);
