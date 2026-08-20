using System;
using System.Collections.Generic;

namespace Manufacture.Services
{
    public class ModulePermission
    {
        public string ModuleKey { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> RoleAccess { get; set; } = new();
    }

    public class MockRbacService
    {
        private static readonly object _lock = new();
        private List<ModulePermission> _matrix = new();

        public static readonly string[] AllRoles = new[]
        {
            "SuperAdmin",
            "ProductionManager",
            "StoreManager",
            "SalesRep",
            "Vendor",
            "HrPayrollManager"
        };

        public static readonly string[] PermissionLevels = new[]
        {
            "None",
            "View Only",
            "Request Only",
            "View, Edit",
            "View, Issue, Edit",
            "Create, Print",
            "Book Own Order",
            "Manage & Disburse",
            "Full Access"
        };

        public MockRbacService()
        {
            ResetToDefaults();
        }

        public List<ModulePermission> GetMatrix()
        {
            lock (_lock)
            {
                // Return deep clone
                var clone = new List<ModulePermission>();
                foreach (var m in _matrix)
                {
                    clone.Add(new ModulePermission
                    {
                        ModuleKey = m.ModuleKey,
                        ModuleName = m.ModuleName,
                        Description = m.Description,
                        RoleAccess = new Dictionary<string, string>(m.RoleAccess)
                    });
                }
                return clone;
            }
        }

        public bool HasAccess(string role, string moduleKey)
        {
            if (string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return true;

            lock (_lock)
            {
                var mod = _matrix.Find(m => string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
                if (mod != null && mod.RoleAccess.TryGetValue(role, out var level))
                {
                    return !string.IsNullOrWhiteSpace(level) && !string.Equals(level, "None", StringComparison.OrdinalIgnoreCase) && level != "—";
                }
                return false;
            }
        }

        public string GetPermissionLevel(string role, string moduleKey)
        {
            lock (_lock)
            {
                var mod = _matrix.Find(m => string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
                if (mod != null && mod.RoleAccess.TryGetValue(role, out var level))
                {
                    return level;
                }
                return "None";
            }
        }

        public void UpdateMatrix(Dictionary<string, Dictionary<string, string>> newMatrix)
        {
            lock (_lock)
            {
                foreach (var mod in _matrix)
                {
                    if (newMatrix.TryGetValue(mod.ModuleKey, out var roles))
                    {
                        foreach (var role in AllRoles)
                        {
                            if (roles.TryGetValue(role, out var level))
                            {
                                mod.RoleAccess[role] = level;
                            }
                        }
                    }
                }
            }
        }

        public void ResetToDefaults()
        {
            lock (_lock)
            {
                _matrix = new List<ModulePermission>
                {
                    new ModulePermission
                    {
                        ModuleKey = "Recipes",
                        ModuleName = "Recipe Builder & Unit Costing",
                        Description = "Ingredients, packaging formulas, margin targets & selling price",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "View, Edit" },
                            { "StoreManager", "View Only" },
                            { "SalesRep", "View Only" },
                            { "Vendor", "None" },
                            { "HrPayrollManager", "None" }
                        }
                    },
                    new ModulePermission
                    {
                        ModuleKey = "Production",
                        ModuleName = "Batch Production Logger",
                        Description = "Flour bags, target loaf count, actual yield & shift variance",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "View, Edit" },
                            { "StoreManager", "View Only" },
                            { "SalesRep", "None" },
                            { "Vendor", "None" },
                            { "HrPayrollManager", "None" }
                        }
                    },
                    new ModulePermission
                    {
                        ModuleKey = "Inventory",
                        ModuleName = "Raw Material Store & Requests",
                        Description = "Flour, sugar, yeast inventory, reorder thresholds & issuance",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "Request Only" },
                            { "StoreManager", "View, Issue, Edit" },
                            { "SalesRep", "None" },
                            { "Vendor", "None" },
                            { "HrPayrollManager", "None" }
                        }
                    },
                    new ModulePermission
                    {
                        ModuleKey = "Sales",
                        ModuleName = "POS Checkout & Cash Routing",
                        Description = "Direct counter sales, cashier drawer & multi-channel tender",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "None" },
                            { "StoreManager", "None" },
                            { "SalesRep", "Create, Print" },
                            { "Vendor", "None" },
                            { "HrPayrollManager", "None" }
                        }
                    },
                    new ModulePermission
                    {
                        ModuleKey = "PreOrders",
                        ModuleName = "Pre-Order Booking Portal",
                        Description = "Supermarket advance standing bookings & dispatch scheduling",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "View Only" },
                            { "StoreManager", "None" },
                            { "SalesRep", "Create, Print" },
                            { "Vendor", "Book Own Order" },
                            { "HrPayrollManager", "None" }
                        }
                    },
                    new ModulePermission
                    {
                        ModuleKey = "Dispatch",
                        ModuleName = "Print-to-Load Dispatch Slips",
                        Description = "Vehicle gate-pass security verification & loading ticket stamp",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "None" },
                            { "StoreManager", "None" },
                            { "SalesRep", "Create, Print" },
                            { "Vendor", "View Only" },
                            { "HrPayrollManager", "None" }
                        }
                    },
                    new ModulePermission
                    {
                        ModuleKey = "HR",
                        ModuleName = "Staff & Payroll Engine",
                        Description = "Guarantors, salary advance deductions & monthly wage runs",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "None" },
                            { "StoreManager", "None" },
                            { "SalesRep", "None" },
                            { "Vendor", "None" },
                            { "HrPayrollManager", "Manage & Disburse" }
                        }
                    },
                    new ModulePermission
                    {
                        ModuleKey = "Fleet",
                        ModuleName = "Logistics & Fuel Logs",
                        Description = "Delivery vehicles, driver routes & fuel consumption receipts",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "View, Edit" },
                            { "StoreManager", "None" },
                            { "SalesRep", "None" },
                            { "Vendor", "None" },
                            { "HrPayrollManager", "None" }
                        }
                    },
                    new ModulePermission
                    {
                        ModuleKey = "Settings",
                        ModuleName = "System Settings & RBAC",
                        Description = "User accounts, terminal PINs, color themes & RBAC permissions",
                        RoleAccess = new Dictionary<string, string>
                        {
                            { "SuperAdmin", "Full Access" },
                            { "ProductionManager", "None" },
                            { "StoreManager", "None" },
                            { "SalesRep", "None" },
                            { "Vendor", "None" },
                            { "HrPayrollManager", "None" }
                        }
                    }
                };
            }
        }
    }
}
