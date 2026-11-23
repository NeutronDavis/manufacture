using Manufacture.Models.Domain;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests;

public class ProductionDashboardMetricsTests
{
    [Fact]
    public void Build_GroupsOutputByProductTypeAndUnit()
    {
        var date = new DateTime(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);
        var batches = new[]
        {
            Batch("Bread", "Family Loaf", "loaves", BatchStatus.Completed, 100, 105, 2500m),
            Batch("Snacks", "Meat Pie", "packs", BatchStatus.Completed, 200, 190, 1800m),
            Batch("Water", "Dispenser Bottle", "bottles", BatchStatus.Baking, 300, 0, 900m)
        };

        var result = ProductionDashboardMetrics.Build(date, batches, defectQuantity: 4);

        Assert.Equal(3, result.TotalBatches);
        Assert.Equal(2, result.CompletedBatches);
        Assert.Equal(1, result.ActiveBatches);
        Assert.Equal(0, result.PlannedBatches);
        Assert.Equal(4, result.DefectQuantity);
        Assert.Equal(5200m, result.ProductionCost);
        Assert.Equal(600m, result.TotalTargetUnits);
        Assert.Equal(295m, result.TotalActualUnits);
        Assert.Equal(49.2m, result.YieldAchievementPercent);
        Assert.Equal(3, result.OutputGroups.Count);
        Assert.Equal(new[] { "loaves", "packs", "bottles" }, result.OutputGroups.Select(g => g.OutputUnit).ToArray());
    }

    [Fact]
    public void Build_ExcludesCancelledBatchesFromEveryTotal()
    {
        var date = new DateTime(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);
        var batches = new[]
        {
            Batch("Bread", "Family Loaf", "loaves", BatchStatus.Completed, 100, 100, 2500m),
            Batch("Bread", "Cancelled Run", "loaves", BatchStatus.Cancelled, 50, 50, 999m)
        };

        var result = ProductionDashboardMetrics.Build(date, batches, defectQuantity: 0);

        Assert.Equal(1, result.TotalBatches);
        Assert.Single(result.OutputGroups);
        Assert.Equal(100m, result.TotalTargetUnits);
        Assert.Equal(100m, result.TotalActualUnits);
        Assert.Equal(2500m, result.ProductionCost);
        Assert.Equal(100m, result.YieldAchievementPercent);
    }

    [Fact]
    public void Build_HandlesEmptyDayWithoutDividingByZero()
    {
        var result = ProductionDashboardMetrics.Build(
            new DateTime(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc),
            Array.Empty<ProductionBatch>(),
            defectQuantity: 0);

        Assert.Equal(0, result.TotalBatches);
        Assert.Empty(result.OutputGroups);
        Assert.Empty(result.Batches);
        Assert.Equal(0m, result.YieldAchievementPercent);
        Assert.Equal(0m, result.ProductionCost);
    }

    private static ProductionBatch Batch(
        string category,
        string productType,
        string outputUnit,
        BatchStatus status,
        int target,
        int actual,
        decimal cost)
    {
        return new ProductionBatch
        {
            Id = Random.Shared.Next(1, 1_000_000),
            CategoryName = category,
            ProductTypeName = productType,
            OutputUnitLabel = outputUnit,
            Status = status,
            TargetQuantity = target,
            ActualQuantity = actual,
            TotalBatchCost = cost,
            ProductionDate = new DateTime(2026, 11, 13, 8, 0, 0, DateTimeKind.Utc)
        };
    }
}
