using Manufacture.Models.Domain;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests;

public class SuperAdminKpiDetailBuilderTests
{
    private static BatchDetailSource Batch(
        int id,
        string product,
        string category = "Bread",
        int categoryId = 1,
        int target = 100,
        int actual = 0,
        string unit = "loaves",
        string status = "Completed",
        decimal price = 100m) => new()
    {
        BatchId = id,
        BatchNumber = $"BTH-{id:0000}",
        ProductName = product,
        CategoryId = categoryId,
        Category = category,
        Unit = unit,
        TargetQuantity = target,
        ActualQuantity = actual,
        Shift = "Morning",
        Baker = "Baker One",
        Status = status,
        Date = new DateTime(2026, 11, 16),
        UnitPrice = price
    };

    private static DispatchLineSource Dispatch(
        int sheetId,
        int status,
        string product = "Jumbo Bread",
        int loaded = 0,
        int sold = 0,
        int closing = 0,
        int damaged = 0,
        int requested = 0,
        string rep = "Rep One",
        int repId = 1,
        string category = "Bread",
        int categoryId = 1) => new()
    {
        RouteSheetId = sheetId,
        RouteSheetRef = $"RTE-{sheetId:0000}",
        RouteDate = new DateOnly(2026, 11, 16),
        Status = status,
        RepId = repId,
        RepName = rep,
        Route = "Ikeja",
        Vehicle = "KJA-001",
        Terminal = "POS-01",
        ProductName = product,
        ProductCode = "BRD-JMB",
        CategoryId = categoryId,
        Category = category,
        Unit = "loaves",
        RequestedQty = requested,
        LoadedQty = loaded,
        SoldQty = sold,
        ClosingStockQty = closing,
        DamagedQty = damaged,
        UnitPrice = 100m
    };

    private static OrderDetailSource OrderLine(
        int orderId,
        string product,
        int quantity,
        decimal price = 100m,
        int repId = 1,
        string rep = "Rep One",
        int categoryId = 1,
        string category = "Bread",
        int bonus = 0) => new()
    {
        OrderId = orderId,
        OrderNumber = $"ORD-{orderId:0000}",
        OrderDate = new DateTime(2026, 11, 16, 9, 0, 0),
        CustomerName = "Corner Store",
        OrderType = "ImmediateSale",
        Status = "Pending",
        RepId = repId,
        RepName = rep,
        Route = "Ikeja",
        Terminal = "POS-01",
        ProductName = product,
        ProductCode = "BRD-JMB",
        CategoryId = categoryId,
        Category = category,
        Unit = "loaves",
        Quantity = quantity,
        BonusQuantity = bonus,
        UnitPrice = price
    };

    [Fact]
    public void BuildProduced_LeavesUnrecordedActualBlankAndOutOfTotals()
    {
        var detail = SuperAdminKpiDetailBuilder.BuildProduced(
        [
            Batch(1, "Jumbo Bread", actual: 90, status: "Completed"),
            Batch(2, "Family Loaf", actual: 0, status: "Planned", target: 50)
        ], "Today", "Mon, 16 Nov", "All Product Lines");

        Assert.Equal(90, detail.Total);
        Assert.Equal("loaves", detail.Unit);
        Assert.False(detail.IsTruncated);
        Assert.Equal(2, detail.RecordCount);

        var planned = detail.Groups.Single().Rows.Single(row => row.Reference == "BTH-0002");
        Assert.Null(planned.Quantity);
        Assert.Equal(50, planned.SecondaryQuantity);

        var completed = detail.Groups.Single().Rows.Single(row => row.Reference == "BTH-0001");
        Assert.Equal(90, completed.Quantity);

        Assert.Equal(90, detail.Groups.Single().Total);
    }

