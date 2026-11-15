using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    /// <summary>
    /// Pure aggregation for the Production Manager dashboard. Kept free of EF
    /// dependencies so the KPI and per-unit output rules can be unit tested.
    /// </summary>
    public static class ProductionDashboardMetrics
    {
        public static ProductionDashboardDto Build(
            DateTime date,
            IEnumerable<ProductionBatch> batches,
            int defectQuantity)
        {
            var rows = (batches ?? Enumerable.Empty<ProductionBatch>())
                .Where(batch => batch.Status != BatchStatus.Cancelled)
                .OrderByDescending(batch => batch.ProductionDate)
                .ThenByDescending(batch => batch.Id)
                .ToList();

            var totalTarget = rows.Sum(batch => (decimal)batch.TargetQuantity);
            var totalActual = rows.Sum(batch => (decimal)batch.ActualQuantity);

            var outputGroups = rows
                .GroupBy(batch => new
                {
                    Category = string.IsNullOrWhiteSpace(batch.CategoryName)
                        ? "Uncategorised"
                        : batch.CategoryName,
                    ProductType = string.IsNullOrWhiteSpace(batch.ProductTypeName)
                        ? "Uncategorised"
                        : batch.ProductTypeName,
                    Unit = string.IsNullOrWhiteSpace(batch.OutputUnitLabel)
                        ? "units"
                        : batch.OutputUnitLabel
                })
                .Select(group => new ProductionOutputGroupDto
                {
                    CategoryName = group.Key.Category,
                    ProductTypeName = group.Key.ProductType,
                    OutputUnit = group.Key.Unit,
                    BatchCount = group.Count(),
                    TargetQuantity = group.Sum(batch => (decimal)batch.TargetQuantity),
                    ActualQuantity = group.Sum(batch => (decimal)batch.ActualQuantity)
                })
                .OrderBy(group => group.CategoryName)
                .ThenBy(group => group.ProductTypeName)
                .ThenBy(group => group.OutputUnit)
                .ToList();

            return new ProductionDashboardDto
            {
                Date = date.Date,
                TotalBatches = rows.Count,
                CompletedBatches = rows.Count(batch => batch.Status == BatchStatus.Completed),
                ActiveBatches = rows.Count(batch =>
                    batch.Status == BatchStatus.InMixing || batch.Status == BatchStatus.Baking),
                PlannedBatches = rows.Count(batch => batch.Status == BatchStatus.Planned),
                DefectQuantity = Math.Max(0, defectQuantity),
                ProductionCost = rows.Sum(batch => batch.TotalBatchCost),
                TotalTargetUnits = totalTarget,
                TotalActualUnits = totalActual,
                YieldAchievementPercent = totalTarget > 0m
                    ? Math.Round((totalActual / totalTarget) * 100m, 1)
                    : 0m,
                OutputGroups = outputGroups,
                Batches = rows
            };
        }
    }
}
