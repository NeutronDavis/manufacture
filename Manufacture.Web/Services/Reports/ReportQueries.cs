using Manufacture.Library.Data;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using BatchStatus = Manufacture.Models.Domain.BatchStatus;

namespace Manufacture.Services.Reports
{
    /// <summary>
    /// Database-backed report handlers. Every Phase 1 report is represented by
    /// one method here and one definition in <see cref="ReportCatalog"/>.
    /// </summary>
    public static class ReportQueries
    {
        public static async Task<ReportViewResultDto> ExecuteAsync(
            ManufactureDbContext db,
            ReportQueryContext context,
            CancellationToken cancellationToken = default)
        {
            return context.Definition.Key switch
            {
                "exec-daily-funnel" => await ExecutiveDailyFunnel(db, context, cancellationToken),
                "exec-product-line" => await ExecutiveProductLine(db, context, cancellationToken),
                "exec-rep-performance" => await ExecutiveRepPerformance(db, context, cancellationToken),
                "exec-revenue-cash-debt" => await ExecutiveRevenueCashDebt(db, context, cancellationToken),
                "exec-unit-economics" => await ExecutiveUnitEconomics(db, context, cancellationToken),
                "sales-register" => await SalesRegister(db, context, cancellationToken),
                "sales-product" => await SalesProduct(db, context, cancellationToken),
                "sales-customer-ledger" => await SalesCustomerLedger(db, context, cancellationToken),
                "sales-ar-ageing" => await SalesArAgeing(db, context, cancellationToken),
                "sales-collections" => await SalesCollections(db, context, cancellationToken),
                "sales-route-performance" => await SalesRoutePerformance(db, context, cancellationToken),
                "sales-loading-shortfall" => await SalesLoadingShortfall(db, context, cancellationToken),
                "sales-closing-damage" => await SalesClosingDamage(db, context, cancellationToken),
                "sales-customer-returns" => await SalesCustomerReturns(db, context, cancellationToken),
                "sales-supermarket-credit" => await SalesSupermarketCredit(db, context, cancellationToken),
                "sales-bonus-discount" => await SalesBonusDiscount(db, context, cancellationToken),
                "sales-fulfillment" => await SalesFulfillment(db, context, cancellationToken),
                "inventory-stock-valuation" => await InventoryStockValuation(db, context, cancellationToken),
                "inventory-low-stock" => await InventoryLowStock(db, context, cancellationToken),
                "inventory-movements" => await InventoryMovements(db, context, cancellationToken),
                "inventory-requisitions" => await InventoryRequisitions(db, context, cancellationToken),
                "inventory-requisition-fill" => await InventoryRequisitionFill(db, context, cancellationToken),
                "inventory-department-consumption" => await InventoryDepartmentConsumption(db, context, cancellationToken),
                "inventory-receipts-issues" => await InventoryReceiptsIssues(db, context, cancellationToken),
                "inventory-slow-moving" => await InventorySlowMoving(db, context, cancellationToken),
                "inventory-supplier-price" => await InventorySupplierPrice(db, context, cancellationToken),
                "production-daily" => await ProductionDaily(db, context, cancellationToken),
                "production-yield" => await ProductionYield(db, context, cancellationToken),
                "production-batch-cost" => await ProductionBatchCost(db, context, cancellationToken),
                "production-recipe-margin" => await ProductionRecipeMargin(db, context, cancellationToken),
                "production-defects" => await ProductionDefects(db, context, cancellationToken),
                "production-price-audit" => await ProductionPriceAudit(db, context, cancellationToken),
                "production-volume" => await ProductionVolume(db, context, cancellationToken),
                "production-shift-baker" => await ProductionShiftBaker(db, context, cancellationToken),
                "hr-headcount" => await HrHeadcount(db, context, cancellationToken),
                "hr-hires-terminations" => await HrHiresTerminations(db, context, cancellationToken),
                "payroll-register" => await PayrollRegister(db, context, cancellationToken),
                "payroll-summary" => await PayrollSummary(db, context, cancellationToken),
                "payroll-deductions" => await PayrollDeductions(db, context, cancellationToken),
                "payroll-advances" => await PayrollAdvances(db, context, cancellationToken),
                "payroll-reconciliation" => await PayrollReconciliation(db, context, cancellationToken),
                "payroll-bank-schedule" => await PayrollBankSchedule(db, context, cancellationToken),
                "payroll-compliance" => await PayrollCompliance(db, context, cancellationToken),
                "payroll-cost-per-unit" => await PayrollCostPerUnit(db, context, cancellationToken),
                "fleet-vehicles" => await FleetVehicles(db, context, cancellationToken),
                "fleet-fuel" => await FleetFuel(db, context, cancellationToken),
                "fleet-maintenance" => await FleetMaintenance(db, context, cancellationToken),
                "fleet-service-due" => await FleetServiceDue(db, context, cancellationToken),
                "fleet-weekly-cost" => await FleetWeeklyCost(db, context, cancellationToken),
                "fleet-charter" => await FleetCharter(db, context, cancellationToken),
                "fleet-utilization" => await FleetUtilization(db, context, cancellationToken),
                "fleet-cost-per-unit" => await FleetCostPerUnit(db, context, cancellationToken),
                "fleet-trip-incidentals" => await FleetTripIncidentals(db, context, cancellationToken),
                "fleet-fuel-comparison" => await FleetFuelComparison(db, context, cancellationToken),
                "admin-user-access" => await AdminUserAccess(db, context, cancellationToken),
                "admin-role-distribution" => await AdminRoleDistribution(db, context, cancellationToken),
                "admin-rbac-matrix" => await AdminRbacMatrix(db, context, cancellationToken),
                "admin-permission-audit" => await AdminPermissionAudit(db, context, cancellationToken),
                "admin-dormant-users" => await AdminDormantUsers(db, context, cancellationToken),
                "admin-login-coverage" => await AdminLoginCoverage(db, context, cancellationToken),
                "admin-master-data" => await AdminMasterData(db, context, cancellationToken),
                "admin-configuration" => await AdminConfiguration(db, context, cancellationToken),
                "admin-audit-extract" => await AdminAuditExtract(db, context, cancellationToken),
                "admin-data-integrity" => await AdminDataIntegrity(db, context, cancellationToken),
                "sales-terminal-productivity" => await SalesTerminalProductivity(db, context, cancellationToken),
                "sales-preorders" => await SalesPreOrders(db, context, cancellationToken),
                "sales-price-realization" => await SalesPriceRealization(db, context, cancellationToken),
                "inventory-turnover" => await InventoryTurnover(db, context, cancellationToken),
                "inventory-density-coverage" => await InventoryDensityCoverage(db, context, cancellationToken),
                "inventory-requisition-cycle" => await InventoryRequisitionCycle(db, context, cancellationToken),
                "inventory-theoretical-actual" => await InventoryTheoreticalActual(db, context, cancellationToken),
                "production-batch-status" => await ProductionBatchStatus(db, context, cancellationToken),
                "production-bom-cost" => await ProductionBomCost(db, context, cancellationToken),
                "production-demand" => await ProductionDemand(db, context, cancellationToken),
                "production-to-sales" => await ProductionToSales(db, context, cancellationToken),
                "exec-management-pack" => await ExecutiveManagementPack(db, context, cancellationToken),
                "exec-working-capital" => await ExecutiveWorkingCapital(db, context, cancellationToken),
                "exec-integrity-pack" => await ExecutiveIntegrityPack(db, context, cancellationToken),
                "exec-alerts" => await ExecutiveAlerts(db, context, cancellationToken),
                _ => Empty(context)
            };
        }

