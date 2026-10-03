using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.AddSingleton<IProductStore>(_ => new PostgresProductStore(connectionString));
builder.Services.AddSingleton<IDashboardStore>(_ => new PostgresDashboardStore(connectionString));
builder.Services.AddSingleton<IOrderStore>(_ => new PostgresOrderStore(connectionString));
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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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

app.UseForwardedHeaders();
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

app.MapGet(
    "/api/workspaces/{workspaceId:guid}/products",
    async (
        Guid workspaceId,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IProductStore productStore,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var isMember = (await accessStore.ListWorkspacesAsync(userId, cancellationToken))
            .Any(workspace => workspace.Workspace.Id == workspaceId);

        if (!isMember)
        {
            return Results.Forbid();
        }

        var products = await productStore.ListAsync(workspaceId, cancellationToken);
        return Results.Ok(products.Select(ToResponse));
    })
    .RequireAuthorization()
    .WithTags("Products");

app.MapPost(
    "/api/workspaces/{workspaceId:guid}/products/import",
    async (
        Guid workspaceId,
        ProductRequest[] request,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IProductStore productStore,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var membership = (await accessStore.ListWorkspacesAsync(userId, cancellationToken))
            .FirstOrDefault(workspace => workspace.Workspace.Id == workspaceId);

        if (membership.Workspace is null)
        {
            return Results.Forbid();
        }

        var error = ValidateProducts(request);
        if (error is not null)
        {
            return Results.UnprocessableEntity(error);
        }

        var products = await productStore.UpsertAsync(workspaceId, request, cancellationToken);
        await accessStore.RecordOnboardingActionAsync(userId, workspaceId, "import-products", cancellationToken);

        return Results.Ok(products.Select(ToResponse));
    })
    .RequireAuthorization()
    .WithTags("Products");

app.MapGet(
    "/api/workspaces/{workspaceId:guid}/inventory/products",
    async (
        Guid workspaceId,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IProductStore productStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        var membership = await FindWorkspaceMembership(principal, accessStore, workspaceId, cancellationToken);
        if (membership is null)
        {
            return Results.Forbid();
        }

        var products = await productStore.ListAsync(workspaceId, cancellationToken);
        var reserved = await orderStore.ReservedByProductAsync(workspaceId, cancellationToken);
        return Results.Ok(BuildInventoryList(products, reserved, membership.Value.Role));
    })
    .RequireAuthorization()
    .WithTags("Inventory");

app.MapGet(
    "/api/workspaces/{workspaceId:guid}/inventory/products/{productId:guid}",
    async (
        Guid workspaceId,
        Guid productId,
        bool? allFailing,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IProductStore productStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        var membership = await FindWorkspaceMembership(principal, accessStore, workspaceId, cancellationToken);
        if (membership is null)
        {
            return Results.Forbid();
        }

        var products = await productStore.ListAsync(workspaceId, cancellationToken);
        var reserved = await orderStore.ReservedByProductAsync(workspaceId, cancellationToken);
        var summary = BuildInventoryRows(products, reserved).FirstOrDefault(product => product.Id == productId);
        if (summary is null)
        {
            return Results.NotFound(new Problem(
                "Product not found",
                "This product is no longer available in the workspace.",
                "product_not_found"));
        }

        return Results.Ok(BuildInventoryDetail(summary, membership.Value.Role, allFailing == true));
    })
    .RequireAuthorization()
    .WithTags("Inventory");

