using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests;

public class SuperAdminDashboardAggregatorTests
{
    private static readonly DateTime Anchor = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Today = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

    private static readonly List<DashboardCategory> Categories =
    [
        new() { Id = 1, Name = "Bread", ScopeKey = "category-1", Line = ProductType.Bread, IconClass = "fa-solid fa-bread-slice" },
        new() { Id = 2, Name = "Snacks", ScopeKey = "category-2", Line = ProductType.Popcorn, IconClass = "fa-solid fa-popcorn" },
        new() { Id = 3, Name = "Drinks", ScopeKey = "category-3", Line = ProductType.Bread, IconClass = "fa-solid fa-box" }
    ];

    private static readonly List<DashboardSku> Skus =
    [
        new() { Id = 10, Code = "BRD-JMB", Name = "Jumbo Bread", ProductionCategoryId = 1, ProductionCategoryName = "Bread", ScopeKey = "category-1", Line = ProductType.Bread, OutputUnit = "loaves", SellingPrice = 100m },
        new() { Id = 20, Code = "SNK-PIE", Name = "Meat Pie", ProductionCategoryId = 2, ProductionCategoryName = "Snacks", ScopeKey = "category-2", Line = ProductType.Popcorn, OutputUnit = "packs", SellingPrice = 100m }
    ];

    [Fact]
    public void Build_UsesDatabaseFactsForTheFourPeriods()
    {
        var overview = BuildOverview(Facts());

        var today = overview.Periods.Single(period => period.Key == "Today").Totals;
        Assert.Equal(100, today.OrdersPlaced);
        Assert.Equal(90, today.Produced);
        Assert.Equal(80, today.Loaded);
        Assert.Equal(5, today.FactoryDefects);
        Assert.Equal(70, today.Sold);
        Assert.Equal(8, today.Returned);
        Assert.Equal(8, today.ClosingStock);
        Assert.Equal(2, today.Damaged);
        Assert.Equal(1, today.CustomerReturns);
        Assert.Equal(7000m, today.ExpectedRevenue);

        var week = overview.Periods.Single(period => period.Key == "This Week").Totals;
        Assert.Equal(150, week.OrdersPlaced);
        Assert.Equal(100, week.Sold);

        var month = overview.Periods.Single(period => period.Key == "This Month").Totals;
        Assert.Equal(170, month.OrdersPlaced);

        var year = overview.Periods.Single(period => period.Key == "This Year").Totals;
        Assert.Equal(180, year.OrdersPlaced);
    }

    [Fact]
    public void Build_GroupsProductLinesByProductionCategory()
    {
        var overview = BuildOverview(Facts());
        var weekLines = overview.ProductLinesByPeriod["This Week"];

        var bread = weekLines.Single(line => line.ProductionCategoryName == "Bread");
        var snacks = weekLines.Single(line => line.ProductionCategoryName == "Snacks");

        Assert.Equal(70, bread.Totals.Sold);
        Assert.Equal(30, snacks.Totals.Sold);
        Assert.Equal(70m, bread.ShareOfVolumePercent);
        Assert.Equal(30m, snacks.ShareOfVolumePercent);
        Assert.Contains("Jumbo Bread", bread.SkuNames);
        Assert.Contains("Meat Pie", snacks.SkuNames);
        Assert.Equal("category-1", bread.ScopeKey);
    }

    [Fact]
    public void Build_IncludesNewActiveCategoryBeforeItHasProducts()
    {
        var overview = BuildOverview(Facts());
        var drinks = overview.ProductLinesByPeriod["Today"]
            .Single(line => line.ProductionCategoryName == "Drinks");

        Assert.Equal("category-3", drinks.ScopeKey);
        Assert.Equal(0, drinks.Totals.OrdersPlaced);
        Assert.Equal(0, drinks.Totals.Sold);
        Assert.Empty(drinks.SkuNames);
        Assert.Contains(overview.DrillDownScopes, scope => scope.ScopeKey == "category-3");
        Assert.Contains(overview.ProductDrillDownScopes, scope => scope.ScopeKey == "category-3");
        Assert.Contains(overview.StageDrillDownScopes, scope => scope.ScopeKey == "category-3");
    }

