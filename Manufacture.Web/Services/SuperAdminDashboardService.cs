using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services
{
    /// <summary>
    /// Reads the operational tables behind the SuperAdmin executive dashboard.
    /// Route-sheet quantities drive the rep lifecycle; production batches and
    /// defects supply plant-level stages; customer returns remain separate.
    /// </summary>
    public class SuperAdminDashboardService : ISuperAdminDashboardService
    {
        private readonly ManufactureDbContext _db;

        public SuperAdminDashboardService(ManufactureDbContext db)
        {
            _db = db;
        }

        public ExecutiveOverviewDto GetOverview(DateTime? date = null)
        {
            var today = DateTime.UtcNow.Date;
            var anchor = (date ?? today).Date;
            if (anchor > today) anchor = today;

            var start = new DateTime(anchor.Year, 1, 1);
            var endExclusive = anchor.AddDays(1);
            var facts = BuildFacts(start, endExclusive);
            var categories = BuildCategories(facts);
            var skus = BuildSkus(categories);

            return SuperAdminDashboardAggregator.Build(anchor, today, facts, categories, skus);
        }

        public DashboardOverviewApiResponse GetOverviewApi(string? timeframe = null, DateTime? date = null)
        {
            var today = DateTime.UtcNow.Date;
            var anchor = (date ?? today).Date;
            if (anchor > today) anchor = today;

            var overview = GetOverview(anchor);
            var periodKey = (timeframe ?? "today").Trim().ToLowerInvariant() switch
            {
                "weekly" or "week" or "this week" or "this_week" => "This Week",
                "monthly" or "month" or "this month" or "this_month" => "This Month",
                "yearly" or "year" or "this year" or "this_year" => "This Year",
                _ => "Today"
            };

            var period = overview.Periods.FirstOrDefault(card => card.Key == periodKey)
                ?? overview.Periods[0];
            var totals = period.Totals;
            var lines = overview.ProductLinesByPeriod.GetValueOrDefault(period.Key) ?? overview.ProductLines;
            var tender = overview.TenderByPeriod.GetValueOrDefault(period.Key) ?? overview.Tender;
            var alerts = overview.AlertsByPeriod.GetValueOrDefault(period.Key) ?? overview.Alerts;
            var weekStart = anchor.AddDays(-(int)anchor.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);

            var normalizedTimeframe = periodKey switch
            {
                "This Week" => "weekly",
                "This Month" => "monthly",
                "This Year" => "yearly",
                _ => "today"
            };

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
                    FactoryDefects = totals.FactoryDefects,
                    CustomerReturns = totals.CustomerReturns,
                    FillRatePct = Ratio(totals.Loaded, totals.OrdersPlaced),
                    SellThroughPct = Ratio(totals.Sold, totals.Loaded),
                    DamageRatePct = Ratio(totals.Damaged, totals.Loaded),
                    CollectionRatePct = Ratio(totals.Collected, totals.ExpectedRevenue)
                },
                Categories = lines.Select(line => new CategorySummaryApiDto
                {
                    CategoryId = line.ScopeKey,
                    CategoryName = line.ProductionCategoryName,
                    VolumeSharePct = line.ShareOfVolumePercent,
                    TotalSoldUnits = line.Totals.Sold,
                    GrossRevenue = line.Totals.ExpectedRevenue,
                    OutstandingBalance = line.Totals.OutstandingBalance,
                    Lifecycle = new CategoryLifecycleApiDto
                    {
                        Orders = line.Totals.OrdersPlaced,
                        Produced = line.Totals.Produced,
                        Loaded = line.Totals.Loaded,
                        Sold = line.Totals.Sold,
                        Returned = line.Totals.Returned,
                        Damaged = line.Totals.Damaged,
                        FactoryDefects = line.Totals.FactoryDefects,
                        CustomerReturns = line.Totals.CustomerReturns
                    }
                }).ToList(),
                TenderSplit = new TenderSplitApiDto
                {
                    Cash = tender.Cash,
                    Transfer = tender.BankTransfer,
                    Pos = tender.PosBank,
                    Outstanding = tender.Outstanding
                },
                AttentionAlerts = alerts.Select(alert => new AttentionAlertApiDto
                {
                    Type = string.IsNullOrWhiteSpace(alert.Type) ? "alert" : alert.Type,
                    Severity = alert.Severity,
                    Message = alert.Message
                }).ToList()
            };
        }

        public KpiDetailDto GetKpiDetail(string? metric, string? timeframe, string? scopeKey, DateTime? date = null)
        {
            var today = DateTime.UtcNow.Date;
            var anchor = (date ?? today).Date;
            if (anchor > today) anchor = today;

            var window = SuperAdminDashboardAggregator.ResolveWindow(anchor, today, timeframe);
            var (categoryId, scopeLabel) = ResolveDetailScope(scopeKey);
            var scope = string.IsNullOrWhiteSpace(scopeKey) ? "all" : scopeKey.Trim();

            var resolvedMetric = Enum.TryParse<LifecycleMetric>(metric, ignoreCase: true, out var parsed)
                ? parsed
                : LifecycleMetric.OrdersPlaced;

            var detail = resolvedMetric switch
            {
                LifecycleMetric.OrdersPlaced => BuildOrderDetail(window, categoryId, scopeLabel),
                LifecycleMetric.Produced => BuildProducedDetail(window, categoryId, scopeLabel),
                LifecycleMetric.FactoryDefects => BuildDefectDetail(window, categoryId, scopeLabel),
                LifecycleMetric.CustomerReturns => BuildReturnDetail(window, categoryId, scopeLabel),
                _ => BuildDispatchDetail(window, resolvedMetric, categoryId, scopeLabel)
            };

            detail.ScopeKey = scope;
            detail.ScopeLabel = scopeLabel;
            detail.PeriodKey = window.Key;
            detail.PeriodLabel = window.Label;
            detail.DateLabel = window.DateLabel;
            return detail;
        }

        private (int? CategoryId, string Label) ResolveDetailScope(string? scopeKey)
        {
            var key = (scopeKey ?? "all").Trim();
            const string prefix = "category-";
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(key[prefix.Length..], out var categoryId))
            {
                var name = _db.ProductionCategories
                    .AsNoTracking()
                    .Where(category => category.Id == categoryId)
                    .Select(category => category.Name)
                    .FirstOrDefault();

                return (categoryId, name ?? (categoryId == 0 ? "Uncategorised" : key));
            }

            return (null, "All Product Lines");
        }

        private KpiDetailDto BuildOrderDetail(DashboardPeriodWindow window, int? categoryId, string scopeLabel)
        {
            var endExclusive = window.EffectiveEnd.AddDays(1);
            var orders = _db.Orders
                .AsNoTracking()
                .Include(order => order.SalesRep)
                .Include(order => order.OrderItems)
                    .ThenInclude(item => item.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(order => order.OrderDate >= window.Start
                    && order.OrderDate < endExclusive
                    && order.Status != (int)OrderStatus.Cancelled)
                .ToList();

            var lines = new List<OrderDetailSource>();
            foreach (var order in orders)
            {
                foreach (var item in order.OrderItems)
                {
                    var category = item.Recipe?.ProductionProductType?.ProductionCategory;
                    var itemCategoryId = category?.Id ?? 0;
                    if (categoryId.HasValue && itemCategoryId != categoryId.Value) continue;

                    lines.Add(new OrderDetailSource
                    {
                        OrderId = order.Id,
                        OrderNumber = order.OrderNumber,
                        OrderDate = order.OrderDate,
                        CustomerName = order.CustomerName,
                        OrderType = ((OrderType)order.OrderType).ToString(),
                        Status = ((OrderStatus)order.Status).ToString(),
                        RepId = order.SalesRepId ?? 0,
                        RepName = order.SalesRepName ?? "Unassigned Rep",
                        Route = order.SalesRep?.RouteName ?? string.Empty,
                        Terminal = order.PosTerminalCode ?? string.Empty,
                        ProductName = item.ProductName,
                        ProductCode = item.Recipe?.Code ?? string.Empty,
                        CategoryId = itemCategoryId,
                        Category = category?.Name ?? "Uncategorised",
                        Unit = item.Recipe?.OutputUnit ?? "units",
                        Quantity = item.Quantity,
                        BonusQuantity = item.BonusQuantity,
                        UnitPrice = item.UnitPrice
                    });
                }
            }

            return SuperAdminKpiDetailBuilder.BuildOrders(lines, window.Key, window.DateLabel, scopeLabel);
        }

        private KpiDetailDto BuildProducedDetail(DashboardPeriodWindow window, int? categoryId, string scopeLabel)
        {
            var endExclusive = window.EffectiveEnd.AddDays(1);
            var batches = _db.ProductionBatches
                .AsNoTracking()
                .Include(batch => batch.ProductionBatchItems)
                    .ThenInclude(item => item.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(batch => batch.ProductionDate >= window.Start
                    && batch.ProductionDate < endExclusive
                    && batch.Status != (int)BatchStatus.Cancelled)
                .ToList();

            var sources = new List<BatchDetailSource>();
            foreach (var batch in batches)
            {
                foreach (var line in batch.ProductionBatchItems)
                {
                    var category = line.Recipe?.ProductionProductType?.ProductionCategory;
                    var itemCategoryId = category?.Id ?? 0;
                    if (categoryId.HasValue && itemCategoryId != categoryId.Value) continue;

                    sources.Add(new BatchDetailSource
                    {
                        BatchId = batch.Id,
                        BatchNumber = batch.BatchNumber,
                        ProductName = line.RecipeName,
                        CategoryId = itemCategoryId,
                        Category = category?.Name ?? "Uncategorised",
                        Unit = string.IsNullOrWhiteSpace(line.OutputUnitLabel)
                            ? line.Recipe?.OutputUnit ?? "units"
                            : line.OutputUnitLabel,
                        TargetQuantity = line.TargetQuantity,
                        ActualQuantity = line.ActualQuantity,
                        Shift = batch.Shift,
                        Baker = batch.BakerInCharge,
                        Status = ((BatchStatus)batch.Status).ToString(),
                        Date = batch.ProductionDate.Date,
                        UnitPrice = line.Recipe?.SellingPrice ?? 0m
                    });
                }
            }

            return SuperAdminKpiDetailBuilder.BuildProduced(sources, window.Key, window.DateLabel, scopeLabel);
        }

        private KpiDetailDto BuildDispatchDetail(
            DashboardPeriodWindow window,
            LifecycleMetric metric,
            int? categoryId,
            string scopeLabel)
        {
            var minStatus = metric == LifecycleMetric.Loaded
                ? (int)SalesReconciliationStatus.Draft
                : (int)SalesReconciliationStatus.Reconciled;

            var startDate = DateOnly.FromDateTime(window.Start);
            var endDate = DateOnly.FromDateTime(window.EffectiveEnd);

            var sheets = _db.SalesReconciliations
                .AsNoTracking()
                .Include(sheet => sheet.SalesRep)
                .Include(sheet => sheet.PosTerminal)
                .Include(sheet => sheet.SalesReconciliationItems)
                    .ThenInclude(item => item.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(sheet => sheet.ReconciliationDate >= startDate
                    && sheet.ReconciliationDate <= endDate
                    && sheet.Status >= minStatus)
                .ToList();

            var sources = new List<DispatchLineSource>();
            foreach (var sheet in sheets)
            {
                var vehicle = sheet.VehicleRegistration;

                foreach (var item in sheet.SalesReconciliationItems)
                {
                    var category = item.Recipe?.ProductionProductType?.ProductionCategory;
                    var itemCategoryId = category?.Id ?? 0;
                    if (categoryId.HasValue && itemCategoryId != categoryId.Value) continue;

                    sources.Add(new DispatchLineSource
                    {
                        RouteSheetId = sheet.Id,
                        RouteSheetRef = sheet.ReconciliationNumber,
                        RouteDate = sheet.ReconciliationDate,
                        Status = sheet.Status,
                        RepId = sheet.SalesRepId,
                        RepName = sheet.SalesRep?.FullName ?? "Unassigned Rep",
                        Route = sheet.RouteName,
                        Vehicle = vehicle,
                        Terminal = sheet.PosTerminal?.TerminalCode ?? string.Empty,
                        ProductName = item.ProductName,
                        ProductCode = item.ProductCode,
                        CategoryId = itemCategoryId,
                        Category = category?.Name ?? "Uncategorised",
                        Unit = item.Recipe?.OutputUnit ?? "units",
                        RequestedQty = item.QtySold,
                        LoadedQty = item.OpeningStock,
                        SoldQty = item.QtySold,
                        ClosingStockQty = item.ClosingStock ?? 0,
                        DamagedQty = item.QtyDamaged,
                        UnitPrice = item.UnitPrice
                    });
                }
            }

            return SuperAdminKpiDetailBuilder.BuildDispatch(
                sources, metric, window.Key, window.DateLabel, scopeLabel);
        }

        private KpiDetailDto BuildDefectDetail(DashboardPeriodWindow window, int? categoryId, string scopeLabel)
        {
            var endExclusive = window.EffectiveEnd.AddDays(1);
            var defects = _db.ProductionDefects
                .AsNoTracking()
                .Include(defect => defect.ProductionBatch)
                    .ThenInclude(batch => batch.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(defect => defect.ProductionBatch.ProductionDate >= window.Start
                    && defect.ProductionBatch.ProductionDate < endExclusive
                    && defect.ProductionBatch.Status != (int)BatchStatus.Cancelled)
                .ToList();

            var sources = new List<DefectDetailSource>();
            foreach (var defect in defects)
            {
                var batch = defect.ProductionBatch;
                var recipe = batch.Recipe;
                var category = recipe?.ProductionProductType?.ProductionCategory;
                var itemCategoryId = category?.Id ?? 0;
                if (categoryId.HasValue && itemCategoryId != categoryId.Value) continue;

                sources.Add(new DefectDetailSource
                {
                    DefectId = defect.Id,
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    RecordedAt = defect.RecordedAt,
                    ProductName = recipe?.Name ?? batch.RecipeName,
                    CategoryId = itemCategoryId,
                    Category = category?.Name ?? "Uncategorised",
                    Unit = string.IsNullOrWhiteSpace(batch.OutputUnitLabel)
                        ? recipe?.OutputUnit ?? "units"
                        : batch.OutputUnitLabel,
                    Quantity = defect.Quantity,
                    Reason = defect.DefectReason,
                    RecordedBy = defect.RecordedBy
                });
            }

            return SuperAdminKpiDetailBuilder.BuildDefects(sources, window.Key, window.DateLabel, scopeLabel);
        }

        private KpiDetailDto BuildReturnDetail(DashboardPeriodWindow window, int? categoryId, string scopeLabel)
        {
            var endExclusive = window.EffectiveEnd.AddDays(1);
            var returns = _db.CustomerReturns
                .AsNoTracking()
                .Include(customerReturn => customerReturn.Recipe)
                    .ThenInclude(recipe => recipe.ProductionProductType)
                        .ThenInclude(productType => productType!.ProductionCategory)
                .Include(customerReturn => customerReturn.SalesRep)
                .Include(customerReturn => customerReturn.Customer)
                .Where(customerReturn => customerReturn.ReturnDate >= window.Start
                    && customerReturn.ReturnDate < endExclusive)
                .ToList();

            var sources = new List<ReturnDetailSource>();
            foreach (var customerReturn in returns)
            {
                var category = customerReturn.Recipe?.ProductionProductType?.ProductionCategory;
                var itemCategoryId = category?.Id ?? 0;
                if (categoryId.HasValue && itemCategoryId != categoryId.Value) continue;

                sources.Add(new ReturnDetailSource
                {
                    ReturnId = customerReturn.Id,
                    ReturnNumber = customerReturn.ReturnNumber,
                    ReturnDate = customerReturn.ReturnDate,
                    RepId = customerReturn.SalesRepId ?? 0,
                    RepName = customerReturn.SalesRep?.FullName ?? "Unassigned Rep",
                    Route = customerReturn.SalesRep?.RouteName ?? string.Empty,
                    CustomerName = customerReturn.Customer?.Name ?? string.Empty,
                    ProductName = customerReturn.Recipe?.Name ?? string.Empty,
                    CategoryId = itemCategoryId,
                    Category = category?.Name ?? "Uncategorised",
                    Unit = customerReturn.Recipe?.OutputUnit ?? "units",
                    Quantity = customerReturn.Quantity,
                    ReplacementQuantity = customerReturn.ReplacementQuantity,
                    UnitPrice = customerReturn.UnitPrice,
                    RefundAmount = customerReturn.RefundAmount,
                    Reason = customerReturn.Reason,
                    ReplacementIssued = customerReturn.ReplacementIssued
                });
            }

            return SuperAdminKpiDetailBuilder.BuildReturns(
                sources, window.Key, window.DateLabel, scopeLabel);
        }

        private List<DashboardFact> BuildFacts(DateTime start, DateTime endExclusive)
        {
            var facts = new List<DashboardFact>();
            var startDate = DateOnly.FromDateTime(start);
            var endDate = DateOnly.FromDateTime(endExclusive.AddDays(-1));

            var sheets = _db.SalesReconciliations
                .AsNoTracking()
                .Include(sheet => sheet.SalesRep)
                .Include(sheet => sheet.PosTerminal)
                .Include(sheet => sheet.SalesReconciliationItems)
                    .ThenInclude(item => item.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(sheet => sheet.ReconciliationDate >= startDate && sheet.ReconciliationDate <= endDate)
                .ToList();

            foreach (var sheet in sheets)
            {
                var date = sheet.ReconciliationDate.ToDateTime(TimeOnly.MinValue);
                var repName = sheet.SalesRep?.FullName ?? "Unassigned Rep";
                var route = sheet.RouteName;
                var vehicle = sheet.VehicleRegistration;
                var terminal = sheet.PosTerminal?.TerminalCode ?? string.Empty;

                foreach (var item in sheet.SalesReconciliationItems)
                {
                    var category = item.Recipe?.ProductionProductType?.ProductionCategory;

                    // Verified quantities only exist after reconciliation. A draft
                    // sheet must not book sales, closing stock, damage or revenue.
                    var isReconciled = sheet.Status >= (int)SalesReconciliationStatus.Reconciled;

                    facts.Add(new DashboardFact
                    {
                        Date = date,
                        RepId = sheet.SalesRepId,
                        RepName = repName,
                        Route = route,
                        VehicleRegistration = vehicle,
                        Terminal = terminal,
                        RecipeId = item.RecipeId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        ProductionCategoryId = category?.Id ?? 0,
                        ProductionCategoryName = category?.Name ?? "Uncategorised",
                        OutputUnit = item.Recipe?.OutputUnit ?? "units",
                        UnitPrice = item.UnitPrice,
                        Loaded = item.OpeningStock,
                        Sold = isReconciled ? item.QtySold : 0,
                        ClosingStock = isReconciled ? item.ClosingStock ?? 0 : 0,
                        Damaged = isReconciled ? item.QtyDamaged : 0,
                        ExpectedRevenue = isReconciled ? item.QtySold * item.UnitPrice : 0m
                    });
                }

                facts.Add(new DashboardFact
                {
                    Date = date,
                    RepId = sheet.SalesRepId,
                    RepName = repName,
                    Route = route,
                    VehicleRegistration = vehicle,
                    Terminal = terminal,
                    Cash = sheet.CashHandedOver,
                    Transfer = 0m,
                    PosBank = sheet.PosTerminalReceipts,
                    Outstanding = sheet.Variance
                        ?? sheet.TotalExpectedRevenue - sheet.CashHandedOver - sheet.PosTerminalReceipts,
                    IsFinanceOnly = true
                });
            }

            // Orders Placed is the order register, not the rep's route request.
            // Cancelled orders never count, and order value is intentionally not
            // booked as revenue: revenue stays driven by verified route sales.
            var orders = _db.Orders
                .AsNoTracking()
                .Include(order => order.SalesRep)
                .Include(order => order.OrderItems)
                    .ThenInclude(item => item.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(order => order.OrderDate >= start
                    && order.OrderDate < endExclusive
                    && order.Status != (int)OrderStatus.Cancelled)
                .ToList();

            foreach (var order in orders)
            {
                foreach (var item in order.OrderItems)
                {
                    var category = item.Recipe?.ProductionProductType?.ProductionCategory;
                    facts.Add(new DashboardFact
                    {
                        Date = order.OrderDate.Date,
                        RepId = order.SalesRepId ?? 0,
                        RepName = order.SalesRepName ?? "Unassigned Rep",
                        Route = order.SalesRep?.RouteName ?? string.Empty,
                        Terminal = order.PosTerminalCode ?? string.Empty,
                        RecipeId = item.RecipeId,
                        ProductCode = item.Recipe?.Code ?? string.Empty,
                        ProductName = item.ProductName,
                        ProductionCategoryId = category?.Id ?? 0,
                        ProductionCategoryName = category?.Name ?? "Uncategorised",
                        OutputUnit = item.Recipe?.OutputUnit ?? "units",
                        UnitPrice = item.UnitPrice,
                        OrdersPlaced = item.Quantity,
                        ExpectedRevenue = 0m
                    });
                }
            }

            var batches = _db.ProductionBatches
                .AsNoTracking()
                .Include(batch => batch.ProductionBatchItems)
                    .ThenInclude(item => item.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(batch => batch.ProductionDate >= start
                    && batch.ProductionDate < endExclusive
                    && batch.Status != (int)BatchStatus.Cancelled)
                .ToList();

            foreach (var batch in batches)
            {
                foreach (var line in batch.ProductionBatchItems)
                {
                    var category = line.Recipe?.ProductionProductType?.ProductionCategory;
                    facts.Add(new DashboardFact
                    {
                        Date = batch.ProductionDate.Date,
                        RepId = 0,
                        RepName = "Plant",
                        RecipeId = line.RecipeId,
                        ProductCode = line.Recipe?.Code ?? line.ProductCode,
                        ProductName = line.RecipeName,
                        ProductionCategoryId = category?.Id ?? 0,
                        ProductionCategoryName = category?.Name ?? "Uncategorised",
                        OutputUnit = string.IsNullOrWhiteSpace(line.OutputUnitLabel)
                            ? line.Recipe?.OutputUnit ?? "units"
                            : line.OutputUnitLabel,
                        UnitPrice = line.Recipe?.SellingPrice ?? 0m,
                        Produced = line.ActualQuantity,
                        IsPlantLevel = true
                    });
                }
            }

            var defects = _db.ProductionDefects
                .AsNoTracking()
                .Include(defect => defect.ProductionBatch)
                    .ThenInclude(batch => batch.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(defect => defect.ProductionBatch.ProductionDate >= start
                    && defect.ProductionBatch.ProductionDate < endExclusive
                    && defect.ProductionBatch.Status != (int)BatchStatus.Cancelled)
                .ToList();

            foreach (var defect in defects)
            {
                var recipe = defect.ProductionBatch.Recipe;
                var category = recipe?.ProductionProductType?.ProductionCategory;
                facts.Add(new DashboardFact
                {
                    Date = defect.ProductionBatch.ProductionDate.Date,
                    RepId = 0,
                    RepName = "Plant",
                    RecipeId = defect.RecipeId,
                    ProductCode = recipe?.Code ?? string.Empty,
                    ProductName = recipe?.Name ?? defect.ProductionBatch.RecipeName,
                    ProductionCategoryId = category?.Id ?? 0,
                    ProductionCategoryName = category?.Name ?? "Uncategorised",
                    OutputUnit = recipe?.OutputUnit ?? defect.ProductionBatch.OutputUnitLabel,
                    UnitPrice = recipe?.SellingPrice ?? 0m,
                    FactoryDefects = defect.Quantity,
                    IsPlantLevel = true
                });
            }

            var returns = _db.CustomerReturns
                .AsNoTracking()
                .Include(customerReturn => customerReturn.Recipe)
                    .ThenInclude(recipe => recipe.ProductionProductType)
                        .ThenInclude(productType => productType!.ProductionCategory)
                .Include(customerReturn => customerReturn.SalesRep)
                .Where(customerReturn => customerReturn.ReturnDate >= start
                    && customerReturn.ReturnDate < endExclusive)
                .ToList();

            foreach (var customerReturn in returns)
            {
                var category = customerReturn.Recipe?.ProductionProductType?.ProductionCategory;
                facts.Add(new DashboardFact
                {
                    Date = customerReturn.ReturnDate.Date,
                    RepId = customerReturn.SalesRepId ?? -1,
                    RepName = customerReturn.SalesRep?.FullName ?? "Unassigned Rep",
                    Route = customerReturn.SalesRep?.RouteName ?? string.Empty,
                    RecipeId = customerReturn.RecipeId,
                    ProductCode = customerReturn.Recipe?.Code ?? string.Empty,
                    ProductName = customerReturn.Recipe?.Name ?? string.Empty,
                    ProductionCategoryId = category?.Id ?? 0,
                    ProductionCategoryName = category?.Name ?? "Uncategorised",
                    OutputUnit = customerReturn.Recipe?.OutputUnit ?? "units",
                    UnitPrice = customerReturn.UnitPrice,
                    CustomerReturns = customerReturn.Quantity
                });
            }

            return facts;
        }

        private List<DashboardCategory> BuildCategories(IReadOnlyList<DashboardFact> facts)
        {
            var configured = _db.ProductionCategories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.Name)
                .ToList();

            var categories = configured.Select(category => new DashboardCategory
            {
                Id = category.Id,
                Name = category.Name,
                ScopeKey = $"category-{category.Id}",
                Line = MapLine(category.Name),
                IconClass = IconFor(category.Name)
            }).ToList();

            foreach (var fact in facts.Where(fact => fact.ProductionCategoryId > 0))
            {
                if (categories.Any(category => category.Id == fact.ProductionCategoryId)) continue;
                categories.Add(new DashboardCategory
                {
                    Id = fact.ProductionCategoryId,
                    Name = fact.ProductionCategoryName,
                    ScopeKey = $"category-{fact.ProductionCategoryId}",
                    Line = MapLine(fact.ProductionCategoryName),
                    IconClass = IconFor(fact.ProductionCategoryName)
                });
            }

            if (facts.Any(fact => !fact.IsFinanceOnly && fact.ProductionCategoryId == 0))
            {
                categories.Add(new DashboardCategory
                {
                    Id = 0,
                    Name = "Uncategorised",
                    ScopeKey = "category-0",
                    Line = ProductType.Bread,
                    IconClass = "fa-solid fa-box"
                });
            }

            return categories
                .OrderBy(category => category.Name == "Uncategorised" ? 1 : 0)
                .ThenBy(category => category.Name)
                .ToList();
        }

        private List<DashboardSku> BuildSkus(IReadOnlyList<DashboardCategory> categories)
        {
            var recipes = _db.Recipes
                .AsNoTracking()
                .Include(recipe => recipe.ProductionProductType)
                    .ThenInclude(productType => productType!.ProductionCategory)
                .Where(recipe => recipe.IsActive)
                .OrderBy(recipe => recipe.Name)
                .ToList();

            return recipes.Select(recipe =>
            {
                var category = recipe.ProductionProductType?.ProductionCategory;
                var categoryId = category?.Id ?? 0;
                var categoryName = category?.Name ?? "Uncategorised";
                var match = categories.FirstOrDefault(item => item.Id == categoryId);
                return new DashboardSku
                {
                    Id = recipe.Id,
                    Code = recipe.Code,
                    Name = recipe.Name,
                    ProductionCategoryId = categoryId,
                    ProductionCategoryName = categoryName,
                    ScopeKey = match?.ScopeKey ?? $"category-{categoryId}",
                    Line = match?.Line ?? MapLine(categoryName),
                    OutputUnit = recipe.OutputUnit,
                    SellingPrice = recipe.SellingPrice
                };
            }).ToList();
        }

        private static decimal Ratio(decimal part, decimal whole) =>
            whole <= 0m ? 0m : Math.Round(part / whole * 100m, 1);

        private static ProductType MapLine(string categoryName)
        {
            if (categoryName.Contains("water", StringComparison.OrdinalIgnoreCase)) return ProductType.Water;
            if (categoryName.Contains("snack", StringComparison.OrdinalIgnoreCase)
                || categoryName.Contains("popcorn", StringComparison.OrdinalIgnoreCase)) return ProductType.Popcorn;
            return ProductType.Bread;
        }

        private static string IconFor(string categoryName)
        {
            if (categoryName.Contains("water", StringComparison.OrdinalIgnoreCase)) return "fa-solid fa-droplet";
            if (categoryName.Contains("snack", StringComparison.OrdinalIgnoreCase)
                || categoryName.Contains("popcorn", StringComparison.OrdinalIgnoreCase)) return "fa-solid fa-bowl-food";
            if (categoryName.Contains("bread", StringComparison.OrdinalIgnoreCase)) return "fa-solid fa-bread-slice";
            return "fa-solid fa-box";
        }
    }
}