app.MapPatch(
    "/api/workspaces/{workspaceId:guid}/inventory/products/{productId:guid}/on-hand",
    async (
        Guid workspaceId,
        Guid productId,
        AdjustOnHandRequest request,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IProductStore productStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        var membership = await FindWorkspaceMembership(principal, accessStore, workspaceId, cancellationToken);
        if (membership is null)
        {
            return Results.Forbid();
        }

        if (!InventoryCapabilitiesFor(membership.Value.Role).CanAdjustOnHand || request.OnHand < 0)
        {
            return Results.UnprocessableEntity(new Problem(
                "Invalid on-hand count",
                "On hand must be zero or greater.",
                "invalid_on_hand"));
        }

        var updated = await productStore.UpdateOnHandAsync(workspaceId, productId, request.OnHand, cancellationToken);
        if (updated is null)
        {
            return Results.NotFound(new Problem(
                "Product not found",
                "This product is no longer available in the workspace.",
                "product_not_found"));
        }

        var reserved = await orderStore.ReservedByProductAsync(workspaceId, cancellationToken);
        return Results.Ok(new InventoryActionResponse("adjusted", BuildInventoryRow(updated, reserved.GetValueOrDefault(updated.Id))));
    })
    .RequireAuthorization()
    .WithTags("Inventory");

app.MapPost(
    "/api/workspaces/{workspaceId:guid}/inventory/products/{productId:guid}/retry-sync",
    async (
        Guid workspaceId,
        Guid productId,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IProductStore productStore,
        CancellationToken cancellationToken) =>
    {
        var membership = await FindWorkspaceMembership(principal, accessStore, workspaceId, cancellationToken);
        if (membership is null)
        {
            return Results.Forbid();
        }

        var product = (await productStore.ListAsync(workspaceId, cancellationToken))
            .FirstOrDefault(item => item.Id == productId);
        if (product is null)
        {
            return Results.NotFound(new Problem(
                "Product not found",
                "This product is no longer available in the workspace.",
                "product_not_found"));
        }

        return Results.Accepted(
            $"/api/workspaces/{workspaceId}/inventory/products/{productId}",
            new InventoryActionResponse("retry_queued", BuildInventoryRow(product)));
    })
    .RequireAuthorization()
    .WithTags("Inventory");


app.MapGet(
    "/api/workspaces/{workspaceId:guid}/dashboard",
    async (
        Guid workspaceId,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IProductStore productStore,
        IDashboardStore dashboardStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        if (!await IsWorkspaceMember(accessStore, userId, workspaceId, cancellationToken))
        {
            return Results.Forbid();
        }

        var products = await productStore.ListAsync(workspaceId, cancellationToken);
        var state = await dashboardStore.GetAsync(workspaceId, cancellationToken);
        var orders = await orderStore.ListAsync(workspaceId, cancellationToken);
        return Results.Ok(BuildDashboardSnapshot(state, products, orders));
    })
    .RequireAuthorization()
    .WithTags("Dashboard");

