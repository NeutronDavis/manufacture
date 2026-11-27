using Manufacture.Models.Domain;
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
            var roleIds = Enum.GetValues<UserRole>().Select(role => (int)role).ToHashSet();

            Assert.Equal(10, RbacDefaults.Modules.Count);
            Assert.Equal(9, RbacDefaults.Levels.Count);
            Assert.Equal(60, RbacDefaults.Matrix.Count);

            var moduleKeys = RbacDefaults.Modules.Select(m => m.ModuleKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var levelNames = RbacDefaults.Levels.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (moduleKey, role, levelName) in RbacDefaults.Matrix)
            {
                Assert.Contains(moduleKey, moduleKeys);
                Assert.Contains((int)role, roleIds);
                Assert.Contains(levelName, levelNames);
            }
        }

        [Fact]
        public void Cache_ResolvesCapabilitiesFromMatrix()
        {
            var cache = new RbacCache();
            cache.SetSnapshot(BuildMatrixFromDefaults());

            Assert.True(cache.HasAccess((int)UserRole.SuperAdmin, "Settings"));
            Assert.True(cache.HasAccess((int)UserRole.ProductionManager, "Recipes"));
            Assert.True(cache.CanEdit((int)UserRole.ProductionManager, "Recipes"));
            Assert.True(cache.CanRequest((int)UserRole.ProductionManager, "Inventory"));
            Assert.True(cache.CanIssue((int)UserRole.StoreManager, "Inventory"));
            Assert.True(cache.CanBookOwn((int)UserRole.Customer, "PreOrders"));

            Assert.False(cache.HasAccess((int)UserRole.Customer, "Reports"));
            Assert.False(cache.CanEdit((int)UserRole.SalesRep, "Recipes"));
            Assert.False(cache.CanManage((int)UserRole.SalesRep, "Sales"));

            // Missing/unknown role IDs fail closed.
            Assert.False(cache.HasAccess(0, "Settings"));
            Assert.False(cache.HasAccess(99, "Sales"));
        }

        private static RbacMatrixDto BuildMatrixFromDefaults()
        {
            var roles = Enum.GetValues<UserRole>()
                .Select(role => new RoleDto
                {
                    Id = (int)role,
                    Name = role.ToString(),
                    DisplayName = role.ToString()
                })
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
                x => (x.ModuleKey, (int)x.Role),
                x => x.LevelName);

            var rows = modules.Select(module => new RbacMatrixRowDto
            {
                Module = module,
                Cells = roles.Select(role =>
                {
                    var levelName = matrix.GetValueOrDefault((module.ModuleKey, role.Id), "None");
                    var level = levels.First(l => l.Name == levelName);

                    return new RbacMatrixCellDto
                    {
                        ModuleId = module.Id,
                        RoleId = role.Id,
                        RoleName = role.Name,
                        RoleDisplayName = role.DisplayName,
                        PermissionLevelId = level.Id,
                        PermissionLevelName = level.Name,
                        IsLocked = role.Id == (int)UserRole.SuperAdmin
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
    }
}
