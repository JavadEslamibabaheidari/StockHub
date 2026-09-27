using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;
using StockHub.Api.Validation;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=stockhub;Username=stockhub;Password=stockhub";

builder.Services.AddSingleton<IAccessStore>(_ => new PostgresAccessStore(connectionString));
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "stockhub.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Configuration.GetValue(
            "Authentication:SecureCookies",
            !builder.Environment.IsDevelopment())
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            if (!Guid.TryParse(context.Principal?.FindFirstValue("session_id"), out var sessionId)
                || !Guid.TryParse(context.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                context.RejectPrincipal();
                return;
            }

            var store = context.HttpContext.RequestServices.GetRequiredService<IAccessStore>();
            var session = await store.FindSessionAsync(sessionId, context.HttpContext.RequestAborted);
            if (session is null || session.UserId != userId)
            {
                context.RejectPrincipal();
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var app = builder.Build();
if (app.Configuration.GetValue("Database:ApplyMigrations", true))
{
    await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
}

app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

    await Results.Problem("An unexpected error occurred.", statusCode: 500).ExecuteAsync(context);
}));

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost(
    "/api/auth/sign-up",
    async (
        SignUpRequest request,
        IAccessStore store,
        IPasswordHasher<User> hasher,
        HttpContext context,
        CancellationToken cancellationToken) =>
    {
        var error = AccessValidation.SignUp(request);

        if (error is not null)
        {
            return Results.UnprocessableEntity(error);
        }

        var normalized = request.Email.Trim().ToUpperInvariant();

        if (await store.FindUserAsync(normalized, cancellationToken) is not null)
        {
            return Results.Conflict(new Problem(
                "Unable to create account",
                "The submitted account could not be created.",
                "account_unavailable"));
        }

        var draft = new User(Guid.Empty, request.FullName, normalized, string.Empty);
        var passwordHash = hasher.HashPassword(draft, request.Password);
        User user;
        try
        {
            user = await store.CreateUserAsync(request.FullName, normalized, passwordHash, cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.Message == "duplicate_user")
        {
            return Results.Conflict(new Problem(
                "Unable to create account",
                "The submitted account could not be created.",
                "account_unavailable"));
        }
        var session = await store.CreateSessionAsync(user.Id, cancellationToken);

        await SignIn(context, user, session.Id);

        return Results.Created("/api/auth/session", new { next = "/workspace/create" });
    })
    .WithTags("Auth");

app.MapPost(
    "/api/auth/sign-in",
    async (
        SignInRequest request,
        IAccessStore store,
        IPasswordHasher<User> hasher,
        HttpContext context,
        CancellationToken cancellationToken) =>
    {
        var error = AccessValidation.SignIn(request);

        if (error is not null)
        {
            return Results.UnprocessableEntity(error);
        }

        var user = await store.FindUserAsync(request.Email.Trim().ToUpperInvariant(), cancellationToken);

        if (user is null
            || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        var session = await store.CreateSessionAsync(user.Id, cancellationToken);

        await SignIn(context, user, session.Id);

        return Results.Ok(new { next = "/workspace" });
    })
    .WithTags("Auth");

app.MapPost(
    "/api/auth/sign-out",
    async (HttpContext context, IAccessStore store, CancellationToken cancellationToken) =>
    {
        if (Guid.TryParse(context.User.FindFirstValue("session_id"), out var sessionId))
        {
            await store.ClearSessionAsync(sessionId, cancellationToken);
        }

        await context.SignOutAsync();

        return Results.NoContent();
    })
    .RequireAuthorization()
    .WithTags("Auth");

app.MapGet(
    "/api/auth/session",
    async (ClaimsPrincipal principal, IAccessStore store, CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var workspaces = await store.ListWorkspacesAsync(userId, cancellationToken);
        var activeWorkspaceId = Guid.TryParse(principal.FindFirstValue("session_id"), out var sessionId)
            ? (await store.FindSessionAsync(sessionId, cancellationToken))?.ActiveWorkspaceId
            : null;

        return Results.Ok(new SessionResponse(
            userId,
            principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            activeWorkspaceId,
            workspaces
                .Select(workspace => new WorkspaceSummary(
                    workspace.Workspace.Id,
                    workspace.Workspace.BusinessName,
                    workspace.Workspace.Country,
                    workspace.Workspace.Currency,
                    workspace.Role))
                .ToArray()));
    })
    .RequireAuthorization()
    .WithTags("Auth");

app.MapGet(
    "/api/workspaces",
    async (ClaimsPrincipal principal, IAccessStore store, CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await store.ListWorkspacesAsync(userId, cancellationToken);

        return Results.Ok(result.Select(workspace => new WorkspaceSummary(
            workspace.Workspace.Id,
            workspace.Workspace.BusinessName,
            workspace.Workspace.Country,
            workspace.Workspace.Currency,
            workspace.Role)));
    })
    .RequireAuthorization()
    .WithTags("Workspaces");

app.MapPost(
    "/api/workspaces",
    async (
        CreateWorkspaceRequest request,
        ClaimsPrincipal principal,
        IAccessStore store,
        HttpRequest httpRequest,
        CancellationToken cancellationToken) =>
    {
        var error = AccessValidation.Workspace(request);

        if (error is not null)
        {
            return Results.UnprocessableEntity(error);
        }

        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var key = httpRequest.Headers["Idempotency-Key"].ToString();
        var result = await store.CreateWorkspaceAsync(userId, request, key, cancellationToken);

        return Results.Created(
            $"/api/workspaces/{result.Workspace.Id}",
            new WorkspaceResponse(
                result.Workspace.Id,
                result.Workspace.BusinessName,
                result.Workspace.Country,
                result.Workspace.Currency,
                result.Workspace.VatNumber,
                result.Workspace.Slug,
                result.Role));
    })
    .RequireAuthorization()
    .WithTags("Workspaces");

app.MapPut(
    "/api/workspaces/{workspaceId:guid}/active",
    async (
        Guid workspaceId,
        ClaimsPrincipal principal,
        IAccessStore store,
        HttpContext context,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Guid.TryParse(principal.FindFirstValue("session_id"), out var sessionId))
        {
            return Results.Unauthorized();
        }

        if (!await store.SetActiveWorkspaceAsync(userId, sessionId, workspaceId, cancellationToken))
        {
            return Results.Forbid();
        }

        await SignIn(
            context,
            new User(
                userId,
                principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                string.Empty),
            sessionId,
            workspaceId);

        return Results.NoContent();
    })
    .RequireAuthorization()
    .WithTags("Workspaces");

app.MapGet(
    "/api/auth/google/start",
    (IConfiguration configuration) =>
    {
        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
            || string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
        {
            return Results.Problem(
                "Google sign-in is not configured for this environment.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["code"] = "google_not_configured" });
        }

        return Results.Redirect("/api/auth/google/callback?state=configuration-required");
    })
    .WithTags("Auth");

app.MapGet(
    "/api/auth/google/callback",
    () => Results.Problem(
        "Google sign-in requires a configured provider adapter.",
        statusCode: StatusCodes.Status503ServiceUnavailable,
        extensions: new Dictionary<string, object?> { ["code"] = "google_adapter_unavailable" }))
    .WithTags("Auth");

app.MapGet(
    "/api/workspaces/{workspaceId:guid}/onboarding",
    async (
        Guid workspaceId,
        ClaimsPrincipal principal,
        IAccessStore store,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var isMember = (await store.ListWorkspacesAsync(userId, cancellationToken))
            .Any(workspace => workspace.Workspace.Id == workspaceId);

        if (!isMember)
        {
            return Results.Forbid();
        }

        return Results.Ok(new[]
        {
            new OnboardingTask("import-products", "Import products", "available", "inventory"),
            new OnboardingTask("connect-platform", "Connect a platform", "available", "platforms"),
            new OnboardingTask("invite-team", "Invite your team", "available", "team")
        });
    })
    .RequireAuthorization()
    .WithTags("Onboarding");

app.MapPost(
    "/api/workspaces/{workspaceId:guid}/onboarding/actions",
    async (
        Guid workspaceId,
        OnboardingActionRequest request,
        ClaimsPrincipal principal,
        IAccessStore store,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var isMember = (await store.ListWorkspacesAsync(userId, cancellationToken))
            .Any(workspace => workspace.Workspace.Id == workspaceId);

        if (!isMember)
        {
            return Results.Forbid();
        }

        await store.RecordOnboardingActionAsync(userId, workspaceId, request.Key, cancellationToken);

        return Results.Accepted(
            $"/api/workspaces/{workspaceId}/onboarding",
            new { selected = request.Key, completed = false });
    })
    .RequireAuthorization()
    .WithTags("Onboarding");

app.MapPost(
    "/api/workspaces/{workspaceId:guid}/invitations",
    async (
        Guid workspaceId,
        InvitationRequest request,
        ClaimsPrincipal principal,
        IAccessStore store,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var membership = (await store.ListWorkspacesAsync(userId, cancellationToken))
            .FirstOrDefault(workspace => workspace.Workspace.Id == workspaceId);

        if (membership.Workspace is null
            || membership.Role is not (WorkspaceRole.Owner or WorkspaceRole.Admin))
        {
            return Results.Forbid();
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        await store.CreateInvitationAsync(
            tokenHash,
            workspaceId,
            request.Email,
            request.Role,
            DateTimeOffset.UtcNow.AddHours(72),
            cancellationToken);

        return Results.Accepted(
            $"/api/invitations/{rawToken}",
            new { status = "requested", expiresInHours = 72 });
    })
    .RequireAuthorization()
    .WithTags("Invitations");

app.MapGet(
    "/api/invitations/{token}",
    async (string token, IAccessStore store, CancellationToken cancellationToken) =>
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        return await store.InvitationExistsAsync(hash, cancellationToken)
            ? Results.Ok(new { status = "pending" })
            : Results.NotFound();
    })
    .WithTags("Invitations");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallbackToFile("index.html");

app.Run();

static Task SignIn(HttpContext context, User user, Guid sessionId, Guid? activeWorkspaceId = null)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.NormalizedEmail),
        new Claim("session_id", sessionId.ToString())
    }.Concat(activeWorkspaceId is null
        ? Array.Empty<Claim>()
        : new[] { new Claim("active_workspace_id", activeWorkspaceId.Value.ToString()) });

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

    return context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));
}

public partial class Program
{
}