app.MapPost(
    "/api/workspaces/{workspaceId:guid}/dashboard/actions",
    async (
        Guid workspaceId,
        DashboardActionRequest request,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IProductStore productStore,
        IDashboardStore dashboardStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        if (!await IsWorkspaceMember(accessStore, userId, workspaceId, cancellationToken))
        {
            return Results.Forbid();
        }

        var state = await dashboardStore.GetAsync(workspaceId, cancellationToken);
        string message;
        switch (request.Action.Trim().ToLowerInvariant())
        {
            case "start-first-sync":
                state = state with { Mode = "live", SyncPlatform = NormalizeDashboardPlatform(request.Platform), DetailPanel = $"{NormalizeDashboardPlatform(request.Platform)} setup opened. Real marketplace authorization is owned by Milestone 6 Platforms." };
                message = "Platform setup opened.";
                break;
            case "finish-first-sync":
                state = state with { Mode = "live", DetailPanel = "Dashboard is using persisted backend product and order data." };
                message = "Dashboard backend data loaded.";
                break;
            case "retry-sync":
                state = state with { EuronicsRetried = true, DetailPanel = "Retry sent for the Euronics sync alert. Live connector repair belongs to Milestone 6 Platforms." };
                message = "Retry queued for Euronics.";
                break;
            case "resolve-mismatch":
                state = state with { MismatchResolved = true, DetailPanel = "Stock mismatch resolved by using the StockHub count for the dashboard projection." };
                message = "Mismatch resolved with the StockHub count.";
                break;
            case "show-more-low-stock":
                state = state with { ShowMoreLowStock = true, DetailPanel = "Showing all low-stock dashboard alerts." };
                message = "More low-stock products are now visible.";
                break;
            case "review-mismatch":
                state = state with { DetailPanel = "Stock mismatch review opened. Full inventory reconciliation belongs to Milestone 3 Inventory." };
                message = "Mismatch review opened.";
                break;
            case "restock":
                state = state with { RestockListed = true, DetailPanel = "Restock request added to the dashboard reorder handoff. Purchase workflows belong to Milestone 3 Inventory." };
                message = "Product added to the reorder handoff.";
                break;
            case "open-reservations":
                state = state with { DetailPanel = "Reservations preview opened. The full Reservations workspace belongs to Milestone 5 Reservations." };
                message = "Reservations preview opened.";
                break;
            case "open-sync-details":
                state = state with { DetailPanel = "Sync details opened. Connector authorization and repair belong to Milestone 6 Platforms." };
                message = "Sync details opened.";
                break;
            case "adjust-on-hand":
                if (request.OnHand is null || request.OnHand < 0 || !Guid.TryParse(request.TargetId, out var productId))
                {
                    return Results.UnprocessableEntity(new Problem(
                        "Validation failed",
                        "Choose a product and enter a non-negative on-hand count.",
                        "dashboard_adjust_invalid"));
                }

                var updated = await productStore.UpdateOnHandAsync(workspaceId, productId, request.OnHand.Value, cancellationToken);
                if (updated is null)
                {
                    return Results.NotFound(new Problem(
                        "Product not found",
                        "This product is no longer available in the workspace.",
                        "product_not_found"));
                }

                state = state with { DetailPanel = $"{updated.Name} on-hand count updated to {updated.OnHand}." };
                message = "On-hand count updated.";
                break;
            default:
                return Results.UnprocessableEntity(new Problem(
                    "Unknown dashboard action",
                    "This dashboard control is not recognized.",
                    "dashboard_action_unknown"));
        }

        state = await dashboardStore.SaveAsync(state, cancellationToken);
        var products = await productStore.ListAsync(workspaceId, cancellationToken);
        var orders = await orderStore.ListAsync(workspaceId, cancellationToken);
        return Results.Ok(new DashboardActionResultResponse(message, message, BuildDashboardSnapshot(state, products, orders)));
    })
    .RequireAuthorization()
    .WithTags("Dashboard");

app.MapPost(
    "/api/workspaces/{workspaceId:guid}/dashboard/import",
    async (
        Guid workspaceId,
        DashboardImportRequest request,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !await IsWorkspaceMember(accessStore, userId, workspaceId, cancellationToken))
        {
            return Results.Forbid();
        }

        var error = ValidateDashboardImport(request);
        if (error is not null)
        {
            return Results.UnprocessableEntity(error);
        }

        try
        {
            return Results.Ok(await orderStore.ImportAsync(workspaceId, request, cancellationToken));
        }
        catch (InvalidOperationException exception) when (exception.Message == "order_status_invalid")
        {
            return Results.UnprocessableEntity(new Problem(
                "Validation failed",
                "Each order status must be AwaitingPayment, PaidToPick, PickedToShip, Shipped, Delivered, ReturnRequested, or Cancelled.",
                "order_status_invalid"));
        }
    })
    .RequireAuthorization()
    .WithTags("Dashboard");

app.MapGet(
    "/api/workspaces/{workspaceId:guid}/orders",
    async (
        Guid workspaceId,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        var membership = await FindWorkspaceMembership(principal, accessStore, workspaceId, cancellationToken);
        if (membership is null)
        {
            return Results.Forbid();
        }

        return Results.Ok(await orderStore.ListAsync(workspaceId, cancellationToken));
    })
    .RequireAuthorization()
    .WithTags("Orders");

