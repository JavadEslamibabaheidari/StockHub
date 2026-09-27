using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<StockHub.Api.IAccessStore, StockHub.Api.InMemoryAccessStore>();
builder.Services.AddSingleton<IPasswordHasher<StockHub.Api.User>, PasswordHasher<StockHub.Api.User>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "stockhub.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
var app = builder.Build();
var invitations = new System.Collections.Concurrent.ConcurrentDictionary<string, (Guid WorkspaceId, string Email, StockHub.Api.WorkspaceRole Role)>();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    context.Response.StatusCode = 500;
    await Results.Problem("An unexpected error occurred.", statusCode: 500).ExecuteAsync(context);
}));
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/sign-up", async (StockHub.Api.SignUpRequest request, StockHub.Api.IAccessStore store, IPasswordHasher<StockHub.Api.User> hasher, HttpContext context, CancellationToken cancellationToken) =>
{
    var error = StockHub.Api.AccessValidation.SignUp(request);
    if (error is not null) return Results.UnprocessableEntity(error);
    var normalized = request.Email.Trim().ToUpperInvariant();
    if (await store.FindUserAsync(normalized, cancellationToken) is not null) return Results.Conflict(new StockHub.Api.Problem("Unable to create account", "The submitted account could not be created.", "account_unavailable"));
    var draft = new StockHub.Api.User(Guid.Empty, request.FullName, normalized, string.Empty);
    var user = await store.CreateUserAsync(request.FullName, normalized, hasher.HashPassword(draft, request.Password), cancellationToken);
    var session = await store.CreateSessionAsync(user.Id, cancellationToken);
    await SignIn(context, user, session.Id);
    return Results.Created("/api/auth/session", new { next = "/workspace/create" });
}).WithTags("Auth");

app.MapPost("/api/auth/sign-in", async (StockHub.Api.SignInRequest request, StockHub.Api.IAccessStore store, IPasswordHasher<StockHub.Api.User> hasher, HttpContext context, CancellationToken cancellationToken) =>
{
    var error = StockHub.Api.AccessValidation.SignIn(request);
    if (error is not null) return Results.UnprocessableEntity(error);
    var user = await store.FindUserAsync(request.Email.Trim().ToUpperInvariant(), cancellationToken);
    if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed) return Results.Unauthorized();
    var session = await store.CreateSessionAsync(user.Id, cancellationToken);
    await SignIn(context, user, session.Id);
    return Results.Ok(new { next = "/workspace" });
}).WithTags("Auth");

app.MapPost("/api/auth/sign-out", async (HttpContext context, StockHub.Api.IAccessStore store, CancellationToken cancellationToken) =>
{
    if (Guid.TryParse(context.User.FindFirstValue("session_id"), out var sessionId)) await store.ClearSessionAsync(sessionId, cancellationToken);
    await context.SignOutAsync();
    return Results.NoContent();
}).RequireAuthorization().WithTags("Auth");

app.MapGet("/api/auth/session", async (ClaimsPrincipal principal, StockHub.Api.IAccessStore store, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
    var workspaces = await store.ListWorkspacesAsync(userId, cancellationToken);
    return Results.Ok(new StockHub.Api.SessionResponse(userId, principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty, principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty, Guid.TryParse(principal.FindFirstValue("active_workspace_id"), out var active) ? active : null, workspaces.Select(x => new StockHub.Api.WorkspaceSummary(x.Workspace.Id, x.Workspace.BusinessName, x.Workspace.Country, x.Workspace.Currency, x.Role)).ToArray()));
}).RequireAuthorization().WithTags("Auth");

app.MapGet("/api/workspaces", async (ClaimsPrincipal principal, StockHub.Api.IAccessStore store, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
    var result = await store.ListWorkspacesAsync(userId, cancellationToken);
    return Results.Ok(result.Select(x => new StockHub.Api.WorkspaceSummary(x.Workspace.Id, x.Workspace.BusinessName, x.Workspace.Country, x.Workspace.Currency, x.Role)));
}).RequireAuthorization().WithTags("Workspaces");

