using Npgsql;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class PostgresProductStore(string connectionString) : IProductStore
{
    public async Task<IReadOnlyList<Product>> ListAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, workspace_id, sku, name, on_hand, base_price, category
            FROM products
            WHERE workspace_id = @workspace
            ORDER BY created_at, sku
            """,
            connection);
        command.Parameters.AddWithValue("workspace", workspaceId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var products = new List<Product>();
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }

    public async Task<IReadOnlyList<Product>> UpsertAsync(
        Guid workspaceId,
        IReadOnlyList<ProductRequest> products,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var result = new List<Product>();

        foreach (var product in products)
        {
            var id = Guid.NewGuid();
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO products(id, workspace_id, sku, name, on_hand, base_price, category)
                VALUES (@id, @workspace, @sku, @name, @onHand, @basePrice, @category)
                ON CONFLICT (workspace_id, sku) DO UPDATE
                SET name = EXCLUDED.name,
                    on_hand = EXCLUDED.on_hand,
                    base_price = EXCLUDED.base_price,
                    category = EXCLUDED.category,
                    updated_at = now()
                RETURNING id, workspace_id, sku, name, on_hand, base_price, category
                """,
                connection,
                transaction);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("workspace", workspaceId);
            command.Parameters.AddWithValue("sku", product.Sku.Trim());
            command.Parameters.AddWithValue("name", product.Name.Trim());
            command.Parameters.AddWithValue("onHand", product.OnHand);
            command.Parameters.AddWithValue("basePrice", product.BasePrice);
            command.Parameters.AddWithValue("category", string.IsNullOrWhiteSpace(product.Category) ? DBNull.Value : product.Category.Trim());

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                result.Add(ReadProduct(reader));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<Product?> UpdateOnHandAsync(
        Guid workspaceId,
        Guid productId,
        int onHand,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE products
            SET on_hand = @onHand,
                updated_at = now()
            WHERE workspace_id = @workspace
              AND id = @product
            RETURNING id, workspace_id, sku, name, on_hand, base_price, category
            """,
            connection);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("product", productId);
        command.Parameters.AddWithValue("onHand", onHand);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProduct(reader) : null;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static Product ReadProduct(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetInt32(4),
        reader.GetDecimal(5),
        reader.IsDBNull(6) ? null : reader.GetString(6));
}