    [Fact]
    public void BuildProduced_GroupsByCategoryAndSortsByCategoryName()
    {
        var detail = SuperAdminKpiDetailBuilder.BuildProduced(
        [
            Batch(1, "Meat Pie", category: "Snacks", categoryId: 2, actual: 20, unit: "packs"),
            Batch(2, "Jumbo Bread", category: "Bread", categoryId: 1, actual: 30, unit: "loaves")
        ], "Today", "Mon, 16 Nov", "All Product Lines");

        Assert.Equal(["Bread", "Snacks"], detail.Groups.Select(group => group.Label));
        Assert.Equal(50, detail.Total);
        Assert.Equal("units", detail.Unit); // mixed loaves and packs
    }

    [Fact]
    public void BuildDispatch_LoadedIncludesAllReconciliations()
    {
        var detail = SuperAdminKpiDetailBuilder.BuildDispatch(
        [
            Dispatch(1, 0, loaded: 9),
            Dispatch(2, 0, loaded: 4),
            Dispatch(3, 1, loaded: 6)
        ], LifecycleMetric.Loaded, "Today", "Mon, 16 Nov", "All Product Lines");

        Assert.Equal(19, detail.Total);
        Assert.Equal(3, detail.RecordCount);
        Assert.Equal(3, detail.Groups.Single().Rows.Count);
        Assert.Contains(detail.Groups.SelectMany(group => group.Rows), row => row.Reference == "RTE-0001");

        var pending = detail.Groups.Single().Rows.Single(row => row.Reference == "RTE-0002");
        Assert.Equal("Draft", pending.Status);
        Assert.Equal("warning", pending.StatusTone);

        var reconciled = detail.Groups.Single().Rows.Single(row => row.Reference == "RTE-0003");
        Assert.Equal("Reconciled", reconciled.Status);
        Assert.Equal("success", reconciled.StatusTone);
    }

    [Fact]
    public void BuildDispatch_SoldOnlyCountsReconciledDispatches()
    {
        var detail = SuperAdminKpiDetailBuilder.BuildDispatch(
        [
            Dispatch(1, 0, loaded: 10, sold: 5),
            Dispatch(2, 1, loaded: 10, sold: 7)
        ], LifecycleMetric.Sold, "Today", "Mon, 16 Nov", "All Product Lines");

        Assert.Equal(7, detail.Total);
        Assert.Single(detail.Groups.Single().Rows);
        Assert.Equal(700m, detail.TotalValue);
    }

    [Fact]
    public void BuildDispatch_ClosingStockAndDamagedUseTheirOwnQuantities()
    {
        var lines = new List<DispatchLineSource>
        {
            Dispatch(1, 1, loaded: 10, closing: 3, damaged: 1)
        };

        var closing = SuperAdminKpiDetailBuilder.BuildDispatch(
            lines, LifecycleMetric.Returned, "Today", "Mon, 16 Nov", "All Product Lines");
        var damaged = SuperAdminKpiDetailBuilder.BuildDispatch(
            lines, LifecycleMetric.Damaged, "Today", "Mon, 16 Nov", "All Product Lines");

        Assert.Equal(3, closing.Total);
        Assert.Equal(10, closing.Groups.Single().Rows.Single().SecondaryQuantity);
        Assert.Equal(0m, closing.TotalValue);

        Assert.Equal(1, damaged.Total);
        Assert.Equal(100m, damaged.TotalValue);
    }

    [Fact]
    public void BuildOrders_MergesItemLinesPerOrderAndGroupsByRep()
    {
        var detail = SuperAdminKpiDetailBuilder.BuildOrders(
        [
            OrderLine(1, "Jumbo Bread", quantity: 10, price: 100m),
            OrderLine(1, "Meat Pie", quantity: 5, price: 50m),
            OrderLine(2, "Jumbo Bread", quantity: 2, price: 100m, repId: 0, rep: "Unassigned Rep")
        ], "Today", "Mon, 16 Nov", "All Product Lines");

        Assert.Equal(17, detail.Total);
        Assert.Equal(2, detail.RecordCount); // two orders across three item lines

        var repGroup = detail.Groups[0];
        Assert.Equal("Rep One", repGroup.Label);
        Assert.Equal(1, repGroup.RecordCount);
        var merged = Assert.Single(repGroup.Rows);
        Assert.Equal(15, merged.Quantity);
        Assert.Equal(1250m, merged.Amount);
        Assert.Contains("Jumbo Bread", merged.Product);
        Assert.Contains("Meat Pie", merged.Product);

        Assert.Equal("Unassigned Rep", detail.Groups[^1].Label);
    }

