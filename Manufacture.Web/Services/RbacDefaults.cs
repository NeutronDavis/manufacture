using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    /// <summary>
    /// Canonical default RBAC matrix. Used by Reset Defaults so the reset path
    /// and the database seed stay aligned.
    /// </summary>
    public static class RbacDefaults
    {
        public static readonly IReadOnlyList<RbacModuleDto> Modules = new List<RbacModuleDto>
        {
            new() { ModuleKey = "Recipes",   ModuleName = "Recipe Builder & Unit Costing", Description = "Ingredients, packaging formulas, margin targets & selling price", SortOrder = 10 },
            new() { ModuleKey = "Production",ModuleName = "Batch Production Logger",       Description = "Flour bags, target loaf count, actual yield & shift variance",     SortOrder = 20 },
            new() { ModuleKey = "Inventory", ModuleName = "Raw Material Store & Requests", Description = "Flour, sugar, yeast inventory, reorder thresholds & issuance",    SortOrder = 30 },
            new() { ModuleKey = "Sales",     ModuleName = "POS Checkout & Cash Routing",   Description = "Direct counter sales, cashier drawer & multi-channel tender",     SortOrder = 40 },
            new() { ModuleKey = "PreOrders", ModuleName = "Pre-Order Booking Portal",      Description = "Supermarket advance standing bookings & dispatch scheduling",     SortOrder = 50 },
            new() { ModuleKey = "Dispatch",  ModuleName = "Print-to-Load Dispatch Slips",  Description = "Vehicle gate-pass security verification & loading ticket stamp",  SortOrder = 60 },
            new() { ModuleKey = "HR",        ModuleName = "Staff & Payroll Engine",        Description = "Guarantors, salary advance deductions & monthly wage runs",       SortOrder = 70 },
            new() { ModuleKey = "Fleet",     ModuleName = "Logistics & Fuel Logs",         Description = "Delivery vehicles, driver routes & fuel consumption receipts",    SortOrder = 80 },
            new() { ModuleKey = "Reports",   ModuleName = "Executive Reports & Analytics", Description = "Operational, financial and logistics reconciliation with exports", SortOrder = 90 },
            new() { ModuleKey = "Settings",  ModuleName = "System Settings & RBAC",        Description = "User accounts, terminal PINs, color themes & RBAC permissions",   SortOrder = 100 }
        };

        public static readonly IReadOnlyList<RbacPermissionLevelDto> Levels = new List<RbacPermissionLevelDto>
        {
            new() { Name = "None", Rank = 0, IsNone = true, SortOrder = 0 },
            new() { Name = "View Only", Rank = 1, CanView = true, SortOrder = 10 },
            new() { Name = "Request Only", Rank = 2, CanView = true, CanRequest = true, SortOrder = 20 },
            new() { Name = "View, Edit", Rank = 3, CanView = true, CanEdit = true, SortOrder = 30 },
            new() { Name = "View, Issue, Edit", Rank = 4, CanView = true, CanEdit = true, CanIssue = true, SortOrder = 40 },
            new() { Name = "Create, Print", Rank = 5, CanView = true, CanCreate = true, CanPrint = true, SortOrder = 50 },
            new() { Name = "Book Own Order", Rank = 6, CanView = true, CanCreate = true, CanBookOwn = true, SortOrder = 60 },
            new() { Name = "Manage & Disburse", Rank = 7, CanView = true, CanEdit = true, CanApprove = true, CanManage = true, CanDisburse = true, SortOrder = 70 },
            new()
            {
                Name = "Full Access", Rank = 8, CanView = true, CanCreate = true, CanEdit = true,
                CanDelete = true, CanApprove = true, CanPrint = true, CanManage = true,
                CanRequest = true, CanIssue = true, CanDisburse = true, CanBookOwn = true,
                SortOrder = 80
            }
        };

        public static readonly IReadOnlyList<(string ModuleKey, string RoleName, string LevelName)> Matrix =
            new List<(string, string, string)>
            {
                ("Recipes", "SuperAdmin", "Full Access"),
                ("Recipes", "ProductionManager", "View, Edit"),
                ("Recipes", "StoreManager", "View Only"),
                ("Recipes", "SalesRep", "View Only"),
                ("Recipes", "Customer", "None"),
                ("Recipes", "HrPayrollManager", "None"),

                ("Production", "SuperAdmin", "Full Access"),
                ("Production", "ProductionManager", "View, Edit"),
                ("Production", "StoreManager", "View Only"),
                ("Production", "SalesRep", "None"),
                ("Production", "Customer", "None"),
                ("Production", "HrPayrollManager", "None"),

                ("Inventory", "SuperAdmin", "Full Access"),
                ("Inventory", "ProductionManager", "Request Only"),
                ("Inventory", "StoreManager", "View, Issue, Edit"),
                ("Inventory", "SalesRep", "None"),
                ("Inventory", "Customer", "None"),
                ("Inventory", "HrPayrollManager", "None"),

                ("Sales", "SuperAdmin", "Full Access"),
                ("Sales", "ProductionManager", "None"),
                ("Sales", "StoreManager", "None"),
                ("Sales", "SalesRep", "Create, Print"),
                ("Sales", "Customer", "None"),
                ("Sales", "HrPayrollManager", "None"),

                ("PreOrders", "SuperAdmin", "Full Access"),
                ("PreOrders", "ProductionManager", "View Only"),
                ("PreOrders", "StoreManager", "None"),
                ("PreOrders", "SalesRep", "Create, Print"),
                ("PreOrders", "Customer", "Book Own Order"),
                ("PreOrders", "HrPayrollManager", "None"),

                ("Dispatch", "SuperAdmin", "Full Access"),
                ("Dispatch", "ProductionManager", "None"),
                ("Dispatch", "StoreManager", "None"),
                ("Dispatch", "SalesRep", "Create, Print"),
                ("Dispatch", "Customer", "View Only"),
                ("Dispatch", "HrPayrollManager", "None"),

                ("HR", "SuperAdmin", "Full Access"),
                ("HR", "ProductionManager", "None"),
                ("HR", "StoreManager", "None"),
                ("HR", "SalesRep", "None"),
                ("HR", "Customer", "None"),
                ("HR", "HrPayrollManager", "Manage & Disburse"),

                ("Fleet", "SuperAdmin", "Full Access"),
                ("Fleet", "ProductionManager", "View, Edit"),
                ("Fleet", "StoreManager", "None"),
                ("Fleet", "SalesRep", "None"),
                ("Fleet", "Customer", "None"),
                ("Fleet", "HrPayrollManager", "None"),

                ("Reports", "SuperAdmin", "Full Access"),
                ("Reports", "ProductionManager", "View Only"),
                ("Reports", "StoreManager", "View Only"),
                ("Reports", "SalesRep", "View Only"),
                ("Reports", "Customer", "None"),
                ("Reports", "HrPayrollManager", "None"),

                ("Settings", "SuperAdmin", "Full Access"),
                ("Settings", "ProductionManager", "None"),
                ("Settings", "StoreManager", "None"),
                ("Settings", "SalesRep", "None"),
                ("Settings", "Customer", "None"),
                ("Settings", "HrPayrollManager", "None")
            };
    }
}