app.MapPost("/api/workspaces", async (StockHub.Api.CreateWorkspaceRequest request, ClaimsPrincipal principal, StockHub.Api.IAccessStore store, HttpRequest httpRequest, CancellationToken cancellationToken) =>
{
    var error = StockHub.Api.AccessValidation.Workspace(request);
    if (error is not null) return Results.UnprocessableEntity(error);
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
    var key = httpRequest.Headers["Idempotency-Key"].ToString();
    var result = await store.CreateWorkspaceAsync(userId, request, key, cancellationToken);
    return Results.Created($"/api/workspaces/{result.Workspace.Id}", new StockHub.Api.WorkspaceResponse(result.Workspace.Id, result.Workspace.BusinessName, result.Workspace.Country, result.Workspace.Currency, result.Workspace.VatNumber, result.Workspace.Slug, result.Role));
}).RequireAuthorization().WithTags("Workspaces");

app.MapPut("/api/workspaces/{workspaceId:guid}/active", async (Guid workspaceId, ClaimsPrincipal principal, StockHub.Api.IAccessStore store, HttpContext context, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || !Guid.TryParse(principal.FindFirstValue("session_id"), out var sessionId)) return Results.Unauthorized();
    if (!await store.SetActiveWorkspaceAsync(userId, sessionId, workspaceId, cancellationToken)) return Results.Forbid();
    await context.SignInAsync(principal);
    return Results.NoContent();
}).RequireAuthorization().WithTags("Workspaces");

app.MapGet("/api/auth/google/start", (IConfiguration configuration) =>
{
    if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) || string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"])) return Results.Problem("Google sign-in is not configured for this environment.", statusCode: 503, extensions: new Dictionary<string, object?> { ["code"] = "google_not_configured" });
    return Results.Redirect("/api/auth/google/callback?state=configuration-required");
}).WithTags("Auth");

app.MapGet("/api/auth/google/callback", () => Results.Problem("Google sign-in requires a configured provider adapter.", statusCode: 503, extensions: new Dictionary<string, object?> { ["code"] = "google_adapter_unavailable" })).WithTags("Auth");

app.MapGet("/api/workspaces/{workspaceId:guid}/onboarding", async (Guid workspaceId, ClaimsPrincipal principal, StockHub.Api.IAccessStore store, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
    var member = (await store.ListWorkspacesAsync(userId, cancellationToken)).Any(x => x.Workspace.Id == workspaceId);
    if (!member) return Results.Forbid();
    return Results.Ok(new[] { new StockHub.Api.OnboardingTask("import-products", "Import products", "available", "inventory"), new StockHub.Api.OnboardingTask("connect-platform", "Connect a platform", "available", "platforms"), new StockHub.Api.OnboardingTask("invite-team", "Invite your team", "available", "team") });
}).RequireAuthorization().WithTags("Onboarding");

app.MapPost("/api/workspaces/{workspaceId:guid}/onboarding/actions", async (Guid workspaceId, StockHub.Api.OnboardingActionRequest request, ClaimsPrincipal principal, StockHub.Api.IAccessStore store, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
    var member = (await store.ListWorkspacesAsync(userId, cancellationToken)).Any(x => x.Workspace.Id == workspaceId);
    if (!member) return Results.Forbid();
    return Results.Accepted($"/api/workspaces/{workspaceId}/onboarding", new { selected = request.Key, completed = false });
}).RequireAuthorization().WithTags("Onboarding");

app.MapPost("/api/workspaces/{workspaceId:guid}/invitations", async (Guid workspaceId, StockHub.Api.InvitationRequest request, ClaimsPrincipal principal, StockHub.Api.IAccessStore store, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
    var membership = (await store.ListWorkspacesAsync(userId, cancellationToken)).FirstOrDefault(x => x.Workspace.Id == workspaceId);
    if (membership.Workspace is null || membership.Role is not (StockHub.Api.WorkspaceRole.Owner or StockHub.Api.WorkspaceRole.Admin)) return Results.Forbid();
    var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    var tokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
    invitations[tokenHash] = (workspaceId, request.Email.Trim(), request.Role);
    return Results.Accepted($"/api/invitations/{rawToken}", new { status = "requested", expiresInHours = 72 });
}).RequireAuthorization().WithTags("Invitations");

app.MapGet("/api/invitations/{token}", (string token) =>
{
    var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    return invitations.ContainsKey(hash) ? Results.Ok(new { status = "pending" }) : Results.NotFound();
}).WithTags("Invitations");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();

static Task SignIn(HttpContext context, StockHub.Api.User user, Guid sessionId) => context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.FullName), new Claim(ClaimTypes.Email, user.NormalizedEmail), new Claim("session_id", sessionId.ToString())
}, CookieAuthenticationDefaults.AuthenticationScheme)));

public partial class Program { }
