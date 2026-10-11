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
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;

namespace StockHub.Api.Tests;

public sealed class AccessEndpointTests
{
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

internal sealed class AccessApiFactory(bool googleConfigured = true) : WebApplicationFactory<Program>
{
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
                ["Authentication:Google:ClientId"] = googleConfigured ? "test-client-id" : string.Empty,
                ["Authentication:Google:ClientSecret"] = googleConfigured ? "test-client-secret" : string.Empty
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccessStore>();
            services.AddSingleton<IAccessStore, InMemoryAccessStore>();
            services.RemoveAll<IRecoveryEmailSender>();
            services.AddSingleton<IRecoveryEmailSender>(EmailSender);
            services.AddTransient<IStartupFilter, ExternalIdentityTestStartupFilter>();
        });
    }
}

internal sealed class CapturingRecoveryEmailSender : IRecoveryEmailSender
{
    public bool IsConfigured => true;
    public bool FailInvitations { get; set; }
    public string? InvitationToken { get; private set; }

    public Task SendPasswordResetAsync(string recipient, Uri resetLink, CancellationToken cancellationToken) => Task.CompletedTask;

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
