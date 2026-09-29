using Npgsql;
using StockHub.Api.Abstractions;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class PostgresDashboardStore(string connectionString) : IDashboardStore
{
    public async Task<DashboardWorkspaceState> GetAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO dashboard_states(workspace_id)
            VALUES (@workspace)
            ON CONFLICT (workspace_id) DO NOTHING;

            SELECT workspace_id, mode, euronics_retried, mismatch_resolved, restock_listed, show_more_low_stock, detail_panel, updated_at
            FROM dashboard_states
            WHERE workspace_id = @workspace
            """,
            connection);
        command.Parameters.AddWithValue("workspace", workspaceId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadState(reader)
            : DashboardWorkspaceState.Create(workspaceId);
    }

    public async Task<DashboardWorkspaceState> SaveAsync(DashboardWorkspaceState state, CancellationToken cancellationToken)
    {
        var saved = state with { UpdatedAt = DateTimeOffset.UtcNow };
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO dashboard_states(workspace_id, mode, euronics_retried, mismatch_resolved, restock_listed, show_more_low_stock, detail_panel, updated_at)
            VALUES (@workspace, @mode, @euronicsRetried, @mismatchResolved, @restockListed, @showMoreLowStock, @detailPanel, @updatedAt)
            ON CONFLICT (workspace_id) DO UPDATE
            SET mode = EXCLUDED.mode,
                euronics_retried = EXCLUDED.euronics_retried,
                mismatch_resolved = EXCLUDED.mismatch_resolved,
                restock_listed = EXCLUDED.restock_listed,
                show_more_low_stock = EXCLUDED.show_more_low_stock,
                detail_panel = EXCLUDED.detail_panel,
                updated_at = EXCLUDED.updated_at
            RETURNING workspace_id, mode, euronics_retried, mismatch_resolved, restock_listed, show_more_low_stock, detail_panel, updated_at
            """,
            connection);
        command.Parameters.AddWithValue("workspace", saved.WorkspaceId);
        command.Parameters.AddWithValue("mode", saved.Mode);
        command.Parameters.AddWithValue("euronicsRetried", saved.EuronicsRetried);
        command.Parameters.AddWithValue("mismatchResolved", saved.MismatchResolved);
        command.Parameters.AddWithValue("restockListed", saved.RestockListed);
        command.Parameters.AddWithValue("showMoreLowStock", saved.ShowMoreLowStock);
        command.Parameters.AddWithValue("detailPanel", (object?)saved.DetailPanel ?? DBNull.Value);
        command.Parameters.AddWithValue("updatedAt", saved.UpdatedAt);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Dashboard state was not saved.");
        }

        return ReadState(reader);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static DashboardWorkspaceState ReadState(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetBoolean(2),
        reader.GetBoolean(3),
        reader.GetBoolean(4),
        reader.GetBoolean(5),
        reader.IsDBNull(6) ? null : reader.GetString(6),
        reader.GetFieldValue<DateTimeOffset>(7));
}
