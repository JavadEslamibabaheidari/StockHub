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

    public async Task<(User User, Session Session)> CreateUserWithSessionAsync(
        string fullName,
        string normalizedEmail,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        var user = new User(Guid.NewGuid(), fullName.Trim(), normalizedEmail, passwordHash);
        var session = new Session(Guid.NewGuid(), user.Id, null, DateTimeOffset.UtcNow.AddHours(8));
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var userCommand = new NpgsqlCommand(
                "INSERT INTO users(id, full_name, email, password_hash) VALUES (@id, @name, @email, @hash)",
                connection,
                transaction))
            {
                userCommand.Parameters.AddWithValue("id", user.Id);
                userCommand.Parameters.AddWithValue("name", user.FullName);
                userCommand.Parameters.AddWithValue("email", user.NormalizedEmail);
                userCommand.Parameters.AddWithValue("hash", user.PasswordHash);
                await userCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var sessionCommand = new NpgsqlCommand(
                "INSERT INTO sessions(id, user_id, expires_at) VALUES (@id, @user, @expires)",
                connection,
                transaction))
            {
                sessionCommand.Parameters.AddWithValue("id", session.Id);
                sessionCommand.Parameters.AddWithValue("user", user.Id);
                sessionCommand.Parameters.AddWithValue("expires", session.ExpiresAt);
                await sessionCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (user, session);
        }
        catch (PostgresException exception) when (exception.SqlState == "23505")
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException("duplicate_user", exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
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

    public async Task CreatePasswordResetAsync(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var remove = new NpgsqlCommand(
            "DELETE FROM password_reset_tokens WHERE user_id = @user",
            connection,
            transaction))
        {
            remove.Parameters.AddWithValue("user", userId);
            await remove.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insert = new NpgsqlCommand(
            "INSERT INTO password_reset_tokens(token_hash, user_id, expires_at) VALUES (@hash, @user, @expires)",
            connection,
            transaction))
        {
            insert.Parameters.AddWithValue("hash", tokenHash);
            insert.Parameters.AddWithValue("user", userId);
            insert.Parameters.AddWithValue("expires", expiresAt);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(
        string tokenHash,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        Guid userId;
        await using (var lookup = new NpgsqlCommand(
            "SELECT user_id FROM password_reset_tokens WHERE token_hash = @hash AND expires_at > now() AND consumed_at IS NULL FOR UPDATE",
            connection,
            transaction))
        {
            lookup.Parameters.AddWithValue("hash", tokenHash);
            var found = await lookup.ExecuteScalarAsync(cancellationToken);
            if (found is not Guid id)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            userId = id;
        }

        await using (var update = new NpgsqlCommand(
            "UPDATE users SET password_hash = @password WHERE id = @user",
            connection,
            transaction))
        {
            update.Parameters.AddWithValue("password", passwordHash);
            update.Parameters.AddWithValue("user", userId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var sessions = new NpgsqlCommand(
            "DELETE FROM sessions WHERE user_id = @user",
            connection,
            transaction))
        {
            sessions.Parameters.AddWithValue("user", userId);
            await sessions.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var tokens = new NpgsqlCommand(
            "DELETE FROM password_reset_tokens WHERE user_id = @user",
            connection,
            transaction))
        {
            tokens.Parameters.AddWithValue("user", userId);
            await tokens.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<User> GetOrCreateGoogleUserAsync(
        string subject,
        string normalizedEmail,
        string fullName,
        bool allowExistingAccountLink,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var bySubject = new NpgsqlCommand(
            "SELECT u.id, u.full_name, u.email::text, u.password_hash FROM external_identities e JOIN users u ON u.id = e.user_id WHERE e.provider = 'google' AND e.subject = @subject",
            connection,
            transaction))
        {
            bySubject.Parameters.AddWithValue("subject", subject);
            await using var reader = await bySubject.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var linkedUser = ReadUser(reader);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return linkedUser;
            }
        }

        User user;
        bool userAlreadyExists;
        await using (var byEmail = new NpgsqlCommand(
            "SELECT id, full_name, email::text, password_hash FROM users WHERE email = @email FOR UPDATE",
            connection,
            transaction))
        {
            byEmail.Parameters.AddWithValue("email", normalizedEmail);
            await using var reader = await byEmail.ExecuteReaderAsync(cancellationToken);
            userAlreadyExists = await reader.ReadAsync(cancellationToken);
            user = userAlreadyExists
                ? ReadUser(reader)
                : new User(Guid.NewGuid(), fullName.Trim(), normalizedEmail, string.Empty);
        }

        if (userAlreadyExists && !allowExistingAccountLink)
        {
            throw new InvalidOperationException("external_account_conflict");
        }

        if (!userAlreadyExists)
        {
            await using var insertUser = new NpgsqlCommand(
                "INSERT INTO users(id, full_name, email, password_hash) VALUES (@id, @name, @email, @hash) ON CONFLICT (email) DO NOTHING",
                connection,
                transaction);
            insertUser.Parameters.AddWithValue("id", user.Id);
            insertUser.Parameters.AddWithValue("name", user.FullName);
            insertUser.Parameters.AddWithValue("email", user.NormalizedEmail);
            insertUser.Parameters.AddWithValue("hash", user.PasswordHash);
            if (await insertUser.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new InvalidOperationException("external_account_conflict");
            }
        }

        try
        {
            await using var link = new NpgsqlCommand(
                "INSERT INTO external_identities(provider, subject, user_id) VALUES ('google', @subject, @user)",
                connection,
                transaction);
            link.Parameters.AddWithValue("subject", subject);
            link.Parameters.AddWithValue("user", user.Id);
            await link.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return user;
        }
        catch (PostgresException exception) when (exception.SqlState == "23505")
        {
            throw new InvalidOperationException("external_account_conflict", exception);
        }
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

    public async Task<IReadOnlyList<Guid>> ListWorkspaceIdsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id FROM workspaces ORDER BY id", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ids = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken)) ids.Add(reader.GetGuid(0));
        return ids;
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
        Guid invitationId,
        string tokenHash,
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var lockCommand = new NpgsqlCommand(
                "SELECT pg_advisory_xact_lock(hashtextextended(@workspace::text, 0))",
                connection,
                transaction))
            {
                lockCommand.Parameters.AddWithValue("workspace", workspaceId);
                await lockCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var pending = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM invitations WHERE workspace_id = @workspace AND email = @email::citext AND consumed_at IS NULL AND expires_at > now())",
                connection,
                transaction))
            {
                pending.Parameters.AddWithValue("workspace", workspaceId);
                pending.Parameters.AddWithValue("email", email.Trim());
                if ((bool)(await pending.ExecuteScalarAsync(cancellationToken) ?? false))
                {
                    throw new InvalidOperationException("invitation_already_pending");
                }
            }

            await using (var member = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM memberships m JOIN users u ON u.id = m.user_id WHERE m.workspace_id = @workspace AND u.email = @email::citext)",
                connection,
                transaction))
            {
                member.Parameters.AddWithValue("workspace", workspaceId);
                member.Parameters.AddWithValue("email", email.Trim());
                if ((bool)(await member.ExecuteScalarAsync(cancellationToken) ?? false))
                {
                    throw new InvalidOperationException("user_already_member");
                }
            }

            await using var command = new NpgsqlCommand(
                "INSERT INTO invitations(id, token_hash, workspace_id, email, role, expires_at) VALUES (@id, @hash, @workspace, @email, @role, @expires)",
                connection,
                transaction);
            command.Parameters.AddWithValue("id", invitationId);
            command.Parameters.AddWithValue("hash", tokenHash);
            command.Parameters.AddWithValue("workspace", workspaceId);
            command.Parameters.AddWithValue("email", email.Trim());
            command.Parameters.AddWithValue("role", role.ToString());
            command.Parameters.AddWithValue("expires", expiresAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Invitation?> FindInvitationAsync(string tokenHash, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, workspace_id, email::text, role, expires_at, created_at, consumed_at
            FROM invitations
            WHERE token_hash = @hash AND consumed_at IS NULL AND expires_at > now()
            """,
            connection);
        command.Parameters.AddWithValue("hash", tokenHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Invitation(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            ParseRole(reader.GetString(3)),
            reader.GetFieldValue<DateTimeOffset>(4),
            reader.GetFieldValue<DateTimeOffset>(5),
            reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6));
    }

    public async Task<IReadOnlyList<TeamMemberResponse>> ListTeamMembersAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT u.id, u.full_name, u.email::text, m.role FROM memberships m JOIN users u ON u.id = m.user_id WHERE m.workspace_id = @workspace ORDER BY (m.role = 'Owner') DESC, u.full_name, u.email",
            connection);
        command.Parameters.AddWithValue("workspace", workspaceId);
        var members = new List<TeamMemberResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            members.Add(new TeamMemberResponse(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2), ParseRole(reader.GetString(3)), "Unavailable"));
        }

        return members;
    }

    public async Task<IReadOnlyList<PendingInvitationResponse>> ListPendingInvitationsAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, email::text, role, created_at, expires_at FROM invitations WHERE workspace_id = @workspace AND consumed_at IS NULL AND expires_at > now() ORDER BY created_at",
            connection);
        command.Parameters.AddWithValue("workspace", workspaceId);
        var invitations = new List<PendingInvitationResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            invitations.Add(new PendingInvitationResponse(
                reader.GetGuid(0), reader.GetString(1), ParseRole(reader.GetString(2)),
                reader.GetFieldValue<DateTimeOffset>(3), reader.GetFieldValue<DateTimeOffset>(4)));
        }

        return invitations;
    }

    public async Task<bool> RotateInvitationAsync(Guid workspaceId, Guid invitationId, string tokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE invitations SET token_hash = @hash, expires_at = @expires, created_at = now() WHERE id = @id AND workspace_id = @workspace AND consumed_at IS NULL AND expires_at > now()",
            connection);
        command.Parameters.AddWithValue("hash", tokenHash);
        command.Parameters.AddWithValue("expires", expiresAt);
        command.Parameters.AddWithValue("id", invitationId);
        command.Parameters.AddWithValue("workspace", workspaceId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> CancelInvitationAsync(Guid workspaceId, Guid invitationId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE invitations SET consumed_at = now() WHERE id = @id AND workspace_id = @workspace AND consumed_at IS NULL AND expires_at > now()",
            connection);
        command.Parameters.AddWithValue("id", invitationId);
        command.Parameters.AddWithValue("workspace", workspaceId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var workspaceLock = new NpgsqlCommand(
                "SELECT id FROM workspaces WHERE id = @workspace FOR UPDATE",
                connection,
                transaction))
            {
                workspaceLock.Parameters.AddWithValue("workspace", workspaceId);
                if (await workspaceLock.ExecuteScalarAsync(cancellationToken) is null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return false;
                }
            }

            await using (var removal = new NpgsqlCommand(
                "DELETE FROM memberships WHERE user_id = @user AND workspace_id = @workspace AND (role <> 'Owner' OR (SELECT count(*) FROM memberships WHERE workspace_id = @workspace AND role = 'Owner') > 1)",
                connection,
                transaction))
            {
                removal.Parameters.AddWithValue("user", userId);
                removal.Parameters.AddWithValue("workspace", workspaceId);
                var removed = await removal.ExecuteNonQueryAsync(cancellationToken) == 1;
                await transaction.CommitAsync(cancellationToken);
                return removed;
            }
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Guid?> AcceptInvitationAsync(string tokenHash, Guid userId, string normalizedEmail, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            Guid workspaceId;
            WorkspaceRole role;
            await using (var lookup = new NpgsqlCommand(
                """
                SELECT workspace_id, role
                FROM invitations
                WHERE token_hash = @hash
                  AND email = @email::citext
                  AND consumed_at IS NULL
                  AND expires_at > now()
                FOR UPDATE
                """,
                connection,
                transaction))
            {
                lookup.Parameters.AddWithValue("hash", tokenHash);
                lookup.Parameters.AddWithValue("email", normalizedEmail);
                await using var reader = await lookup.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                workspaceId = reader.GetGuid(0);
                role = ParseRole(reader.GetString(1));
            }

            await using (var membership = new NpgsqlCommand(
                """
                INSERT INTO memberships(user_id, workspace_id, role)
                VALUES (@user, @workspace, @role)
                ON CONFLICT (user_id, workspace_id) DO NOTHING
                """,
                connection,
                transaction))
            {
                membership.Parameters.AddWithValue("user", userId);
                membership.Parameters.AddWithValue("workspace", workspaceId);
                membership.Parameters.AddWithValue("role", role.ToString());
                await membership.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var consume = new NpgsqlCommand(
                "UPDATE invitations SET consumed_at = now() WHERE token_hash = @hash",
                connection,
                transaction))
            {
                consume.Parameters.AddWithValue("hash", tokenHash);
                await consume.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return workspaceId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task RecordOnboardingActionAsync(
        Guid userId,
        Guid workspaceId,
        string actionKey,
        CancellationToken cancellationToken)
    {
        if (!OnboardingTask.IsSupportedAction(actionKey))
        {
            throw new ArgumentException("Unsupported onboarding action.", nameof(actionKey));
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO onboarding_actions(user_id, workspace_id, action_key) VALUES (@user, @workspace, @key) ON CONFLICT (user_id, workspace_id, action_key) DO UPDATE SET requested_at = now()",
            connection,
            transaction);
        command.Parameters.AddWithValue("user", userId);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("key", actionKey);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await using var selectionCommand = new NpgsqlCommand(
            "INSERT INTO onboarding_selections(user_id, workspace_id, selected_action_key) VALUES (@user, @workspace, @key) ON CONFLICT (user_id, workspace_id) DO UPDATE SET selected_action_key = EXCLUDED.selected_action_key, selected_at = now()",
            connection,
            transaction);
        selectionCommand.Parameters.AddWithValue("user", userId);
        selectionCommand.Parameters.AddWithValue("workspace", workspaceId);
        selectionCommand.Parameters.AddWithValue("key", actionKey);
        await selectionCommand.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<string?> GetSelectedOnboardingActionAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT selected_action_key FROM onboarding_selections WHERE user_id = @user AND workspace_id = @workspace",
            connection);
        command.Parameters.AddWithValue("user", userId);
        command.Parameters.AddWithValue("workspace", workspaceId);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
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
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var migrationName in new[] { "001_access.sql", "002_auth_completion.sql", "003_onboarding_products.sql", "004_dashboard_state.sql", "005_orders.sql", "006_pricing.sql", "007_reports.sql", "008_team_management.sql", "009_onboarding_selection.sql", "010_business_notifications.sql" })
            {
                var migrationPath = Path.Combine(AppContext.BaseDirectory, "Database", "Migrations", migrationName);
                if (!File.Exists(migrationPath))
                {
                    throw new FileNotFoundException("Access database migration was not published.", migrationPath);
                }

                await using var command = new NpgsqlCommand(
                    await File.ReadAllTextAsync(migrationPath, cancellationToken),
                    connection,
                    transaction);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