    [Fact]
    public void BuildOrders_TruncatesRowsButKeepsTrueTotals()
    {
        var lines = Enumerable.Range(1, SuperAdminKpiDetailBuilder.MaxRowsPerDetail + 5)
            .Select(id => OrderLine(id, "Jumbo Bread", quantity: 1))
            .ToList();

        var detail = SuperAdminKpiDetailBuilder.BuildOrders(lines, "This Year", "2026", "All Product Lines");

        Assert.True(detail.IsTruncated);
        Assert.Equal(SuperAdminKpiDetailBuilder.MaxRowsPerDetail + 5, detail.Total);
        Assert.Equal(SuperAdminKpiDetailBuilder.MaxRowsPerDetail + 5, detail.RecordCount);
        Assert.Equal(SuperAdminKpiDetailBuilder.MaxRowsPerDetail, detail.Groups.Sum(group => group.Rows.Count));
    }

    [Fact]
    public void BuildReturns_GroupsByRepAndCarriesRefundValue()
    {
        var detail = SuperAdminKpiDetailBuilder.BuildReturns(
        [
            new ReturnDetailSource
            {
                ReturnId = 1, ReturnNumber = "RET-0001", ReturnDate = new DateTime(2026, 11, 16),
                RepId = 1, RepName = "Rep One", Route = "Ikeja", CustomerName = "Corner Store",
                ProductName = "Jumbo Bread", CategoryId = 1, Category = "Bread", Unit = "loaves",
                Quantity = 2, UnitPrice = 100m, Reason = "Expired", ReplacementIssued = true,
                ReplacementQuantity = 2
            },
            new ReturnDetailSource
            {
                ReturnId = 2, ReturnNumber = "RET-0002", ReturnDate = new DateTime(2026, 11, 16),
                RepId = 1, RepName = "Rep One", Route = "Ikeja", CustomerName = "Corner Store",
                ProductName = "Jumbo Bread", CategoryId = 1, Category = "Bread", Unit = "loaves",
                Quantity = 1, UnitPrice = 100m, Reason = "Spoiled"
            }
        ], "Today", "Mon, 16 Nov", "All Product Lines");

        Assert.Equal(3, detail.Total);
        Assert.Equal(300m, detail.TotalValue);
        Assert.Equal(2, detail.RecordCount);
        Assert.Equal("replaced", detail.Groups.Single().Rows[0].Status.ToLowerInvariant());
    }

    [Fact]
    public void BuildDefects_GroupsByCategory()
    {
        var detail = SuperAdminKpiDetailBuilder.BuildDefects(
        [
            new DefectDetailSource
            {
                DefectId = 1, BatchId = 1, BatchNumber = "BTH-0001", RecordedAt = new DateTime(2026, 11, 16),
                ProductName = "Jumbo Bread", CategoryId = 1, Category = "Bread", Unit = "loaves",
                Quantity = 4, Reason = "Burnt", RecordedBy = "Baker One"
            },
            new DefectDetailSource
            {
                DefectId = 2, BatchId = 1, BatchNumber = "BTH-0001", RecordedAt = new DateTime(2026, 11, 16),
                ProductName = "Jumbo Bread", CategoryId = 1, Category = "Bread", Unit = "loaves",
                Quantity = 3, Reason = "Underweight", RecordedBy = "Baker One"
            }
        ], "Today", "Mon, 16 Nov", "All Product Lines");

        Assert.Equal(7, detail.Total);
        Assert.Single(detail.Groups);
        Assert.Equal(2, detail.Groups[0].Rows.Count);
        Assert.Equal("warning", detail.Groups[0].Rows[0].StatusTone);
    }
}
