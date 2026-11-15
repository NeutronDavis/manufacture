using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    /// <summary>
    /// One operational fact used to build the executive dashboard. Route-sheet
    /// rows are rep-attributed; production and defect rows are plant-level.
    /// Financial-only rows carry one route sheet's tender totals exactly once.
    /// </summary>
    public sealed class DashboardFact
    {
        public DateTime Date { get; set; }
        public int RepId { get; set; }
        public string RepName { get; set; } = "Unassigned Rep";
        public string Route { get; set; } = string.Empty;
        public string VehicleRegistration { get; set; } = string.Empty;
        public string Terminal { get; set; } = string.Empty;
        public int RecipeId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ProductionCategoryId { get; set; }
        public string ProductionCategoryName { get; set; } = "Uncategorised";
        public string OutputUnit { get; set; } = "units";
        public decimal UnitPrice { get; set; }
        public int OrdersPlaced { get; set; }
        public int Produced { get; set; }
        public int Loaded { get; set; }
        public int FactoryDefects { get; set; }
        public int Sold { get; set; }
        public int ClosingStock { get; set; }
        public int Damaged { get; set; }
        public int CustomerReturns { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal Cash { get; set; }
        public decimal Transfer { get; set; }
        public decimal PosBank { get; set; }
        public decimal Outstanding { get; set; }
        public bool IsPlantLevel { get; set; }
        public bool IsFinanceOnly { get; set; }
    }

    public sealed class DashboardCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Uncategorised";
        public string ScopeKey { get; set; } = "category-0";
        public ProductType Line { get; set; } = ProductType.Bread;
        public string IconClass { get; set; } = "fa-solid fa-box";
    }

    public sealed class DashboardSku
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ProductionCategoryId { get; set; }
        public string ProductionCategoryName { get; set; } = "Uncategorised";
        public string ScopeKey { get; set; } = "category-0";
        public ProductType Line { get; set; } = ProductType.Bread;
        public string OutputUnit { get; set; } = "units";
        public decimal SellingPrice { get; set; }
    }

    /// <summary>
    /// Pure aggregation for the database-backed executive dashboard. Keeping
    /// this separate from EF makes the period, funnel, product and rep rules
    /// directly testable.
    /// </summary>
    public static class SuperAdminDashboardAggregator
    {
        private const string AllScope = "all";

        public static ExecutiveOverviewDto Build(
            DateTime anchor,
            DateTime today,
            IReadOnlyList<DashboardFact> facts,
            IReadOnlyList<DashboardCategory> categories,
            IReadOnlyList<DashboardSku> skus)
        {
            var anchorDate = anchor.Date;
            var todayDate = today.Date;
            if (anchorDate > todayDate) anchorDate = todayDate;

            var specs = PeriodSpecs(anchorDate, todayDate);
            var safeFacts = facts ?? Array.Empty<DashboardFact>();
            var safeCategories = categories ?? Array.Empty<DashboardCategory>();
            var safeSkus = skus ?? Array.Empty<DashboardSku>();

            var overview = new ExecutiveOverviewDto
            {
                GeneratedAt = DateTime.UtcNow,
                AnchorDate = anchorDate,
                AnchorDateLabel = anchorDate.ToString("dddd, dd MMMM yyyy")
            };

            var allRepScope = new RepDrillDownScopeDto
            {
                ScopeKey = AllScope,
                ScopeLabel = "All Product Lines"
            };
            var allProductScope = new ProductDrillDownScopeDto
            {
                ScopeKey = AllScope,
                ScopeLabel = "All Product Lines"
            };
            var allStageScope = new StageDrillDownScopeDto
            {
                ScopeKey = AllScope,
                ScopeLabel = "All Product Lines"
            };

            var categoryRepScopes = safeCategories.ToDictionary(
                category => category.Id,
                category => new RepDrillDownScopeDto
                {
                    ScopeKey = category.ScopeKey,
                    ScopeLabel = category.Name
                });
            var categoryProductScopes = safeCategories.ToDictionary(
                category => category.Id,
                category => new ProductDrillDownScopeDto
                {
                    ScopeKey = category.ScopeKey,
                    ScopeLabel = category.Name
                });
            var categoryStageScopes = safeCategories.ToDictionary(
                category => category.Id,
                category => new StageDrillDownScopeDto
                {
                    ScopeKey = category.ScopeKey,
                    ScopeLabel = category.Name
                });

            foreach (var spec in specs)
            {
                var periodFacts = safeFacts
                    .Where(fact => fact.Date.Date >= spec.Start && fact.Date.Date <= spec.EffectiveEnd)
                    .ToList();

                var totals = BuildTotals(periodFacts);
                var lines = BuildProductLines(periodFacts, safeCategories, safeSkus);
                var tender = BuildTender(periodFacts);
                var repRows = BuildRepRows(periodFacts);
                var alerts = BuildAlerts(totals, lines, tender);

                overview.Periods.Add(new KpiPeriodCardDto
                {
                    Key = spec.Key,
                    Label = spec.Label,
                    DateLabel = spec.DateLabel,
                    ReportsTimeframe = spec.ReportsTimeframe,
                    RepCount = repRows.Count(row => row.RepId > 0),
                    Totals = totals
                });

                overview.ProductLinesByPeriod[spec.Key] = lines;
                overview.TenderByPeriod[spec.Key] = tender;
                overview.AlertsByPeriod[spec.Key] = alerts;

                AddRepPeriod(allRepScope, spec.Key, periodFacts);
                AddProductPeriod(allProductScope, spec.Key, periodFacts);
                AddStagePeriods(allStageScope, spec.Key, periodFacts, categoryId: null);

                foreach (var category in safeCategories)
                {
                    var categoryFacts = periodFacts
                        .Where(fact => fact.ProductionCategoryId == category.Id)
                        .ToList();

                    AddRepPeriod(categoryRepScopes[category.Id], spec.Key, categoryFacts);
                    AddProductPeriod(categoryProductScopes[category.Id], spec.Key, categoryFacts);
                    AddStagePeriods(categoryStageScopes[category.Id], spec.Key, categoryFacts, category.Id);
                }
            }

            overview.DrillDownScopes.Add(allRepScope);
            overview.ProductDrillDownScopes.Add(allProductScope);
            overview.StageDrillDownScopes.Add(allStageScope);

            // Categories with no facts still exist so a new Snacks/Drinks
            // category is visible before its first production run.
            foreach (var category in safeCategories)
            {
                overview.DrillDownScopes.Add(categoryRepScopes[category.Id]);
                overview.ProductDrillDownScopes.Add(categoryProductScopes[category.Id]);
                overview.StageDrillDownScopes.Add(categoryStageScopes[category.Id]);
            }

            var defaultSpec = specs[0];
            overview.Today = overview.Periods[0].Totals;
            overview.ProductLines = overview.ProductLinesByPeriod[defaultSpec.Key];
            overview.Tender = overview.TenderByPeriod[defaultSpec.Key];
            overview.Alerts = overview.AlertsByPeriod[defaultSpec.Key];
            overview.RepToday = overview.DrillDownScopes
                .First(scope => scope.ScopeKey == AllScope)
                .Periods[defaultSpec.Key];

            return overview;
        }

        public static LifecycleTotalsDto BuildTotals(IEnumerable<DashboardFact> facts)
        {
            var rows = facts?.ToList() ?? new List<DashboardFact>();
            return new LifecycleTotalsDto
            {
                OrdersPlaced = rows.Sum(row => row.OrdersPlaced),
                Produced = rows.Sum(row => row.Produced),
                Loaded = rows.Sum(row => row.Loaded),
                FactoryDefects = rows.Sum(row => row.FactoryDefects),
                Sold = rows.Sum(row => row.Sold),
                Returned = rows.Sum(row => row.ClosingStock),
                Damaged = rows.Sum(row => row.Damaged),
                CustomerReturns = rows.Sum(row => row.CustomerReturns),
                ExpectedRevenue = rows.Sum(row => row.ExpectedRevenue),
                Collected = rows.Sum(row => row.Cash + row.Transfer + row.PosBank),
                OutstandingBalance = rows.Sum(row => row.Outstanding)
            };
        }

        public static string MetricDisplayName(LifecycleMetric metric) => metric switch
        {
            LifecycleMetric.OrdersPlaced => "Orders Placed",
            LifecycleMetric.Produced => "Produced",
            LifecycleMetric.Loaded => "Loaded",
            LifecycleMetric.FactoryDefects => "Factory Defects",
            LifecycleMetric.Sold => "Sold",
            LifecycleMetric.Returned => "Closing Stock",
            LifecycleMetric.Damaged => "Damaged",
            LifecycleMetric.CustomerReturns => "Returns",
            _ => metric.ToString()
        };

        private static List<ProductLineBreakdownDto> BuildProductLines(
            IReadOnlyList<DashboardFact> facts,
            IReadOnlyList<DashboardCategory> categories,
            IReadOnlyList<DashboardSku> skus)
        {
            var lines = new List<ProductLineBreakdownDto>();
            var stageFacts = facts.Where(fact => !fact.IsFinanceOnly).ToList();
            var totalSold = stageFacts.Sum(fact => fact.Sold);

            foreach (var category in categories)
            {
                var categoryFacts = stageFacts.Where(fact => fact.ProductionCategoryId == category.Id).ToList();
                var line = new ProductLineBreakdownDto
                {
                    ProductionCategoryId = category.Id,
                    ProductionCategoryName = category.Name,
                    ScopeKey = category.ScopeKey,
                    Line = category.Line,
                    LineLabel = category.Name,
                    IconClass = category.IconClass,
                    Totals = BuildTotals(categoryFacts),
                    SkuNames = skus
                        .Where(sku => sku.ProductionCategoryId == category.Id)
                        .Select(sku => sku.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name)
                        .ToList()
                };

                if (line.SkuNames.Count == 0)
                {
                    line.SkuNames = categoryFacts
                        .Where(fact => !string.IsNullOrWhiteSpace(fact.ProductName))
                        .Select(fact => fact.ProductName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name)
                        .ToList();
                }

                line.ShareOfVolumePercent = totalSold > 0
                    ? Math.Round(line.Totals.Sold / (decimal)totalSold * 100m, 1)
                    : 0m;
                lines.Add(line);
            }

            var uncategorised = stageFacts.Where(fact => fact.ProductionCategoryId == 0).ToList();
            if (uncategorised.Count > 0)
            {
                lines.Add(new ProductLineBreakdownDto
                {
                    ProductionCategoryId = 0,
                    ProductionCategoryName = "Uncategorised",
                    ScopeKey = "category-0",
                    Line = ProductType.Bread,
                    LineLabel = "Uncategorised",
                    IconClass = "fa-solid fa-box",
                    Totals = BuildTotals(uncategorised),
                    SkuNames = uncategorised
                        .Where(fact => !string.IsNullOrWhiteSpace(fact.ProductName))
                        .Select(fact => fact.ProductName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    ShareOfVolumePercent = totalSold > 0
                        ? Math.Round(uncategorised.Sum(fact => fact.Sold) / (decimal)totalSold * 100m, 1)
                        : 0m
                });
            }

            return lines;
        }

        private static TenderSplitDto BuildTender(IEnumerable<DashboardFact> facts)
        {
            var rows = facts?.ToList() ?? new List<DashboardFact>();
            return new TenderSplitDto
            {
                Cash = rows.Sum(row => row.Cash),
                BankTransfer = rows.Sum(row => row.Transfer),
                PosBank = rows.Sum(row => row.PosBank),
                Outstanding = rows.Sum(row => row.Outstanding)
            };
        }

        private static List<RepDrillDownRowDto> BuildRepRows(IReadOnlyList<DashboardFact> facts)
        {
            return facts
                .Where(fact => !fact.IsPlantLevel)
                .GroupBy(fact => new { fact.RepId, fact.RepName, fact.Route, fact.VehicleRegistration, fact.Terminal })
                .Select(group =>
                {
                    var rows = group.ToList();
                    var totals = BuildTotals(rows.Where(row => !row.IsFinanceOnly));
                    var tender = BuildTender(rows);
                    return new RepDrillDownRowDto
                    {
                        RepId = group.Key.RepId,
                        RepName = group.Key.RepName,
                        Route = group.Key.Route,
                        Terminal = group.Key.Terminal,
                        VehicleRegistration = group.Key.VehicleRegistration,
                        OrdersPlaced = totals.OrdersPlaced,
                        Produced = totals.Produced,
                        Loaded = totals.Loaded,
                        Sold = totals.Sold,
                        Returned = totals.Returned,
                        Damaged = totals.Damaged,
                        FactoryDefects = totals.FactoryDefects,
                        CustomerReturns = totals.CustomerReturns,
                        ExpectedRevenue = totals.ExpectedRevenue,
                        Collected = tender.Total,
                        CashCollected = tender.Cash,
                        OutstandingBalance = tender.Outstanding,
                        FillRatePercent = totals.OrdersPlaced > 0
                            ? Math.Round(totals.Loaded / (decimal)totals.OrdersPlaced * 100m, 1)
                            : 0m,
                        SellThroughPercent = totals.Loaded > 0
                            ? Math.Round(totals.Sold / (decimal)totals.Loaded * 100m, 1)
                            : 0m,
                        DamagePercent = totals.Loaded > 0
                            ? Math.Round(totals.Damaged / (decimal)totals.Loaded * 100m, 1)
                            : 0m
                    };
                })
                .OrderByDescending(row => row.Sold)
                .ThenBy(row => row.RepName)
                .ToList();
        }

        private static List<ProductDrillDownRowDto> BuildProductRows(IReadOnlyList<DashboardFact> facts)
        {
            return facts
                .Where(fact => !fact.IsFinanceOnly && fact.RecipeId > 0)
                .GroupBy(fact => fact.RecipeId)
                .Select(group =>
                {
                    var rows = group.ToList();
                    var first = rows.First();
                    var totals = BuildTotals(rows);
                    return new ProductDrillDownRowDto
                    {
                        ProductId = group.Key,
                        ProductCode = first.ProductCode,
                        ProductName = first.ProductName,
                        ProductionCategoryId = first.ProductionCategoryId,
                        ProductionCategoryName = first.ProductionCategoryName,
                        Line = MapLine(first.ProductionCategoryName),
                        LineLabel = first.ProductionCategoryName,
                        OutputUnit = first.OutputUnit,
                        UnitPrice = first.UnitPrice,
                        OrdersPlaced = totals.OrdersPlaced,
                        Produced = totals.Produced,
                        Loaded = totals.Loaded,
                        Sold = totals.Sold,
                        Returned = totals.Returned,
                        Damaged = totals.Damaged,
                        FactoryDefects = totals.FactoryDefects,
                        CustomerReturns = totals.CustomerReturns,
                        ExpectedRevenue = totals.ExpectedRevenue
                    };
                })
                .OrderByDescending(row => row.Sold)
                .ThenBy(row => row.ProductName)
                .ToList();
        }

        private static void AddRepPeriod(
            RepDrillDownScopeDto scope,
            string periodKey,
            IReadOnlyList<DashboardFact> facts)
        {
            scope.Periods[periodKey] = BuildRepRows(facts);
        }

        private static void AddProductPeriod(
            ProductDrillDownScopeDto scope,
            string periodKey,
            IReadOnlyList<DashboardFact> facts)
        {
            scope.Periods[periodKey] = BuildProductRows(facts);
        }

        private static void AddStagePeriods(
            StageDrillDownScopeDto scope,
            string periodKey,
            IReadOnlyList<DashboardFact> facts,
            int? categoryId)
        {
            foreach (LifecycleMetric metric in Enum.GetValues<LifecycleMetric>())
            {
                if (!scope.Metrics.TryGetValue(metric.ToString(), out var byPeriod))
                {
                    byPeriod = new Dictionary<string, StageDrillDownDto>();
                    scope.Metrics[metric.ToString()] = byPeriod;
                }

                byPeriod[periodKey] = BuildStageDrillDown(metric, facts, categoryId);
            }
        }

        private static StageDrillDownDto BuildStageDrillDown(
            LifecycleMetric metric,
            IReadOnlyList<DashboardFact> facts,
            int? categoryId)
        {
            var result = new StageDrillDownDto
            {
                Metric = metric.ToString(),
                MetricLabel = MetricDisplayName(metric),
                GroupingLabel = metric switch
                {
                    LifecycleMetric.Produced or LifecycleMetric.FactoryDefects => "By Product Category",
                    LifecycleMetric.Loaded => "By Sales Rep, Vehicle & Product Category",
                    _ => "By Sales Rep & Product Category"
                },
                GroupingHint = metric is LifecycleMetric.Produced or LifecycleMetric.FactoryDefects
                    ? "Plant-level totals; no sales-rep attribution."
                    : "Operational quantities are grouped by the rep who carried the stock.",
                Note = metric switch
                {
                    LifecycleMetric.OrdersPlaced => "Requested by reps before route departure.",
                    LifecycleMetric.Produced => "Actual batch output recorded by production.",
                    LifecycleMetric.Loaded => "Physical stock released from the loading bay.",
                    LifecycleMetric.FactoryDefects => "Production losses identified before loading.",
                    LifecycleMetric.Sold => "Verified route sales reported on the rep sheet.",
                    LifecycleMetric.Returned => "Good unsold stock returned to the rep and carried into tomorrow.",
                    LifecycleMetric.Damaged => "Loaded stock spoiled before sale and written off.",
                    LifecycleMetric.CustomerReturns => "Prior-sale customer returns replaced with fresh stock.",
                    _ => string.Empty
                }
            };

            var scopedFacts = categoryId.HasValue
                ? facts.Where(fact => fact.ProductionCategoryId == categoryId.Value).ToList()
                : facts.ToList();

            var rows = metric is LifecycleMetric.Produced or LifecycleMetric.FactoryDefects
                ? BuildPlantStageRows(scopedFacts, metric)
                : BuildRepStageRows(scopedFacts, metric);

            result.Total = rows.Sum(row => row.MetricValue);
            foreach (var row in rows)
            {
                row.SharePercent = result.Total > 0
                    ? Math.Round(row.MetricValue / (decimal)result.Total * 100m, 1)
                    : 0m;
            }

            result.Rows = rows
                .OrderByDescending(row => row.MetricValue)
                .ThenBy(row => row.GroupLabel)
                .ThenBy(row => row.CategoryLabel)
                .ToList();

            var units = result.Rows
                .Select(row => row.OutputUnit)
                .Where(unit => !string.IsNullOrWhiteSpace(unit))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            result.OutputUnit = units.Count == 1 ? units[0] : "units";
            return result;
        }

        private static List<StageDrillDownRowDto> BuildPlantStageRows(
            IReadOnlyList<DashboardFact> facts,
            LifecycleMetric metric)
        {
            return facts
                .Where(fact => fact.IsPlantLevel && MetricValue(fact, metric) > 0)
                .GroupBy(fact => fact.ProductionCategoryId)
                .Select(group =>
                {
                    var rows = group.ToList();
                    var first = rows.First();
                    var totals = BuildTotals(rows);
                    return new StageDrillDownRowDto
                    {
                        GroupKey = $"category-{group.Key}",
                        GroupLabel = first.ProductionCategoryName,
                        GroupSubLabel = "Plant production",
                        CategoryLabel = first.ProductionCategoryName,
                        OutputUnit = first.OutputUnit,
                        OrdersPlaced = totals.OrdersPlaced,
                        Produced = totals.Produced,
                        Loaded = totals.Loaded,
                        FactoryDefects = totals.FactoryDefects,
                        Sold = totals.Sold,
                        ClosingStock = totals.ClosingStock,
                        Damaged = totals.Damaged,
                        CustomerReturns = totals.CustomerReturns,
                        ExpectedRevenue = totals.ExpectedRevenue,
                        MetricValue = rows.Sum(row => MetricValue(row, metric))
                    };
                })
                .ToList();
        }

        private static List<StageDrillDownRowDto> BuildRepStageRows(
            IReadOnlyList<DashboardFact> facts,
            LifecycleMetric metric)
        {
            return facts
                .Where(fact => !fact.IsPlantLevel && !fact.IsFinanceOnly && MetricValue(fact, metric) > 0)
                .GroupBy(fact => new { fact.RepId, fact.RepName, fact.Route, fact.VehicleRegistration, fact.Terminal, fact.ProductionCategoryId, fact.ProductionCategoryName })
                .Select(group =>
                {
                    var rows = group.ToList();
                    var first = rows.First();
                    var totals = BuildTotals(rows);
                    return new StageDrillDownRowDto
                    {
                        GroupKey = $"rep-{group.Key.RepId}-category-{group.Key.ProductionCategoryId}",
                        GroupLabel = group.Key.RepName,
                        GroupSubLabel = string.Join(" • ", new[] { group.Key.Route, group.Key.VehicleRegistration }.Where(value => !string.IsNullOrWhiteSpace(value))),
                        CategoryLabel = group.Key.ProductionCategoryName,
                        RepName = group.Key.RepName,
                        Route = group.Key.Route,
                        VehicleRegistration = group.Key.VehicleRegistration,
                        OutputUnit = first.OutputUnit,
                        OrdersPlaced = totals.OrdersPlaced,
                        Produced = totals.Produced,
                        Loaded = totals.Loaded,
                        FactoryDefects = totals.FactoryDefects,
                        Sold = totals.Sold,
                        ClosingStock = totals.ClosingStock,
                        Damaged = totals.Damaged,
                        CustomerReturns = totals.CustomerReturns,
                        ExpectedRevenue = totals.ExpectedRevenue,
                        MetricValue = rows.Sum(row => MetricValue(row, metric))
                    };
                })
                .ToList();
        }

        private static int MetricValue(DashboardFact fact, LifecycleMetric metric) => metric switch
        {
            LifecycleMetric.OrdersPlaced => fact.OrdersPlaced,
            LifecycleMetric.Produced => fact.Produced,
            LifecycleMetric.Loaded => fact.Loaded,
            LifecycleMetric.FactoryDefects => fact.FactoryDefects,
            LifecycleMetric.Sold => fact.Sold,
            LifecycleMetric.Returned => fact.ClosingStock,
            LifecycleMetric.Damaged => fact.Damaged,
            LifecycleMetric.CustomerReturns => fact.CustomerReturns,
            _ => 0
        };

        private static List<ExecutiveAlertDto> BuildAlerts(
            LifecycleTotalsDto totals,
            IReadOnlyList<ProductLineBreakdownDto> lines,
            TenderSplitDto tender)
        {
            var alerts = new List<ExecutiveAlertDto>();
            if (totals.OrdersPlaced > 0 && totals.Loaded < totals.OrdersPlaced)
            {
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "loading_shortfall",
                    Severity = totals.Loaded < totals.OrdersPlaced * 0.9m ? "high" : "medium",
                    Title = "Loading shortfall",
                    Message = $"{totals.OrdersPlaced - totals.Loaded:N0} requested units were not loaded."
                });
            }

            if (totals.Loaded > 0 && totals.Sold / (decimal)totals.Loaded < 0.75m)
            {
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "high_closing_stock",
                    Severity = "medium",
                    Title = "High closing stock",
                    Message = $"Sell-through is {totals.SellThroughPercent:0.0}% of loaded stock."
                });
            }

            var damageRate = totals.Loaded > 0
                ? Math.Round(totals.Damaged / (decimal)totals.Loaded * 100m, 1)
                : 0m;
            if (damageRate >= 2.5m)
            {
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "damage_spike",
                    Severity = "high",
                    Title = "Damage above tolerance",
                    Message = $"Damaged goods are {damageRate:0.0}% of loaded stock."
                });
            }

            if (totals.Sold > 0 && totals.CustomerReturns / (decimal)totals.Sold >= 0.02m)
            {
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "customer_returns",
                    Severity = "medium",
                    Title = "Customer returns elevated",
                    Message = $"Customer returns are {totals.CustomerReturnPercent:0.0}% of sold volume."
                });
            }

            if (alerts.Count == 0)
            {
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "lifecycle_ok",
                    Severity = "success",
                    Title = "Lifecycle on plan",
                    Message = "All eight stages are within the current operating tolerance."
                });
            }

            return alerts;
        }

        private static ProductType MapLine(string categoryName)
        {
            if (categoryName.Contains("water", StringComparison.OrdinalIgnoreCase)) return ProductType.Water;
            if (categoryName.Contains("snack", StringComparison.OrdinalIgnoreCase)
                || categoryName.Contains("popcorn", StringComparison.OrdinalIgnoreCase)) return ProductType.Popcorn;
            return ProductType.Bread;
        }

        private static List<PeriodSpec> PeriodSpecs(DateTime anchor, DateTime today)
        {
            var weekStart = anchor.AddDays(-(int)anchor.DayOfWeek);
            var monthStart = new DateTime(anchor.Year, anchor.Month, 1);
            var yearStart = new DateTime(anchor.Year, 1, 1);

            return new List<PeriodSpec>
            {
                new("Today", "Today", anchor, anchor, anchor.ToString("ddd, dd MMM"), "Daily", today),
                new("This Week", "This Week", weekStart, weekStart.AddDays(6), $"{weekStart:dd MMM} - {weekStart.AddDays(6):dd MMM yyyy}", "Week", today),
                new("This Month", "This Month", monthStart, monthStart.AddMonths(1).AddDays(-1), monthStart.ToString("MMMM yyyy"), "Monthly", today),
                new("This Year", "This Year", yearStart, new DateTime(anchor.Year, 12, 31), anchor.Year.ToString(), "Yearly", today)
            };
        }

        private sealed record PeriodSpec(
            string Key,
            string Label,
            DateTime Start,
            DateTime End,
            string DateLabel,
            string ReportsTimeframe,
            DateTime Today)
        {
            public DateTime EffectiveEnd => End.Date > Today.Date ? Today.Date : End.Date;
        }
    }
}
