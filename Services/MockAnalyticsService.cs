using System.Collections.Concurrent;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Services
{
    /// <summary>
    /// Deterministic analytics layer behind the executive dashboard and the
    /// /reports engine.
    ///
    /// There is no database yet, so this service synthesises the operational
    /// lifecycle (requested -> produced -> loaded -> sold -> returned -> damaged)
    /// on top of the live recipe master, the sales rep roster and the vehicle fleet.
    ///
    /// Every figure is derived from a stable FNV-1a hash of its natural key
    /// (date + SKU + rep). Two consequences matter:
    ///   * reloading a page, switching between table and charts, or paginating
    ///     never changes a number, and
    ///   * narrowing a range down to a single day returns exactly the slice that
    ///     was aggregated into the wider range.
    /// </summary>
    public class MockAnalyticsService
    {
        /// <summary>Planned cash share of revenue; the benchmark for cash variance.</summary>
        private const decimal PlannedCashShare = 0.32m;

        private const string ScopeAll = "all";

        private readonly MockProductionService _productionService;
        private readonly MockUserService _userService;
        private readonly MockLogisticsService _logisticsService;

        /// <summary>
        /// The field roster is owned by sales; analytics only reads it, so a rep can
        /// never be one person on the dashboard and somebody else on an order.
        /// </summary>
        private readonly MockSalesService _salesService;

        private readonly List<ProductSkuDto> _catalog;
        private readonly List<SalesRepProfile> _reps;
        private readonly Dictionary<string, Vehicle> _vehicles = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<DateTime, List<Cell>> _operationalCache = new();
        private readonly ConcurrentDictionary<DateTime, List<FleetCell>> _fleetCache = new();

        public MockAnalyticsService(
            MockProductionService productionService,
            MockUserService userService,
            MockLogisticsService logisticsService,
            MockSalesService salesService)
        {
            _productionService = productionService;
            _userService = userService;
            _logisticsService = logisticsService;
            _salesService = salesService;

            foreach (var vehicle in _logisticsService.GetAllVehicles())
            {
                _vehicles[vehicle.RegistrationNumber] = vehicle;
            }

            _catalog = BuildCatalog();
            _reps = _salesService.GetAllReps();
        }

        // =============================================================
        // Catalog
        // =============================================================

        /// <summary>Sellable SKUs, projected straight from the live recipe master.</summary>
        public List<ProductSkuDto> GetProductCatalog() => _catalog.ToList();

        /// <summary>Sales reps that own a route, a terminal and a share of the fleet.</summary>
        public List<SalesRepProfile> GetSalesReps() => _reps.ToList();

        public List<RepFilterOptionDto> GetRepFilterOptions() => _reps
            .Select(r => new RepFilterOptionDto
            {
                Id = r.Id,
                FullName = r.FullName,
                RouteName = r.RouteName,
                PosTerminalCode = r.PosTerminalCode,
                VehicleRegistration = r.VehicleRegistration
            })
            .ToList();

        public List<string> GetRoutes() => _reps.Select(r => r.RouteName).Distinct().OrderBy(r => r).ToList();

        public List<ProductType> GetProductLines() => Enum.GetValues<ProductType>().ToList();

        private List<ProductSkuDto> BuildCatalog()
        {
            // Bread carries a heavier daily basket than a single dispenser run,
            // and popcorn is snack volume, so scale the baseline yield per line.
            var lineFactor = new Dictionary<ProductType, decimal>
            {
                [ProductType.Bread] = 0.45m,
                [ProductType.Water] = 0.60m,
                [ProductType.Popcorn] = 1.20m
            };

            var icons = new Dictionary<ProductType, string>
            {
                [ProductType.Bread] = "fa-solid fa-bread-slice",
                [ProductType.Water] = "fa-solid fa-droplet",
                [ProductType.Popcorn] = "fa-solid fa-popcorn"
            };

            return _productionService.GetAllRecipes()
                .Where(r => r.IsActive)
                .Select(r =>
                {
                    var factor = lineFactor.TryGetValue(r.ProductType, out var f) ? f : 0.5m;
                    return new ProductSkuDto
                    {
                        Id = r.Id,
                        Code = r.Code,
                        Name = r.Name,
                        Line = r.ProductType,
                        LineLabel = r.ProductType.ToString(),
                        OutputUnit = r.OutputUnit,
                        SellingPrice = r.SellingPrice,
                        BaselineYield = r.ExpectedYield,
                        BaseDailyDemand = Math.Round(r.ExpectedYield * factor),
                        IconClass = icons.TryGetValue(r.ProductType, out var icon) ? icon : "fa-solid fa-box"
                    };
                })
                .OrderBy(s => (int)s.Line)
                .ThenByDescending(s => s.BaseDailyDemand)
                .ThenBy(s => s.Name)
                .ToList();
        }

        // =============================================================
        // Date range resolution
        // =============================================================

        /// <summary>Sunday of the operational week (Sun-Sat) that contains <paramref name="anchor"/>.</summary>
        public static DateTime WeekStart(DateTime anchor) => anchor.Date.AddDays(-(int)anchor.DayOfWeek);

        public static (DateTime Start, DateTime End, string Label) ResolveRange(
            ReportTimeframe timeframe,
            DateTime anchor,
            DateTime? customStart,
            DateTime? customEnd)
        {
            var day = anchor.Date;
            switch (timeframe)
            {
                case ReportTimeframe.Daily:
                    return (day, day, day.ToString("ddd, dd MMM yyyy"));

                case ReportTimeframe.Week:
                {
                    var start = WeekStart(day);
                    var end = start.AddDays(6);
                    return (start, end, $"{start:dd MMM} – {end:dd MMM yyyy}");
                }

                case ReportTimeframe.Monthly:
                {
                    var start = new DateTime(day.Year, day.Month, 1);
                    var end = start.AddMonths(1).AddDays(-1);
                    return (start, end, start.ToString("MMMM yyyy"));
                }

                case ReportTimeframe.Yearly:
                    return (new DateTime(day.Year, 1, 1), new DateTime(day.Year, 12, 31), day.Year.ToString());

                default:
                {
                    var start = (customStart ?? WeekStart(day)).Date;
                    var end = (customEnd ?? day).Date;
                    if (end < start) (start, end) = (end, start);
                    return (start, end, $"{start:dd MMM yyyy} – {end:dd MMM yyyy}");
                }
            }
        }

        /// <summary>
        /// Resolves the nominal range, clamps the effective end to the last day that
        /// actually has data, and records the chart bucket granularity.
        /// </summary>
        public ReportsQueryDto Normalise(ReportsQueryDto query, DateTime today)
        {
            var anchor = (query.AnchorDate == default ? today : query.AnchorDate).Date;
            query.AnchorDate = anchor;

            var (start, end, label) = ResolveRange(query.Timeframe, anchor, query.StartDate, query.EndDate);
            query.StartDate = start;
            query.EndDate = end;

            // A window that has not opened yet stays empty rather than being dragged
            // back to its nominal start, so a report never books a shift before it ran.
            var effectiveEnd = end > today ? today : end;
            query.EffectiveEndDate = effectiveEnd;
            query.IsClamped = effectiveEnd < end;
            query.RangeLabel = query.IsClamped ? label + " (to date)" : label;

            // Year-long ranges are charted by month; everything else by day.
            query.BucketLabel = (effectiveEnd - start).TotalDays > 70 ? "Month" : "Day";

            query.Limit = Math.Clamp(query.Limit, 5, 100);
            query.Page = Math.Max(1, query.Page);
            return query;
        }

        // =============================================================
        // Executive dashboard
        // =============================================================

        public ExecutiveOverviewDto GetDashboardOverview(DateTime? date = null)
        {
            var today = DateTime.UtcNow.Date;
            var anchor = (date ?? today).Date;
            if (anchor > today) anchor = today;

            var specs = PeriodSpecs(anchor, today);
            var cellsByPeriod = specs.ToDictionary(
                s => s.Key,
                s => EnumerateDates(s.Start, s.EffectiveEnd).SelectMany(GetOperationalDay).ToList());

            var periods = specs.Select(s => new KpiPeriodCardDto
            {
                Key = s.Key,
                Label = s.Key,
                DateLabel = s.Label,
                ReportsTimeframe = s.Timeframe,
                RepCount = cellsByPeriod[s.Key].Select(c => c.RepId).Distinct().Count(),
                Totals = Aggregate(cellsByPeriod[s.Key])
            }).ToList();

            var overview = new ExecutiveOverviewDto
            {
                GeneratedAt = DateTime.UtcNow,
                AnchorDate = anchor,
                AnchorDateLabel = anchor.ToString("dddd, dd MMMM yyyy"),
                Today = periods[0].Totals,
                Periods = periods
            };

            // The drill-down scopes already carry per-rep rows for every window,
            // so they are built first and then reused by the alert rules.
            overview.DrillDownScopes = BuildDrillDownScopes(specs, cellsByPeriod);
            overview.ProductDrillDownScopes = BuildProductDrillDownScopes(specs, cellsByPeriod);
            overview.RepToday = overview.DrillDownScopes
                .FirstOrDefault(s => s.ScopeKey == ScopeAll)?
                .Periods.GetValueOrDefault(specs[0].Key) ?? new List<RepDrillDownRowDto>();

            // Every widget is pre-computed for every window, so the dashboard
            // timeframe switcher only has to reveal the right panel instead of
            // re-deriving anything in the browser.
            var allReps = overview.DrillDownScopes.First(s => s.ScopeKey == ScopeAll);
            foreach (var period in periods)
            {
                var cells = cellsByPeriod[period.Key];
                var lines = BuildProductLineBreakdown(cells);
                var tender = TenderOf(cells);
                var reps = allReps.Periods.GetValueOrDefault(period.Key) ?? new List<RepDrillDownRowDto>();

                overview.ProductLinesByPeriod[period.Key] = lines;
                overview.TenderByPeriod[period.Key] = tender;
                overview.AlertsByPeriod[period.Key] = BuildAlerts(period.Totals, lines, tender, reps);
            }

            // The singular properties remain the default (Today) view.
            overview.ProductLines = overview.ProductLinesByPeriod[specs[0].Key];
            overview.Tender = overview.TenderByPeriod[specs[0].Key];
            overview.Alerts = overview.AlertsByPeriod[specs[0].Key];

            return overview;
        }

        public DashboardOverviewApiResponse GetDashboardOverviewApi(string? timeframe = null, DateTime? date = null)
        {
            var today = DateTime.UtcNow.Date;
            var anchor = (date ?? today).Date;
            if (anchor > today) anchor = today;

            var overview = GetDashboardOverview(anchor);

            var tfRaw = (timeframe ?? "today").Trim().ToLowerInvariant();
            var periodKey = tfRaw switch
            {
                "weekly" or "week" or "this week" or "this_week" => "This Week",
                "monthly" or "month" or "this month" or "this_month" => "This Month",
                "yearly" or "year" or "this year" or "this_year" => "This Year",
                _ => "Today"
            };

            var normalizedTimeframe = periodKey switch
            {
                "This Week" => "weekly",
                "This Month" => "monthly",
                "This Year" => "yearly",
                _ => "today"
            };

            var period = overview.Periods.FirstOrDefault(p => p.Key == periodKey) ?? overview.Periods[0];
            var totals = period.Totals;
            var lines = overview.ProductLinesByPeriod.GetValueOrDefault(period.Key) ?? overview.ProductLines;
            var tender = overview.TenderByPeriod.GetValueOrDefault(period.Key) ?? overview.Tender;
            var alerts = overview.AlertsByPeriod.GetValueOrDefault(period.Key) ?? overview.Alerts;

            var weekStart = WeekStart(anchor);
            var weekEnd = weekStart.AddDays(6);

            var fillRate = totals.OrdersPlaced <= 0 ? 0m : Math.Round(totals.Loaded / (decimal)totals.OrdersPlaced * 100m, 1);
            var sellThrough = totals.Loaded <= 0 ? 0m : Math.Round(totals.Sold / (decimal)totals.Loaded * 100m, 1);
            var damageRate = totals.Loaded <= 0 ? 0m : Math.Round(totals.Damaged / (decimal)totals.Loaded * 100m, 1);
            var collectionRate = totals.ExpectedRevenue <= 0 ? 0m : Math.Round(totals.Collected / totals.ExpectedRevenue * 100m, 1);

            return new DashboardOverviewApiResponse
            {
                Timeframe = normalizedTimeframe,
                OperatingWeekRange = new OperatingWeekRangeDto
                {
                    Start = weekStart.ToString("yyyy-MM-dd"),
                    End = weekEnd.ToString("yyyy-MM-dd")
                },
                LifecycleSummary = new LifecycleSummaryApiDto
                {
                    OrdersPlaced = totals.OrdersPlaced,
                    Produced = totals.Produced,
                    Loaded = totals.Loaded,
                    Sold = totals.Sold,
                    Returned = totals.Returned,
                    Damaged = totals.Damaged,
                    FillRatePct = fillRate,
                    SellThroughPct = sellThrough,
                    DamageRatePct = damageRate,
                    CollectionRatePct = collectionRate
                },
                Categories = lines.Select(l => new CategorySummaryApiDto
                {
                    CategoryId = l.Line.ToString().ToLowerInvariant(),
                    CategoryName = $"{l.LineLabel} Line",
                    VolumeSharePct = l.ShareOfVolumePercent,
                    TotalSoldUnits = l.Totals.Sold,
                    GrossRevenue = l.Totals.ExpectedRevenue,
                    OutstandingBalance = l.Totals.OutstandingBalance,
                    Lifecycle = new CategoryLifecycleApiDto
                    {
                        Orders = l.Totals.OrdersPlaced,
                        Produced = l.Totals.Produced,
                        Loaded = l.Totals.Loaded,
                        Sold = l.Totals.Sold,
                        Returned = l.Totals.Returned,
                        Damaged = l.Totals.Damaged
                    }
                }).ToList(),
                TenderSplit = new TenderSplitApiDto
                {
                    Cash = tender.Cash,
                    Transfer = tender.BankTransfer,
                    Pos = tender.PosBank,
                    Outstanding = tender.Outstanding
                },
                AttentionAlerts = alerts.Select(a => new AttentionAlertApiDto
                {
                    Type = string.IsNullOrEmpty(a.Type) ? "alert" : a.Type,
                    Severity = a.Severity,
                    Message = a.Message
                }).ToList()
            };
        }

        private List<PeriodSpec> PeriodSpecs(DateTime anchor, DateTime today)
        {
            var weekStart = WeekStart(anchor);
            var monthStart = new DateTime(anchor.Year, anchor.Month, 1);
            var yearStart = new DateTime(anchor.Year, 1, 1);

            return new List<PeriodSpec>
            {
                PeriodSpec.Of("Today",      "Daily",   anchor,                           anchor,                           anchor.ToString("ddd, dd MMM"), today),
                PeriodSpec.Of("This Week",  "Week",    weekStart,                        weekStart.AddDays(6),            $"{weekStart:dd MMM} – {weekStart.AddDays(6):dd MMM yyyy}", today),
                PeriodSpec.Of("This Month", "Monthly", monthStart,                       monthStart.AddMonths(1).AddDays(-1), monthStart.ToString("MMMM yyyy"), today),
                PeriodSpec.Of("This Year",  "Yearly",  yearStart,                        new DateTime(anchor.Year, 12, 31), $"{yearStart:dd MMM yyyy} – {anchor:dd MMM yyyy}", today)
            };
        }

        private List<ProductLineBreakdownDto> BuildProductLineBreakdown(List<Cell> todayCells)
        {
            var grandSold = todayCells.Sum(c => c.Sold);
            var result = new List<ProductLineBreakdownDto>();

            foreach (var line in GetProductLines())
            {
                var skus = _catalog.Where(s => s.Line == line).ToList();
                if (skus.Count == 0) continue;

                var cells = todayCells.Where(c => c.Line == line).ToList();
                var sold = cells.Sum(c => c.Sold);

                result.Add(new ProductLineBreakdownDto
                {
                    Line = line,
                    LineLabel = skus[0].LineLabel,
                    IconClass = skus[0].IconClass,
                    SkuNames = skus.Select(s => s.Name).ToList(),
                    ShareOfVolumePercent = grandSold <= 0 ? 0m : Math.Round(sold / (decimal)grandSold * 100m, 1),
                    Totals = Aggregate(cells)
                });
            }

            return result;
        }

        private List<RepDrillDownScopeDto> BuildDrillDownScopes(
            List<PeriodSpec> specs, Dictionary<string, List<Cell>> cellsByPeriod)
        {
            var scopes = new List<RepDrillDownScopeDto>
            {
                new() { ScopeKey = ScopeAll, ScopeLabel = "All Product Lines" }
            };

            foreach (var line in GetProductLines())
            {
                var skus = _catalog.Where(s => s.Line == line).ToList();
                if (skus.Count == 0) continue;
                scopes.Add(new RepDrillDownScopeDto
                {
                    ScopeKey = line.ToString(),
                    ScopeLabel = skus[0].LineLabel + " Line"
                });
            }

            foreach (var scope in scopes)
            {
                var line = Enum.TryParse<ProductType>(scope.ScopeKey, ignoreCase: true, out var parsed)
                    ? parsed
                    : (ProductType?)null;

                foreach (var spec in specs)
                {
                    var cells = cellsByPeriod[spec.Key];
                    if (line.HasValue) cells = cells.Where(c => c.Line == line.Value).ToList();
                    scope.Periods[spec.Key] = BuildRepRows(cells);
                }
            }

            return scopes;
        }

        private List<ProductDrillDownScopeDto> BuildProductDrillDownScopes(
            List<PeriodSpec> specs, Dictionary<string, List<Cell>> cellsByPeriod)
        {
            var scopes = new List<ProductDrillDownScopeDto>
            {
                new() { ScopeKey = ScopeAll, ScopeLabel = "All Product Lines" }
            };

            foreach (var line in GetProductLines())
            {
                var skus = _catalog.Where(s => s.Line == line).ToList();
                if (skus.Count == 0) continue;
                scopes.Add(new ProductDrillDownScopeDto
                {
                    ScopeKey = line.ToString(),
                    ScopeLabel = skus[0].LineLabel + " Line"
                });
            }

            foreach (var scope in scopes)
            {
                var line = Enum.TryParse<ProductType>(scope.ScopeKey, ignoreCase: true, out var parsed)
                    ? parsed
                    : (ProductType?)null;

                foreach (var spec in specs)
                {
                    var cells = cellsByPeriod[spec.Key];
                    if (line.HasValue) cells = cells.Where(c => c.Line == line.Value).ToList();
                    scope.Periods[spec.Key] = BuildProductRows(cells);
                }
            }

            return scopes;
        }

        private List<ProductDrillDownRowDto> BuildProductRows(List<Cell> cells) =>
            cells
                .GroupBy(c => c.ProductId)
                .Select(g =>
                {
                    var first = g.First();
                    var sku = _catalog.FirstOrDefault(s => s.Id == g.Key);
                    var requested = g.Sum(c => c.Requested);
                    var produced = g.Sum(c => c.Produced);
                    var loaded = g.Sum(c => c.Loaded);
                    var sold = g.Sum(c => c.Sold);
                    var returned = g.Sum(c => c.Returned);
                    var damaged = g.Sum(c => c.Damaged);
                    var revenue = g.Sum(c => c.ExpectedRevenue);

                    return new ProductDrillDownRowDto
                    {
                        ProductId = g.Key,
                        ProductCode = first.ProductCode,
                        ProductName = first.ProductName,
                        Line = first.Line,
                        LineLabel = first.LineLabel,
                        OutputUnit = first.OutputUnit,
                        UnitPrice = sku?.SellingPrice ?? (sold > 0 ? Math.Round(revenue / sold, 2) : 0m),
                        OrdersPlaced = requested,
                        Produced = produced,
                        Loaded = loaded,
                        Sold = sold,
                        Returned = returned,
                        Damaged = damaged,
                        ExpectedRevenue = revenue
                    };
                })
                .OrderByDescending(r => r.OrdersPlaced)
                .ThenBy(r => r.ProductName)
                .ToList();

        private List<RepDrillDownRowDto> BuildRepRows(List<Cell> cells) =>
            cells
                .GroupBy(c => c.RepId)
                .Select(g => ToRepRow(g.Key, g.ToList()))
                .OrderByDescending(r => r.Sold)
                .ThenBy(r => r.RepName)
                .ToList();

        private RepDrillDownRowDto ToRepRow(int repId, List<Cell> cells)
        {
            var rep = _reps.FirstOrDefault(r => r.Id == repId);
            var totals = Aggregate(cells);
            var tender = TenderOf(cells);
            return new RepDrillDownRowDto
            {
                RepId = repId,
                RepName = rep?.FullName ?? $"Rep {repId}",
                Route = rep?.RouteName ?? "—",
                Terminal = rep?.PosTerminalCode ?? "—",
                VehicleRegistration = rep?.VehicleRegistration ?? "—",
                TicketCount = totals.TicketCount,
                OrdersPlaced = totals.OrdersPlaced,
                Produced = totals.Produced,
                Loaded = totals.Loaded,
                Sold = totals.Sold,
                Returned = totals.Returned,
                Damaged = totals.Damaged,
                ExpectedRevenue = totals.ExpectedRevenue,
                Collected = totals.Collected,
                CashCollected = tender.Cash,
                OutstandingBalance = totals.OutstandingBalance,
                FillRatePercent = totals.FillRatePercent,
                SellThroughPercent = totals.SellThroughPercent,
                DamagePercent = totals.DamagePercent
            };
        }

        /// <summary>
        /// Raises the exceptions worth an executive's attention for one window.
        /// Takes the period's own aggregates rather than the whole overview so
        /// the same rules can run for Today, the week, the month and the year.
        /// </summary>
        private static List<ExecutiveAlertDto> BuildAlerts(
            LifecycleTotalsDto totals,
            List<ProductLineBreakdownDto> lines,
            TenderSplitDto tender,
            List<RepDrillDownRowDto> reps)
        {
            var alerts = new List<ExecutiveAlertDto>();

            var shortLine = lines.FirstOrDefault(p => p.Totals.OrdersPlaced > p.Totals.Loaded || (p.Totals.FillRatePercent > 0 && p.Totals.FillRatePercent < 95m));
            if (shortLine != null)
            {
                var shortfall = Math.Max(shortLine.Totals.OrdersPlaced - shortLine.Totals.Loaded, 0);
                var unitName = shortLine.Line switch
                {
                    ProductType.Bread => "loaves",
                    ProductType.Water => "bottles",
                    ProductType.Popcorn => "bags",
                    _ => "units"
                };
                var message = shortfall > 0
                    ? $"{shortLine.LineLabel} production yielded {shortfall:N0} {unitName} below requested orders"
                    : $"Produced {shortLine.Totals.FillRatePercent}% of what reps requested. Check oven capacity and flour bag draw.";
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "loading_shortfall",
                    Severity = "medium",
                    Title = $"{shortLine.LineLabel} loading shortfall",
                    Message = message
                });
            }

            var worstDamage = lines
                .Where(p => p.Totals.DamagePercent > 0)
                .OrderByDescending(p => p.Totals.DamagePercent)
                .FirstOrDefault();
            if (worstDamage != null && worstDamage.Totals.DamagePercent >= 2.0m)
            {
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "damage_spike",
                    Severity = worstDamage.Totals.DamagePercent >= 3.0m ? "high" : "medium",
                    Title = $"Damaged {worstDamage.LineLabel} above tolerance",
                    Message = $"{worstDamage.Totals.DamagePercent:0.#}% of returned {worstDamage.LineLabel.ToLower()} stock was recorded as damaged."
                });
            }

            var uncollectedReps = reps.Where(r => r.OutstandingBalance > 0).ToList();
            if (tender.Outstanding > 0)
            {
                var repCount = uncollectedReps.Count;
                var repText = repCount > 0 ? $" across {repCount} sales rep{(repCount == 1 ? "" : "s")}" : "";
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "uncollected_balance",
                    Severity = tender.Outstanding > 25000m ? "high" : "medium",
                    Title = "Uncollected rep balances",
                    Message = $"{ReportColumnCatalog.FormatMoney(tender.Outstanding)} uncollected{repText}"
                });
            }

            var idleRep = reps.FirstOrDefault(r => r.Loaded > 0 && r.SellThroughPercent < 70m);
            if (idleRep != null)
            {
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "low_sell_through",
                    Severity = "info",
                    Title = "Low sell-through",
                    Message = $"{idleRep.RepName} ({idleRep.Route}) only converted {idleRep.SellThroughPercent}% of loaded stock."
                });
            }

            if (alerts.Count == 0)
            {
                alerts.Add(new ExecutiveAlertDto
                {
                    Type = "lifecycle_ok",
                    Severity = "success",
                    Title = "Lifecycle on plan",
                    Message = "Requested, produced, loaded, sold and return reconciliation is inside tolerance for every product line."
                });
            }

            return alerts;
        }

        // =============================================================
        // Reports query
        // =============================================================

        public ReportResultDto QueryReports(ReportsQueryDto query)
        {
            var today = DateTime.UtcNow.Date;
            query = Normalise(query, today);

            var cells = EnumerateDates(query.StartDate, query.EffectiveEndDate)
                .SelectMany(GetOperationalDay)
                .Where(c => Matches(c, query))
                .ToList();

            var fleet = EnumerateDates(query.StartDate, query.EffectiveEndDate)
                .SelectMany(GetFleetDay)
                .Where(f => Matches(f, query))
                .ToList();

            var logistics = BuildLogisticsPoints(cells, fleet);

            var result = new ReportResultDto
            {
                Query = query,
                Module = query.Module,
                ModuleLabel = ModuleLabel(query.Module),
                ModuleDescription = ModuleDescription(query.Module),
                GroupByLabel = GroupByLabel(query.GroupBy),
                Catalog = _catalog.ToList(),
                Reps = GetRepFilterOptions(),
                Routes = GetRoutes(),
                Columns = ReportColumnCatalog.ForModule(query.Module),
                ActiveFilterLabels = DescribeFilters(query),
                GrandTotals = Aggregate(cells),
                Tender = TenderOf(cells),
                LogisticsSummary = new LogisticsChartDto { Rows = logistics }
            };

            var allRows = query.Module switch
            {
                AnalyticsModule.Financial => BuildFinancialRows(cells),
                AnalyticsModule.Logistics => BuildLogisticsRows(cells, logistics),
                _ => BuildOperationalRows(cells, query)
            };

            result.TotalRows = allRows.Count;
            result.Limit = query.Limit;
            result.TotalPages = Math.Max(1, (int)Math.Ceiling(allRows.Count / (double)query.Limit));
            result.Page = Math.Min(Math.Max(1, query.Page), result.TotalPages);

            var sorted = string.IsNullOrWhiteSpace(query.Sort)
                ? allRows.OrderByDescending(r => r.Sold).ThenBy(r => r.EntityLabel).ToList()
                : ReportColumnCatalog.Sort(allRows, query.Sort, query.Direction).ToList();

            result.Rows = sorted
                .Skip((result.Page - 1) * result.Limit)
                .Take(result.Limit)
                .ToList();
            result.AllRows = sorted;

            result.Charts = BuildCharts(query, cells, logistics);
            return result;
        }

        private bool Matches(Cell cell, ReportsQueryDto query)
        {
            if (query.ProductIds.Count > 0 && !query.ProductIds.Contains(cell.ProductId)) return false;
            if (query.Lines.Count > 0 && !query.Lines.Contains(cell.Line)) return false;
            if (query.RepIds.Count > 0 && !query.RepIds.Contains(cell.RepId)) return false;
            if (!string.IsNullOrWhiteSpace(query.Route) && !string.Equals(cell.Route, query.Route, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private bool Matches(FleetCell cell, ReportsQueryDto query)
        {
            if (query.RepIds.Count > 0 && !query.RepIds.Contains(cell.RepId)) return false;
            if (!string.IsNullOrWhiteSpace(query.Route) && !string.Equals(cell.Route, query.Route, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        // ---- Row builders ------------------------------------------

        private List<ReportRowDto> BuildOperationalRows(List<Cell> cells, ReportsQueryDto query)
        {
            // A single-rep filter scopes the whole report to one person, so stamp
            // that identity onto every row rather than leaving the rep columns blank.
            var onlyRep = query.RepIds.Count == 1
                ? cells.FirstOrDefault(c => c.RepId == query.RepIds[0])
                : null;

            return query.GroupBy switch
            {
                ReportGroupBy.Sku => cells
                    .GroupBy(c => c.ProductId)
                    .Select(g =>
                    {
                        var row = new ReportRowDto
                        {
                            RowKey = $"sku-{g.Key}",
                            ProductId = g.Key,
                            ProductCode = g.First().ProductCode,
                            ProductName = g.First().ProductName,
                            ProductLine = g.First().LineLabel,
                            OutputUnit = g.First().OutputUnit,
                            EntityLabel = g.First().ProductName,
                            EntitySubLabel = g.First().ProductCode
                        };
                        StampRep(row, onlyRep);
                        return row.Apply(Aggregate(g.ToList()));
                    })
                    .ToList(),

                ReportGroupBy.SalesRep => cells
                    .GroupBy(c => c.RepId)
                    .Select(g => new ReportRowDto
                    {
                        RowKey = $"rep-{g.Key}",
                        RepId = g.Key,
                        RepName = g.First().RepName,
                        Route = g.First().Route,
                        Terminal = g.First().Terminal,
                        VehicleRegistration = g.First().VehicleRegistration,
                        OutputUnit = "units",
                        EntityLabel = g.First().RepName,
                        EntitySubLabel = g.First().Route
                    }.Apply(Aggregate(g.ToList())))
                    .ToList(),

                _ => cells
                    .GroupBy(c => c.LineLabel)
                    .Select(g =>
                    {
                        var row = new ReportRowDto
                        {
                            RowKey = $"line-{g.Key}",
                            ProductLine = g.Key,
                            OutputUnit = g.Select(c => c.OutputUnit).FirstOrDefault() ?? "units",
                            EntityLabel = g.Key + " Line",
                            // Distinct SKUs, not cells: the group spans every day in the range.
                            EntitySubLabel = g.Select(c => c.ProductId).Distinct().Count() + " SKU(s)"
                        };
                        StampRep(row, onlyRep);
                        return row.Apply(Aggregate(g.ToList()));
                    })
                    .ToList()
            };
        }

        private static void StampRep(ReportRowDto row, Cell? rep)
        {
            if (rep == null) return;
            row.RepId = rep.RepId;
            row.RepName = rep.RepName;
            row.Route = rep.Route;
            row.Terminal = rep.Terminal;
            row.VehicleRegistration = rep.VehicleRegistration;
        }

        private List<ReportRowDto> BuildFinancialRows(List<Cell> cells) =>
            cells
                .GroupBy(c => c.RepId)
                .Select(g =>
                {
                    var group = g.ToList();
                    var totals = Aggregate(group);
                    var first = group.First();
                    var row = new ReportRowDto
                    {
                        RowKey = $"fin-rep-{g.Key}",
                        RepId = g.Key,
                        RepName = first.RepName,
                        Route = first.Route,
                        Terminal = first.Terminal,
                        VehicleRegistration = first.VehicleRegistration,
                        OutputUnit = "units",
                        EntityLabel = first.RepName,
                        EntitySubLabel = first.Route
                    }.Apply(totals)
                     .ApplyTender(TenderOf(group));

                    // Cash held by the rep versus the share of revenue the plant
                    // expects to be settled in physical cash.
                    row.CashVarianceAmount = Math.Round(row.CollectedCash - (totals.ExpectedRevenue * PlannedCashShare));
                    row.CashVariancePercent = totals.ExpectedRevenue <= 0
                        ? 0m
                        : Math.Round(row.CashVarianceAmount / totals.ExpectedRevenue * 100m, 1);

                    return row.FlagCollection();
                })
                .ToList();

        private List<ReportRowDto> BuildLogisticsRows(List<Cell> cells, List<LogisticsPointDto> logistics) =>
            logistics
                .Select(p => new ReportRowDto
                {
                    RowKey = $"fleet-{p.VehicleRegistration}",
                    VehicleRegistration = p.VehicleRegistration,
                    VehicleName = p.VehicleName,
                    DriverName = p.DriverName,
                    Route = p.Route,
                    RepId = p.RepId,
                    RepName = p.DriverName,
                    EntityLabel = p.VehicleRegistration,
                    EntitySubLabel = p.Route,
                    Trips = p.Trips,
                    DistanceKm = p.DistanceKm,
                    Litres = p.Litres,
                    FuelCost = p.FuelCost,
                    MaintenanceCost = p.MaintenanceCost,
                    TripExpenses = p.TripExpenses,
                    OnTimePercent = p.OnTimePercent,
                    DeliveryEfficiencyIndex = p.DeliveryEfficiencyIndex,
                    CostPerUnitSold = p.CostPerUnitSold,
                    Sold = p.SoldVolume
                }.Flagged())
                .ToList();

        // ---- Logistics ---------------------------------------------

        private List<LogisticsPointDto> BuildLogisticsPoints(List<Cell> cells, List<FleetCell> fleet)
        {
            var soldByRep = cells
                .GroupBy(c => c.RepId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Sold));

            return fleet
                .GroupBy(f => f.VehicleRegistration, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var list = g.ToList();
                    var distance = list.Sum(f => f.DistanceKm);
                    var cost = list.Sum(f => f.FuelCost + f.MaintenanceCost + f.TripExpenses);
                    var sold = list.Sum(f => soldByRep.TryGetValue(f.RepId, out var s) ? s : 0);
                    var rep = _reps.FirstOrDefault(r => string.Equals(r.VehicleRegistration, g.Key, StringComparison.OrdinalIgnoreCase));
                    _vehicles.TryGetValue(g.Key, out var vehicle);

                    return new LogisticsPointDto
                    {
                        VehicleRegistration = g.Key,
                        VehicleName = vehicle?.MakeAndModel ?? "Delivery vehicle",
                        DriverName = list.Select(f => f.DriverName).FirstOrDefault() ?? vehicle?.AssignedDriverName ?? "—",
                        Route = string.Join(" / ", list.Select(f => f.Route).Distinct().OrderBy(r => r)),
                        RepId = rep?.Id ?? 0,
                        SoldVolume = sold,
                        Trips = list.Sum(f => f.Trips),
                        DistanceKm = distance,
                        Litres = list.Sum(f => f.Litres),
                        FuelCost = list.Sum(f => f.FuelCost),
                        MaintenanceCost = list.Sum(f => f.MaintenanceCost),
                        TripExpenses = list.Sum(f => f.TripExpenses),
                        OnTimePercent = Math.Round((decimal)list.Average(f => f.OnTimePercent), 1),
                        DeliveryEfficiencyIndex = distance <= 0 ? 0m : Math.Round(sold / (distance / 100m), 1),
                        CostPerUnitSold = sold <= 0 ? 0m : Math.Round(cost / sold, 1)
                    };
                })
                .OrderByDescending(p => p.FuelCost + p.MaintenanceCost + p.TripExpenses)
                .ThenBy(p => p.VehicleRegistration)
                .ToList();
        }

        // ---- Charts -------------------------------------------------

        private ChartBundleDto BuildCharts(ReportsQueryDto query, List<Cell> cells, List<LogisticsPointDto> logistics) =>
            new()
            {
                Lifecycle = BuildLifecycleChart(query, cells),
                Revenue = BuildRevenueChart(query, cells),
                Volume = BuildVolumeChart(query, cells),
                RepPerformance = BuildRepPerformanceChart(cells),
                Logistics = new LogisticsChartDto { Rows = logistics }
            };

        private LifecycleComparisonChartDto BuildLifecycleChart(ReportsQueryDto query, List<Cell> cells)
        {
            var chart = new LifecycleComparisonChartDto
            {
                GroupByLabel = query.ChartGroupByRep ? "Sales Rep" : GroupByLabel(query.GroupBy)
            };

            if (cells.Count == 0)
            {
                chart.EmptyMessage = "No rows match the active filters.";
                return chart;
            }

            var key = CategoryKey(query);
            var groups = cells
                .Select(c => CategoryLabel(c, key))
                .Distinct()
                .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var definitions = new (string Key, string Label, Func<Cell, int> Select)[]
            {
                ("requested", "Requested", c => c.Requested),
                ("loaded",    "Loaded",    c => c.Loaded),
                ("sold",      "Sold",      c => c.Sold),
                ("returned",  "Returned",  c => c.Returned),
                ("damaged",   "Damaged",   c => c.Damaged)
            };

            var buckets = cells
                .GroupBy(c => CategoryLabel(c, key))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var definition in definitions)
            {
                chart.Series.Add(new ChartSeriesDto
                {
                    Key = definition.Key,
                    Label = definition.Label,
                    Data = groups
                        .Select(g => buckets.TryGetValue(g, out var list) ? (decimal)list.Sum(definition.Select) : 0m)
                        .ToList()
                });
            }

            chart.Categories = groups;
            return chart;
        }

        private static string CategoryKey(ReportsQueryDto query)
        {
            if (query.ChartGroupByRep) return "rep";
            return query.GroupBy == ReportGroupBy.Sku ? "sku" : "line";
        }

        private static string CategoryLabel(Cell cell, string key) => key switch
        {
            "rep" => cell.RepName,
            "sku" => cell.ProductName,
            _ => cell.LineLabel
        };

        private RevenueCollectionChartDto BuildRevenueChart(ReportsQueryDto query, List<Cell> cells)
        {
            var chart = new RevenueCollectionChartDto { BucketLabel = query.BucketLabel };

            foreach (var bucket in Bucketise(query.StartDate, query.EffectiveEndDate, query.BucketLabel == "Month"))
            {
                var slice = cells.Where(c => c.Date >= bucket.Start && c.Date <= bucket.End).ToList();
                var expected = slice.Sum(c => c.ExpectedRevenue);
                var collected = slice.Sum(c => c.Cash + c.Transfer + c.PosBank);

                chart.Points.Add(new ChartPointDto
                {
                    Key = bucket.Key,
                    Label = bucket.Label,
                    Value = Math.Round(expected),
                    SecondaryLabel = "Collected",
                    SecondaryValue = Math.Round(collected)
                });
            }

            return chart;
        }

        private VolumeDistributionChartDto BuildVolumeChart(ReportsQueryDto query, List<Cell> cells)
        {
            var chart = new VolumeDistributionChartDto();
            var total = cells.Sum(c => c.Sold);
            if (total <= 0)
            {
                chart.EmptyMessage = "No sales volume in the selected range.";
                return chart;
            }

            // With a single product line in scope, break the donut down by SKU so
            // the chart still carries detail instead of one flat slice.
            var bySku = query.Lines.Count == 1 || (query.Lines.Count == 0 && query.ProductIds.Count > 0);

            var groups = (bySku
                    ? cells.GroupBy(c => c.ProductName)
                    : cells.GroupBy(c => c.LineLabel))
                .Select(g => new { Label = bySku ? g.Key : g.Key + " Line", Sold = g.Sum(x => x.Sold) })
                .OrderByDescending(g => g.Sold)
                .ToList();

            // Rounded independently, the slices can add up to 99.9%; the largest
            // remainder carries the shortfall so the donut always reads 100%.
            var shares = SharesSummingTo100(groups.Select(g => (decimal)g.Sold / total * 100m));

            for (var i = 0; i < groups.Count; i++)
            {
                chart.Slices.Add(new ChartPointDto
                {
                    Key = groups[i].Label,
                    Label = groups[i].Label,
                    Value = groups[i].Sold,
                    SecondaryValue = shares[i]
                });
            }

            return chart;
        }

        /// <summary>
        /// Rounds percentage shares to one decimal place without losing a tenth
        /// anywhere, so the parts always sum to exactly 100.
        /// </summary>
        private static List<decimal> SharesSummingTo100(IEnumerable<decimal> exactShares)
        {
            var tenths = exactShares.Select(v => Math.Floor(v * 10m)).ToList();

            // The exact shares already total 100, so the tenths they were floored
            // from are short by at most one per slice.
            var residual = 1000 - (int)tenths.Sum();
            var picks = exactShares
                .Select((v, i) => (Index: i, Remainder: (v * 10m) - tenths[i]))
                .OrderByDescending(x => x.Remainder)
                .ThenBy(x => x.Index)
                .Take(Math.Clamp(residual, 0, tenths.Count))
                .ToList();

            foreach (var pick in picks) tenths[pick.Index] += 1m;
            return tenths.Select(t => t / 10m).ToList();
        }

        private RepPerformanceChartDto BuildRepPerformanceChart(List<Cell> cells)
        {
            var totalSold = cells.Sum(c => c.Sold);

            return new RepPerformanceChartDto
            {
                Rows = cells
                    .GroupBy(c => c.RepId)
                    .Select(g =>
                    {
                        var list = g.ToList();
                        var expected = list.Sum(c => c.ExpectedRevenue);
                        var collected = list.Sum(c => c.Cash + c.Transfer + c.PosBank);
                        var cash = list.Sum(c => c.Cash);
                        var variance = cash - Math.Round(expected * PlannedCashShare);
                        var first = g.First();

                        return new RepPerformancePointDto
                        {
                            RepId = g.Key,
                            RepName = first.RepName,
                            Route = first.Route,
                            // Rep ranking is by share of the filtered volume.
                            SoldUnits = list.Sum(c => c.Sold),
                            VolumeSharePercent = totalSold <= 0
                                ? 0m
                                : Math.Round(list.Sum(c => c.Sold) / (decimal)totalSold * 100m, 1),
                            ExpectedRevenue = Math.Round(expected),
                            Collected = Math.Round(collected),
                            OutstandingBalance = Math.Round(expected - collected),
                            VarianceAmount = Math.Round(variance),
                            VariancePercent = expected <= 0 ? 0m : Math.Round(variance / expected * 100m, 1)
                        };
                    })
                    .OrderByDescending(r => r.SoldUnits)
                    .ThenBy(r => r.RepName)
                    .ToList()
            };
        }

        private static List<ChartBucket> Bucketise(DateTime start, DateTime end, bool monthly)
        {
            var buckets = new List<ChartBucket>();
            if (monthly)
            {
                var cursor = new DateTime(start.Year, start.Month, 1);
                while (cursor <= end)
                {
                    var last = cursor.AddMonths(1).AddDays(-1);
                    buckets.Add(new ChartBucket(cursor, last, cursor.ToString("yyyy-MM"), cursor.ToString("MMM yyyy")));
                    cursor = last.AddDays(1);
                }
            }
            else
            {
                var cursor = start.Date;
                while (cursor <= end)
                {
                    buckets.Add(new ChartBucket(cursor, cursor, cursor.ToString("yyyy-MM-dd"), cursor.ToString("dd MMM")));
                    cursor = cursor.AddDays(1);
                }
            }

            return buckets;
        }

        // =============================================================
        // Deterministic data generation
        // =============================================================

        private static readonly Dictionary<DayOfWeek, double> WeekdayFactor = new()
        {
            [DayOfWeek.Sunday] = 1.25,
            [DayOfWeek.Monday] = 0.86,
            [DayOfWeek.Tuesday] = 0.90,
            [DayOfWeek.Wednesday] = 0.95,
            [DayOfWeek.Thursday] = 1.00,
            [DayOfWeek.Friday] = 1.16,
            [DayOfWeek.Saturday] = 1.45
        };

        private List<Cell> GetOperationalDay(DateTime date) =>
            _operationalCache.GetOrAdd(date.Date, BuildOperationalDay);

        private List<FleetCell> GetFleetDay(DateTime date) =>
            _fleetCache.GetOrAdd(date.Date, BuildFleetDay);

        private List<Cell> BuildOperationalDay(DateTime day)
        {
            var cells = new List<Cell>();
            var today = DateTime.UtcNow.Date;
            var hasHappened = day <= today;

            // A gentle seasonal wave so month-over-month trends are visible.
            var dayOfYear = (day - new DateTime(day.Year, 1, 1)).TotalDays;
            var trend = 1d + (0.08d * Math.Sin(dayOfYear / 365d * Math.PI * 6d));

            foreach (var sku in _catalog)
            {
                var weekday = WeekdayFactor[day.DayOfWeek];
                var noise = 0.86d + (0.28d * Unit($"sku|{sku.Id}|{day:yyyyMMdd}"));
                var requested = (int)Math.Round((double)sku.BaseDailyDemand * weekday * trend * noise);
                if (requested <= 0) continue;

                // The plant never runs ahead of the order book: fill rate lands
                // somewhere between 82% and 100%, so shortfalls are real but the
                // funnel stays monotonic (requested >= produced >= loaded).
                var produced = Math.Min(requested,
                    (int)Math.Round(requested * (0.82d + (0.18d * Unit($"prod|{sku.Id}|{day:yyyyMMdd}")))));
                var loaded = Math.Min(produced, (int)Math.Round(requested * (0.93d + (0.11d * Unit($"load|{sku.Id}|{day:yyyyMMdd}")))));

                if (!hasHappened) { produced = 0; loaded = 0; }

                // Production and loading are plant/loading-bay totals, allocated
                // across reps on the same weight used to split the request, so the
                // rep rows always sum back to the product totals.
                var repWeights = _reps.Select(r => RepWeight(r, sku, day)).ToList();
                var requestedSplit = Allocate(requested, repWeights);
                var producedSplit = Allocate(produced, repWeights);
                var loadedSplit = Allocate(loaded, repWeights);

                for (var i = 0; i < _reps.Count; i++)
                {
                    var rep = _reps[i];
                    var repLoaded = loadedSplit[i];
                    if (repLoaded <= 0 && requestedSplit[i] <= 0) continue;

                    var sellThrough = 0.76d + (0.22d * Unit($"sell|{rep.Id}|{sku.Id}|{day:yyyyMMdd}"));
                    var sold = (int)Math.Round(repLoaded * sellThrough);
                    var returned = Math.Max(0, repLoaded - sold);
                    var damaged = Math.Min(returned, (int)Math.Round(returned * 0.075d * Unit($"dmg|{rep.Id}|{sku.Id}|{day:yyyyMMdd}")));

                    var expectedRevenue = sold * sku.SellingPrice;
                    var collected = Math.Round(expectedRevenue * (decimal)(0.84d + (0.15d * Unit($"coll|{rep.Id}|{day:yyyyMMdd}"))), 2);

                    var tender = TenderWeights(rep, day);
                    var cash = Math.Round(collected * (decimal)tender[0], 2);
                    var transfer = Math.Round(collected * (decimal)tender[1], 2);
                    var posBank = Math.Max(0m, collected - cash - transfer);

                    cells.Add(new Cell
                    {
                        Date = day,
                        RepId = rep.Id,
                        RepName = rep.FullName,
                        Route = rep.RouteName,
                        Terminal = rep.PosTerminalCode,
                        VehicleRegistration = rep.VehicleRegistration,
                        ProductId = sku.Id,
                        ProductCode = sku.Code,
                        ProductName = sku.Name,
                        Line = sku.Line,
                        LineLabel = sku.LineLabel,
                        OutputUnit = sku.OutputUnit,
                        Tickets = Math.Max(1, (int)Math.Round(sold / 12d)),
                        Requested = requestedSplit[i],
                        Produced = producedSplit[i],
                        Loaded = repLoaded,
                        Sold = sold,
                        Returned = returned,
                        Damaged = damaged,
                        ExpectedRevenue = expectedRevenue,
                        Cash = cash,
                        Transfer = transfer,
                        PosBank = posBank,
                        Outstanding = expectedRevenue - collected
                    });
                }
            }

            return cells;
        }

        private List<FleetCell> BuildFleetDay(DateTime day)
        {
            var cells = new List<FleetCell>();
            var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            foreach (var rep in _reps)
            {
                _vehicles.TryGetValue(rep.VehicleRegistration, out var vehicle);
                var trips = day <= DateTime.UtcNow.Date ? (weekend ? 2 : 1) : 0;

                var legDistance = 28m + (55m * (decimal)Unit($"dist|{rep.Id}|{day:yyyyMMdd}"));
                var distance = Math.Round(trips * legDistance);
                var litres = Math.Round(distance * (0.11m + (0.05m * (decimal)Unit($"eco|{rep.Id}|{day:yyyyMMdd}"))));

                var diesel = vehicle?.FuelType?.StartsWith("AGO", StringComparison.OrdinalIgnoreCase) == true;
                var fuelCost = Math.Round(litres * (diesel ? 295m : 168m));

                // Planted breakdowns: rare, lumpy, and they land on the oldest vehicles.
                var wear = (decimal)Unit($"mnt|{rep.VehicleRegistration}|{day:yyyyMMdd}");
                var maintenance = wear > 0.94m
                    ? Math.Round(6000m + (22000m * (wear - 0.94m) / 0.06m))
                    : 0m;

                var tripExpenses = Math.Round(trips * (500m + (2400m * (decimal)Unit($"exp|{rep.Id}|{day:yyyyMMdd}"))));

                cells.Add(new FleetCell
                {
                    Date = day,
                    RepId = rep.Id,
                    RepName = rep.FullName,
                    Route = rep.RouteName,
                    VehicleRegistration = rep.VehicleRegistration,
                    DriverName = vehicle?.AssignedDriverName ?? rep.FullName,
                    Trips = trips,
                    DistanceKm = distance,
                    Litres = litres,
                    FuelCost = fuelCost,
                    MaintenanceCost = maintenance,
                    TripExpenses = tripExpenses,
                    OnTimePercent = 78d + (21d * Unit($"ontime|{rep.VehicleRegistration}|{day:yyyyMMdd}"))
                });
            }

            return cells;
        }

        private static double RepWeight(SalesRepProfile rep, ProductSkuDto sku, DateTime day)
        {
            var focus = rep.ProductFocus.Contains(sku.LineLabel, StringComparison.OrdinalIgnoreCase) ? 1.45d : 0.6d;
            return focus * (0.75d + (0.5d * Unit($"w|{rep.Id}|{sku.Id}|{day:yyyyMMdd}")));
        }

        private static double[] TenderWeights(SalesRepProfile rep, DateTime day)
        {
            var cash = Math.Clamp(0.28d + (0.18d * Unit($"cash|{rep.Id}|{day:yyyyMMdd}")), 0.14d, 0.50d);
            var transfer = Math.Clamp(0.40d + (0.16d * Unit($"trf|{rep.Id}|{day:yyyyMMdd}")), 0.30d, 0.70d);
            if (cash + transfer > 0.92d) transfer = 0.92d - cash;

            var pos = Math.Max(0.08d, 1d - cash - transfer);
            var total = cash + transfer + pos;
            return new[] { cash / total, transfer / total, pos / total };
        }

        private static IEnumerable<DateTime> EnumerateDates(DateTime start, DateTime end)
        {
            for (var cursor = start.Date; cursor <= end.Date; cursor = cursor.AddDays(1))
            {
                yield return cursor;
            }
        }

        // =============================================================
        // Aggregation helpers
        // =============================================================

        private static LifecycleTotalsDto Aggregate(List<Cell> cells)
        {
            var expected = cells.Sum(c => c.ExpectedRevenue);
            var collected = cells.Sum(c => c.Cash + c.Transfer + c.PosBank);

            return new LifecycleTotalsDto
            {
                OrdersPlaced = cells.Sum(c => c.Requested),
                Produced = cells.Sum(c => c.Produced),
                Loaded = cells.Sum(c => c.Loaded),
                Sold = cells.Sum(c => c.Sold),
                Returned = cells.Sum(c => c.Returned),
                Damaged = cells.Sum(c => c.Damaged),
                TicketCount = cells.Sum(c => c.Tickets),
                ExpectedRevenue = expected,
                Collected = collected,
                OutstandingBalance = expected - collected
            };
        }

        private static TenderSplitDto TenderOf(List<Cell> cells) => new()
        {
            Cash = cells.Sum(c => c.Cash),
            BankTransfer = cells.Sum(c => c.Transfer),
            PosBank = cells.Sum(c => c.PosBank),
            Outstanding = cells.Sum(c => c.Outstanding)
        };

        /// <summary>
        /// Splits <paramref name="total"/> across <paramref name="weights"/> using
        /// largest-remainder rounding, so the parts always sum back to the whole.
        /// </summary>
        private static int[] Allocate(int total, IReadOnlyList<double> weights)
        {
            var n = weights.Count;
            var result = new int[n];
            if (n == 0 || total <= 0) return result;

            var sum = weights.Sum();
            if (sum <= 0)
            {
                var even = total / n;
                for (var i = 0; i < n; i++) result[i] = even;
                return result;
            }

            var shortfall = total;
            for (var i = 0; i < n; i++)
            {
                result[i] = (int)Math.Floor(total * weights[i] / sum);
                shortfall -= result[i];
            }

            Enumerable.Range(0, n)
                .OrderByDescending(i => (total * weights[i] / sum) - result[i])
                .ThenBy(i => i)
                .Take(Math.Max(0, shortfall))
                .ToList()
                .ForEach(i => result[i]++);

            return result;
        }

        /// <summary>
        /// Stable FNV-1a hash mapped into [0,1). Deterministic across restarts,
        /// unlike <see cref="string.GetHashCode()"/>.
        /// </summary>
        private static double Unit(string key)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var ch in key)
                {
                    hash ^= ch;
                    hash *= 16777619;
                }

                // Avalanche so neighbouring keys land far apart.
                hash ^= hash >> 15;
                hash *= 2246822519;
                hash ^= hash >> 13;
                hash *= 3266489917;
                hash ^= hash >> 16;

                return hash / (double)uint.MaxValue;
            }
        }

        private static string ModuleLabel(AnalyticsModule module) => module switch
        {
            AnalyticsModule.Financial => "Financial Reconciliation",
            AnalyticsModule.Logistics => "Logistics & Fleet",
            _ => "Operational Reconciliation"
        };

        private static string ModuleDescription(AnalyticsModule module) => module switch
        {
            AnalyticsModule.Financial => "Expected revenue against cash, transfer and POS-bank collections, plus the balance still owed on each rep account.",
            AnalyticsModule.Logistics => "Fuel, maintenance and trip expenditure reconciled against trips run, distance covered and units actually delivered.",
            _ => "What reps requested against what the plant produced, what the loading bay released, what sold on the route and what came back."
        };

        private static string GroupByLabel(ReportGroupBy groupBy) => groupBy switch
        {
            ReportGroupBy.Sku => "Product SKU",
            ReportGroupBy.SalesRep => "Sales Rep",
            _ => "Product Line"
        };

        private static List<string> DescribeFilters(ReportsQueryDto query)
        {
            var labels = new List<string>
            {
                query.Timeframe switch
                {
                    ReportTimeframe.Daily => "Daily",
                    ReportTimeframe.Week => "Operational week (Sun–Sat)",
                    ReportTimeframe.Monthly => "Monthly",
                    ReportTimeframe.Yearly => "Year to date",
                    _ => "Custom range"
                }
            };

            if (query.Lines.Count > 0) labels.Add(string.Join(" + ", query.Lines.Select(l => l.ToString())));
            if (query.ProductIds.Count > 0) labels.Add($"{query.ProductIds.Count} SKU(s)");
            if (query.RepIds.Count > 0) labels.Add($"{query.RepIds.Count} rep(s)");
            if (!string.IsNullOrWhiteSpace(query.Route)) labels.Add("Route: " + query.Route);
            labels.Add("Grouped by " + GroupByLabel(query.GroupBy));

            return labels;
        }

        // =============================================================
        // Internal shapes
        // =============================================================

        private sealed class Cell
        {
            public DateTime Date;
            public int RepId;
            public string RepName = string.Empty;
            public string Route = string.Empty;
            public string Terminal = string.Empty;
            public string VehicleRegistration = string.Empty;
            public int ProductId;
            public string ProductCode = string.Empty;
            public string ProductName = string.Empty;
            public ProductType Line;
            public string LineLabel = string.Empty;
            public string OutputUnit = string.Empty;
            public int Tickets;
            public int Requested;
            public int Produced;
            public int Loaded;
            public int Sold;
            public int Returned;
            public int Damaged;
            public decimal ExpectedRevenue;
            public decimal Cash;
            public decimal Transfer;
            public decimal PosBank;
            public decimal Outstanding;
        }

        private sealed class FleetCell
        {
            public DateTime Date;
            public int RepId;
            public string RepName = string.Empty;
            public string Route = string.Empty;
            public string VehicleRegistration = string.Empty;
            public string DriverName = string.Empty;
            public int Trips;
            public decimal DistanceKm;
            public decimal Litres;
            public decimal FuelCost;
            public decimal MaintenanceCost;
            public decimal TripExpenses;
            public double OnTimePercent;
        }

        private sealed class ChartBucket
        {
            public ChartBucket(DateTime start, DateTime end, string key, string label)
            {
                Start = start;
                End = end;
                Key = key;
                Label = label;
            }

            public DateTime Start { get; }
            public DateTime End { get; }
            public string Key { get; }
            public string Label { get; }
        }

        private sealed class PeriodSpec
        {
            public string Key { get; private init; } = string.Empty;
            public string Timeframe { get; private init; } = string.Empty;
            public DateTime Start { get; private init; }
            public DateTime End { get; private init; }
            public DateTime EffectiveEnd { get; private init; }
            public string Label { get; private init; } = string.Empty;

            public static PeriodSpec Of(string key, string timeframe, DateTime start, DateTime end, string label, DateTime today)
            {
                var effective = end > today ? today : end;
                if (effective < start) effective = start;
                return new PeriodSpec
                {
                    Key = key,
                    Timeframe = timeframe,
                    Start = start.Date,
                    End = end.Date,
                    EffectiveEnd = effective.Date,
                    Label = effective < end.Date ? label + " (to date)" : label
                };
            }
        }
    }

    /// <summary>Row shaping helpers kept out of the service so extensions compile.</summary>
    internal static class ReportRowExtensions
    {
        public static ReportRowDto Apply(this ReportRowDto row, LifecycleTotalsDto totals)
        {
            row.TicketCount = totals.TicketCount;
            row.OrdersPlaced = totals.OrdersPlaced;
            row.Produced = totals.Produced;
            row.Loaded = totals.Loaded;
            row.Sold = totals.Sold;
            row.Returned = totals.Returned;
            row.Damaged = totals.Damaged;
            row.FillRatePercent = totals.FillRatePercent;
            row.SellThroughPercent = totals.SellThroughPercent;
            row.DamagePercent = totals.DamagePercent;
            row.ExpectedRevenue = totals.ExpectedRevenue;
            row.CollectionRatePercent = totals.ExpectedRevenue <= 0
                ? 0m
                : Math.Round(totals.Collected / totals.ExpectedRevenue * 100m, 1);
            row.OutstandingBalance = totals.OutstandingBalance;
            return row.Flagged();
        }

        /// <summary>
        /// Copies the multi-channel tender split onto a row. Held separately from
        /// <see cref="Apply"/> because the operational and logistics modules do not
        /// show tender columns, but the financial module cannot be built without them.
        /// </summary>
        public static ReportRowDto ApplyTender(this ReportRowDto row, TenderSplitDto tender)
        {
            row.CollectedCash = tender.Cash;
            row.CollectedTransfer = tender.BankTransfer;
            row.CollectedPosBank = tender.PosBank;
            return row;
        }

        /// <summary>Attaches the operational red-flag variance the table highlights.</summary>
        public static ReportRowDto Flagged(this ReportRowDto row)
        {
            row.AlertLevel = ReportAlertLevel.None;
            row.AlertMessage = string.Empty;

            if (row.OrdersPlaced > 0 && row.FillRatePercent is > 0 and < 90m)
            {
                row.AlertLevel = ReportAlertLevel.Warning;
                row.AlertMessage = "Production shortfall";
            }

            if (row.Loaded > 0 && row.SellThroughPercent is > 0 and < 75m)
            {
                row.AlertLevel = ReportAlertLevel.Warning;
                row.AlertMessage = "High closing stock";
            }

            if (row.DamagePercent >= 2.5m)
            {
                row.AlertLevel = ReportAlertLevel.Error;
                row.AlertMessage = "Damage above tolerance";
            }

            return row;
        }

        /// <summary>Adds the settlement red flag used by the financial module.</summary>
        public static ReportRowDto FlagCollection(this ReportRowDto row)
        {
            if (row.ExpectedRevenue > 0 && row.CollectionRatePercent < 92m)
            {
                if (row.AlertLevel != ReportAlertLevel.Error) row.AlertLevel = ReportAlertLevel.Warning;
                row.AlertMessage = string.IsNullOrEmpty(row.AlertMessage)
                    ? "Unsettled account"
                    : row.AlertMessage + " / unsettled account";
            }

            if (row.CashVarianceAmount > 0 && row.ExpectedRevenue > 0 && row.CashVariancePercent > 6m)
            {
                row.AlertLevel = ReportAlertLevel.Warning;
                row.AlertMessage = string.IsNullOrEmpty(row.AlertMessage)
                    ? "Cash float above plan"
                    : row.AlertMessage + " / cash float above plan";
            }

            return row;
        }
    }
}
