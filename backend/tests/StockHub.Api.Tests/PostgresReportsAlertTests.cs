using Microsoft.AspNetCore.DataProtection;
using Npgsql;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;
using StockHub.Api.Infrastructure;

namespace StockHub.Api.Tests;

public sealed class PostgresReportsAlertTests
{
    [Fact]
    public async Task Alert_jobs_deduplicate_and_failed_email_is_scheduled_without_false_success()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKHUB_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await PostgresDatabaseInitializer.ApplyAsync(connectionString, CancellationToken.None);
        var access = new PostgresAccessStore(connectionString);
        var suffix = Guid.NewGuid().ToString("N");
        var user = await access.CreateUserAsync("Alert QA", $"ALERT-{suffix}@EXAMPLE.COM", "hash", CancellationToken.None);
        var workspace = await access.CreateWorkspaceAsync(user.Id,
            new CreateWorkspaceRequest("Alert QA", "IT", "EUR", null), $"alert-{suffix}", CancellationToken.None);
        var store = new PostgresReportsStore(connectionString, new EphemeralDataProtectionProvider());
        var dedupeKey = $"lowStock:product-{Guid.NewGuid():N}:incident:email:{user.Id:N}";

        await store.EnqueueAsync(workspace.Workspace.Id, user.Id, "lowStock", "product", "email", dedupeKey,
            "Low stock", "Test alert", DateTimeOffset.UtcNow, CancellationToken.None);
        await store.EnqueueAsync(workspace.Workspace.Id, user.Id, "lowStock", "product", "email", dedupeKey,
            "Low stock", "Test alert", DateTimeOffset.UtcNow, CancellationToken.None);

        var outcome = await store.DispatchNextAsync(new FailingAlertEmailSender(), new HttpClient(), CancellationToken.None);
        Assert.Equal(NotificationDispatchOutcome.Failed, outcome);

        var status = Assert.Single(await store.GetDeliveryStatusAsync(workspace.Workspace.Id, CancellationToken.None));
        Assert.Equal("email", status.Channel);
        Assert.Equal(1, status.Pending);
        Assert.Equal(0, status.Delivered);
        Assert.Equal(0, status.Failed);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*),min(status),min(attempts),min(next_attempt_at),min(last_error) FROM alert_notification_jobs WHERE workspace_id=@workspace AND dedupe_key=@dedupe", connection);
        command.Parameters.AddWithValue("workspace", workspace.Workspace.Id);
        command.Parameters.AddWithValue("dedupe", dedupeKey);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal("pending", reader.GetString(1));
        Assert.Equal(1, reader.GetInt32(2));
        Assert.True(reader.GetFieldValue<DateTimeOffset>(3) > DateTimeOffset.UtcNow);
        Assert.DoesNotContain("provider secret", reader.GetString(4), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FailingAlertEmailSender : IRecoveryEmailSender
    {
        public bool IsConfigured => true;
        public Task SendPasswordResetAsync(string recipient, Uri resetLink, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendInvitationAsync(string recipient, Uri invitationLink, string workspaceName, WorkspaceRole role, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendBusinessAlertAsync(string recipient, string title, string body, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("provider secret: test failure");
    }
}