    [Fact]
    public void Build_NewCategoryDataUsesItsOwnScopeAndDoesNotInheritBread()
    {
        var facts = Facts();
        facts.Add(new DashboardFact
        {
            Date = new DateTime(2026, 11, 13),
            RepId = 1,
            RepName = "Rep One",
            Route = "Ikeja",
            RecipeId = 30,
            ProductCode = "DRK-001",
            ProductName = "Bottled Drink",
            ProductionCategoryId = 3,
            ProductionCategoryName = "Drinks",
            OutputUnit = "bottles",
            UnitPrice = 150m,
            OrdersPlaced = 12,
            Loaded = 12,
            Sold = 10,
            ClosingStock = 2,
            ExpectedRevenue = 1500m
        });

        var overview = SuperAdminDashboardAggregator.Build(Anchor, Today, facts, Categories, Skus);
        var drinks = overview.ProductLinesByPeriod["Today"]
            .Single(line => line.ProductionCategoryName == "Drinks");

        Assert.Equal(10, drinks.Totals.Sold);
        Assert.Equal("category-3", drinks.ScopeKey);
        Assert.Equal("Drinks", drinks.LineLabel);

        var allStage = overview.StageDrillDownScopes.Single(scope => scope.ScopeKey == "all");
        var drinksRow = allStage.Metrics["Sold"]["Today"].Rows
            .Single(row => row.CategoryLabel == "Drinks");
        Assert.Equal("Rep One", drinksRow.GroupLabel);
        Assert.Equal(10, drinksRow.MetricValue);

        var drinksScope = overview.StageDrillDownScopes
            .Single(scope => scope.ScopeKey == "category-3");
        var drinksScopeRow = drinksScope.Metrics["Sold"]["Today"].Rows.Single();
        Assert.Equal("Drinks", drinksScopeRow.CategoryLabel);
        Assert.Equal(10, drinksScopeRow.MetricValue);
    }

    [Fact]
    public void Build_GroupsRepStagesByRepAndCategoryAndPlantStagesByCategory()
    {
        var overview = BuildOverview(Facts());
        var allScope = overview.StageDrillDownScopes.Single(scope => scope.ScopeKey == "all");

        var orders = allScope.Metrics["OrdersPlaced"]["Today"];
        Assert.Single(orders.Rows);
        Assert.Equal("Rep One", orders.Rows[0].GroupLabel);
        Assert.Equal("Bread", orders.Rows[0].CategoryLabel);
        Assert.Equal(100, orders.Rows[0].MetricValue);

        var produced = allScope.Metrics["Produced"]["Today"];
        Assert.Single(produced.Rows);
        Assert.Equal("Bread", produced.Rows[0].GroupLabel);
        Assert.Equal(90, produced.Rows[0].MetricValue);

        var defects = allScope.Metrics["FactoryDefects"]["Today"];
        Assert.Single(defects.Rows);
        Assert.Equal("Bread", defects.Rows[0].GroupLabel);
        Assert.Equal(5, defects.Rows[0].MetricValue);

        var closing = allScope.Metrics["Returned"]["Today"];
        Assert.Single(closing.Rows);
        Assert.Equal("Rep One", closing.Rows[0].GroupLabel);
        Assert.Equal(8, closing.Rows[0].MetricValue);

        var customerReturns = allScope.Metrics["CustomerReturns"]["Today"];
        Assert.Single(customerReturns.Rows);
        Assert.Equal("Rep One", customerReturns.Rows[0].GroupLabel);
        Assert.Equal(1, customerReturns.Rows[0].MetricValue);
    }

    [Fact]
    public void Build_KeepsClosedStockOutOfCustomerReturns()
    {
        var overview = BuildOverview(Facts());
        var today = overview.Periods.Single(period => period.Key == "Today").Totals;

        Assert.Equal(8, today.ClosingStock);
        Assert.Equal(1, today.CustomerReturns);
    }

    [Fact]
    public void Build_ReturnsZeroesForAnEmptyDay()
    {
        var overview = SuperAdminDashboardAggregator.Build(
            Anchor,
            Today,
            Array.Empty<DashboardFact>(),
            Categories,
            Skus);

        foreach (var period in overview.Periods)
        {
            Assert.Equal(0, period.Totals.OrdersPlaced);
            Assert.Equal(0, period.Totals.Produced);
            Assert.Equal(0, period.Totals.Sold);
            Assert.Equal(0, period.Totals.CustomerReturns);
            Assert.Equal(0m, period.Totals.ExpectedRevenue);
        }

        Assert.All(overview.ProductLines, line =>
        {
            Assert.Equal(0, line.Totals.Sold);
            Assert.Equal(0m, line.ShareOfVolumePercent);
        });
    }

