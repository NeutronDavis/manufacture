using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    // ---------------------------------------------------------------------
    // Flattened source rows. They are deliberately simple settable classes so
    // both EF projections and unit tests can populate them.
    // ---------------------------------------------------------------------

    public sealed class OrderDetailSource
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int RepId { get; set; }
        public string RepName { get; set; } = "Unassigned Rep";
        public string Route { get; set; } = string.Empty;
        public string Terminal { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = "Uncategorised";
        public string Unit { get; set; } = "units";
        public int Quantity { get; set; }
        public int BonusQuantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class BatchDetailSource
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = "Uncategorised";
        public string Unit { get; set; } = "units";
        public int TargetQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string Baker { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class DispatchLineSource
    {
        public int RouteSheetId { get; set; }
        public string RouteSheetRef { get; set; } = string.Empty;
        public DateOnly RouteDate { get; set; }
        public int Status { get; set; }
        public int RepId { get; set; }
        public string RepName { get; set; } = "Unassigned Rep";
        public string Route { get; set; } = string.Empty;
        public string Vehicle { get; set; } = string.Empty;
        public string Terminal { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = "Uncategorised";
        public string Unit { get; set; } = "units";
        public int RequestedQty { get; set; }
        public int LoadedQty { get; set; }
        public int SoldQty { get; set; }
        public int ClosingStockQty { get; set; }
        public int DamagedQty { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class ReturnDetailSource
    {
        public int ReturnId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int RepId { get; set; }
        public string RepName { get; set; } = "Unassigned Rep";
        public string Route { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = "Uncategorised";
        public string Unit { get; set; } = "units";
        public int Quantity { get; set; }
        public int ReplacementQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? RefundAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool ReplacementIssued { get; set; }
    }

    public sealed class DefectDetailSource
    {
        public int DefectId { get; set; }
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = "Uncategorised";
        public string Unit { get; set; } = "units";
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pure shaping of source records for the SuperAdmin KPI record drawer.
    /// Grouping, totals, ordering, blank actual values and the row cap live here
    /// so they can be tested without EF or a database.
    /// </summary>
    public static class SuperAdminKpiDetailBuilder
    {
        /// <summary>Maximum rendered rows; totals are always computed first.</summary>
        public const int MaxRowsPerDetail = 1000;

        private const string UnassignedRep = "Unassigned Rep";

        // -----------------------------------------------------------------
        // Orders Placed
        // -----------------------------------------------------------------

        public static KpiDetailDto BuildOrders(
            IEnumerable<OrderDetailSource> lines,
            string periodKey,
            string dateLabel,
            string scopeLabel)
        {
            var rows = (lines ?? Enumerable.Empty<OrderDetailSource>()).ToList();

            var groups = rows
                .GroupBy(line => new { line.RepId, line.RepName, line.Route, line.Terminal })
                .Select(group =>
                {
                    var groupLines = group.ToList();
                    var orderRows = groupLines
                        .GroupBy(line => line.OrderId)
                        .Select(orderGroup =>
                        {
                            var orderLines = orderGroup.ToList();
                            var first = orderLines[0];
                            var quantity = orderLines.Sum(line => line.Quantity);
                            var bonus = orderLines.Sum(line => line.BonusQuantity);
                            var amount = orderLines.Sum(line => line.Quantity * line.UnitPrice);
                            var products = string.Join(", ", orderLines.Select(line => $"{line.ProductName} \u00d7{line.Quantity}"));
                            var meta = string.Join(" \u2022 ", new[] { first.CustomerName, first.OrderType }
                                .Where(value => !string.IsNullOrWhiteSpace(value)));

                            return new
                            {
                                Date = orderLines.Max(line => line.OrderDate),
                                Row = new KpiDetailRowDto
                                {
                                    Reference = first.OrderNumber,
                                    ReferenceUrl = $"/Sales/Orders/Details?id={first.OrderId}",
                                    Product = products,
                                    Category = string.Join(", ", orderLines
                                        .Select(line => line.Category)
                                        .Distinct(StringComparer.OrdinalIgnoreCase)),
                                    UnitLabel = ResolveUnit(orderLines.Select(line => line.Unit)),
                                    Quantity = quantity,
                                    SecondaryQuantity = bonus > 0 ? bonus : null,
                                    Amount = amount,
                                    Status = first.Status,
                                    StatusTone = StatusTone(first.Status),
                                    Meta = meta,
                                    DateLabel = first.OrderDate.ToString("dd MMM yyyy, HH:mm")
                                }
                            };
                        })
                        .OrderByDescending(item => item.Date)
                        .ThenBy(item => item.Row.Reference)
                        .Select(item => item.Row)
                        .ToList();

                    return new KpiDetailGroupDto
                    {
                        Key = $"rep-{group.Key.RepId}",
                        Label = string.IsNullOrWhiteSpace(group.Key.RepName) ? UnassignedRep : group.Key.RepName,
                        SubLabel = JoinMeta(group.Key.Route, group.Key.Terminal),
                        Unit = ResolveUnit(groupLines.Select(line => line.Unit)),
                        Total = groupLines.Sum(line => line.Quantity),
                        TotalValue = groupLines.Sum(line => line.Quantity * line.UnitPrice),
                        RecordCount = groupLines.Select(line => line.OrderId).Distinct().Count(),
                        Rows = orderRows
                    };
                })
                .OrderBy(group => group.Label == UnassignedRep ? 1 : 0)
                .ThenByDescending(group => group.Total)
                .ThenBy(group => group.Label)
                .ToList();

            return Finalize(new KpiDetailDto
            {
                Metric = LifecycleMetric.OrdersPlaced.ToString(),
                MetricLabel = SuperAdminDashboardAggregator.MetricDisplayName(LifecycleMetric.OrdersPlaced),
                PeriodKey = periodKey,
                DateLabel = dateLabel,
                ScopeLabel = scopeLabel,
                GroupingLabel = "By Sales Rep",
                Note = "Sales orders booked in the register for this period.",
                Unit = ResolveUnit(rows.Select(line => line.Unit))
            }, groups);
        }

        // -----------------------------------------------------------------
        // Produced
        // -----------------------------------------------------------------

        public static KpiDetailDto BuildProduced(
            IEnumerable<BatchDetailSource> batches,
            string periodKey,
            string dateLabel,
            string scopeLabel)
        {
            var rows = (batches ?? Enumerable.Empty<BatchDetailSource>()).ToList();

            var groups = rows
                .GroupBy(batch => new { batch.CategoryId, batch.Category })
                .Select(group =>
                {
                    var groupBatches = group.ToList();
                    var detailRows = groupBatches
                        .OrderBy(batch => batch.ProductName)
                        .ThenBy(batch => batch.BatchNumber)
                        .Select(batch => new KpiDetailRowDto
                        {
                            Reference = batch.BatchNumber,
                            ReferenceUrl = $"/Production/Batches/Details?id={batch.BatchId}",
                            Product = batch.ProductName,
                            Category = batch.Category,
                            UnitLabel = batch.Unit,
                            // No recorded output stays blank; never fabricate a zero.
                            Quantity = batch.ActualQuantity > 0 ? batch.ActualQuantity : null,
                            SecondaryQuantity = batch.TargetQuantity,
                            Amount = batch.ActualQuantity > 0 ? batch.ActualQuantity * batch.UnitPrice : null,
                            Status = batch.Status,
                            StatusTone = StatusTone(batch.Status),
                            Meta = JoinMeta(batch.Shift, batch.Baker),
                            DateLabel = batch.Date.ToString("dd MMM yyyy")
                        })
                        .ToList();

                    return new KpiDetailGroupDto
                    {
                        Key = $"category-{group.Key.CategoryId}",
                        Label = group.Key.Category,
                        SubLabel = "Plant production",
                        Unit = ResolveUnit(groupBatches.Select(batch => batch.Unit)),
                        Total = groupBatches.Sum(batch => Math.Max(0, batch.ActualQuantity)),
                        TotalValue = groupBatches.Sum(batch => Math.Max(0, batch.ActualQuantity) * batch.UnitPrice),
                        RecordCount = groupBatches.Count,
                        Rows = detailRows
                    };
                })
                .OrderBy(group => group.Label == "Uncategorised" ? 1 : 0)
                .ThenBy(group => group.Label)
                .ToList();

            return Finalize(new KpiDetailDto
            {
                Metric = LifecycleMetric.Produced.ToString(),
                MetricLabel = SuperAdminDashboardAggregator.MetricDisplayName(LifecycleMetric.Produced),
                PeriodKey = periodKey,
                DateLabel = dateLabel,
                ScopeLabel = scopeLabel,
                GroupingLabel = "By Product Category",
                Note = "Actual batch output recorded by production. Blank means no actual value was captured.",
                Unit = ResolveUnit(rows.Select(batch => batch.Unit))
            }, groups);
        }

        // -----------------------------------------------------------------
        // Loaded / Sold / Closing Stock / Damaged
        // -----------------------------------------------------------------

        public static KpiDetailDto BuildDispatch(
            IEnumerable<DispatchLineSource> lines,
            LifecycleMetric metric,
            string periodKey,
            string dateLabel,
            string scopeLabel)
        {
            var minStatus = metric == LifecycleMetric.Loaded
                ? (int)RouteSheetStatus.LoadedOnRoute
                : (int)RouteSheetStatus.Reconciled;

            var rows = (lines ?? Enumerable.Empty<DispatchLineSource>())
                .Where(line => line.Status >= minStatus)
                .Select(line => new { Line = line, Quantity = DispatchQuantity(line, metric) })
                .Where(item => item.Quantity.HasValue && item.Quantity.Value > 0)
                .ToList();

            var groups = rows
                .GroupBy(item => new
                {
                    item.Line.RepId,
                    item.Line.RepName,
                    item.Line.Route,
                    item.Line.Vehicle,
                    item.Line.Terminal
                })
                .Select(group =>
                {
                    var groupRows = group.ToList();
                    var detailRows = groupRows
                        .OrderByDescending(item => item.Line.RouteDate)
                        .ThenBy(item => item.Line.ProductName)
                        .Select(item =>
                        {
                            var quantity = item.Quantity!.Value;
                            return new KpiDetailRowDto
                            {
                                Reference = item.Line.RouteSheetRef,
                                ReferenceUrl = $"/Sales/Reconciliation?sheetId={item.Line.RouteSheetId}",
                                Product = item.Line.ProductName,
                                Category = item.Line.Category,
                                UnitLabel = item.Line.Unit,
                                Quantity = quantity,
                                SecondaryQuantity = SecondaryQuantity(item.Line, metric),
                                UnitPrice = item.Line.UnitPrice,
                                Amount = AmountOf(item.Line, metric, quantity),
                                Status = SheetStatusLabel(item.Line.Status),
                                StatusTone = StatusTone(SheetStatusLabel(item.Line.Status)),
                                Meta = string.Empty,
                                DateLabel = item.Line.RouteDate.ToString("dd MMM yyyy")
                            };
                        })
                        .ToList();

                    return new KpiDetailGroupDto
                    {
                        Key = $"rep-{group.Key.RepId}",
                        Label = string.IsNullOrWhiteSpace(group.Key.RepName) ? UnassignedRep : group.Key.RepName,
                        SubLabel = JoinMeta(group.Key.Route, group.Key.Vehicle, group.Key.Terminal),
                        Unit = ResolveUnit(groupRows.Select(item => item.Line.Unit)),
                        Total = groupRows.Sum(item => item.Quantity!.Value),
                        TotalValue = groupRows.Sum(item => AmountOf(item.Line, metric, item.Quantity!.Value) ?? 0m),
                        RecordCount = groupRows.Count,
                        Rows = detailRows
                    };
                })
                .OrderBy(group => group.Label == UnassignedRep ? 1 : 0)
                .ThenByDescending(group => group.Total)
                .ThenBy(group => group.Label)
                .ToList();

            return Finalize(new KpiDetailDto
            {
                Metric = metric.ToString(),
                MetricLabel = SuperAdminDashboardAggregator.MetricDisplayName(metric),
                PeriodKey = periodKey,
                DateLabel = dateLabel,
                ScopeLabel = scopeLabel,
                GroupingLabel = "By Sales Rep",
                Note = DispatchNote(metric),
                Unit = ResolveUnit(rows.Select(item => item.Line.Unit))
            }, groups);
        }

        // -----------------------------------------------------------------
        // Factory Defects
        // -----------------------------------------------------------------

        public static KpiDetailDto BuildDefects(
            IEnumerable<DefectDetailSource> defects,
            string periodKey,
            string dateLabel,
            string scopeLabel)
        {
            var rows = (defects ?? Enumerable.Empty<DefectDetailSource>()).ToList();

            var groups = rows
                .GroupBy(defect => new { defect.CategoryId, defect.Category })
                .Select(group =>
                {
                    var groupDefects = group.ToList();
                    var detailRows = groupDefects
                        .OrderBy(defect => defect.ProductName)
                        .ThenBy(defect => defect.BatchNumber)
                        .Select(defect => new KpiDetailRowDto
                        {
                            Reference = defect.BatchNumber,
                            ReferenceUrl = $"/Production/Batches/Details?id={defect.BatchId}",
                            Product = defect.ProductName,
                            Category = defect.Category,
                            UnitLabel = defect.Unit,
                            Quantity = defect.Quantity,
                            Status = "Recorded",
                            StatusTone = "warning",
                            Meta = JoinMeta(defect.Reason, defect.RecordedBy),
                            DateLabel = defect.RecordedAt.ToString("dd MMM yyyy")
                        })
                        .ToList();

                    return new KpiDetailGroupDto
                    {
                        Key = $"category-{group.Key.CategoryId}",
                        Label = group.Key.Category,
                        SubLabel = "Plant production",
                        Unit = ResolveUnit(groupDefects.Select(defect => defect.Unit)),
                        Total = groupDefects.Sum(defect => defect.Quantity),
                        RecordCount = groupDefects.Count,
                        Rows = detailRows
                    };
                })
                .OrderBy(group => group.Label == "Uncategorised" ? 1 : 0)
                .ThenBy(group => group.Label)
                .ToList();

            return Finalize(new KpiDetailDto
            {
                Metric = LifecycleMetric.FactoryDefects.ToString(),
                MetricLabel = SuperAdminDashboardAggregator.MetricDisplayName(LifecycleMetric.FactoryDefects),
                PeriodKey = periodKey,
                DateLabel = dateLabel,
                ScopeLabel = scopeLabel,
                GroupingLabel = "By Product Category",
                Note = "Production losses identified before loading.",
                Unit = ResolveUnit(rows.Select(defect => defect.Unit))
            }, groups);
        }

        // -----------------------------------------------------------------
        // Customer Returns
        // -----------------------------------------------------------------

        public static KpiDetailDto BuildReturns(
            IEnumerable<ReturnDetailSource> returns,
            string periodKey,
            string dateLabel,
            string scopeLabel)
        {
            var rows = (returns ?? Enumerable.Empty<ReturnDetailSource>()).ToList();

            var groups = rows
                .GroupBy(item => new { item.RepId, item.RepName, item.Route })
                .Select(group =>
                {
                    var groupReturns = group.ToList();
                    var detailRows = groupReturns
                        .OrderByDescending(item => item.ReturnDate)
                        .ThenBy(item => item.ReturnNumber)
                        .Select(item => new KpiDetailRowDto
                        {
                            Reference = item.ReturnNumber,
                            Product = item.ProductName,
                            Category = item.Category,
                            UnitLabel = item.Unit,
                            Quantity = item.Quantity,
                            SecondaryQuantity = item.ReplacementQuantity > 0 ? item.ReplacementQuantity : null,
                            UnitPrice = item.UnitPrice,
                            Amount = item.RefundAmount ?? item.Quantity * item.UnitPrice,
                            Status = item.ReplacementIssued ? "Replaced" : "Recorded",
                            StatusTone = item.ReplacementIssued ? "success" : "warning",
                            Meta = JoinMeta(item.CustomerName, item.Reason),
                            DateLabel = item.ReturnDate.ToString("dd MMM yyyy")
                        })
                        .ToList();

                    return new KpiDetailGroupDto
                    {
                        Key = $"rep-{group.Key.RepId}",
                        Label = string.IsNullOrWhiteSpace(group.Key.RepName) ? UnassignedRep : group.Key.RepName,
                        SubLabel = group.Key.Route,
                        Unit = ResolveUnit(groupReturns.Select(item => item.Unit)),
                        Total = groupReturns.Sum(item => item.Quantity),
                        TotalValue = groupReturns.Sum(item => item.RefundAmount ?? item.Quantity * item.UnitPrice),
                        RecordCount = groupReturns.Count,
                        Rows = detailRows
                    };
                })
                .OrderBy(group => group.Label == UnassignedRep ? 1 : 0)
                .ThenByDescending(group => group.Total)
                .ThenBy(group => group.Label)
                .ToList();

            return Finalize(new KpiDetailDto
            {
                Metric = LifecycleMetric.CustomerReturns.ToString(),
                MetricLabel = SuperAdminDashboardAggregator.MetricDisplayName(LifecycleMetric.CustomerReturns),
                PeriodKey = periodKey,
                DateLabel = dateLabel,
                ScopeLabel = scopeLabel,
                GroupingLabel = "By Sales Rep",
                Note = "Prior-sale customer returns recorded against each rep.",
                Unit = ResolveUnit(rows.Select(item => item.Unit))
            }, groups);
        }

        // -----------------------------------------------------------------
        // Shared helpers
        // -----------------------------------------------------------------

        /// <summary>Route-sheet status label shown in the drawer badge.</summary>
        public static string SheetStatusLabel(int status) => (RouteSheetStatus)status switch
        {
            RouteSheetStatus.Reconciled => "Reconciled",
            RouteSheetStatus.Closed => "Closed",
            RouteSheetStatus.LoadedOnRoute => "Loaded \u00b7 pending reconciliation",
            _ => "Ordered"
        };

        /// <summary>Map a record status onto a daisyUI badge tone.</summary>
        public static string StatusTone(string status) => status switch
        {
            "Reconciled" or "Closed" or "Completed" or "Dispatched" or "Replaced" => "success",
            "Loaded \u00b7 pending reconciliation" or "LoadedOnRoute" or "Baking" or "InMixing"
                or "InProduction" or "Planned" or "ReadyToLoad" or "Confirmed" or "Recorded" => "warning",
            "Cancelled" => "error",
            "Pending" or "Ordered" => "info",
            _ => "ghost"
        };

        private static int? DispatchQuantity(DispatchLineSource line, LifecycleMetric metric) => metric switch
        {
            LifecycleMetric.Loaded => line.LoadedQty,
            LifecycleMetric.Sold => line.SoldQty,
            LifecycleMetric.Returned => line.ClosingStockQty,
            LifecycleMetric.Damaged => line.DamagedQty,
            _ => null
        };

        private static int? SecondaryQuantity(DispatchLineSource line, LifecycleMetric metric) => metric switch
        {
            LifecycleMetric.Loaded => line.RequestedQty,
            LifecycleMetric.Returned => line.LoadedQty,
            LifecycleMetric.Damaged => line.LoadedQty,
            _ => null
        };

        private static decimal? AmountOf(DispatchLineSource line, LifecycleMetric metric, int quantity) => metric switch
        {
            LifecycleMetric.Loaded or LifecycleMetric.Sold or LifecycleMetric.Damaged => quantity * line.UnitPrice,
            _ => null
        };

        private static string DispatchNote(LifecycleMetric metric) => metric switch
        {
            LifecycleMetric.Loaded => "Physical stock released from the loading bay for signed-off dispatches.",
            LifecycleMetric.Sold => "Verified route sales reported on reconciled dispatches.",
            LifecycleMetric.Returned => "Good unsold stock returned to the rep and carried into tomorrow.",
            LifecycleMetric.Damaged => "Loaded stock spoiled before sale and written off.",
            _ => "Route-sheet records for the selected stage."
        };

        private static KpiDetailDto Finalize(KpiDetailDto detail, List<KpiDetailGroupDto> groups)
        {
            detail.Groups = groups;
            detail.Total = groups.Sum(group => group.Total);
            detail.TotalValue = groups.Sum(group => group.TotalValue);
            detail.RecordCount = groups.Sum(group => group.RecordCount);
            detail.Unit = ResolveUnit(groups.SelectMany(group => group.Rows).Select(row => row.UnitLabel));
            ApplyRowCap(detail);
            return detail;
        }

        /// <summary>
        /// Keeps the payload light for very wide windows. Group and grand totals
        /// were already computed, so the header still tells the truth.
        /// </summary>
        private static void ApplyRowCap(KpiDetailDto detail)
        {
            var totalRows = detail.Groups.Sum(group => group.Rows.Count);
            if (totalRows <= MaxRowsPerDetail) return;

            detail.IsTruncated = true;
            var remaining = MaxRowsPerDetail;

            foreach (var group in detail.Groups)
            {
                if (remaining <= 0)
                {
                    group.Rows = new List<KpiDetailRowDto>();
                    continue;
                }

                if (group.Rows.Count > remaining)
                {
                    group.Rows = group.Rows.Take(remaining).ToList();
                    remaining = 0;
                }
                else
                {
                    remaining -= group.Rows.Count;
                }
            }

            detail.Groups = detail.Groups.Where(group => group.Rows.Count > 0).ToList();
        }

        private static string ResolveUnit(IEnumerable<string> units)
        {
            var distinct = (units ?? Enumerable.Empty<string>())
                .Where(unit => !string.IsNullOrWhiteSpace(unit))
                .Select(unit => unit.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return distinct.Count == 1 ? distinct[0] : "units";
        }

        private static string JoinMeta(params string?[] values) =>
            string.Join(" \u2022 ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
