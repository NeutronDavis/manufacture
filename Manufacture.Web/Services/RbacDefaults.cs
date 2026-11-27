using Manufacture.Models.Domain;
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

        public static readonly IReadOnlyList<(string ModuleKey, UserRole Role, string LevelName)> Matrix =
            new List<(string, UserRole, string)>
            {
                ("Recipes", UserRole.SuperAdmin, "Full Access"),
                ("Recipes", UserRole.ProductionManager, "View, Edit"),
                ("Recipes", UserRole.StoreManager, "View Only"),
                ("Recipes", UserRole.SalesRep, "None"),
                ("Recipes", UserRole.Customer, "None"),
                ("Recipes", UserRole.HrPayrollManager, "None"),

                ("Production", UserRole.SuperAdmin, "Full Access"),
                ("Production", UserRole.ProductionManager, "View, Edit"),
                ("Production", UserRole.StoreManager, "View Only"),
                ("Production", UserRole.SalesRep, "None"),
                ("Production", UserRole.Customer, "None"),
                ("Production", UserRole.HrPayrollManager, "None"),

                ("Inventory", UserRole.SuperAdmin, "Full Access"),
                ("Inventory", UserRole.ProductionManager, "Request Only"),
                ("Inventory", UserRole.StoreManager, "View, Issue, Edit"),
                ("Inventory", UserRole.SalesRep, "None"),
                ("Inventory", UserRole.Customer, "None"),
                ("Inventory", UserRole.HrPayrollManager, "None"),

                ("Sales", UserRole.SuperAdmin, "Full Access"),
                ("Sales", UserRole.ProductionManager, "None"),
                ("Sales", UserRole.StoreManager, "None"),
                ("Sales", UserRole.SalesRep, "Create, Print"),
                ("Sales", UserRole.Customer, "None"),
                ("Sales", UserRole.HrPayrollManager, "None"),

                ("PreOrders", UserRole.SuperAdmin, "Full Access"),
                ("PreOrders", UserRole.ProductionManager, "View Only"),
                ("PreOrders", UserRole.StoreManager, "None"),
                ("PreOrders", UserRole.SalesRep, "Create, Print"),
                ("PreOrders", UserRole.Customer, "Book Own Order"),
                ("PreOrders", UserRole.HrPayrollManager, "None"),

                ("Dispatch", UserRole.SuperAdmin, "Full Access"),
                ("Dispatch", UserRole.ProductionManager, "None"),
                ("Dispatch", UserRole.StoreManager, "None"),
                ("Dispatch", UserRole.SalesRep, "Create, Print"),
                ("Dispatch", UserRole.Customer, "View Only"),
                ("Dispatch", UserRole.HrPayrollManager, "None"),

                ("HR", UserRole.SuperAdmin, "Full Access"),
                ("HR", UserRole.ProductionManager, "None"),
                ("HR", UserRole.StoreManager, "None"),
                ("HR", UserRole.SalesRep, "None"),
                ("HR", UserRole.Customer, "None"),
                ("HR", UserRole.HrPayrollManager, "Manage & Disburse"),

                ("Fleet", UserRole.SuperAdmin, "Full Access"),
                ("Fleet", UserRole.ProductionManager, "View, Edit"),
                ("Fleet", UserRole.StoreManager, "None"),
                ("Fleet", UserRole.SalesRep, "None"),
                ("Fleet", UserRole.Customer, "None"),
                ("Fleet", UserRole.HrPayrollManager, "None"),

                ("Reports", UserRole.SuperAdmin, "Full Access"),
                ("Reports", UserRole.ProductionManager, "View Only"),
                ("Reports", UserRole.StoreManager, "View Only"),
                ("Reports", UserRole.SalesRep, "View Only"),
                ("Reports", UserRole.Customer, "None"),
                ("Reports", UserRole.HrPayrollManager, "View Only"),

                ("Settings", UserRole.SuperAdmin, "Full Access"),
                ("Settings", UserRole.ProductionManager, "None"),
                ("Settings", UserRole.StoreManager, "None"),
                ("Settings", UserRole.SalesRep, "None"),
                ("Settings", UserRole.Customer, "None"),
                ("Settings", UserRole.HrPayrollManager, "None")
            };
    }
}
