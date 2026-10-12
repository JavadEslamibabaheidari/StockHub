using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;

namespace StockHub.Api.Tests;

public sealed class AccessEndpointTests
{
    [Fact]
    public async Task Signup_api_creates_personal_account_session_and_handles_duplicate_retry_safely()
    {
        using var factory = new AccessApiFactory();
        using var client = factory.CreateAccessClient();

        var first = await client.PostAsJsonAsync("/api/auth/sign-up", new
        {
            fullName = "Ada Lovelace",
            email = "ada.personal@gmail.com",
            password = "correct-horse-123"
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal("/api/auth/session", first.Headers.Location?.ToString());

        var session = await client.GetFromJsonAsync<SessionResponse>("/api/auth/session");
        Assert.NotNull(session);
        Assert.Equal("ADA.PERSONAL@GMAIL.COM", session.Email);
        Assert.Equal("Ada Lovelace", session.FullName);

        var duplicate = await client.PostAsJsonAsync("/api/auth/sign-up", new
        {
            fullName = "Ada Retry",
            email = "ADA.PERSONAL@gmail.com",
            password = "correct-horse-123"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("Try signing in or resetting your password", problem, StringComparison.Ordinal);

        var afterRetry = await client.GetFromJsonAsync<SessionResponse>("/api/auth/session");
        Assert.Equal(session.UserId, afterRetry?.UserId);
        Assert.Equal("Ada Lovelace", afterRetry?.FullName);
    }

    [Fact]
    public async Task Sign_out_revokes_the_current_session()
    {
        using var factory = new AccessApiFactory();
        using var client = factory.CreateAccessClient();
        await SignUpAsync(client, "Ada", "ada@example.test");

        var signedOut = await client.PostAsync("/api/auth/sign-out", content: null);

        Assert.Equal(HttpStatusCode.NoContent, signedOut.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Persistent_data_protection_keys_keep_a_database_session_cookie_valid_across_host_restarts()
    {
        var keysPath = Path.Combine(Path.GetTempPath(), $"stockhub-dpkeys-{Guid.NewGuid():N}");
        try
        {
            using var firstFactory = new AccessApiFactory(dataProtectionKeysPath: keysPath);
            using var firstClient = firstFactory.CreateAccessClient();
            var signup = await firstClient.PostAsJsonAsync("/api/auth/sign-up", new
            {
                fullName = "Ada Lovelace",
                email = "ada.restart@example.test",
                password = "correct-horse-123"
            });
            Assert.Equal(HttpStatusCode.Created, signup.StatusCode);
            var sessionCookie = Assert.Single(signup.Headers.GetValues("Set-Cookie"))
                .Split(';', 2)[0];

            using var restartedFactory = new AccessApiFactory(
                dataProtectionKeysPath: keysPath,
                store: firstFactory.Store);
            using var restartedClient = restartedFactory.CreateAccessClient();
            restartedClient.DefaultRequestHeaders.Add("Cookie", sessionCookie);

            var session = await restartedClient.GetAsync("/api/auth/session");

            Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        }
        finally
        {
            if (Directory.Exists(keysPath))
            {
                Directory.Delete(keysPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Password_reset_api_sends_a_single_use_link_and_revokes_existing_sessions()
    {
        using var factory = new AccessApiFactory();
        using var client = factory.CreateAccessClient();
        await SignUpAsync(client, "Ada", "ada.personal@gmail.com");

        var unknown = await client.PostAsJsonAsync("/api/auth/password-reset/request", new { email = "unknown@example.test" });
        var requested = await client.PostAsJsonAsync("/api/auth/password-reset/request", new { email = "ada.personal@gmail.com" });

        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, requested.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await requested.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(factory.EmailSender.PasswordResetToken));

        var reset = await client.PostAsJsonAsync("/api/auth/password-reset/confirm", new
        {
            token = factory.EmailSender.PasswordResetToken,
            newPassword = "new-correct-horse-456"
        });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);

        var replay = await client.PostAsJsonAsync("/api/auth/password-reset/confirm", new
        {
            token = factory.EmailSender.PasswordResetToken,
            newPassword = "another-correct-horse-789"
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, replay.StatusCode);

        using var newSession = factory.CreateAccessClient();
        var oldPassword = await newSession.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email = "ada.personal@gmail.com",
            password = "correct-horse-123"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);

        var newPassword = await newSession.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email = "ada.personal@gmail.com",
            password = "new-correct-horse-456"
        });
        Assert.Equal(HttpStatusCode.OK, newPassword.StatusCode);
    }

    [Fact]
    public async Task Password_reset_api_reports_unconfigured_delivery_without_looking_up_an_account()
    {
        using var factory = new AccessApiFactory();
        factory.EmailSender.Configured = false;
        using var client = factory.CreateAccessClient();

        var response = await client.PostAsJsonAsync("/api/auth/password-reset/request", new { email = "unknown@example.test" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("not configured", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task First_use_dashboard_uses_the_created_workspace_name()
    {
        using var factory = new AccessApiFactory();
        using var client = factory.CreateAccessClient();
        await SignUpAsync(client, "Ada", "ada@example.test");
        var workspaceId = await CreateWorkspaceAsync(client, "M14 Isolated Test Shop");

        var dashboard = await client.GetFromJsonAsync<DashboardSnapshotResponse>(
            $"/api/workspaces/{workspaceId}/dashboard");

        Assert.NotNull(dashboard);
        Assert.Equal("first-use", dashboard.State);
        Assert.Contains("M14 Isolated Test Shop", dashboard.Subtitle, StringComparison.Ordinal);
        Assert.DoesNotContain("Rossi Elettronica", dashboard.Subtitle, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Onboarding_api_round_trips_selection_and_enforces_workspace_membership()
    {
        using var factory = new AccessApiFactory();
        using var client = factory.CreateAccessClient();
        await SignUpAsync(client, "Owner", "owner@example.test");
        var workspaceId = await CreateWorkspaceAsync(client, "North shop");

        var initial = await client.GetFromJsonAsync<OnboardingTaskResponse[]>($"/api/workspaces/{workspaceId}/onboarding");
        Assert.NotNull(initial);
        Assert.All(initial, task => Assert.Equal("available", task.Status));

        var invalid = await client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/onboarding/actions", new { key = "unknown" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var selection = await client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/onboarding/actions", new { key = "connect-platform" });
        Assert.Equal(HttpStatusCode.Accepted, selection.StatusCode);
        var acceptedBody = await selection.Content.ReadFromJsonAsync<OnboardingSelectionResponse>();
        Assert.Equal("connect-platform", acceptedBody?.Selected);
        Assert.False(acceptedBody?.Completed);

        var selected = await client.GetFromJsonAsync<OnboardingTaskResponse[]>($"/api/workspaces/{workspaceId}/onboarding");
        Assert.NotNull(selected);
        Assert.Equal("selected", Assert.Single(selected, task => task.Key == "connect-platform").Status);
        Assert.All(selected, task => Assert.NotEqual("completed", task.Status));

        using var otherClient = factory.CreateAccessClient();
        await SignUpAsync(otherClient, "Other", "other@example.test");
        var otherWorkspace = await CreateWorkspaceAsync(otherClient, "Other shop");
        var unauthorized = await client.GetAsync($"/api/workspaces/{otherWorkspace}/onboarding");
        Assert.Equal(HttpStatusCode.Forbidden, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Invitation_api_sends_accept_link_checks_identity_and_consumes_it_once()
    {
        using var factory = new AccessApiFactory();
        using var owner = factory.CreateAccessClient();
        await SignUpAsync(owner, "Owner", "owner@example.test");
        var workspaceId = await CreateWorkspaceAsync(owner, "North shop");

        var invite = await owner.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/invitations",
            new { email = "invitee@example.test", role = "Viewer" });
        Assert.Equal(HttpStatusCode.Accepted, invite.StatusCode);
        var token = factory.EmailSender.InvitationToken;
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var wrongUser = factory.CreateAccessClient();
        await SignUpAsync(wrongUser, "Wrong user", "wrong@example.test");
        var unauthorizedInvite = await wrongUser.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/invitations",
            new { email = "another@example.test", role = "Viewer" });
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedInvite.StatusCode);
        var wrongAcceptance = await wrongUser.PostAsJsonAsync("/api/invitations/accept", new { token });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongAcceptance.StatusCode);

        using var invitedUser = factory.CreateAccessClient();
        await SignUpAsync(invitedUser, "Invitee", "invitee@example.test");
        var accepted = await invitedUser.PostAsJsonAsync("/api/invitations/accept", new { token });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var session = await invitedUser.GetFromJsonAsync<SessionResponse>("/api/auth/session");
        Assert.Equal(workspaceId, session?.ActiveWorkspaceId);

        var replay = await invitedUser.PostAsJsonAsync("/api/invitations/accept", new { token });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, replay.StatusCode);
    }

    [Fact]
    public async Task Invitation_delivery_failure_invalidates_the_pending_link()
    {
        using var factory = new AccessApiFactory();
        factory.EmailSender.FailInvitations = true;
        using var owner = factory.CreateAccessClient();
        await SignUpAsync(owner, "Owner", "owner@example.test");
        var workspaceId = await CreateWorkspaceAsync(owner, "North shop");

        var invite = await owner.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/invitations",
            new { email = "invitee@example.test", role = "Viewer" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, invite.StatusCode);

        var team = await owner.GetFromJsonAsync<TeamResponse>($"/api/workspaces/{workspaceId}/team");
        Assert.Empty(team!.Invitations);
    }

    [Fact]
    public async Task Google_start_reports_missing_configuration_and_configured_flow_accepts_only_verified_test_identity()
    {
        using (var unavailableFactory = new AccessApiFactory(googleConfigured: false))
        using (var unavailableClient = unavailableFactory.CreateAccessClient())
        {
            var availability = await unavailableClient.GetFromJsonAsync<GoogleAvailabilityResponse>("/api/auth/google/availability");
            Assert.False(availability?.Available);
            var start = await unavailableClient.GetAsync("/api/auth/google/start");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, start.StatusCode);
            Assert.Contains("Google sign-in is not configured", await start.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        using var factory = new AccessApiFactory();
        using var client = factory.CreateAccessClient();
        var challenge = await client.GetAsync("/api/auth/google/start");
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        Assert.Contains("accounts.google.com", challenge.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        var invalidCallback = await client.GetAsync("/api/auth/google/callback?code=untrusted&state=invalid");
        Assert.Equal(HttpStatusCode.Redirect, invalidCallback.StatusCode);
        Assert.Equal("/?authError=google#signin", invalidCallback.Headers.Location?.ToString());

        // This test-only middleware represents claims returned by a provider after its OIDC middleware has validated the callback.
        var testProvider = await client.GetAsync("/__test/google-identity?subject=google-1&email=google-user%40gmail.com&name=Google%20User&verified=true");
        Assert.Equal(HttpStatusCode.NoContent, testProvider.StatusCode);
        var complete = await client.GetAsync("/api/auth/google/complete");
        Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
        Assert.Equal("/#workspace", complete.Headers.Location?.ToString());

        var session = await client.GetFromJsonAsync<SessionResponse>("/api/auth/session");
        Assert.Equal("GOOGLE-USER@GMAIL.COM", session?.Email);

        using var unverifiedClient = factory.CreateAccessClient();
        var unverifiedProvider = await unverifiedClient.GetAsync("/__test/google-identity?subject=google-2&email=unverified%40gmail.com&name=Unverified&verified=false");
        Assert.Equal(HttpStatusCode.NoContent, unverifiedProvider.StatusCode);
        var unverifiedComplete = await unverifiedClient.GetAsync("/api/auth/google/complete");
        Assert.Equal("/?authError=google-unverified#signin", unverifiedComplete.Headers.Location?.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await unverifiedClient.GetAsync("/api/auth/session")).StatusCode);
    }

    private static async Task SignUpAsync(HttpClient client, string name, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/sign-up", new { fullName = name, email, password = "correct-horse-123" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/workspaces")
        {
            Content = JsonContent.Create(new { businessName = name, country = "IT", currency = "EUR", vatNumber = (string?)null })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var workspace = await response.Content.ReadFromJsonAsync<WorkspaceResponse>();
        Assert.NotNull(workspace);
        return workspace.Id;
    }

    private sealed record OnboardingTaskResponse(string Key, string Status);
    private sealed record OnboardingSelectionResponse(string Selected, bool Completed);
    private sealed record GoogleAvailabilityResponse(bool Available);
    private sealed record WorkspaceResponse(Guid Id);
    private sealed record SessionResponse(Guid UserId, string FullName, string Email, Guid? ActiveWorkspaceId, IReadOnlyList<object> Workspaces);
    private sealed record TeamResponse(IReadOnlyList<object> Members, IReadOnlyList<object> Invitations, bool CanManage);
}

internal sealed class AccessApiFactory(
    bool googleConfigured = true,
    string? dataProtectionKeysPath = null,
    InMemoryAccessStore? store = null) : WebApplicationFactory<Program>
{
    public InMemoryAccessStore Store { get; } = store ?? new InMemoryAccessStore();
    public CapturingRecoveryEmailSender EmailSender { get; } = new();

    public HttpClient CreateAccessClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:Google:ClientId", googleConfigured ? "test-client-id" : string.Empty);
        builder.UseSetting("Authentication:Google:ClientSecret", googleConfigured ? "test-client-secret" : string.Empty);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Database:ApplyMigrations"] = "false",
                ["Application:PublicBaseUrl"] = "http://localhost",
                ["Authentication:DataProtectionKeysPath"] = dataProtectionKeysPath,
                ["Authentication:Google:ClientId"] = googleConfigured ? "test-client-id" : string.Empty,
                ["Authentication:Google:ClientSecret"] = googleConfigured ? "test-client-secret" : string.Empty
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccessStore>();
            services.AddSingleton<IAccessStore>(Store);
            services.RemoveAll<IProductStore>();
            services.AddSingleton<IProductStore, EmptyProductStore>();
            services.RemoveAll<IDashboardStore>();
            services.AddSingleton<IDashboardStore, InMemoryDashboardStore>();
            services.RemoveAll<IOrderStore>();
            services.AddSingleton<IOrderStore, EmptyOrderStore>();
            services.RemoveAll<IRecoveryEmailSender>();
            services.AddSingleton<IRecoveryEmailSender>(EmailSender);
            services.AddTransient<IStartupFilter, ExternalIdentityTestStartupFilter>();
        });
    }
}

internal sealed class EmptyProductStore : IProductStore
{
    public Task<IReadOnlyList<Product>> ListAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>([]);

    public Task<IReadOnlyList<Product>> UpsertAsync(Guid workspaceId, IReadOnlyList<ProductRequest> products, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>([]);

    public Task<Product?> UpdateOnHandAsync(Guid workspaceId, Guid productId, int onHand, CancellationToken cancellationToken) =>
        Task.FromResult<Product?>(null);
}

internal sealed class EmptyOrderStore : IOrderStore
{
    public Task<DashboardImportResponse> ImportAsync(Guid workspaceId, DashboardImportRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new DashboardImportResponse(0, 0));

    public Task<OrderListResponse> ListAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        Task.FromResult(new OrderListResponse(0, "No orders yet", []));

    public Task<OrderDetailResponse?> GetAsync(Guid workspaceId, Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult<OrderDetailResponse?>(null);

    public Task<OrderDetailResponse?> ApplyActionAsync(Guid workspaceId, Guid orderId, OrderActionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<OrderDetailResponse?>(null);

    public Task<IReadOnlyDictionary<Guid, int>> ReservedByProductAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

    public Task<IReadOnlyList<ReportOrderRecord>> LoadReportOrdersAsync(Guid workspaceId, DateTimeOffset start, DateTimeOffset endExclusive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportOrderRecord>>([]);

    public Task<IReadOnlyList<ReportProductSalesRecord>> LoadReportProductSalesAsync(Guid workspaceId, DateTimeOffset start, DateTimeOffset endExclusive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportProductSalesRecord>>([]);
}

internal sealed class CapturingRecoveryEmailSender : IRecoveryEmailSender
{
    public bool Configured { get; set; } = true;
    public bool IsConfigured => Configured;
    public bool FailInvitations { get; set; }
    public string? InvitationToken { get; private set; }
    public string? PasswordResetToken { get; private set; }

    public Task SendPasswordResetAsync(string recipient, Uri resetLink, CancellationToken cancellationToken)
    {
        var marker = "#reset?token=";
        PasswordResetToken = resetLink.Fragment.StartsWith(marker, StringComparison.Ordinal)
            ? Uri.UnescapeDataString(resetLink.Fragment[marker.Length..])
            : null;
        return Task.CompletedTask;
    }

    public Task SendInvitationAsync(string recipient, Uri invitationLink, string workspaceName, WorkspaceRole role, CancellationToken cancellationToken)
    {
        if (FailInvitations)
        {
            throw new InvalidOperationException("Simulated delivery failure.");
        }

        const string marker = "#accept?token=";
        var fragment = invitationLink.Fragment;
        InvitationToken = fragment.StartsWith(marker, StringComparison.Ordinal)
            ? Uri.UnescapeDataString(fragment[marker.Length..])
            : null;
        return Task.CompletedTask;
    }
}

internal sealed class ExternalIdentityTestStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Path != "/__test/google-identity")
            {
                await nextMiddleware();
                return;
            }

            var query = context.Request.Query;
            var claims = new List<Claim>
            {
                new("sub", query["subject"].ToString()),
                new("email", query["email"].ToString()),
                new("name", query["name"].ToString()),
                new("email_verified", query["verified"].ToString())
            };
            if (!string.IsNullOrWhiteSpace(query["hostedDomain"].ToString()))
            {
                claims.Add(new Claim("hd", query["hostedDomain"].ToString()));
            }

            await context.SignInAsync("External", new ClaimsPrincipal(new ClaimsIdentity(claims, "DeterministicGoogleTestAdapter")));
            context.Response.StatusCode = StatusCodes.Status204NoContent;
        });

        next(app);
    };
}
