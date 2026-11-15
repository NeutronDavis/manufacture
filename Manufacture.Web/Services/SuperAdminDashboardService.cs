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

        private List<DashboardFact> BuildFacts(DateTime start, DateTime endExclusive)
        {
            var facts = new List<DashboardFact>();
            var startDate = DateOnly.FromDateTime(start);
            var endDate = DateOnly.FromDateTime(endExclusive.AddDays(-1));

            var sheets = _db.DailyRepRouteSheets
                .AsNoTracking()
                .Include(sheet => sheet.SalesRep)
                .Include(sheet => sheet.Vehicle)
                .Include(sheet => sheet.PosTerminal)
                .Include(sheet => sheet.RouteSheetItems)
                    .ThenInclude(item => item.Recipe)
                        .ThenInclude(recipe => recipe.ProductionProductType)
                            .ThenInclude(productType => productType!.ProductionCategory)
                .Where(sheet => sheet.RouteDate >= startDate && sheet.RouteDate <= endDate)
                .ToList();

            foreach (var sheet in sheets)
            {
                var date = sheet.RouteDate.ToDateTime(TimeOnly.MinValue);
                var repName = sheet.SalesRep?.FullName ?? "Unassigned Rep";
                var route = sheet.RouteName;
                var vehicle = sheet.VehicleRegistration ?? sheet.Vehicle?.RegistrationNumber ?? string.Empty;
                var terminal = sheet.PosTerminalCode ?? sheet.PosTerminal?.TerminalCode ?? string.Empty;

                foreach (var item in sheet.RouteSheetItems)
                {
                    var category = item.Recipe?.ProductionProductType?.ProductionCategory;
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
                        UnitPrice = item.UnitSellingPrice,
                        OrdersPlaced = item.RepOrderRequestedQty,
                        Loaded = item.TodayNewlyLoadedQty,
                        Sold = item.QtySold,
                        ClosingStock = item.ClosingStockQty,
                        Damaged = item.QtyDamaged,
                        ExpectedRevenue = item.QtySold * item.UnitSellingPrice
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
                    Cash = sheet.CashCollected,
                    Transfer = sheet.BankTransferCollected,
                    PosBank = sheet.PosTerminalCollected,
                    Outstanding = sheet.NewOutstandingAccountBalance
                        ?? Math.Max(0m, sheet.PreviousAccountBalance + sheet.TotalExpectedSalesRevenue
                            - sheet.CashCollected - sheet.BankTransferCollected - sheet.PosTerminalCollected),
                    IsFinanceOnly = true
                });
            }

            var batches = _db.ProductionBatches
                .AsNoTracking()
                .Include(batch => batch.Recipe)
                    .ThenInclude(recipe => recipe.ProductionProductType)
                        .ThenInclude(productType => productType!.ProductionCategory)
                .Where(batch => batch.ProductionDate >= start
                    && batch.ProductionDate < endExclusive
                    && batch.Status != (int)BatchStatus.Cancelled)
                .ToList();

            foreach (var batch in batches)
            {
                var category = batch.Recipe?.ProductionProductType?.ProductionCategory;
                facts.Add(new DashboardFact
                {
                    Date = batch.ProductionDate.Date,
                    RepId = 0,
                    RepName = "Plant",
                    RecipeId = batch.RecipeId,
                    ProductCode = batch.Recipe?.Code ?? string.Empty,
                    ProductName = batch.RecipeName,
                    ProductionCategoryId = category?.Id ?? 0,
                    ProductionCategoryName = category?.Name ?? "Uncategorised",
                    OutputUnit = string.IsNullOrWhiteSpace(batch.OutputUnitLabel)
                        ? batch.Recipe?.OutputUnit ?? "units"
                        : batch.OutputUnitLabel,
                    UnitPrice = batch.Recipe?.SellingPrice ?? 0m,
                    Produced = batch.ActualQuantity,
                    IsPlantLevel = true
                });
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
