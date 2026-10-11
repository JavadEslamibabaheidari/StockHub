using System.Text.Json;
using Npgsql;
using StockHub.Api.Abstractions;
using StockHub.Api.Contracts;
using StockHub.Api.Domain;

namespace StockHub.Api.Infrastructure;

public sealed class PostgresReportsStore(string connectionString)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReportData> LoadAsync(Guid workspaceId, string currency, DateOnly start, DateOnly endExclusive, IOrderStore orderStore, IProductStore productStore, CancellationToken ct)
    {
        var startUtc = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var endUtc = new DateTimeOffset(endExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var rows = await orderStore.LoadReportOrdersAsync(workspaceId,startUtc.AddDays(-1),endUtc,ct);
        var reportSales = (await orderStore.LoadReportProductSalesAsync(workspaceId,startUtc,endUtc,ct)).ToDictionary(x => x.ProductId);
        var restockSales = (await orderStore.LoadReportProductSalesAsync(workspaceId,endUtc.AddDays(-14),endUtc,ct)).ToDictionary(x => x.ProductId);
        var trailingSales = (await orderStore.LoadReportProductSalesAsync(workspaceId,endUtc.AddDays(-30),endUtc,ct)).ToDictionary(x => x.ProductId);
        var reservedByProduct = await orderStore.ReservedByProductAsync(workspaceId,ct);
        var productRows = (await productStore.ListAsync(workspaceId,ct)).Select(product =>
        {
            reportSales.TryGetValue(product.Id,out var report); restockSales.TryGetValue(product.Id,out var restock); trailingSales.TryGetValue(product.Id,out var trailing);
            var available = ReportMath.AvailableUnits(product.OnHand,reservedByProduct.GetValueOrDefault(product.Id));
            return new ReportProduct(product.Id,product.Name,product.Sku,available,restock?.Units ?? 0,report?.Units ?? 0,report?.Revenue ?? 0m,trailing?.Units ?? 0,trailing?.Revenue ?? 0m);
        }).ToArray();
        var reorder = (await GetReorderIdsAsync(workspaceId,ct)).ToHashSet();
        var rangeStart = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rangeEnd = endExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var paid = rows.Where(r => IsRevenue(r) && r.PlacedAt >= rangeStart && r.PlacedAt < rangeEnd).ToArray();
        var platforms = paid.GroupBy(row => row.Platform).Select(group => new PlatformSales(group.Key, group.Count(), group.Sum(x => x.Units), group.Sum(x => x.Revenue), 0m)).ToArray();
        var totalRevenue = paid.Sum(x => x.Revenue);
        platforms = platforms.Select(x => x with { Share = totalRevenue == 0 ? 0 : Math.Round(x.Revenue * 100m / totalRevenue, 2) }).ToArray();
        var products = productRows.Select(p => new ProductSales(p.Id, p.Name, p.Sku, p.PeriodUnits, p.Revenue)).ToArray();
        var sold = ReportMath.TopProducts(products);
        IReadOnlyList<ProductSales> leastSold = productRows.Select(p => new ProductSales(p.Id,p.Name,p.Sku,p.Units30Days,p.Revenue30Days))
            .ToArray();
        leastSold = ReportMath.BottomProducts(leastSold);
        var restock = productRows.Select(p => {
            var metrics = ReportMath.RestockMetrics(p.Available,p.Units);
            return new RestockSuggestion(p.Id,p.Name,p.Sku,p.Available,metrics.AverageUnitsPerDay,metrics.DaysUntilStockout,metrics.SuggestedUnits,reorder.Contains(p.Id));
        }).Where(x => x.Available <= 10 || x.SuggestedUnits > 0).OrderBy(x => x.Available).ToArray();
        var lost = rows.Where(r => r.Status == "AwaitingPayment" && r.PlacedAt.AddMinutes(10) >= start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) && r.PlacedAt.AddMinutes(10) < endExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .Select(r => new LostSale(r.OrderNumber, r.ProductName, r.Platform, r.PlacedAt.AddMinutes(10), r.Revenue)).ToArray();
        var daily = paid.GroupBy(r => DateOnly.FromDateTime(r.PlacedAt.UtcDateTime)).ToDictionary(g => g.Key, g => g.Sum(r => r.Revenue));
        return new ReportData(currency, new ReportSummary(totalRevenue, paid.Length, paid.Sum(x => x.Units), paid.Length == 0 ? 0 : Math.Round(totalRevenue / paid.Length,2)), platforms,
            lost, sold, leastSold, restock,
            lost.Length, daily);
    }

    public async Task<IReadOnlyList<Guid>> GetReorderIdsAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("SELECT product_id FROM report_reorder_items WHERE workspace_id=@workspace ORDER BY created_at,product_id", connection);
        command.Parameters.AddWithValue("workspace",workspaceId); await using var reader = await command.ExecuteReaderAsync(ct);
        var values = new List<Guid>(); while (await reader.ReadAsync(ct)) values.Add(reader.GetGuid(0)); return values;
    }

    public async Task<bool> SetReorderAsync(Guid workspaceId, Guid productId, bool listed, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(listed
            ? "INSERT INTO report_reorder_items(workspace_id,product_id) VALUES(@workspace,@product) ON CONFLICT DO NOTHING"
            : "DELETE FROM report_reorder_items WHERE workspace_id=@workspace AND product_id=@product", connection);
        command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("product",productId); await command.ExecuteNonQueryAsync(ct); return true;
    }

    public async Task<IReadOnlyList<AlertRule>> GetAlertsAsync(Guid workspaceId, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("SELECT settings FROM report_alert_settings WHERE workspace_id=@workspace", connection);
        command.Parameters.AddWithValue("workspace",workspaceId); var raw = await command.ExecuteScalarAsync(ct);
        return raw is string json ? JsonSerializer.Deserialize<AlertRule[]>(json, JsonOptions)! : DefaultAlerts;
    }

    public async Task SaveAlertsAsync(Guid workspaceId, IReadOnlyList<AlertRule> alerts, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("INSERT INTO report_alert_settings(workspace_id,settings,updated_at) VALUES(@workspace,@settings::jsonb,now()) ON CONFLICT(workspace_id) DO UPDATE SET settings=EXCLUDED.settings,updated_at=now()", connection);
        command.Parameters.AddWithValue("workspace",workspaceId); command.Parameters.AddWithValue("settings",JsonSerializer.Serialize(alerts,JsonOptions)); await command.ExecuteNonQueryAsync(ct);
    }

    public static readonly AlertRule[] DefaultAlerts = [
        new("lowStock",true,5,new(true,true,false),"daily"), new("outOfStock",true,0,new(true,true,true),"instant"),
        new("syncFailure",true,5,new(true,true,true),"instant"), new("oversellingRisk",true,3,new(false,true,true),"instant") ];
    private static bool IsRevenue(ReportOrderRecord row) => (row.Status is "PaidToPick" or "PickedToShip" or "Shipped" or "Delivered" or "ReturnRequested") && row.ReturnStage != "Refunded";
}

