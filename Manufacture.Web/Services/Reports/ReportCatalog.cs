using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Services.Reports
{
    /// <summary>
    /// Code-defined report catalogue. Add a definition here and a matching
    /// handler in <see cref="ReportHandlers"/> to expose a new report.
    /// Visibility is keyed on dbo.Roles.Id, never on role names.
    /// </summary>
    public static class ReportCatalog
    {
        private static readonly UserRole[] ExecutiveRoles = [UserRole.SuperAdmin, UserRole.ProductionManager];
        private static readonly UserRole[] SalesRoles = [UserRole.SuperAdmin, UserRole.SalesRep];
        private static readonly UserRole[] InventoryRoles = [UserRole.SuperAdmin, UserRole.StoreManager, UserRole.ProductionManager];
        private static readonly UserRole[] ProductionRoles = [UserRole.SuperAdmin, UserRole.ProductionManager];
        private static readonly UserRole[] HrRoles = [UserRole.SuperAdmin, UserRole.HrPayrollManager];
        private static readonly UserRole[] FleetRoles = [UserRole.SuperAdmin];
        private static readonly UserRole[] AdminRoles = [UserRole.SuperAdmin];

        public static readonly IReadOnlyList<ReportDefinitionDto> All =
        [
            Define("exec-daily-funnel", "Daily Operating Funnel", "Orders, production, loading, sales, closing stock, damage and customer returns for the selected period.", "Executive", "fa-solid fa-filter", ExecutiveRoles, true, false, false, 30),
            Define("exec-product-line", "Product Line & SKU Performance", "Volume, revenue, production, loading, sales and returns by production category and SKU.", "Executive", "fa-solid fa-boxes-stacked", ExecutiveRoles, true, true, false, 30),
            Define("exec-rep-performance", "Sales Rep Performance & Cash Variance", "Rep-level requested, loaded, sold, closing stock, damage, revenue, collections and outstanding balance.", "Executive", "fa-solid fa-user-group", SalesRoles, true, true, false, 30),
            Define("exec-revenue-cash-debt", "Revenue-to-Cash-to-Debt Bridge", "Expected revenue against collections, outstanding balances and credit exposure.", "Executive", "fa-solid fa-money-bill-transfer", SalesRoles, false, true, false, 30),
            Define("exec-unit-economics", "Unit Economics / Product Profitability", "Selling price, material cost per unit, actual batch unit cost and product margin.", "Executive", "fa-solid fa-scale-balanced", ExecutiveRoles, true, false, false, 90),

            Define("sales-register", "Sales Register", "Order-level sales register with customer, rep, terminal, payment and balance status.", "Sales & POS", "fa-solid fa-receipt", SalesRoles, false, true, true, 30),
            Define("sales-product", "Product & Product-Line Sales", "Units sold, bonus units, line revenue and share by product category and SKU.", "Sales & POS", "fa-solid fa-tags", SalesRoles, true, true, false, 30),
            Define("sales-customer-ledger", "Customer Sales Ledger / Statement", "Customer order value, payments, balance, returns and net exposure.", "Sales & POS", "fa-solid fa-book-open", SalesRoles, false, false, false, 90),
            Define("sales-ar-ageing", "Accounts Receivable Ageing", "Outstanding customer balances aged from order date.", "Sales & POS", "fa-solid fa-clock", SalesRoles, false, false, false, 90),
            Define("sales-collections", "Collections & Payment-Mode Register", "Payment register with cash, transfer, POS and credit split.", "Sales & POS", "fa-solid fa-hand-holding-dollar", SalesRoles, false, true, false, 30),
            Define("sales-route-performance", "Sales Rep Route Performance", "Requested, loaded, sold, closing stock, damaged and replacements by route and rep.", "Sales & POS", "fa-solid fa-route", SalesRoles, true, true, false, 30),
            Define("sales-loading-shortfall", "Ordered vs Loaded", "Requested versus physically loaded quantities and loading shortfall.", "Sales & POS", "fa-solid fa-truck-ramp-box", SalesRoles, true, true, false, 30),
            Define("sales-closing-damage", "Closing Stock & Damage Write-Off", "Good closing stock and damaged quantities written off by rep and product.", "Sales & POS", "fa-solid fa-boxes-packing", SalesRoles, true, true, false, 30),
            Define("sales-customer-returns", "Customer Returns & Replacements", "Prior-sale customer returns, refund value and replacement quantity.", "Sales & POS", "fa-solid fa-rotate-left", SalesRoles, true, true, false, 90),
            Define("sales-supermarket-credit", "Supermarket Credit Exposure", "Account credit limit, outstanding exposure, utilization and headroom.", "Sales & POS", "fa-solid fa-store", SalesRoles, false, true, false, 90),
            Define("sales-bonus-discount", "Bonus & Discount Leakage", "Bonus units, discounts and price realization by product and customer.", "Sales & POS", "fa-solid fa-gift", SalesRoles, true, true, false, 90),
            Define("sales-fulfillment", "Order Fulfillment Pipeline & Cycle Time", "Order status, loading and dispatch timestamps and fulfilment backlog.", "Sales & POS", "fa-solid fa-list-check", SalesRoles, false, true, true, 30),

            Define("inventory-stock-valuation", "Stock on Hand & Valuation", "Quantity, package conversion, unit cost and stock value by item and category.", "Inventory", "fa-solid fa-warehouse", InventoryRoles, false, false, false, 1),
            Define("inventory-low-stock", "Low-Stock / Reorder Worklist", "Items at or below reorder threshold with suggested reorder quantity.", "Inventory", "fa-solid fa-triangle-exclamation", InventoryRoles, false, false, false, 1),
            Define("inventory-movements", "Stock Movement Ledger", "Receipt and issue ledger with running balance and references.", "Inventory", "fa-solid fa-arrow-right-arrow-left", InventoryRoles, false, false, false, 30),
            Define("inventory-requisitions", "Requisition Register & Approval Aging", "Store requisitions, status, department, approval and issue timing.", "Inventory", "fa-solid fa-clipboard-check", InventoryRoles, false, false, true, 90),
            Define("inventory-requisition-fill", "Requisition Fill / Issue Variance", "Requested versus issued quantity and short-issue percentage.", "Inventory", "fa-solid fa-scale-unbalanced", InventoryRoles, false, false, false, 90),
            Define("inventory-department-consumption", "Departmental Material Consumption", "Issued material quantity and value by department and item.", "Inventory", "fa-solid fa-building", InventoryRoles, false, false, false, 90),
            Define("inventory-receipts-issues", "Receipts vs Issues Summary", "Opening, receipts, issues and closing movement by item.", "Inventory", "fa-solid fa-arrow-up-right-dots", InventoryRoles, false, false, false, 90),
            Define("inventory-slow-moving", "Slow-Moving / Non-Moving Stock", "Items with no recent issue and the capital tied up in them.", "Inventory", "fa-solid fa-hourglass-half", InventoryRoles, false, false, false, 90),
            Define("inventory-supplier-price", "Supplier Price List & Change History", "Current supplier price and ingredient price-change history.", "Inventory", "fa-solid fa-money-bill-wave", InventoryRoles, false, false, true, 180),

            Define("production-daily", "Daily Production Summary", "Batch output, target, variance and cost by date, shift and product category.", "Production", "fa-solid fa-industry", ProductionRoles, true, false, false, 30),
            Define("production-yield", "Batch Yield & Variance", "Target versus actual output and yield compliance for every batch.", "Production", "fa-solid fa-chart-simple", ProductionRoles, true, false, true, 30),
            Define("production-batch-cost", "Batch Cost & Unit-Cost Variance", "Batch material cost, actual unit cost and expected-cost variance.", "Production", "fa-solid fa-coins", ProductionRoles, true, false, false, 30),
            Define("production-recipe-margin", "Recipe Profitability & Margin", "Recipe selling price, material cost, batch unit cost and margin.", "Production", "fa-solid fa-percent", ProductionRoles, true, false, false, 30),
            Define("production-defects", "Factory Defects Analysis", "Defective quantity, defect rate and reasons by batch and product.", "Production", "fa-solid fa-bug", ProductionRoles, true, false, true, 30),
            Define("production-price-audit", "Ingredient Price Change Audit", "Submitted, approved and rejected ingredient price changes.", "Production", "fa-solid fa-clock-rotate-left", ProductionRoles, false, false, true, 90),
            Define("production-volume", "Production Volume by Category / SKU", "Actual production volume and share by category and recipe.", "Production", "fa-solid fa-cubes", ProductionRoles, true, false, false, 30),
            Define("production-shift-baker", "Shift & Baker Performance", "Batch count, output, yield, defects and cost by shift and baker.", "Production", "fa-solid fa-users-gear", ProductionRoles, false, false, false, 30),

            Define("hr-headcount", "Employee Master & Headcount", "Headcount, employment status, department, role, salary and tenure.", "HR & Payroll", "fa-solid fa-users", HrRoles, false, false, false, 365),
            Define("hr-hires-terminations", "New Hires, Terminations & Attrition", "Hires, terminations, net headcount and attrition by period and department.", "HR & Payroll", "fa-solid fa-user-clock", HrRoles, false, false, false, 365),
            Define("payroll-register", "Payroll Register", "Employee-level payroll earnings, deductions, net pay and payment status.", "HR & Payroll", "fa-solid fa-money-check-dollar", HrRoles, false, false, true, 180),
            Define("payroll-summary", "Payroll Summary by Department / Role", "Payroll cost, headcount and net pay by department and job role.", "HR & Payroll", "fa-solid fa-building-user", HrRoles, false, false, false, 180),
            Define("payroll-deductions", "Earnings & Deductions Breakdown", "Tax, pension, health, loan and disciplinary deductions by type.", "HR & Payroll", "fa-solid fa-receipt", HrRoles, false, false, false, 180),
            Define("payroll-advances", "Salary Advance / Loan Register", "Advance principal, monthly recovery, repaid amount and outstanding balance.", "HR & Payroll", "fa-solid fa-hand-holding-dollar", HrRoles, false, false, true, 365),
            Define("payroll-reconciliation", "Payroll Run Reconciliation", "Run totals against employee records, payment status and variances.", "HR & Payroll", "fa-solid fa-scale-balanced", HrRoles, false, false, true, 180),
            Define("payroll-bank-schedule", "Bank Payment Schedule", "Employee net pay, bank details and payment status by period.", "HR & Payroll", "fa-solid fa-building-columns", HrRoles, false, false, true, 180),
            Define("payroll-compliance", "Employee Data Compliance", "Missing bank, guarantor, emergency-contact and duplicate staff data.", "HR & Payroll", "fa-solid fa-shield-halved", HrRoles, false, false, false, 365),
            Define("payroll-cost-per-unit", "Payroll Cost per Production Unit", "Monthly payroll cost against production output and cost per unit.", "HR & Payroll", "fa-solid fa-calculator", HrRoles, true, false, false, 365),

            Define("fleet-vehicles", "Vehicle Register & Status", "Vehicle type, function, assignment, status, odometer and last service.", "Fleet & Logistics", "fa-solid fa-truck", FleetRoles, false, false, true, 365),
            Define("fleet-fuel", "Fuel Purchase & Cost", "Fuel purchases, litres, price per litre and total cost by vehicle.", "Fleet & Logistics", "fa-solid fa-gas-pump", FleetRoles, false, false, false, 90),
            Define("fleet-maintenance", "Maintenance Cost & Service History", "Maintenance and repair costs by vehicle, date and vendor.", "Fleet & Logistics", "fa-solid fa-wrench", FleetRoles, false, false, false, 180),
            Define("fleet-service-due", "Service Due / Last-Service Aging", "Days since last service and vehicles past the service threshold.", "Fleet & Logistics", "fa-solid fa-calendar-check", FleetRoles, false, false, false, 365),
            Define("fleet-weekly-cost", "Weekly Fleet Running Cost", "Fuel, maintenance, incidentals and total running cost by week and vehicle.", "Fleet & Logistics", "fa-solid fa-calendar-week", FleetRoles, false, false, false, 180),
            Define("fleet-charter", "Charter Revenue & Profitability", "Charter gross fee, incidentals, net revenue and margin.", "Fleet & Logistics", "fa-solid fa-route", FleetRoles, false, false, true, 180),
            Define("fleet-utilization", "Driver / Vehicle Utilization", "Route sheets, trips, units sold, fuel and maintenance by vehicle and driver.", "Fleet & Logistics", "fa-solid fa-gauge-high", FleetRoles, false, true, false, 90),
            Define("fleet-cost-per-unit", "Fleet Cost per Unit Sold", "Fuel and maintenance cost divided by units sold by vehicle.", "Fleet & Logistics", "fa-solid fa-coins", FleetRoles, false, false, false, 90),
            Define("fleet-trip-incidentals", "Trip Incidentals Report", "Driver allowance, security escort and road levies by charter.", "Fleet & Logistics", "fa-solid fa-suitcase-rolling", FleetRoles, false, false, true, 180),
            Define("fleet-fuel-comparison", "Fuel Type / Station Comparison", "Fuel price and volume comparison by station and fuel type.", "Fleet & Logistics", "fa-solid fa-chart-line", FleetRoles, false, false, false, 180),

            Define("admin-user-access", "User Access Register", "Users, roles, linked employee/customer and last login.", "Settings & Access", "fa-solid fa-user-shield", AdminRoles, false, false, true, 365),
            Define("admin-role-distribution", "Role Distribution & Access Coverage", "User count and module access coverage by role.", "Settings & Access", "fa-solid fa-users-rectangle", AdminRoles, false, false, false, 365),
            Define("admin-rbac-matrix", "RBAC Matrix / Effective Permissions", "Role-by-module permission matrix and capability flags.", "Settings & Access", "fa-solid fa-table-cells-large", AdminRoles, false, false, false, 365),
            Define("admin-permission-audit", "Permission Change Audit", "RBAC permission changes, old/new levels and changed-by history.", "Settings & Access", "fa-solid fa-clock-rotate-left", AdminRoles, false, false, false, 365),
            Define("admin-dormant-users", "Dormant / Never-Logged-In Users", "Users with no login or prolonged inactivity.", "Settings & Access", "fa-solid fa-user-slash", AdminRoles, false, false, true, 365),
            Define("admin-login-coverage", "Employee-to-Login Coverage", "Employees with and without user accounts and orphan user links.", "Settings & Access", "fa-solid fa-link", AdminRoles, false, false, false, 365),
            Define("admin-master-data", "Master Data Register", "Production, HR and measurement master data with active status.", "Settings & Access", "fa-solid fa-database", AdminRoles, false, false, true, 365),
            Define("admin-configuration", "Configuration Register", "Treasury and payroll policy settings and last updated metadata.", "Settings & Access", "fa-solid fa-sliders", AdminRoles, false, false, false, 365),
            Define("admin-audit-extract", "General Audit Extract", "System audit events by user, action, entity and date.", "Settings & Access", "fa-solid fa-list-check", AdminRoles, false, false, false, 90),
            Define("admin-data-integrity", "Data Integrity Exceptions", "Missing roles, permissions, department links and duplicate master data.", "Settings & Access", "fa-solid fa-triangle-exclamation", AdminRoles, false, false, false, 365),

            Define("sales-terminal-productivity", "Terminal / Cashier Productivity", "Orders, revenue, units and payment mix by terminal and cashier.", "Sales & POS", "fa-solid fa-cash-register", SalesRoles, false, true, false, 30),
            Define("sales-preorders", "Pre-Order Book & Delivery Schedule", "Pre-order quantities, value, balance and delivery status.", "Sales & POS", "fa-solid fa-calendar-check", SalesRoles, false, true, true, 30),
            Define("sales-price-realization", "Price Realization vs List / Agreed Price", "Actual selling price versus current list or agreed account price.", "Sales & POS", "fa-solid fa-tag", SalesRoles, true, true, false, 90),

            Define("inventory-turnover", "Inventory Turnover & Days of Cover", "Issue value, average stock value, turnover and days of cover.", "Inventory", "fa-solid fa-arrows-spin", InventoryRoles, false, false, false, 90),
            Define("inventory-density-coverage", "Unit Conversion / Density Coverage", "Items with density conversion and items missing density.", "Inventory", "fa-solid fa-flask", InventoryRoles, false, false, false, 1),
            Define("inventory-requisition-cycle", "Requisition Cycle-Time", "Request-to-approval and approval-to-issue cycle times.", "Inventory", "fa-solid fa-hourglass-half", InventoryRoles, false, false, true, 90),
            Define("inventory-theoretical-actual", "Theoretical vs Actual Material Usage", "Recipe-based expected usage versus issued material usage.", "Inventory", "fa-solid fa-scale-unbalanced-flip", InventoryRoles, false, false, false, 30),

            Define("production-batch-status", "Batch Status / Aging", "Planned, active, completed and cancelled batches with age.", "Production", "fa-solid fa-list-check", ProductionRoles, true, false, true, 30),
            Define("production-bom-cost", "Recipe BOM Cost Breakdown", "Ingredient quantities, unit costs, line costs and cost share.", "Production", "fa-solid fa-sitemap", ProductionRoles, true, false, false, 30),
            Define("production-demand", "Demand vs Production", "Pre-order demand versus planned and actual production.", "Production", "fa-solid fa-chart-column", ProductionRoles, true, false, false, 30),
            Define("production-to-sales", "Production-to-Sales Fulfillment", "Produced, loaded, sold, closing and returns by product.", "Production", "fa-solid fa-arrow-right-arrow-left", ProductionRoles, true, false, false, 30),

            Define("exec-management-pack", "Management Monthly Pack", "Cross-module production, sales, collections, inventory, payroll and fleet cost.", "Executive", "fa-solid fa-briefcase", AdminRoles, false, false, false, 365),
            Define("exec-working-capital", "Working Capital View", "Inventory value, receivables, credit exposure, advances and unpaid payroll.", "Executive", "fa-solid fa-vault", AdminRoles, false, false, false, 90),
            Define("exec-integrity-pack", "Reconciliation / Integrity Exceptions", "Computed-versus-detail checks across orders, inventory, payroll and fleet.", "Executive", "fa-solid fa-shield-halved", AdminRoles, false, false, false, 90),
            Define("exec-alerts", "Exception / Alert Report", "Low stock, loading shortfall, damage, receivables and service alerts.", "Executive", "fa-solid fa-bell", AdminRoles, false, false, false, 30)
        ];

        public static ReportDefinitionDto? Find(string key) =>
            All.FirstOrDefault(report => string.Equals(report.Key, key, StringComparison.OrdinalIgnoreCase));

        public static List<ReportDefinitionDto> ForRole(int roleId) =>
            roleId == (int)UserRole.SuperAdmin
                ? All.ToList()
                : All.Where(report => report.AllowedRoleIds.Contains(roleId)).ToList();

        private static ReportDefinitionDto Define(
            string key,
            string title,
            string description,
            string group,
            string icon,
            UserRole[] roles,
            bool categoryFilter,
            bool repFilter,
            bool statusFilter,
            int defaultDays) =>
            new()
            {
                Key = key,
                Title = title,
                Description = description,
                Group = group,
                IconClass = icon,
                AllowedRoleIds = roles.Select(role => (int)role).ToList(),
                SupportsCategoryFilter = categoryFilter,
                SupportsRepFilter = repFilter,
                SupportsStatusFilter = statusFilter,
                DefaultDays = defaultDays
            };
    }
}
