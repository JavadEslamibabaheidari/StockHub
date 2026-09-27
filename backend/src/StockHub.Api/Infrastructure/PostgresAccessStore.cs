using System.Security.Cryptography;
using System.Text;
using Npgsql;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class PostgresAccessStore(string connectionString) : IAccessStore
{
    public async Task<User?> FindUserAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, full_name, email::text, password_hash FROM users WHERE email = @email",
            connection);
        command.Parameters.AddWithValue("email", normalizedEmail);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadUser(reader) : null;
    }

    public async Task<User> CreateUserAsync(
        string fullName,
        string normalizedEmail,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        var user = new User(Guid.NewGuid(), fullName.Trim(), normalizedEmail, passwordHash);

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "INSERT INTO users(id, full_name, email, password_hash) VALUES (@id, @name, @email, @hash)",
                connection);
            command.Parameters.AddWithValue("id", user.Id);
            command.Parameters.AddWithValue("name", user.FullName);
            command.Parameters.AddWithValue("email", user.NormalizedEmail);
            command.Parameters.AddWithValue("hash", user.PasswordHash);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return user;
        }
        catch (PostgresException exception) when (exception.SqlState == "23505")
        {
            throw new InvalidOperationException("duplicate_user", exception);
        }
    }

    public async Task<Session> CreateSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = new Session(Guid.NewGuid(), userId, null, DateTimeOffset.UtcNow.AddHours(8));
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO sessions(id, user_id, expires_at) VALUES (@id, @user, @expires)",
            connection);
        command.Parameters.AddWithValue("id", session.Id);
        command.Parameters.AddWithValue("user", session.UserId);
        command.Parameters.AddWithValue("expires", session.ExpiresAt);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return session;
    }

    public async Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, user_id, active_workspace_id, expires_at FROM sessions WHERE id = @id AND expires_at > now()",
            connection);
        command.Parameters.AddWithValue("id", sessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Session(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.GetFieldValue<DateTimeOffset>(3));
    }

    public async Task ClearSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM sessions WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", sessionId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Workspace Workspace, WorkspaceRole Role)>> ListWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT w.id, w.business_name, w.country, w.currency, w.vat_number, w.slug, m.role
            FROM workspaces w
            JOIN memberships m ON m.workspace_id = w.id
            WHERE m.user_id = @user
            ORDER BY w.created_at, w.id
            """,
            connection);
        command.Parameters.AddWithValue("user", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<(Workspace, WorkspaceRole)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add((ReadWorkspace(reader), ParseRole(reader.GetString(6))));
        }

        return result;
    }

    public async Task<(Workspace Workspace, WorkspaceRole Role)> CreateWorkspaceAsync(
        Guid userId,
        CreateWorkspaceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var fingerprint = Fingerprint(request);
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await using var existingCommand = new NpgsqlCommand(
                    "SELECT workspace_id, request_fingerprint FROM workspace_idempotency WHERE user_id = @user AND idempotency_key = @key FOR UPDATE",
                    connection,
                    transaction);
                existingCommand.Parameters.AddWithValue("user", userId);
                existingCommand.Parameters.AddWithValue("key", idempotencyKey);

                await using var existingReader = await existingCommand.ExecuteReaderAsync(cancellationToken);
                if (await existingReader.ReadAsync(cancellationToken))
                {
                    var existingWorkspaceId = existingReader.GetGuid(0);
                    var existingFingerprint = existingReader.GetString(1);
                    if (!string.Equals(existingFingerprint, fingerprint, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("idempotency_conflict");
                    }

                    await existingReader.DisposeAsync();
                    var existing = await ReadWorkspaceWithRoleAsync(
                        connection,
                        transaction,
                        userId,
                        existingWorkspaceId,
                        cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return existing;
                }
            }

            var baseSlug = Slugify(request.BusinessName);
            await using (var lockCommand = new NpgsqlCommand(
                "SELECT pg_advisory_xact_lock(hashtext(@slug))",
                connection,
                transaction))
            {
                lockCommand.Parameters.AddWithValue("slug", baseSlug);
                await lockCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var slug = baseSlug;
            var suffix = 1;
            while (await WorkspaceSlugExistsAsync(connection, transaction, slug, cancellationToken))
            {
                slug = $"{baseSlug}-{suffix++}";
            }

            var workspace = new Workspace(
                Guid.NewGuid(),
                request.BusinessName.Trim(),
                request.Country.ToUpperInvariant(),
                request.Currency.ToUpperInvariant(),
                request.VatNumber?.Trim(),
                slug);

            await using (var workspaceCommand = new NpgsqlCommand(
                "INSERT INTO workspaces(id, business_name, country, currency, vat_number, slug) VALUES (@id, @name, @country, @currency, @vat, @slug)",
                connection,
                transaction))
            {
                workspaceCommand.Parameters.AddWithValue("id", workspace.Id);
                workspaceCommand.Parameters.AddWithValue("name", workspace.BusinessName);
                workspaceCommand.Parameters.AddWithValue("country", workspace.Country);
                workspaceCommand.Parameters.AddWithValue("currency", workspace.Currency);
                workspaceCommand.Parameters.AddWithValue("vat", (object?)workspace.VatNumber ?? DBNull.Value);
                workspaceCommand.Parameters.AddWithValue("slug", workspace.Slug);
                await workspaceCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var membershipCommand = new NpgsqlCommand(
                "INSERT INTO memberships(user_id, workspace_id, role) VALUES (@user, @workspace, 'Owner')",
                connection,
                transaction))
            {
                membershipCommand.Parameters.AddWithValue("user", userId);
                membershipCommand.Parameters.AddWithValue("workspace", workspace.Id);
                await membershipCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await using var idempotencyCommand = new NpgsqlCommand(
                    "INSERT INTO workspace_idempotency(user_id, idempotency_key, workspace_id, request_fingerprint) VALUES (@user, @key, @workspace, @fingerprint)",
                    connection,
                    transaction);
                idempotencyCommand.Parameters.AddWithValue("user", userId);
                idempotencyCommand.Parameters.AddWithValue("key", idempotencyKey);
                idempotencyCommand.Parameters.AddWithValue("workspace", workspace.Id);
                idempotencyCommand.Parameters.AddWithValue("fingerprint", fingerprint);
                await idempotencyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (workspace, WorkspaceRole.Owner);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> SetActiveWorkspaceAsync(
        Guid userId,
        Guid sessionId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE sessions s
            SET active_workspace_id = @workspace
            WHERE s.id = @session
              AND s.user_id = @user
              AND s.expires_at > now()
              AND EXISTS (
                  SELECT 1 FROM memberships m
                  WHERE m.user_id = s.user_id AND m.workspace_id = @workspace)
            """,
            connection);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("session", sessionId);
        command.Parameters.AddWithValue("user", userId);

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task CreateInvitationAsync(
        string tokenHash,
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO invitations(token_hash, workspace_id, email, role, expires_at) VALUES (@hash, @workspace, @email, @role, @expires)",
            connection);
        command.Parameters.AddWithValue("hash", tokenHash);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("email", email.Trim());
        command.Parameters.AddWithValue("role", role.ToString());
        command.Parameters.AddWithValue("expires", expiresAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> InvitationExistsAsync(string tokenHash, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM invitations WHERE token_hash = @hash AND consumed_at IS NULL AND expires_at > now())",
            connection);
        command.Parameters.AddWithValue("hash", tokenHash);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task RecordOnboardingActionAsync(
        Guid userId,
        Guid workspaceId,
        string actionKey,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO onboarding_actions(user_id, workspace_id, action_key) VALUES (@user, @workspace, @key) ON CONFLICT (user_id, workspace_id, action_key) DO UPDATE SET requested_at = now()",
            connection);
        command.Parameters.AddWithValue("user", userId);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("key", actionKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static User ReadUser(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2).ToUpperInvariant(),
        reader.GetString(3));

    private static Workspace ReadWorkspace(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2).Trim(),
        reader.GetString(3).Trim(),
        reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.GetString(5));

    private static async Task<(Workspace Workspace, WorkspaceRole Role)> ReadWorkspaceWithRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT w.id, w.business_name, w.country, w.currency, w.vat_number, w.slug, m.role FROM workspaces w JOIN memberships m ON m.workspace_id = w.id WHERE w.id = @workspace AND m.user_id = @user",
            connection,
            transaction);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("user", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("idempotency_target_missing");
        }

        return (ReadWorkspace(reader), ParseRole(reader.GetString(6)));
    }

    private static async Task<bool> WorkspaceSlugExistsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string slug,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM workspaces WHERE slug = @slug)",
            connection,
            transaction);
        command.Parameters.AddWithValue("slug", slug);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private static string Fingerprint(CreateWorkspaceRequest request) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{request.BusinessName.Trim()}|{request.Country.Trim().ToUpperInvariant()}|{request.Currency.Trim().ToUpperInvariant()}|{request.VatNumber?.Trim()}")));

    private static WorkspaceRole ParseRole(string value) => Enum.Parse<WorkspaceRole>(value, ignoreCase: true);

    private static string Slugify(string value)
    {
        var slugParts = value
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join('-', slugParts).Replace("'", string.Empty);
    }
}

public static class PostgresDatabaseInitializer
{
    public static async Task ApplyAsync(string connectionString, CancellationToken cancellationToken)
    {
        var migrationPath = Path.Combine(AppContext.BaseDirectory, "Database", "Migrations", "001_access.sql");
        if (!File.Exists(migrationPath))
        {
            throw new FileNotFoundException("Access database migration was not published.", migrationPath);
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = new NpgsqlCommand(
                await File.ReadAllTextAsync(migrationPath, cancellationToken),
                connection,
                transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
