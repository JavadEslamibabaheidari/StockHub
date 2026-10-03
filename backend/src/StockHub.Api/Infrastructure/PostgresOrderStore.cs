using Npgsql;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;

namespace StockHub.Api.Infrastructure;

public sealed class PostgresOrderStore(string connectionString) : IOrderStore
{
    private static readonly string[] ValidStatuses = ["AwaitingPayment", "PaidToPick", "PickedToShip", "Shipped", "Delivered", "ReturnRequested", "Cancelled"];

    public async Task<DashboardImportResponse> ImportAsync(
        Guid workspaceId,
        DashboardImportRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var productIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var product in request.Products)
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
                RETURNING id
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
            productIds[product.Sku.Trim()] = (Guid)(await command.ExecuteScalarAsync(cancellationToken) ?? id);
        }

        foreach (var order in request.Orders)
        {
            var status = NormalizeStatus(order.Status);
            if (!ValidStatuses.Contains(status, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("order_status_invalid");
            }

            var id = Guid.NewGuid();
            await using (var command = new NpgsqlCommand(
                """
                INSERT INTO orders(id, workspace_id, order_number, platform, customer_name, ship_to, carrier, placed_at, status, return_stage, return_reason)
                VALUES (@id, @workspace, @orderNumber, @platform, @customer, @shipTo, @carrier, @placedAt, @status, @returnStage, @returnReason)
                ON CONFLICT (workspace_id, order_number) DO UPDATE
                SET platform = EXCLUDED.platform,
                    customer_name = EXCLUDED.customer_name,
                    ship_to = EXCLUDED.ship_to,
                    carrier = EXCLUDED.carrier,
                    placed_at = EXCLUDED.placed_at,
                    status = EXCLUDED.status,
                    return_stage = EXCLUDED.return_stage,
                    return_reason = EXCLUDED.return_reason,
                    cancel_reason = NULL,
                    updated_at = now()
                RETURNING id
                """,
                connection,
                transaction))
            {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("workspace", workspaceId);
                command.Parameters.AddWithValue("orderNumber", order.OrderNumber.Trim());
                command.Parameters.AddWithValue("platform", NormalizePlatform(order.Platform));
                command.Parameters.AddWithValue("customer", order.CustomerName.Trim());
                command.Parameters.AddWithValue("shipTo", order.ShipTo.Trim());
                command.Parameters.AddWithValue("carrier", order.Carrier.Trim());
                command.Parameters.AddWithValue("placedAt", order.PlacedAt);
                command.Parameters.AddWithValue("status", status);
                command.Parameters.AddWithValue("returnStage", status == "ReturnRequested" ? "Requested" : DBNull.Value);
                command.Parameters.AddWithValue("returnReason", string.IsNullOrWhiteSpace(order.ReturnReason) ? DBNull.Value : order.ReturnReason.Trim());
                id = (Guid)(await command.ExecuteScalarAsync(cancellationToken) ?? id);
            }

            await using (var deleteItems = new NpgsqlCommand("DELETE FROM order_items WHERE order_id = @order", connection, transaction))
            {
                deleteItems.Parameters.AddWithValue("order", id);
                await deleteItems.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var item in order.Items)
            {
                var sku = item.Sku.Trim();
                productIds.TryGetValue(sku, out var productId);
                if (productId == Guid.Empty)
                {
                    productId = await FindProductIdAsync(connection, transaction, workspaceId, sku, cancellationToken) ?? Guid.Empty;
                }

                await using var itemCommand = new NpgsqlCommand(
                    """
                    INSERT INTO order_items(id, order_id, product_id, sku, product_name, quantity, unit_price, cost_of_goods)
                    VALUES (@id, @order, @product, @sku, @name, @quantity, @unitPrice, @costOfGoods)
                    """,
                    connection,
                    transaction);
                itemCommand.Parameters.AddWithValue("id", Guid.NewGuid());
                itemCommand.Parameters.AddWithValue("order", id);
                itemCommand.Parameters.AddWithValue("product", productId == Guid.Empty ? DBNull.Value : productId);
                itemCommand.Parameters.AddWithValue("sku", sku);
                itemCommand.Parameters.AddWithValue("name", item.ProductName.Trim());
                itemCommand.Parameters.AddWithValue("quantity", item.Quantity);
                itemCommand.Parameters.AddWithValue("unitPrice", item.UnitPrice);
                itemCommand.Parameters.AddWithValue("costOfGoods", item.CostOfGoods ?? Math.Round(item.UnitPrice * .72m, 2));
                await itemCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await RecordEventAsync(connection, transaction, id, "Imported", $"Imported {order.OrderNumber.Trim()}", $"Status {DisplayStatus(status)} from {NormalizePlatform(order.Platform)}", order.PlacedAt, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new DashboardImportResponse(request.Products.Count, request.Orders.Count);
    }

    public async Task<OrderListResponse> ListAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var rows = await LoadSummariesAsync(workspaceId, null, cancellationToken);
        var today = rows.Count(row => row.PlacedAt.Date == DateTimeOffset.UtcNow.Date);
        return new OrderListResponse(
            today == 0 ? rows.Count : today,
            rows.Count == 0 ? "No orders imported" : "Backend data · no mocked orders",
            rows);
    }

    public async Task<OrderDetailResponse?> GetAsync(Guid workspaceId, Guid orderId, CancellationToken cancellationToken)
    {
        var summaries = await LoadSummariesAsync(workspaceId, orderId, cancellationToken);
        var summary = summaries.SingleOrDefault();
        if (summary is null)
        {
            return null;
        }

        return await LoadDetailAsync(workspaceId, summary, cancellationToken);
    }

    public async Task<OrderDetailResponse?> ApplyActionAsync(
        Guid workspaceId,
        Guid orderId,
        OrderActionRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var current = await LoadOrderStatusAsync(connection, transaction, workspaceId, orderId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var (status, returnStage, message) = request.Action.Trim().ToLowerInvariant() switch
        {
            "mark-picked" or "pick" => ("PickedToShip", (string?)null, "Order picked and stock deducted."),
            "mark-shipped" or "ship" => ("Shipped", (string?)null, "Order marked shipped."),
            "cancel" => ("Cancelled", (string?)null, "Order cancelled and reserved stock released."),
            "request-return" => ("ReturnRequested", "Requested", "Return requested."),
            "approve-return" => ("ReturnRequested", "Approved", "Return approved."),
            "receive-return" => ("ReturnRequested", "Received", "Return received."),
            "restock-return" => ("ReturnRequested", "Restocked", "Return restocked and stock increased."),
            "refund" => ("ReturnRequested", "Refunded", "Return refunded."),
            "reject-return" => ("Delivered", "Rejected", "Return rejected."),
            _ => throw new InvalidOperationException("order_action_unknown")
        };

        if (request.Action.Trim().Equals("mark-picked", StringComparison.OrdinalIgnoreCase)
            || request.Action.Trim().Equals("pick", StringComparison.OrdinalIgnoreCase))
        {
            await AdjustProductsForOrderAsync(connection, transaction, orderId, -1, cancellationToken);
        }

        if (request.Action.Trim().Equals("restock-return", StringComparison.OrdinalIgnoreCase))
        {
            await AdjustProductsForOrderAsync(connection, transaction, orderId, 1, cancellationToken);
        }

        await using (var command = new NpgsqlCommand(
            """
            UPDATE orders
            SET status = @status,
                return_stage = @returnStage::text,
                cancel_reason = CASE WHEN @status = 'Cancelled' THEN @reason::text ELSE cancel_reason END,
                return_reason = CASE WHEN @returnStage::text IS NOT NULL THEN COALESCE(return_reason, @reason::text) ELSE return_reason END,
                updated_at = now()
            WHERE workspace_id = @workspace AND id = @order
            """,
            connection,
            transaction))
        {
            command.Parameters.AddWithValue("workspace", workspaceId);
            command.Parameters.AddWithValue("order", orderId);
            command.Parameters.AddWithValue("status", status);
            command.Parameters.AddWithValue("returnStage", (object?)returnStage ?? DBNull.Value);
            command.Parameters.AddWithValue("reason", string.IsNullOrWhiteSpace(request.Reason) ? DBNull.Value : request.Reason.Trim());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await RecordEventAsync(connection, transaction, orderId, "Action", message, request.Reason ?? DisplayStatus(status), DateTimeOffset.UtcNow, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(workspaceId, orderId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> ReservedByProductAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT item.product_id, COALESCE(SUM(item.quantity), 0)::int
            FROM orders ord
            JOIN order_items item ON item.order_id = ord.id
            WHERE ord.workspace_id = @workspace
              AND ord.status IN ('AwaitingPayment','PaidToPick')
              AND item.product_id IS NOT NULL
            GROUP BY item.product_id
            """,
            connection);
        command.Parameters.AddWithValue("workspace", workspaceId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new Dictionary<Guid, int>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result[reader.GetGuid(0)] = reader.GetInt32(1);
        }

        return result;
    }

    private async Task<IReadOnlyList<OrderSummaryResponse>> LoadSummariesAsync(Guid workspaceId, Guid? orderId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT ord.id,
                   ord.order_number,
                   ord.platform,
                   ord.customer_name,
                   ord.placed_at,
                   ord.status,
                   COALESCE(SUM(item.quantity), 0)::int quantity,
                   COALESCE(SUM(item.quantity * item.unit_price), 0)::numeric total,
                   COALESCE(SUM(item.quantity * item.cost_of_goods), 0)::numeric cost_of_goods,
                   MIN(item.product_name) product_name,
                   MIN(item.sku) sku
            FROM orders ord
            JOIN order_items item ON item.order_id = ord.id
            WHERE ord.workspace_id = @workspace
              AND (@orderId::uuid IS NULL OR ord.id = @orderId::uuid)
            GROUP BY ord.id
            ORDER BY ord.placed_at DESC
            """,
            connection);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("orderId", (object?)orderId ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<OrderSummaryResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var platform = reader.GetString(2);
            var total = reader.GetDecimal(7);
            var fee = PlatformFee(platform, total);
            var net = total - fee;
            var cost = reader.GetDecimal(8);
            var status = reader.GetString(5);
            result.Add(new OrderSummaryResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(9),
                reader.GetString(10),
                platform,
                PlatformCode(platform),
                reader.GetInt32(6),
                DisplayStatus(status),
                total,
                fee,
                net,
                status == "Cancelled" ? null : net - cost,
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                Age(reader.GetFieldValue<DateTimeOffset>(4)),
                NextStep(status)));
        }

        return result;
    }

    private async Task<OrderDetailResponse> LoadDetailAsync(Guid workspaceId, OrderSummaryResponse summary, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var items = new List<OrderItemResponse>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT COALESCE(product_id, '00000000-0000-0000-0000-000000000000'::uuid), product_name, sku, quantity, unit_price
            FROM order_items
            WHERE order_id = @order
            ORDER BY product_name
            """,
            connection))
        {
            command.Parameters.AddWithValue("order", summary.Id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var quantity = reader.GetInt32(3);
                var unit = reader.GetDecimal(4);
                items.Add(new OrderItemResponse(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), quantity, unit, quantity * unit));
            }
        }

        string shipTo;
        string carrier;
        string? returnStage;
        string? returnReason;
        await using (var command = new NpgsqlCommand(
            "SELECT ship_to, carrier, return_stage, return_reason FROM orders WHERE workspace_id = @workspace AND id = @order",
            connection))
        {
            command.Parameters.AddWithValue("workspace", workspaceId);
            command.Parameters.AddWithValue("order", summary.Id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            shipTo = reader.GetString(0);
            carrier = reader.GetString(1);
            returnStage = reader.IsDBNull(2) ? null : reader.GetString(2);
            returnReason = reader.IsDBNull(3) ? null : reader.GetString(3);
        }

        var events = new List<OrderStockMovementResponse>();
        await using (var command = new NpgsqlCommand(
            "SELECT kind, title, detail, occurred_at FROM order_events WHERE order_id = @order ORDER BY occurred_at",
            connection))
        {
            command.Parameters.AddWithValue("order", summary.Id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                events.Add(new OrderStockMovementResponse(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetFieldValue<DateTimeOffset>(3)));
            }
        }

        var cost = await CostOfGoodsAsync(connection, summary.Id, cancellationToken);
        return new OrderDetailResponse(
            summary,
            items,
            new OrderCustomerShippingResponse(summary.Customer, shipTo, carrier),
            Timeline(summary.Status),
            events,
            new OrderFinancialsResponse(summary.Total, Math.Round(summary.Total * .22m / 1.22m, 2), summary.Fee, summary.Net, cost, summary.Margin),
            returnStage is null ? null : new OrderReturnResponse(returnStage, returnReason ?? "Customer request", summary.PlacedAt));
    }

    private async Task<(string Status, string? ReturnStage)?> LoadOrderStatusAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid workspaceId, Guid orderId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT status, return_stage FROM orders WHERE workspace_id = @workspace AND id = @order", connection, transaction);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("order", orderId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? (reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1))
            : null;
    }

    private static async Task<Guid?> FindProductIdAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid workspaceId, string sku, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT id FROM products WHERE workspace_id = @workspace AND sku = @sku", connection, transaction);
        command.Parameters.AddWithValue("workspace", workspaceId);
        command.Parameters.AddWithValue("sku", sku);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : null;
    }

    private static async Task AdjustProductsForOrderAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid orderId, int direction, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE products product
            SET on_hand = GREATEST(0, product.on_hand + (item.quantity * @direction)),
                updated_at = now()
            FROM order_items item
            WHERE item.order_id = @order
              AND item.product_id = product.id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("order", orderId);
        command.Parameters.AddWithValue("direction", direction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RecordEventAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid orderId, string kind, string title, string detail, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "INSERT INTO order_events(id, order_id, kind, title, detail, occurred_at) VALUES (@id, @order, @kind, @title, @detail, @occurredAt)",
            connection,
            transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("order", orderId);
        command.Parameters.AddWithValue("kind", kind);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("detail", detail);
        command.Parameters.AddWithValue("occurredAt", occurredAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<decimal> CostOfGoodsAsync(NpgsqlConnection connection, Guid orderId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT COALESCE(SUM(quantity * cost_of_goods), 0)::numeric FROM order_items WHERE order_id = @order", connection);
        command.Parameters.AddWithValue("order", orderId);
        return (decimal)(await command.ExecuteScalarAsync(cancellationToken) ?? 0m);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string NormalizeStatus(string status) => status.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "") switch
    {
        "awaitingpayment" => "AwaitingPayment",
        "paidtopick" => "PaidToPick",
        "pickedtoship" => "PickedToShip",
        "shipped" => "Shipped",
        "delivered" => "Delivered",
        "returnrequested" => "ReturnRequested",
        "cancelled" or "canceled" => "Cancelled",
        _ => status
    };

    private static string DisplayStatus(string status) => status switch
    {
        "AwaitingPayment" => "Awaiting payment",
        "PaidToPick" => "Paid - to pick",
        "PickedToShip" => "Picked - to ship",
        "ReturnRequested" => "Return requested",
        _ => status
    };

    private static string NormalizePlatform(string platform) => platform.Trim().ToLowerInvariant() switch
    {
        "amazon" or "am" => "Amazon",
        "unieuro" or "un" => "Unieuro",
        "euronics" or "eu" => "Euronics",
        "ebay" or "eb" => "eBay",
        _ => platform.Trim()
    };

    private static string PlatformCode(string platform) => platform switch
    {
        "Amazon" => "Am",
        "Unieuro" => "Un",
        "Euronics" => "Eu",
        "eBay" => "eB",
        _ => platform[..Math.Min(2, platform.Length)]
    };

    private static decimal PlatformFee(string platform, decimal total) => Math.Round(total * (platform switch
    {
        "Amazon" => .08m,
        "Unieuro" => .10m,
        "Euronics" => .09m,
        "eBay" => .08m,
        _ => .08m
    }), 2);

    private static string NextStep(string status) => status switch
    {
        "AwaitingPayment" => "Held until payment",
        "PaidToPick" => "Pick",
        "PickedToShip" => "Mark shipped",
        "Shipped" => "Tracking",
        "Delivered" => "Done",
        "ReturnRequested" => "Receive return",
        "Cancelled" => "Nothing to do",
        _ => "Review"
    };

    private static string Age(DateTimeOffset placedAt)
    {
        var elapsed = DateTimeOffset.UtcNow - placedAt.ToUniversalTime();
        if (elapsed.TotalMinutes < 60)
        {
            return $"{Math.Max(1, (int)elapsed.TotalMinutes)} min ago";
        }

        if (elapsed.TotalHours < 24)
        {
            return $"{Math.Max(1, (int)elapsed.TotalHours)} h ago";
        }

        return placedAt.ToString("dd MMM yyyy, HH:mm");
    }

    private static IReadOnlyList<OrderTimelineStepResponse> Timeline(string displayStatus)
    {
        var order = new[] { "Reserved", "Paid", "Picked", "Shipped", "Delivered" };
        var reached = displayStatus switch
        {
            "Awaiting payment" => 0,
            "Paid - to pick" => 1,
            "Picked - to ship" => 2,
            "Shipped" => 3,
            "Delivered" or "Return requested" => 4,
            _ => -1
        };
        return order.Select((label, index) => new OrderTimelineStepResponse(label.ToLowerInvariant(), label, index <= reached ? "done" : index == reached + 1 ? "active" : "pending", index <= reached ? "Completed" : "Pending")).ToArray();
    }
}
