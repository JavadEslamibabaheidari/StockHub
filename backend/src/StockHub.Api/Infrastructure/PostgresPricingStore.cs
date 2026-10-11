using Npgsql;
using NpgsqlTypes;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class PostgresPricingStore(string connectionString)
{
    public async Task<IReadOnlyList<StoredPricingRule>> ListAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        return await ReadAsync(connection, workspaceId, null, ct);
    }

    public async Task<PricingSnapshot> SnapshotAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var result = new PricingSnapshot(
            await ProductsAsync(connection, workspaceId, transaction, ct),
            await ReadAsync(connection, workspaceId, transaction, ct));
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<PricingRule?> SaveAsync(Guid workspaceId, Guid? ruleId, RuleInput input, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await LockWorkspaceAsync(connection, transaction, workspaceId, ct);
        var rules = await ReadAsync(connection, workspaceId, transaction, ct);
        var existing = rules.FirstOrDefault(rule => rule.Rule.Id == ruleId);
        if (ruleId is not null && existing is null) return null;

        var products = await ProductsAsync(connection, workspaceId, transaction, ct);
        var valid = Pricing.Validate(input, products);
        var rule = Pricing.ToRule(ruleId ?? Guid.NewGuid(), valid);
        var candidate = rules.Where(item => item.Rule.Id != rule.Id)
            .Append(new StoredPricingRule(rule, existing?.Sequence ?? long.MaxValue)).ToArray();
        Pricing.ValidatePrices(products, candidate, DateOnly.FromDateTime(DateTime.UtcNow));

        var sql = ruleId is null
            ? "INSERT INTO pricing_rules(id,workspace_id,name,platform,scope,scope_value,adjustment,unit,enabled,priority,start_date,end_date) VALUES (@id,@workspace,@name,@platform,@scope,@scopeValue,@adjustment,@unit,@enabled,@priority,@start,@end)"
            : "UPDATE pricing_rules SET name=@name,platform=@platform,scope=@scope,scope_value=@scopeValue,adjustment=@adjustment,unit=@unit,enabled=@enabled,priority=@priority,start_date=@start,end_date=@end WHERE workspace_id=@workspace AND id=@id";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddRuleParameters(command, workspaceId, rule);
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        return rule;
    }

    public async Task<bool> DeleteAsync(Guid workspaceId, Guid ruleId, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await LockWorkspaceAsync(connection, transaction, workspaceId, ct);
        var rules = await ReadAsync(connection, workspaceId, transaction, ct);
        if (!rules.Any(rule => rule.Rule.Id == ruleId)) return false;
        var remaining = rules.Where(rule => rule.Rule.Id != ruleId).ToArray();
        Pricing.ValidatePrices(await ProductsAsync(connection, workspaceId, transaction, ct), remaining,
            DateOnly.FromDateTime(DateTime.UtcNow));
        await using var command = new NpgsqlCommand(
            "DELETE FROM pricing_rules WHERE workspace_id=@workspace AND id=@id", connection, transaction);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("id", ruleId);
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<PricingPreview> PreviewAsync(Guid workspaceId, PreviewInput input, CancellationToken ct)
    {
        var snapshot = await SnapshotAsync(workspaceId, ct);
        return Pricing.Preview(input, snapshot.Products, snapshot.Rules, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    internal static async Task LockWorkspaceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid workspaceId,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            "SELECT id FROM workspaces WHERE id=@workspace FOR UPDATE", connection, transaction);
        command.Parameters.AddWithValue("workspace", workspaceId);
        if (await command.ExecuteScalarAsync(ct) is null)
            throw new KeyNotFoundException("Workspace not found.");
    }

    internal static async Task<IReadOnlyList<StoredPricingRule>> ReadAsync(
        NpgsqlConnection connection,
        Guid workspaceId,
        NpgsqlTransaction? transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            "SELECT id,name,platform,scope,scope_value,adjustment,unit,enabled,priority,start_date,end_date,creation_sequence FROM pricing_rules WHERE workspace_id=@workspace ORDER BY priority,creation_sequence,id",
            connection, transaction);
        command.Parameters.AddWithValue("workspace", workspaceId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<StoredPricingRule>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new(new PricingRule(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4), reader.GetDecimal(5), reader.GetString(6),
                reader.GetBoolean(7), reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetFieldValue<DateOnly>(9),
                reader.IsDBNull(10) ? null : reader.GetFieldValue<DateOnly>(10)), reader.GetInt64(11)));
        }
        return result;
    }

    internal static async Task<IReadOnlyList<Product>> ProductsAsync(
        NpgsqlConnection connection,
        Guid workspaceId,
        NpgsqlTransaction? transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            "SELECT id,workspace_id,sku,name,on_hand,base_price,category FROM products WHERE workspace_id=@workspace ORDER BY sku",
            connection, transaction);
        command.Parameters.AddWithValue("workspace", workspaceId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var products = new List<Product>();
        while (await reader.ReadAsync(ct))
        {
            products.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                reader.GetInt32(4), reader.GetDecimal(5), reader.IsDBNull(6) ? null : reader.GetString(6)));
        }
        return products;
    }

    internal static void AddRuleParameters(NpgsqlCommand command, Guid workspaceId, PricingRule rule)
    {
        command.Parameters.AddWithValue("id", rule.Id);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("name", rule.Name);
        command.Parameters.AddWithValue("platform", rule.Platform);
        command.Parameters.AddWithValue("scope", rule.Scope);
        command.Parameters.AddWithValue("scopeValue", NpgsqlDbType.Text, (object?)rule.ScopeValue ?? DBNull.Value);
        command.Parameters.AddWithValue("adjustment", rule.Adjustment);
        command.Parameters.AddWithValue("unit", rule.Unit);
        command.Parameters.AddWithValue("enabled", rule.Enabled);
        command.Parameters.AddWithValue("priority", rule.Priority);
        command.Parameters.AddWithValue("start", NpgsqlDbType.Date, (object?)rule.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("end", NpgsqlDbType.Date, (object?)rule.EndDate ?? DBNull.Value);
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
