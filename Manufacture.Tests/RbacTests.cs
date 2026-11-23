using Manufacture.Models.DTOs;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    public class RbacTests
    {
        [Fact]
        public void Defaults_ContainEveryModuleRoleCell()
        {
            var roleNames = new[]
            {
                "SuperAdmin", "ProductionManager", "StoreManager",
                "SalesRep", "Customer", "HrPayrollManager"
            };

            Assert.Equal(10, RbacDefaults.Modules.Count);
            Assert.Equal(9, RbacDefaults.Levels.Count);
            Assert.Equal(60, RbacDefaults.Matrix.Count);

            var moduleKeys = RbacDefaults.Modules.Select(m => m.ModuleKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var levelNames = RbacDefaults.Levels.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (moduleKey, roleName, levelName) in RbacDefaults.Matrix)
            {
                Assert.Contains(moduleKey, moduleKeys);
                Assert.Contains(roleName, roleNames);
                Assert.Contains(levelName, levelNames);
            }
        }

        [Fact]
        public void Cache_ResolvesCapabilitiesFromMatrix()
        {
            var cache = new RbacCache();
            cache.SetSnapshot(BuildMatrixFromDefaults());

            Assert.True(cache.HasAccess("SuperAdmin", "Settings"));
            Assert.True(cache.HasAccess("ProductionManager", "Recipes"));
            Assert.True(cache.CanEdit("ProductionManager", "Recipes"));
            Assert.True(cache.CanRequest("ProductionManager", "Inventory"));
            Assert.True(cache.CanIssue("StoreManager", "Inventory"));
            Assert.True(cache.CanBookOwn("Customer", "PreOrders"));

            Assert.False(cache.HasAccess("Customer", "Reports"));
            Assert.False(cache.CanEdit("SalesRep", "Recipes"));
            Assert.False(cache.CanManage("SalesRep", "Sales"));
        }

        private static RbacMatrixDto BuildMatrixFromDefaults()
        {
            var roleNames = new[]
            {
                "SuperAdmin", "ProductionManager", "StoreManager",
                "SalesRep", "Customer", "HrPayrollManager"
            };

            var roles = roleNames
                .Select((name, index) => new RoleDto { Id = index + 1, Name = name, DisplayName = name })
                .ToList();

            var modules = RbacDefaults.Modules
                .Select((module, index) =>
                {
                    var copy = new RbacModuleDto
                    {
                        Id = index + 1,
                        ModuleKey = module.ModuleKey,
                        ModuleName = module.ModuleName,
                        Description = module.Description,
                        SortOrder = module.SortOrder,
                        IsActive = true
                    };
                    return copy;
                })
                .ToList();

            var levels = RbacDefaults.Levels
                .Select((level, index) =>
                {
                    var copy = new RbacPermissionLevelDto
                    {
                        Id = index + 1,
                        Name = level.Name,
                        Rank = level.Rank,
                        CanView = level.CanView,
                        CanCreate = level.CanCreate,
                        CanEdit = level.CanEdit,
                        CanDelete = level.CanDelete,
                        CanApprove = level.CanApprove,
                        CanPrint = level.CanPrint,
                        CanManage = level.CanManage,
                        CanRequest = level.CanRequest,
                        CanIssue = level.CanIssue,
                        CanDisburse = level.CanDisburse,
                        CanBookOwn = level.CanBookOwn,
                        IsNone = level.IsNone,
                        SortOrder = level.SortOrder
                    };
                    return copy;
                })
                .ToList();

            var matrix = RbacDefaults.Matrix.ToDictionary(
                x => (x.ModuleKey, x.RoleName),
                x => x.LevelName,
                new MatrixKeyComparer());

            var rows = modules.Select(module => new RbacMatrixRowDto
            {
                Module = module,
                Cells = roles.Select(role =>
                {
                    var levelName = matrix.GetValueOrDefault((module.ModuleKey, role.Name), "None");
                    var level = levels.First(l => l.Name == levelName);

                    return new RbacMatrixCellDto
                    {
                        ModuleId = module.Id,
                        RoleId = role.Id,
                        RoleName = role.Name,
                        RoleDisplayName = role.DisplayName,
                        PermissionLevelId = level.Id,
                        PermissionLevelName = level.Name,
                        IsLocked = role.Name == "SuperAdmin"
                    };
                }).ToList()
            }).ToList();

            return new RbacMatrixDto
            {
                Roles = roles,
                Modules = modules,
                PermissionLevels = levels,
                Rows = rows
            };
        }

        private sealed class MatrixKeyComparer : IEqualityComparer<(string ModuleKey, string RoleName)>
        {
            public bool Equals((string ModuleKey, string RoleName) x, (string ModuleKey, string RoleName) y) =>
                string.Equals(x.ModuleKey, y.ModuleKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.RoleName, y.RoleName, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string ModuleKey, string RoleName) obj) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ModuleKey),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.RoleName));
        }
    }
}