app.MapGet(
    "/api/workspaces/{workspaceId:guid}/orders/{orderId:guid}",
    async (
        Guid workspaceId,
        Guid orderId,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        var membership = await FindWorkspaceMembership(principal, accessStore, workspaceId, cancellationToken);
        if (membership is null)
        {
            return Results.Forbid();
        }

        var order = await orderStore.GetAsync(workspaceId, orderId, cancellationToken);
        return order is null
            ? Results.NotFound(new Problem("Order not found", "This order is no longer available in the workspace.", "order_not_found"))
            : Results.Ok(order);
    })
    .RequireAuthorization()
    .WithTags("Orders");

app.MapPost(
    "/api/workspaces/{workspaceId:guid}/orders/{orderId:guid}/actions",
    async (
        Guid workspaceId,
        Guid orderId,
        OrderActionRequest request,
        ClaimsPrincipal principal,
        IAccessStore accessStore,
        IOrderStore orderStore,
        CancellationToken cancellationToken) =>
    {
        var membership = await FindWorkspaceMembership(principal, accessStore, workspaceId, cancellationToken);
        if (membership is null)
        {
            return Results.Forbid();
        }

        try
        {
            var order = await orderStore.ApplyActionAsync(workspaceId, orderId, request, cancellationToken);
            return order is null
                ? Results.NotFound(new Problem("Order not found", "This order is no longer available in the workspace.", "order_not_found"))
                : Results.Ok(new OrderActionResponse("updated", "Order updated.", order));
        }
        catch (InvalidOperationException exception) when (exception.Message == "order_action_unknown")
        {
            return Results.UnprocessableEntity(new Problem(
                "Unknown order action",
                "This order action is not recognized.",
                "order_action_unknown"));
        }
    })
    .RequireAuthorization()
    .WithTags("Orders");

