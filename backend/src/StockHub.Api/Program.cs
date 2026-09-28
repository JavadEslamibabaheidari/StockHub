using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
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
builder.Services.AddSingleton<IRecoveryEmailSender, SmtpRecoveryEmailSender>();

var authenticationBuilder = builder.Services
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
    })
    .AddCookie("External", options =>
    {
        options.Cookie.Name = "stockhub.external";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Configuration.GetValue(
            "Authentication:SecureCookies",
            !builder.Environment.IsDevelopment())
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    });

var googleConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]);
if (googleConfigured)
{
    authenticationBuilder.AddOpenIdConnect("Google", options =>
    {
        options.Authority = "https://accounts.google.com";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SignInScheme = "External";
        options.CallbackPath = "/api/auth/google/callback";
        options.MapInboundClaims = false;
        options.SaveTokens = false;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Events = new OpenIdConnectEvents
        {
            OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/?authError=google#signin");
                return Task.CompletedTask;
            }
        };
    });
}

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var app = builder.Build();
if (args.Contains("--migrate-only", StringComparer.Ordinal))
{
    await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
    return;
}

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
                "This email cannot create a new account. Try signing in or resetting your password.",
                "account_unavailable"));
        }

        var draft = new User(Guid.Empty, request.FullName, normalized, string.Empty);
        var passwordHash = hasher.HashPassword(draft, request.Password);
        User user;
        Session session;
        try
        {
            (user, session) = await store.CreateUserWithSessionAsync(
                request.FullName,
                normalized,
                passwordHash,
                cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.Message == "duplicate_user")
        {
            return Results.Conflict(new Problem(
                "Unable to create account",
                "This email cannot create a new account. Try signing in or resetting your password.",
                "account_unavailable"));
        }

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

app.MapPost(
    "/api/auth/password-reset/request",
    async (
        PasswordResetRequest request,
        IAccessStore store,
        IRecoveryEmailSender emailSender,
        IConfiguration configuration,
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        if (!AccessValidation.IsValidEmail(request.Email))
        {
            return Results.UnprocessableEntity(new Problem(
                "Validation failed",
                "Enter a valid email address.",
                "email_invalid"));
        }

        if (!emailSender.IsConfigured)
        {
            return Results.Problem(
                "Password recovery email is not configured for this environment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var user = await store.FindUserAsync(request.Email.Trim().ToUpperInvariant(), cancellationToken);
        if (user is not null)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
            await store.CreatePasswordResetAsync(
                user.Id,
                tokenHash,
                DateTimeOffset.UtcNow.AddMinutes(30),
                cancellationToken);

            var baseUri = new Uri(configuration["Application:PublicBaseUrl"]!, UriKind.Absolute);
            var resetLink = new Uri(baseUri, $"/#reset?token={Uri.EscapeDataString(token)}");
            try
            {
                await emailSender.SendPasswordResetAsync(request.Email.Trim(), resetLink, cancellationToken);
            }
            catch (Exception exception) when (exception is System.Net.Mail.SmtpException or InvalidOperationException)
            {
                logger.LogError(exception, "Password recovery email delivery failed");
                return Results.Problem(
                    "Password recovery is temporarily unavailable. Please try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        return Results.Accepted(value: new { message = "If an account uses this email, a reset link is on its way." });
    })
    .WithTags("Auth");

app.MapPost(
    "/api/auth/password-reset/confirm",
    async (
        PasswordResetConfirmRequest request,
        IAccessStore store,
        IPasswordHasher<User> hasher,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return Results.UnprocessableEntity(new Problem(
                "Validation failed",
                "Password must be at least 8 characters.",
                "password_too_short"));
        }

        if (string.IsNullOrEmpty(request.Token) || request.Token.Length != 64 || !request.Token.All(Uri.IsHexDigit))
        {
            return Results.UnprocessableEntity(new Problem(
                "Invalid link",
                "This password reset link is invalid or has expired.",
                "reset_invalid"));
        }

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var passwordHash = hasher.HashPassword(
            new User(Guid.Empty, string.Empty, string.Empty, string.Empty),
            request.NewPassword);
        if (!await store.ResetPasswordAsync(tokenHash, passwordHash, cancellationToken))
        {
            return Results.UnprocessableEntity(new Problem(
                "Invalid link",
                "This password reset link is invalid or has expired.",
                "reset_invalid"));
        }

        return Results.NoContent();
    })
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
    "/api/auth/google/availability",
    () => Results.Ok(new { available = googleConfigured }))
    .WithTags("Auth");

app.MapGet(
    "/api/auth/google/start",
    () =>
    {
        if (!googleConfigured)
        {
            return Results.Problem(
                "Google sign-in is not configured for this environment.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["code"] = "google_not_configured" });
        }

        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = "/api/auth/google/complete" },
            ["Google"]);
    })
    .WithTags("Auth");

app.MapGet(
    "/api/auth/google/complete",
    async (HttpContext context, IAccessStore store, CancellationToken cancellationToken) =>
    {
        var external = await context.AuthenticateAsync("External");
        if (!external.Succeeded || external.Principal is null)
        {
            return Results.Redirect("/?authError=google#signin");
        }

        var subject = external.Principal.FindFirstValue("sub");
        var email = external.Principal.FindFirstValue("email");
        var name = external.Principal.FindFirstValue("name");
        var emailVerified = string.Equals(
            external.Principal.FindFirstValue("email_verified"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(subject)
            || string.IsNullOrWhiteSpace(email)
            || !AccessValidation.IsValidEmail(email)
            || !emailVerified)
        {
            await context.SignOutAsync("External");
            return Results.Redirect("/?authError=google-unverified#signin");
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var emailDomain = email.Split('@')[1];
        var hostedDomain = external.Principal.FindFirstValue("hd");
        var googleOwnsAddress = emailDomain.Equals("gmail.com", StringComparison.OrdinalIgnoreCase)
            || emailDomain.Equals("googlemail.com", StringComparison.OrdinalIgnoreCase)
            || (hostedDomain is not null
                && hostedDomain.Equals(emailDomain, StringComparison.OrdinalIgnoreCase));

        User user;
        try
        {
            user = await store.GetOrCreateGoogleUserAsync(
                subject,
                normalizedEmail,
                string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name,
                googleOwnsAddress,
                cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.Message == "external_account_conflict")
        {
            await context.SignOutAsync("External");
            return Results.Redirect("/?authError=google-link#signin");
        }

        var session = await store.CreateSessionAsync(user.Id, cancellationToken);
        await context.SignOutAsync("External");
        await SignIn(context, user, session.Id);
        var hasWorkspace = (await store.ListWorkspacesAsync(user.Id, cancellationToken)).Count > 0;
        return Results.Redirect(hasWorkspace ? "/#onboarding" : "/#workspace");
    })
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
app.MapGet("/ready", async (CancellationToken cancellationToken) =>
{
    try
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return Results.Ok(new { status = "ready" });
    }
    catch (NpgsqlException)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});
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