public sealed record ReportData(string Currency, ReportSummary Summary, IReadOnlyList<PlatformSales> Platforms, IReadOnlyList<LostSale> LostSales,
    IReadOnlyList<ProductSales> Top, IReadOnlyList<ProductSales> Bottom, IReadOnlyList<RestockSuggestion> Restock, int ExpiredCount, IReadOnlyDictionary<DateOnly,decimal> RevenueByDay);
internal sealed record ReportProduct(Guid Id,string Name,string Sku,int Available,int Units,int PeriodUnits,decimal Revenue,int Units30Days,decimal Revenue30Days);
public sealed record RestockMetrics(double AverageUnitsPerDay,double? DaysUntilStockout,int SuggestedUnits);
public static class ReportMath
{
    public static IReadOnlyList<ProductSales> TopProducts(IEnumerable<ProductSales> products) =>
        products.OrderByDescending(p => p.UnitCount).ThenBy(p => p.Name).Take(5).ToArray();

    public static IReadOnlyList<ProductSales> BottomProducts(IEnumerable<ProductSales> products) =>
        products.OrderBy(p => p.UnitCount).ThenBy(p => p.Name).Take(5).ToArray();

    public static decimal? RevenueChangePercent(decimal currentRevenue,decimal previousRevenue) => previousRevenue > 0
        ? Math.Round((currentRevenue-previousRevenue)*100m/previousRevenue,1)
        : null;

    public static int AvailableUnits(int onHand,int reserved) => Math.Max(0,onHand-reserved);

    public static RestockMetrics RestockMetrics(int available,int unitsSoldLast14Days)
    {
        var average = unitsSoldLast14Days / 14d;
        var days = average > 0 ? Math.Round(available / average,1) : (double?)null;
        return new RestockMetrics(Math.Round(average,2),days,Math.Max(0,(int)Math.Ceiling(average*14)-available));
    }
}