app.MapPost(
    "/api/workspaces/{workspaceId:guid}/invitations",
    async (
        Guid workspaceId,
        InvitationRequest request,
        ClaimsPrincipal principal,
        IAccessStore store,
        IRecoveryEmailSender emailSender,
        IConfiguration configuration,
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        if (!AccessValidation.IsValidEmail(request.Email) || request.Role is WorkspaceRole.Owner)
        {
            return Results.UnprocessableEntity(new Problem(
                "Validation failed",
                "Enter a valid teammate email and choose a non-owner role.",
                "invitation_invalid"));
        }

        if (!emailSender.IsConfigured)
        {
            return Results.Problem(
                "Invitation email is not configured for this environment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
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
            request.Email.Trim(),
            request.Role,
            DateTimeOffset.UtcNow.AddHours(72),
            cancellationToken);

        var baseUri = new Uri(configuration["Application:PublicBaseUrl"]!, UriKind.Absolute);
        var invitationLink = new Uri(baseUri, $"/#accept?token={Uri.EscapeDataString(rawToken)}");
        try
        {
            await emailSender.SendInvitationAsync(
                request.Email.Trim(),
                invitationLink,
                membership.Workspace.BusinessName,
                request.Role,
                cancellationToken);
        }
        catch (Exception exception) when (exception is System.Net.Mail.SmtpException or InvalidOperationException)
        {
            logger.LogError(exception, "Invitation email delivery failed");
            return Results.Problem(
                "Invitation email is temporarily unavailable. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Accepted(
            $"/api/invitations/{rawToken}",
            new { status = "sent", expiresInHours = 72 });
    })
    .RequireAuthorization()
    .WithTags("Invitations");

app.MapGet(
    "/api/invitations/{token}",
    async (string token, IAccessStore store, CancellationToken cancellationToken) =>
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var invitation = await store.FindInvitationAsync(hash, cancellationToken);

        return invitation is null
            ? Results.NotFound()
            : Results.Ok(new InvitationResponse(invitation.Email, invitation.Role, "pending"));
    })
    .WithTags("Invitations");

app.MapPost(
    "/api/invitations/accept",
    async (
        AcceptInvitationRequest request,
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

        if (string.IsNullOrEmpty(request.Token) || request.Token.Length != 64 || !request.Token.All(Uri.IsHexDigit))
        {
            return Results.UnprocessableEntity(new Problem(
                "Invalid invitation",
                "This invitation link is invalid or has expired.",
                "invitation_invalid"));
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var normalizedEmail = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var joinedWorkspaceId = await store.AcceptInvitationAsync(hash, userId, normalizedEmail, cancellationToken);
        if (joinedWorkspaceId is null)
        {
            return Results.UnprocessableEntity(new Problem(
                "Invalid invitation",
                "This invitation is expired, already used, or belongs to another email address.",
                "invitation_invalid"));
        }

        await store.SetActiveWorkspaceAsync(userId, sessionId, joinedWorkspaceId.Value, cancellationToken);
        await SignIn(
            context,
            new User(
                userId,
                principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                normalizedEmail,
                string.Empty),
            sessionId,
            joinedWorkspaceId.Value);

        return Results.Ok(new { status = "accepted" });
    })
    .RequireAuthorization()
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

static ProductResponse ToResponse(Product product) => new(
    product.Id,
    product.Sku,
    product.Name,
    product.OnHand,
    product.BasePrice,
    product.Category);

static Problem? ValidateProducts(IReadOnlyList<ProductRequest> products)
{
    if (products.Count is 0 or > 500)
    {
        return new Problem("Validation failed", "Import between 1 and 500 products at a time.", "products_count_invalid");
    }

    var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var product in products)
    {
        if (string.IsNullOrWhiteSpace(product.Sku)
            || string.IsNullOrWhiteSpace(product.Name)
            || product.Sku.Trim().Length > 80
            || product.Name.Trim().Length > 200
            || product.OnHand < 0
            || product.BasePrice < 0)
        {
            return new Problem("Validation failed", "Each product needs a SKU, name, non-negative stock, and non-negative base price.", "product_invalid");
        }

        if (!skus.Add(product.Sku.Trim()))
        {
            return new Problem("Validation failed", "Each imported SKU must be unique.", "product_sku_duplicate");
        }
    }

    return null;
}

static Problem? ValidateDashboardImport(DashboardImportRequest request)
{
    var productError = ValidateProducts(request.Products);
    if (productError is not null)
    {
        return productError;
    }

    if (request.Orders.Count > 500)
    {
        return new Problem("Validation failed", "Import at most 500 orders at a time.", "orders_count_invalid");
    }

    var orderNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var skus = request.Products.Select(product => product.Sku.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var order in request.Orders)
    {
        if (string.IsNullOrWhiteSpace(order.OrderNumber)
            || string.IsNullOrWhiteSpace(order.Platform)
            || string.IsNullOrWhiteSpace(order.CustomerName)
            || string.IsNullOrWhiteSpace(order.ShipTo)
            || string.IsNullOrWhiteSpace(order.Carrier)
            || order.Items.Count is 0 or > 50
            || !orderNumbers.Add(order.OrderNumber.Trim()))
        {
            return new Problem("Validation failed", "Each order needs a unique number, platform, customer, shipping, carrier, and at least one item.", "order_invalid");
        }

        foreach (var item in order.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku)
                || string.IsNullOrWhiteSpace(item.ProductName)
                || item.Quantity <= 0
                || item.UnitPrice < 0
                || item.CostOfGoods < 0
                || !skus.Contains(item.Sku.Trim()))
            {
                return new Problem("Validation failed", "Each order item must reference an imported product SKU and include positive quantity and non-negative prices.", "order_item_invalid");
            }
        }
    }

    return null;
}




