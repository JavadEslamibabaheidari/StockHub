using StockHub.Api.Contracts;
using StockHub.Api.Infrastructure;
using StockHub.Api.Validation;

namespace StockHub.Api.Tests;

public sealed class ReportValidationTests
{
    private static AlertRule[] ValidRules() => [
        new("lowStock",true,5,new(true,true,false),"daily"),
        new("outOfStock",true,0,new(true,true,true),"instant"),
        new("syncFailure",true,5,new(true,true,true),"instant"),
        new("oversellingRisk",true,3,new(false,true,true),"instant") ];

    [Fact]
    public void AcceptsMockupAlertThresholds()
    {
        Assert.Null(ReportValidation.Alerts(new AlertSettingsInput(ValidRules())));
    }

    [Fact]
    public void RejectsMissingOrDuplicateKeysAndNonzeroOutOfStockThreshold()
    {
        var valid = ValidRules();
        Assert.NotNull(ReportValidation.Alerts(new AlertSettingsInput(valid.Take(3).ToArray())));
        Assert.NotNull(ReportValidation.Alerts(new AlertSettingsInput([.. valid[..3], valid[0]])));
        Assert.NotNull(ReportValidation.Alerts(new AlertSettingsInput([valid[0], valid[1] with { Threshold = 1 }, .. valid[2..]])));
        Assert.NotNull(ReportValidation.Alerts(new AlertSettingsInput([valid[0], valid[1] with { Threshold = null }, .. valid[2..]])));
    }

    [Fact]
    public void RequiresPositiveSyncDurationAndOversellingThreshold()
    {
        var valid = ValidRules();
        Assert.NotNull(ReportValidation.Alerts(new AlertSettingsInput([valid[0], valid[1], valid[2] with { Threshold = 0 }, valid[3]])));
        Assert.NotNull(ReportValidation.Alerts(new AlertSettingsInput([valid[0], valid[1], valid[2], valid[3] with { Threshold = 0 }])));
    }

    [Fact]
    public void RejectsNegativeLowStockThresholdAndUnknownFrequency()
    {
        var valid = ValidRules();
        Assert.NotNull(ReportValidation.Alerts(new AlertSettingsInput([valid[0] with { Threshold = -1 }, .. valid[1..]])));
        Assert.NotNull(ReportValidation.Alerts(new AlertSettingsInput([valid[0] with { Frequency = "weekly" }, .. valid[1..]])));
    }

    [Fact]
    public void RestockUsesReservedAdjustedAvailabilityAndFourteenDaySalesVelocity()
    {
        var available = ReportMath.AvailableUnits(onHand: 12,reserved: 5);
        var metrics = ReportMath.RestockMetrics(available,unitsSoldLast14Days:14);

        Assert.Equal(7,available);
        Assert.Equal(1,metrics.AverageUnitsPerDay);
        Assert.Equal(7,metrics.DaysUntilStockout);
        Assert.Equal(7,metrics.SuggestedUnits);
    }

    [Fact]
    public void RestockAvailabilityCannotFallBelowZero()
    {
        Assert.Equal(0,ReportMath.AvailableUnits(onHand: 2,reserved: 5));
    }

    [Fact]
    public void RevenueChangeUsesPreviousWeekAndIsNullWhenPriorWeekHasNoRevenue()
    {
        Assert.Equal(8.2m,ReportMath.RevenueChangePercent(currentRevenue:108.2m,previousRevenue:100m));
        Assert.Null(ReportMath.RevenueChangePercent(currentRevenue:50m,previousRevenue:0m));
    }

    [Fact]
    public void ProductRankingsUseUnitsAndIncludeZeroSalesInBottomFive()
    {
        var products = Enumerable.Range(0,7).Select(i => new ProductSales(Guid.NewGuid(),$"Product {i}",$"SKU{i}",i,0m)).ToArray();

        Assert.Equal(6,ReportMath.TopProducts(products)[0].UnitCount);
        var bottom = ReportMath.BottomProducts(products);
        Assert.Equal(0,bottom[0].UnitCount);
        Assert.Equal(5,bottom.Count);
        Assert.Equal(new[] { 0,1,2,3,4 },bottom.Select(p => p.UnitCount));
    }
}
