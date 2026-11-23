using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services
{
    /// <summary>
    /// Database-backed RBAC matrix. All access checks in the application read
    /// from <see cref="RbacCache"/>, which this service refreshes after every
    /// save or reset.
    /// </summary>
    public class RbacService : IRbacService
    {
        private const string SuperAdminRole = "SuperAdmin";
        private const string FullAccessLevel = "Full Access";

        private readonly ManufactureDbContext _db;
        private readonly RbacCache _cache;

        public RbacService(ManufactureDbContext db, RbacCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<RbacMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _db.Roles
                .AsNoTracking()
                .OrderBy(r => r.Id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName,
                    Description = r.Description
                })
                .ToListAsync(cancellationToken);

            var modules = await _db.RbacModules
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.ModuleName)
                .Select(m => new RbacModuleDto
                {
                    Id = m.Id,
                    ModuleKey = m.ModuleKey,
                    ModuleName = m.ModuleName,
                    Description = m.Description,
                    SortOrder = m.SortOrder,
                    IsActive = m.IsActive
                })
                .ToListAsync(cancellationToken);

            var levels = await _db.RbacPermissionLevels
                .AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.SortOrder)
                .ThenBy(l => l.Rank)
                .Select(l => new RbacPermissionLevelDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Rank = l.Rank,
                    CanView = l.CanView,
                    CanCreate = l.CanCreate,
                    CanEdit = l.CanEdit,
                    CanDelete = l.CanDelete,
                    CanApprove = l.CanApprove,
                    CanPrint = l.CanPrint,
                    CanManage = l.CanManage,
                    CanRequest = l.CanRequest,
                    CanIssue = l.CanIssue,
                    CanDisburse = l.CanDisburse,
                    CanBookOwn = l.CanBookOwn,
                    IsNone = l.IsNone,
                    SortOrder = l.SortOrder
                })
                .ToListAsync(cancellationToken);

            var permissions = await _db.RbacRolePermissions
                .AsNoTracking()
                .Include(p => p.Role)
                .Include(p => p.Module)
                .Include(p => p.PermissionLevel)
                .ToListAsync(cancellationToken);

            var lookup = permissions.ToDictionary(p => (p.RoleId, p.ModuleId));
            var noneLevel = levels.FirstOrDefault(l => l.IsNone) ?? levels.OrderBy(l => l.Rank).FirstOrDefault();

            var rows = modules.Select(module => new RbacMatrixRowDto
            {
                Module = module,
                Cells = roles.Select(role =>
                {
                    lookup.TryGetValue((role.Id, module.Id), out var permission);
                    var level = permission?.PermissionLevel;

                    return new RbacMatrixCellDto
                    {
                        ModuleId = module.Id,
                        RoleId = role.Id,
                        RoleName = role.Name,
                        RoleDisplayName = role.DisplayName,
                        PermissionLevelId = level?.Id ?? noneLevel?.Id ?? 0,
                        PermissionLevelName = level?.Name ?? noneLevel?.Name ?? "None",
                        IsLocked = string.Equals(role.Name, SuperAdminRole, StringComparison.OrdinalIgnoreCase)
                    };
                }).ToList()
            }).ToList();

            var lastUpdated = permissions
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefault();

            return new RbacMatrixDto
            {
                Roles = roles,
                Modules = modules,
                PermissionLevels = levels,
                Rows = rows,
                LastUpdatedAt = lastUpdated?.UpdatedAt,
                LastUpdatedBy = lastUpdated?.UpdatedBy
            };
        }

        public async Task SaveMatrixAsync(
            IReadOnlyList<RbacCellInputDto> cells,
            string updatedBy,
            CancellationToken cancellationToken = default)
        {
            if (cells == null || cells.Count == 0)
            {
                return;
            }

            var changedBy = string.IsNullOrWhiteSpace(updatedBy) ? "System" : updatedBy.Trim();
            var roles = await _db.Roles.ToDictionaryAsync(r => r.Id, cancellationToken);
            var modules = await _db.RbacModules.ToDictionaryAsync(m => m.Id, cancellationToken);
            var levels = await _db.RbacPermissionLevels.ToDictionaryAsync(l => l.Id, cancellationToken);
            var superAdmin = roles.Values.FirstOrDefault(r =>
                string.Equals(r.Name, SuperAdminRole, StringComparison.OrdinalIgnoreCase));
            var fullAccess = levels.Values.FirstOrDefault(l =>
                string.Equals(l.Name, FullAccessLevel, StringComparison.OrdinalIgnoreCase));

            if (fullAccess == null)
            {
                throw new InvalidOperationException("The Full Access permission level is not configured.");
            }

            var requested = new Dictionary<(int RoleId, int ModuleId), int>();
            foreach (var cell in cells)
            {
                if (!roles.TryGetValue(cell.RoleId, out var role))
                {
                    throw new InvalidOperationException($"Role {cell.RoleId} does not exist.");
                }

                if (!modules.TryGetValue(cell.ModuleId, out var module) || !module.IsActive)
                {
                    throw new InvalidOperationException($"Module {cell.ModuleId} does not exist or is inactive.");
                }

                if (!levels.TryGetValue(cell.PermissionLevelId, out var level) || !level.IsActive)
                {
                    throw new InvalidOperationException($"Permission level {cell.PermissionLevelId} does not exist or is inactive.");
                }

                var key = (role.Id, module.Id);
                if (requested.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate permission cell for role '{role.Name}' and module '{module.ModuleKey}'.");
                }

                // Super Admin is always full access, regardless of what was posted.
                var levelId = superAdmin != null && role.Id == superAdmin.Id
                    ? fullAccess.Id
                    : level.Id;

                requested[key] = levelId;
            }

            var existing = await _db.RbacRolePermissions
                .Include(p => p.Role)
                .Include(p => p.Module)
                .Include(p => p.PermissionLevel)
                .ToListAsync(cancellationToken);

            var lookup = existing.ToDictionary(p => (p.RoleId, p.ModuleId));
            var now = DateTime.UtcNow;

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            foreach (var ((roleId, moduleId), levelId) in requested)
            {
                var role = roles[roleId];
                var module = modules[moduleId];
                var newLevel = levels[levelId];

                if (lookup.TryGetValue((roleId, moduleId), out var permission))
                {
                    if (permission.PermissionLevelId == levelId)
                    {
                        continue;
                    }

                    AddAudit(role, module, permission.PermissionLevel, newLevel, changedBy);
                    permission.PermissionLevelId = levelId;
                    permission.UpdatedAt = now;
                    permission.UpdatedBy = changedBy;
                }
                else
                {
                    var newPermission = new RbacRolePermission
                    {
                        RoleId = roleId,
                        ModuleId = moduleId,
                        PermissionLevelId = levelId,
                        UpdatedAt = now,
                        UpdatedBy = changedBy
                    };

                    _db.RbacRolePermissions.Add(newPermission);
                    AddAudit(role, module, null, newLevel, changedBy);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await RefreshCacheAsync(cancellationToken);
        }

        public async Task ResetToDefaultsAsync(string updatedBy, CancellationToken cancellationToken = default)
        {
            var changedBy = string.IsNullOrWhiteSpace(updatedBy) ? "System" : updatedBy.Trim();
            var roles = await _db.Roles.ToDictionaryAsync(r => r.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var modules = await _db.RbacModules.ToDictionaryAsync(m => m.ModuleKey, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var levels = await _db.RbacPermissionLevels.ToDictionaryAsync(l => l.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var existing = await _db.RbacRolePermissions
                .Include(p => p.Role)
                .Include(p => p.Module)
                .Include(p => p.PermissionLevel)
                .ToListAsync(cancellationToken);

            var existingLookup = existing.ToDictionary(p => (p.RoleId, p.ModuleId));
            var now = DateTime.UtcNow;

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            _db.RbacRolePermissions.RemoveRange(existing);

            foreach (var (moduleKey, roleName, levelName) in RbacDefaults.Matrix)
            {
                if (!roles.TryGetValue(roleName, out var role) ||
                    !modules.TryGetValue(moduleKey, out var module) ||
                    !levels.TryGetValue(levelName, out var level))
                {
                    continue;
                }

                _db.RbacRolePermissions.Add(new RbacRolePermission
                {
                    RoleId = role.Id,
                    ModuleId = module.Id,
                    PermissionLevelId = level.Id,
                    UpdatedAt = now,
                    UpdatedBy = changedBy
                });

                existingLookup.TryGetValue((role.Id, module.Id), out var previous);
                AddAudit(role, module, previous?.PermissionLevel, level, changedBy);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await RefreshCacheAsync(cancellationToken);
        }

        public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
        {
            var matrix = await GetMatrixAsync(cancellationToken);
            _cache.SetSnapshot(matrix);
        }

        private void AddAudit(
            Role role,
            RbacModule module,
            RbacPermissionLevel? oldLevel,
            RbacPermissionLevel newLevel,
            string changedBy)
        {
            _db.RbacPermissionAudits.Add(new RbacPermissionAudit
            {
                RoleId = role.Id,
                RoleName = role.Name,
                ModuleId = module.Id,
                ModuleKey = module.ModuleKey,
                OldPermissionLevelId = oldLevel?.Id,
                OldPermissionLevelName = oldLevel?.Name,
                NewPermissionLevelId = newLevel.Id,
                NewPermissionLevelName = newLevel.Name,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow
            });
        }
    }
}