    [Fact]
    public void ResolveWindow_UsesTheSameClampedWindowAsTheKpiCards()
    {
        var anchor = new DateTime(2026, 11, 16);
        var futureInWeek = anchor.AddDays(4);

        var facts = new List<DashboardFact>
        {
            new()
            {
                Date = futureInWeek,
                RepId = 1,
                RepName = "Rep One",
                RecipeId = 10,
                OrdersPlaced = 25,
                ProductionCategoryId = 1,
                ProductionCategoryName = "Bread"
            }
        };

        var overview = SuperAdminDashboardAggregator.Build(anchor, anchor, facts, Categories, Skus);
        var weekCard = overview.Periods.Single(period => period.Key == "This Week");
        Assert.Equal(0, weekCard.Totals.OrdersPlaced);

        foreach (var (timeframe, key) in new[]
                 {
                     ("today", "Today"),
                     ("weekly", "This Week"),
                     ("monthly", "This Month"),
                     ("yearly", "This Year")
                 })
        {
            var card = overview.Periods.Single(period => period.Key == key);
            var window = SuperAdminDashboardAggregator.ResolveWindow(anchor, anchor, timeframe);

            Assert.Equal(card.Key, window.Key);
            Assert.Equal(card.Label, window.Label);
            Assert.Equal(card.ReportsTimeframe, window.ReportsTimeframe);
            Assert.True(window.Start >= new DateTime(anchor.Year, 1, 1));
            Assert.True(window.EffectiveEnd <= anchor);
        }

        Assert.Equal(anchor, SuperAdminDashboardAggregator.ResolveWindow(anchor, anchor, "today").EffectiveEnd);
        Assert.Equal(new DateTime(2026, 1, 1), SuperAdminDashboardAggregator.ResolveWindow(anchor, anchor, "yearly").Start);
        Assert.Equal(anchor.AddDays(-(int)anchor.DayOfWeek), SuperAdminDashboardAggregator.ResolveWindow(anchor, anchor, "weekly").Start);
    }

    private static ExecutiveOverviewDto BuildOverview(List<DashboardFact> facts) =>
        SuperAdminDashboardAggregator.Build(Anchor, Today, facts, Categories, Skus);

    private static List<DashboardFact> Facts()
    {
        return
        [
            new()
            {
                Date = new DateTime(2026, 11, 13),
                RepId = 1,
                RepName = "Rep One",
                Route = "Ikeja",
                VehicleRegistration = "KJA-001",
                Terminal = "POS-01",
                RecipeId = 10,
                ProductCode = "BRD-JMB",
                ProductName = "Jumbo Bread",
                ProductionCategoryId = 1,
                ProductionCategoryName = "Bread",
                OutputUnit = "loaves",
                UnitPrice = 100m,
                OrdersPlaced = 100,
                Loaded = 80,
                Sold = 70,
                ClosingStock = 8,
                Damaged = 2,
                CustomerReturns = 1,
                ExpectedRevenue = 7000m,
                Cash = 5000m,
                Transfer = 1000m,
                PosBank = 500m,
                Outstanding = 500m
            },
            new()
            {
                Date = new DateTime(2026, 11, 13),
                RepId = 0,
                RepName = "Plant",
                RecipeId = 10,
                ProductCode = "BRD-JMB",
                ProductName = "Jumbo Bread",
                ProductionCategoryId = 1,
                ProductionCategoryName = "Bread",
                OutputUnit = "loaves",
                Produced = 90,
                IsPlantLevel = true
            },
            new()
            {
                Date = new DateTime(2026, 11, 13),
                RepId = 0,
                RepName = "Plant",
                RecipeId = 10,
                ProductCode = "BRD-JMB",
                ProductName = "Jumbo Bread",
                ProductionCategoryId = 1,
                ProductionCategoryName = "Bread",
                OutputUnit = "loaves",
                FactoryDefects = 5,
                IsPlantLevel = true
            },
            new()
            {
                Date = new DateTime(2026, 11, 9),
                RepId = 2,
                RepName = "Rep Two",
                Route = "Yaba",
                RecipeId = 20,
                ProductCode = "SNK-PIE",
                ProductName = "Meat Pie",
                ProductionCategoryId = 2,
                ProductionCategoryName = "Snacks",
                OutputUnit = "packs",
                UnitPrice = 100m,
                OrdersPlaced = 50,
                Loaded = 40,
                Sold = 30,
                ClosingStock = 5,
                ExpectedRevenue = 3000m,
                Cash = 2500m,
                Transfer = 300m,
                PosBank = 100m,
                Outstanding = 100m
            },
            new()
            {
                Date = new DateTime(2026, 11, 2),
                RepId = 1,
                RepName = "Rep One",
                Route = "Ikeja",
                RecipeId = 10,
                ProductCode = "BRD-JMB",
                ProductName = "Jumbo Bread",
                ProductionCategoryId = 1,
                ProductionCategoryName = "Bread",
                OutputUnit = "loaves",
                OrdersPlaced = 20,
                Loaded = 20,
                Sold = 18,
                ClosingStock = 2,
                ExpectedRevenue = 1800m
            },
            new()
            {
                Date = new DateTime(2026, 3, 1),
                RepId = 2,
                RepName = "Rep Two",
                Route = "Yaba",
                RecipeId = 20,
                ProductCode = "SNK-PIE",
                ProductName = "Meat Pie",
                ProductionCategoryId = 2,
                ProductionCategoryName = "Snacks",
                OutputUnit = "packs",
                OrdersPlaced = 10,
                Loaded = 10,
                Sold = 9,
                ClosingStock = 1,
                ExpectedRevenue = 900m
            }
        ];
    }
}