static string NormalizeDashboardPlatform(string? platform) =>
    platform?.Trim().ToLowerInvariant() switch
    {
        "unieuro" or "un" => "Unieuro",
        "euronics" or "eu" => "Euronics",
        "ebay" or "eb" => "eBay",
        _ => "Amazon"
    };

static async Task<bool> IsWorkspaceMember(
    IAccessStore accessStore,
    Guid userId,
    Guid workspaceId,
    CancellationToken cancellationToken) =>
    (await accessStore.ListWorkspacesAsync(userId, cancellationToken))
    .Any(workspace => workspace.Workspace.Id == workspaceId);

static async Task<(Workspace Workspace, WorkspaceRole Role)?> FindWorkspaceMembership(
    ClaimsPrincipal principal,
    IAccessStore accessStore,
    Guid workspaceId,
    CancellationToken cancellationToken)
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
    {
        return null;
    }

    foreach (var membership in await accessStore.ListWorkspacesAsync(userId, cancellationToken))
    {
        if (membership.Workspace.Id == workspaceId)
        {
            return membership;
        }
    }

    return null;
}

static InventoryListResponse BuildInventoryList(
    IReadOnlyList<Product> products,
    IReadOnlyDictionary<Guid, int> reservedByProduct,
    WorkspaceRole role)
{
    var rows = BuildInventoryRows(products, reservedByProduct);
    var total = rows.Count;

    return new InventoryListResponse(
        total,
        rows.Count == 0 ? "No products imported" : "Backend inventory · platform sync pending",
        rows,
        InventoryCapabilitiesFor(role));
}

static IReadOnlyList<InventoryProductSummary> BuildInventoryRows(
    IReadOnlyList<Product> products,
    IReadOnlyDictionary<Guid, int> reservedByProduct)
{
    return products.Select(product => BuildInventoryRow(product, reservedByProduct.GetValueOrDefault(product.Id))).ToArray();
}

static InventoryProductSummary BuildInventoryRow(Product product, int reserved = 0)
{
    var available = Math.Max(0, product.OnHand - reserved);
    var category = string.IsNullOrWhiteSpace(product.Category) ? "General" : product.Category;

    return new InventoryProductSummary(
        product.Id,
        product.Sku,
        product.Name,
        category,
        product.OnHand,
        reserved,
        available,
        product.BasePrice,
        InventoryStockStatus(available),
        InventoryPlatforms(available, product.OnHand == 0));
}

static InventoryProductDetail BuildInventoryDetail(
    InventoryProductSummary product,
    WorkspaceRole role,
    bool allFailing)
{
    return new InventoryProductDetail(
        product,
        SafetyBufferEnabled: false,
        LowStockAlertAt: 5,
        "Backend inventory loaded from persisted products",
        InventoryPricing(product.BasePrice, allFailing),
        InventoryAudit(product),
        InventoryCapabilitiesFor(role));
}

static InventoryCapabilities InventoryCapabilitiesFor(WorkspaceRole role) =>
    role == WorkspaceRole.WarehouseStaff
        ? new InventoryCapabilities(CanAdjustOnHand: true, CanChangePrice: false, CanManageListings: false)
        : new InventoryCapabilities(
            CanAdjustOnHand: role is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Manager,
            CanChangePrice: role is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Manager,
            CanManageListings: role is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Manager);

static IReadOnlyList<InventoryPlatformStatus> InventoryPlatforms(int available, bool outOfStock) =>
[
    new("Amazon", "Am", "Not connected", $"Backend available {available}", available, false),
    new("Unieuro", "Un", "Not connected", $"Backend available {available}", available, false),
    new("Euronics", "Eu", "Not connected", $"Backend available {available}", available, false),
    new("eBay", "eB", outOfStock ? "Not listed" : "Not connected", $"Backend available {available}", available, false)
];