        private static async Task<ReportViewResultDto> ExecutiveDailyFunnel(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date;
            var end = ctx.Filters.EndDate.Date.AddDays(1);
            var items = await db.RouteSheetItems.AsNoTracking()
                .Include(i => i.RouteSheet)
                .Include(i => i.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(i => i.RouteSheet.RouteDate >= DateOnly.FromDateTime(start) && i.RouteSheet.RouteDate < DateOnly.FromDateTime(end))
                .ToListAsync(ct);
            var batches = await db.ProductionBatches.AsNoTracking()
                .Include(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled)
                .ToListAsync(ct);
            var defects = await db.ProductionDefects.AsNoTracking()
                .Include(d => d.ProductionBatch).ThenInclude(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(d => d.ProductionBatch.ProductionDate >= start && d.ProductionBatch.ProductionDate < end && d.ProductionBatch.Status != (int)BatchStatus.Cancelled)
                .ToListAsync(ct);
            var returns = await db.CustomerReturns.AsNoTracking()
                .Include(r => r.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(r => r.ReturnDate >= start && r.ReturnDate < end)
                .ToListAsync(ct);

            var rows = new Dictionary<int, Dictionary<string, object?>>();
            foreach (var item in items)
            {
                var c = item.Recipe?.ProductionProductType?.ProductionCategory;
                var id = c?.Id ?? 0;
                Row(rows, id, "Category", c?.Name ?? "Uncategorised");
                Add(rows, id, "Orders", item.RepOrderRequestedQty);
                Add(rows, id, "Loaded", item.TodayNewlyLoadedQty);
                Add(rows, id, "Sold", item.QtySold);
                Add(rows, id, "Closing", item.ClosingStockQty);
                Add(rows, id, "Damaged", item.QtyDamaged);
            }
            foreach (var batch in batches)
            {
                var c = batch.Recipe?.ProductionProductType?.ProductionCategory;
                var id = c?.Id ?? 0;
                Row(rows, id, "Category", c?.Name ?? "Uncategorised");
                Add(rows, id, "Produced", batch.ActualQuantity);
            }
            foreach (var defect in defects)
            {
                var c = defect.ProductionBatch.Recipe?.ProductionProductType?.ProductionCategory;
                var id = c?.Id ?? 0;
                Row(rows, id, "Category", c?.Name ?? "Uncategorised");
                Add(rows, id, "Defects", defect.Quantity);
            }
            foreach (var ret in returns)
            {
                var c = ret.Recipe?.ProductionProductType?.ProductionCategory;
                var id = c?.Id ?? 0;
                Row(rows, id, "Category", c?.Name ?? "Uncategorised");
                Add(rows, id, "Returns", ret.Quantity);
            }

            var list = rows.Values
                .Where(r => ctx.Filters.CategoryId is null || string.Equals(r["Category"]?.ToString(), CategoryName(db, ctx.Filters.CategoryId.Value), StringComparison.OrdinalIgnoreCase))
                .Select(r =>
                {
                    var orders = N(r, "Orders"); var produced = N(r, "Produced"); var loaded = N(r, "Loaded"); var sold = N(r, "Sold"); var closing = N(r, "Closing"); var damaged = N(r, "Damaged"); var defectsQty = N(r, "Defects"); var returnsQty = N(r, "Returns");
                    return new Dictionary<string, object?>
                    {
                        ["category"] = r["Category"],
                        ["orders"] = orders, ["produced"] = produced, ["loaded"] = loaded, ["defects"] = defectsQty, ["sold"] = sold, ["closing"] = closing, ["damaged"] = damaged, ["returns"] = returnsQty,
                        ["fill"] = Ratio(loaded, orders), ["sell"] = Ratio(sold, loaded), ["damage"] = Ratio(damaged, loaded),
                        ["status"] = string.Empty
                    };
                }).OrderBy(r => r["category"]?.ToString()).ToList();

            return Build(ctx, [
                ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Integer("orders", "Orders", true),
                ReportViewColumnDto.Integer("produced", "Produced", true),
                ReportViewColumnDto.Integer("loaded", "Loaded", true),
                ReportViewColumnDto.Integer("defects", "Factory Defects", true),
                ReportViewColumnDto.Integer("sold", "Sold", true),
                ReportViewColumnDto.Integer("closing", "Closing Stock", true),
                ReportViewColumnDto.Integer("damaged", "Damaged", true),
                ReportViewColumnDto.Integer("returns", "Returns", true),
                ReportViewColumnDto.Percent("fill", "Fill Rate", true),
                ReportViewColumnDto.Percent("sell", "Sell-Through", true),
                ReportViewColumnDto.Percent("damage", "Damage %", true)
            ], list);
        }

        private static async Task<ReportViewResultDto> ExecutiveProductLine(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await ProductLifecycleRows(db, ctx, ct);
            return Build(ctx, [
                ReportViewColumnDto.Text("category", "Category"), ReportViewColumnDto.Text("sku", "Product / SKU"),
                ReportViewColumnDto.Integer("orders", "Orders", true), ReportViewColumnDto.Integer("produced", "Produced", true),
                ReportViewColumnDto.Integer("loaded", "Loaded", true), ReportViewColumnDto.Integer("sold", "Sold", true),
                ReportViewColumnDto.Integer("closing", "Closing", true), ReportViewColumnDto.Integer("damaged", "Damaged", true),
                ReportViewColumnDto.Integer("returns", "Returns", true), ReportViewColumnDto.Money("revenue", "Revenue", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ExecutiveRepPerformance(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await RouteRepRows(db, ctx, ct);
            return Build(ctx, [
                ReportViewColumnDto.Text("rep", "Sales Rep"),
                ReportViewColumnDto.Text("route", "Route"),
                ReportViewColumnDto.Integer("orders", "Requested", true),
                ReportViewColumnDto.Integer("loaded", "Loaded", true),
                ReportViewColumnDto.Integer("sold", "Sold", true),
                ReportViewColumnDto.Integer("closing", "Closing Stock", true),
                ReportViewColumnDto.Integer("damaged", "Damaged", true),
                ReportViewColumnDto.Money("revenue", "Expected Revenue", true),
                ReportViewColumnDto.Money("collected", "Collected", true),
                ReportViewColumnDto.Money("outstanding", "Outstanding", true),
                ReportViewColumnDto.Percent("fill", "Fill Rate", true),
                ReportViewColumnDto.Percent("sell", "Sell-Through", true),
                ReportViewColumnDto.Percent("damage", "Damage %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ExecutiveRevenueCashDebt(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = (await RouteRepRows(db, ctx, ct)).Select(r => new Dictionary<string, object?>
            {
                ["rep"] = r["rep"], ["route"] = r["route"],
                ["expected"] = r["revenue"], ["cash"] = 0m, ["transfer"] = 0m, ["pos"] = 0m,
                ["collected"] = r["collected"], ["outstanding"] = r["outstanding"]
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("rep", "Sales Rep"),
                ReportViewColumnDto.Text("route", "Route"),
                ReportViewColumnDto.Money("expected", "Expected Revenue", true),
                ReportViewColumnDto.Money("collected", "Collected", true),
                ReportViewColumnDto.Money("outstanding", "Outstanding", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ExecutiveUnitEconomics(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct) =>
            await RecipeMargin(db, ctx, ct);

        private static async Task<ReportViewResultDto> SalesRegister(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date;
            var end = ctx.Filters.EndDate.Date.AddDays(1);
            var query = db.Orders.AsNoTracking().Include(o => o.OrderItems).ThenInclude(i => i.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory).Where(o => o.OrderDate >= start && o.OrderDate < end);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(o => o.SalesRepId == ctx.CurrentSalesRepId || o.SalesRepId == null);
            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync(ct);
            var rows = orders
                .Where(o => !ctx.Filters.CategoryId.HasValue || o.OrderItems.Any(i => i.Recipe?.ProductionProductType?.ProductionCategoryId == ctx.Filters.CategoryId))
                .Select(o => new Dictionary<string, object?>
                {
                    ["order"] = o.OrderNumber, ["date"] = o.OrderDate, ["customer"] = o.CustomerName,
                    ["rep"] = o.SalesRepName ?? "Unassigned", ["terminal"] = o.PosTerminalCode ?? "",
                    ["status"] = o.Status.ToString(), ["payment"] = o.PaymentStatus.ToString(),
                    ["units"] = o.OrderItems.Sum(i => i.Quantity + i.BonusQuantity),
                    ["subtotal"] = o.OrderItems.Sum(i => i.Quantity * i.UnitPrice), ["discount"] = o.Discount,
                    ["total"] = o.TotalAmount, ["paid"] = o.AmountPaid, ["balance"] = o.BalanceDue
                }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("order", "Order"), ReportViewColumnDto.Date("date", "Date"), ReportViewColumnDto.Text("customer", "Customer"),
                ReportViewColumnDto.Text("rep", "Sales Rep"), ReportViewColumnDto.Text("terminal", "Terminal"), ReportViewColumnDto.Text("status", "Status"),
                ReportViewColumnDto.Text("payment", "Payment"), ReportViewColumnDto.Integer("units", "Units", true),
                ReportViewColumnDto.Money("subtotal", "Subtotal", true), ReportViewColumnDto.Money("discount", "Discount", true),
                ReportViewColumnDto.Money("total", "Net Total", true), ReportViewColumnDto.Money("paid", "Paid", true), ReportViewColumnDto.Money("balance", "Balance", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesProduct(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await ProductRows(db, ctx, ct);
            return Build(ctx, [
                ReportViewColumnDto.Text("category", "Category"), ReportViewColumnDto.Text("sku", "Product / SKU"),
                ReportViewColumnDto.Integer("units", "Units", true), ReportViewColumnDto.Integer("bonus", "Bonus", true),
                ReportViewColumnDto.Money("revenue", "Revenue", true), ReportViewColumnDto.Number("average", "Avg Price", true),
                ReportViewColumnDto.Percent("share", "Share", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesCustomerLedger(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var orders = await db.Orders.AsNoTracking().Include(o => o.OrderItems).Where(o => o.OrderDate >= start && o.OrderDate < end).ToListAsync(ct);
            var returns = await db.CustomerReturns.AsNoTracking().Where(r => r.ReturnDate >= start && r.ReturnDate < end).ToListAsync(ct);
            var rows = orders.GroupBy(o => new { o.CustomerId, o.CustomerName })
                .Select(g => new Dictionary<string, object?>
                {
                    ["customer"] = g.Key.CustomerName,
                    ["orders"] = g.Count(),
                    ["units"] = g.Sum(o => o.OrderItems.Sum(i => i.Quantity + i.BonusQuantity)),
                    ["invoiced"] = g.Sum(o => o.TotalAmount),
                    ["paid"] = g.Sum(o => o.AmountPaid),
                    ["balance"] = g.Sum(o => o.BalanceDue),
                    ["returns"] = returns.Where(r => r.CustomerId == g.Key.CustomerId).Sum(r => r.RefundAmount ?? 0m)
                })
                .OrderByDescending(r => ReportFormatter.Numeric(r["balance"]))
                .ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("customer", "Customer"), ReportViewColumnDto.Integer("orders", "Orders", true),
                ReportViewColumnDto.Integer("units", "Units", true), ReportViewColumnDto.Money("invoiced", "Invoiced", true),
                ReportViewColumnDto.Money("paid", "Paid", true), ReportViewColumnDto.Money("balance", "Balance", true),
                ReportViewColumnDto.Money("returns", "Returns / Refunds", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesArAgeing(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var orders = await db.Orders.AsNoTracking().Include(o => o.OrderItems).Where(o => o.OrderDate >= start && o.OrderDate < end && o.BalanceDue > 0).ToListAsync(ct);
            var rows = orders.GroupBy(o => new { o.CustomerId, o.CustomerName })
                .Select(g =>
                {
                    var oldest = g.Max(o => (DateTime.UtcNow - o.OrderDate).TotalDays);
                    var bucket = oldest <= 7 ? "0-7 days" : oldest <= 14 ? "8-14 days" : oldest <= 30 ? "15-30 days" : oldest <= 60 ? "31-60 days" : "60+ days";
                    return new Dictionary<string, object?>
                    {
                        ["customer"] = g.Key.CustomerName, ["bucket"] = bucket, ["orders"] = g.Count(),
                        ["invoiced"] = g.Sum(o => o.TotalAmount), ["paid"] = g.Sum(o => o.AmountPaid),
                        ["balance"] = g.Sum(o => o.BalanceDue), ["oldest"] = Math.Round(oldest, 0)
                    };
                }).OrderByDescending(r => ReportFormatter.Numeric(r["balance"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("customer", "Customer"), ReportViewColumnDto.Text("bucket", "Age Bucket"),
                ReportViewColumnDto.Integer("orders", "Open Orders", true), ReportViewColumnDto.Money("invoiced", "Invoiced", true),
                ReportViewColumnDto.Money("paid", "Paid", true), ReportViewColumnDto.Money("balance", "Balance", true),
                ReportViewColumnDto.Integer("oldest", "Oldest Days")
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesCollections(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var query = db.OrderPayments.AsNoTracking().Include(p => p.Order).Where(p => p.PaymentDate >= start && p.PaymentDate < end);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(p => p.Order.SalesRepId == ctx.CurrentSalesRepId);
            var payments = await query.ToListAsync(ct);
            var rows = payments.GroupBy(p => new { p.PaymentDate.Date, p.PaymentMethod, p.ReceivedBy })
                .Select(g => new Dictionary<string, object?>
                {
                    ["date"] = g.Key.Date, ["method"] = g.Key.PaymentMethod.ToString(), ["received"] = g.Key.ReceivedBy,
                    ["count"] = g.Count(), ["amount"] = g.Sum(p => p.Amount)
                }).OrderByDescending(r => r["date"]).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("date", "Date"), ReportViewColumnDto.Text("method", "Method"),
                ReportViewColumnDto.Text("received", "Received By"), ReportViewColumnDto.Integer("count", "Payments", true),
                ReportViewColumnDto.Money("amount", "Amount", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesRoutePerformance(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await RouteRepRows(db, ctx, ct);
            return Build(ctx, [
                ReportViewColumnDto.Text("rep", "Sales Rep"), ReportViewColumnDto.Text("route", "Route"),
                ReportViewColumnDto.Integer("orders", "Requested", true), ReportViewColumnDto.Integer("loaded", "Loaded", true),
                ReportViewColumnDto.Integer("sold", "Sold", true), ReportViewColumnDto.Integer("closing", "Closing Stock", true),
                ReportViewColumnDto.Integer("damaged", "Damaged", true), ReportViewColumnDto.Money("revenue", "Expected Revenue", true),
                ReportViewColumnDto.Money("collected", "Collected", true), ReportViewColumnDto.Money("outstanding", "Outstanding", true),
                ReportViewColumnDto.Percent("fill", "Fill Rate", true), ReportViewColumnDto.Percent("sell", "Sell-Through", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesLoadingShortfall(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await RouteItemRows(db, ctx, ct, "shortfall");
            return Build(ctx, [
                ReportViewColumnDto.Text("rep", "Sales Rep"), ReportViewColumnDto.Text("sku", "Product / SKU"),
                ReportViewColumnDto.Integer("requested", "Requested", true), ReportViewColumnDto.Integer("loaded", "Loaded", true),
                ReportViewColumnDto.Integer("short", "Short-Load", true), ReportViewColumnDto.Percent("fill", "Fill Rate", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesClosingDamage(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await RouteItemRows(db, ctx, ct, "closing");
            return Build(ctx, [
                ReportViewColumnDto.Text("rep", "Sales Rep"), ReportViewColumnDto.Text("sku", "Product / SKU"),
                ReportViewColumnDto.Integer("closing", "Closing Qty", true), ReportViewColumnDto.Money("closingValue", "Closing Value", true),
                ReportViewColumnDto.Integer("damaged", "Damaged Qty", true), ReportViewColumnDto.Money("damagedValue", "Damaged Value", true),
                ReportViewColumnDto.Percent("damage", "Damage %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesCustomerReturns(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var query = db.CustomerReturns.AsNoTracking().Include(r => r.Customer).Include(r => r.SalesRep).Include(r => r.Recipe).Where(r => r.ReturnDate >= start && r.ReturnDate < end);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(r => r.SalesRepId == ctx.CurrentSalesRepId);
            var returns = await query.OrderByDescending(r => r.ReturnDate).ToListAsync(ct);
            var rows = returns.Select(r => new Dictionary<string, object?>
            {
                ["return"] = r.ReturnNumber, ["date"] = r.ReturnDate, ["customer"] = r.Customer?.Name ?? "Unassigned",
                ["rep"] = r.SalesRep?.FullName ?? "Unassigned", ["sku"] = r.Recipe?.Name ?? "",
                ["reason"] = r.Reason, ["qty"] = r.Quantity, ["refund"] = r.RefundAmount ?? 0m,
                ["replacement"] = r.ReplacementQuantity
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("return", "Return #"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("customer", "Customer"), ReportViewColumnDto.Text("rep", "Sales Rep"),
                ReportViewColumnDto.Text("sku", "Product"), ReportViewColumnDto.Text("reason", "Reason"),
                ReportViewColumnDto.Integer("qty", "Qty", true), ReportViewColumnDto.Money("refund", "Refund", true),
                ReportViewColumnDto.Integer("replacement", "Replacement", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesSupermarketCredit(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var query = db.SupermarketAccounts.AsNoTracking().Include(a => a.Customer).Where(a => a.IsActive);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(a => a.AssignedRepId == ctx.CurrentSalesRepId);
            var accounts = await query.ToListAsync(ct);
            var rows = accounts.Select(a => new Dictionary<string, object?>
            {
                ["account"] = a.AccountName, ["customer"] = a.Customer?.Name ?? "",
                ["rep"] = a.AssignedRepId, ["limit"] = a.CreditLimit, ["outstanding"] = a.CurrentOutstandingCredit,
                ["utilization"] = a.CreditLimit <= 0 ? 0m : Math.Round(a.CurrentOutstandingCredit / a.CreditLimit * 100m, 1),
                ["headroom"] = Math.Max(0m, a.CreditLimit - a.CurrentOutstandingCredit),
                ["status"] = a.IsActive ? "Active" : "Inactive"
            }).OrderByDescending(r => ReportFormatter.Numeric(r["outstanding"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("account", "Account"), ReportViewColumnDto.Text("customer", "Customer"),
                ReportViewColumnDto.Money("limit", "Credit Limit", true), ReportViewColumnDto.Money("outstanding", "Outstanding", true),
                ReportViewColumnDto.Percent("utilization", "Utilization", true), ReportViewColumnDto.Money("headroom", "Headroom", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesBonusDiscount(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var items = await db.OrderItems.AsNoTracking().Include(i => i.Order).Include(i => i.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(i => i.Order.OrderDate >= start && i.Order.OrderDate < end && (i.BonusQuantity > 0 || i.Order.Discount > 0)).ToListAsync(ct);
            var rows = items.Select(i => new Dictionary<string, object?>
            {
                ["order"] = i.Order.OrderNumber, ["date"] = i.Order.OrderDate, ["customer"] = i.Order.CustomerName,
                ["rep"] = i.Order.SalesRepName ?? "Unassigned", ["sku"] = i.ProductName,
                ["qty"] = i.Quantity, ["bonus"] = i.BonusQuantity, ["bonusValue"] = i.BonusQuantity * i.UnitPrice,
                ["discount"] = i.Order.Discount, ["discountRate"] = i.Order.SubTotal <= 0 ? 0m : Math.Round(i.Order.Discount / i.Order.SubTotal * 100m, 1)
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("order", "Order"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("customer", "Customer"), ReportViewColumnDto.Text("rep", "Sales Rep"),
                ReportViewColumnDto.Text("sku", "Product"), ReportViewColumnDto.Integer("qty", "Qty", true),
                ReportViewColumnDto.Integer("bonus", "Bonus Qty", true), ReportViewColumnDto.Money("bonusValue", "Bonus Value", true),
                ReportViewColumnDto.Money("discount", "Discount", true), ReportViewColumnDto.Percent("discountRate", "Discount %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesFulfillment(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var query = db.Orders.AsNoTracking().Where(o => o.OrderDate >= start && o.OrderDate < end);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(o => o.SalesRepId == ctx.CurrentSalesRepId);
            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync(ct);
            var rows = orders.Select(o => new Dictionary<string, object?>
            {
                ["order"] = o.OrderNumber, ["date"] = o.OrderDate, ["customer"] = o.CustomerName,
                ["rep"] = o.SalesRepName ?? "Unassigned", ["status"] = o.Status.ToString(),
                ["printed"] = o.IsPrintedForLoading ? "Yes" : "No",
                ["toPrinted"] = o.PrintedAt.HasValue ? Math.Round((o.PrintedAt.Value - o.OrderDate).TotalHours, 1) : 0m,
                ["toLoaded"] = o.LoadedAt.HasValue ? Math.Round((o.LoadedAt.Value - o.OrderDate).TotalHours, 1) : 0m,
                ["toClosed"] = o.ClosedAt.HasValue ? Math.Round((o.ClosedAt.Value - o.OrderDate).TotalHours, 1) : 0m
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("order", "Order"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("customer", "Customer"), ReportViewColumnDto.Text("rep", "Sales Rep"),
                ReportViewColumnDto.Text("status", "Status"), ReportViewColumnDto.Text("printed", "Printed"),
                ReportViewColumnDto.Number("toPrinted", "Hours to Print"), ReportViewColumnDto.Number("toLoaded", "Hours to Load"),
                ReportViewColumnDto.Number("toClosed", "Hours to Close")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryStockValuation(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var items = await db.InventoryItems.AsNoTracking().Include(i => i.PreferredSupplier).Where(i => i.IsActive).ToListAsync(ct);
            var rows = items.Select(i =>
            {
                var baseQty = i.QuantityInStock * i.PackageSize;
                var baseCost = i.PackageSize > 0 ? i.UnitCostPrice / i.PackageSize : i.UnitCostPrice;
                return new Dictionary<string, object?>
                {
                    ["item"] = i.Name, ["code"] = i.ItemCode, ["category"] = i.Category, ["supplier"] = i.PreferredSupplier?.Name ?? "",
                    ["unit"] = i.PackageUnitOfMeasure, ["qty"] = i.QuantityInStock, ["package"] = i.PackageSize,
                    ["baseQty"] = baseQty, ["cost"] = baseCost, ["value"] = baseQty * baseCost,
                    ["restocked"] = i.LastRestockedDate.ToDateTime(TimeOnly.MinValue)
                };
            }).OrderBy(r => r["item"]?.ToString()).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("item", "Item"), ReportViewColumnDto.Text("code", "Code"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Text("supplier", "Supplier"), ReportViewColumnDto.Text("unit", "Base Unit"),
                ReportViewColumnDto.Number("qty", "Package Qty", true), ReportViewColumnDto.Number("package", "Package Size"),
                ReportViewColumnDto.Number("baseQty", "Base Qty", true), ReportViewColumnDto.Money("cost", "Base Cost"),
                ReportViewColumnDto.Money("value", "Stock Value", true), ReportViewColumnDto.Date("restocked", "Last Restocked")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryLowStock(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var items = await db.InventoryItems.AsNoTracking().Include(i => i.PreferredSupplier).Where(i => i.IsActive && i.QuantityInStock <= i.ReorderThreshold).ToListAsync(ct);
            var rows = items.Select(i => new Dictionary<string, object?>
            {
                ["item"] = i.Name, ["category"] = i.Category, ["supplier"] = i.PreferredSupplier?.Name ?? "",
                ["stock"] = i.QuantityInStock, ["threshold"] = i.ReorderThreshold,
                ["gap"] = Math.Max(0m, i.ReorderThreshold - i.QuantityInStock), ["suggested"] = Math.Max(i.ReorderThreshold * 2 - i.QuantityInStock, i.ReorderThreshold),
                ["days"] = (DateTime.UtcNow.Date - i.LastRestockedDate.ToDateTime(TimeOnly.MinValue).Date).Days
            }).OrderBy(r => ReportFormatter.Numeric(r["stock"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("item", "Item"), ReportViewColumnDto.Text("category", "Category"), ReportViewColumnDto.Text("supplier", "Supplier"),
                ReportViewColumnDto.Number("stock", "Stock", true), ReportViewColumnDto.Number("threshold", "Reorder Point", true),
                ReportViewColumnDto.Number("gap", "Gap", true), ReportViewColumnDto.Number("suggested", "Suggested Order", true),
                ReportViewColumnDto.Integer("days", "Days Since Restock")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryMovements(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var movements = await db.StockMovements.AsNoTracking().Include(m => m.InventoryItem).Where(m => m.CreatedAt >= start && m.CreatedAt < end).OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
            var rows = movements.Select(m => new Dictionary<string, object?>
            {
                ["date"] = m.CreatedAt, ["item"] = m.InventoryItem?.Name ?? "", ["type"] = m.MovementType,
                ["qty"] = m.Quantity, ["balance"] = m.BalanceAfter, ["reference"] = m.ReferenceNumber ?? "",
                ["by"] = m.PerformedBy, ["notes"] = m.Notes
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("date", "Date"), ReportViewColumnDto.Text("item", "Item"),
                ReportViewColumnDto.Text("type", "Type"), ReportViewColumnDto.Number("qty", "Qty", true),
                ReportViewColumnDto.Number("balance", "Balance After"), ReportViewColumnDto.Text("reference", "Reference"),
                ReportViewColumnDto.Text("by", "Performed By"), ReportViewColumnDto.Text("notes", "Notes")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryRequisitions(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var requests = await db.StoreRequests.AsNoTracking().Include(r => r.StoreRequestItems).Where(r => r.RequestedAt >= start && r.RequestedAt < end).OrderByDescending(r => r.RequestedAt).ToListAsync(ct);
            var rows = requests.Select(r => new Dictionary<string, object?>
            {
                ["request"] = r.RequestNumber, ["date"] = r.RequestedAt, ["by"] = r.RequestedBy, ["department"] = r.Department,
                ["status"] = r.Status.ToString(), ["lines"] = r.StoreRequestItems.Count,
                ["requested"] = r.StoreRequestItems.Sum(i => i.RequestedQuantity), ["issued"] = r.StoreRequestItems.Sum(i => i.IssuedQuantity),
                ["age"] = Math.Round((DateTime.UtcNow - r.RequestedAt).TotalDays, 1)
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("request", "Request"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("by", "Requested By"), ReportViewColumnDto.Text("department", "Department"),
                ReportViewColumnDto.Text("status", "Status"), ReportViewColumnDto.Integer("lines", "Lines", true),
                ReportViewColumnDto.Number("requested", "Requested Qty", true), ReportViewColumnDto.Number("issued", "Issued Qty", true),
                ReportViewColumnDto.Number("age", "Age (days)")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryRequisitionFill(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var items = await db.StoreRequestItems.AsNoTracking().Include(i => i.StoreRequest).Where(i => i.StoreRequest.RequestedAt >= start && i.StoreRequest.RequestedAt < end).ToListAsync(ct);
            var rows = items.Select(i => new Dictionary<string, object?>
            {
                ["request"] = i.StoreRequest.RequestNumber, ["date"] = i.StoreRequest.RequestedAt,
                ["department"] = i.StoreRequest.Department, ["item"] = i.ItemName,
                ["requested"] = i.RequestedQuantity, ["issued"] = i.IssuedQuantity,
                ["short"] = Math.Max(0m, i.RequestedQuantity - i.IssuedQuantity),
                ["fill"] = i.RequestedQuantity <= 0 ? 0m : Math.Round(i.IssuedQuantity / i.RequestedQuantity * 100m, 1)
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("request", "Request"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("department", "Department"), ReportViewColumnDto.Text("item", "Item"),
                ReportViewColumnDto.Number("requested", "Requested", true), ReportViewColumnDto.Number("issued", "Issued", true),
                ReportViewColumnDto.Number("short", "Short", true), ReportViewColumnDto.Percent("fill", "Fill Rate", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryDepartmentConsumption(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var items = await db.StoreRequestItems.AsNoTracking().Include(i => i.StoreRequest).Include(i => i.InventoryItem)
                .Where(i => i.StoreRequest.RequestedAt >= start && i.StoreRequest.RequestedAt < end).ToListAsync(ct);
            var rows = items.GroupBy(i => new { i.StoreRequest.Department, i.ItemName })
                .Select(g =>
                {
                    var first = g.First();
                    var packageSize = first.InventoryItem?.PackageSize > 0 ? first.InventoryItem.PackageSize : 1m;
                    var unitCost = first.InventoryItem != null ? first.InventoryItem.UnitCostPrice / packageSize : 0m;
                    var issued = g.Sum(i => i.IssuedQuantity);
                    return new Dictionary<string, object?>
                    {
                        ["department"] = g.Key.Department, ["item"] = g.Key.ItemName,
                        ["requests"] = g.Select(i => i.StoreRequestId).Distinct().Count(),
                        ["issued"] = issued, ["value"] = issued * unitCost
                    };
                }).OrderByDescending(r => ReportFormatter.Numeric(r["value"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("department", "Department"), ReportViewColumnDto.Text("item", "Item"),
                ReportViewColumnDto.Integer("requests", "Requests", true), ReportViewColumnDto.Number("issued", "Issued Qty", true),
                ReportViewColumnDto.Money("value", "Issued Value", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryReceiptsIssues(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var movements = await db.StockMovements.AsNoTracking().Include(m => m.InventoryItem).Where(m => m.CreatedAt >= start && m.CreatedAt < end).ToListAsync(ct);
            var rows = movements.GroupBy(m => m.InventoryItem?.Name ?? $"Item {m.InventoryItemId}")
                .Select(g =>
                {
                    var receipts = g.Where(m => m.MovementType == "Receipt").Sum(m => m.Quantity);
                    var issues = g.Where(m => m.MovementType == "Issue").Sum(m => m.Quantity);
                    return new Dictionary<string, object?>
                    {
                        ["item"] = g.Key, ["receipts"] = receipts, ["issues"] = issues,
                        ["net"] = receipts - issues, ["closing"] = g.OrderByDescending(m => m.CreatedAt).First().BalanceAfter
                    };
                }).OrderBy(r => r["item"]?.ToString()).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("item", "Item"), ReportViewColumnDto.Number("receipts", "Receipts", true),
                ReportViewColumnDto.Number("issues", "Issues", true), ReportViewColumnDto.Number("net", "Net Change", true),
                ReportViewColumnDto.Number("closing", "Closing Balance")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventorySlowMoving(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var items = await db.InventoryItems.AsNoTracking().Include(i => i.PreferredSupplier).Where(i => i.IsActive).ToListAsync(ct);
            var lastIssues = await db.StockMovements.AsNoTracking()
                .Where(m => m.MovementType == "Issue")
                .GroupBy(m => m.InventoryItemId)
                .Select(g => new { ItemId = g.Key, Last = g.Max(m => m.CreatedAt) })
                .ToDictionaryAsync(x => x.ItemId, x => x.Last, ct);
            var rows = items.Select(i =>
            {
                var last = lastIssues.TryGetValue(i.Id, out var date) ? date : (DateTime?)null;
                var baseQty = i.QuantityInStock * i.PackageSize;
                var baseCost = i.PackageSize > 0 ? i.UnitCostPrice / i.PackageSize : i.UnitCostPrice;
                return new Dictionary<string, object?>
                {
                    ["item"] = i.Name, ["category"] = i.Category, ["supplier"] = i.PreferredSupplier?.Name ?? "",
                    ["qty"] = baseQty, ["value"] = baseQty * baseCost,
                    ["last"] = last, ["days"] = last.HasValue ? (DateTime.UtcNow.Date - last.Value.Date).Days : 9999
                };
            }).OrderByDescending(r => ReportFormatter.Numeric(r["days"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("item", "Item"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Text("supplier", "Supplier"), ReportViewColumnDto.Number("qty", "Stock Qty", true),
                ReportViewColumnDto.Money("value", "Stock Value", true), ReportViewColumnDto.Date("last", "Last Issue"),
                ReportViewColumnDto.Integer("days", "Days Since Issue")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventorySupplierPrice(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var items = await db.InventoryItems.AsNoTracking().Include(i => i.PreferredSupplier).Where(i => i.IsActive).ToListAsync(ct);
            var requests = await db.IngredientPriceChangeRequests.AsNoTracking().OrderByDescending(r => r.RequestedAt).ToListAsync(ct);
            var rows = items.Select(i =>
            {
                var last = requests.FirstOrDefault(r => r.InventoryItemId == i.Id);
                var packageSize = i.PackageSize > 0 ? i.PackageSize : 1m;
                return new Dictionary<string, object?>
                {
                    ["item"] = i.Name, ["supplier"] = i.PreferredSupplier?.Name ?? "",
                    ["unit"] = i.PackageUnitOfMeasure, ["package"] = i.UnitCostPrice,
                    ["base"] = i.UnitCostPrice / packageSize,
                    ["request"] = last?.RequestNumber ?? "", ["status"] = last?.Status.ToString() ?? "",
                    ["requested"] = last?.RequestedUnitCost ?? 0m, ["date"] = last?.RequestedAt
                };
            }).OrderBy(r => r["item"]?.ToString()).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("item", "Item"), ReportViewColumnDto.Text("supplier", "Supplier"),
                ReportViewColumnDto.Text("unit", "Unit"), ReportViewColumnDto.Money("package", "Package Cost"),
                ReportViewColumnDto.Money("base", "Base Cost"), ReportViewColumnDto.Text("request", "Last Request"),
                ReportViewColumnDto.Text("status", "Status"), ReportViewColumnDto.Money("requested", "Requested Cost"),
                ReportViewColumnDto.Date("date", "Requested At")
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionDaily(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var batches = await db.ProductionBatches.AsNoTracking().Include(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled).ToListAsync(ct);
            var rows = batches.GroupBy(b => new { b.ProductionDate.Date, Category = b.Recipe?.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised" })
                .Select(g =>
                {
                    var target = g.Sum(b => b.TargetQuantity); var actual = g.Sum(b => b.ActualQuantity); var cost = g.Sum(b => b.TotalBatchCost);
                    return new Dictionary<string, object?>
                    {
                        ["date"] = g.Key.Date, ["category"] = g.Key.Category, ["batches"] = g.Count(),
                        ["target"] = target, ["actual"] = actual, ["variance"] = actual - target,
                        ["yield"] = Ratio(actual, target), ["cost"] = cost, ["unitCost"] = actual > 0 ? cost / actual : 0m
                    };
                }).OrderByDescending(r => r["date"]).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("date", "Date"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Integer("batches", "Batches", true), ReportViewColumnDto.Integer("target", "Target", true),
                ReportViewColumnDto.Integer("actual", "Actual", true), ReportViewColumnDto.Integer("variance", "Variance", true),
                ReportViewColumnDto.Percent("yield", "Yield %", true), ReportViewColumnDto.Money("cost", "Batch Cost", true),
                ReportViewColumnDto.Money("unitCost", "Unit Cost", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionYield(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var batches = await db.ProductionBatches.AsNoTracking().Include(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled).OrderByDescending(b => b.ProductionDate).ToListAsync(ct);
            var rows = batches.Select(b => new Dictionary<string, object?>
            {
                ["batch"] = b.BatchNumber, ["date"] = b.ProductionDate, ["recipe"] = b.RecipeName,
                ["category"] = b.Recipe?.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised",
                ["shift"] = b.Shift, ["baker"] = b.BakerInCharge, ["target"] = b.TargetQuantity,
                ["actual"] = b.ActualQuantity, ["variance"] = b.ActualQuantity - b.TargetQuantity,
                ["yield"] = Ratio(b.ActualQuantity, b.TargetQuantity), ["status"] = b.Status.ToString()
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("batch", "Batch"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("recipe", "Recipe"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Text("shift", "Shift"), ReportViewColumnDto.Text("baker", "Baker"),
                ReportViewColumnDto.Integer("target", "Target", true), ReportViewColumnDto.Integer("actual", "Actual", true),
                ReportViewColumnDto.Integer("variance", "Variance", true), ReportViewColumnDto.Percent("yield", "Yield %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionBatchCost(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var batches = await db.ProductionBatches.AsNoTracking().Include(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled).OrderByDescending(b => b.ProductionDate).ToListAsync(ct);
            var rows = batches.Select(b => new Dictionary<string, object?>
            {
                ["batch"] = b.BatchNumber, ["date"] = b.ProductionDate, ["recipe"] = b.RecipeName,
                ["shift"] = b.Shift, ["cost"] = b.TotalBatchCost, ["actualCost"] = b.CalculatedUnitCost,
                ["expectedCost"] = b.ExpectedUnitCost, ["variance"] = b.CalculatedUnitCost - b.ExpectedUnitCost
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("batch", "Batch"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("recipe", "Recipe"), ReportViewColumnDto.Text("shift", "Shift"),
                ReportViewColumnDto.Money("cost", "Batch Cost", true), ReportViewColumnDto.Money("actualCost", "Actual Unit Cost"),
                ReportViewColumnDto.Money("expectedCost", "Expected Unit Cost"), ReportViewColumnDto.Money("variance", "Cost Variance")
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionRecipeMargin(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct) =>
            await RecipeMargin(db, ctx, ct);

        private static async Task<ReportViewResultDto> ProductionDefects(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var defects = await db.ProductionDefects.AsNoTracking().Include(d => d.ProductionBatch).Include(d => d.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(d => d.ProductionBatch.ProductionDate >= start && d.ProductionBatch.ProductionDate < end).OrderByDescending(d => d.RecordedAt).ToListAsync(ct);
            var rows = defects.Select(d => new Dictionary<string, object?>
            {
                ["date"] = d.ProductionBatch.ProductionDate, ["batch"] = d.ProductionBatch.BatchNumber,
                ["recipe"] = d.ProductionBatch.RecipeName, ["category"] = d.Recipe?.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised",
                ["qty"] = d.Quantity, ["reason"] = d.DefectReason, ["by"] = d.RecordedBy,
                ["produced"] = d.ProductionBatch.ActualQuantity,
                ["rate"] = Ratio(d.Quantity, d.ProductionBatch.ActualQuantity)
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("date", "Date"), ReportViewColumnDto.Text("batch", "Batch"),
                ReportViewColumnDto.Text("recipe", "Recipe"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Integer("qty", "Defects", true), ReportViewColumnDto.Text("reason", "Reason"),
                ReportViewColumnDto.Text("by", "Recorded By"), ReportViewColumnDto.Integer("produced", "Produced", true),
                ReportViewColumnDto.Percent("rate", "Defect %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionPriceAudit(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var requests = await db.IngredientPriceChangeRequests.AsNoTracking().Where(r => r.RequestedAt >= start && r.RequestedAt < end).OrderByDescending(r => r.RequestedAt).ToListAsync(ct);
            var rows = requests.Select(r => new Dictionary<string, object?>
            {
                ["request"] = r.RequestNumber, ["date"] = r.RequestedAt, ["item"] = r.InventoryItemName,
                ["previous"] = r.PreviousUnitCost, ["requested"] = r.RequestedUnitCost,
                ["change"] = r.RequestedUnitCost - r.PreviousUnitCost,
                ["changePct"] = r.PreviousUnitCost <= 0 ? 0m : Math.Round((r.RequestedUnitCost - r.PreviousUnitCost) / r.PreviousUnitCost * 100m, 1),
                ["status"] = ((IngredientPriceChangeStatus)r.Status).ToString(),
                ["by"] = r.RequestedByName, ["reviewed"] = r.ReviewedByName ?? ""
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("request", "Request"), ReportViewColumnDto.Date("date", "Requested"),
                ReportViewColumnDto.Text("item", "Material"), ReportViewColumnDto.Money("previous", "Previous"),
                ReportViewColumnDto.Money("requested", "Requested"), ReportViewColumnDto.Money("change", "Change", true),
                ReportViewColumnDto.Percent("changePct", "Change %", true), ReportViewColumnDto.Text("status", "Status"),
                ReportViewColumnDto.Text("by", "Requested By"), ReportViewColumnDto.Text("reviewed", "Reviewed By")
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionVolume(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var batches = await db.ProductionBatches.AsNoTracking().Include(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled).ToListAsync(ct);
            var total = batches.Sum(b => b.ActualQuantity);
            var rows = batches.GroupBy(b => new { Category = b.Recipe?.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised", b.RecipeName })
                .Select(g => new Dictionary<string, object?>
                {
                    ["category"] = g.Key.Category, ["recipe"] = g.Key.RecipeName,
                    ["batches"] = g.Count(), ["actual"] = g.Sum(b => b.ActualQuantity),
                    ["target"] = g.Sum(b => b.TargetQuantity), ["yield"] = Ratio(g.Sum(b => b.ActualQuantity), g.Sum(b => b.TargetQuantity)),
                    ["share"] = total <= 0 ? 0m : Math.Round(g.Sum(b => b.ActualQuantity) / (decimal)total * 100m, 1)
                }).OrderByDescending(r => ReportFormatter.Numeric(r["actual"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("category", "Category"), ReportViewColumnDto.Text("recipe", "Recipe"),
                ReportViewColumnDto.Integer("batches", "Batches", true), ReportViewColumnDto.Integer("actual", "Actual", true),
                ReportViewColumnDto.Integer("target", "Target", true), ReportViewColumnDto.Percent("yield", "Yield %", true),
                ReportViewColumnDto.Percent("share", "Volume Share", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionShiftBaker(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var batches = await db.ProductionBatches.AsNoTracking().Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled).ToListAsync(ct);
            var defects = await db.ProductionDefects.AsNoTracking().Where(d => d.RecordedAt >= start && d.RecordedAt < end).ToListAsync(ct);
            var defectByBatch = defects.GroupBy(d => d.ProductionBatchId).ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));
            var rows = batches.GroupBy(b => new { b.Shift, b.BakerInCharge })
                .Select(g =>
                {
                    var actual = g.Sum(b => b.ActualQuantity); var target = g.Sum(b => b.TargetQuantity);
                    var defectQty = g.Sum(b => defectByBatch.GetValueOrDefault(b.Id));
                    return new Dictionary<string, object?>
                    {
                        ["shift"] = g.Key.Shift, ["baker"] = g.Key.BakerInCharge, ["batches"] = g.Count(),
                        ["actual"] = actual, ["target"] = target, ["yield"] = Ratio(actual, target),
                        ["defects"] = defectQty, ["cost"] = g.Sum(b => b.TotalBatchCost),
                        ["unitCost"] = actual > 0 ? g.Sum(b => b.TotalBatchCost) / actual : 0m
                    };
                }).OrderByDescending(r => ReportFormatter.Numeric(r["actual"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("shift", "Shift"), ReportViewColumnDto.Text("baker", "Baker / Team"),
                ReportViewColumnDto.Integer("batches", "Batches", true), ReportViewColumnDto.Integer("actual", "Actual", true),
                ReportViewColumnDto.Integer("target", "Target", true), ReportViewColumnDto.Percent("yield", "Yield %", true),
                ReportViewColumnDto.Integer("defects", "Defects", true), ReportViewColumnDto.Money("cost", "Batch Cost", true),
                ReportViewColumnDto.Money("unitCost", "Unit Cost", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> HrHeadcount(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var employees = await db.Employees.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(ct);
            var rows = employees.GroupBy(e => new { e.Department, e.Designation, e.EmploymentStatus })
                .Select(g => new Dictionary<string, object?>
                {
                    ["department"] = g.Key.Department,
                    ["role"] = g.Key.Designation,
                    ["status"] = g.Key.EmploymentStatus,
                    ["headcount"] = g.Count(),
                    ["salary"] = g.Sum(e => e.BaseSalary),
                    ["average"] = g.Average(e => e.BaseSalary),
                    ["tenure"] = g.Average(e => Math.Round((DateTime.UtcNow.Date - e.EmploymentDate.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25, 1))
                }).OrderBy(r => r["department"]?.ToString()).ThenBy(r => r["role"]?.ToString()).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("department", "Department"), ReportViewColumnDto.Text("role", "Job Role"),
                ReportViewColumnDto.Text("status", "Status"), ReportViewColumnDto.Integer("headcount", "Headcount", true),
                ReportViewColumnDto.Money("salary", "Base Salary", true), ReportViewColumnDto.Money("average", "Average Salary"),
                ReportViewColumnDto.Number("tenure", "Avg Tenure (yrs)")
            ], rows);
        }

        private static async Task<ReportViewResultDto> HrHiresTerminations(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var employees = await db.Employees.AsNoTracking().ToListAsync(ct);
            var start = MonthStart(ctx.Filters.StartDate);
            var end = MonthStart(ctx.Filters.EndDate);
            var months = new List<DateTime>();
            for (var month = start; month <= end; month = month.AddMonths(1)) months.Add(month);
            var rows = months.Select(month =>
            {
                var monthStartDate = DateOnly.FromDateTime(month);
                var monthEnd = month.AddMonths(1);
                var monthEndDate = DateOnly.FromDateTime(monthEnd);
                var hires = employees.Count(e => e.EmploymentDate >= monthStartDate && e.EmploymentDate < monthEndDate);
                var terminations = employees.Count(e => e.TerminationDate.HasValue && e.TerminationDate >= monthStartDate && e.TerminationDate < monthEndDate);
                var headcount = employees.Count(e => e.EmploymentDate < monthEndDate && (!e.TerminationDate.HasValue || e.TerminationDate >= monthEndDate));
                return new Dictionary<string, object?>
                {
                    ["month"] = month.ToString("yyyy-MM"), ["hires"] = hires, ["terminations"] = terminations,
                    ["net"] = hires - terminations, ["headcount"] = headcount,
                    ["attrition"] = headcount <= 0 ? 0m : Math.Round(terminations / (decimal)headcount * 100m, 1)
                };
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("month", "Month"), ReportViewColumnDto.Integer("hires", "Hires", true),
                ReportViewColumnDto.Integer("terminations", "Terminations", true), ReportViewColumnDto.Integer("net", "Net Change", true),
                ReportViewColumnDto.Integer("headcount", "Headcount"), ReportViewColumnDto.Percent("attrition", "Attrition %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> PayrollRegister(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var startYear = (short)ctx.Filters.StartDate.Year;
            var startMonth = (byte)ctx.Filters.StartDate.Month;
            var endYear = (short)ctx.Filters.EndDate.Year;
            var endMonth = (byte)ctx.Filters.EndDate.Month;
            var records = await db.PayrollRecords.AsNoTracking().Include(r => r.Employee).Include(r => r.PayrollRun)
                .Where(r => (r.PayrollYear > startYear || (r.PayrollYear == startYear && r.PayrollMonth >= startMonth))
                    && (r.PayrollYear < endYear || (r.PayrollYear == endYear && r.PayrollMonth <= endMonth)))
                .OrderByDescending(r => r.PayrollYear).ThenByDescending(r => r.PayrollMonth).ThenBy(r => r.Employee.FullName).ToListAsync(ct);
            var rows = records.Select(r => new Dictionary<string, object?>
            {
                ["period"] = $"{r.PayrollMonth:00}/{r.PayrollYear}", ["employee"] = r.Employee.FullName,
                ["department"] = r.Employee.Department, ["role"] = r.Employee.Designation,
                ["base"] = r.BaseSalary, ["bonus"] = r.PerformanceBonus,
                ["allowances"] = r.HousingAllowance + r.TransportAllowance + r.OtherAllowances,
                ["gross"] = r.GrossSalary ?? (r.BaseSalary + r.PerformanceBonus + r.HousingAllowance + r.TransportAllowance + r.OtherAllowances),
                ["deductions"] = r.TotalDeductions ?? (r.DisciplinaryDeductions + r.AdvanceDeduction + r.TaxAndLevies + r.PensionDeduction + r.HealthInsuranceDeduction),
                ["net"] = r.NetSalary ?? 0m, ["status"] = r.PaymentStatus, ["paid"] = r.PaymentDate
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("period", "Period"), ReportViewColumnDto.Text("employee", "Employee"),
                ReportViewColumnDto.Text("department", "Department"), ReportViewColumnDto.Text("role", "Role"),
                ReportViewColumnDto.Money("base", "Base", true), ReportViewColumnDto.Money("bonus", "Bonus", true),
                ReportViewColumnDto.Money("allowances", "Allowances", true), ReportViewColumnDto.Money("gross", "Gross", true),
                ReportViewColumnDto.Money("deductions", "Deductions", true), ReportViewColumnDto.Money("net", "Net", true),
                ReportViewColumnDto.Text("status", "Status"), ReportViewColumnDto.Date("paid", "Paid On")
            ], rows);
        }

        private static async Task<ReportViewResultDto> PayrollSummary(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var startYear = (short)ctx.Filters.StartDate.Year;
            var startMonth = (byte)ctx.Filters.StartDate.Month;
            var endYear = (short)ctx.Filters.EndDate.Year;
            var endMonth = (byte)ctx.Filters.EndDate.Month;
            var records = await db.PayrollRecords.AsNoTracking().Include(r => r.Employee)
                .Where(r => (r.PayrollYear > startYear || (r.PayrollYear == startYear && r.PayrollMonth >= startMonth))
                    && (r.PayrollYear < endYear || (r.PayrollYear == endYear && r.PayrollMonth <= endMonth)))
                .ToListAsync(ct);
            var rows = records.GroupBy(r => new { r.PayrollYear, r.PayrollMonth, r.Employee.Department, r.Employee.Designation })
                .Select(g =>
                {
                    var gross = g.Sum(r => r.GrossSalary ?? 0m); var deductions = g.Sum(r => r.TotalDeductions ?? 0m); var net = g.Sum(r => r.NetSalary ?? 0m);
                    return new Dictionary<string, object?>
                    {
                        ["period"] = $"{g.Key.PayrollMonth:00}/{g.Key.PayrollYear}", ["department"] = g.Key.Department,
                        ["role"] = g.Key.Designation, ["headcount"] = g.Count(), ["gross"] = gross,
                        ["deductions"] = deductions, ["net"] = net, ["average"] = g.Count() == 0 ? 0m : net / g.Count()
                    };
                }).OrderByDescending(r => r["period"]?.ToString()).ThenBy(r => r["department"]?.ToString()).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("period", "Period"), ReportViewColumnDto.Text("department", "Department"),
                ReportViewColumnDto.Text("role", "Role"), ReportViewColumnDto.Integer("headcount", "Employees", true),
                ReportViewColumnDto.Money("gross", "Gross", true), ReportViewColumnDto.Money("deductions", "Deductions", true),
                ReportViewColumnDto.Money("net", "Net", true), ReportViewColumnDto.Money("average", "Average Net")
            ], rows);
        }

        private static async Task<ReportViewResultDto> PayrollDeductions(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var startYear = (short)ctx.Filters.StartDate.Year;
            var startMonth = (byte)ctx.Filters.StartDate.Month;
            var endYear = (short)ctx.Filters.EndDate.Year;
            var endMonth = (byte)ctx.Filters.EndDate.Month;
            var lines = await db.PayrollDeductionLines.AsNoTracking().Include(l => l.PayrollRecord).Include(l => l.Employee)
                .Where(l => (l.PayrollRecord.PayrollYear > startYear || (l.PayrollRecord.PayrollYear == startYear && l.PayrollRecord.PayrollMonth >= startMonth))
                    && (l.PayrollRecord.PayrollYear < endYear || (l.PayrollRecord.PayrollYear == endYear && l.PayrollRecord.PayrollMonth <= endMonth)))
                .ToListAsync(ct);
            var total = lines.Sum(l => l.Amount);
            var rows = lines.GroupBy(l => new { l.DeductionType, l.Employee.Department })
                .Select(g => new Dictionary<string, object?>
                {
                    ["type"] = g.Key.DeductionType, ["department"] = g.Key.Department,
                    ["lines"] = g.Count(), ["amount"] = g.Sum(l => l.Amount),
                    ["share"] = total <= 0 ? 0m : Math.Round(g.Sum(l => l.Amount) / total * 100m, 1)
                }).OrderByDescending(r => ReportFormatter.Numeric(r["amount"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("type", "Deduction Type"), ReportViewColumnDto.Text("department", "Department"),
                ReportViewColumnDto.Integer("lines", "Lines", true), ReportViewColumnDto.Money("amount", "Amount", true),
                ReportViewColumnDto.Percent("share", "Share", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> PayrollAdvances(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = DateOnly.FromDateTime(ctx.Filters.StartDate.Date); var end = DateOnly.FromDateTime(ctx.Filters.EndDate.Date);
            var advances = await db.SalaryAdvances.AsNoTracking().Include(a => a.Employee)
                .Where(a => a.RequestDate >= start && a.RequestDate <= end).OrderByDescending(a => a.RequestDate).ToListAsync(ct);
            var rows = advances.Select(a => new Dictionary<string, object?>
            {
                ["employee"] = a.Employee.FullName, ["department"] = a.Employee.Department, ["date"] = a.RequestDate,
                ["principal"] = a.PrincipalAmount, ["monthly"] = a.MonthlyDeductionAmount, ["repaid"] = a.TotalRepaid,
                ["balance"] = a.RemainingBalance ?? Math.Max(0m, a.PrincipalAmount - a.TotalRepaid),
                ["status"] = a.Status, ["age"] = (DateTime.UtcNow.Date - a.RequestDate.ToDateTime(TimeOnly.MinValue)).Days,
                ["progress"] = a.PrincipalAmount <= 0 ? 0m : Math.Round(a.TotalRepaid / a.PrincipalAmount * 100m, 1)
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("employee", "Employee"), ReportViewColumnDto.Text("department", "Department"),
                ReportViewColumnDto.Date("date", "Request Date"), ReportViewColumnDto.Money("principal", "Principal", true),
                ReportViewColumnDto.Money("monthly", "Monthly", true), ReportViewColumnDto.Money("repaid", "Repaid", true),
                ReportViewColumnDto.Money("balance", "Balance", true), ReportViewColumnDto.Text("status", "Status"),
                ReportViewColumnDto.Integer("age", "Age (days)"), ReportViewColumnDto.Percent("progress", "Repaid %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> PayrollReconciliation(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var startYear = (short)ctx.Filters.StartDate.Year;
            var startMonth = (byte)ctx.Filters.StartDate.Month;
            var endYear = (short)ctx.Filters.EndDate.Year;
            var endMonth = (byte)ctx.Filters.EndDate.Month;
            var runs = await db.PayrollRuns.AsNoTracking().Include(r => r.PayrollRecords)
                .Where(r => (r.PayrollYear > startYear || (r.PayrollYear == startYear && r.PayrollMonth >= startMonth))
                    && (r.PayrollYear < endYear || (r.PayrollYear == endYear && r.PayrollMonth <= endMonth)))
                .OrderByDescending(r => r.PayrollYear).ThenByDescending(r => r.PayrollMonth).ToListAsync(ct);
            var rows = runs.Select(r =>
            {
                var gross = r.PayrollRecords.Sum(x => x.GrossSalary ?? 0m);
                var deductions = r.PayrollRecords.Sum(x => x.TotalDeductions ?? 0m);
                var net = r.PayrollRecords.Sum(x => x.NetSalary ?? 0m);
                return new Dictionary<string, object?>
                {
                    ["run"] = r.RunNumber, ["period"] = $"{r.PayrollMonth:00}/{r.PayrollYear}", ["status"] = r.Status,
                    ["employees"] = r.EmployeeCount, ["records"] = r.PayrollRecords.Count,
                    ["gross"] = r.GrossTotal, ["recordGross"] = gross, ["grossVariance"] = r.GrossTotal - gross,
                    ["deductions"] = r.DeductionsTotal, ["recordDeductions"] = deductions, ["deductionVariance"] = r.DeductionsTotal - deductions,
                    ["net"] = r.NetTotal, ["recordNet"] = net, ["netVariance"] = r.NetTotal - net
                };
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("run", "Run"), ReportViewColumnDto.Text("period", "Period"),
                ReportViewColumnDto.Text("status", "Status"), ReportViewColumnDto.Integer("employees", "Employees"),
                ReportViewColumnDto.Integer("records", "Records"), ReportViewColumnDto.Money("gross", "Run Gross", true),
                ReportViewColumnDto.Money("recordGross", "Record Gross", true), ReportViewColumnDto.Money("grossVariance", "Gross Variance", true),
                ReportViewColumnDto.Money("net", "Run Net", true), ReportViewColumnDto.Money("recordNet", "Record Net", true),
                ReportViewColumnDto.Money("netVariance", "Net Variance", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> PayrollBankSchedule(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var startYear = (short)ctx.Filters.StartDate.Year;
            var startMonth = (byte)ctx.Filters.StartDate.Month;
            var endYear = (short)ctx.Filters.EndDate.Year;
            var endMonth = (byte)ctx.Filters.EndDate.Month;
            var records = await db.PayrollRecords.AsNoTracking().Include(r => r.Employee)
                .Where(r => (r.PayrollYear > startYear || (r.PayrollYear == startYear && r.PayrollMonth >= startMonth))
                    && (r.PayrollYear < endYear || (r.PayrollYear == endYear && r.PayrollMonth <= endMonth)))
                .OrderBy(r => r.Employee.FullName).ToListAsync(ct);
            var rows = records.Select(r => new Dictionary<string, object?>
            {
                ["employee"] = r.Employee.FullName, ["department"] = r.Employee.Department,
                ["bank"] = r.Employee.BankName, ["account"] = r.Employee.BankAccountNumber,
                ["net"] = r.NetSalary ?? 0m, ["status"] = r.PaymentStatus, ["paid"] = r.PaymentDate
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("employee", "Employee"), ReportViewColumnDto.Text("department", "Department"),
                ReportViewColumnDto.Text("bank", "Bank"), ReportViewColumnDto.Text("account", "Account Number"),
                ReportViewColumnDto.Money("net", "Net Pay", true), ReportViewColumnDto.Text("status", "Status"),
                ReportViewColumnDto.Date("paid", "Paid On")
            ], rows);
        }

        private static async Task<ReportViewResultDto> PayrollCompliance(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var employees = await db.Employees.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(ct);
            var duplicates = employees.GroupBy(e => e.StaffCode).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
            var rows = employees.Select(e =>
            {
                var missing = new List<string>();
                if (string.IsNullOrWhiteSpace(e.BankName)) missing.Add("Bank");
                if (string.IsNullOrWhiteSpace(e.BankAccountNumber)) missing.Add("Account");
                if (string.IsNullOrWhiteSpace(e.Email)) missing.Add("Email");
                if (string.IsNullOrWhiteSpace(e.GuarantorName)) missing.Add("Guarantor");
                if (string.IsNullOrWhiteSpace(e.EmergencyContactName)) missing.Add("Emergency contact");
                return new Dictionary<string, object?>
                {
                    ["employee"] = e.FullName, ["staff"] = e.StaffCode, ["department"] = e.Department,
                    ["missing"] = missing.Count == 0 ? "Complete" : string.Join(", ", missing),
                    ["duplicate"] = duplicates.Contains(e.StaffCode) ? "Yes" : "No",
                    ["status"] = e.EmploymentStatus
                };
            }).Where(r => !string.Equals(r["missing"]?.ToString(), "Complete", StringComparison.OrdinalIgnoreCase) || string.Equals(r["duplicate"]?.ToString(), "Yes", StringComparison.OrdinalIgnoreCase)).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("employee", "Employee"), ReportViewColumnDto.Text("staff", "Staff Code"),
                ReportViewColumnDto.Text("department", "Department"), ReportViewColumnDto.Text("missing", "Missing Fields"),
                ReportViewColumnDto.Text("duplicate", "Duplicate Code"), ReportViewColumnDto.Text("status", "Status")
            ], rows);
        }

        private static async Task<ReportViewResultDto> PayrollCostPerUnit(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var runs = await db.PayrollRuns.AsNoTracking().ToListAsync(ct);
            var batches = await db.ProductionBatches.AsNoTracking().Where(b => b.Status != (int)BatchStatus.Cancelled).ToListAsync(ct);
            var rows = runs.GroupBy(r => new { r.PayrollYear, r.PayrollMonth })
                .Select(g =>
                {
                    var month = new DateTime(g.Key.PayrollYear, g.Key.PayrollMonth, 1);
                    var produced = batches.Where(b => b.ProductionDate >= month && b.ProductionDate < month.AddMonths(1)).Sum(b => b.ActualQuantity);
                    var payroll = g.Sum(r => r.GrossTotal);
                    return new Dictionary<string, object?>
                    {
                        ["month"] = month.ToString("yyyy-MM"), ["payroll"] = payroll,
                        ["produced"] = produced, ["cost"] = produced <= 0 ? 0m : payroll / produced
                    };
                }).Where(r => string.CompareOrdinal(r["month"]?.ToString(), ctx.Filters.StartDate.ToString("yyyy-MM")) >= 0)
                  .Where(r => string.CompareOrdinal(r["month"]?.ToString(), ctx.Filters.EndDate.ToString("yyyy-MM")) <= 0)
                  .OrderByDescending(r => r["month"]?.ToString()).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("month", "Month"), ReportViewColumnDto.Money("payroll", "Payroll Cost", true),
                ReportViewColumnDto.Integer("produced", "Units Produced", true), ReportViewColumnDto.Money("cost", "Payroll Cost / Unit")
            ], rows);
        }

        private static async Task<ReportViewResultDto> FleetVehicles(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var vehicles = await db.Vehicles.AsNoTracking().Include(v => v.AssignedEmployee).OrderBy(v => v.RegistrationNumber).ToListAsync(ct);
            var rows = vehicles.Select(v => new Dictionary<string, object?>
            {
                ["vehicle"] = v.RegistrationNumber, ["model"] = v.MakeAndModel, ["type"] = v.VehicleType,
                ["function"] = v.VehicleFunction, ["driver"] = v.AssignedEmployee?.FullName ?? "Unassigned",
                ["status"] = v.Status, ["odometer"] = v.CurrentOdometerKm, ["service"] = v.LastServiceDate, ["active"] = v.IsActive ? "Yes" : "No"
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("vehicle", "Vehicle"), ReportViewColumnDto.Text("model", "Make / Model"),
                ReportViewColumnDto.Text("type", "Type"), ReportViewColumnDto.Text("function", "Function"),
                ReportViewColumnDto.Text("driver", "Driver"), ReportViewColumnDto.Text("status", "Status"),
                ReportViewColumnDto.Number("odometer", "Odometer (km)"), ReportViewColumnDto.Date("service", "Last Service"),
                ReportViewColumnDto.Text("active", "Active")
            ], rows);
        }

        private static async Task<ReportViewResultDto> FleetFuel(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var logs = await db.FuelLogs.AsNoTracking()
                .Include(f => f.Vehicle)
                .Include(f => f.Employee)
                .Include(f => f.CharterBooking)
                .Include(f => f.FleetDispatch)
                .Where(f => f.FuelDate >= start && f.FuelDate < end)
                .OrderByDescending(f => f.FuelDate)
                .ToListAsync(ct);
            var rows = logs.Select(f => new Dictionary<string, object?>
            {
                ["date"] = f.FuelDate, ["vehicle"] = f.Vehicle.RegistrationNumber, ["driver"] = f.Employee?.FullName ?? "",
                ["fuelFor"] = f.CharterBookingId.HasValue ? "Charter" : f.FleetDispatchId.HasValue ? "Dispatch" : "General",
                ["reference"] = f.CharterBooking?.BookingRef ?? f.FleetDispatch?.DispatchRef ?? "",
                ["fuel"] = f.FuelType, ["station"] = f.PetrolStation, ["litres"] = f.LitresDispensed,
                ["price"] = f.PricePerLitre, ["cost"] = f.TotalCost ?? f.LitresDispensed * f.PricePerLitre
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("date", "Date"), ReportViewColumnDto.Text("vehicle", "Vehicle"),
                ReportViewColumnDto.Text("fuelFor", "Fuel For"), ReportViewColumnDto.Text("reference", "Reference"),
                ReportViewColumnDto.Text("driver", "Driver"), ReportViewColumnDto.Text("fuel", "Fuel Type"),
                ReportViewColumnDto.Text("station", "Station"), ReportViewColumnDto.Number("litres", "Litres", true),
                ReportViewColumnDto.Money("price", "Price / Litre"), ReportViewColumnDto.Money("cost", "Total Cost", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> FleetMaintenance(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = DateOnly.FromDateTime(ctx.Filters.StartDate.Date); var end = DateOnly.FromDateTime(ctx.Filters.EndDate.Date);
            var logs = await db.MaintenanceLogs.AsNoTracking().Include(m => m.Vehicle)
                .Where(m => m.ServiceDate >= start && m.ServiceDate <= end).OrderByDescending(m => m.ServiceDate).ToListAsync(ct);
            var rows = logs.Select(m => new Dictionary<string, object?>
            {
                ["date"] = m.ServiceDate, ["vehicle"] = m.Vehicle.RegistrationNumber, ["description"] = m.Description,
                ["vendor"] = m.WorkshopOrVendor, ["invoice"] = m.InvoiceNumber, ["cost"] = m.Cost
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("date", "Service Date"), ReportViewColumnDto.Text("vehicle", "Vehicle"),
                ReportViewColumnDto.Text("description", "Description"), ReportViewColumnDto.Text("vendor", "Workshop / Vendor"),
                ReportViewColumnDto.Text("invoice", "Invoice"), ReportViewColumnDto.Money("cost", "Cost", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> FleetServiceDue(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var vehicles = await db.Vehicles.AsNoTracking().OrderBy(v => v.RegistrationNumber).ToListAsync(ct);
            var rows = vehicles.Select(v =>
            {
                var days = v.LastServiceDate.HasValue ? (DateTime.UtcNow.Date - v.LastServiceDate.Value.ToDateTime(TimeOnly.MinValue)).Days : 9999;
                return new Dictionary<string, object?>
                {
                    ["vehicle"] = v.RegistrationNumber, ["model"] = v.MakeAndModel, ["type"] = v.VehicleType,
                    ["status"] = v.Status, ["service"] = v.LastServiceDate, ["days"] = days,
                    ["overdue"] = days > 90 ? "Yes" : "No"
                };
            }).OrderByDescending(r => ReportFormatter.Numeric(r["days"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("vehicle", "Vehicle"), ReportViewColumnDto.Text("model", "Make / Model"),
                ReportViewColumnDto.Text("type", "Type"), ReportViewColumnDto.Text("status", "Status"),
                ReportViewColumnDto.Date("service", "Last Service"), ReportViewColumnDto.Integer("days", "Days Since"),
                ReportViewColumnDto.Text("overdue", "Overdue")
            ], rows);
        }

        private static async Task<ReportViewResultDto> FleetWeeklyCost(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var fuel = await db.FuelLogs.AsNoTracking().Include(f => f.Vehicle).ThenInclude(v => v.AssignedEmployee)
                .Where(f => f.FuelDate >= start && f.FuelDate < end).ToListAsync(ct);
            var maintenance = await db.MaintenanceLogs.AsNoTracking().Include(m => m.Vehicle).ThenInclude(v => v.AssignedEmployee)
                .Where(m => m.ServiceDate >= DateOnly.FromDateTime(start) && m.ServiceDate <= DateOnly.FromDateTime(ctx.Filters.EndDate.Date)).ToListAsync(ct);
            var rows = new Dictionary<string, Dictionary<string, object?>>();
            void Add(string key, string vehicle, string driver, DateTime week, string costKey, decimal value)
            {
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new Dictionary<string, object?> { ["week"] = week, ["vehicle"] = vehicle, ["driver"] = driver, ["fuel"] = 0m, ["maintenance"] = 0m };
                    rows[key] = row;
                }
                row[costKey] = N(row, costKey) + value;
            }
            foreach (var f in fuel)
            {
                var week = WeekStart(f.FuelDate);
                Add($"{week:yyyyMMdd}-{f.VehicleId}", f.Vehicle.RegistrationNumber, f.Vehicle.AssignedEmployee?.FullName ?? "", week, "fuel", f.TotalCost ?? f.LitresDispensed * f.PricePerLitre);
            }
            foreach (var m in maintenance)
            {
                var date = m.ServiceDate.ToDateTime(TimeOnly.MinValue);
                var week = WeekStart(date);
                Add($"{week:yyyyMMdd}-{m.VehicleId}", m.Vehicle.RegistrationNumber, m.Vehicle.AssignedEmployee?.FullName ?? "", week, "maintenance", m.Cost);
            }
            var list = rows.Values.Select(r =>
            {
                r["total"] = N(r, "fuel") + N(r, "maintenance");
                return r;
            }).OrderByDescending(r => r["week"]).ThenBy(r => r["vehicle"]?.ToString()).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("week", "Week Starting"), ReportViewColumnDto.Text("vehicle", "Vehicle"),
                ReportViewColumnDto.Text("driver", "Driver"), ReportViewColumnDto.Money("fuel", "Fuel", true),
                ReportViewColumnDto.Money("maintenance", "Maintenance", true), ReportViewColumnDto.Money("total", "Total", true)
            ], list);
        }

        private static async Task<ReportViewResultDto> FleetCharter(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = DateOnly.FromDateTime(ctx.Filters.StartDate.Date); var end = DateOnly.FromDateTime(ctx.Filters.EndDate.Date);
            var bookings = await db.CharterBookings.AsNoTracking().Include(b => b.Vehicle).Include(b => b.DriverEmployee).Include(b => b.TripLegs)
                .Where(b => b.DepartureDate >= start && b.DepartureDate <= end).OrderByDescending(b => b.DepartureDate).ToListAsync(ct);
            var rows = bookings.Select(b =>
            {
                var incidentals = b.TotalTripIncidentals ?? (b.DriverAllowance + b.SecurityEscortFee + b.TollsAndRoadLevies);
                var net = b.NetCharterRevenue ?? (b.GrossCharterFee - incidentals);
                return new Dictionary<string, object?>
                {
                    ["booking"] = b.BookingRef, ["date"] = b.DepartureDate, ["customer"] = b.CustomerName,
                    ["vehicle"] = b.Vehicle.RegistrationNumber, ["driver"] = b.DriverEmployee?.FullName ?? b.DriverName,
                    ["gross"] = b.GrossCharterFee, ["incidentals"] = incidentals, ["net"] = net,
                    ["margin"] = b.GrossCharterFee <= 0 ? 0m : Math.Round(net / b.GrossCharterFee * 100m, 1),
                    ["status"] = ((CharterTripStatus)b.TripStatus).ToString()
                };
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("booking", "Booking"), ReportViewColumnDto.Date("date", "Departure"),
                ReportViewColumnDto.Text("customer", "Customer"), ReportViewColumnDto.Text("vehicle", "Vehicle"),
                ReportViewColumnDto.Text("driver", "Driver"), ReportViewColumnDto.Money("gross", "Gross Fee", true),
                ReportViewColumnDto.Money("incidentals", "Incidentals", true), ReportViewColumnDto.Money("net", "Net Revenue", true),
                ReportViewColumnDto.Percent("margin", "Margin %", true), ReportViewColumnDto.Text("status", "Status")
            ], rows);
        }

        private static async Task<List<Dictionary<string, object?>>> FleetVehicleCostRows(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var sheets = await db.DailyRepRouteSheets.AsNoTracking().Include(s => s.Vehicle).Include(s => s.RouteSheetItems)
                .Where(s => s.RouteDate >= DateOnly.FromDateTime(start) && s.RouteDate <= DateOnly.FromDateTime(ctx.Filters.EndDate.Date)).ToListAsync(ct);
            var fuel = await db.FuelLogs.AsNoTracking().Where(f => f.FuelDate >= start && f.FuelDate < end).ToListAsync(ct);
            var maintenance = await db.MaintenanceLogs.AsNoTracking().Where(m => m.ServiceDate >= DateOnly.FromDateTime(start) && m.ServiceDate <= DateOnly.FromDateTime(ctx.Filters.EndDate.Date)).ToListAsync(ct);
            var vehicles = await db.Vehicles.AsNoTracking().Include(v => v.AssignedEmployee).ToListAsync(ct);
            var rows = vehicles.Select(v =>
            {
                var vehicleSheets = sheets.Where(s => s.VehicleId == v.Id).ToList();
                var units = vehicleSheets.Sum(s => s.RouteSheetItems.Sum(i => i.QtySold));
                var fuelCost = fuel.Where(f => f.VehicleId == v.Id).Sum(f => f.TotalCost ?? f.LitresDispensed * f.PricePerLitre);
                var maintenanceCost = maintenance.Where(m => m.VehicleId == v.Id).Sum(m => m.Cost);
                var total = fuelCost + maintenanceCost;
                return new Dictionary<string, object?>
                {
                    ["vehicle"] = v.RegistrationNumber, ["driver"] = v.AssignedEmployee?.FullName ?? "Unassigned",
                    ["routes"] = vehicleSheets.Count, ["units"] = units, ["fuel"] = fuelCost,
                    ["maintenance"] = maintenanceCost, ["total"] = total,
                    ["cost"] = units <= 0 ? 0m : total / units
                };
            }).OrderByDescending(r => ReportFormatter.Numeric(r["total"])).ToList();
            return rows;
        }

        private static async Task<ReportViewResultDto> FleetUtilization(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await FleetVehicleCostRows(db, ctx, ct);
            return Build(ctx, [
                ReportViewColumnDto.Text("vehicle", "Vehicle"), ReportViewColumnDto.Text("driver", "Driver"),
                ReportViewColumnDto.Integer("routes", "Route Sheets", true), ReportViewColumnDto.Integer("units", "Units Sold", true),
                ReportViewColumnDto.Money("fuel", "Fuel Cost", true), ReportViewColumnDto.Money("maintenance", "Maintenance", true),
                ReportViewColumnDto.Money("total", "Total Cost", true), ReportViewColumnDto.Money("cost", "Cost / Unit")
            ], rows);
        }

        private static async Task<ReportViewResultDto> FleetCostPerUnit(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await FleetVehicleCostRows(db, ctx, ct);
            return Build(ctx, [
                ReportViewColumnDto.Text("vehicle", "Vehicle"), ReportViewColumnDto.Text("driver", "Driver"),
                ReportViewColumnDto.Integer("units", "Units Sold", true), ReportViewColumnDto.Money("fuel", "Fuel", true),
                ReportViewColumnDto.Money("maintenance", "Maintenance", true), ReportViewColumnDto.Money("total", "Total", true),
                ReportViewColumnDto.Money("cost", "Cost / Unit")
            ], rows);
        }

        private static async Task<ReportViewResultDto> FleetTripIncidentals(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = DateOnly.FromDateTime(ctx.Filters.StartDate.Date); var end = DateOnly.FromDateTime(ctx.Filters.EndDate.Date);
            var bookings = await db.CharterBookings.AsNoTracking().Include(b => b.Vehicle).Include(b => b.DriverEmployee)
                .Where(b => b.DepartureDate >= start && b.DepartureDate <= end).OrderByDescending(b => b.DepartureDate).ToListAsync(ct);
            var rows = bookings.Select(b =>
            {
                var total = b.TotalTripIncidentals ?? (b.DriverAllowance + b.SecurityEscortFee + b.TollsAndRoadLevies);
                return new Dictionary<string, object?>
                {
                    ["booking"] = b.BookingRef, ["date"] = b.DepartureDate, ["driver"] = b.DriverEmployee?.FullName ?? b.DriverName,
                    ["vehicle"] = b.Vehicle.RegistrationNumber, ["allowance"] = b.DriverAllowance,
                    ["security"] = b.SecurityEscortFee, ["tolls"] = b.TollsAndRoadLevies, ["total"] = total,
                    ["share"] = b.GrossCharterFee <= 0 ? 0m : Math.Round(total / b.GrossCharterFee * 100m, 1),
                    ["status"] = ((CharterTripStatus)b.TripStatus).ToString()
                };
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("booking", "Booking"), ReportViewColumnDto.Date("date", "Departure"),
                ReportViewColumnDto.Text("driver", "Driver"), ReportViewColumnDto.Text("vehicle", "Vehicle"),
                ReportViewColumnDto.Money("allowance", "Driver Allowance", true), ReportViewColumnDto.Money("security", "Security", true),
                ReportViewColumnDto.Money("tolls", "Tolls / Levies", true), ReportViewColumnDto.Money("total", "Total", true),
                ReportViewColumnDto.Percent("share", "% of Gross", true), ReportViewColumnDto.Text("status", "Status")
            ], rows);
        }

        private static async Task<ReportViewResultDto> FleetFuelComparison(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var logs = await db.FuelLogs.AsNoTracking().Where(f => f.FuelDate >= start && f.FuelDate < end).ToListAsync(ct);
            var rows = logs.GroupBy(f => new
                {
                    f.PetrolStation,
                    f.FuelType,
                    FuelFor = f.CharterBookingId.HasValue ? "Charter" : f.FleetDispatchId.HasValue ? "Dispatch" : "General"
                })
                .Select(g => new Dictionary<string, object?>
                {
                    ["station"] = g.Key.PetrolStation, ["fuel"] = g.Key.FuelType, ["fuelFor"] = g.Key.FuelFor,
                    ["purchases"] = g.Count(),
                    ["litres"] = g.Sum(f => f.LitresDispensed), ["min"] = g.Min(f => f.PricePerLitre),
                    ["max"] = g.Max(f => f.PricePerLitre), ["average"] = g.Average(f => f.PricePerLitre),
                    ["cost"] = g.Sum(f => f.TotalCost ?? f.LitresDispensed * f.PricePerLitre)
                }).OrderByDescending(r => ReportFormatter.Numeric(r["cost"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("station", "Station"), ReportViewColumnDto.Text("fuel", "Fuel Type"),
                ReportViewColumnDto.Text("fuelFor", "Fuel For"),
                ReportViewColumnDto.Integer("purchases", "Purchases", true), ReportViewColumnDto.Number("litres", "Litres", true),
                ReportViewColumnDto.Money("min", "Min Price"), ReportViewColumnDto.Money("max", "Max Price"),
                ReportViewColumnDto.Money("average", "Avg Price"), ReportViewColumnDto.Money("cost", "Total Cost", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminUserAccess(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var users = await db.Users.AsNoTracking().Include(u => u.Role).Include(u => u.Employee).Include(u => u.Customer)
                .OrderBy(u => u.Name).ToListAsync(ct);
            var rows = users.Select(u => new Dictionary<string, object?>
            {
                ["name"] = u.Name, ["email"] = u.Email, ["role"] = u.Role.Name,
                ["linked"] = u.Employee?.FullName ?? u.Customer?.Name ?? "Unlinked",
                ["active"] = u.IsActive ? "Active" : "Inactive", ["last"] = u.LastLoginAt,
                ["change"] = u.MustChangePassword ? "Yes" : "No", ["terminal"] = u.PosTerminalCode
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("name", "User"), ReportViewColumnDto.Text("email", "Email"),
                ReportViewColumnDto.Text("role", "Role"), ReportViewColumnDto.Text("linked", "Linked Record"),
                ReportViewColumnDto.Text("active", "Status"), ReportViewColumnDto.Date("last", "Last Login"),
                ReportViewColumnDto.Text("change", "Must Change"), ReportViewColumnDto.Text("terminal", "Terminal")
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminRoleDistribution(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var roles = await db.Roles.AsNoTracking().Include(r => r.Users).Include(r => r.RbacRolePermissions).ThenInclude(p => p.PermissionLevel)
                .OrderBy(r => r.Name).ToListAsync(ct);
            var rows = roles.Select(r => new Dictionary<string, object?>
            {
                ["role"] = r.Name, ["display"] = r.DisplayName, ["users"] = r.Users.Count,
                ["active"] = r.Users.Count(u => u.IsActive), ["modules"] = r.RbacRolePermissions.Count,
                ["full"] = r.RbacRolePermissions.Count(p => p.PermissionLevel.CanManage),
                ["system"] = r.IsSystemRole ? "Yes" : "No"
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("role", "Role"), ReportViewColumnDto.Text("display", "Display Name"),
                ReportViewColumnDto.Integer("users", "Users", true), ReportViewColumnDto.Integer("active", "Active Users", true),
                ReportViewColumnDto.Integer("modules", "Modules", true), ReportViewColumnDto.Integer("full", "Manage-Level Modules", true),
                ReportViewColumnDto.Text("system", "System Role")
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminRbacMatrix(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var permissions = await db.RbacRolePermissions.AsNoTracking().Include(p => p.Role).Include(p => p.Module).Include(p => p.PermissionLevel)
                .OrderBy(p => p.Role.Name).ThenBy(p => p.Module.SortOrder).ToListAsync(ct);
            var rows = permissions.Select(p => new Dictionary<string, object?>
            {
                ["role"] = p.Role.Name, ["module"] = p.Module.ModuleKey, ["moduleName"] = p.Module.ModuleName,
                ["level"] = p.PermissionLevel.Name, ["rank"] = p.PermissionLevel.Rank,
                ["capabilities"] = string.Join(", ", new[]
                {
                    p.PermissionLevel.CanView ? "View" : null, p.PermissionLevel.CanCreate ? "Create" : null,
                    p.PermissionLevel.CanEdit ? "Edit" : null, p.PermissionLevel.CanDelete ? "Delete" : null,
                    p.PermissionLevel.CanApprove ? "Approve" : null, p.PermissionLevel.CanManage ? "Manage" : null,
                    p.PermissionLevel.CanRequest ? "Request" : null, p.PermissionLevel.CanIssue ? "Issue" : null
                }.Where(value => value is not null))
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("role", "Role"), ReportViewColumnDto.Text("module", "Module Key"),
                ReportViewColumnDto.Text("moduleName", "Module"), ReportViewColumnDto.Text("level", "Permission Level"),
                ReportViewColumnDto.Integer("rank", "Rank"), ReportViewColumnDto.Text("capabilities", "Capabilities")
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminPermissionAudit(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var audits = await db.RbacPermissionAudits.AsNoTracking().Where(a => a.ChangedAt >= start && a.ChangedAt < end)
                .OrderByDescending(a => a.ChangedAt).ToListAsync(ct);
            var rows = audits.Select(a => new Dictionary<string, object?>
            {
                ["date"] = a.ChangedAt, ["role"] = a.RoleName, ["module"] = a.ModuleKey,
                ["old"] = a.OldPermissionLevelName ?? "None", ["new"] = a.NewPermissionLevelName, ["by"] = a.ChangedBy
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("date", "Changed At"), ReportViewColumnDto.Text("role", "Role"),
                ReportViewColumnDto.Text("module", "Module"), ReportViewColumnDto.Text("old", "Old Level"),
                ReportViewColumnDto.Text("new", "New Level"), ReportViewColumnDto.Text("by", "Changed By")
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminDormantUsers(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var users = await db.Users.AsNoTracking().Include(u => u.Role).OrderBy(u => u.Name).ToListAsync(ct);
            var rows = users.Select(u => new Dictionary<string, object?>
            {
                ["name"] = u.Name, ["email"] = u.Email, ["role"] = u.Role.Name,
                ["status"] = u.IsActive ? "Active" : "Inactive", ["last"] = u.LastLoginAt,
                ["days"] = u.LastLoginAt.HasValue ? (DateTime.UtcNow.Date - u.LastLoginAt.Value.Date).Days : 9999
            }).OrderByDescending(r => ReportFormatter.Numeric(r["days"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("name", "User"), ReportViewColumnDto.Text("email", "Email"),
                ReportViewColumnDto.Text("role", "Role"), ReportViewColumnDto.Text("status", "Status"),
                ReportViewColumnDto.Date("last", "Last Login"), ReportViewColumnDto.Integer("days", "Days Since Login")
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminLoginCoverage(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var employees = await db.Employees.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(ct);
            var users = await db.Users.AsNoTracking().Include(u => u.Role).Where(u => u.EmployeeId.HasValue).ToListAsync(ct);
            var rows = employees.Select(e =>
            {
                var user = users.FirstOrDefault(u => u.EmployeeId == e.Id);
                return new Dictionary<string, object?>
                {
                    ["employee"] = e.FullName, ["department"] = e.Department, ["role"] = e.Designation,
                    ["email"] = user?.Email ?? e.Email, ["login"] = user is null ? "No" : "Yes",
                    ["accountRole"] = user?.Role.Name ?? "", ["status"] = e.EmploymentStatus
                };
            }).OrderBy(r => r["login"]?.ToString()).ThenBy(r => r["employee"]?.ToString()).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("employee", "Employee"), ReportViewColumnDto.Text("department", "Department"),
                ReportViewColumnDto.Text("role", "Job Role"), ReportViewColumnDto.Text("email", "Email"),
                ReportViewColumnDto.Text("login", "Has Login"), ReportViewColumnDto.Text("accountRole", "Account Role"),
                ReportViewColumnDto.Text("status", "Status")
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminMasterData(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = new List<Dictionary<string, object?>>
            {
                MasterRow("Departments", await db.Departments.CountAsync(ct), await db.Departments.CountAsync(x => x.IsActive, ct)),
                MasterRow("Job Roles", await db.JobRoles.CountAsync(ct), await db.JobRoles.CountAsync(x => x.IsActive, ct)),
                MasterRow("Production Categories", await db.ProductionCategories.CountAsync(ct), await db.ProductionCategories.CountAsync(x => x.IsActive, ct)),
                MasterRow("Product Types", await db.ProductionProductTypes.CountAsync(ct), await db.ProductionProductTypes.CountAsync(x => x.IsActive, ct)),
                MasterRow("Production Shifts", await db.ProductionShifts.CountAsync(ct), await db.ProductionShifts.CountAsync(x => x.IsActive, ct)),
                MasterRow("Measurement Densities", await db.MeasurementDensities.CountAsync(ct), await db.MeasurementDensities.CountAsync(x => x.IsActive, ct)),
                MasterRow("Sales Bonus Rules", await db.SalesBonusRules.CountAsync(ct), await db.SalesBonusRules.CountAsync(x => x.IsActive, ct))
            };
            return Build(ctx, [
                ReportViewColumnDto.Text("entity", "Master Data"), ReportViewColumnDto.Integer("total", "Total", true),
                ReportViewColumnDto.Integer("active", "Active", true), ReportViewColumnDto.Integer("inactive", "Inactive", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminConfiguration(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var treasury = await db.TreasuryPolicySettings.AsNoTracking().FirstOrDefaultAsync(ct);
            var payroll = await db.PayrollSettings.AsNoTracking().FirstOrDefaultAsync(ct);
            var rows = new List<Dictionary<string, object?>>();
            if (treasury is not null)
            {
                rows.Add(SettingRow("Planned cash %", treasury.PlannedCashPercent + "%", treasury.UpdatedBy, treasury.LastUpdatedUtc));
                rows.Add(SettingRow("Alert tolerance %", treasury.AlertTolerancePercent + "%", treasury.UpdatedBy, treasury.LastUpdatedUtc));
                rows.Add(SettingRow("Treasury memo", treasury.PolicyMemo, treasury.UpdatedBy, treasury.LastUpdatedUtc));
            }
            if (payroll is not null)
            {
                rows.Add(SettingRow("Loan monthly repayment %", payroll.LoanMonthlyRepaymentPercent + "%", payroll.UpdatedBy, payroll.UpdatedAt));
                rows.Add(SettingRow("Max loan % of salary", payroll.MaxLoanPercentOfSalary + "%", payroll.UpdatedBy, payroll.UpdatedAt));
                rows.Add(SettingRow("Housing allowance %", payroll.HousingAllowancePercent + "%", payroll.UpdatedBy, payroll.UpdatedAt));
                rows.Add(SettingRow("Transport allowance %", payroll.TransportAllowancePercent + "%", payroll.UpdatedBy, payroll.UpdatedAt));
                rows.Add(SettingRow("PAYE %", payroll.PayeTaxPercent + "%", payroll.UpdatedBy, payroll.UpdatedAt));
                rows.Add(SettingRow("Pension %", payroll.PensionPercent + "%", payroll.UpdatedBy, payroll.UpdatedAt));
                rows.Add(SettingRow("Health insurance %", payroll.HealthInsurancePercent + "%", payroll.UpdatedBy, payroll.UpdatedAt));
            }
            return Build(ctx, [
                ReportViewColumnDto.Text("setting", "Setting"), ReportViewColumnDto.Text("value", "Value"),
                ReportViewColumnDto.Text("by", "Updated By"), ReportViewColumnDto.Date("at", "Updated At")
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminAuditExtract(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var logs = await db.AuditLogs.AsNoTracking().Include(a => a.User).Where(a => a.OccurredAt >= start && a.OccurredAt < end)
                .OrderByDescending(a => a.OccurredAt).ToListAsync(ct);
            var rows = logs.Select(a => new Dictionary<string, object?>
            {
                ["date"] = a.OccurredAt, ["user"] = a.User?.Name ?? "System", ["action"] = a.Action,
                ["entity"] = a.EntityName, ["entityId"] = a.EntityId, ["ip"] = a.IpAddress
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Date("date", "Occurred At"), ReportViewColumnDto.Text("user", "User"),
                ReportViewColumnDto.Text("action", "Action"), ReportViewColumnDto.Text("entity", "Entity"),
                ReportViewColumnDto.Text("entityId", "Entity ID"), ReportViewColumnDto.Text("ip", "IP Address")
            ], rows);
        }

        private static async Task<ReportViewResultDto> AdminDataIntegrity(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rolesWithoutPermissions = await db.Roles.CountAsync(r => !r.RbacRolePermissions.Any(), ct);
            var employeesWithoutLinks = await db.Employees.CountAsync(e => !e.IsDeleted && (e.DepartmentId == null || e.JobRoleId == null), ct);
            var duplicateStaffCodes = (await db.Employees.AsNoTracking().Where(e => !e.IsDeleted).GroupBy(e => e.StaffCode).Where(g => g.Count() > 1).Select(g => g.Key).ToListAsync(ct)).Count;
            var itemsWithoutSupplier = await db.InventoryItems.CountAsync(i => i.IsActive && i.PreferredSupplierId == null, ct);
            var itemsWithoutDensity = await db.InventoryItems.CountAsync(i => i.IsActive && i.MeasurementDensityId == null, ct);
            var usersWithoutLoginLink = await db.Users.CountAsync(u => u.IsActive && u.EmployeeId == null && u.CustomerId == null, ct);
            var rows = new List<Dictionary<string, object?>>
            {
                Issue("Roles without permission rows", "Role", rolesWithoutPermissions, "High"),
                Issue("Employees without department or job role", "Employee", employeesWithoutLinks, "Medium"),
                Issue("Duplicate staff codes", "Employee", duplicateStaffCodes, "High"),
                Issue("Active inventory items without supplier", "InventoryItem", itemsWithoutSupplier, "Medium"),
                Issue("Active inventory items without density", "InventoryItem", itemsWithoutDensity, "Low"),
                Issue("Active users without employee/customer link", "User", usersWithoutLoginLink, "Medium")
            };
            return Build(ctx, [
                ReportViewColumnDto.Text("issue", "Integrity Check"), ReportViewColumnDto.Text("entity", "Entity"),
                ReportViewColumnDto.Integer("count", "Records", true), ReportViewColumnDto.Text("severity", "Severity")
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesTerminalProductivity(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var query = db.Orders.AsNoTracking().Include(o => o.OrderItems).Where(o => o.OrderDate >= start && o.OrderDate < end);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(o => o.SalesRepId == ctx.CurrentSalesRepId || o.SalesRepId == null);
            var orders = await query.ToListAsync(ct);
            var rows = orders.GroupBy(o => new { Terminal = o.PosTerminalCode ?? "No terminal", o.SalesRepName })
                .Select(g =>
                {
                    var revenue = g.Sum(o => o.TotalAmount ?? 0m);
                    return new Dictionary<string, object?>
                    {
                        ["terminal"] = g.Key.Terminal, ["cashier"] = g.Key.SalesRepName ?? "Unassigned",
                        ["orders"] = g.Count(), ["units"] = g.Sum(o => o.OrderItems.Sum(i => i.Quantity + i.BonusQuantity)),
                        ["revenue"] = revenue, ["average"] = g.Count() == 0 ? 0m : revenue / g.Count(),
                        ["cash"] = g.Count(o => o.PaymentMethod == 0), ["transfer"] = g.Count(o => o.PaymentMethod == 1),
                        ["pos"] = g.Count(o => o.PaymentMethod == 2), ["credit"] = g.Count(o => o.PaymentMethod == 3)
                    };
                }).OrderByDescending(r => ReportFormatter.Numeric(r["revenue"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("terminal", "Terminal"), ReportViewColumnDto.Text("cashier", "Cashier / Rep"),
                ReportViewColumnDto.Integer("orders", "Orders", true), ReportViewColumnDto.Integer("units", "Units", true),
                ReportViewColumnDto.Money("revenue", "Revenue", true), ReportViewColumnDto.Money("average", "Avg Basket"),
                ReportViewColumnDto.Integer("cash", "Cash", true), ReportViewColumnDto.Integer("transfer", "Transfer", true),
                ReportViewColumnDto.Integer("pos", "POS", true), ReportViewColumnDto.Integer("credit", "Credit", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesPreOrders(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var query = db.Orders.AsNoTracking().Include(o => o.OrderItems).Where(o => o.OrderType == 1 && o.OrderDate >= start && o.OrderDate < end);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(o => o.SalesRepId == ctx.CurrentSalesRepId || o.SalesRepId == null);
            var orders = await query.OrderBy(o => o.TargetDeliveryDate).ToListAsync(ct);
            var rows = orders.Select(o => new Dictionary<string, object?>
            {
                ["order"] = o.OrderNumber, ["date"] = o.OrderDate, ["delivery"] = o.TargetDeliveryDate,
                ["customer"] = o.CustomerName, ["rep"] = o.SalesRepName ?? "Unassigned",
                ["status"] = o.Status.ToString(), ["units"] = o.OrderItems.Sum(i => i.Quantity + i.BonusQuantity),
                ["total"] = o.TotalAmount ?? 0m, ["paid"] = o.AmountPaid, ["balance"] = o.BalanceDue ?? 0m,
                ["lead"] = (o.TargetDeliveryDate.Date - o.OrderDate.Date).Days
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("order", "Order"), ReportViewColumnDto.Date("date", "Ordered"),
                ReportViewColumnDto.Date("delivery", "Delivery"), ReportViewColumnDto.Text("customer", "Customer"),
                ReportViewColumnDto.Text("rep", "Sales Rep"), ReportViewColumnDto.Text("status", "Status"),
                ReportViewColumnDto.Integer("units", "Units", true), ReportViewColumnDto.Money("total", "Total", true),
                ReportViewColumnDto.Money("paid", "Paid", true), ReportViewColumnDto.Money("balance", "Balance", true),
                ReportViewColumnDto.Integer("lead", "Lead Days")
            ], rows);
        }

        private static async Task<ReportViewResultDto> SalesPriceRealization(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var items = await db.OrderItems.AsNoTracking().Include(i => i.Order).Include(i => i.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(i => i.Order.OrderDate >= start && i.Order.OrderDate < end).ToListAsync(ct);
            var rows = items.Select(i =>
            {
                var list = i.Recipe?.SellingPrice ?? i.UnitPrice;
                var variance = i.UnitPrice - list;
                return new Dictionary<string, object?>
                {
                    ["order"] = i.Order.OrderNumber, ["date"] = i.Order.OrderDate, ["customer"] = i.Order.CustomerName,
                    ["product"] = i.ProductName, ["qty"] = i.Quantity, ["actual"] = i.UnitPrice,
                    ["list"] = list, ["variance"] = variance,
                    ["percent"] = list <= 0 ? 0m : Math.Round(variance / list * 100m, 1),
                    ["categoryId"] = i.Recipe?.ProductionProductType?.ProductionCategoryId ?? 0
                };
            }).Where(r => ctx.Filters.CategoryId is null || Convert.ToInt32(r["categoryId"]) == ctx.Filters.CategoryId)
              .OrderBy(r => ReportFormatter.Numeric(r["percent"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("order", "Order"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("customer", "Customer"), ReportViewColumnDto.Text("product", "Product"),
                ReportViewColumnDto.Integer("qty", "Qty", true), ReportViewColumnDto.Money("actual", "Actual Price"),
                ReportViewColumnDto.Money("list", "List Price"), ReportViewColumnDto.Money("variance", "Variance", true),
                ReportViewColumnDto.Percent("percent", "Variance %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryTurnover(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var movements = await db.StockMovements.AsNoTracking().Include(m => m.InventoryItem).Where(m => m.CreatedAt >= start && m.CreatedAt < end && m.MovementType == "Issue").ToListAsync(ct);
            var items = await db.InventoryItems.AsNoTracking().Where(i => i.IsActive).ToListAsync(ct);
            var rows = items.Select(i =>
            {
                var issues = movements.Where(m => m.InventoryItemId == i.Id).Sum(m => m.Quantity);
                var packageSize = i.PackageSize > 0 ? i.PackageSize : 1m;
                var unitCost = i.UnitCostPrice / packageSize;
                var issueValue = issues * unitCost;
                var stockValue = i.QuantityInStock * i.UnitCostPrice;
                var turnover = stockValue <= 0 ? 0m : Math.Round(issueValue / stockValue, 2);
                return new Dictionary<string, object?>
                {
                    ["item"] = i.Name, ["category"] = i.Category, ["issues"] = issues, ["issueValue"] = issueValue,
                    ["stockValue"] = stockValue, ["turnover"] = turnover,
                    ["cover"] = turnover <= 0 ? 0m : Math.Round(365m / turnover, 1)
                };
            }).OrderByDescending(r => ReportFormatter.Numeric(r["issueValue"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("item", "Item"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Number("issues", "Issued Qty", true), ReportViewColumnDto.Money("issueValue", "Issue Value", true),
                ReportViewColumnDto.Money("stockValue", "Stock Value", true), ReportViewColumnDto.Number("turnover", "Turnover"),
                ReportViewColumnDto.Number("cover", "Days Cover")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryDensityCoverage(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var items = await db.InventoryItems.AsNoTracking().Include(i => i.MeasurementDensity).Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync(ct);
            var rows = items.Select(i => new Dictionary<string, object?>
            {
                ["item"] = i.Name, ["category"] = i.Category, ["unit"] = i.PackageUnitOfMeasure,
                ["package"] = i.PackageSize, ["density"] = i.DensityKgPerLitre ?? i.MeasurementDensity?.DensityKgPerLitre,
                ["conversion"] = i.DensityKgPerLitre.HasValue || i.MeasurementDensity is not null ? "Configured" : "Missing",
                ["status"] = i.IsActive ? "Active" : "Inactive"
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("item", "Item"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Text("unit", "Package Unit"), ReportViewColumnDto.Number("package", "Package Size"),
                ReportViewColumnDto.Number("density", "Density kg/L"), ReportViewColumnDto.Text("conversion", "Conversion"),
                ReportViewColumnDto.Text("status", "Status")
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryRequisitionCycle(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var requests = await db.StoreRequests.AsNoTracking().Where(r => r.RequestedAt >= start && r.RequestedAt < end)
                .OrderByDescending(r => r.RequestedAt).ToListAsync(ct);
            var rows = requests.Select(r =>
            {
                var approval = r.ApprovedAt.HasValue ? (r.ApprovedAt.Value - r.RequestedAt).TotalHours : 0d;
                var issue = r.ApprovedAt.HasValue ? (r.UpdatedAt - r.ApprovedAt.Value).TotalHours : 0d;
                return new Dictionary<string, object?>
                {
                    ["request"] = r.RequestNumber, ["department"] = r.Department, ["status"] = r.Status.ToString(),
                    ["requested"] = r.RequestedAt, ["approved"] = r.ApprovedAt,
                    ["toApproval"] = Math.Round(approval, 1), ["toIssue"] = Math.Round(Math.Max(0d, issue), 1),
                    ["total"] = Math.Round(approval + Math.Max(0d, issue), 1)
                };
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("request", "Request"), ReportViewColumnDto.Text("department", "Department"),
                ReportViewColumnDto.Text("status", "Status"), ReportViewColumnDto.Date("requested", "Requested"),
                ReportViewColumnDto.Date("approved", "Approved"), ReportViewColumnDto.Number("toApproval", "Hours to Approve", true),
                ReportViewColumnDto.Number("toIssue", "Hours to Issue", true), ReportViewColumnDto.Number("total", "Total Hours", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> InventoryTheoreticalActual(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var batches = await db.ProductionBatches.AsNoTracking().Include(b => b.Recipe).ThenInclude(r => r.RecipeIngredients).ThenInclude(i => i.InventoryItem)
                .Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled).ToListAsync(ct);
            var issues = await db.StoreRequestItems.AsNoTracking().Where(i => i.StoreRequest.RequestedAt >= start && i.StoreRequest.RequestedAt < end).ToListAsync(ct);
            var rows = batches.SelectMany(b => b.Recipe.RecipeIngredients.Select(i => new { b, i }))
                .GroupBy(x => new { x.b.RecipeId, x.b.RecipeName, x.i.InventoryItemId, x.i.InventoryItem.Name })
                .Select(g =>
                {
                    var expected = g.Sum(x => x.i.QuantityPerBatch);
                    var actual = issues.Where(i => i.InventoryItemId == g.Key.InventoryItemId).Sum(i => i.IssuedQuantity);
                    return new Dictionary<string, object?>
                    {
                        ["recipe"] = g.Key.RecipeName, ["item"] = g.Key.Name, ["batches"] = g.Select(x => x.b.Id).Distinct().Count(),
                        ["expected"] = expected, ["actual"] = actual, ["variance"] = actual - expected,
                        ["percent"] = expected <= 0 ? 0m : Math.Round((actual - expected) / expected * 100m, 1)
                    };
                }).OrderByDescending(r => ReportFormatter.Numeric(r["variance"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("recipe", "Recipe"), ReportViewColumnDto.Text("item", "Inventory Item"),
                ReportViewColumnDto.Integer("batches", "Batches", true), ReportViewColumnDto.Number("expected", "Expected Use", true),
                ReportViewColumnDto.Number("actual", "Issued Use", true), ReportViewColumnDto.Number("variance", "Variance", true),
                ReportViewColumnDto.Percent("percent", "Variance %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionBatchStatus(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var batches = await db.ProductionBatches.AsNoTracking().Include(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(b => b.ProductionDate >= start && b.ProductionDate < end).OrderByDescending(b => b.ProductionDate).ToListAsync(ct);
            var rows = batches.Select(b => new Dictionary<string, object?>
            {
                ["batch"] = b.BatchNumber, ["date"] = b.ProductionDate, ["recipe"] = b.RecipeName,
                ["category"] = b.Recipe?.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised",
                ["status"] = ((BatchStatus)b.Status).ToString(), ["target"] = b.TargetQuantity,
                ["actual"] = b.ActualQuantity, ["age"] = (DateTime.UtcNow.Date - b.ProductionDate.Date).Days,
                ["completion"] = b.TargetQuantity <= 0 ? 0m : Math.Round(b.ActualQuantity / (decimal)b.TargetQuantity * 100m, 1)
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("batch", "Batch"), ReportViewColumnDto.Date("date", "Date"),
                ReportViewColumnDto.Text("recipe", "Recipe"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Text("status", "Status"), ReportViewColumnDto.Integer("target", "Target", true),
                ReportViewColumnDto.Integer("actual", "Actual", true), ReportViewColumnDto.Integer("age", "Age (days)"),
                ReportViewColumnDto.Percent("completion", "Completion", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionBomCost(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var recipes = await db.Recipes.AsNoTracking().Include(r => r.RecipeIngredients).ThenInclude(i => i.InventoryItem).Include(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(r => r.IsActive).ToListAsync(ct);
            var rows = recipes.SelectMany(r => r.RecipeIngredients.Select(i => new { r, i }))
                .GroupBy(x => x.r.Id)
                .SelectMany(g =>
                {
                    var recipeTotal = g.Sum(x => x.i.QuantityPerBatch * x.i.IngredientUnitCost);
                    return g.Select(x => new Dictionary<string, object?>
                    {
                        ["recipe"] = x.r.Name, ["category"] = x.r.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised",
                        ["ingredient"] = x.i.InventoryItem.Name, ["quantity"] = x.i.QuantityPerBatch, ["unit"] = x.i.Unit,
                        ["cost"] = x.i.IngredientUnitCost, ["line"] = x.i.QuantityPerBatch * x.i.IngredientUnitCost,
                        ["share"] = recipeTotal <= 0 ? 0m : Math.Round(x.i.QuantityPerBatch * x.i.IngredientUnitCost / recipeTotal * 100m, 1),
                        ["categoryId"] = x.r.ProductionProductType?.ProductionCategoryId ?? 0
                    });
                }).Where(r => ctx.Filters.CategoryId is null || Convert.ToInt32(r["categoryId"]) == ctx.Filters.CategoryId)
                  .OrderBy(r => r["recipe"]?.ToString()).ThenByDescending(r => ReportFormatter.Numeric(r["line"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("recipe", "Recipe"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Text("ingredient", "Ingredient"), ReportViewColumnDto.Number("quantity", "Qty / Batch", true),
                ReportViewColumnDto.Text("unit", "Unit"), ReportViewColumnDto.Money("cost", "Unit Cost"),
                ReportViewColumnDto.Money("line", "Line Cost", true), ReportViewColumnDto.Percent("share", "Cost Share", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionDemand(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var demandItems = await db.OrderItems.AsNoTracking().Include(i => i.Order).Include(i => i.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(i => i.Order.OrderType == 1 && i.Order.OrderDate >= start && i.Order.OrderDate < end).ToListAsync(ct);
            var batches = await db.ProductionBatches.AsNoTracking().Include(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled).ToListAsync(ct);
            var rows = demandItems.GroupBy(i => new { i.RecipeId, i.ProductName, Category = i.Recipe?.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised", CategoryId = i.Recipe?.ProductionProductType?.ProductionCategoryId ?? 0 })
                .Select(g =>
                {
                    var demand = g.Sum(i => i.Quantity + i.BonusQuantity);
                    var produced = batches.Where(b => b.RecipeId == g.Key.RecipeId).Sum(b => b.ActualQuantity);
                    return new Dictionary<string, object?>
                    {
                        ["recipe"] = g.Key.ProductName, ["category"] = g.Key.Category, ["demand"] = demand,
                        ["produced"] = produced, ["gap"] = produced - demand,
                        ["fill"] = demand <= 0 ? 0m : Math.Round(produced / (decimal)demand * 100m, 1),
                        ["categoryId"] = g.Key.CategoryId
                    };
                }).Where(r => ctx.Filters.CategoryId is null || Convert.ToInt32(r["categoryId"]) == ctx.Filters.CategoryId)
                  .OrderByDescending(r => ReportFormatter.Numeric(r["demand"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("recipe", "Recipe / Product"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Integer("demand", "Pre-Order Demand", true), ReportViewColumnDto.Integer("produced", "Produced", true),
                ReportViewColumnDto.Integer("gap", "Gap", true), ReportViewColumnDto.Percent("fill", "Fill %", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductionToSales(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await ProductLifecycleRows(db, ctx, ct);
            var list = rows.Select(r => new Dictionary<string, object?>
            {
                ["category"] = r["category"], ["recipe"] = r["sku"], ["produced"] = r["produced"],
                ["loaded"] = r["loaded"], ["sold"] = r["sold"], ["closing"] = r["closing"],
                ["damaged"] = r["damaged"], ["returns"] = r["returns"]
            }).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("category", "Category"), ReportViewColumnDto.Text("recipe", "Recipe / Product"),
                ReportViewColumnDto.Integer("produced", "Produced", true), ReportViewColumnDto.Integer("loaded", "Loaded", true),
                ReportViewColumnDto.Integer("sold", "Sold", true), ReportViewColumnDto.Integer("closing", "Closing", true),
                ReportViewColumnDto.Integer("damaged", "Damaged", true), ReportViewColumnDto.Integer("returns", "Returns", true)
            ], list);
        }

        private static async Task<ReportViewResultDto> ExecutiveManagementPack(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var produced = await db.ProductionBatches.AsNoTracking().Where(b => b.ProductionDate >= start && b.ProductionDate < end && b.Status != (int)BatchStatus.Cancelled).SumAsync(b => (decimal)b.ActualQuantity, ct);
            var sales = await db.Orders.AsNoTracking().Where(o => o.OrderDate >= start && o.OrderDate < end).SumAsync(o => o.TotalAmount ?? 0m, ct);
            var collections = await db.OrderPayments.AsNoTracking().Where(p => p.PaymentDate >= start && p.PaymentDate < end).SumAsync(p => p.Amount, ct);
            var inventory = await db.InventoryItems.AsNoTracking().Where(i => i.IsActive).SumAsync(i => i.QuantityInStock * i.UnitCostPrice, ct);
            var startYear = (short)start.Year;
            var startMonth = (byte)start.Month;
            var endYear = (short)ctx.Filters.EndDate.Year;
            var endMonth = (byte)ctx.Filters.EndDate.Month;
            var payroll = await db.PayrollRecords.AsNoTracking()
                .Where(r => (r.PayrollYear > startYear || (r.PayrollYear == startYear && r.PayrollMonth >= startMonth))
                    && (r.PayrollYear < endYear || (r.PayrollYear == endYear && r.PayrollMonth <= endMonth)))
                .SumAsync(r => r.NetSalary ?? 0m, ct);
            var fuel = await db.FuelLogs.AsNoTracking().Where(f => f.FuelDate >= start && f.FuelDate < end).SumAsync(f => f.TotalCost ?? f.LitresDispensed * f.PricePerLitre, ct);
            var maintenance = await db.MaintenanceLogs.AsNoTracking().Where(m => m.ServiceDate >= DateOnly.FromDateTime(start) && m.ServiceDate <= DateOnly.FromDateTime(ctx.Filters.EndDate.Date)).SumAsync(m => m.Cost, ct);
            var headcount = await db.Employees.CountAsync(e => !e.IsDeleted && e.IsActive, ct);
            var rows = new List<Dictionary<string, object?>>
            {
                MetricRow("Units produced", produced, "Production batches"),
                MetricRow("Sales revenue", sales, "Orders"),
                MetricRow("Collections", collections, "Order payments"),
                MetricRow("Inventory value", inventory, "Current stock at cost"),
                MetricRow("Payroll net", payroll, "Payroll records"),
                MetricRow("Fleet cost", fuel + maintenance, "Fuel + maintenance"),
                MetricRow("Headcount", headcount, "Active employees")
            };
            return Build(ctx, [
                ReportViewColumnDto.Text("metric", "Metric"), ReportViewColumnDto.Number("value", "Value", true),
                ReportViewColumnDto.Text("note", "Source")
            ], rows);
        }

        private static async Task<ReportViewResultDto> ExecutiveWorkingCapital(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var inventory = await db.InventoryItems.AsNoTracking().Where(i => i.IsActive).SumAsync(i => i.QuantityInStock * i.UnitCostPrice, ct);
            var receivables = await db.Orders.AsNoTracking().SumAsync(o => o.BalanceDue ?? 0m, ct);
            var credit = await db.SupermarketAccounts.AsNoTracking().Where(a => a.IsActive).SumAsync(a => a.CurrentOutstandingCredit, ct);
            var advances = await db.SalaryAdvances.AsNoTracking().SumAsync(a => a.RemainingBalance ?? 0m, ct);
            var unpaidPayroll = await db.PayrollRecords.AsNoTracking().Where(r => r.PaymentStatus != "Paid").SumAsync(r => r.NetSalary ?? 0m, ct);
            var rows = new List<Dictionary<string, object?>>
            {
                MetricRow("Inventory value", inventory, "Stock on hand"),
                MetricRow("Trade receivables", receivables, "Order balances"),
                MetricRow("Supermarket credit exposure", credit, "Active accounts"),
                MetricRow("Salary advances outstanding", advances, "Loan balances"),
                MetricRow("Unpaid payroll", unpaidPayroll, "Pending payroll records"),
                MetricRow("Total working capital exposure", inventory + receivables + credit + advances + unpaidPayroll, "Combined")
            };
            return Build(ctx, [
                ReportViewColumnDto.Text("metric", "Component"), ReportViewColumnDto.Money("value", "Value", true),
                ReportViewColumnDto.Text("note", "Source")
            ], rows);
        }

        private static async Task<ReportViewResultDto> ExecutiveIntegrityPack(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var orders = await db.Orders.AsNoTracking().Include(o => o.OrderPayments).ToListAsync(ct);
            var paymentDifference = orders.Sum(o => o.AmountPaid - o.OrderPayments.Sum(p => p.Amount));
            var routeSheets = await db.DailyRepRouteSheets.AsNoTracking().Include(s => s.RouteSheetItems).ToListAsync(ct);
            var routeDifference = routeSheets.Sum(s => s.TotalExpectedSalesRevenue - s.RouteSheetItems.Sum(i => i.ExpectedItemRevenue ?? 0m));
            var customers = await db.Customers.AsNoTracking().ToListAsync(ct);
            var openBalances = orders.Where(o => o.CustomerId > 0).GroupBy(o => o.CustomerId).ToDictionary(g => g.Key, g => g.Sum(o => o.BalanceDue ?? 0m));
            var customerDifference = customers.Sum(c => c.OutstandingBalance - openBalances.GetValueOrDefault(c.Id));
            var inventory = await db.InventoryItems.AsNoTracking().ToListAsync(ct);
            var lastBalances = await db.StockMovements.AsNoTracking().GroupBy(m => m.InventoryItemId)
                .Select(g => new { ItemId = g.Key, Balance = g.OrderByDescending(m => m.CreatedAt).First().BalanceAfter }).ToDictionaryAsync(x => x.ItemId, x => x.Balance, ct);
            var inventoryDifference = inventory.Sum(i => i.QuantityInStock - (lastBalances.TryGetValue(i.Id, out var balance) ? balance : i.QuantityInStock));
            var payrollRuns = await db.PayrollRuns.AsNoTracking().Include(r => r.PayrollRecords).ToListAsync(ct);
            var payrollDifference = payrollRuns.Sum(r => r.NetTotal - r.PayrollRecords.Sum(x => x.NetSalary ?? 0m));
            var rows = new List<Dictionary<string, object?>>
            {
                IntegrityRow("Order AmountPaid vs payment rows", paymentDifference, "High"),
                IntegrityRow("Route expected revenue vs item totals", routeDifference, "High"),
                IntegrityRow("Customer outstanding vs open orders", customerDifference, "Medium"),
                IntegrityRow("Inventory quantity vs last movement balance", inventoryDifference, "High"),
                IntegrityRow("Payroll run net vs record net", payrollDifference, "High")
            };
            return Build(ctx, [
                ReportViewColumnDto.Text("check", "Integrity Check"), ReportViewColumnDto.Money("difference", "Difference", true),
                ReportViewColumnDto.Text("severity", "Severity")
            ], rows);
        }

        private static async Task<ReportViewResultDto> ExecutiveAlerts(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var lowStock = await db.InventoryItems.CountAsync(i => i.IsActive && i.QuantityInStock <= i.ReorderThreshold, ct);
            var shortLoads = await db.RouteSheetItems.CountAsync(i => i.RepOrderRequestedQty > i.TodayNewlyLoadedQty, ct);
            var damage = await db.RouteSheetItems.CountAsync(i => i.QtyDamaged > 0, ct);
            var receivables = await db.Orders.CountAsync(o => (o.BalanceDue ?? 0m) > 0, ct);
            var serviceDue = await db.Vehicles.CountAsync(v => v.IsActive && (!v.LastServiceDate.HasValue || v.LastServiceDate < DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-90))), ct);
            var negativeVariance = await db.ProductionBatches.CountAsync(b => b.Status != (int)BatchStatus.Cancelled && b.ActualQuantity < b.TargetQuantity, ct);
            var rows = new List<Dictionary<string, object?>>
            {
                AlertRow("Low-stock items", "Inventory", lowStock, "Normal"),
                AlertRow("Ordered > loaded lines", "Dispatch", shortLoads, "High"),
                AlertRow("Route damage lines", "Sales", damage, "Medium"),
                AlertRow("Orders with outstanding balance", "Receivables", receivables, "High"),
                AlertRow("Vehicles past service threshold", "Fleet", serviceDue, "Medium"),
                AlertRow("Batches below target", "Production", negativeVariance, "Medium")
            }.Where(r => ReportFormatter.Numeric(r["count"]) > 0).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("alert", "Alert"), ReportViewColumnDto.Text("module", "Module"),
                ReportViewColumnDto.Integer("count", "Count", true), ReportViewColumnDto.Text("severity", "Severity")
            ], rows);
        }

        private static async Task<ReportViewResultDto> ProductPerformance(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var rows = await ProductRows(db, ctx, ct);
            return Build(ctx, [
                ReportViewColumnDto.Text("category", "Category"), ReportViewColumnDto.Text("sku", "Product / SKU"),
                ReportViewColumnDto.Integer("orders", "Orders", true), ReportViewColumnDto.Integer("produced", "Produced", true),
                ReportViewColumnDto.Integer("loaded", "Loaded", true), ReportViewColumnDto.Integer("sold", "Sold", true),
                ReportViewColumnDto.Integer("closing", "Closing", true), ReportViewColumnDto.Integer("damaged", "Damaged", true),
                ReportViewColumnDto.Integer("returns", "Returns", true), ReportViewColumnDto.Money("revenue", "Revenue", true)
            ], rows);
        }

        private static async Task<ReportViewResultDto> RecipeMargin(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var recipes = await db.Recipes.AsNoTracking().Include(r => r.RecipeIngredients).Include(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(r => r.IsActive).ToListAsync(ct);
            var rows = recipes.Select(r =>
            {
                var material = r.RecipeIngredients.Sum(i => i.QuantityPerUnit.GetValueOrDefault() * i.IngredientUnitCost);
                var margin = r.SellingPrice - material;
                return new Dictionary<string, object?>
                {
                    ["recipe"] = r.Name, ["category"] = r.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised",
                    ["price"] = r.SellingPrice, ["material"] = material, ["margin"] = margin,
                    ["marginPct"] = r.SellingPrice <= 0 ? 0m : Math.Round(margin / r.SellingPrice * 100m, 1)
                };
            }).Where(r => ctx.Filters.CategoryId is null || string.Equals(r["category"]?.ToString(), CategoryName(db, ctx.Filters.CategoryId.Value), StringComparison.OrdinalIgnoreCase))
              .OrderByDescending(r => ReportFormatter.Numeric(r["margin"])).ToList();
            return Build(ctx, [
                ReportViewColumnDto.Text("recipe", "Recipe"), ReportViewColumnDto.Text("category", "Category"),
                ReportViewColumnDto.Money("price", "Selling Price", true), ReportViewColumnDto.Money("material", "Material Cost / Unit", true),
                ReportViewColumnDto.Money("margin", "Margin / Unit", true), ReportViewColumnDto.Percent("marginPct", "Margin %", true)
            ], rows);
        }

        private static async Task<List<Dictionary<string, object?>>> ProductLifecycleRows(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var baseRows = await ProductRows(db, ctx, ct);
            var rows = new Dictionary<int, Dictionary<string, object?>>();
            foreach (var row in baseRows)
            {
                var recipeId = Convert.ToInt32(row.GetValueOrDefault("recipeId") ?? 0);
                if (recipeId > 0) rows[recipeId] = row;
            }

            void Ensure(int recipeId, string category, string sku, string code)
            {
                if (rows.ContainsKey(recipeId)) return;
                rows[recipeId] = new Dictionary<string, object?>
                {
                    ["category"] = category, ["sku"] = sku, ["code"] = code,
                    ["orders"] = 0, ["produced"] = 0, ["loaded"] = 0, ["sold"] = 0,
                    ["closing"] = 0, ["damaged"] = 0, ["returns"] = 0,
                    ["revenue"] = 0m, ["recipeId"] = recipeId, ["categoryId"] = 0
                };
            }

            var start = DateOnly.FromDateTime(ctx.Filters.StartDate.Date);
            var end = DateOnly.FromDateTime(ctx.Filters.EndDate.Date);
            var routeItems = await db.RouteSheetItems.AsNoTracking()
                .Include(i => i.RouteSheet)
                .Include(i => i.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(i => i.RouteSheet.RouteDate >= start && i.RouteSheet.RouteDate <= end)
                .ToListAsync(ct);
            foreach (var group in routeItems.GroupBy(i => i.RecipeId))
            {
                var first = group.First();
                var category = first.Recipe?.ProductionProductType?.ProductionCategory;
                Ensure(group.Key, category?.Name ?? "Uncategorised", first.ProductName, first.ProductCode);
                var row = rows[group.Key];
                row["categoryId"] = category?.Id ?? 0;
                row["loaded"] = group.Sum(i => i.TodayNewlyLoadedQty);
                row["sold"] = group.Sum(i => i.QtySold);
                row["closing"] = group.Sum(i => i.ClosingStockQty);
                row["damaged"] = group.Sum(i => i.QtyDamaged);
                var routeRevenue = group.Sum(i => i.QtySold * i.UnitSellingPrice);
                if (routeRevenue > 0) row["revenue"] = routeRevenue;
            }

            var batches = await db.ProductionBatches.AsNoTracking()
                .Include(b => b.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(b => b.ProductionDate >= ctx.Filters.StartDate.Date
                    && b.ProductionDate < ctx.Filters.EndDate.Date.AddDays(1)
                    && b.Status != (int)BatchStatus.Cancelled)
                .ToListAsync(ct);
            foreach (var group in batches.GroupBy(b => b.RecipeId))
            {
                var first = group.First();
                var category = first.Recipe?.ProductionProductType?.ProductionCategory;
                Ensure(group.Key, category?.Name ?? "Uncategorised", first.RecipeName, first.Recipe?.Code ?? string.Empty);
                var row = rows[group.Key];
                row["categoryId"] = category?.Id ?? 0;
                row["produced"] = group.Sum(b => b.ActualQuantity);
            }

            var returns = await db.CustomerReturns.AsNoTracking()
                .Include(r => r.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(r => r.ReturnDate >= ctx.Filters.StartDate.Date && r.ReturnDate < ctx.Filters.EndDate.Date.AddDays(1))
                .ToListAsync(ct);
            foreach (var group in returns.GroupBy(r => r.RecipeId))
            {
                var first = group.First();
                var category = first.Recipe?.ProductionProductType?.ProductionCategory;
                Ensure(group.Key, category?.Name ?? "Uncategorised", first.Recipe?.Name ?? string.Empty, first.Recipe?.Code ?? string.Empty);
                var row = rows[group.Key];
                row["categoryId"] = category?.Id ?? 0;
                row["returns"] = group.Sum(r => r.Quantity);
            }

            return rows.Values
                .Where(row => ctx.Filters.CategoryId is null || Convert.ToInt32(row.GetValueOrDefault("categoryId") ?? 0) == ctx.Filters.CategoryId)
                .OrderByDescending(row => ReportFormatter.Numeric(row.GetValueOrDefault("sold")))
                .ToList();
        }

        private static async Task<List<Dictionary<string, object?>>> ProductRows(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = ctx.Filters.StartDate.Date; var end = ctx.Filters.EndDate.Date.AddDays(1);
            var orders = await db.OrderItems.AsNoTracking().Include(i => i.Order).Include(i => i.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(i => i.Order.OrderDate >= start && i.Order.OrderDate < end).ToListAsync(ct);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) orders = orders.Where(i => i.Order.SalesRepId == ctx.CurrentSalesRepId || i.Order.SalesRepId == null).ToList();
            var rows = orders.GroupBy(i => new { CategoryId = i.Recipe?.ProductionProductType?.ProductionCategoryId ?? 0, Category = i.Recipe?.ProductionProductType?.ProductionCategory?.Name ?? "Uncategorised", i.RecipeId, i.ProductName })
                .Select(g =>
                {
                    var units = g.Sum(i => i.Quantity); var bonus = g.Sum(i => i.BonusQuantity); var revenue = g.Sum(i => i.Quantity * i.UnitPrice);
                    return new Dictionary<string, object?>
                    {
                        ["category"] = g.Key.Category, ["sku"] = g.Key.ProductName,
                        ["orders"] = g.Select(i => i.OrderId).Distinct().Count(), ["units"] = units, ["bonus"] = bonus,
                        ["revenue"] = revenue, ["average"] = units > 0 ? revenue / units : 0m,
                        ["produced"] = 0, ["loaded"] = 0, ["sold"] = units, ["closing"] = 0,
                        ["damaged"] = 0, ["returns"] = 0, ["share"] = 0m,
                        ["categoryId"] = g.Key.CategoryId, ["recipeId"] = g.Key.RecipeId
                    };
                }).ToList();
            var totalRevenue = rows.Sum(r => ReportFormatter.Numeric(r["revenue"]));
            foreach (var row in rows) row["share"] = totalRevenue <= 0 ? 0m : Math.Round(ReportFormatter.Numeric(row["revenue"]) / totalRevenue * 100m, 1);
            return rows
                .Where(r => ctx.Filters.CategoryId is null || Convert.ToInt32(r["categoryId"]) == ctx.Filters.CategoryId)
                .OrderByDescending(r => ReportFormatter.Numeric(r["revenue"])).ToList();
        }

        private static async Task<List<Dictionary<string, object?>>> RouteRepRows(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct)
        {
            var start = DateOnly.FromDateTime(ctx.Filters.StartDate.Date);
            var end = DateOnly.FromDateTime(ctx.Filters.EndDate.Date);
            var query = db.DailyRepRouteSheets.AsNoTracking().Include(s => s.SalesRep).Include(s => s.RouteSheetItems).Where(s => s.RouteDate >= start && s.RouteDate <= end);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(s => s.SalesRepId == ctx.CurrentSalesRepId);
            var sheets = await query.ToListAsync(ct);
            return sheets.GroupBy(s => new { s.SalesRepId, RepName = s.SalesRep?.FullName ?? "Unassigned", s.RouteName })
                .Select(g =>
                {
                    var orders = g.Sum(s => s.RouteSheetItems.Sum(i => i.RepOrderRequestedQty));
                    var loaded = g.Sum(s => s.RouteSheetItems.Sum(i => i.TodayNewlyLoadedQty));
                    var sold = g.Sum(s => s.RouteSheetItems.Sum(i => i.QtySold));
                    var closing = g.Sum(s => s.RouteSheetItems.Sum(i => i.ClosingStockQty));
                    var damaged = g.Sum(s => s.RouteSheetItems.Sum(i => i.QtyDamaged));
                    var revenue = g.Sum(s => s.RouteSheetItems.Sum(i => i.QtySold * i.UnitSellingPrice));
                    var collected = g.Sum(s => s.CashCollected + s.BankTransferCollected + s.PosTerminalCollected);
                    var outstanding = g.Sum(s => s.NewOutstandingAccountBalance ?? 0m);
                    return new Dictionary<string, object?>
                    {
                        ["rep"] = g.Key.RepName, ["route"] = g.Key.RouteName,
                        ["orders"] = orders, ["loaded"] = loaded, ["sold"] = sold, ["closing"] = closing, ["damaged"] = damaged,
                        ["revenue"] = revenue, ["collected"] = collected, ["outstanding"] = outstanding,
                        ["fill"] = Ratio(loaded, orders), ["sell"] = Ratio(sold, loaded), ["damage"] = Ratio(damaged, loaded)
                    };
                }).OrderByDescending(r => ReportFormatter.Numeric(r["sold"])).ToList();
        }

        private static async Task<List<Dictionary<string, object?>>> RouteItemRows(ManufactureDbContext db, ReportQueryContext ctx, CancellationToken ct, string mode)
        {
            var start = DateOnly.FromDateTime(ctx.Filters.StartDate.Date);
            var end = DateOnly.FromDateTime(ctx.Filters.EndDate.Date);
            var query = db.RouteSheetItems.AsNoTracking().Include(i => i.RouteSheet).ThenInclude(s => s.SalesRep)
                .Include(i => i.Recipe).ThenInclude(r => r.ProductionProductType).ThenInclude(p => p!.ProductionCategory)
                .Where(i => i.RouteSheet.RouteDate >= start && i.RouteSheet.RouteDate <= end);
            if (ctx.Role == "SalesRep" && ctx.CurrentSalesRepId.HasValue) query = query.Where(i => i.RouteSheet.SalesRepId == ctx.CurrentSalesRepId);
            var items = await query.ToListAsync(ct);
            return items
                .Where(i => ctx.Filters.CategoryId is null || i.Recipe?.ProductionProductType?.ProductionCategoryId == ctx.Filters.CategoryId)
                .GroupBy(i => new { i.RouteSheet.SalesRepId, Rep = i.RouteSheet.SalesRep?.FullName ?? "Unassigned", i.RecipeId, i.ProductName })
                .Select(g =>
                {
                    var requested = g.Sum(i => i.RepOrderRequestedQty); var loaded = g.Sum(i => i.TodayNewlyLoadedQty);
                    var sold = g.Sum(i => i.QtySold); var closing = g.Sum(i => i.ClosingStockQty); var damaged = g.Sum(i => i.QtyDamaged);
                    var price = g.First().UnitSellingPrice;
                    return new Dictionary<string, object?>
                    {
                        ["rep"] = g.Key.Rep, ["sku"] = g.Key.ProductName,
                        ["requested"] = requested, ["loaded"] = loaded, ["sold"] = sold, ["closing"] = closing, ["damaged"] = damaged,
                        ["short"] = Math.Max(0, requested - loaded), ["fill"] = Ratio(loaded, requested),
                        ["closingValue"] = closing * price, ["damagedValue"] = damaged * price,
                        ["damage"] = Ratio(damaged, loaded)
                    };
                }).OrderByDescending(r => mode == "shortfall" ? ReportFormatter.Numeric(r["short"]) : ReportFormatter.Numeric(r["closing"])).ToList();
        }

        private static DateTime MonthStart(DateTime date) => new(date.Year, date.Month, 1);

        private static DateTime WeekStart(DateTime date)
        {
            var offset = ((int)date.DayOfWeek + 6) % 7;
            return date.Date.AddDays(-offset);
        }

        private static Dictionary<string, object?> MasterRow(string entity, int total, int active) => new()
        {
            ["entity"] = entity,
            ["total"] = total,
            ["active"] = active,
            ["inactive"] = total - active
        };

        private static Dictionary<string, object?> SettingRow(string setting, string value, string by, DateTime at) => new()
        {
            ["setting"] = setting,
            ["value"] = value,
            ["by"] = by,
            ["at"] = at
        };

        private static Dictionary<string, object?> Issue(string issue, string entity, int count, string severity) => new()
        {
            ["issue"] = issue,
            ["entity"] = entity,
            ["count"] = count,
            ["severity"] = severity
        };

        private static Dictionary<string, object?> MetricRow(string metric, decimal value, string note) => new()
        {
            ["metric"] = metric,
            ["value"] = value,
            ["note"] = note
        };

        private static Dictionary<string, object?> IntegrityRow(string check, decimal difference, string severity) => new()
        {
            ["check"] = check,
            ["difference"] = difference,
            ["severity"] = severity
        };

        private static Dictionary<string, object?> AlertRow(string alert, string module, int count, string severity) => new()
        {
            ["alert"] = alert,
            ["module"] = module,
            ["count"] = count,
            ["severity"] = severity
        };

        private static ReportViewResultDto Build(ReportQueryContext ctx, List<ReportViewColumnDto> columns, List<Dictionary<string, object?>> rows)
        {
            var search = ctx.Filters.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                rows = rows.Where(row => row.Values.Any(value =>
                    value is not null && value.ToString()!.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            var status = ctx.Filters.Status?.Trim();
            if (!string.IsNullOrWhiteSpace(status))
            {
                rows = rows.Where(row => row.TryGetValue("status", out var value)
                    && value?.ToString()?.Contains(status, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            var totals = new Dictionary<string, object?>();
            foreach (var column in columns.Where(c => c.TotalMode != ReportTotalMode.None))
            {
                var numbers = rows.Select(r => ReportFormatter.Numeric(r.GetValueOrDefault(column.Key))).ToList();
                totals[column.Key] = column.TotalMode == ReportTotalMode.Average
                    ? (numbers.Count == 0 ? 0m : Math.Round(numbers.Average(), 1))
                    : numbers.Sum();
            }

            return new ReportViewResultDto
            {
                Key = ctx.Definition.Key,
                Title = ctx.Definition.Title,
                Description = ctx.Definition.Description,
                Group = ctx.Definition.Group,
                DateRangeLabel = $"{ctx.Filters.StartDate:dd MMM yyyy} - {ctx.Filters.EndDate:dd MMM yyyy}",
                Columns = columns,
                Rows = rows,
                Totals = totals,
                TotalRows = rows.Count,
                Page = ctx.Filters.Page,
                PageSize = ctx.Filters.PageSize
            };
        }

        private static ReportViewResultDto Empty(ReportQueryContext ctx) => Build(ctx, [ReportViewColumnDto.Text("message", "Report")], new List<Dictionary<string, object?>>
        {
            new() { ["message"] = "This report is not available in the current delivery." }
        });

        private static void Row(Dictionary<int, Dictionary<string, object?>> rows, int id, string key, object? value)
        {
            if (!rows.TryGetValue(id, out var row))
            {
                row = new Dictionary<string, object?>();
                rows[id] = row;
            }
            row[key] = value;
        }

        private static void Add(Dictionary<int, Dictionary<string, object?>> rows, int id, string key, decimal value)
        {
            if (!rows.TryGetValue(id, out var row))
            {
                row = new Dictionary<string, object?>();
                rows[id] = row;
            }
            row[key] = N(row, key) + value;
        }

        private static decimal N(Dictionary<string, object?> row, string key) =>
            row.TryGetValue(key, out var value) ? ReportFormatter.Numeric(value) : 0m;

        private static decimal Ratio(decimal part, decimal whole) =>
            whole <= 0 ? 0m : Math.Round(part / whole * 100m, 1);

        private static string CategoryName(ManufactureDbContext db, int id) =>
            db.ProductionCategories.AsNoTracking().Where(c => c.Id == id).Select(c => c.Name).FirstOrDefault() ?? "Uncategorised";
    }
}