static IReadOnlyList<InventoryPricingRow> InventoryPricing(decimal basePrice, bool failing) =>
[
    new("Amazon", "Marketplace fees", "+5.0%", Math.Round(basePrice * 1.05m, 2), Math.Round(basePrice * .084m, 2), Math.Round(basePrice * .101m, 2), failing ? "Not connected" : "Backend estimate"),
    new("Unieuro", "Base price", "—", basePrice, Math.Round(basePrice * .1m, 2), Math.Round(basePrice * .044m, 2), failing ? "Not connected" : "Backend estimate"),
    new("Euronics", "Promo", "-20.00", Math.Max(0, basePrice - 20m), Math.Round(basePrice * .088m, 2), Math.Round(basePrice * .036m, 2), failing ? "Not connected" : "Backend estimate"),
    new("eBay", "Base price", "—", basePrice, Math.Round(basePrice * .08m, 2), Math.Round(basePrice * .064m, 2), failing ? "Not connected" : "Backend estimate")
];

static IReadOnlyList<InventoryAuditEntry> InventoryAudit(InventoryProductSummary product) =>
[
    product.Reserved > 0
        ? new("Reserved", $"Reserved {product.Reserved} unit{(product.Reserved == 1 ? string.Empty : "s")}", $"Available {product.OnHand} → {product.Available}", "Current orders")
        : new("Available", "No active reservations", $"Available {product.Available}", "Current orders"),
    new("Adjusted", "On hand managed in StockHub", $"On hand {product.OnHand} · available {product.Available}", "Current inventory"),
    new("Pricing", "Base price stored in StockHub", $"€{product.BasePrice:0.00}", "Current product")
];

static string InventoryStockStatus(int available) =>
    available == 0 ? "Out of stock" : available <= 5 ? "Low stock" : "In stock";

static DashboardSnapshotResponse BuildDashboardSnapshot(
    DashboardWorkspaceState state,
    IReadOnlyList<Product> products,
    OrderListResponse orders)
{
    var mode = products.Count == 0 ? "first-use" : "live";

    var lowStockProducts = products.Where(product => product.OnHand <= 5).ToArray();
    var lowStockCount = lowStockProducts.Length;

    if (mode == "first-use")
    {
        return new DashboardSnapshotResponse(
            mode,
            "Dashboard",
            DateTimeOffset.UtcNow.ToString("dddd, dd MMMM") + " · Rossi Elettronica",
            "No platforms connected",
            "neutral",
            [
                new("revenue", "Revenue today", "€0.00", "Appears after your first order", "neutral"),
                new("orders", "Orders today", "0", "Appears after your first order", "neutral"),
                new("reservations", "Active reservations", "0", "Held 10 min per order", "neutral"),
                new("products", "Products", "—", "Import to get started", "neutral"),
                new("sync", "Sync status", "—", "No platform connected", "neutral")
            ],
            [],
            [],
            [new("empty", "info", "ok", "Nothing needs your attention.", "Connect a platform when you are ready.", [])],
            [
                new("import-products", "Import products", "Upload a CSV or add products one by one", products.Count > 0 ? "done" : "available"),
                new("connect-platform", "Connect a platform", "Choose where you sell. You can add more later.", "available"),
                new("invite-team", "Invite your team", "Give your staff their own login and role", "available")
            ],
            null,
            BuildSearchIndex(products, []),
            ["Nothing needs your attention."]);
    }

    var reservations = BuildReservations(orders);
    var attention = BuildAttention(products);
    var revenueToday = orders.Orders.Sum(order => order.Total);
    var averageOrder = orders.Orders.Count == 0 ? 0m : revenueToday / orders.Orders.Count;
    return new DashboardSnapshotResponse(
        mode,
        "Dashboard",
        DateTimeOffset.UtcNow.ToString("dddd, dd MMMM") + " · backend data",
        "Backend data loaded",
        "ok",
        [
            new("revenue", "Revenue today", $"€{revenueToday:0.00}", "Calculated from imported orders", revenueToday > 0 ? "ok" : "neutral"),
            new("orders", "Orders today", orders.TotalToday.ToString(), $"Avg. order €{averageOrder:0.00}", "neutral"),
            new("reservations", "Active reservations", reservations.Count.ToString(), reservations.Count == 0 ? "No active holds" : "Held for unpaid orders", reservations.Count == 0 ? "neutral" : "warning"),
            new("low", "Low-stock products", lowStockCount.ToString(), "5 or fewer available", "warning"),
            new("sync", "Data source", "Backend", "Platform sync pending", "neutral")
        ],
        reservations,
        BuildSalesByPlatform(orders),
        attention,
        [],
        null,
        BuildSearchIndex(products, reservations.Select(item => item.ProductName).Concat(attention.Select(item => item.Title))),
        attention.Select(item => item.Title).ToArray());
}

static IReadOnlyList<DashboardReservationResponse> BuildReservations(OrderListResponse orders) =>
    orders.Orders
        .Where(order => order.Status is "Awaiting payment" or "Paid - to pick")
        .Select(order =>
        {
            var elapsed = DateTimeOffset.UtcNow - order.PlacedAt.ToUniversalTime();
            var remaining = TimeSpan.FromMinutes(10) - elapsed;
            var percent = Math.Clamp((int)(elapsed.TotalMinutes / 10d * 100), 0, 100);
            var label = remaining <= TimeSpan.Zero
                ? "Expired"
                : $"{Math.Max(0, remaining.Minutes):00}:{Math.Max(0, remaining.Seconds):00} left";
            return new DashboardReservationResponse(
                order.Id.ToString(),
                order.Product,
                order.Platform,
                order.OrderNumber,
                $"{order.Quantity} unit{(order.Quantity == 1 ? string.Empty : "s")}",
                label,
                percent);
        })
        .ToArray();

static IReadOnlyList<DashboardPlatformSaleResponse> BuildSalesByPlatform(OrderListResponse orders)
{
    var total = orders.Orders.Sum(order => order.Total);
    if (total <= 0)
    {
        return [];
    }

    var tones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Amazon"] = "dark",
        ["Unieuro"] = "sage",
        ["Euronics"] = "neutral",
        ["eBay"] = "light"
    };

    return orders.Orders
        .GroupBy(order => order.Platform)
        .OrderByDescending(group => group.Sum(order => order.Total))
        .Select(group => new DashboardPlatformSaleResponse(
            group.Key,
            Math.Round(group.Sum(order => order.Total) / total * 100m, 1),
            tones.GetValueOrDefault(group.Key, "neutral")))
        .ToArray();
}

static IReadOnlyList<DashboardAttentionResponse> BuildAttention(IReadOnlyList<Product> products)
{
    var items = new List<DashboardAttentionResponse>();

    foreach (var product in products.Where(product => product.OnHand <= 5).Take(5))
    {
        items.Add(new(
            $"product:{product.Id}",
            "low-stock",
            "warning",
            $"{product.Name}: {product.OnHand} on hand",
            "Low-stock product visible on the dashboard.",
            [new("adjust-on-hand", "Adjust on hand", "link")]));
    }

    if (items.Count == 0)
    {
        items.Add(new(
            "stock-healthy",
            "info",
            "ok",
            "No product stock alerts",
            "Dashboard alerts are calculated from persisted product counts.",
            []));
    }

    return items;
}

static IReadOnlyList<DashboardSearchResultResponse> BuildSearchIndex(
    IReadOnlyList<Product> products,
    IEnumerable<string> extra)
{
    var result = products.Select(product => new DashboardSearchResultResponse(
        product.Id.ToString(),
        "product",
        $"{product.Name} · {product.Sku}",
        $"{product.OnHand} on hand · €{product.BasePrice:0.00}",
        "adjust-on-hand")).ToList();

    result.AddRange(extra.Distinct().Select((label, index) => new DashboardSearchResultResponse(
        $"dashboard-{index}",
        "dashboard",
        label,
        "Dashboard result",
        "open-detail")));

    return result;
}

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
